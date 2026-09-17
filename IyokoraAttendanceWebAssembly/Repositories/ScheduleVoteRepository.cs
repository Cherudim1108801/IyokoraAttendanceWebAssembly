using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>scheduleVotes</c> コレクションに対する <see cref="IScheduleVoteRepository"/> の実装。</summary>
public class ScheduleVoteRepository(IFirestoreClient client) : IScheduleVoteRepository
{
    private const string Collection = "scheduleVotes";

    /// <summary>
    /// サーバー側で絞り込んだ結果のみを取得する（コレクション全体は転送しない）。
    /// </summary>
    public async Task<List<ScheduleVote>> GetForCandidateAsync(string candidateId, CancellationToken ct = default)
    {
        var filters = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["candidateId"] = candidateId
        };
        var docs = await client.QueryDocumentsAsync(Collection, filters, ct);
        return docs.Select(ToVote).ToList();
    }

    public Task SetStatusAsync(string candidateId, string memberId, string memberName, AttendanceStatus status, DateTime updatedAt, CancellationToken ct = default)
    {
        var id = ScheduleVote.BuildId(candidateId, memberId);
        var fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["candidateId"] = candidateId,
            ["memberId"] = memberId,
            ["memberName"] = memberName,
            ["status"] = status.ToString(),
            ["updatedAt"] = updatedAt
        };
        return client.UpsertDocumentAsync(Collection, id, fields, ct);
    }

    public async Task DeleteForCandidateAsync(string candidateId, CancellationToken ct = default)
    {
        var votes = await GetForCandidateAsync(candidateId, ct);
        await Task.WhenAll(votes.Select(v => client.DeleteDocumentAsync(Collection, v.Id, ct)));
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
