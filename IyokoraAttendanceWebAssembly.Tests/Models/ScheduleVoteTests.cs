using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class ScheduleVoteTests
{
    [Fact]
    public void 候補日IDとメンバーIDから複合キーが生成される()
    {
        var id = ScheduleVote.BuildId("candidate1", "member1");

        Assert.Equal("candidate1_member1", id);
    }
}
