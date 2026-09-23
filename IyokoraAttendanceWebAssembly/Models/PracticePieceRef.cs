namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>練習予定における演奏予定曲への参照。曲名は登録時点のスナップショットとして非正規化保持する。</summary>
public class PracticePieceRef
{
    public required string PieceId { get; init; }
    public required string Title { get; init; }

    /// <summary>この練習でのこの曲の録音音源（OneDriveなどへのリンク）。1曲につき複数件登録できる。</summary>
    public List<PracticeRecording> Recordings { get; init; } = [];
}
