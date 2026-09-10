using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class RoleExtensionsTests
{
    [Theory]
    [InlineData(Role.Admin, "管理者")]
    [InlineData(Role.GeneralMember, "一般団員")]
    public void 表示名が役割ごとに正しく変換される(Role role, string expected)
    {
        Assert.Equal(expected, role.ToDisplayName());
    }

    [Fact]
    public void 全役割が一般団員管理者の順で列挙される()
    {
        Assert.Equal([Role.GeneralMember, Role.Admin], RoleExtensions.All);
    }
}
