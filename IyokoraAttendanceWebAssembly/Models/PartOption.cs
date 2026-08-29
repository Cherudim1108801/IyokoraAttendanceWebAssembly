namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>選択UIに表示するための、パートと表示名の組。</summary>
public class PartOption
{
    public required PartType Part { get; init; }
    public required string Label { get; init; }

    /// <summary>全パートを選択肢として列挙したもの。</summary>
    public static List<PartOption> All { get; } = PartTypeExtensions.All
        .Select(p => new PartOption { Part = p, Label = p.ToDisplayName() })
        .ToList();
}
