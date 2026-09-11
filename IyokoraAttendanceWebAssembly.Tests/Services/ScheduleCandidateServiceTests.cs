using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class ScheduleCandidateServiceTests
{
    private static FirestoreDocument CreateCandidateDoc(string id, DateTime date, string groupId = "default", TimeOfDay? timeOfDay = null) => new()
    {
        Id = id,
        Fields = new Dictionary<string, object?>
        {
            ["groupId"] = groupId,
            ["date"] = date,
            ["timeOfDay"] = (timeOfDay ?? TimeOfDay.Morning).ToString(),
            ["createdAt"] = DateTime.UtcNow
        }
    };

    private static (ScheduleCandidateService service, Mock<IFirestoreClient> client) CreateService(IEnumerable<FirestoreDocument>? docs = null)
    {
        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.ListDocumentsAsync("scheduleCandidates", It.IsAny<CancellationToken>()))
            .ReturnsAsync((docs ?? []).ToList());
        clientMock
            .Setup(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientMock
            .Setup(c => c.DeleteDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new ScheduleCandidateService(clientMock.Object), clientMock);
    }

    [Fact]
    public async Task 今日以降の候補日が日付の古い順で取得される()
    {
        var today = DateTime.Today;
        var docs = new[]
        {
            CreateCandidateDoc("c1", today.AddDays(10)),
            CreateCandidateDoc("c2", today.AddDays(3)),
            CreateCandidateDoc("c3", today.AddDays(-1))
        };
        var (service, _) = CreateService(docs);

        var upcoming = await service.GetUpcomingAsync();

        Assert.Equal(["c2", "c1"], upcoming.Select(c => c.Id));
    }

    [Fact]
    public async Task 他団体の候補日は取得されない()
    {
        var today = DateTime.Today;
        var docs = new[]
        {
            CreateCandidateDoc("c1", today.AddDays(1), groupId: "other")
        };
        var (service, _) = CreateService(docs);

        var upcoming = await service.GetUpcomingAsync();

        Assert.Empty(upcoming);
    }

    [Fact]
    public async Task 候補日を登録すると日付が時刻無しのUTCとして保存される()
    {
        var (service, client) = CreateService();

        await service.CreateAsync(new DateTime(2026, 5, 10, 13, 45, 0), TimeOfDay.Afternoon);

        client.Verify(c => c.UpsertDocumentAsync(
            "scheduleCandidates",
            It.IsAny<string>(),
            It.Is<Dictionary<string, object?>>(f =>
                (DateTime)f["date"]! == new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 候補日を登録すると時間帯区分が保存される()
    {
        var (service, client) = CreateService();

        await service.CreateAsync(new DateTime(2026, 5, 10), TimeOfDay.Evening);

        client.Verify(c => c.UpsertDocumentAsync(
            "scheduleCandidates",
            It.IsAny<string>(),
            It.Is<Dictionary<string, object?>>(f => (string)f["timeOfDay"]! == TimeOfDay.Evening.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 候補日を取得すると時間帯区分が復元される()
    {
        var today = DateTime.Today;
        var docs = new[] { CreateCandidateDoc("c1", today.AddDays(1), timeOfDay: TimeOfDay.Evening) };
        var (service, _) = CreateService(docs);

        var upcoming = await service.GetUpcomingAsync();

        Assert.Equal(TimeOfDay.Evening, upcoming.Single().TimeOfDay);
    }

    [Fact]
    public async Task 時間帯区分が未設定の候補日は午前として復元される()
    {
        var today = DateTime.Today;
        var doc = new FirestoreDocument
        {
            Id = "c1",
            Fields = new Dictionary<string, object?>
            {
                ["groupId"] = "default",
                ["date"] = today.AddDays(1),
                ["createdAt"] = DateTime.UtcNow
            }
        };
        var (service, _) = CreateService([doc]);

        var upcoming = await service.GetUpcomingAsync();

        Assert.Equal(TimeOfDay.Morning, upcoming.Single().TimeOfDay);
    }

    [Fact]
    public async Task 候補日を削除すると該当IDで削除が呼ばれる()
    {
        var (service, client) = CreateService();

        await service.DeleteAsync("c1");

        client.Verify(c => c.DeleteDocumentAsync("scheduleCandidates", "c1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
