using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class AttendanceStatusExtensionsTests
{
    [Theory]
    [InlineData(AttendanceStatus.Attending, "参加")]
    [InlineData(AttendanceStatus.NotAttending, "不参加")]
    [InlineData(AttendanceStatus.Undecided, "未定")]
    public void 表示名が出欠状態ごとに正しく変換される(AttendanceStatus status, string expected)
    {
        Assert.Equal(expected, status.ToDisplayName());
    }

    [Theory]
    [InlineData(AttendanceStatus.Attending, "#3EC1A4")]
    [InlineData(AttendanceStatus.NotAttending, "#E0607A")]
    [InlineData(AttendanceStatus.Undecided, "#B0B0B8")]
    public void 識別色が出欠状態ごとに正しく変換される(AttendanceStatus status, string expected)
    {
        Assert.Equal(expected, status.ToColorHex());
    }
}
