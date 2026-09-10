namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>1回分の練習予定（Firestore の <c>practices</c> ドキュメントに対応）。</summary>
public class Practice
{
    /// <summary>練習予定ID（Firestore のドキュメントID）。</summary>
    public required string Id { get; set; }

    /// <summary>練習日。カレンダー日付として扱い、時刻情報は持たない。</summary>
    public DateTime Date { get; set; }

    /// <summary>タイトル（任意）。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>場所（任意）。</summary>
    public string Place { get; set; } = string.Empty;

    /// <summary>練習開始時刻（"HH:mm" 形式、任意）。未設定の場合は空文字列。</summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>練習終了時刻（"HH:mm" 形式、任意）。未設定の場合は空文字列。</summary>
    public string EndTime { get; set; } = string.Empty;

    /// <summary>練習のタイムスケジュール（10分単位で登録する詳細な予定）。</summary>
    public List<PracticeTimelineItem> TimelineItems { get; set; } = [];

    /// <summary>この練習で演奏予定の曲。</summary>
    public List<PracticePieceRef> Pieces { get; set; } = [];

    /// <summary>登録日時（UTC）。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>鍵の受け取りが必要かどうか。練習予定の登録時に選択する。管理者のみが参照できる情報。</summary>
    public bool RequiresKeyPickup { get; set; }

    /// <summary>鍵を受け取り済みかどうか。<see cref="RequiresKeyPickup"/> が true の場合のみ意味を持つ。管理者のみが参照・更新できる情報。</summary>
    public bool KeyPickedUp { get; set; }

    /// <summary>鍵を受け取ったメンバーの表示名。<see cref="KeyPickedUp"/> が true の場合のみ意味を持つ。</summary>
    public string KeyPickedUpByName { get; set; } = string.Empty;

    /// <summary>一覧表示用：演奏予定曲の曲名を読点区切りで結合した文字列。未選択の場合は空文字列。</summary>
    public string PiecesSummary => string.Join("、", Pieces.Select(p => p.Title));

    /// <summary>一覧表示用：開始〜終了時刻を結合した文字列。いずれか未設定の場合は空文字列。</summary>
    public string TimeRangeSummary => !string.IsNullOrEmpty(StartTime) && !string.IsNullOrEmpty(EndTime)
        ? $"{StartTime}〜{EndTime}"
        : string.Empty;
}
