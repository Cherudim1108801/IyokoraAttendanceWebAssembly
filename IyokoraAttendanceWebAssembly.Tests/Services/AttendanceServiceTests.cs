using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class AttendanceServiceTests
{
    private static (AttendanceService service, Mock<IAttendanceRepository> repository) CreateService()
    {
        var repositoryMock = new Mock<IAttendanceRepository>();
        repositoryMock
            .Setup(r => r.SetStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PartType>(), It.IsAny<AttendanceStatus>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new AttendanceService(repositoryMock.Object), repositoryMock);
    }

    [Fact]
    public async Task 出欠取得は練習IDでリポジトリに委譲される()
    {
        var (service, repository) = CreateService();
        repository
            .Setup(r => r.GetForPracticeAsync("practice1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Attendance { Id = "a1", PracticeId = "practice1", MemberId = "m1", MemberName = "テスト", Part = PartType.Soprano, Status = AttendanceStatus.Attending }]);

        var attendances = await service.GetForPracticeAsync("practice1");

        Assert.Equal(["m1"], attendances.Select(a => a.MemberId));
    }

    [Fact]
    public async Task 出欠を登録すると現在時刻とともにリポジトリへ渡される()
    {
        var (service, repository) = CreateService();

        await service.SetStatusAsync("practice1", "member1", "山田 太郎", PartType.Alto, AttendanceStatus.Attending);

        repository.Verify(r => r.SetStatusAsync(
            "practice1", "member1", "山田 太郎", PartType.Alto, AttendanceStatus.Attending,
            It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
