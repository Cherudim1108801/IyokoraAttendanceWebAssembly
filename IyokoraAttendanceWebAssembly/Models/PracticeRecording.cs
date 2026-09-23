namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>練習予定の曲に紐づく録音音源1件（OneDriveなどへのリンク）。1曲につき複数件登録できる。</summary>
public class PracticeRecording
{
    /// <summary>この録音を一意に識別するID（同一曲内の録音の編集・削除・強調表示切替に使う）。</summary>
    public required string Id { get; init; }

    /// <summary>録音を管理しやすくするための名前（任意）。未設定の場合は空文字列。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>録音音源へのリンク（OneDriveなど）。</summary>
    public required string Url { get; init; }

    /// <summary>表示用のラベル。名前が設定されていればそれを使い、無ければ既定の文言。</summary>
    public string DisplayName => string.IsNullOrEmpty(Name) ? "録音を聴く" : Name;

    /// <summary>「音源」タブで強調表示（ピン留め）するかどうか。</summary>
    public bool IsFeatured { get; init; }
}
