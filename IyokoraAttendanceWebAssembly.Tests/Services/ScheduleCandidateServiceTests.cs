using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class ScheduleCandidateServiceTests
{
    private static ScheduleCandidate CreateCandidate(string id, DateTime date, TimeOfDay timeOfDay = TimeOfDay.Morning) => new()
    {
        Id = id,
        Date = date,
        TimeOfDay = timeOfDay
    };

    private static (ScheduleCandidateService service, Mock<IScheduleCandidateRepository> repository) CreateService(IEnumerable<ScheduleCandidate>? candidates = null)
    {
        var repositoryMock = new Mock<IScheduleCandidateRepository>();
        repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((candidates ?? []).ToList());

        return (new ScheduleCandidateService(repositoryMock.Object), repositoryMock);
    }

    [Fact]
    public async Task 今日以降の候補日が日付の古い順で取得される()
    {
        var today = DateTime.Today;
        var candidates = new[]
        {
            CreateCandidate("c1", today.AddDays(10)),
            CreateCandidate("c2", today.AddDays(3)),
            CreateCandidate("c3", today.AddDays(-1))
        };
        var (service, _) = CreateService(candidates);

        var upcoming = await service.GetUpcomingAsync();

        Assert.Equal(["c2", "c1"], upcoming.Select(c => c.Id));
    }
}
