namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>「音源」タブに表示する、練習で録音された1曲分の音源データ。</summary>
public class RecordingItem
{
    public required string PracticeId { get; init; }
    public required string PieceId { get; init; }

    /// <summary>曲名。</summary>
    public required string Title { get; init; }

    /// <summary>この録音が行われた練習の日付を含む表示用ラベル。</summary>
    public required string PracticeLabel { get; init; }

    /// <summary>録音音源へのリンク（OneDriveなど）。</summary>
    public required string RecordingUrl { get; init; }

    /// <summary>強調表示（ピン留め）されているかどうか。</summary>
    public bool IsFeatured { get; init; }
}
