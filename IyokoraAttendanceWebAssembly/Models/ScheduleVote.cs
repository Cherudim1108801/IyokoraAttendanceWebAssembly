namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>日程投票の候補日に対する、あるメンバー1人分の参加意思（Firestore の <c>scheduleVotes</c> ドキュメントに対応）。</summary>
public class ScheduleVote
{
    /// <summary>候補日IDとメンバーIDの複合キー（Firestore のドキュメントID）。</summary>
    public required string Id { get; set; }

    /// <summary>対象の候補日ID。</summary>
    public required string CandidateId { get; set; }

    /// <summary>投票したメンバーのID。</summary>
    public required string MemberId { get; set; }

    /// <summary>投票したメンバーの表示名。</summary>
    public required string MemberName { get; set; }

    /// <summary>参加意思。「未定」はまだ投票していない状態を表す。</summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>更新日時（UTC）。</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>候補日IDとメンバーIDから、Firestore ドキュメントIDとなる複合キーを生成する。</summary>
    public static string BuildId(string candidateId, string memberId) => $"{candidateId}_{memberId}";
}
