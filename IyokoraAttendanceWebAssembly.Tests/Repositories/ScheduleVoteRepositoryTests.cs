using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Repositories;

public class ScheduleVoteRepositoryTests
{
    private static FirestoreDocument CreateVoteDoc(string id, string candidateId, string memberId, AttendanceStatus status, string groupId = "default") => new()
    {
        Id = id,
        Fields = new Dictionary<string, object?>
        {
            ["groupId"] = groupId,
            ["candidateId"] = candidateId,
            ["memberId"] = memberId,
            ["memberName"] = "テスト",
            ["status"] = status.ToString(),
            ["updatedAt"] = DateTime.UtcNow
        }
    };

    private static (ScheduleVoteRepository repository, Mock<IFirestoreClient> client) CreateRepository(IEnumerable<FirestoreDocument>? docs = null)
    {
        var list = (docs ?? []).ToList();
        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.QueryDocumentsAsync("scheduleVotes", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IReadOnlyDictionary<string, object?> filters, CancellationToken _) =>
                list.Where(d => filters.All(f => d.Fields.TryGetValue(f.Key, out var v) && Equals(v, f.Value))).ToList());
        clientMock
            .Setup(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientMock
            .Setup(c => c.DeleteDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new ScheduleVoteRepository(clientMock.Object), clientMock);
    }

    [Fact]
    public async Task 指定候補日の自団体分の投票のみ取得される()
    {
        var docs = new[]
        {
            CreateVoteDoc("v1", "c1", "m1", AttendanceStatus.Attending),
            CreateVoteDoc("v2", "c2", "m2", AttendanceStatus.Attending),
            CreateVoteDoc("v3", "c1", "m3", AttendanceStatus.Attending, groupId: "other")
        };
        var (repository, _) = CreateRepository(docs);

        var votes = await repository.GetForCandidateAsync("c1");

        Assert.Equal(["m1"], votes.Select(v => v.MemberId));
    }

    [Fact]
    public async Task 候補日の投票取得はコレクション全体を取得せずgroupIdと候補日IDで絞り込む()
    {
        var docs = new[] { CreateVoteDoc("v1", "c1", "m1", AttendanceStatus.Attending) };
        var (repository, client) = CreateRepository(docs);

        await repository.GetForCandidateAsync("c1");

        client.Verify(c => c.QueryDocumentsAsync(
            "scheduleVotes",
            It.Is<IReadOnlyDictionary<string, object?>>(f => (string)f["groupId"]! == FirebaseOptions.GroupId && (string)f["candidateId"]! == "c1"),
            It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.ListDocumentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task 候補日を指定して投票を削除すると該当する投票のみ削除される()
    {
        var docs = new[]
        {
            CreateVoteDoc("v1", "c1", "m1", AttendanceStatus.Attending),
            CreateVoteDoc("v2", "c2", "m2", AttendanceStatus.Attending)
        };
        var (repository, client) = CreateRepository(docs);

        await repository.DeleteForCandidateAsync("c1");

        client.Verify(c => c.DeleteDocumentAsync("scheduleVotes", "v1", It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.DeleteDocumentAsync("scheduleVotes", "v2", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task 参加意思を登録すると候補日IDとメンバーIDの複合キーで保存される()
    {
        var (repository, client) = CreateRepository();

        await repository.SetStatusAsync("c1", "member1", "山田 太郎", AttendanceStatus.Attending, DateTime.UtcNow);

        client.Verify(c => c.UpsertDocumentAsync(
            "scheduleVotes",
            ScheduleVote.BuildId("c1", "member1"),
            It.Is<Dictionary<string, object?>>(f =>
                (string)f["candidateId"]! == "c1" &&
                (string)f["memberId"]! == "member1" &&
                (string)f["memberName"]! == "山田 太郎" &&
                (string)f["status"]! == AttendanceStatus.Attending.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 参加意思はいつでも変更できる()
    {
        var (repository, client) = CreateRepository();

        await repository.SetStatusAsync("c1", "member1", "山田 太郎", AttendanceStatus.Attending, DateTime.UtcNow);
        await repository.SetStatusAsync("c1", "member1", "山田 太郎", AttendanceStatus.NotAttending, DateTime.UtcNow);

        client.Verify(c => c.UpsertDocumentAsync(
            "scheduleVotes",
            ScheduleVote.BuildId("c1", "member1"),
            It.Is<Dictionary<string, object?>>(f => (string)f["status"]! == AttendanceStatus.NotAttending.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 未回答の状態は未定として扱われる()
    {
        var docs = new[] { CreateVoteDoc("v1", "c1", "m1", AttendanceStatus.Undecided) };
        var (repository, _) = CreateRepository(docs);

        var votes = await repository.GetForCandidateAsync("c1");

        Assert.Equal(AttendanceStatus.Undecided, votes.Single().Status);
    }
}
