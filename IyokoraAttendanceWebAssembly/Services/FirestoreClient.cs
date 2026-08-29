using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>
/// Cloud Firestore の REST API を直接呼び出す薄いクライアント。
/// 認証なし運用のため、Firestore 側のセキュリティルールで
/// 未認証アクセスを許可しておく必要がある（<see cref="FirebaseOptions"/> 参照）。
/// </summary>
public class FirestoreClient(HttpClient http)
{
    /// <summary>指定コレクション内の全ドキュメントを取得する（ページングを内部で吸収）。</summary>
    /// <param name="collection">コレクション名。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<FirestoreDocument>> ListDocumentsAsync(string collection, CancellationToken ct = default)
    {
        var results = new List<FirestoreDocument>();
        string? pageToken = null;

        do
        {
            var url = $"{FirebaseOptions.FirestoreBaseUrl}/{collection}?pageSize=300";
            if (!string.IsNullOrEmpty(pageToken))
                url += $"&pageToken={Uri.EscapeDataString(pageToken)}";

            using var resp = await http.GetAsync(url, ct);
            if (resp.StatusCode == HttpStatusCode.NotFound)
                break; // コレクション未作成 = データなし

            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);
            var node = JsonNode.Parse(json)?.AsObject();

            if (node?["documents"] is JsonArray docs)
            {
                foreach (var doc in docs)
                {
                    if (doc is JsonObject docObj)
                        results.Add(ParseDocument(docObj));
                }
            }

            pageToken = node?["nextPageToken"]?.GetValue<string>();
        } while (!string.IsNullOrEmpty(pageToken));

        return results;
    }

    /// <summary>指定IDのドキュメントを1件取得する。存在しない場合は null。</summary>
    /// <param name="collection">コレクション名。</param>
    /// <param name="documentId">ドキュメントID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<FirestoreDocument?> GetDocumentAsync(string collection, string documentId, CancellationToken ct = default)
    {
        var url = $"{FirebaseOptions.FirestoreBaseUrl}/{collection}/{Uri.EscapeDataString(documentId)}";
        using var resp = await http.GetAsync(url, ct);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;

        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        var node = JsonNode.Parse(json)?.AsObject();
        return node is null ? null : ParseDocument(node);
    }

    /// <summary>指定IDのドキュメントを作成または上書き保存する（存在しなければ新規作成）。</summary>
    /// <param name="collection">コレクション名。</param>
    /// <param name="documentId">ドキュメントID。</param>
    /// <param name="fields">保存するフィールドの一覧。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task UpsertDocumentAsync(string collection, string documentId, Dictionary<string, object?> fields, CancellationToken ct = default)
    {
        var body = new JsonObject { ["fields"] = ToFirestoreFields(fields) };
        var mask = string.Join("&", fields.Keys.Select(k => $"updateMask.fieldPaths={Uri.EscapeDataString(k)}"));
        var url = $"{FirebaseOptions.FirestoreBaseUrl}/{collection}/{Uri.EscapeDataString(documentId)}?{mask}";

        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        using var resp = await http.SendAsync(request, ct);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>指定IDのドキュメントを削除する。存在しない場合は何もしない。</summary>
    /// <param name="collection">コレクション名。</param>
    /// <param name="documentId">ドキュメントID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task DeleteDocumentAsync(string collection, string documentId, CancellationToken ct = default)
    {
        var url = $"{FirebaseOptions.FirestoreBaseUrl}/{collection}/{Uri.EscapeDataString(documentId)}";
        using var resp = await http.DeleteAsync(url, ct);
        if (resp.StatusCode != HttpStatusCode.NotFound)
            resp.EnsureSuccessStatusCode();
    }

    private static JsonObject ToFirestoreFields(Dictionary<string, object?> fields)
    {
        var obj = new JsonObject();
        foreach (var (key, value) in fields)
            obj[key] = ToFirestoreValue(value);
        return obj;
    }

    private static JsonObject ToFirestoreValue(object? value) => value switch
    {
        null => new JsonObject { ["nullValue"] = null },
        string s => new JsonObject { ["stringValue"] = s },
        bool b => new JsonObject { ["booleanValue"] = b },
        int i => new JsonObject { ["integerValue"] = i.ToString(CultureInfo.InvariantCulture) },
        long l => new JsonObject { ["integerValue"] = l.ToString(CultureInfo.InvariantCulture) },
        double d => new JsonObject { ["doubleValue"] = d },
        DateTime dt => new JsonObject { ["timestampValue"] = dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture) },
        Enum e => new JsonObject { ["stringValue"] = e.ToString() },
        Dictionary<string, object?> map => new JsonObject { ["mapValue"] = new JsonObject { ["fields"] = ToFirestoreFields(map) } },
        IEnumerable<object?> list => new JsonObject { ["arrayValue"] = new JsonObject { ["values"] = new JsonArray(list.Select(ToFirestoreValue).ToArray()) } },
        _ => throw new NotSupportedException($"Unsupported Firestore value type: {value.GetType()}")
    };

    private static FirestoreDocument ParseDocument(JsonObject doc)
    {
        var name = doc["name"]?.GetValue<string>() ?? string.Empty;
        var id = name.Contains('/') ? name[(name.LastIndexOf('/') + 1)..] : name;

        var fields = new Dictionary<string, object?>();
        if (doc["fields"] is JsonObject fieldsObj)
        {
            foreach (var (key, value) in fieldsObj)
            {
                if (value is JsonObject valueObj)
                    fields[key] = ParseValue(valueObj);
            }
        }

        return new FirestoreDocument { Id = id, Fields = fields };
    }

    private static object? ParseValue(JsonObject valueObj)
    {
        foreach (var (kind, value) in valueObj)
        {
            return kind switch
            {
                "stringValue" => value?.GetValue<string>(),
                "booleanValue" => value?.GetValue<bool>(),
                "integerValue" => long.Parse(value!.GetValue<string>(), CultureInfo.InvariantCulture),
                "doubleValue" => value?.GetValue<double>(),
                "timestampValue" => DateTime.Parse(value!.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                "arrayValue" => ParseArray(value as JsonObject),
                "mapValue" => ParseMap(value as JsonObject),
                "nullValue" => null,
                _ => null
            };
        }
        return null;
    }

    private static List<object?> ParseArray(JsonObject? arrayValue)
    {
        var list = new List<object?>();
        if (arrayValue?["values"] is JsonArray values)
        {
            foreach (var v in values)
            {
                if (v is JsonObject vo)
                    list.Add(ParseValue(vo));
            }
        }
        return list;
    }

    private static Dictionary<string, object?> ParseMap(JsonObject? mapValue)
    {
        var dict = new Dictionary<string, object?>();
        if (mapValue?["fields"] is JsonObject fields)
        {
            foreach (var (key, val) in fields)
            {
                if (val is JsonObject vo)
                    dict[key] = ParseValue(vo);
            }
        }
        return dict;
    }
}
