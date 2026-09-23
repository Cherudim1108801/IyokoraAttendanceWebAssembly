namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>練習予定の曲に紐づく録音音源1件（OneDriveなどへのリンク）。1曲につき複数件登録できる。</summary>
public class PracticeRecording
{
    /// <summary>この録音を一意に識別するID（同一曲内の録音の編集・削除・強調表示切替に使う）。</summary>
    public required string Id { get; init; }

    /// <summary>録音音源へのリンク（OneDriveなど）。</summary>
    public required string Url { get; init; }

    /// <summary>「音源」タブで強調表示（ピン留め）するかどうか。</summary>
    public bool IsFeatured { get; init; }
}
