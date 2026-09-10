using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class PieceServiceTests
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

    private static (PieceService service, Mock<IFirestoreClient> client) CreateService(IEnumerable<FirestoreDocument>? docs = null)
    {
        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.ListDocumentsAsync("pieces", It.IsAny<CancellationToken>()))
            .ReturnsAsync((docs ?? []).ToList());
        clientMock
            .Setup(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientMock
            .Setup(c => c.DeleteDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new PieceService(clientMock.Object), clientMock);
    }

    [Fact]
    public async Task 既定では非表示にされた曲が一覧から除外される()
    {
        var docs = new[] { CreatePieceDoc("p1", "曲A"), CreatePieceDoc("p2", "曲B", isArchived: true) };
        var (service, _) = CreateService(docs);

        var pieces = await service.GetAllAsync();

        Assert.Equal(["曲A"], pieces.Select(p => p.Title));
    }

    [Fact]
    public async Task includeArchivedを指定すると非表示の曲も含まれる()
    {
        var docs = new[] { CreatePieceDoc("p1", "曲A"), CreatePieceDoc("p2", "曲B", isArchived: true) };
        var (service, _) = CreateService(docs);

        var pieces = await service.GetAllAsync(includeArchived: true);

        Assert.Equal(["曲A", "曲B"], pieces.Select(p => p.Title));
    }

    [Fact]
    public async Task 他団体の曲は一覧から除外される()
    {
        var docs = new[] { CreatePieceDoc("p1", "自団体の曲", groupId: FirebaseOptions.GroupId), CreatePieceDoc("p2", "他団体の曲", groupId: "other") };
        var (service, _) = CreateService(docs);

        var pieces = await service.GetAllAsync();

        Assert.Equal(["自団体の曲"], pieces.Select(p => p.Title));
    }

    [Fact]
    public async Task 曲名の大文字小文字を無視した順で並び替えられる()
    {
        var docs = new[] { CreatePieceDoc("p1", "banana"), CreatePieceDoc("p2", "Apple") };
        var (service, _) = CreateService(docs);

        var pieces = await service.GetAllAsync();

        Assert.Equal(["Apple", "banana"], pieces.Select(p => p.Title));
    }

    [Fact]
    public async Task 曲を登録すると団体IDと曲名を含めて保存される()
    {
        var (service, client) = CreateService();

        var id = await service.CreateAsync("新しい曲", []);

        client.Verify(c => c.UpsertDocumentAsync(
            "pieces",
            id,
            It.Is<Dictionary<string, object?>>(f => (string)f["groupId"]! == FirebaseOptions.GroupId && (string)f["title"]! == "新しい曲"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 非表示状態を設定すると該当曲のisArchivedフィールドが更新される()
    {
        var (service, client) = CreateService();

        await service.SetArchivedAsync("piece1", true);

        client.Verify(c => c.UpsertDocumentAsync(
            "pieces",
            "piece1",
            It.Is<Dictionary<string, object?>>(f => (bool)f["isArchived"]! == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 曲を削除すると該当IDで削除が呼ばれる()
    {
        var (service, client) = CreateService();

        await service.DeleteAsync("piece1");

        client.Verify(c => c.DeleteDocumentAsync("pieces", "piece1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
