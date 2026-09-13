using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class ProfileTests : BunitContext
{
    private (FakeFirestoreClient client, LocalProfileStore profile) RegisterServices(bool confirmSwitch = true)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(FakeMemberService.Create(client));
        Services.AddSingleton(new PieceService(client));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        profile.MemberId = "me";
        profile.Name = "現在の名前";
        profile.Part = PartType.Soprano;
        profile.Role = Role.GeneralMember;
        profile.LoginId = "IK1234";
        Services.AddSingleton(profile);

        var jsMock = new Mock<IJSRuntime>();
        jsMock
            .Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<object?[]>()))
            .ReturnsAsync(confirmSwitch);
        Services.AddSingleton(jsMock.Object);

        return (client, profile);
    }

    [Fact]
    public void 現在のログインIDと名前が表示される()
    {
        RegisterServices();

        var cut = Render<Profile>();

        Assert.Contains("IK1234", cut.Markup);
        Assert.Equal("現在の名前", cut.Find("input[type=text]").GetAttribute("value"));
    }

    [Fact]
    public void 名前を空にして保存するとエラーが表示され保存されない()
    {
        var (_, profile) = RegisterServices();
        var cut = Render<Profile>();

        cut.Find("input[type=text]").Input("   ");
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Contains("名前を入力してください", cut.Find("p.error-text").TextContent);
        Assert.Equal("現在の名前", profile.Name);
    }

    [Fact]
    public void 名前を変更して保存すると端末のプロフィールが更新される()
    {
        var (_, profile) = RegisterServices();
        var cut = Render<Profile>();

        cut.Find("input[type=text]").Input("新しい名前");
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Contains("保存しました", cut.Find("p.success-text").TextContent);
        Assert.Equal("新しい名前", profile.Name);
    }

    [Fact]
    public void 内部分割のある曲は選択中パートの分割方式のみ表示される()
    {
        var (client, _) = RegisterServices();
        client.Seed("pieces", "p1", Seed.Piece("分割曲", partAssignments:
        [
            new() { Part = PartType.Soprano, Division = PartDivision.UpperLower }
        ]));
        client.Seed("pieces", "p2", Seed.Piece("分割なし曲", partAssignments:
        [
            new() { Part = PartType.Soprano, Division = PartDivision.None }
        ]));

        var cut = Render<Profile>();

        Assert.Contains("分割曲", cut.Markup);
        Assert.DoesNotContain("分割なし曲", cut.Markup);
    }

    [Fact]
    public void 旧プレフィックスのログインIDの場合は更新ボタンが表示される()
    {
        RegisterServices();

        var cut = Render<Profile>();

        Assert.Contains("ログインIDを更新する", cut.Markup);
    }

    [Fact]
    public void 現在のプレフィックスのログインIDの場合は更新ボタンが表示されない()
    {
        var (_, profile) = RegisterServices();
        profile.LoginId = FirebaseOptions.LoginIdPrefix + "1234";

        var cut = Render<Profile>();

        Assert.DoesNotContain("ログインIDを更新する", cut.Markup);
    }

    [Fact]
    public void ログインID更新ボタンで確認ダイアログを承認すると新しいIDが発行され端末にも保存される()
    {
        var (_, profile) = RegisterServices(confirmSwitch: true);
        var cut = Render<Profile>();

        cut.Find("button.iyk-btn:not(.iyk-btn-primary):not(.iyk-btn-outline-danger)").Click();

        Assert.StartsWith(FirebaseOptions.LoginIdPrefix, profile.LoginId, StringComparison.Ordinal);
        Assert.Contains("新しいログインID", cut.Markup);
        Assert.Contains(profile.LoginId, cut.Markup);
    }

    [Fact]
    public void ログインID更新ボタンで確認ダイアログを拒否すると何も変わらない()
    {
        var (_, profile) = RegisterServices(confirmSwitch: false);
        var cut = Render<Profile>();

        cut.Find("button.iyk-btn:not(.iyk-btn-primary):not(.iyk-btn-outline-danger)").Click();

        Assert.Equal("IK1234", profile.LoginId);
        Assert.DoesNotContain("新しいログインID", cut.Markup);
    }

    [Fact]
    public void 別のプロフィールを使うで確認ダイアログを承認すると端末情報がクリアされオンボーディングへ遷移する()
    {
        var (_, profile) = RegisterServices(confirmSwitch: true);
        var cut = Render<Profile>();

        cut.Find("button.iyk-btn-outline-danger").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("onboarding", nav.Uri);
        Assert.Equal(string.Empty, profile.Name);
    }

    [Fact]
    public void 別のプロフィールを使うで確認ダイアログを拒否すると何も変わらない()
    {
        var (_, profile) = RegisterServices(confirmSwitch: false);
        var cut = Render<Profile>();

        cut.Find("button.iyk-btn-outline-danger").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.DoesNotContain("onboarding", nav.Uri);
        Assert.Equal("現在の名前", profile.Name);
    }
}
