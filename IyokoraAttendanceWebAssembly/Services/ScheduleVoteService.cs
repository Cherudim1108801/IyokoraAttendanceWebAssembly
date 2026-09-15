using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary><c>scheduleVotes</c> に対する日程投票の参加意思の取得・更新を担う。</summary>
public class ScheduleVoteService(IScheduleVoteRepository repository)
{
    /// <summary>登録されている全候補日分の投票を取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public Task<List<ScheduleVote>> GetAllAsync(CancellationToken ct = default) => repository.GetAllAsync(ct);

    /// <summary>指定候補日への参加意思を登録・変更する。</summary>
    /// <param name="candidateId">候補日ID。</param>
    /// <param name="memberId">投票するメンバーのID。</param>
    /// <param name="memberName">投票するメンバーの表示名。</param>
    /// <param name="status">参加意思。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task SetStatusAsync(string candidateId, string memberId, string memberName, AttendanceStatus status, CancellationToken ct = default) =>
        repository.SetStatusAsync(candidateId, memberId, memberName, status, DateTime.UtcNow, ct);
}
