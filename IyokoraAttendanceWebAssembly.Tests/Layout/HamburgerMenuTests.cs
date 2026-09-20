using Bunit;
using IyokoraAttendanceWebAssembly.Layout;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Layout;

public class HamburgerMenuTests : BunitContext
{
    private void RegisterProfile(Role role = Role.GeneralMember) =>
        Services.AddSingleton(new LocalProfileStore(FakeLocalStorage.Create()) { Role = role });

    [Fact]
    public void 初期状態ではメニューは閉じている()
    {
        RegisterProfile();
        var cut = Render<HamburgerMenu>();

        Assert.Empty(cut.FindAll("nav.hamburger-menu"));
    }

    [Fact]
    public void ハンバーガーボタンをタップするとメニューが開く()
    {
        RegisterProfile();
        var cut = Render<HamburgerMenu>();

        cut.Find("button.hamburger-button").Click();

        Assert.Single(cut.FindAll("nav.hamburger-menu"));
    }

    [Fact]
    public void 背景をタップするとメニューが閉じる()
    {
        RegisterProfile();
        var cut = Render<HamburgerMenu>();
        cut.Find("button.hamburger-button").Click();

        cut.Find("div.hamburger-backdrop").Click();

        Assert.Empty(cut.FindAll("nav.hamburger-menu"));
    }

    [Fact]
    public void メニュー項目をタップするとメニューが閉じる()
    {
        RegisterProfile();
        var cut = Render<HamburgerMenu>();
        cut.Find("button.hamburger-button").Click();

        cut.Find("a.hamburger-menu-item").Click();

        Assert.Empty(cut.FindAll("nav.hamburger-menu"));
    }

    [Fact]
    public void 一般団員にはホーム以外の全ての遷移先が表示される()
    {
        RegisterProfile(Role.GeneralMember);
        var cut = Render<HamburgerMenu>();
        cut.Find("button.hamburger-button").Click();

        var hrefs = cut.FindAll("a.hamburger-menu-item").Select(a => a.GetAttribute("href")).ToList();

        Assert.Equal(["", "schedule", "schedule-poll", "history", "recordings", "pieces", "profile"], hrefs);
    }

    [Fact]
    public void 管理者には団員管理への遷移先も表示される()
    {
        RegisterProfile(Role.Admin);
        var cut = Render<HamburgerMenu>();
        cut.Find("button.hamburger-button").Click();

        var hrefs = cut.FindAll("a.hamburger-menu-item").Select(a => a.GetAttribute("href")).ToList();

        Assert.Equal(["", "schedule", "schedule-poll", "history", "recordings", "pieces", "profile", "members"], hrefs);
    }
}
