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
        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.ListDocumentsAsync("scheduleVotes", It.IsAny<CancellationToken>()))
            .ReturnsAsync((docs ?? []).ToList());
        clientMock
            .Setup(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new ScheduleVoteRepository(clientMock.Object), clientMock);
    }

    [Fact]
    public async Task 自団体分の投票のみ取得される()
    {
        var docs = new[]
        {
            CreateVoteDoc("v1", "c1", "m1", AttendanceStatus.Attending),
            CreateVoteDoc("v2", "c1", "m2", AttendanceStatus.Attending, groupId: "other")
        };
        var (repository, _) = CreateRepository(docs);

        var votes = await repository.GetAllAsync();

        Assert.Equal(["m1"], votes.Select(v => v.MemberId));
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

        var votes = await repository.GetAllAsync();

        Assert.Equal(AttendanceStatus.Undecided, votes.Single().Status);
    }
}
