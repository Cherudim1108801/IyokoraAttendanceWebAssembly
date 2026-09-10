using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class PartTypeExtensionsTests
{
    [Theory]
    [InlineData(PartType.Soprano, "ソプラノ")]
    [InlineData(PartType.Alto, "アルト")]
    [InlineData(PartType.Tenor, "テナー")]
    [InlineData(PartType.Bass, "ベース")]
    public void 表示名がパートごとに正しく変換される(PartType part, string expected)
    {
        Assert.Equal(expected, part.ToDisplayName());
    }

    [Theory]
    [InlineData(PartType.Soprano, "#FF6B9D")]
    [InlineData(PartType.Alto, "#7B7FF6")]
    [InlineData(PartType.Tenor, "#3EC1A4")]
    [InlineData(PartType.Bass, "#4A4A6A")]
    public void パート識別色がパートごとに正しく変換される(PartType part, string expected)
    {
        Assert.Equal(expected, part.ToColorHex());
    }

    [Theory]
    [InlineData(PartType.Soprano, "#FFCCFF")]
    [InlineData(PartType.Alto, "#FFFFCC")]
    [InlineData(PartType.Tenor, "#CDFFCC")]
    [InlineData(PartType.Bass, "#CAFFFF")]
    public void カード背景色がパートごとに正しく変換される(PartType part, string expected)
    {
        Assert.Equal(expected, part.ToCardBackgroundColorHex());
    }

    [Fact]
    public void 全パートがソプラノアルトテナーベースの順で列挙される()
    {
        Assert.Equal(
            [PartType.Soprano, PartType.Alto, PartType.Tenor, PartType.Bass],
            PartTypeExtensions.All);
    }
}
