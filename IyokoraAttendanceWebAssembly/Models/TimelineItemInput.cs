namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>タイムスケジュール1項目分の入力フォーム用の状態（画面での編集用。保存時に <see cref="PracticeTimelineItem"/> に変換する）。</summary>
public class TimelineItemInput
{
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string Content { get; set; } = string.Empty;
}
