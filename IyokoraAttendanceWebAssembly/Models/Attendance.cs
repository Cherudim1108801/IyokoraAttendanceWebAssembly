namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>ある練習に対する、あるメンバー1人分の出欠（Firestore の <c>attendances</c> ドキュメントに対応）。</summary>
public class Attendance
{
    /// <summary>ドキュメントID。<see cref="BuildId"/> で生成される複合キー。</summary>
    public required string Id { get; set; }

    /// <summary>対象の練習予定ID。</summary>
    public required string PracticeId { get; set; }

    /// <summary>対象のメンバーID。</summary>
    public required string MemberId { get; set; }

    /// <summary>メンバー名（表示用に非正規化して保持）。</summary>
    public required string MemberName { get; set; }

    /// <summary>所属パート（表示・集計用に非正規化して保持）。</summary>
    public required PartType Part { get; set; }

    /// <summary>出欠状態。</summary>
    public AttendanceStatus Status { get; set; }

    /// <summary>最終更新日時（UTC）。</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>練習予定IDとメンバーIDから、Firestore ドキュメントIDとなる複合キーを生成する。</summary>
    public static string BuildId(string practiceId, string memberId) => $"{practiceId}_{memberId}";
}
