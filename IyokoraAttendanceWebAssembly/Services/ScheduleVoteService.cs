using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary><c>scheduleVotes</c> に対する日程投票の参加意思の取得・更新を担う。</summary>
public class ScheduleVoteService(IScheduleVoteRepository repository)
{
    /// <summary>指定候補日に対する投票を取得する。</summary>
    /// <param name="candidateId">候補日ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task<List<ScheduleVote>> GetForCandidateAsync(string candidateId, CancellationToken ct = default) =>
        repository.GetForCandidateAsync(candidateId, ct);

    /// <summary>指定した複数の候補日に対する投票をまとめて取得する。</summary>
    /// <param name="candidateIds">候補日IDの一覧。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<ScheduleVote>> GetForCandidatesAsync(IEnumerable<string> candidateIds, CancellationToken ct = default)
    {
        var results = await Task.WhenAll(candidateIds.Select(id => repository.GetForCandidateAsync(id, ct)));
        return results.SelectMany(votes => votes).ToList();
    }

    /// <summary>指定候補日への参加意思を登録・変更する。</summary>
    /// <param name="candidateId">候補日ID。</param>
    /// <param name="memberId">投票するメンバーのID。</param>
    /// <param name="memberName">投票するメンバーの表示名。</param>
    /// <param name="status">参加意思。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task SetStatusAsync(string candidateId, string memberId, string memberName, AttendanceStatus status, CancellationToken ct = default) =>
        repository.SetStatusAsync(candidateId, memberId, memberName, status, DateTime.UtcNow, ct);
}
