using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class PieceTests
{
    private static Piece CreatePiece(List<PiecePartAssignment>? assignments = null) => new()
    {
        Id = "p1",
        Title = "曲A",
        PartAssignments = assignments ?? []
    };

    [Fact]
    public void パート未割り当ての場合はパート未設定と表示される()
    {
        var piece = CreatePiece();

        Assert.Equal("パート未設定", piece.PartsSummary);
    }

    [Fact]
    public void 分割なしのパートは表示名のみで結合される()
    {
        var piece = CreatePiece([
            new() { Part = PartType.Soprano, Division = PartDivision.None },
            new() { Part = PartType.Alto, Division = PartDivision.None }
        ]);

        Assert.Equal("ソプラノ、アルト", piece.PartsSummary);
    }

    [Fact]
    public void 分割ありのパートは分割名を括弧付きで表示する()
    {
        var piece = CreatePiece([
            new() { Part = PartType.Tenor, Division = PartDivision.UpperLower }
        ]);

        Assert.Equal($"テナー（{PartDivision.UpperLower.ToDisplayName()}）", piece.PartsSummary);
    }
}
