using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class PracticeServiceTests
{
    private static Practice CreatePractice(string id, DateTime date, List<PracticePieceRef>? pieces = null) => new()
    {
        Id = id,
        Date = date,
        Pieces = pieces ?? []
    };

    private static (PracticeService service, Mock<IPracticeRepository> repository) CreateService(IEnumerable<Practice>? practices = null, Practice? byIdResult = null)
    {
        var repositoryMock = new Mock<IPracticeRepository>();
        repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((practices ?? []).ToList());
        repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(byIdResult);
        repositoryMock
            .Setup(r => r.UpdatePiecesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<PracticePieceRef>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new PracticeService(repositoryMock.Object), repositoryMock);
    }

    [Fact]
    public async Task 今日以降の練習予定が日付の古い順で取得される()
    {
        var today = DateTime.Today;
        var practices = new[]
        {
            CreatePractice("p1", today.AddDays(10)),
            CreatePractice("p2", today.AddDays(3)),
            CreatePractice("p3", today.AddDays(-1))
        };
        var (service, _) = CreateService(practices);

        var upcoming = await service.GetUpcomingAsync();

        Assert.Equal(["p2", "p1"], upcoming.Select(p => p.Id));
    }

    [Fact]
    public async Task 今日より前の練習予定が日付の新しい順で取得される()
    {
        var today = DateTime.Today;
        var practices = new[]
        {
            CreatePractice("p1", today.AddDays(-10)),
            CreatePractice("p2", today.AddDays(-3)),
            CreatePractice("p3", today.AddDays(1))
        };
        var (service, _) = CreateService(practices);

        var past = await service.GetPastAsync();

        Assert.Equal(["p2", "p1"], past.Select(p => p.Id));
    }

    [Fact]
    public async Task 今日以降の練習予定が無い場合は次回の練習予定がnullになる()
    {
        var practices = new[] { CreatePractice("p1", DateTime.Today.AddDays(-1)) };
        var (service, _) = CreateService(practices);

        var next = await service.GetNextUpcomingAsync();

        Assert.Null(next);
    }

    [Fact]
    public async Task 鍵を受け取り済みにすると受け取ったメンバー名が渡される()
    {
        var (service, repository) = CreateService();

        await service.SetKeyPickedUpAsync("practice1", true, "山田 太郎");

        repository.Verify(r => r.SetKeyPickedUpAsync("practice1", true, "山田 太郎", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 鍵を未受け取りに戻すと受け取ったメンバー名の記録がクリアされる()
    {
        var (service, repository) = CreateService();

        await service.SetKeyPickedUpAsync("practice1", false, "山田 太郎");

        repository.Verify(r => r.SetKeyPickedUpAsync("practice1", false, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task 練習場所を更新すると指定した値と鍵の受け取り要否がそのままリポジトリへ渡される()
    {
        var (service, repository) = CreateService();

        await service.UpdatePlaceAsync("practice1", "市民会館", requiresKeyPickup: true);

        repository.Verify(r => r.UpdatePlaceAsync("practice1", "市民会館", true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
