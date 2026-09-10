using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class AttendanceTests
{
    [Fact]
    public void 練習IDとメンバーIDからアンダースコア区切りの複合キーが生成される()
    {
        var id = Attendance.BuildId("practice-001", "member-002");

        Assert.Equal("practice-001_member-002", id);
    }
}
