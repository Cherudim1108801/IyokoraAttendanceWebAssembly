using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class AttendanceServiceTests
{
    private static FirestoreDocument CreateAttendanceDoc(string id, string practiceId, string memberId, AttendanceStatus status, string groupId = "default") => new()
    {
        Id = id,
        Fields = new Dictionary<string, object?>
        {
            ["groupId"] = groupId,
            ["practiceId"] = practiceId,
            ["memberId"] = memberId,
            ["memberName"] = "テスト",
            ["part"] = PartType.Soprano.ToString(),
            ["status"] = status.ToString(),
            ["updatedAt"] = DateTime.UtcNow
        }
    };

    private static (AttendanceService service, Mock<IFirestoreClient> client) CreateService(IEnumerable<FirestoreDocument>? docs = null)
    {
        var clientMock = new Mock<IFirestoreClient>();
        clientMock
            .Setup(c => c.ListDocumentsAsync("attendances", It.IsAny<CancellationToken>()))
            .ReturnsAsync((docs ?? []).ToList());
        clientMock
            .Setup(c => c.UpsertDocumentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (new AttendanceService(clientMock.Object), clientMock);
    }

    [Fact]
    public async Task 指定した練習の自団体分のみ出欠が取得される()
    {
        var docs = new[]
        {
            CreateAttendanceDoc("a1", "practice1", "m1", AttendanceStatus.Attending),
            CreateAttendanceDoc("a2", "practice2", "m2", AttendanceStatus.Attending),
            CreateAttendanceDoc("a3", "practice1", "m3", AttendanceStatus.Attending, groupId: "other")
        };
        var (service, _) = CreateService(docs);

        var attendances = await service.GetForPracticeAsync("practice1");

        Assert.Equal(["m1"], attendances.Select(a => a.MemberId));
    }

    [Fact]
    public async Task 指定メンバーの出欠が見つからない場合はnullが返る()
    {
        var docs = new[] { CreateAttendanceDoc("a1", "practice1", "m1", AttendanceStatus.Attending) };
        var (service, _) = CreateService(docs);

        var mine = await service.GetForMemberAsync("practice1", "m2");

        Assert.Null(mine);
    }

    [Fact]
    public async Task 指定メンバーの出欠が見つかる場合はその内容が返る()
    {
        var docs = new[] { CreateAttendanceDoc("a1", "practice1", "m1", AttendanceStatus.NotAttending) };
        var (service, _) = CreateService(docs);

        var mine = await service.GetForMemberAsync("practice1", "m1");

        Assert.NotNull(mine);
        Assert.Equal(AttendanceStatus.NotAttending, mine!.Status);
    }

    [Fact]
    public async Task 出欠を登録すると練習IDとメンバーIDの複合キーで保存される()
    {
        var (service, client) = CreateService();

        await service.SetStatusAsync("practice1", "member1", "山田 太郎", PartType.Alto, AttendanceStatus.Attending);

        client.Verify(c => c.UpsertDocumentAsync(
            "attendances",
            Attendance.BuildId("practice1", "member1"),
            It.Is<Dictionary<string, object?>>(f =>
                (string)f["memberId"]! == "member1" &&
                (string)f["memberName"]! == "山田 太郎" &&
                (string)f["part"]! == PartType.Alto.ToString() &&
                (string)f["status"]! == AttendanceStatus.Attending.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
