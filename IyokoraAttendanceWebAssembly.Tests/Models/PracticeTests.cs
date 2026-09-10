using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class PracticeTests
{
    [Fact]
    public void 開始終了時刻が両方設定されていると波ダッシュ区切りの文字列になる()
    {
        var practice = new Practice { Id = "p1", StartTime = "18:00", EndTime = "20:00" };

        Assert.Equal("18:00〜20:00", practice.TimeRangeSummary);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("18:00", "")]
    [InlineData("", "20:00")]
    public void 開始終了時刻のいずれかが未設定の場合は空文字列になる(string start, string end)
    {
        var practice = new Practice { Id = "p1", StartTime = start, EndTime = end };

        Assert.Equal(string.Empty, practice.TimeRangeSummary);
    }
}
