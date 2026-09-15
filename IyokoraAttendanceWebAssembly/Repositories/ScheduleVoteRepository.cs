using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>scheduleVotes</c> コレクションに対する <see cref="IScheduleVoteRepository"/> の実装。</summary>
public class ScheduleVoteRepository(IFirestoreClient client) : IScheduleVoteRepository
{
    private const string Collection = "scheduleVotes";

    public async Task<List<ScheduleVote>> GetAllAsync(CancellationToken ct = default)
    {
        var docs = await client.ListDocumentsAsync(Collection, ct);
        return docs
            .Where(d => d.GetString("groupId") == FirebaseOptions.GroupId)
            .Select(ToVote)
            .ToList();
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
