using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class MemberServiceTests
{
    private static Member CreateMember(string id, string name, PartType part, Role role, string loginId) => new()
    {
        Id = id,
        Name = name,
        LoginId = loginId,
        Part = part,
        Role = role,
        PieceParts = [],
        UpdatedAt = DateTime.UtcNow
    };

    private static (MemberService service, Mock<IMemberRepository> repository) CreateService(IEnumerable<Member>? members = null)
    {
        var list = (members ?? []).ToList();
        var repositoryMock = new Mock<IMemberRepository>();
        repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        repositoryMock
            .Setup(r => r.FindByLoginIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string loginId, CancellationToken _) => list.FirstOrDefault(m => m.LoginId == loginId));
        repositoryMock
            .Setup(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PartType>(), It.IsAny<Role>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<MemberPiecePart>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repositoryMock
            .Setup(r => r.UpdateLoginIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repositoryMock
            .Setup(r => r.UpdateNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
        return (new MemberService(repositoryMock.Object, nameCipher, NullLogger<MemberService>.Instance), repositoryMock);
    }

    [Fact]
    public async Task メンバー一覧がパートの順で並び替えられる()
    {
        var members = new[]
        {
            CreateMember("m1", "アルトの人", PartType.Alto, Role.GeneralMember, "IK0001"),
            CreateMember("m2", "ソプラノの人", PartType.Soprano, Role.GeneralMember, "IK0002")
        };
        var (service, _) = CreateService(members);

        var result = await service.GetAllAsync();

        Assert.Equal(["ソプラノの人", "アルトの人"], result.Select(m => m.Name));
    }

    [Fact]
    public async Task 同じパート内では名前順で並び替えられる()
    {
        var members = new[]
        {
            CreateMember("m1", "Bob", PartType.Soprano, Role.GeneralMember, "IK0001"),
            CreateMember("m2", "Alice", PartType.Soprano, Role.GeneralMember, "IK0002")
        };
        var (service, _) = CreateService(members);

        var result = await service.GetAllAsync();

        Assert.Equal(["Alice", "Bob"], result.Select(m => m.Name));
    }

    [Fact]
    public async Task 既存のログインIDを指定した場合は一意性チェックを行わずそのまま維持される()
    {
        var (service, repository) = CreateService();

        var savedLoginId = await service.SaveAsync("member1", "山田 太郎", PartType.Soprano, Role.GeneralMember, [], existingLoginId: "IK1234");

        Assert.Equal("IK1234", savedLoginId);
        repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ログインID未指定の場合は既存メンバーを調べたうえで新しいIDが発行される()
    {
        var members = new[] { CreateMember("m1", "既存", PartType.Soprano, Role.GeneralMember, "IK0001") };
        var (service, repository) = CreateService(members);

        var savedLoginId = await service.SaveAsync("member2", "新規 太郎", PartType.Soprano, Role.GeneralMember, [], existingLoginId: null);

        Assert.True(LoginIdGenerator.IsValidFormat(savedLoginId));
        repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ログインIDで検索すると大文字小文字を無視して一致するメンバーが見つかる()
    {
        var members = new[] { CreateMember("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234") };
        var (service, repository) = CreateService(members);

        var found = await service.FindByLoginIdAsync("ik1234");

        Assert.NotNull(found);
        Assert.Equal("対象", found!.Name);
        repository.Verify(r => r.FindByLoginIdAsync("IK1234", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 一致するログインIDが無い場合はnullが返る()
    {
        var members = new[] { CreateMember("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234") };
        var (service, _) = CreateService(members);

        var found = await service.FindByLoginIdAsync("IK9999");

        Assert.Null(found);
    }

    [Fact]
    public async Task ログインIDを再発行すると現在のプレフィックスの新しいIDが払い出され保存される()
    {
        var members = new[] { CreateMember("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234") };
        var (service, repository) = CreateService(members);

        var newLoginId = await service.ReissueLoginIdAsync("m1");

        Assert.True(LoginIdGenerator.IsValidFormat(newLoginId));
        Assert.StartsWith(FirebaseOptions.LoginIdPrefix, newLoginId, StringComparison.Ordinal);
        repository.Verify(r => r.UpdateLoginIdAsync("m1", newLoginId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ログインIDの再発行は既存メンバーのIDと重複しない()
    {
        var members = new[]
        {
            CreateMember("m1", "対象", PartType.Soprano, Role.GeneralMember, "IK1234"),
            CreateMember("m2", "既存", PartType.Soprano, Role.GeneralMember, FirebaseOptions.LoginIdPrefix + "0001")
        };
        var (service, _) = CreateService(members);

        var newLoginId = await service.ReissueLoginIdAsync("m1");

        Assert.NotEqual(FirebaseOptions.LoginIdPrefix + "0001", newLoginId);
    }

    [Fact]
    public async Task 旧CBC形式で復号された氏名は画面表示をブロックせずAES_GCM形式へ自動的に再暗号化される()
    {
        var members = new[] { CreateMember("m1", "レガシー太郎", PartType.Soprano, Role.GeneralMember, "IK0001") };

        var repositoryMock = new Mock<IMemberRepository>();
        repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(members.ToList());

        var upserted = new TaskCompletionSource<string>();
        repositoryMock
            .Setup(r => r.UpdateNameAsync("m1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string _, string storedName, CancellationToken _) =>
            {
                upserted.TrySetResult(storedName);
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

        var service = new MemberService(repositoryMock.Object, new NameCipher(jsRuntimeMock.Object), NullLogger<MemberService>.Instance);

        var result = await service.GetAllAsync();

        // 画面表示（復号結果）はバックグラウンド処理の完了を待たずに得られる。
        Assert.Equal("レガシー太郎", result[0].Name);

        var completed = await Task.WhenAny(upserted.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(upserted.Task, completed);

        var storedName = await upserted.Task;
        Assert.Equal("GCM再暗号化:レガシー太郎", storedName);
    }
}
