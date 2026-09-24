using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>practices</c> コレクションに対する <see cref="IPracticeRepository"/> の実装。</summary>
public class PracticeRepository(IFirestoreClient client) : IPracticeRepository
{
    private const string Collection = "practices";

    /// <summary>
    /// サーバー側で絞り込んだ結果のみを取得する（コレクション全体は転送しない）。
    /// </summary>
    public async Task<List<Practice>> GetAllAsync(CancellationToken ct = default)
    {
        var filters = new Dictionary<string, object?> { ["groupId"] = FirebaseOptions.GroupId };
        var docs = await client.QueryDocumentsAsync(Collection, filters, ct);
        return docs
            .Select(ToPractice)
            .OrderBy(p => p.Date)
            .ToList();
    }

    public async Task<Practice?> GetByIdAsync(string practiceId, CancellationToken ct = default)
    {
        var doc = await client.GetDocumentAsync(Collection, practiceId, ct);
        return doc is null ? null : ToPractice(doc);
    }

    public Task CreateAsync(string practiceId, DateTime date, string title, string place, string startTime, string endTime, IReadOnlyList<PracticeTimelineItem> timelineItems, IReadOnlyList<PracticePieceRef> pieces, bool requiresKeyPickup, DateTime createdAt, CancellationToken ct = default)
    {
        // 練習日は時刻を持たないカレンダー日付として扱う。DateTime.ToUniversalTime() による
        // タイムゾーン変換で日付がずれないよう、日付部分だけを UTC として保存する。
        var dateOnly = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["date"] = dateOnly,
            ["title"] = title,
            ["place"] = place,
            ["startTime"] = startTime,
            ["endTime"] = endTime,
            ["timeline"] = ToTimelineFields(timelineItems),
            ["pieces"] = ToPieceFields(pieces),
            ["requiresKeyPickup"] = requiresKeyPickup,
            ["keyPickedUp"] = false,
            ["createdAt"] = createdAt
        };
        return client.UpsertDocumentAsync(Collection, practiceId, fields, ct);
    }

    public Task UpdateScheduleAsync(string practiceId, string startTime, string endTime, IReadOnlyList<PracticeTimelineItem> timelineItems, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, object?>
        {
            ["startTime"] = startTime,
            ["endTime"] = endTime,
            ["timeline"] = ToTimelineFields(timelineItems)
        };
        return client.UpsertDocumentAsync(Collection, practiceId, fields, ct);
    }

    public Task UpdatePiecesAsync(string practiceId, IReadOnlyList<PracticePieceRef> pieces, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, object?>
        {
            ["pieces"] = ToPieceFields(pieces)
        };
        return client.UpsertDocumentAsync(Collection, practiceId, fields, ct);
    }

    public Task UpdatePlaceAsync(string practiceId, string place, bool requiresKeyPickup, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, object?>
        {
            ["place"] = place,
            ["requiresKeyPickup"] = requiresKeyPickup,
            ["keyPickedUp"] = false,
            ["keyPickedUpByName"] = null
        };
        return client.UpsertDocumentAsync(Collection, practiceId, fields, ct);
    }

    public Task DeleteAsync(string practiceId, CancellationToken ct = default) =>
        client.DeleteDocumentAsync(Collection, practiceId, ct);

    public Task SetKeyPickedUpAsync(string practiceId, bool keyPickedUp, string? keyPickedUpByName, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, object?>
        {
            ["keyPickedUp"] = keyPickedUp,
            ["keyPickedUpByName"] = keyPickedUpByName
        };
        return client.UpsertDocumentAsync(Collection, practiceId, fields, ct);
    }

    private static List<object?> ToTimelineFields(IReadOnlyList<PracticeTimelineItem> items) => items
        .Select(i => new Dictionary<string, object?>
        {
            ["startTime"] = i.StartTime,
            ["endTime"] = i.EndTime,
            ["content"] = i.Content
        })
        .Cast<object?>()
        .ToList();

    private static PracticeTimelineItem ToTimelineItem(Dictionary<string, object?> fields) => new()
    {
        StartTime = fields.GetValueOrDefault("startTime") as string ?? string.Empty,
        EndTime = fields.GetValueOrDefault("endTime") as string ?? string.Empty,
        Content = fields.GetValueOrDefault("content") as string ?? string.Empty
    };

    private static List<object?> ToPieceFields(IReadOnlyList<PracticePieceRef> pieces) => pieces
        .Select(p => new Dictionary<string, object?>
        {
            ["pieceId"] = p.PieceId,
            ["title"] = p.Title,
            ["recordings"] = ToRecordingFields(p.Recordings)
        })
        .Cast<object?>()
        .ToList();

    private static List<object?> ToRecordingFields(IReadOnlyList<PracticeRecording> recordings) => recordings
        .Select(r => new Dictionary<string, object?>
        {
            ["id"] = r.Id,
            ["name"] = r.Name,
            ["url"] = r.Url,
            ["featured"] = r.IsFeatured
        })
        .Cast<object?>()
        .ToList();

    private static Practice ToPractice(FirestoreDocument doc) => new()
    {
        Id = doc.Id,
        Date = doc.GetDateTime("date"),
        Title = doc.GetString("title"),
        Place = doc.GetString("place"),
        StartTime = doc.GetString("startTime"),
        EndTime = doc.GetString("endTime"),
        TimelineItems = doc.GetList("timeline")
            .OfType<Dictionary<string, object?>>()
            .Select(ToTimelineItem)
            .ToList(),
        Pieces = doc.GetList("pieces")
            .OfType<Dictionary<string, object?>>()
            .Select(ToPieceRef)
            .ToList(),
        CreatedAt = doc.GetDateTime("createdAt"),
        RequiresKeyPickup = doc.GetBool("requiresKeyPickup"),
        KeyPickedUp = doc.GetBool("keyPickedUp"),
        KeyPickedUpByName = doc.GetString("keyPickedUpByName")
    };

    private static PracticePieceRef ToPieceRef(Dictionary<string, object?> fields) => new()
    {
        PieceId = fields.GetValueOrDefault("pieceId") as string ?? string.Empty,
        Title = fields.GetValueOrDefault("title") as string ?? string.Empty,
        Recordings = ToRecordings(fields)
    };

    /// <summary>
    /// 曲ごとの録音一覧を読み取る。新形式の <c>recordings</c> 配列があればそれを使い、無い場合は
    /// 1曲につき録音1件のみだった旧形式（<c>recordingUrl</c>・<c>featured</c>）から移行して読み込む。
    /// 移行時に発行したIDは、次回保存（<see cref="UpdatePiecesAsync"/>）時に新形式として永続化される。
    /// </summary>
    private static List<PracticeRecording> ToRecordings(Dictionary<string, object?> fields)
    {
        if (fields.GetValueOrDefault("recordings") is List<object?> recordingsRaw)
            return recordingsRaw.OfType<Dictionary<string, object?>>().Select(ToRecording).ToList();

        var legacyUrl = fields.GetValueOrDefault("recordingUrl") as string;
        if (string.IsNullOrEmpty(legacyUrl))
            return [];

        return [new PracticeRecording { Id = Guid.NewGuid().ToString("N"), Url = legacyUrl, IsFeatured = fields.GetValueOrDefault("featured") as bool? ?? false }];
    }

    private static PracticeRecording ToRecording(Dictionary<string, object?> fields) => new()
    {
        Id = fields.GetValueOrDefault("id") as string ?? string.Empty,
        Name = fields.GetValueOrDefault("name") as string ?? string.Empty,
        Url = fields.GetValueOrDefault("url") as string ?? string.Empty,
        IsFeatured = fields.GetValueOrDefault("featured") as bool? ?? false
    };
}
