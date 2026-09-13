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

    /// <summary>
    /// 指定コレクション内から、フィールドの完全一致条件（複数指定時は AND）に合致するドキュメントのみを
    /// サーバー側で絞り込んで取得する。<see cref="ListDocumentsAsync"/> と異なりコレクション全体を
    /// 転送しないため、絞り込み後の件数が少ない場合は通信量を大きく削減できる。
    /// </summary>
    /// <param name="collection">コレクション名。</param>
    /// <param name="equalsFilters">「フィールド名 = 値」の一致条件。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task<List<FirestoreDocument>> QueryDocumentsAsync(string collection, IReadOnlyDictionary<string, object?> equalsFilters, CancellationToken ct = default);

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
