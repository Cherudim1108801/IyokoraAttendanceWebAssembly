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
    public async Task 指定候補日の投票取得はリポジトリに委譲される()
    {
        var (service, repository) = CreateService();
        repository
            .Setup(r => r.GetForCandidateAsync("c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ScheduleVote { Id = "v1", CandidateId = "c1", MemberId = "m1", MemberName = "テスト", Status = AttendanceStatus.Attending }]);

        var votes = await service.GetForCandidateAsync("c1");

        Assert.Equal(["m1"], votes.Select(v => v.MemberId));
    }

    [Fact]
    public async Task 複数候補日分の投票取得は候補日ごとの結果がまとめて返される()
    {
        var (service, repository) = CreateService();
        repository
            .Setup(r => r.GetForCandidateAsync("c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ScheduleVote { Id = "v1", CandidateId = "c1", MemberId = "m1", MemberName = "テスト1", Status = AttendanceStatus.Attending }]);
        repository
            .Setup(r => r.GetForCandidateAsync("c2", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ScheduleVote { Id = "v2", CandidateId = "c2", MemberId = "m2", MemberName = "テスト2", Status = AttendanceStatus.Attending }]);

        var votes = await service.GetForCandidatesAsync(["c1", "c2"]);

        Assert.Equal(["m1", "m2"], votes.Select(v => v.MemberId));
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
