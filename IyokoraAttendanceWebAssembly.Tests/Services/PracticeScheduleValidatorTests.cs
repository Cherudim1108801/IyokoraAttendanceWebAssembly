using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class PracticeScheduleValidatorTests
{
    [Fact]
    public void 開始終了時刻がどちらも未入力なら検証を通過する()
    {
        var isValid = PracticeScheduleValidator.TryValidateTimeRange(null, null, out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void 開始時刻のみ入力されている場合はエラーになる()
    {
        var isValid = PracticeScheduleValidator.TryValidateTimeRange(new TimeOnly(18, 0), null, out var error);

        Assert.False(isValid);
        Assert.Equal("開始時刻と終了時刻は両方入力してください。", error);
    }

    [Fact]
    public void 終了時刻のみ入力されている場合はエラーになる()
    {
        var isValid = PracticeScheduleValidator.TryValidateTimeRange(null, new TimeOnly(20, 0), out var error);

        Assert.False(isValid);
        Assert.Equal("開始時刻と終了時刻は両方入力してください。", error);
    }

    [Fact]
    public void 分が10分単位でない時刻はエラーになる()
    {
        var isValid = PracticeScheduleValidator.TryValidateTimeRange(new TimeOnly(18, 5), new TimeOnly(20, 0), out var error);

        Assert.False(isValid);
        Assert.Equal("時刻は10分単位で入力してください。", error);
    }

    [Fact]
    public void 終了時刻が開始時刻以前の場合はエラーになる()
    {
        var isValid = PracticeScheduleValidator.TryValidateTimeRange(new TimeOnly(20, 0), new TimeOnly(18, 0), out var error);

        Assert.False(isValid);
        Assert.Equal("終了時刻は開始時刻より後にしてください。", error);
    }

    [Fact]
    public void 終了時刻が開始時刻と同時刻の場合はエラーになる()
    {
        var isValid = PracticeScheduleValidator.TryValidateTimeRange(new TimeOnly(18, 0), new TimeOnly(18, 0), out var error);

        Assert.False(isValid);
        Assert.Equal("終了時刻は開始時刻より後にしてください。", error);
    }

    [Fact]
    public void 分が10分単位で開始より後の終了時刻なら検証を通過する()
    {
        var isValid = PracticeScheduleValidator.TryValidateTimeRange(new TimeOnly(18, 0), new TimeOnly(18, 10), out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void タイムスケジュール項目は開始終了時刻が両方揃っていないとエラーになる()
    {
        var item = new TimelineItemInput { StartTime = null, EndTime = null, Content = "基礎合奏" };

        var isValid = PracticeScheduleValidator.TryValidateTimelineItem(item, out var error);

        Assert.False(isValid);
        Assert.Equal("タイムスケジュールの開始・終了時刻を入力してください。", error);
    }

    [Fact]
    public void タイムスケジュール項目の時刻が妥当なら検証を通過する()
    {
        var item = new TimelineItemInput { StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(18, 10), Content = "基礎合奏" };

        var isValid = PracticeScheduleValidator.TryValidateTimelineItem(item, out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public void HHmm形式の文字列はTimeOnlyに変換される()
    {
        var time = PracticeScheduleValidator.ParseTimeOrNull("18:30");

        Assert.Equal(new TimeOnly(18, 30), time);
    }

    [Theory]
    [InlineData("")]
    [InlineData("不正な値")]
    public void 空文字列や不正な形式はnullに変換される(string value)
    {
        var time = PracticeScheduleValidator.ParseTimeOrNull(value);

        Assert.Null(time);
    }
}
