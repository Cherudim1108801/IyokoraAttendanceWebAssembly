using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;
using Microsoft.Extensions.Logging.Abstractions;
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

        // NameCipher は JS Interop (SubtleCrypto) 経由で暗号化・復号するため、
        // IJSRuntime をモックしてそのまま値を通す（暗号化・復号のロジック自体は wwwroot/js のJS実装側にあり対象外）。
        // 復号は常に「旧形式ではない（WasLegacyFormat=false）」を返し、自動再暗号化は発生させない。
        var jsRuntimeMock = new Mock<IJSRuntime>();
        jsRuntimeMock
            .Setup(js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string _, object?[] args) => ValueTask.FromResult((string)args[1]!));
        jsRuntimeMock
            .Setup(js => js.InvokeAsync<NameDecryptResult>(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string _, object?[] args) => ValueTask.FromResult(new NameDecryptResult((string)args[1]!, WasLegacyFormat: false)));

        var nameCipher = new NameCipher(jsRuntimeMock.Object);
        return (new MemberService(clientMock.Object, nameCipher, NullLogger<MemberService>.Instance), clientMock);
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

    [Fact]
    public async Task ログインID検索はメンバー全件を取得せずgroupIdとログインIDで絞り込む()
    {
        var docs = new[] { CreateMemberDoc("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234") };
        var (service, client) = CreateService(docs);

        await service.FindByLoginIdAsync("ik1234");

        client.Verify(c => c.QueryDocumentsAsync(
            "members",
            It.Is<IReadOnlyDictionary<string, object?>>(f => (string)f["groupId"]! == FirebaseOptions.GroupId && (string)f["loginId"]! == "IK1234"),
            It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.ListDocumentsAsync("members", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ログインIDを再発行すると現在のプレフィックスの新しいIDが払い出され保存される()
    {
        var docs = new[] { CreateMemberDoc("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234") };
        var (service, client) = CreateService(docs);

        var newLoginId = await service.ReissueLoginIdAsync("m1");

        Assert.True(LoginIdGenerator.IsValidFormat(newLoginId));
        Assert.StartsWith(FirebaseOptions.LoginIdPrefix, newLoginId, StringComparison.Ordinal);
        client.Verify(c => c.UpsertDocumentAsync(
            "members",
            "m1",
            It.Is<Dictionary<string, object?>>(f => (string)f["loginId"]! == newLoginId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ログインIDの再発行は既存メンバーのIDと重複しない()
    {
        var docs = new[]
        {
            CreateMemberDoc("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234"),
            CreateMemberDoc("m2", "既存", PartType.Soprano, Role.GeneralMember, FirebaseOptions.LoginIdPrefix + "0001")
        };
        var (service, _) = CreateService(docs);

        var newLoginId = await service.ReissueLoginIdAsync("m1");

        Assert.NotEqual(FirebaseOptions.LoginIdPrefix + "0001", newLoginId);
    }

    [Fact]
    public async Task 旧CBC形式で復号された氏名は画面表示をブロックせずAES_GCM形式へ自動的に再暗号化される()
    {
        var docs = new[] { CreateMemberDoc("m1", "レガシー太郎", PartType.Soprano, Role.GeneralMember, "IK0001") };

        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.ListDocumentsAsync("members", It.IsAny<CancellationToken>()))
            .ReturnsAsync(docs.ToList());

        var upserted = new TaskCompletionSource<Dictionary<string, object?>>();
        clientMock
            .Setup(c => c.UpsertDocumentAsync("members", "m1", It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string _, Dictionary<string, object?> fields, CancellationToken _) =>
            {
                upserted.TrySetResult(fields);
                return Task.CompletedTask;
            });

        // 復号時に WasLegacyFormat=true を返し、旧CBC形式からの復号を再現する。
        var jsRuntimeMock = new Mock<IJSRuntime>();
        jsRuntimeMock
            .Setup(js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string _, object?[] args) => ValueTask.FromResult("GCM再暗号化:" + (string)args[1]!));
        jsRuntimeMock
            .Setup(js => js.InvokeAsync<NameDecryptResult>(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string _, object?[] args) => ValueTask.FromResult(new NameDecryptResult((string)args[1]!, WasLegacyFormat: true)));

        var service = new MemberService(clientMock.Object, new NameCipher(jsRuntimeMock.Object), NullLogger<MemberService>.Instance);

        var members = await service.GetAllAsync();

        // 画面表示（復号結果）はバックグラウンド処理の完了を待たずに得られる。
        Assert.Equal("レガシー太郎", members[0].Name);

        var completed = await Task.WhenAny(upserted.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(upserted.Task, completed);

        var savedFields = await upserted.Task;
        Assert.Equal("GCM再暗号化:レガシー太郎", savedFields["name"]);
    }
}
