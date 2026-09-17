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

    private static (ScheduleCandidateService service, Mock<IScheduleCandidateRepository> repository, Mock<IScheduleVoteRepository> voteRepository) CreateService(IEnumerable<ScheduleCandidate>? candidates = null)
    {
        var repositoryMock = new Mock<IScheduleCandidateRepository>();
        repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((candidates ?? []).ToList());
        repositoryMock
            .Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var voteRepositoryMock = new Mock<IScheduleVoteRepository>();
        voteRepositoryMock
            .Setup(r => r.DeleteForCandidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new ScheduleCandidateService(repositoryMock.Object, voteRepositoryMock.Object), repositoryMock, voteRepositoryMock);
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
        var (service, _, _) = CreateService(candidates);

        var upcoming = await service.GetUpcomingAsync();

        Assert.Equal(["c2", "c1"], upcoming.Select(c => c.Id));
    }

    [Fact]
    public async Task 候補日を削除すると紐づく投票も削除される()
    {
        var (service, repository, voteRepository) = CreateService();

        await service.DeleteAsync("c1");

        voteRepository.Verify(r => r.DeleteForCandidateAsync("c1", It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.DeleteAsync("c1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
