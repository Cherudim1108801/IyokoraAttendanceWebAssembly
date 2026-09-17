using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Repositories;

public class PieceRepositoryTests
{
    private static FirestoreDocument CreatePieceDoc(string id, string title, bool isArchived = false, string groupId = "default") => new()
    {
        Id = id,
        Fields = new Dictionary<string, object?>
        {
            ["groupId"] = groupId,
            ["title"] = title,
            ["parts"] = new List<object?>(),
            ["createdAt"] = DateTime.UtcNow,
            ["isArchived"] = isArchived
        }
    };

    private static (PieceRepository repository, Mock<IFirestoreClient> client) CreateRepository(IEnumerable<FirestoreDocument>? docs = null)
    {
        var list = (docs ?? []).ToList();
        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.QueryDocumentsAsync("pieces", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IReadOnlyDictionary<string, object?> filters, CancellationToken _) =>
                list.Where(d => filters.All(f => d.Fields.TryGetValue(f.Key, out var v) && Equals(v, f.Value))).ToList());
        clientMock
            .Setup(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientMock
            .Setup(c => c.DeleteDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new PieceRepository(clientMock.Object), clientMock);
    }

    [Fact]
    public async Task 他団体の曲は一覧から除外される()
    {
        var docs = new[] { CreatePieceDoc("p1", "自団体の曲", groupId: FirebaseOptions.GroupId), CreatePieceDoc("p2", "他団体の曲", groupId: "other") };
        var (repository, _) = CreateRepository(docs);

        var pieces = await repository.GetAllAsync();

        Assert.Equal(["自団体の曲"], pieces.Select(p => p.Title));
    }

    [Fact]
    public async Task 曲一覧取得はコレクション全体を取得せずgroupIdで絞り込む()
    {
        var docs = new[] { CreatePieceDoc("p1", "自団体の曲", groupId: FirebaseOptions.GroupId) };
        var (repository, client) = CreateRepository(docs);

        await repository.GetAllAsync();

        client.Verify(c => c.QueryDocumentsAsync(
            "pieces",
            It.Is<IReadOnlyDictionary<string, object?>>(f => (string)f["groupId"]! == FirebaseOptions.GroupId),
            It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.ListDocumentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task 曲を登録すると団体IDと曲名を含めて保存される()
    {
        var (repository, client) = CreateRepository();

        await repository.CreateAsync("p1", "新しい曲", [], DateTime.UtcNow);

        client.Verify(c => c.UpsertDocumentAsync(
            "pieces",
            "p1",
            It.Is<Dictionary<string, object?>>(f => (string)f["groupId"]! == FirebaseOptions.GroupId && (string)f["title"]! == "新しい曲"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 非表示状態を設定すると該当曲のisArchivedフィールドが更新される()
    {
        var (repository, client) = CreateRepository();

        await repository.SetArchivedAsync("piece1", true);

        client.Verify(c => c.UpsertDocumentAsync(
            "pieces",
            "piece1",
            It.Is<Dictionary<string, object?>>(f => (bool)f["isArchived"]! == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 曲を削除すると該当IDで削除が呼ばれる()
    {
        var (repository, client) = CreateRepository();

        await repository.DeleteAsync("piece1");

        client.Verify(c => c.DeleteDocumentAsync("pieces", "piece1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
