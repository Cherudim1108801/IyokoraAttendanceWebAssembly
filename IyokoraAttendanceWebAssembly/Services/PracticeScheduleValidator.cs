using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>練習の開始・終了時刻およびタイムスケジュール入力の検証ロジック。練習予定の新規登録・編集画面で共通利用する。</summary>
public static class PracticeScheduleValidator
{
    /// <summary>開始・終了時刻の組が未入力、または妥当（10分単位・開始 &lt; 終了）かどうかを検証する。</summary>
    public static bool TryValidateTimeRange(TimeOnly? start, TimeOnly? end, out string? error)
    {
        error = null;
        if (start is null && end is null)
            return true;

        if (start is null || end is null)
        {
            error = "開始時刻と終了時刻は両方入力してください。";
            return false;
        }

        if (start.Value.Minute % 10 != 0 || end.Value.Minute % 10 != 0)
        {
            error = "時刻は10分単位で入力してください。";
            return false;
        }

        if (end <= start)
        {
            error = "終了時刻は開始時刻より後にしてください。";
            return false;
        }

        return true;
    }

    /// <summary>タイムスケジュール1項目分の開始・終了時刻が必須かつ妥当であることを検証する。</summary>
    public static bool TryValidateTimelineItem(TimelineItemInput item, out string? error)
    {
        if (!TryValidateTimeRange(item.StartTime, item.EndTime, out error))
            return false;

        if (item.StartTime is null)
        {
            error = "タイムスケジュールの開始・終了時刻を入力してください。";
            return false;
        }

        return true;
    }

    /// <summary>"HH:mm" 形式の文字列を <see cref="TimeOnly"/> に変換する。空文字列や不正な形式の場合は null。</summary>
    public static TimeOnly? ParseTimeOrNull(string value) =>
        TimeOnly.TryParse(value, out var time) ? time : null;
}
