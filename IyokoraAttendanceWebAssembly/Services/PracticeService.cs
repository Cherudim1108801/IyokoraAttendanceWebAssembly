using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary><c>practices</c> に対する練習予定の取得・作成・削除を担う。</summary>
public class PracticeService(IPracticeRepository repository)
{
    /// <summary>登録されている全練習予定を日付昇順で取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public Task<List<Practice>> GetAllAsync(CancellationToken ct = default) => repository.GetAllAsync(ct);

    /// <summary>指定IDの練習予定を1件取得する。存在しない場合は null。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task<Practice?> GetByIdAsync(string practiceId, CancellationToken ct = default) => repository.GetByIdAsync(practiceId, ct);

    /// <summary>今日以降の練習予定を、日付の古い順に取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<Practice>> GetUpcomingAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        var today = DateTime.Today;
        return all.Where(p => p.Date.Date >= today).OrderBy(p => p.Date).ToList();
    }

    /// <summary>今日より前の練習予定（過去の練習データ）を、日付の新しい順に取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<Practice>> GetPastAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        var today = DateTime.Today;
        return all.Where(p => p.Date.Date < today).OrderByDescending(p => p.Date).ToList();
    }

    /// <summary>今日以降で最も近い練習予定を取得する。存在しない場合は null。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<Practice?> GetNextUpcomingAsync(CancellationToken ct = default)
    {
        var upcoming = await GetUpcomingAsync(ct);
        return upcoming.FirstOrDefault();
    }

    /// <summary>新しい練習予定を登録する。</summary>
    /// <param name="date">練習日（時刻情報は無視される）。</param>
    /// <param name="title">タイトル（任意）。</param>
    /// <param name="place">場所（任意）。</param>
    /// <param name="startTime">練習開始時刻（"HH:mm" 形式、任意）。</param>
    /// <param name="endTime">練習終了時刻（"HH:mm" 形式、任意）。</param>
    /// <param name="timelineItems">タイムスケジュール（10分単位の詳細な予定）。</param>
    /// <param name="pieces">演奏予定曲。</param>
    /// <param name="requiresKeyPickup">鍵の受け取りが必要かどうか。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>発行された練習予定ID。</returns>
    public async Task<string> CreateAsync(DateTime date, string title, string place, string startTime, string endTime, IReadOnlyList<PracticeTimelineItem> timelineItems, IReadOnlyList<PracticePieceRef> pieces, bool requiresKeyPickup, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        await repository.CreateAsync(id, date, title, place, startTime, endTime, timelineItems, pieces, requiresKeyPickup, DateTime.UtcNow, ct);
        return id;
    }

    /// <summary>練習予定の開始・終了時刻とタイムスケジュールを更新する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="startTime">練習開始時刻（"HH:mm" 形式、任意）。</param>
    /// <param name="endTime">練習終了時刻（"HH:mm" 形式、任意）。</param>
    /// <param name="timelineItems">タイムスケジュール（10分単位の詳細な予定）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task UpdateScheduleAsync(string practiceId, string startTime, string endTime, IReadOnlyList<PracticeTimelineItem> timelineItems, CancellationToken ct = default) =>
        repository.UpdateScheduleAsync(practiceId, startTime, endTime, timelineItems, ct);

    /// <summary>練習予定の演奏予定曲を更新する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="pieces">演奏予定曲（録音リンク・強調表示の設定を含む）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task UpdatePiecesAsync(string practiceId, IReadOnlyList<PracticePieceRef> pieces, CancellationToken ct = default) =>
        repository.UpdatePiecesAsync(practiceId, pieces, ct);

    /// <summary>指定IDの練習予定を削除する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task DeleteAsync(string practiceId, CancellationToken ct = default) =>
        repository.DeleteAsync(practiceId, ct);

    /// <summary>指定の練習における、指定の曲の録音音源リンクを設定・変更・削除する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="pieceId">対象の曲ID。</param>
    /// <param name="recordingUrl">録音音源へのリンク（OneDriveなど）。削除する場合は null。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task SetPieceRecordingUrlAsync(string practiceId, string pieceId, string? recordingUrl, CancellationToken ct = default)
    {
        var practice = await GetByIdAsync(practiceId, ct);
        if (practice is null)
            return;

        var updatedPieces = practice.Pieces
            .Select(p => p.PieceId == pieceId
                ? new PracticePieceRef { PieceId = p.PieceId, Title = p.Title, RecordingUrl = recordingUrl, IsFeatured = p.IsFeatured }
                : p)
            .ToList();

        await repository.UpdatePiecesAsync(practiceId, updatedPieces, ct);
    }

    /// <summary>指定の練習における、指定の曲の録音を「音源」タブで強調表示（ピン留め）するかどうかを設定する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="pieceId">対象の曲ID。</param>
    /// <param name="isFeatured">強調表示するかどうか。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task SetPieceRecordingFeaturedAsync(string practiceId, string pieceId, bool isFeatured, CancellationToken ct = default)
    {
        var practice = await GetByIdAsync(practiceId, ct);
        if (practice is null)
            return;

        var updatedPieces = practice.Pieces
            .Select(p => p.PieceId == pieceId
                ? new PracticePieceRef { PieceId = p.PieceId, Title = p.Title, RecordingUrl = p.RecordingUrl, IsFeatured = isFeatured }
                : p)
            .ToList();

        await repository.UpdatePiecesAsync(practiceId, updatedPieces, ct);
    }

    /// <summary>指定の練習の鍵の受け取り状況を設定する。受け取り済みにする場合は、受け取ったメンバーの名前を記録する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="keyPickedUp">受け取り済みかどうか。</param>
    /// <param name="memberName">受け取ったメンバーの表示名。<paramref name="keyPickedUp"/> が true の場合に記録する。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task SetKeyPickedUpAsync(string practiceId, bool keyPickedUp, string memberName, CancellationToken ct = default) =>
        repository.SetKeyPickedUpAsync(practiceId, keyPickedUp, KeyPickupRecorder.ResolveRecordedName(keyPickedUp, memberName), ct);
}
