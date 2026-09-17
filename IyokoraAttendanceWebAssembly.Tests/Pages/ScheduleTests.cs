using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class ScheduleTests : BunitContext
{
    private FakeFirestoreClient RegisterServices(Role role = Role.GeneralMember)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(new PracticeService(new PracticeRepository(client)));
        Services.AddSingleton(new PieceService(new PieceRepository(client)));
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

        var cut = Render<Schedule>();

        Assert.Empty(cut.FindAll("button.iyk-btn-primary"));
        Assert.Empty(cut.FindAll("button.iyk-btn-text-danger"));
    }

    [Fact]
    public void 予定が無い場合は案内メッセージが表示される()
    {
        RegisterServices(Role.Admin);

        var cut = Render<Schedule>();

        Assert.Contains("登録されている練習予定はありません", cut.Markup);
    }

    [Fact]
    public void 過去の予定は表示されず今後の予定のみ表示される()
    {
        var client = RegisterServices();
        client.Seed("practices", "past", Seed.Practice(DateTime.Today.AddDays(-1), title: "過去の練習"));
        client.Seed("practices", "future", Seed.Practice(DateTime.Today.AddDays(3), title: "今後の練習"));

        var cut = Render<Schedule>();

        Assert.DoesNotContain("過去の練習", cut.Markup);
        Assert.Contains("今後の練習", cut.Markup);
    }

    [Fact]
    public void 管理者が日付のみで練習予定を追加すると一覧に反映される()
    {
        var client = RegisterServices(Role.Admin);
        var cut = Render<Schedule>();

        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Single(cut.FindAll("div.list-card"));
        Assert.Empty(cut.FindAll("p.error-text"));
    }

    [Fact]
    public void 開始時刻のみ入力し終了時刻を入力しない場合はエラーが表示される()
    {
        RegisterServices(Role.Admin);
        var cut = Render<Schedule>();
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

        var cut = Render<Schedule>();
        cut.Find("div.list-card").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("practice/p1", nav.Uri);
    }

    [Fact]
    public void 管理者が削除すると一覧から消える()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = Render<Schedule>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Empty(cut.FindAll("div.list-card"));
        Assert.False(client.Contains("practices", "p1"));
    }

    [Fact]
    public void タイトルや時間_場所_曲目が設定された予定は一覧カードに全て表示される()
    {
        var client = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(
            DateTime.Today.AddDays(3),
            title: "定期練習",
            place: "市民会館",
            startTime: "10:00",
            endTime: "12:00",
            pieces: [new PracticePieceRef { PieceId = "piece1", Title = "曲A" }]));

        var cut = Render<Schedule>();

        Assert.Contains("定期練習", cut.Markup);
        Assert.Contains("10:00〜12:00", cut.Markup);
        Assert.Contains("市民会館", cut.Markup);
        Assert.Contains("曲A", cut.Markup);
    }

    [Fact]
    public void 読み込みに失敗した場合はエラーメッセージが表示される()
    {
        var client = RegisterServices();
        client.FailNextCall("Query", "practices");

        var cut = Render<Schedule>();

        Assert.Contains("読み込みに失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Empty(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void 曲が登録されている場合はチェックボックスが表示され選択した曲のみ登録される()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("pieces", "piece1", Seed.Piece("曲A"));
        client.Seed("pieces", "piece2", Seed.Piece("曲B"));

        var cut = Render<Schedule>();
        cut.Find("button.iyk-btn-primary").Click();

        var checkboxes = cut.FindAll("input[type=checkbox][id^=piece_]");
        Assert.Equal(2, checkboxes.Count);
        checkboxes[0].Change(true);

        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Contains("曲A", cut.Markup);
        Assert.DoesNotContain("曲B", cut.Find("div.list-card").TextContent);
    }

    [Fact]
    public void タイムスケジュール項目を追加すると入力欄が増え削除ボタンで減らせる()
    {
        RegisterServices(Role.Admin);
        var cut = Render<Schedule>();
        cut.Find("button.iyk-btn-primary").Click();

        cut.Find("div.highlight-card button.iyk-btn-block:not(.iyk-btn-primary)").Click();
        cut.Find("div.highlight-card button.iyk-btn-block:not(.iyk-btn-primary)").Click();
        Assert.Equal(2, cut.FindAll("input[type=text][placeholder='例：基礎合奏']").Count);

        cut.FindAll("div.highlight-card button.iyk-btn-text-danger")[0].Click();
        Assert.Single(cut.FindAll("input[type=text][placeholder='例：基礎合奏']"));
    }

    [Fact]
    public void 予定の追加に失敗した場合はエラーメッセージが表示される()
    {
        var client = RegisterServices(Role.Admin);
        client.FailNextCall("Upsert", "practices");

        var cut = Render<Schedule>();
        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Contains("予定の追加に失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Empty(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void 削除に失敗した場合はエラーメッセージが表示され一覧に残る()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.FailNextCall("Delete", "practices");

        var cut = Render<Schedule>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Contains("削除に失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Single(cut.FindAll("div.list-card"));
        Assert.True(client.Contains("practices", "p1"));
    }
}
