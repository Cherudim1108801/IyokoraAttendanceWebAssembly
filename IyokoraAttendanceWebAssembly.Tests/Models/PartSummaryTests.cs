using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class PartSummaryTests
{
    [Fact]
    public void 参加人数と所属人数からラベルが人数スラッシュ人数人の形式で生成される()
    {
        var summary = new PartSummary
        {
            Part = PartType.Soprano,
            Label = "ソプラノ",
            ColorHex = "#FF6B9D",
            CardBackgroundColorHex = "#FFCCFF",
            AttendingCount = 3,
            MemberCount = 5,
            AttendeeNames = ["田中", "佐藤", "鈴木"]
        };

        Assert.Equal("3 / 5 人", summary.CountLabel);
    }
}
