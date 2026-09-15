using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Repositories;

public class ScheduleCandidateRepositoryTests
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

    private static (ScheduleCandidateRepository repository, Mock<IFirestoreClient> client) CreateRepository(IEnumerable<FirestoreDocument>? docs = null)
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

        return (new ScheduleCandidateRepository(clientMock.Object), clientMock);
    }

    [Fact]
    public async Task 他団体の候補日は取得されない()
    {
        var docs = new[] { CreateCandidateDoc("c1", DateTime.Today.AddDays(1), groupId: "other") };
        var (repository, _) = CreateRepository(docs);

        var all = await repository.GetAllAsync();

        Assert.Empty(all);
    }

    [Fact]
    public async Task 候補日を登録すると日付が時刻無しのUTCとして保存される()
    {
        var (repository, client) = CreateRepository();

        await repository.CreateAsync("c1", new DateTime(2026, 5, 10, 13, 45, 0), TimeOfDay.Afternoon, DateTime.UtcNow);

        client.Verify(c => c.UpsertDocumentAsync(
            "scheduleCandidates",
            "c1",
            It.Is<Dictionary<string, object?>>(f =>
                (DateTime)f["date"]! == new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 候補日を登録すると時間帯区分が保存される()
    {
        var (repository, client) = CreateRepository();

        await repository.CreateAsync("c1", new DateTime(2026, 5, 10), TimeOfDay.Evening, DateTime.UtcNow);

        client.Verify(c => c.UpsertDocumentAsync(
            "scheduleCandidates",
            "c1",
            It.Is<Dictionary<string, object?>>(f => (string)f["timeOfDay"]! == TimeOfDay.Evening.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 候補日を取得すると時間帯区分が復元される()
    {
        var docs = new[] { CreateCandidateDoc("c1", DateTime.Today.AddDays(1), timeOfDay: TimeOfDay.Evening) };
        var (repository, _) = CreateRepository(docs);

        var all = await repository.GetAllAsync();

        Assert.Equal(TimeOfDay.Evening, all.Single().TimeOfDay);
    }

    [Fact]
    public async Task 時間帯区分が未設定の候補日は午前として復元される()
    {
        var doc = new FirestoreDocument
        {
            Id = "c1",
            Fields = new Dictionary<string, object?>
            {
                ["groupId"] = "default",
                ["date"] = DateTime.Today.AddDays(1),
                ["createdAt"] = DateTime.UtcNow
            }
        };
        var (repository, _) = CreateRepository([doc]);

        var all = await repository.GetAllAsync();

        Assert.Equal(TimeOfDay.Morning, all.Single().TimeOfDay);
    }

    [Fact]
    public async Task 候補日を削除すると該当IDで削除が呼ばれる()
    {
        var (repository, client) = CreateRepository();

        await repository.DeleteAsync("c1");

        client.Verify(c => c.DeleteDocumentAsync("scheduleCandidates", "c1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
