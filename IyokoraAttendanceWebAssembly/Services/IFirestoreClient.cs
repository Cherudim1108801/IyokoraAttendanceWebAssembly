namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>
/// <see cref="FirestoreClient"/> の抽象。各サービスはこれに依存することで、
/// 単体テストで Moq 等によるモックに差し替えられるようにする。
/// </summary>
public interface IFirestoreClient
{
    /// <summary>指定コレクション内の全ドキュメントを取得する（ページングを内部で吸収）。</summary>
    /// <param name="collection">コレクション名。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task<List<FirestoreDocument>> ListDocumentsAsync(string collection, CancellationToken ct = default);

    /// <summary>指定IDのドキュメントを1件取得する。存在しない場合は null。</summary>
    /// <param name="collection">コレクション名。</param>
    /// <param name="documentId">ドキュメントID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task<FirestoreDocument?> GetDocumentAsync(string collection, string documentId, CancellationToken ct = default);

    /// <summary>指定IDのドキュメントを作成または上書き保存する（存在しなければ新規作成）。</summary>
    /// <param name="collection">コレクション名。</param>
    /// <param name="documentId">ドキュメントID。</param>
    /// <param name="fields">保存するフィールドの一覧。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task UpsertDocumentAsync(string collection, string documentId, Dictionary<string, object?> fields, CancellationToken ct = default);

    /// <summary>指定IDのドキュメントを削除する。存在しない場合は何もしない。</summary>
    /// <param name="collection">コレクション名。</param>
    /// <param name="documentId">ドキュメントID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task DeleteDocumentAsync(string collection, string documentId, CancellationToken ct = default);
}
