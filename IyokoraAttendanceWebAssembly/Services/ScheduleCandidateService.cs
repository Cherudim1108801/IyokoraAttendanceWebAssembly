using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>Firestore の <c>scheduleCandidates</c> コレクションに対する日程投票の候補日の取得・作成・削除を担う。</summary>
public class ScheduleCandidateService(IFirestoreClient client)
{
    private const string Collection = "scheduleCandidates";

    /// <summary>今日以降の候補日を、日付の古い順に取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<ScheduleCandidate>> GetUpcomingAsync(CancellationToken ct = default)
    {
        var docs = await client.ListDocumentsAsync(Collection, ct);
        var today = DateTime.Today;
        return docs
            .Where(d => d.GetString("groupId") == FirebaseOptions.GroupId)
            .Select(ToCandidate)
            .Where(c => c.Date.Date >= today)
            .OrderBy(c => c.Date)
            .ToList();
    }

    /// <summary>新しい候補日を登録する。</summary>
    /// <param name="date">候補日（時刻情報は無視される）。</param>
    /// <param name="timeOfDay">候補日の時間帯区分（午前／午後／夜間）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>発行された候補日ID。</returns>
    public async Task<string> CreateAsync(DateTime date, TimeOfDay timeOfDay, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
        // 候補日は時刻を持たないカレンダー日付として扱う。DateTime.ToUniversalTime() による
        // タイムゾーン変換で日付がずれないよう、日付部分だけを UTC として保存する。
        var dateOnly = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["date"] = dateOnly,
            ["timeOfDay"] = timeOfDay.ToString(),
            ["createdAt"] = DateTime.UtcNow
        };
        await client.UpsertDocumentAsync(Collection, id, fields, ct);
        return id;
    }

    /// <summary>指定IDの候補日を削除する。</summary>
    /// <param name="candidateId">候補日ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
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
