using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class RecordingsTests : BunitContext
{
    private FakeFirestoreClient RegisterServices()
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(new PracticeService(new PracticeRepository(client)));
        return client;
    }

    [Fact]
    public void 録音データが無い場合は案内メッセージが表示される()
    {
        RegisterServices();

        var cut = Render<Recordings>();

        Assert.Contains("録音データはまだありません", cut.Markup);
    }

    [Fact]
    public void 録音の無い曲は一覧に表示されない()
    {
        var client = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc1", Title = "録音無し" }
        ]));

        var cut = Render<Recordings>();

        Assert.Contains("録音データはまだありません", cut.Markup);
    }

    [Fact]
    public void 強調表示されている録音は注目の音源セクションにも表示される()
    {
        var client = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), title: "第1回練習", pieces:
        [
            new() { PieceId = "pc1", Title = "強調曲", Recordings = [Seed.Recording("https://example.com/a", isFeatured: true)] },
            new() { PieceId = "pc2", Title = "通常曲", Recordings = [Seed.Recording("https://example.com/b")] }
        ]));

        var cut = Render<Recordings>();

        var featuredSection = cut.Find("h3.section-title");
        Assert.Contains("注目の音源", featuredSection.TextContent);
        var allTitles = cut.FindAll("p.list-card-title").Select(e => e.TextContent).ToList();
        Assert.Equal(3, allTitles.Count); // 注目1件 + 全件2件
        Assert.Contains(allTitles, t => t.Contains("強調曲"));
        Assert.Contains(allTitles, t => t.Contains("通常曲"));
    }

    [Fact]
    public void 同じ曲に複数の録音があると全件一覧に表示される()
    {
        var client = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [Seed.Recording("https://example.com/a"), Seed.Recording("https://example.com/b")] }
        ]));

        var cut = Render<Recordings>();

        var links = cut.FindAll("a.list-card");
        Assert.Equal(2, links.Count);
    }

    [Fact]
    public void 名前が付いた録音はその名前が表示される()
    {
        var client = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [Seed.Recording("https://example.com/a", name: "本番前通し")] }
        ]));

        var cut = Render<Recordings>();

        Assert.Contains("本番前通し", cut.Markup);
    }

    [Fact]
    public void 練習日の新しい順に表示される()
    {
        var client = RegisterServices();
        client.Seed("practices", "old", Seed.Practice(DateTime.Today.AddDays(-10), pieces:
        [
            new() { PieceId = "pc1", Title = "古い曲", Recordings = [Seed.Recording("https://example.com/old")] }
        ]));
        client.Seed("practices", "recent", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc2", Title = "新しい曲", Recordings = [Seed.Recording("https://example.com/recent")] }
        ]));

        var cut = Render<Recordings>();

        var titles = cut.FindAll("p.list-card-title").Select(e => e.TextContent).ToList();
        Assert.Equal(["新しい曲", "古い曲"], titles);
    }

    [Fact]
    public void 読み込みに失敗した場合はエラーメッセージが表示される()
    {
        var client = RegisterServices();
        client.FailNextCall("Query", "practices");

        var cut = Render<Recordings>();

        Assert.Contains("読み込みに失敗しました", cut.Find("p.error-text").TextContent);
    }
}
