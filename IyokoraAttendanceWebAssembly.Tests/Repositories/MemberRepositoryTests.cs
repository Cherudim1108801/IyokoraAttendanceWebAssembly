using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Repositories;

public class MemberRepositoryTests
{
    private static FirestoreDocument CreateMemberDoc(string id, string name, PartType part, Role role, string loginId, string groupId = "default") => new()
    {
        Id = id,
        Fields = new Dictionary<string, object?>
        {
            ["groupId"] = groupId,
            ["name"] = name,
            ["part"] = part.ToString(),
            ["role"] = role.ToString(),
            ["loginId"] = loginId,
            ["pieceParts"] = new List<object?>(),
            ["updatedAt"] = DateTime.UtcNow
        }
    };

    private static (MemberRepository repository, Mock<IFirestoreClient> client) CreateRepository(IEnumerable<FirestoreDocument>? docs = null)
    {
        var list = (docs ?? []).ToList();
        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.ListDocumentsAsync("members", It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        clientMock
            .Setup(c => c.QueryDocumentsAsync("members", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IReadOnlyDictionary<string, object?> filters, CancellationToken _) =>
                list.Where(d => filters.All(f => d.Fields.TryGetValue(f.Key, out var v) && Equals(v, f.Value))).ToList());
        clientMock
            .Setup(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new MemberRepository(clientMock.Object), clientMock);
    }

    [Fact]
    public async Task 他団体のメンバーは一覧から除外される()
    {
        var docs = new[]
        {
            CreateMemberDoc("m1", "自団体", PartType.Soprano, Role.GeneralMember, "IK0001", groupId: FirebaseOptions.GroupId),
            CreateMemberDoc("m2", "他団体", PartType.Soprano, Role.GeneralMember, "IK0002", groupId: "other")
        };
        var (repository, _) = CreateRepository(docs);

        var members = await repository.GetAllAsync();

        Assert.Equal(["自団体"], members.Select(m => m.Name));
    }

    [Fact]
    public async Task ログインID検索はメンバー全件を取得せずgroupIdとログインIDで絞り込む()
    {
        var docs = new[] { CreateMemberDoc("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234") };
        var (repository, client) = CreateRepository(docs);

        var found = await repository.FindByLoginIdAsync("IK1234");

        Assert.NotNull(found);
        Assert.Equal("対象", found!.Name);
        client.Verify(c => c.QueryDocumentsAsync(
            "members",
            It.Is<IReadOnlyDictionary<string, object?>>(f => (string)f["groupId"]! == FirebaseOptions.GroupId && (string)f["loginId"]! == "IK1234"),
            It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.ListDocumentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task 一致するログインIDが無い場合はnullが返る()
    {
        var docs = new[] { CreateMemberDoc("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234") };
        var (repository, _) = CreateRepository(docs);

        var found = await repository.FindByLoginIdAsync("IK9999");

        Assert.Null(found);
    }

    [Fact]
    public async Task メンバー登録時は団体IDと入力内容がフィールドに保存される()
    {
        var (repository, client) = CreateRepository();
        var pieceParts = new List<MemberPiecePart> { new() { PieceId = "piece1", SubPart = "Alto1" } };

        await repository.UpsertAsync("member1", "暗号化済み氏名", PartType.Alto, Role.Admin, "IK1234", pieceParts, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        client.Verify(c => c.UpsertDocumentAsync(
            "members",
            "member1",
            It.Is<Dictionary<string, object?>>(f =>
                (string)f["groupId"]! == FirebaseOptions.GroupId &&
                (string)f["name"]! == "暗号化済み氏名" &&
                (string)f["part"]! == PartType.Alto.ToString() &&
                (string)f["role"]! == Role.Admin.ToString() &&
                (string)f["loginId"]! == "IK1234"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ログインIDのみの更新では他のフィールドは送信されない()
    {
        var (repository, client) = CreateRepository();

        await repository.UpdateLoginIdAsync("member1", "IK9999", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        client.Verify(c => c.UpsertDocumentAsync(
            "members",
            "member1",
            It.Is<Dictionary<string, object?>>(f => f.Count == 2 && (string)f["loginId"]! == "IK9999"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 氏名のみの更新では他のフィールドは送信されない()
    {
        var (repository, client) = CreateRepository();

        await repository.UpdateNameAsync("member1", "再暗号化済み氏名");

        client.Verify(c => c.UpsertDocumentAsync(
            "members",
            "member1",
            It.Is<Dictionary<string, object?>>(f => f.Count == 1 && (string)f["name"]! == "再暗号化済み氏名"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
