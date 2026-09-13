using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>Firestore の <c>attendances</c> コレクションに対する出欠情報の取得・更新を担う。</summary>
public class AttendanceService(IFirestoreClient client)
{
    private const string Collection = "attendances";

    /// <summary>指定練習に対する全メンバーの出欠を取得する。コレクション全体ではなく、サーバー側で絞り込んだ結果のみを取得する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
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
    /// 指定練習における、指定メンバー1人分の出欠を取得する。未回答の場合は null。
    /// ドキュメントIDが <see cref="Attendance.BuildId"/> の複合キー規則で発行されていることを利用し、
    /// 練習全体を取得・絞り込みせず該当ドキュメントを直接1件取得する。
    /// </summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="memberId">メンバーID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<Attendance?> GetForMemberAsync(string practiceId, string memberId, CancellationToken ct = default)
    {
        var doc = await client.GetDocumentAsync(Collection, Attendance.BuildId(practiceId, memberId), ct);
        return doc is null ? null : ToAttendance(doc);
    }

    /// <summary>指定練習・指定メンバーの出欠状態を登録または更新する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="memberId">メンバーID。</param>
    /// <param name="memberName">メンバー名（非正規化して保存）。</param>
    /// <param name="part">所属パート（非正規化して保存）。</param>
    /// <param name="status">出欠状態。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task SetStatusAsync(string practiceId, string memberId, string memberName, PartType part, AttendanceStatus status, CancellationToken ct = default)
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
            ["updatedAt"] = DateTime.UtcNow
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
