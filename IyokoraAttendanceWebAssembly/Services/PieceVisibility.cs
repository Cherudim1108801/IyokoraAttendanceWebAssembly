using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>取り組みが終わり非表示（アーカイブ）にされた曲を、曲一覧の表示対象から絞り込む純粋ロジック。</summary>
public static class PieceVisibility
{
    /// <summary><paramref name="includeArchived"/> が false の場合、非表示にされた曲を除外する。</summary>
    public static IEnumerable<Piece> Filter(IEnumerable<Piece> pieces, bool includeArchived) =>
        includeArchived ? pieces : pieces.Where(p => !p.IsArchived);
}
