using Bunit;
using IyokoraAttendanceWebAssembly.Layout;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Layout;

public class MainLayoutTests : BunitContext
{
    private FakeBootManifestFetcher RegisterProfile(bool isRegistered)
    {
        var initial = isRegistered
            ? new Dictionary<string, string> { ["profile.memberId"] = "member1", ["profile.name"] = "山田 太郎" }
            : null;

        Services.AddSingleton(new LocalProfileStore(FakeLocalStorage.Create(initial)));

        var fetcher = new FakeBootManifestFetcher();
        Services.AddSingleton<IBootManifestFetcher>(fetcher);
        Services.AddSingleton(new AppUpdateWatcher(fetcher));
        return fetcher;
    }

    private static RenderFragment EmptyBody => builder => builder.AddContent(0, "本文");

    [Fact]
    public void 未登録端末でトップページにアクセスするとオンボーディングへリダイレクトされる()
    {
        RegisterProfile(isRegistered: false);

        var cut = Render<MainLayout>(p => p.Add(x => x.Body, EmptyBody));

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("onboarding", nav.Uri);
        Assert.Empty(cut.FindAll("button.hamburger-button"));
    }

    [Fact]
    public void 登録済み端末でトップページにアクセスするとハンバーガーメニューが表示される()
    {
        RegisterProfile(isRegistered: true);

        var cut = Render<MainLayout>(p => p.Add(x => x.Body, EmptyBody));

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

        var cut = Render<MainLayout>(p => p.Add(x => x.Body, EmptyBody));

        Assert.EndsWith("onboarding", nav.Uri);
        Assert.Empty(cut.FindAll("button.hamburger-button"));
        Assert.DoesNotContain("app-main-with-menu", cut.Find("main").GetAttribute("class"));
    }

    [Fact]
    public void タブを開いている間に新しいバージョンがデプロイされると更新案内が表示される()
    {
        var fetcher = RegisterProfile(isRegistered: true);
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, EmptyBody));
        Assert.Empty(cut.FindAll(".update-banner"));

        // デプロイにより _framework/blazor.boot.json の内容が変わったことを模倣する。
        fetcher.Manifest = "v2";
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("history");

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".update-banner")));
    }

    [Fact]
    public void 更新案内の今すぐ更新を押すと現在のURLへ強制リロードする()
    {
        var fetcher = RegisterProfile(isRegistered: true);
        var cut = Render<MainLayout>(p => p.Add(x => x.Body, EmptyBody));
        var nav = Services.GetRequiredService<NavigationManager>();

        fetcher.Manifest = "v2";
        nav.NavigateTo("history");
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".update-banner")));

        cut.Find(".update-banner button").Click();

        Assert.EndsWith("history", nav.Uri);
    }
}
