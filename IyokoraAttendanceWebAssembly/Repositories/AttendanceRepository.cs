using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>attendances</c> コレクションに対する <see cref="IAttendanceRepository"/> の実装。</summary>
public class AttendanceRepository(IFirestoreClient client) : IAttendanceRepository
{
    private const string Collection = "attendances";

    /// <summary>
    /// サーバー側で絞り込んだ結果のみを取得する（コレクション全体は転送しない）。
    /// </summary>
    public async Task<List<Attendance>> GetForPracticeAsync(string practiceId, CancellationToken ct = default)
    {
        var filters = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["practiceId"] = practiceId
        };
        var docs = await client.QueryDocumentsAsync(Collection, filters, ct);
        return docs.Select(ToAttendance).ToList();
    }

    /// <summary>
    /// ドキュメントIDが <see cref="Attendance.BuildId"/> の複合キー規則で発行されていることを利用し、
    /// 練習全体を取得・絞り込みせず該当ドキュメントを直接1件取得する。
    /// </summary>
    public async Task<Attendance?> GetForMemberAsync(string practiceId, string memberId, CancellationToken ct = default)
    {
        var doc = await client.GetDocumentAsync(Collection, Attendance.BuildId(practiceId, memberId), ct);
        return doc is null ? null : ToAttendance(doc);
    }

    public Task SetStatusAsync(string practiceId, string memberId, string memberName, PartType part, AttendanceStatus status, DateTime updatedAt, CancellationToken ct = default)
    {
        var id = Attendance.BuildId(practiceId, memberId);
        var fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["practiceId"] = practiceId,
            ["memberId"] = memberId,
            ["memberName"] = memberName,
            ["part"] = part.ToString(),
            ["status"] = status.ToString(),
            ["updatedAt"] = updatedAt
        };
        return client.UpsertDocumentAsync(Collection, id, fields, ct);
    }

    private static Attendance ToAttendance(FirestoreDocument doc) => new()
    {
        Id = doc.Id,
        PracticeId = doc.GetString("practiceId"),
        MemberId = doc.GetString("memberId"),
        MemberName = doc.GetString("memberName"),
        Part = Enum.TryParse<PartType>(doc.GetString("part"), out var part) ? part : PartType.Soprano,
        Status = Enum.TryParse<AttendanceStatus>(doc.GetString("status"), out var status) ? status : AttendanceStatus.Undecided,
        UpdatedAt = doc.GetDateTime("updatedAt")
    };
}
