namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>日程投票の候補日1件（Firestore の <c>scheduleCandidates</c> ドキュメントに対応）。</summary>
public class ScheduleCandidate
{
    /// <summary>候補日ID（Firestore のドキュメントID）。</summary>
    public required string Id { get; set; }

    /// <summary>候補日。カレンダー日付として扱い、時刻情報は持たない。</summary>
    public DateTime Date { get; set; }

    /// <summary>候補日の時間帯区分（午前／午後／夜間）。</summary>
    public TimeOfDay TimeOfDay { get; set; }

    /// <summary>登録日時（UTC）。</summary>
    public DateTime CreatedAt { get; set; }
}
