namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>メンバーの、特定の曲における内部パート（分割）担当。</summary>
public class MemberPiecePart
{
    public required string PieceId { get; init; }
    public required string SubPart { get; init; }
}
