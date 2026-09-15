using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>scheduleVotes</c> コレクションに対するデータアクセスの抽象。</summary>
public interface IScheduleVoteRepository
{
    /// <summary>自団体（<see cref="Services.FirebaseOptions.GroupId"/>）の全候補日分の投票を取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    Task<List<ScheduleVote>> GetAllAsync(CancellationToken ct = default);

    /// <summary>指定候補日への参加意思を登録・変更する。</summary>
    /// <param name="candidateId">候補日ID。</param>
    /// <param name="memberId">投票するメンバーのID。</param>
    /// <param name="memberName">投票するメンバーの表示名。</param>
    /// <param name="status">参加意思。</param>
    /// <param name="updatedAt">更新日時（UTC）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task SetStatusAsync(string candidateId, string memberId, string memberName, AttendanceStatus status, DateTime updatedAt, CancellationToken ct = default);
}
