using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Tests.TestSupport;

/// <summary>
/// Firestore の REST API 呼び出しをメモリ上のコレクションで模倣する <see cref="IFirestoreClient"/>。
/// 画面（Pages）のテストで、一覧取得・作成・更新・削除が一貫して反映されることを検証するために使用する。
/// UpsertDocumentAsync は実装（<see cref="FirestoreClient"/>）と同様に updateMask 相当のフィールド単位マージを行う。
/// </summary>
internal class FakeFirestoreClient : IFirestoreClient
{
    private readonly Dictionary<string, Dictionary<string, Dictionary<string, object?>>> _collections = [];

    /// <summary>各メソッドの呼び出し履歴（メソッド種別, コレクション名）。テストで通信回数(全件取得の有無等)を検証するために使用する。</summary>
    public List<(string Method, string Collection)> Calls { get; } = [];

    public Task<List<FirestoreDocument>> ListDocumentsAsync(string collection, CancellationToken ct = default)
    {
        Calls.Add(("List", collection));
        return Task.FromResult(GetCollection(collection)
            .Select(kv => new FirestoreDocument { Id = kv.Key, Fields = new Dictionary<string, object?>(kv.Value) })
            .ToList());
    }

    public Task<FirestoreDocument?> GetDocumentAsync(string collection, string documentId, CancellationToken ct = default)
    {
        Calls.Add(("Get", collection));
        var found = GetCollection(collection).TryGetValue(documentId, out var fields)
            ? new FirestoreDocument { Id = documentId, Fields = new Dictionary<string, object?>(fields) }
            : null;
        return Task.FromResult(found);
    }

    public Task<List<FirestoreDocument>> QueryDocumentsAsync(string collection, IReadOnlyDictionary<string, object?> equalsFilters, CancellationToken ct = default)
    {
        Calls.Add(("Query", collection));
        var matches = GetCollection(collection)
            .Where(kv => equalsFilters.All(f => kv.Value.TryGetValue(f.Key, out var v) && Equals(v, f.Value)))
            .Select(kv => new FirestoreDocument { Id = kv.Key, Fields = new Dictionary<string, object?>(kv.Value) })
            .ToList();
        return Task.FromResult(matches);
    }

    public Task UpsertDocumentAsync(string collection, string documentId, Dictionary<string, object?> fields, CancellationToken ct = default)
    {
        var col = GetCollection(collection);
        if (!col.TryGetValue(documentId, out var existing))
        {
            existing = [];
            col[documentId] = existing;
        }

        foreach (var (key, value) in fields)
            existing[key] = value;

        return Task.CompletedTask;
    }

    public Task DeleteDocumentAsync(string collection, string documentId, CancellationToken ct = default)
    {
        GetCollection(collection).Remove(documentId);
        return Task.CompletedTask;
    }

    /// <summary>テストの初期データとして、指定コレクションにドキュメントを直接投入する。</summary>
    public void Seed(string collection, string documentId, Dictionary<string, object?> fields) =>
        GetCollection(collection)[documentId] = new Dictionary<string, object?>(fields);

    public bool Contains(string collection, string documentId) => GetCollection(collection).ContainsKey(documentId);

    private Dictionary<string, Dictionary<string, object?>> GetCollection(string collection)
    {
        if (!_collections.TryGetValue(collection, out var col))
        {
            col = [];
            _collections[collection] = col;
        }
        return col;
    }
}
