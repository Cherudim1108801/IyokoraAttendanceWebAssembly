using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class PracticeHistoryTests : TestContext
{
    private FakeFirestoreClient RegisterServices(Role role = Role.GeneralMember)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(new PracticeService(client));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        profile.Role = role;
        Services.AddSingleton(profile);
        return client;
    }

    [Fact]
    public void 過去の練習が無い場合は案内メッセージが表示される()
    {
        RegisterServices();

        var cut = RenderComponent<PracticeHistory>();

        Assert.Contains("過去の練習データはありません", cut.Find("p.empty-text").TextContent);
    }

    [Fact]
    public void 過去の練習のみ新しい順に表示される()
    {
        var client = RegisterServices();
        client.Seed("practices", "old", Seed.Practice(DateTime.Today.AddDays(-10), title: "古い練習"));
        client.Seed("practices", "recent", Seed.Practice(DateTime.Today.AddDays(-1), title: "最近の練習"));
        client.Seed("practices", "future", Seed.Practice(DateTime.Today.AddDays(5), title: "未来の練習"));

        var cut = RenderComponent<PracticeHistory>();

        var titles = cut.FindAll("p.list-card-title").Select(e => e.TextContent).ToList();
        Assert.DoesNotContain(titles, t => t.Contains("未来"));
        var subs = cut.FindAll("p.list-card-sub").Select(e => e.TextContent).ToList();
        Assert.Contains(subs, s => s.Contains("最近の練習"));
        Assert.Contains(subs, s => s.Contains("古い練習"));
    }

    [Fact]
    public void 一般団員には削除ボタンが表示されない()
    {
        var client = RegisterServices(Role.GeneralMember);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1)));

        var cut = RenderComponent<PracticeHistory>();

        Assert.Empty(cut.FindAll("button.iyk-btn-text-danger"));
    }

    [Fact]
    public void 管理者が削除すると一覧から消える()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1), title: "削除対象"));

        var cut = RenderComponent<PracticeHistory>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Empty(cut.FindAll("div.list-card"));
        Assert.False(client.Contains("practices", "p1"));
    }

    [Fact]
    public void カードをタップすると練習詳細へ遷移する()
    {
        var client = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-1)));

        var cut = RenderComponent<PracticeHistory>();
        cut.Find("div.list-card").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("practice/p1", nav.Uri);
    }
}
