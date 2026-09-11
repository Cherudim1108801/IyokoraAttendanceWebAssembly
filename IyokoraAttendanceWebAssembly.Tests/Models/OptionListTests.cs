using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class OptionListTests
{
    [Fact]
    public void PartOptionのAllは全パートを表示名付きで固定順に列挙する()
    {
        Assert.Equal(PartTypeExtensions.All, PartOption.All.Select(o => o.Part));
        Assert.Equal(PartTypeExtensions.All.Select(p => p.ToDisplayName()), PartOption.All.Select(o => o.Label));
    }

    [Fact]
    public void PartDivisionOptionのAllは全分割方式を表示名付きで固定順に列挙する()
    {
        Assert.Equal(PartDivisionExtensions.All, PartDivisionOption.All.Select(o => o.Division));
        Assert.Equal(PartDivisionExtensions.All.Select(d => d.ToDisplayName()), PartDivisionOption.All.Select(o => o.Label));
    }

    [Fact]
    public void RoleOptionのAllは全役割を表示名付きで固定順に列挙する()
    {
        Assert.Equal(RoleExtensions.All, RoleOption.All.Select(o => o.Role));
        Assert.Equal(RoleExtensions.All.Select(r => r.ToDisplayName()), RoleOption.All.Select(o => o.Label));
    }
}
