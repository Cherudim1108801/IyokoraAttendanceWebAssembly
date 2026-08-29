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

    /// <summary>この練習で演奏予定の曲。</summary>
    public List<PracticePieceRef> Pieces { get; set; } = [];

    /// <summary>登録日時（UTC）。</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>鍵の受け取りが必要かどうか。練習予定の登録時に選択する。管理者のみが参照できる情報。</summary>
    public bool RequiresKeyPickup { get; set; }

    /// <summary>鍵を受け取り済みかどうか。<see cref="RequiresKeyPickup"/> が true の場合のみ意味を持つ。管理者のみが参照・更新できる情報。</summary>
    public bool KeyPickedUp { get; set; }

    /// <summary>一覧表示用：演奏予定曲の曲名を読点区切りで結合した文字列。未選択の場合は空文字列。</summary>
    public string PiecesSummary => string.Join("、", Pieces.Select(p => p.Title));
}
