using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Shared;

namespace IyokoraAttendanceWebAssembly.Tests.Shared;

public class AttendanceButtonsTests : TestContext
{
    [Fact]
    public void 現在の出欠状態に対応するボタンにselectedクラスが付く()
    {
        var cut = RenderComponent<AttendanceButtons>(p => p.Add(x => x.Status, AttendanceStatus.Attending));

        Assert.Contains("selected", cut.Find("button.attend-yes").GetAttribute("class"));
        Assert.DoesNotContain("selected", cut.Find("button.attend-no").GetAttribute("class"));
        Assert.DoesNotContain("selected", cut.Find("button.attend-undecided").GetAttribute("class"));
    }

    [Fact]
    public void HideUndecidedがtrueの場合は未定ボタンが表示されない()
    {
        var cut = RenderComponent<AttendanceButtons>(p => p.Add(x => x.HideUndecided, true));

        Assert.Empty(cut.FindAll("button.attend-undecided"));
    }

    [Fact]
    public void Disabledがtrueの場合は全ボタンが無効化される()
    {
        var cut = RenderComponent<AttendanceButtons>(p => p.Add(x => x.Disabled, true));

        Assert.True(cut.Find("button.attend-yes").HasAttribute("disabled"));
        Assert.True(cut.Find("button.attend-no").HasAttribute("disabled"));
        Assert.True(cut.Find("button.attend-undecided").HasAttribute("disabled"));
        Assert.Contains("disabled", cut.Find("div.attendance-buttons").GetAttribute("class"));
    }

    [Theory]
    [InlineData("button.attend-yes", AttendanceStatus.Attending)]
    [InlineData("button.attend-no", AttendanceStatus.NotAttending)]
    [InlineData("button.attend-undecided", AttendanceStatus.Undecided)]
    public void ボタンをタップすると対応する出欠状態がStatusChangedで通知される(string selector, AttendanceStatus expected)
    {
        AttendanceStatus? notified = null;
        var cut = RenderComponent<AttendanceButtons>(p => p
            .Add(x => x.Status, AttendanceStatus.Undecided)
            .Add(x => x.StatusChanged, status => notified = status));

        cut.Find(selector).Click();

        Assert.Equal(expected, notified);
    }
}
