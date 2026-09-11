namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>候補日の時間帯区分。</summary>
public enum TimeOfDay
{
    Morning,
    Afternoon,
    Evening
}

/// <summary><see cref="TimeOfDay"/> の表示用ヘルパー。</summary>
public static class TimeOfDayExtensions
{
    /// <summary>UI表示用の日本語名を返す。</summary>
    public static string ToDisplayName(this TimeOfDay timeOfDay) => timeOfDay switch
    {
        TimeOfDay.Morning => "午前",
        TimeOfDay.Afternoon => "午後",
        TimeOfDay.Evening => "夜間",
        _ => timeOfDay.ToString()
    };

    /// <summary>全時間帯区分を固定順（午前→午後→夜間）で列挙する。</summary>
    public static readonly TimeOfDay[] All =
    [
        TimeOfDay.Morning,
        TimeOfDay.Afternoon,
        TimeOfDay.Evening
    ];
}
