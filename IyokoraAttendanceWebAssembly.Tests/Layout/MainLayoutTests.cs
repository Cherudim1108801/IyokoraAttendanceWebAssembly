using Bunit;
using IyokoraAttendanceWebAssembly.Layout;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Layout;

public class MainLayoutTests : TestContext
{
    private void RegisterProfile(bool isRegistered)
    {
        var initial = isRegistered
            ? new Dictionary<string, string> { ["profile.memberId"] = "member1", ["profile.name"] = "山田 太郎" }
            : null;

        Services.AddSingleton(new LocalProfileStore(FakeLocalStorage.Create(initial)));
    }

    private static RenderFragment EmptyBody => builder => builder.AddContent(0, "本文");

    [Fact]
    public void 未登録端末でトップページにアクセスするとオンボーディングへリダイレクトされる()
    {
        RegisterProfile(isRegistered: false);

        var cut = RenderComponent<MainLayout>(p => p.Add(x => x.Body, EmptyBody));

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("onboarding", nav.Uri);
        Assert.Empty(cut.FindAll("button.hamburger-button"));
    }

    [Fact]
    public void 登録済み端末でトップページにアクセスするとハンバーガーメニューが表示される()
    {
        RegisterProfile(isRegistered: true);

        var cut = RenderComponent<MainLayout>(p => p.Add(x => x.Body, EmptyBody));

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.DoesNotContain("onboarding", nav.Uri);
        Assert.Single(cut.FindAll("button.hamburger-button"));
        Assert.Contains("app-main-with-menu", cut.Find("main").GetAttribute("class"));
    }

    [Fact]
    public void 未登録端末でもオンボーディング画面自体はリダイレクトされずメニューも表示されない()
    {
        RegisterProfile(isRegistered: false);
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("onboarding");

        var cut = RenderComponent<MainLayout>(p => p.Add(x => x.Body, EmptyBody));

        Assert.EndsWith("onboarding", nav.Uri);
        Assert.Empty(cut.FindAll("button.hamburger-button"));
        Assert.DoesNotContain("app-main-with-menu", cut.Find("main").GetAttribute("class"));
    }
}
