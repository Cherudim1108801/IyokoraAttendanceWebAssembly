namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>曲選択フォーム用の入力状態（練習予定の演奏予定曲を選択する画面で使用）。</summary>
public class PieceSelectionInput
{
    public required string PieceId { get; init; }
    public required string Title { get; init; }
    public bool IsSelected { get; set; }
}
