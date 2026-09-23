using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Repositories;

public class PracticeRepositoryTests
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

    private static (PracticeRepository repository, Mock<IFirestoreClient> client) CreateRepository(IEnumerable<FirestoreDocument>? docs = null, FirestoreDocument? byIdResult = null)
    {
        var list = (docs ?? []).ToList();
        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.QueryDocumentsAsync("practices", It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IReadOnlyDictionary<string, object?> filters, CancellationToken _) =>
                list.Where(d => filters.All(f => d.Fields.TryGetValue(f.Key, out var v) && Equals(v, f.Value))).ToList());
        clientMock
            .Setup(c => c.GetDocumentAsync("practices", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(byIdResult);
        clientMock
            .Setup(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        clientMock
            .Setup(c => c.DeleteDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new PracticeRepository(clientMock.Object), clientMock);
    }

    [Fact]
    public async Task 他団体の練習予定は一覧から除外される()
    {
        var docs = new[]
        {
            CreatePracticeDoc("p1", DateTime.Today.AddDays(1), groupId: FirebaseOptions.GroupId),
            CreatePracticeDoc("p2", DateTime.Today.AddDays(2), groupId: "other")
        };
        var (repository, _) = CreateRepository(docs);

        var practices = await repository.GetAllAsync();

        Assert.Equal(["p1"], practices.Select(p => p.Id));
    }

    [Fact]
    public async Task 練習予定一覧取得はコレクション全体を取得せずgroupIdで絞り込む()
    {
        var docs = new[] { CreatePracticeDoc("p1", DateTime.Today.AddDays(1), groupId: FirebaseOptions.GroupId) };
        var (repository, client) = CreateRepository(docs);

        await repository.GetAllAsync();

        client.Verify(c => c.QueryDocumentsAsync(
            "practices",
            It.Is<IReadOnlyDictionary<string, object?>>(f => (string)f["groupId"]! == FirebaseOptions.GroupId),
            It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.ListDocumentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task 練習予定を登録すると日付が時刻無しのUTCとして保存される()
    {
        var (repository, client) = CreateRepository();
        var date = new DateTime(2026, 5, 3, 15, 30, 0);

        await repository.CreateAsync("p1", date, "定期練習", "市民会館", "18:00", "20:00", [], [], requiresKeyPickup: true, DateTime.UtcNow);

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "p1",
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
        var (repository, client) = CreateRepository();
        var timeline = new List<PracticeTimelineItem>
        {
            new() { StartTime = "18:00", EndTime = "18:10", Content = "準備" },
            new() { StartTime = "18:10", EndTime = "18:30", Content = "基礎合奏" }
        };

        await repository.CreateAsync("p1", DateTime.Today, "定期練習", "市民会館", "18:00", "20:00", timeline, [], requiresKeyPickup: false, DateTime.UtcNow);

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "p1",
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
        var (repository, _) = CreateRepository(byIdResult: doc);

        var practice = await repository.GetByIdAsync("practice1");

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
        var (repository, _) = CreateRepository(byIdResult: doc);

        var practice = await repository.GetByIdAsync("practice1");

        Assert.NotNull(practice);
        Assert.Equal(string.Empty, practice!.StartTime);
        Assert.Equal(string.Empty, practice.EndTime);
        Assert.Empty(practice.TimelineItems);
        Assert.Equal(string.Empty, practice.TimeRangeSummary);
    }

    [Fact]
    public async Task タイムスケジュールを更新すると開始終了時刻と項目が上書きされる()
    {
        var (repository, client) = CreateRepository();
        var timeline = new List<PracticeTimelineItem> { new() { StartTime = "19:00", EndTime = "19:20", Content = "パート練習" } };

        await repository.UpdateScheduleAsync("practice1", "19:00", "21:00", timeline);

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
        var (repository, client) = CreateRepository();

        await repository.DeleteAsync("practice1");

        client.Verify(c => c.DeleteDocumentAsync("practices", "practice1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 鍵を受け取り済みにすると受け取ったメンバー名が保存される()
    {
        var (repository, client) = CreateRepository();

        await repository.SetKeyPickedUpAsync("practice1", true, "山田 太郎");

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "practice1",
            It.Is<Dictionary<string, object?>>(f => (bool)f["keyPickedUp"]! == true && (string)f["keyPickedUpByName"]! == "山田 太郎"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 鍵を未受け取りに戻すと受け取ったメンバー名の記録がクリアされる()
    {
        var (repository, client) = CreateRepository();

        await repository.SetKeyPickedUpAsync("practice1", false, null);

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "practice1",
            It.Is<Dictionary<string, object?>>(f => (bool)f["keyPickedUp"]! == false && f["keyPickedUpByName"] == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 演奏予定曲を更新すると指定した曲一覧で保存される()
    {
        var (repository, client) = CreateRepository();
        var pieces = new List<PracticePieceRef>
        {
            new() { PieceId = "piece1", Title = "曲A" },
            new() { PieceId = "piece2", Title = "曲B", Recordings = [new() { Id = "rec1", Url = "https://example.com", IsFeatured = true }] }
        };

        await repository.UpdatePiecesAsync("practice1", pieces);

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "practice1",
            It.Is<Dictionary<string, object?>>(f =>
                ((List<object?>)f["pieces"]!).Cast<Dictionary<string, object?>>().Select(p => (string)p["pieceId"]!).SequenceEqual(new[] { "piece1", "piece2" }) &&
                GetRecordings(f, "piece2").Single()["url"] as string == "https://example.com" &&
                (bool)GetRecordings(f, "piece2").Single()["featured"]! == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 演奏予定曲を更新すると1曲に複数の録音を保存できる()
    {
        var (repository, client) = CreateRepository();
        var pieces = new List<PracticePieceRef>
        {
            new() { PieceId = "piece1", Title = "曲A", Recordings = [
                new() { Id = "rec1", Url = "https://example.com/1", IsFeatured = false },
                new() { Id = "rec2", Url = "https://example.com/2", IsFeatured = true }
            ] }
        };

        await repository.UpdatePiecesAsync("practice1", pieces);

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "practice1",
            It.Is<Dictionary<string, object?>>(f => GetRecordings(f, "piece1").Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 演奏予定曲を更新すると録音の名前も保存される()
    {
        var (repository, client) = CreateRepository();
        var pieces = new List<PracticePieceRef>
        {
            new() { PieceId = "piece1", Title = "曲A", Recordings = [new() { Id = "rec1", Name = "本番前通し", Url = "https://example.com", IsFeatured = false }] }
        };

        await repository.UpdatePiecesAsync("practice1", pieces);

        client.Verify(c => c.UpsertDocumentAsync(
            "practices",
            "practice1",
            It.Is<Dictionary<string, object?>>(f => GetRecordings(f, "piece1").Single()["name"] as string == "本番前通し"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 保存した録音の名前を読み込み時に復元できる()
    {
        var pieces = new List<object?>
        {
            new Dictionary<string, object?>
            {
                ["pieceId"] = "piece1",
                ["title"] = "曲A",
                ["recordings"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["id"] = "rec1", ["name"] = "本番前通し", ["url"] = "https://example.com", ["featured"] = false }
                }
            }
        };
        var doc = CreatePracticeDoc("p1", DateTime.Today, groupId: FirebaseOptions.GroupId, pieces: pieces);
        var (repository, _) = CreateRepository([doc]);

        var practices = await repository.GetAllAsync();

        var recording = Assert.Single(Assert.Single(practices).Pieces.Single().Recordings);
        Assert.Equal("本番前通し", recording.Name);
    }

    [Fact]
    public async Task 旧形式の録音URLは読み込み時に録音1件として移行される()
    {
        var pieces = new List<object?>
        {
            new Dictionary<string, object?> { ["pieceId"] = "piece1", ["title"] = "曲A", ["recordingUrl"] = "https://example.com/legacy", ["featured"] = true }
        };
        var doc = CreatePracticeDoc("p1", DateTime.Today, groupId: FirebaseOptions.GroupId, pieces: pieces);
        var (repository, _) = CreateRepository([doc]);

        var practices = await repository.GetAllAsync();

        var recording = Assert.Single(Assert.Single(practices).Pieces.Single().Recordings);
        Assert.Equal("https://example.com/legacy", recording.Url);
        Assert.True(recording.IsFeatured);
    }

    private static List<Dictionary<string, object?>> GetRecordings(Dictionary<string, object?> fields, string pieceId) =>
        ((List<object?>)((List<object?>)fields["pieces"]!)
            .Cast<Dictionary<string, object?>>()
            .First(p => (string)p["pieceId"]! == pieceId)["recordings"]!)
        .Cast<Dictionary<string, object?>>()
        .ToList();
}
