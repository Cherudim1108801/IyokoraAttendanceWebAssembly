using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class OnboardingTests : BunitContext
{
    private LocalProfileStore RegisterServices()
    {
        Services.AddSingleton(FakeMemberService.Create());
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
}
