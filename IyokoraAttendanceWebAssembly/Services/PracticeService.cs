using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>Firestore の <c>practices</c> コレクションに対する練習予定の取得・作成・削除を担う。</summary>
public class PracticeService(FirestoreClient client)
{
    private const string Collection = "practices";

    /// <summary>登録されている全練習予定を日付昇順で取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<Practice>> GetAllAsync(CancellationToken ct = default)
    {
        var docs = await client.ListDocumentsAsync(Collection, ct);
        return docs
            .Where(d => d.GetString("groupId") == FirebaseOptions.GroupId)
            .Select(ToPractice)
            .OrderBy(p => p.Date)
            .ToList();
    }

    /// <summary>指定IDの練習予定を1件取得する。存在しない場合は null。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<Practice?> GetByIdAsync(string practiceId, CancellationToken ct = default)
    {
        var doc = await client.GetDocumentAsync(Collection, practiceId, ct);
        return doc is null ? null : ToPractice(doc);
    }

    /// <summary>今日以降の練習予定を、日付の古い順に取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<Practice>> GetUpcomingAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        var today = DateTime.Today;
        return all.Where(p => p.Date.Date >= today).OrderBy(p => p.Date).ToList();
    }

    /// <summary>今日より前の練習予定（過去の練習データ）を、日付の新しい順に取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<Practice>> GetPastAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        var today = DateTime.Today;
        return all.Where(p => p.Date.Date < today).OrderByDescending(p => p.Date).ToList();
    }

    /// <summary>今日以降で最も近い練習予定を取得する。存在しない場合は null。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<Practice?> GetNextUpcomingAsync(CancellationToken ct = default)
    {
        var upcoming = await GetUpcomingAsync(ct);
        return upcoming.FirstOrDefault();
    }

    /// <summary>新しい練習予定を登録する。</summary>
    /// <param name="date">練習日（時刻情報は無視される）。</param>
    /// <param name="title">タイトル（任意）。</param>
    /// <param name="place">場所（任意）。</param>
    /// <param name="pieces">演奏予定曲。</param>
    /// <param name="requiresKeyPickup">鍵の受け取りが必要かどうか。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>発行された練習予定ID。</returns>
    public async Task<string> CreateAsync(DateTime date, string title, string place, IReadOnlyList<PracticePieceRef> pieces, bool requiresKeyPickup, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        // 練習日は時刻を持たないカレンダー日付として扱う。DateTime.ToUniversalTime() による
        // タイムゾーン変換で日付がずれないよう、日付部分だけを UTC として保存する。
        var dateOnly = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["date"] = dateOnly,
            ["title"] = title,
            ["place"] = place,
            ["pieces"] = ToPieceFields(pieces),
            ["requiresKeyPickup"] = requiresKeyPickup,
            ["keyPickedUp"] = false,
            ["createdAt"] = DateTime.UtcNow
        };
        await client.UpsertDocumentAsync(Collection, id, fields, ct);
        return id;
    }

    /// <summary>指定IDの練習予定を削除する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task DeleteAsync(string practiceId, CancellationToken ct = default) =>
        client.DeleteDocumentAsync(Collection, practiceId, ct);

    /// <summary>指定の練習における、指定の曲の録音音源リンクを設定・変更・削除する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="pieceId">対象の曲ID。</param>
    /// <param name="recordingUrl">録音音源へのリンク（OneDriveなど）。削除する場合は null。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task SetPieceRecordingUrlAsync(string practiceId, string pieceId, string? recordingUrl, CancellationToken ct = default)
    {
        var practice = await GetByIdAsync(practiceId, ct);
        if (practice is null)
            return;

        var updatedPieces = practice.Pieces
            .Select(p => p.PieceId == pieceId
                ? new PracticePieceRef { PieceId = p.PieceId, Title = p.Title, RecordingUrl = recordingUrl, IsFeatured = p.IsFeatured }
                : p)
            .ToList();

        var fields = new Dictionary<string, object?>
        {
            ["pieces"] = ToPieceFields(updatedPieces)
        };
        await client.UpsertDocumentAsync(Collection, practiceId, fields, ct);
    }

    /// <summary>指定の練習における、指定の曲の録音を「音源」タブで強調表示（ピン留め）するかどうかを設定する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="pieceId">対象の曲ID。</param>
    /// <param name="isFeatured">強調表示するかどうか。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task SetPieceRecordingFeaturedAsync(string practiceId, string pieceId, bool isFeatured, CancellationToken ct = default)
    {
        var practice = await GetByIdAsync(practiceId, ct);
        if (practice is null)
            return;

        var updatedPieces = practice.Pieces
            .Select(p => p.PieceId == pieceId
                ? new PracticePieceRef { PieceId = p.PieceId, Title = p.Title, RecordingUrl = p.RecordingUrl, IsFeatured = isFeatured }
                : p)
            .ToList();

        var fields = new Dictionary<string, object?>
        {
            ["pieces"] = ToPieceFields(updatedPieces)
        };
        await client.UpsertDocumentAsync(Collection, practiceId, fields, ct);
    }

    /// <summary>指定の練習の鍵の受け取り状況を設定する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="keyPickedUp">受け取り済みかどうか。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task SetKeyPickedUpAsync(string practiceId, bool keyPickedUp, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, object?>
        {
            ["keyPickedUp"] = keyPickedUp
        };
        await client.UpsertDocumentAsync(Collection, practiceId, fields, ct);
    }

    private static List<object?> ToPieceFields(IReadOnlyList<PracticePieceRef> pieces) => pieces
        .Select(p => new Dictionary<string, object?>
        {
            ["pieceId"] = p.PieceId,
            ["title"] = p.Title,
            ["recordingUrl"] = p.RecordingUrl,
            ["featured"] = p.IsFeatured
        })
        .Cast<object?>()
        .ToList();

    private static Practice ToPractice(FirestoreDocument doc) => new()
    {
        Id = doc.Id,
        Date = doc.GetDateTime("date"),
        Title = doc.GetString("title"),
        Place = doc.GetString("place"),
        Pieces = doc.GetList("pieces")
            .OfType<Dictionary<string, object?>>()
            .Select(ToPieceRef)
            .ToList(),
        CreatedAt = doc.GetDateTime("createdAt"),
        RequiresKeyPickup = doc.GetBool("requiresKeyPickup"),
        KeyPickedUp = doc.GetBool("keyPickedUp")
    };

    private static PracticePieceRef ToPieceRef(Dictionary<string, object?> fields) => new()
    {
        PieceId = fields.GetValueOrDefault("pieceId") as string ?? string.Empty,
        Title = fields.GetValueOrDefault("title") as string ?? string.Empty,
        RecordingUrl = fields.GetValueOrDefault("recordingUrl") as string,
        IsFeatured = fields.GetValueOrDefault("featured") as bool? ?? false
    };
}
