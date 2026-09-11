using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class ScheduleTests : TestContext
{
    private FakeFirestoreClient RegisterServices(Role role = Role.GeneralMember)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(new PracticeService(client));
        Services.AddSingleton(new PieceService(client));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        profile.Role = role;
        Services.AddSingleton(profile);
        return client;
    }

    [Fact]
    public void 一般団員には追加ボタンや削除ボタンが表示されない()
    {
        var client = RegisterServices(Role.GeneralMember);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = RenderComponent<Schedule>();

        Assert.Empty(cut.FindAll("button.iyk-btn-primary"));
        Assert.Empty(cut.FindAll("button.iyk-btn-text-danger"));
    }

    [Fact]
    public void 予定が無い場合は案内メッセージが表示される()
    {
        RegisterServices(Role.Admin);

        var cut = RenderComponent<Schedule>();

        Assert.Contains("登録されている練習予定はありません", cut.Markup);
    }

    [Fact]
    public void 過去の予定は表示されず今後の予定のみ表示される()
    {
        var client = RegisterServices();
        client.Seed("practices", "past", Seed.Practice(DateTime.Today.AddDays(-1), title: "過去の練習"));
        client.Seed("practices", "future", Seed.Practice(DateTime.Today.AddDays(3), title: "今後の練習"));

        var cut = RenderComponent<Schedule>();

        Assert.DoesNotContain("過去の練習", cut.Markup);
        Assert.Contains("今後の練習", cut.Markup);
    }

    [Fact]
    public void 管理者が日付のみで練習予定を追加すると一覧に反映される()
    {
        var client = RegisterServices(Role.Admin);
        var cut = RenderComponent<Schedule>();

        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Single(cut.FindAll("div.list-card"));
        Assert.Empty(cut.FindAll("p.error-text"));
    }

    [Fact]
    public void 開始時刻のみ入力し終了時刻を入力しない場合はエラーが表示される()
    {
        RegisterServices(Role.Admin);
        var cut = RenderComponent<Schedule>();
        cut.Find("button.iyk-btn-primary").Click();

        cut.FindAll("input[type=time]")[0].Input("10:00:00");
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Contains("開始時刻と終了時刻は両方入力してください", cut.Find("p.error-text").TextContent);
        Assert.Empty(cut.FindAll("div.list-card"));
        Assert.Empty(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void カードをタップすると練習詳細へ遷移する()
    {
        var client = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = RenderComponent<Schedule>();
        cut.Find("div.list-card").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("practice/p1", nav.Uri);
    }

    [Fact]
    public void 管理者が削除すると一覧から消える()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = RenderComponent<Schedule>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Empty(cut.FindAll("div.list-card"));
        Assert.False(client.Contains("practices", "p1"));
    }
}
