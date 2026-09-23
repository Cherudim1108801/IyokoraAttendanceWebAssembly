using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class PracticeHistoryTests : BunitContext
{
    private (FakeFirestoreClient client, Mock<IJSRuntime> js) RegisterServices(Role role = Role.GeneralMember)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(new PracticeService(new PracticeRepository(client)));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        profile.Role = role;
        Services.AddSingleton(profile);

        var jsMock = new Mock<IJSRuntime>();
        Services.AddSingleton(jsMock.Object);

        return (client, jsMock);
    }

    [Fact]
    public void 過去の練習が無い場合は案内メッセージが表示される()
    {
        RegisterServices();

        var cut = Render<PracticeHistory>();

        Assert.Contains("過去の練習データはありません", cut.Find("p.empty-text").TextContent);
    }

    [Fact]
    public void 過去の練習のみ新しい順に表示される()
    {
        var (client, _) = RegisterServices();
        client.Seed("practices", "old", Seed.Practice(DateTime.Today.AddDays(-10), title: "古い練習"));
        client.Seed("practices", "recent", Seed.Practice(DateTime.Today.AddDays(-1), title: "最近の練習"));
        client.Seed("practices", "future", Seed.Practice(DateTime.Today.AddDays(5), title: "未来の練習"));

        var cut = Render<PracticeHistory>();

        var titles = cut.FindAll("p.list-card-title").Select(e => e.TextContent).ToList();
        Assert.DoesNotContain(titles, t => t.Contains("未来"));
        var subs = cut.FindAll("p.list-card-sub").Select(e => e.TextContent).ToList();
        Assert.Contains(subs, s => s.Contains("最近の練習"));
        Assert.Contains(subs, s => s.Contains("古い練習"));
    }

    [Fact]
    public void 一般団員には削除ボタンが表示されない()
    {
        var (client, _) = RegisterServices(Role.GeneralMember);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1)));

        var cut = Render<PracticeHistory>();

        Assert.Empty(cut.FindAll("button.iyk-btn-text-danger"));
    }

    [Fact]
    public void 管理者が削除すると一覧から消える()
    {
        var (client, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), title: "削除対象"));

        var cut = Render<PracticeHistory>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Empty(cut.FindAll("div.list-card"));
        Assert.False(client.Contains("practices", "p1"));
    }

    [Fact]
    public void カードをタップすると練習詳細へ遷移する()
    {
        var (client, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1)));

        var cut = Render<PracticeHistory>();
        cut.Find("div.list-card").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("practice/p1", nav.Uri);
    }

    [Fact]
    public void 場所や曲目が設定された練習は一覧カードに表示される()
    {
        var (client, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(
            DateTime.Today.AddDays(-1),
            place: "市民会館",
            pieces: [new PracticePieceRef { PieceId = "piece1", Title = "曲A" }]));

        var cut = Render<PracticeHistory>();

        Assert.Contains("市民会館", cut.Markup);
        Assert.Contains("曲A", cut.Markup);
    }

    [Fact]
    public void 読み込みに失敗した場合はエラーメッセージが表示される()
    {
        var (client, _) = RegisterServices();
        client.FailNextCall("Query", "practices");

        var cut = Render<PracticeHistory>();

        Assert.Contains("読み込みに失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Empty(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void 削除に失敗した場合はエラーメッセージが表示され一覧に残る()
    {
        var (client, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1)));
        client.FailNextCall("Delete", "practices");

        var cut = Render<PracticeHistory>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Contains("削除に失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Single(cut.FindAll("div.list-card"));
        Assert.True(client.Contains("practices", "p1"));
    }

    [Fact]
    public void 曲が登録されていない練習には音源管理ボタンが表示されない()
    {
        var (client, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1)));

        var cut = Render<PracticeHistory>();

        Assert.DoesNotContain(cut.FindAll("button.iyk-btn-text"), b => b.TextContent.Contains("音源を管理"));
    }

    [Fact]
    public void 音源を管理ボタンをタップすると曲ごとの録音状況が表示される()
    {
        var (client, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [Seed.Recording("https://example.com/rec", isFeatured: true)] },
            new() { PieceId = "pc2", Title = "曲B" }
        ]));

        var cut = Render<PracticeHistory>();
        cut.FindAll("button.iyk-btn-text").Single(b => b.TextContent.Contains("音源を管理")).Click();

        Assert.Contains("曲A", cut.Markup);
        Assert.Contains("録音を聴く", cut.Markup);
        Assert.Contains("曲B", cut.Markup);
        Assert.Contains("録音リンク未登録", cut.Markup);
    }

    [Fact]
    public void 同じ曲の複数の録音が音源パネルに表示される()
    {
        var (client, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [Seed.Recording("https://example.com/rec1"), Seed.Recording("https://example.com/rec2")] }
        ]));

        var cut = Render<PracticeHistory>();
        cut.FindAll("button.iyk-btn-text").Single(b => b.TextContent.Contains("音源を管理")).Click();

        var links = cut.FindAll("a").Where(a => a.TextContent.Contains("録音を聴く")).ToList();
        Assert.Equal(2, links.Count);
    }

    [Fact]
    public void 一般団員が音源パネルを開いても編集ボタンは表示されない()
    {
        var (client, _) = RegisterServices(Role.GeneralMember);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [Seed.Recording("https://example.com/rec")] }
        ]));

        var cut = Render<PracticeHistory>();
        cut.FindAll("button.iyk-btn-text").Single(b => b.TextContent.Contains("音源を管理")).Click();

        Assert.DoesNotContain(cut.FindAll("button.iyk-btn-text"), b => b.TextContent.Contains("編集"));
    }

    [Fact]
    public void 管理者が練習履歴画面から録音リンクを追加すると音源リンクが表示される()
    {
        var (client, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        js.Setup(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>())).ReturnsAsync("https://example.com/rec");

        var cut = Render<PracticeHistory>();
        cut.FindAll("button.iyk-btn-text").Single(b => b.TextContent.Contains("音源を管理")).Click();
        cut.FindAll("button.iyk-btn-text").Single(b => b.TextContent.Contains("＋ 録音を追加")).Click();

        Assert.Contains("録音を聴く", cut.Markup);
        Assert.Contains("★ 注目に設定", cut.Markup);
        Assert.True(client.Contains("practices", "p1"));
    }

    [Fact]
    public void 不正な形式の録音リンクを入力するとエラーが表示される()
    {
        var (client, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        js.Setup(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>())).ReturnsAsync("不正なリンク");

        var cut = Render<PracticeHistory>();
        cut.FindAll("button.iyk-btn-text").Single(b => b.TextContent.Contains("音源を管理")).Click();
        cut.FindAll("button.iyk-btn-text").Single(b => b.TextContent.Contains("＋ 録音を追加")).Click();

        Assert.Contains("リンクの形式が正しくありません", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 録音を編集して空欄で保存するとその録音だけ削除される()
    {
        var (client, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [Seed.Recording("https://example.com/rec1", id: "r1"), Seed.Recording("https://example.com/rec2", id: "r2")] }
        ]));
        js.Setup(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>())).ReturnsAsync("");

        var cut = Render<PracticeHistory>();
        cut.FindAll("button.iyk-btn-text").Single(b => b.TextContent.Contains("音源を管理")).Click();
        cut.FindAll("button.iyk-btn-text").First(b => b.TextContent.Contains("編集")).Click();

        var links = cut.FindAll("a").Where(a => a.TextContent.Contains("録音を聴く")).ToList();
        Assert.Single(links);
    }

    [Fact]
    public void 管理者が練習履歴画面から録音済みの曲を注目に設定すると表示が切り替わる()
    {
        var (client, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [Seed.Recording("https://example.com/rec")] }
        ]));

        var cut = Render<PracticeHistory>();
        cut.FindAll("button.iyk-btn-text").Single(b => b.TextContent.Contains("音源を管理")).Click();
        Assert.Contains("★ 注目に設定", cut.Markup);

        cut.FindAll("button.iyk-btn-text").Single(b => b.TextContent.Contains("★ 注目に設定")).Click();

        Assert.Contains("★ 注目解除", cut.Markup);
        Assert.DoesNotContain("★ 注目に設定", cut.Markup);
    }
}
