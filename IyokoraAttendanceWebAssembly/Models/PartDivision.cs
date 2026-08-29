namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>曲におけるパート内部の分割方式。</summary>
public enum PartDivision
{
    /// <summary>分割しない。</summary>
    None,

    /// <summary>上・下の2つに分割する（例：ソプラノ上／ソプラノ下）。</summary>
    UpperLower
}

/// <summary><see cref="PartDivision"/> の表示用ヘルパー。</summary>
public static class PartDivisionExtensions
{
    /// <summary>UI表示用の日本語名を返す。</summary>
    public static string ToDisplayName(this PartDivision division) => division switch
    {
        PartDivision.None => "分割しない",
        PartDivision.UpperLower => "上・下に分割",
        _ => division.ToString()
    };

    /// <summary>指定パートに対する内部パート名の一覧を返す（分割しない場合はパート名そのもの1件）。</summary>
    public static IReadOnlyList<string> ToSubPartLabels(this PartDivision division, PartType part) => division switch
    {
        PartDivision.UpperLower => [$"{part.ToDisplayName()}上", $"{part.ToDisplayName()}下"],
        _ => [part.ToDisplayName()]
    };

    /// <summary>選択可能な分割方式を固定順で列挙する。</summary>
    public static readonly PartDivision[] All =
    [
        PartDivision.None,
        PartDivision.UpperLower
    ];
}
