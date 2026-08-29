namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>ある練習に対するメンバー1人分の出欠状態。</summary>
public enum AttendanceStatus
{
    Undecided,
    Attending,
    NotAttending
}

/// <summary><see cref="AttendanceStatus"/> の表示用ヘルパー。</summary>
public static class AttendanceStatusExtensions
{
    /// <summary>UI表示用の日本語名を返す。</summary>
    public static string ToDisplayName(this AttendanceStatus status) => status switch
    {
        AttendanceStatus.Attending => "参加",
        AttendanceStatus.NotAttending => "不参加",
        AttendanceStatus.Undecided => "未定",
        _ => status.ToString()
    };

    /// <summary>出欠状態を識別するためのUI表示色を返す。</summary>
    public static string ToColorHex(this AttendanceStatus status) => status switch
    {
        AttendanceStatus.Attending => "#3EC1A4",
        AttendanceStatus.NotAttending => "#E0607A",
        AttendanceStatus.Undecided => "#B0B0B8",
        _ => "#808080"
    };
}
