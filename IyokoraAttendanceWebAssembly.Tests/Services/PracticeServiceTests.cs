using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class PracticeServiceTests
{
    private static FirestoreDocument CreatePracticeDoc(string id, DateTime date, string groupId = "default", List<object?>? pieces = null, string startTime = "", string endTime = "", List<object?>? timeline = null) => new()
    {
        Id = id,
        Fields = new Dictionary<string, object?>
        {
            ["groupId"] = groupId,
            ["date"] = date,
            ["title"] = string.Empty,
            ["place"] = string.Empty,
            ["startTime"] = startTime,
            ["endTime"] = endTime,
            ["timeline"] = timeline ?? new List<object?>(),
            ["pieces"] = pieces ?? new List<object?>(),
            ["createdAt"] = DateTime.UtcNow,
            ["requiresKeyPickup"] = false,
            ["keyPickedUp"] = false
        }
    };

    private static (PracticeService service, Mock<IFirestoreClient> client) CreateService(IEnumerable<FirestoreDocument>? docs = null, FirestoreDocument? byIdResult = null)
    {
        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.ListDocumentsAsync("practices", It.IsAny<CancellationToken>()))
            .ReturnsAsync((docs ?? []).ToList());
        clientMock
            .Setup(c => c.GetDocumentAsync("practices", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(byIdResult);
        clientMock
            .Setup(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientMock
            .Setup(c => c.DeleteDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new PracticeService(clientMock.Object), clientMock);
    }

    [Fact]
    public async Task 今日以降の練習予定が日付の古い順で取得される()
    {
        var today = DateTime.Today;
        var docs = new[]
        {
            CreatePracticeDoc("p1", today.AddDays(10)),
            CreatePracticeDoc("p2", today.AddDays(3)),
            CreatePracticeDoc("p3", today.AddDays(-1))
        };
        var (service, _) = CreateService(docs);

        var upcoming = await service.GetUpcomingAsync();

        Assert.Equal(["p2", "p1"], upcoming.Select(p => p.Id));
    }

    [Fact]
    public async Task 今日より前の練習予定が日付の新しい順で取得される()
    {
        var today = DateTime.Today;
        var docs = new[]
        {
            CreatePracticeDoc("p1", today.AddDays(-10)),
            CreatePracticeDoc("p2", today.AddDays(-3)),
            CreatePracticeDoc("p3", today.AddDays(1))
        };
        var (service, _) = CreateService(docs);

        var past = await service.GetPastAsync();

        Assert.Equal(["p2", "p1"], past.Select(p => p.Id));
    }

    [Fact]
    public async Task 今日以降の練習予定が無い場合は次回の練習予定がnullになる()
    {
        var docs = new[] { CreatePracticeDoc("p1", DateTime.Today.AddDays(-1)) };
        var (service, _) = CreateService(docs);

        var next = await service.GetNextUpcomingAsync();

        Assert.Null(next);
    }

    [Fact]
    public async Task 練習予定を登録すると日付が時刻無しのUTCとして保存される()
    {
        var (service, client) = CreateService();
        var date = new DateTime(2026, 5, 3, 15, 30, 0);

        var id = await service.CreateAsync(date, "定期練習", "市民会館", "18:00", "20:00", [], [], requiresKeyPickup: true);

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            id,
            It.Is<Dictionary<string, object?>>(f =>
                (DateTime)f["date"]! == new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc) &&
                (string)f["startTime"]! == "18:00" &&
                (string)f["endTime"]! == "20:00" &&
                (bool)f["requiresKeyPickup"]! == true &&
                (bool)f["keyPickedUp"]! == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 練習予定を登録するとタイムスケジュールの各項目が保存される()
    {
        var (service, client) = CreateService();
        var timeline = new List<PracticeTimelineItem>
        {
            new() { StartTime = "18:00", EndTime = "18:10", Content = "準備" },
            new() { StartTime = "18:10", EndTime = "18:30", Content = "基礎合奏" }
        };

        var id = await service.CreateAsync(DateTime.Today, "定期練習", "市民会館", "18:00", "20:00", timeline, [], requiresKeyPickup: false);

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            id,
            It.Is<Dictionary<string, object?>>(f =>
                ((List<object?>)f["timeline"]!).Cast<Dictionary<string, object?>>().Select(t => (string)t["content"]!).SequenceEqual(new[] { "準備", "基礎合奏" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 練習予定を取得すると開始終了時刻とタイムスケジュールが復元される()
    {
        var timeline = new List<object?>
        {
            new Dictionary<string, object?> { ["startTime"] = "18:00", ["endTime"] = "18:10", ["content"] = "準備" },
            new Dictionary<string, object?> { ["startTime"] = "18:10", ["endTime"] = "18:30", ["content"] = "基礎合奏" }
        };
        var doc = CreatePracticeDoc("practice1", DateTime.Today, startTime: "18:00", endTime: "20:00", timeline: timeline);
        var (service, _) = CreateService(byIdResult: doc);

        var practice = await service.GetByIdAsync("practice1");

        Assert.NotNull(practice);
        Assert.Equal("18:00", practice!.StartTime);
        Assert.Equal("20:00", practice.EndTime);
        Assert.Equal(["準備", "基礎合奏"], practice.TimelineItems.Select(i => i.Content));
        Assert.Equal("18:10", practice.TimelineItems[1].StartTime);
    }

    [Fact]
    public async Task 開始終了時刻もタイムスケジュールも未設定の練習予定は空として復元される()
    {
        var doc = CreatePracticeDoc("practice1", DateTime.Today);
        var (service, _) = CreateService(byIdResult: doc);

        var practice = await service.GetByIdAsync("practice1");

        Assert.NotNull(practice);
        Assert.Equal(string.Empty, practice!.StartTime);
        Assert.Equal(string.Empty, practice.EndTime);
        Assert.Empty(practice.TimelineItems);
        Assert.Equal(string.Empty, practice.TimeRangeSummary);
    }

    [Fact]
    public async Task タイムスケジュールを更新すると開始終了時刻と項目が上書きされる()
    {
        var (service, client) = CreateService();
        var timeline = new List<PracticeTimelineItem> { new() { StartTime = "19:00", EndTime = "19:20", Content = "パート練習" } };

        await service.UpdateScheduleAsync("practice1", "19:00", "21:00", timeline);

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "practice1",
            It.Is<Dictionary<string, object?>>(f =>
                (string)f["startTime"]! == "19:00" &&
                (string)f["endTime"]! == "21:00" &&
                ((List<object?>)f["timeline"]!).Cast<Dictionary<string, object?>>().Single()["content"] as string == "パート練習"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 練習予定を削除すると該当IDで削除が呼ばれる()
    {
        var (service, client) = CreateService();

        await service.DeleteAsync("practice1");

        client.Verify(c => c.DeleteDocumentAsync("practices", "practice1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 鍵を受け取り済みにすると受け取ったメンバー名が保存される()
    {
        var (service, client) = CreateService();

        await service.SetKeyPickedUpAsync("practice1", true, "山田 太郎");

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "practice1",
            It.Is<Dictionary<string, object?>>(f => (bool)f["keyPickedUp"]! == true && (string)f["keyPickedUpByName"]! == "山田 太郎"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 鍵を未受け取りに戻すと受け取ったメンバー名の記録がクリアされる()
    {
        var (service, client) = CreateService();

        await service.SetKeyPickedUpAsync("practice1", false, "山田 太郎");

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "practice1",
            It.Is<Dictionary<string, object?>>(f => (bool)f["keyPickedUp"]! == false && f["keyPickedUpByName"] == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 演奏予定曲を更新すると指定した曲一覧で保存される()
    {
        var (service, client) = CreateService();
        var pieces = new List<PracticePieceRef>
        {
            new() { PieceId = "piece1", Title = "曲A" },
            new() { PieceId = "piece2", Title = "曲B", RecordingUrl = "https://example.com", IsFeatured = true }
        };

        await service.UpdatePiecesAsync("practice1", pieces);

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "practice1",
            It.Is<Dictionary<string, object?>>(f =>
                ((List<object?>)f["pieces"]!).Cast<Dictionary<string, object?>>().Select(p => (string)p["pieceId"]!).SequenceEqual(new[] { "piece1", "piece2" }) &&
                ((List<object?>)f["pieces"]!).Cast<Dictionary<string, object?>>().First(p => (string)p["pieceId"]! == "piece2")["recordingUrl"] as string == "https://example.com" &&
                (bool)((List<object?>)f["pieces"]!).Cast<Dictionary<string, object?>>().First(p => (string)p["pieceId"]! == "piece2")["featured"]! == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 存在しない練習予定に録音リンクを設定しても保存は行われない()
    {
        var (service, client) = CreateService(byIdResult: null);

        await service.SetPieceRecordingUrlAsync("missing", "piece1", "https://example.com");

        client.Verify(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task 録音リンクを設定すると対象の曲だけが更新される()
    {
        var pieces = new List<object?>
        {
            new Dictionary<string, object?> { ["pieceId"] = "piece1", ["title"] = "曲A", ["recordingUrl"] = null, ["featured"] = false },
            new Dictionary<string, object?> { ["pieceId"] = "piece2", ["title"] = "曲B", ["recordingUrl"] = null, ["featured"] = false }
        };
        var doc = CreatePracticeDoc("practice1", DateTime.Today, pieces: pieces);
        var (service, client) = CreateService(byIdResult: doc);

        await service.SetPieceRecordingUrlAsync("practice1", "piece1", "https://example.com/rec");

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "practice1",
            It.Is<Dictionary<string, object?>>(f =>
                ((List<object?>)f["pieces"]!).Cast<Dictionary<string, object?>>().First(p => (string)p["pieceId"]! == "piece1")["recordingUrl"] as string == "https://example.com/rec" &&
                ((List<object?>)f["pieces"]!).Cast<Dictionary<string, object?>>().First(p => (string)p["pieceId"]! == "piece2")["recordingUrl"] == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
