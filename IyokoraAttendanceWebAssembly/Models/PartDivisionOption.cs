namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>選択UIに表示するための、分割方式と表示名の組。</summary>
public class PartDivisionOption
{
    public required PartDivision Division { get; init; }
    public required string Label { get; init; }

    /// <summary>選択可能な分割方式を選択肢として列挙したもの。</summary>
    public static List<PartDivisionOption> All { get; } = PartDivisionExtensions.All
        .Select(d => new PartDivisionOption { Division = d, Label = d.ToDisplayName() })
        .ToList();
}
