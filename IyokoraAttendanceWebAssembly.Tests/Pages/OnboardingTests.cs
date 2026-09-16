using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class OnboardingTests : BunitContext
{
    private LocalProfileStore RegisterServices(FakeFirestoreClient? client = null)
    {
        Services.AddSingleton(FakeMemberService.Create(client));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        Services.AddSingleton(profile);
        return profile;
    }

    [Fact]
    public void 名前が未入力の場合は保存されずエラーメッセージが表示される()
    {
        RegisterServices();
        var cut = Render<Onboarding>();

        cut.Find("button.iyk-btn-primary").Click();

        Assert.Contains("名前を入力してください", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 名前とパートを入力して登録するとログインIDが発行され完了画面が表示される()
    {
        var profile = RegisterServices();
        var cut = Render<Onboarding>();

        cut.Find("input").Input("山田 太郎");
        cut.Find("select").Change(PartType.Alto.ToString());
        cut.Find("button.iyk-btn-primary").Click();

        Assert.True(LoginIdGenerator.IsValidFormat(profile.LoginId));
        Assert.Equal("山田 太郎", profile.Name);
        Assert.Equal(PartType.Alto, profile.Part);
        Assert.Contains("登録が完了しました", cut.Find("h1").TextContent);
        Assert.Contains(profile.LoginId, cut.Find("div.highlight-card").TextContent);
    }

    [Fact]
    public void 前後の空白のみの名前は未入力として扱われる()
    {
        RegisterServices();
        var cut = Render<Onboarding>();

        cut.Find("input").Input("   ");
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Contains("名前を入力してください", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 保存に失敗した場合はエラーメッセージが表示される()
    {
        var client = new FakeFirestoreClient();
        client.FailNextCall("Upsert", "members");
        RegisterServices(client);
        var cut = Render<Onboarding>();

        cut.Find("input").Input("山田 太郎");
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Contains("保存に失敗しました", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 登録済みの端末では既存の名前とパートが初期表示される()
    {
        var profile = RegisterServices();
        profile.MemberId = "me";
        profile.Name = "既存 太郎";
        profile.Part = PartType.Alto;

        var cut = Render<Onboarding>();

        Assert.Equal("既存 太郎", cut.Find("input").GetAttribute("value"));
        cut.Find("input").Input("既存 太郎"); // 空白トリム後でも既存の名前のまま登録できることの確認
        cut.Find("button.iyk-btn-primary").Click();
        Assert.Equal(PartType.Alto, profile.Part);
    }
}
