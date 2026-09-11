using Bunit;
using IyokoraAttendanceWebAssembly.Pages;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class NotFoundTests : BunitContext
{
    [Fact]
    public void ページが見つからない旨のメッセージが表示される()
    {
        var cut = Render<NotFound>();

        Assert.Contains("ページが見つかりません", cut.Markup);
    }
}
