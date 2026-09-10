using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class PartDivisionExtensionsTests
{
    [Theory]
    [InlineData(PartDivision.None, "分割しない")]
    [InlineData(PartDivision.UpperLower, "上・下に分割")]
    public void 表示名が分割方式ごとに正しく変換される(PartDivision division, string expected)
    {
        Assert.Equal(expected, division.ToDisplayName());
    }

    [Fact]
    public void 分割しない場合はパート名そのものが1件返る()
    {
        var labels = PartDivision.None.ToSubPartLabels(PartType.Soprano);

        Assert.Equal(["ソプラノ"], labels);
    }

    [Fact]
    public void 上下に分割する場合はパート名に上下を付けた2件が返る()
    {
        var labels = PartDivision.UpperLower.ToSubPartLabels(PartType.Alto);

        Assert.Equal(["アルト上", "アルト下"], labels);
    }

    [Fact]
    public void 選択可能な分割方式が分割しない上下分割の順で列挙される()
    {
        Assert.Equal(
            [PartDivision.None, PartDivision.UpperLower],
            PartDivisionExtensions.All);
    }
}
