using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>Firestore の <c>scheduleVotes</c> コレクションに対する日程投票の参加意思の取得・更新を担う。</summary>
public class ScheduleVoteService(IFirestoreClient client)
{
    private const string Collection = "scheduleVotes";

    /// <summary>登録されている全候補日分の投票を取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<ScheduleVote>> GetAllAsync(CancellationToken ct = default)
    {
        var docs = await client.ListDocumentsAsync(Collection, ct);
        return docs
            .Where(d => d.GetString("groupId") == FirebaseOptions.GroupId)
            .Select(ToVote)
            .ToList();
    }

    /// <summary>指定候補日への参加意思を登録・変更する。</summary>
    /// <param name="candidateId">候補日ID。</param>
    /// <param name="memberId">投票するメンバーのID。</param>
    /// <param name="memberName">投票するメンバーの表示名。</param>
    /// <param name="status">参加意思。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task SetStatusAsync(string candidateId, string memberId, string memberName, AttendanceStatus status, CancellationToken ct = default)
    {
        var id = ScheduleVote.BuildId(candidateId, memberId);
        var fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["candidateId"] = candidateId,
            ["memberId"] = memberId,
            ["memberName"] = memberName,
            ["status"] = status.ToString(),
            ["updatedAt"] = DateTime.UtcNow
        };
        return client.UpsertDocumentAsync(Collection, id, fields, ct);
    }

    private static ScheduleVote ToVote(FirestoreDocument doc) => new()
    {
        Id = doc.Id,
        CandidateId = doc.GetString("candidateId"),
        MemberId = doc.GetString("memberId"),
        MemberName = doc.GetString("memberName"),
        Status = Enum.TryParse<AttendanceStatus>(doc.GetString("status"), out var status) ? status : AttendanceStatus.Undecided,
        UpdatedAt = doc.GetDateTime("updatedAt")
    };
}
