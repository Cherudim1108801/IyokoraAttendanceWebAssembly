using Bunit;
using IyokoraAttendanceWebAssembly.Layout;

namespace IyokoraAttendanceWebAssembly.Tests.Layout;

public class HamburgerMenuTests : TestContext
{
    [Fact]
    public void 初期状態ではメニューは閉じている()
    {
        var cut = RenderComponent<HamburgerMenu>();

        Assert.Empty(cut.FindAll("nav.hamburger-menu"));
    }

    [Fact]
    public void ハンバーガーボタンをタップするとメニューが開く()
    {
        var cut = RenderComponent<HamburgerMenu>();

        cut.Find("button.hamburger-button").Click();

        Assert.Single(cut.FindAll("nav.hamburger-menu"));
    }

    [Fact]
    public void 背景をタップするとメニューが閉じる()
    {
        var cut = RenderComponent<HamburgerMenu>();
        cut.Find("button.hamburger-button").Click();

        cut.Find("div.hamburger-backdrop").Click();

        Assert.Empty(cut.FindAll("nav.hamburger-menu"));
    }

    [Fact]
    public void メニュー項目をタップするとメニューが閉じる()
    {
        var cut = RenderComponent<HamburgerMenu>();
        cut.Find("button.hamburger-button").Click();

        cut.Find("a.hamburger-menu-item").Click();

        Assert.Empty(cut.FindAll("nav.hamburger-menu"));
    }

    [Fact]
    public void メニューにはホーム以外の全ての遷移先が表示される()
    {
        var cut = RenderComponent<HamburgerMenu>();
        cut.Find("button.hamburger-button").Click();

        var hrefs = cut.FindAll("a.hamburger-menu-item").Select(a => a.GetAttribute("href")).ToList();

        Assert.Equal(["", "schedule", "schedule-poll", "history", "recordings", "pieces", "profile"], hrefs);
    }
}
