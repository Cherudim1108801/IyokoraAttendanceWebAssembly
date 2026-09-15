using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary><c>scheduleCandidates</c> に対する日程投票の候補日の取得・作成・削除を担う。</summary>
public class ScheduleCandidateService(IScheduleCandidateRepository repository)
{
    /// <summary>今日以降の候補日を、日付の古い順に取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<ScheduleCandidate>> GetUpcomingAsync(CancellationToken ct = default)
    {
        var all = await repository.GetAllAsync(ct);
        var today = DateTime.Today;
        return all.Where(c => c.Date.Date >= today).OrderBy(c => c.Date).ToList();
    }

    /// <summary>新しい候補日を登録する。</summary>
    /// <param name="date">候補日（時刻情報は無視される）。</param>
    /// <param name="timeOfDay">候補日の時間帯区分（午前／午後／夜間）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>発行された候補日ID。</returns>
    public async Task<string> CreateAsync(DateTime date, TimeOfDay timeOfDay, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        await repository.CreateAsync(id, date, timeOfDay, DateTime.UtcNow, ct);
        return id;
    }

    /// <summary>指定IDの候補日を削除する。</summary>
    /// <param name="candidateId">候補日ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task DeleteAsync(string candidateId, CancellationToken ct = default) =>
        repository.DeleteAsync(candidateId, ct);
}
