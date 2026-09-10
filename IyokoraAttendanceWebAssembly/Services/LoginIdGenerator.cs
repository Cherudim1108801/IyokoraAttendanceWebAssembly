namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>
/// ログインID（複数端末から同じアカウントを使うための、パスワード不要の識別子）の
/// 生成・検証を行う。ID は「団体ごとに設定するアルファベット prefix + 数字4桁」の形式。
/// </summary>
public static class LoginIdGenerator
{
    private const int DigitLength = 4;
    private const int MaxNumber = 9999;

    /// <summary>指定 prefix を使い、ランダムな数字4桁を組み合わせた候補IDを1件生成する。</summary>
    public static string GenerateCandidate(string prefix) =>
        Format(prefix, Random.Shared.Next(0, MaxNumber + 1));

    /// <summary>prefix と数字を、先頭 prefix + ゼロ埋め数字4桁の形式に整形する。</summary>
    public static string Format(string prefix, int number) => $"{prefix}{number:D4}";

    /// <summary>
    /// 指定文字列が有効なログインID形式（英字1文字以上 + 数字4桁のみ）かどうかを判定する。
    /// 比較前に <see cref="Normalize"/> で正規化しておくこと。
    /// </summary>
    public static bool IsValidFormat(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= DigitLength)
            return false;

        var letters = value[..^DigitLength];
        var digits = value[^DigitLength..];
        return letters.Length > 0
            && letters.All(char.IsAsciiLetterUpper)
            && digits.All(char.IsAsciiDigit);
    }

    /// <summary>入力値を比較・保存用に正規化する（前後空白の除去、英字の大文字化）。</summary>
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();

    /// <summary>
    /// <paramref name="candidateFactory"/> で候補IDを生成し、<paramref name="isTaken"/> が使用済みと判定する間は
    /// 再生成を繰り返して、未使用の一意なIDを求める。
    /// </summary>
    /// <param name="candidateFactory">候補IDを1件生成する関数。</param>
    /// <param name="isTaken">候補IDが既に使用済みかどうかを判定する関数。</param>
    /// <param name="maxAttempts">最大試行回数。既定は1000回。</param>
    /// <exception cref="InvalidOperationException">試行回数内に未使用のIDを見つけられなかった場合。</exception>
    public static string ResolveUnique(Func<string> candidateFactory, Func<string, bool> isTaken, int maxAttempts = 1000)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidate = candidateFactory();
            if (!isTaken(candidate))
                return candidate;
        }

        throw new InvalidOperationException("一意なログインIDを生成できませんでした。");
    }
}
