using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class TimeOfDayExtensionsTests
{
    [Theory]
    [InlineData(TimeOfDay.Morning, "午前")]
    [InlineData(TimeOfDay.Afternoon, "午後")]
    [InlineData(TimeOfDay.Evening, "夜間")]
    public void 表示名が時間帯区分ごとに正しく変換される(TimeOfDay timeOfDay, string expected)
    {
        Assert.Equal(expected, timeOfDay.ToDisplayName());
    }

    [Fact]
    public void 全時間帯区分が午前午後夜間の順で列挙される()
    {
        Assert.Equal([TimeOfDay.Morning, TimeOfDay.Afternoon, TimeOfDay.Evening], TimeOfDayExtensions.All);
    }
}
