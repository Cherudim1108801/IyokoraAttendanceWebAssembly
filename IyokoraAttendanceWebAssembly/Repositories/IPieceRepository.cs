using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>pieces</c> コレクションに対するデータアクセスの抽象。</summary>
public interface IPieceRepository
{
    /// <summary>自団体（<see cref="Services.FirebaseOptions.GroupId"/>）に登録されている全ての曲を取得する（非表示曲を含む）。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    Task<List<Piece>> GetAllAsync(CancellationToken ct = default);

    /// <summary>曲の非表示状態を設定する。</summary>
    /// <param name="pieceId">曲ID。</param>
    /// <param name="isArchived">非表示にするかどうか。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task SetArchivedAsync(string pieceId, bool isArchived, CancellationToken ct = default);

    /// <summary>新しい曲を登録する。</summary>
    /// <param name="pieceId">発行済みの曲ID。</param>
    /// <param name="title">曲名。</param>
    /// <param name="partAssignments">使用するパートの割り振り。</param>
    /// <param name="createdAt">登録日時（UTC）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task CreateAsync(string pieceId, string title, IReadOnlyList<PiecePartAssignment> partAssignments, DateTime createdAt, CancellationToken ct = default);

    /// <summary>指定IDの曲を削除する。</summary>
    /// <param name="pieceId">曲ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task DeleteAsync(string pieceId, CancellationToken ct = default);
}
