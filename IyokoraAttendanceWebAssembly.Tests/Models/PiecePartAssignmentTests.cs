using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class PiecePartAssignmentTests
{
    [Fact]
    public void 分割なしの割り振りではパート名そのものが返る()
    {
        var assignment = new PiecePartAssignment { Part = PartType.Tenor, Division = PartDivision.None };

        Assert.Equal(["テナー"], assignment.SubPartLabels);
    }

    [Fact]
    public void 上下分割の割り振りでは上下を付けた2件のパート名が返る()
    {
        var assignment = new PiecePartAssignment { Part = PartType.Bass, Division = PartDivision.UpperLower };

        Assert.Equal(["ベース上", "ベース下"], assignment.SubPartLabels);
    }
}
