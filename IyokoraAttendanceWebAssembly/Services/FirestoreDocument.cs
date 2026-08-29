namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>Firestore ドキュメント1件分を、ID とフィールドの緩やかな型付き辞書として表す。</summary>
public class FirestoreDocument
{
    public required string Id { get; init; }
    public required Dictionary<string, object?> Fields { get; init; }

    /// <summary>指定フィールドを文字列として取得する。存在しない・型が異なる場合は <paramref name="fallback"/>。</summary>
    public string GetString(string key, string fallback = "") =>
        Fields.TryGetValue(key, out var v) && v is string s ? s : fallback;

    /// <summary>指定フィールドを整数として取得する。存在しない・型が異なる場合は <paramref name="fallback"/>。</summary>
    public long GetLong(string key, long fallback = 0) =>
        Fields.TryGetValue(key, out var v) && v is long l ? l : fallback;

    /// <summary>指定フィールドを日時として取得する。存在しない・型が異なる場合は <paramref name="fallback"/>。</summary>
    public DateTime GetDateTime(string key, DateTime fallback = default) =>
        Fields.TryGetValue(key, out var v) && v is DateTime dt ? dt : fallback;

    /// <summary>指定フィールドを真偽値として取得する。存在しない・型が異なる場合は <paramref name="fallback"/>。</summary>
    public bool GetBool(string key, bool fallback = false) =>
        Fields.TryGetValue(key, out var v) && v is bool b ? b : fallback;

    /// <summary>指定フィールドを配列として取得する。存在しない・型が異なる場合は空リスト。</summary>
    public List<object?> GetList(string key) =>
        Fields.TryGetValue(key, out var v) && v is List<object?> list ? list : [];
}
