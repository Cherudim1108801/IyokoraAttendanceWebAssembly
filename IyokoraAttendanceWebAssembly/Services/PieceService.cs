using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary><c>pieces</c> に対する練習曲（レパートリー）の取得・作成・削除を担う。</summary>
public class PieceService(IPieceRepository repository)
{
    /// <summary>登録されている曲を曲名順で取得する。</summary>
    /// <param name="includeArchived">取り組みが終わり非表示にされている曲も含めるかどうか。既定は含めない。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<Piece>> GetAllAsync(bool includeArchived = false, CancellationToken ct = default)
    {
        var pieces = await repository.GetAllAsync(ct);
        return PieceVisibility.Filter(pieces, includeArchived)
            .OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>曲の非表示状態を設定する。</summary>
    /// <param name="pieceId">曲ID。</param>
    /// <param name="isArchived">非表示にするかどうか。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task SetArchivedAsync(string pieceId, bool isArchived, CancellationToken ct = default) =>
        repository.SetArchivedAsync(pieceId, isArchived, ct);

    /// <summary>新しい曲を登録する。</summary>
    /// <param name="title">曲名。</param>
    /// <param name="partAssignments">使用するパートの割り振り。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>発行された曲ID。</returns>
    public async Task<string> CreateAsync(string title, IReadOnlyList<PiecePartAssignment> partAssignments, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        await repository.CreateAsync(id, title, partAssignments, DateTime.UtcNow, ct);
        return id;
    }

    /// <summary>指定IDの曲を削除する。</summary>
    /// <param name="pieceId">曲ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task DeleteAsync(string pieceId, CancellationToken ct = default) =>
        repository.DeleteAsync(pieceId, ct);
}
