namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>合唱団のレパートリー曲1件（Firestore の <c>pieces</c> ドキュメントに対応）。</summary>
public class Piece
{
    /// <summary>曲ID（Firestore のドキュメントID）。</summary>
    public required string Id { get; set; }

    /// <summary>曲名。</summary>
    public required string Title { get; set; }

    /// <summary>この曲で使用するパートの割り振り。</summary>
    public List<PiecePartAssignment> PartAssignments { get; set; } = [];

    /// <summary>登録日時（UTC）。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>一覧表示用：割り振り済みパートを読点区切りで結合した文字列。</summary>
    public string PartsSummary => PartAssignments.Count == 0
        ? "パート未設定"
        : string.Join("、", PartAssignments.Select(a => a.Division == PartDivision.None
            ? a.Part.ToDisplayName()
            : $"{a.Part.ToDisplayName()}（{a.Division.ToDisplayName()}）"));
}
