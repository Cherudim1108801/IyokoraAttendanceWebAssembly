using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;
using Microsoft.JSInterop;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class MemberServiceTests
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

    private static (MemberService service, Mock<IFirestoreClient> client) CreateService(IEnumerable<FirestoreDocument>? docs = null)
    {
        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.ListDocumentsAsync("members", It.IsAny<CancellationToken>()))
            .ReturnsAsync((docs ?? []).ToList());
        clientMock
            .Setup(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // NameCipher は JS Interop (SubtleCrypto) 経由で暗号化・復号するため、
        // IJSRuntime をモックしてそのまま値を通す（暗号化・復号のロジック自体は wwwroot/js のJS実装側にあり対象外）。
        var jsRuntimeMock = new Mock<IJSRuntime>();
        jsRuntimeMock
            .Setup(js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string _, object?[] args) => ValueTask.FromResult((string)args[1]!));

        var nameCipher = new NameCipher(jsRuntimeMock.Object);
        return (new MemberService(clientMock.Object, nameCipher), clientMock);
    }

    [Fact]
    public async Task メンバー一覧がパートの順で並び替えられる()
    {
        var docs = new[]
        {
            CreateMemberDoc("m1", "アルトの人", PartType.Alto, Role.GeneralMember, "IK0001"),
            CreateMemberDoc("m2", "ソプラノの人", PartType.Soprano, Role.GeneralMember, "IK0002")
        };
        var (service, _) = CreateService(docs);

        var members = await service.GetAllAsync();

        Assert.Equal(["ソプラノの人", "アルトの人"], members.Select(m => m.Name));
    }

    [Fact]
    public async Task 同じパート内では名前順で並び替えられる()
    {
        var docs = new[]
        {
            CreateMemberDoc("m1", "Bob", PartType.Soprano, Role.GeneralMember, "IK0001"),
            CreateMemberDoc("m2", "Alice", PartType.Soprano, Role.GeneralMember, "IK0002")
        };
        var (service, _) = CreateService(docs);

        var members = await service.GetAllAsync();

        Assert.Equal(["Alice", "Bob"], members.Select(m => m.Name));
    }

    [Fact]
    public async Task 他団体のメンバーは一覧から除外される()
    {
        var docs = new[]
        {
            CreateMemberDoc("m1", "自団体", PartType.Soprano, Role.GeneralMember, "IK0001", groupId: FirebaseOptions.GroupId),
            CreateMemberDoc("m2", "他団体", PartType.Soprano, Role.GeneralMember, "IK0002", groupId: "other")
        };
        var (service, _) = CreateService(docs);

        var members = await service.GetAllAsync();

        Assert.Equal(["自団体"], members.Select(m => m.Name));
    }

    [Fact]
    public async Task 既存のログインIDを指定した場合は一意性チェックを行わずそのまま維持される()
    {
        var (service, client) = CreateService();

        var savedLoginId = await service.SaveAsync("member1", "山田 太郎", PartType.Soprano, Role.GeneralMember, [], existingLoginId: "IK1234");

        Assert.Equal("IK1234", savedLoginId);
        client.Verify(c => c.ListDocumentsAsync("members", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ログインID未指定の場合は既存メンバーを調べたうえで新しいIDが発行される()
    {
        var docs = new[] { CreateMemberDoc("m1", "既存", PartType.Soprano, Role.GeneralMember, "IK0001") };
        var (service, client) = CreateService(docs);

        var savedLoginId = await service.SaveAsync("member2", "新規 太郎", PartType.Soprano, Role.GeneralMember, [], existingLoginId: null);

        Assert.True(LoginIdGenerator.IsValidFormat(savedLoginId));
        client.Verify(c => c.ListDocumentsAsync("members", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ログインIDで検索すると大文字小文字を無視して一致するメンバーが見つかる()
    {
        var docs = new[] { CreateMemberDoc("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234") };
        var (service, _) = CreateService(docs);

        var found = await service.FindByLoginIdAsync("ik1234");

        Assert.NotNull(found);
        Assert.Equal("対象", found!.Name);
    }

    [Fact]
    public async Task 一致するログインIDが無い場合はnullが返る()
    {
        var docs = new[] { CreateMemberDoc("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234") };
        var (service, _) = CreateService(docs);

        var found = await service.FindByLoginIdAsync("IK9999");

        Assert.Null(found);
    }
}
