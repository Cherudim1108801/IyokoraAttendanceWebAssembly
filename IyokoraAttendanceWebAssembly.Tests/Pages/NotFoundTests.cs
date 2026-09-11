using Bunit;
using IyokoraAttendanceWebAssembly.Pages;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class NotFoundTests : TestContext
{
    [Fact]
    public void ページが見つからない旨のメッセージが表示される()
    {
        var cut = RenderComponent<NotFound>();

        Assert.Contains("ページが見つかりません", cut.Markup);
    }
}
