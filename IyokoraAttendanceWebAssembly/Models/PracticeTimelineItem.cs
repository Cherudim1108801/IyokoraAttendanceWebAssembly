namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>練習のタイムスケジュールにおける1コマ分の予定。10分単位で開始・終了時刻を登録する。</summary>
public class PracticeTimelineItem
{
    /// <summary>開始時刻（"HH:mm" 形式）。</summary>
    public required string StartTime { get; set; }

    /// <summary>終了時刻（"HH:mm" 形式）。</summary>
    public required string EndTime { get; set; }

    /// <summary>この時間帯に行う内容。</summary>
    public string Content { get; set; } = string.Empty;
}
