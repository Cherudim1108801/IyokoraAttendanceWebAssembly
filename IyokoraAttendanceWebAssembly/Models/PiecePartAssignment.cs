namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>曲におけるパートの割り振り（使用パートと内部分割方式）の1件分。</summary>
public class PiecePartAssignment
{
    public required PartType Part { get; init; }
    public required PartDivision Division { get; init; }

    /// <summary>内部分割を反映した、このパートの実際のパート名一覧。</summary>
    public IReadOnlyList<string> SubPartLabels => Division.ToSubPartLabels(Part);
}
