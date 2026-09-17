using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>scheduleCandidates</c> コレクションに対する <see cref="IScheduleCandidateRepository"/> の実装。</summary>
public class ScheduleCandidateRepository(IFirestoreClient client) : IScheduleCandidateRepository
{
    private const string Collection = "scheduleCandidates";

    /// <summary>
    /// サーバー側で絞り込んだ結果のみを取得する（コレクション全体は転送しない）。
    /// </summary>
    public async Task<List<ScheduleCandidate>> GetAllAsync(CancellationToken ct = default)
    {
        var filters = new Dictionary<string, object?> { ["groupId"] = FirebaseOptions.GroupId };
        var docs = await client.QueryDocumentsAsync(Collection, filters, ct);
        return docs.Select(ToCandidate).ToList();
    }

    public Task CreateAsync(string candidateId, DateTime date, TimeOfDay timeOfDay, DateTime createdAt, CancellationToken ct = default)
    {
        // 候補日は時刻を持たないカレンダー日付として扱う。DateTime.ToUniversalTime() による
        // タイムゾーン変換で日付がずれないよう、日付部分だけを UTC として保存する。
        var dateOnly = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["date"] = dateOnly,
            ["timeOfDay"] = timeOfDay.ToString(),
            ["createdAt"] = createdAt
        };
        return client.UpsertDocumentAsync(Collection, candidateId, fields, ct);
    }

    public Task DeleteAsync(string candidateId, CancellationToken ct = default) =>
        client.DeleteDocumentAsync(Collection, candidateId, ct);

    private static ScheduleCandidate ToCandidate(FirestoreDocument doc) => new()
    {
        Id = doc.Id,
        Date = doc.GetDateTime("date"),
        TimeOfDay = Enum.TryParse<TimeOfDay>(doc.GetString("timeOfDay"), out var timeOfDay) ? timeOfDay : TimeOfDay.Morning,
        CreatedAt = doc.GetDateTime("createdAt")
    };
}
