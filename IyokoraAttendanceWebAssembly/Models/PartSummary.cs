namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>次回練習における、パートごとの参加状況の集計結果（トップ画面カード表示用）。</summary>
public class PartSummary
{
    public required PartType Part { get; init; }
    public required string Label { get; init; }
    public required string ColorHex { get; init; }
    public required string CardBackgroundColorHex { get; init; }
    public int AttendingCount { get; init; }
    public int MemberCount { get; init; }

    /// <summary>参加予定メンバーの名前一覧（詳細モーダル表示用）。</summary>
    public required IReadOnlyList<string> AttendeeNames { get; init; }

    public string CountLabel => $"{AttendingCount} / {MemberCount} 人";
}
