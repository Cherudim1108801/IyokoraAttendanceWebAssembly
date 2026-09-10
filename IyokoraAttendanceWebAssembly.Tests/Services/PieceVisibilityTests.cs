using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class PieceVisibilityTests
{
    private static Piece CreatePiece(string id, bool isArchived) => new()
    {
        Id = id,
        Title = id,
        IsArchived = isArchived
    };

    [Fact]
    public void 非表示を含めない場合は非表示の曲が除外される()
    {
        var pieces = new[] { CreatePiece("A", isArchived: false), CreatePiece("B", isArchived: true) };

        var visible = PieceVisibility.Filter(pieces, includeArchived: false).ToList();

        Assert.Equal(["A"], visible.Select(p => p.Id));
    }

    [Fact]
    public void 非表示を含める場合は全ての曲がそのまま返る()
    {
        var pieces = new[] { CreatePiece("A", isArchived: false), CreatePiece("B", isArchived: true) };

        var visible = PieceVisibility.Filter(pieces, includeArchived: true).ToList();

        Assert.Equal(["A", "B"], visible.Select(p => p.Id));
    }
}
