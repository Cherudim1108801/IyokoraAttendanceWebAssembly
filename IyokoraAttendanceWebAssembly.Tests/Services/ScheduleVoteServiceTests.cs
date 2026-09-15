using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class ScheduleVoteServiceTests
{
    private static (ScheduleVoteService service, Mock<IScheduleVoteRepository> repository) CreateService()
    {
        var repositoryMock = new Mock<IScheduleVoteRepository>();
        repositoryMock
            .Setup(r => r.SetStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AttendanceStatus>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new ScheduleVoteService(repositoryMock.Object), repositoryMock);
    }

    [Fact]
    public async Task 投票一覧の取得はリポジトリに委譲される()
    {
        var (service, repository) = CreateService();
        repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ScheduleVote { Id = "v1", CandidateId = "c1", MemberId = "m1", MemberName = "テスト", Status = AttendanceStatus.Attending }]);

        var votes = await service.GetAllAsync();

        Assert.Equal(["m1"], votes.Select(v => v.MemberId));
    }

    [Fact]
    public async Task 参加意思を登録すると現在時刻とともにリポジトリへ渡される()
    {
        var (service, repository) = CreateService();

        await service.SetStatusAsync("c1", "member1", "山田 太郎", AttendanceStatus.Attending);

        repository.Verify(r => r.SetStatusAsync(
            "c1", "member1", "山田 太郎", AttendanceStatus.Attending,
            It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
