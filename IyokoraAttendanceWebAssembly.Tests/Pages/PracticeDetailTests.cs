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

public class PracticeDetailTests : BunitContext
{
    private (FakeFirestoreClient client, LocalProfileStore profile, Mock<IJSRuntime> js) RegisterServices(Role role = Role.GeneralMember)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(new PracticeService(new PracticeRepository(client)));
        Services.AddSingleton(FakeMemberService.Create(client));
        Services.AddSingleton(new AttendanceService(new AttendanceRepository(client)));
        Services.AddSingleton(new PieceService(new PieceRepository(client)));

        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        profile.MemberId = "me";
        profile.Name = "自分";
        profile.Part = PartType.Soprano;
        profile.Role = role;
        Services.AddSingleton(profile);

        var jsMock = new Mock<IJSRuntime>();
        Services.AddSingleton(jsMock.Object);

        return (client, profile, jsMock);
    }

    private IRenderedComponent<PracticeDetail> Render(string practiceId = "p1") =>
        Render<PracticeDetail>(p => p.Add(x => x.PracticeId, practiceId));

    [Fact]
    public void 練習の基本情報と参加予定人数が表示される()
    {
        var (client, _, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), title: "定期練習", place: "市民会館"));
        client.Seed("members", "m1", Seed.Member("メンバー1"));
        client.Seed("attendances", "p1_m1", Seed.Attendance("p1", "m1", "メンバー1", PartType.Soprano, AttendanceStatus.Attending));

        var cut = Render();

        Assert.Contains("定期練習", cut.Markup);
        Assert.Contains("📍 市民会館", cut.Markup);
        Assert.Contains("参加予定: 1 / 1 人", cut.Markup);
    }

    [Fact]
    public void タイムスケジュール未登録の場合は案内メッセージが表示される()
    {
        var (client, _, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = Render();

        Assert.Contains("タイムスケジュールは登録されていません", cut.Markup);
    }

    [Fact]
    public void 出欠ボタンをタップすると出欠が保存され合計が更新される()
    {
        var (client, profile, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.Seed("members", "me", Seed.Member("自分"));

        var cut = Render();
        cut.Find("button.attend-yes").Click();

        Assert.Contains("参加予定: 1 / 1 人", cut.Markup);
        Assert.True(client.Contains("attendances", Attendance.BuildId("p1", "me")));
    }

    [Fact]
    public void 出欠ボタンをタップすると出欠一覧のみ再取得され練習_メンバー_曲の再取得は発生しない()
    {
        var (client, profile, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.Seed("members", "me", Seed.Member("自分"));

        var cut = Render();
        var practiceCallsAfterLoad = client.Calls.Count(c => c.Collection == "practices");
        var memberCallsAfterLoad = client.Calls.Count(c => c.Collection == "members");
        var pieceCallsAfterLoad = client.Calls.Count(c => c.Collection == "pieces");
        var attendanceCallsAfterLoad = client.Calls.Count(c => c.Collection == "attendances");

        cut.Find("button.attend-yes").Click();

        Assert.Equal(practiceCallsAfterLoad, client.Calls.Count(c => c.Collection == "practices"));
        Assert.Equal(memberCallsAfterLoad, client.Calls.Count(c => c.Collection == "members"));
        Assert.Equal(pieceCallsAfterLoad, client.Calls.Count(c => c.Collection == "pieces"));
        Assert.Equal(attendanceCallsAfterLoad + 1, client.Calls.Count(c => c.Collection == "attendances"));
    }

    [Fact]
    public void 出欠ボタンをタップすると他メンバーが直前に登録した出欠も合計に反映される()
    {
        var (client, profile, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.Seed("members", "me", Seed.Member("自分"));
        client.Seed("members", "m2", Seed.Member("相方"));

        var cut = Render();

        // 自分がボタンをタップするまでの間に、他のメンバーが別セッションで出欠を登録したことを模倣する。
        client.Seed("attendances", Attendance.BuildId("p1", "m2"), Seed.Attendance("p1", "m2", "相方", PartType.Soprano, AttendanceStatus.Attending));

        cut.Find("button.attend-yes").Click();

        Assert.Contains("参加予定: 2 / 2 人", cut.Markup);
    }

    [Fact]
    public void 過去の練習では出欠ボタンが無効化され案内が表示される()
    {
        var (client, _, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(-3)));

        var cut = Render();

        Assert.Contains("過去の練習のため、出欠は変更できません", cut.Markup);
        Assert.True(cut.Find("button.attend-yes").HasAttribute("disabled"));
    }

    [Fact]
    public void 一般団員には編集ボタンや削除ボタンが表示されない()
    {
        var (client, _, _) = RegisterServices(Role.GeneralMember);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = Render();

        Assert.Empty(cut.FindAll("button.iyk-btn-text"));
        Assert.Empty(cut.FindAll("button.iyk-btn-outline-danger"));
    }

    [Fact]
    public void 管理者がタイムスケジュールを編集して保存すると一覧に反映される()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = Render();
        cut.Find("button.iyk-btn-text").Click();
        cut.FindAll("input[type=time]")[0].Input("10:00:00");
        cut.FindAll("input[type=time]")[1].Input("12:00:00");
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Contains("10:00〜12:00", cut.Markup);
    }

    [Fact]
    public void タイムスケジュールを保存しても各コレクションの再取得は発生しない()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = Render();
        cut.Find("button.iyk-btn-text").Click();
        cut.FindAll("input[type=time]")[0].Input("10:00:00");
        cut.FindAll("input[type=time]")[1].Input("12:00:00");
        var callsAfterLoad = client.Calls.Count;
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Equal(callsAfterLoad, client.Calls.Count);
    }

    [Fact]
    public void 開始時刻のみ入力すると検証エラーが表示され保存されない()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = Render();
        cut.Find("button.iyk-btn-text").Click();
        cut.FindAll("input[type=time]")[0].Input("10:00:00");
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Contains("開始時刻と終了時刻は両方入力してください", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 管理者が演奏予定曲を編集して保存すると反映される()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.Seed("pieces", "pc1", Seed.Piece("編集後の曲"));

        var cut = Render();
        var editButtons = cut.FindAll("button.iyk-btn-text");
        editButtons[1].Click(); // タイムスケジュール分の次(演奏予定曲の編集)
        cut.Find("input[type=checkbox]").Change(true);
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Contains("編集後の曲", cut.Markup);
    }

    [Fact]
    public void 演奏予定曲を保存しても各コレクションの再取得は発生しない()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.Seed("pieces", "pc1", Seed.Piece("編集後の曲"));

        var cut = Render();
        var editButtons = cut.FindAll("button.iyk-btn-text");
        editButtons[1].Click();
        cut.Find("input[type=checkbox]").Change(true);
        var callsAfterLoad = client.Calls.Count;
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Equal(callsAfterLoad, client.Calls.Count);
    }

    [Fact]
    public void 管理者が録音リンクを追加すると音源リンクが表示される()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        js.SetupSequence(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>()))
            .ReturnsAsync("")
            .ReturnsAsync("https://example.com/rec");

        var cut = Render();
        // [0]=タイムスケジュール編集 [1]=演奏予定曲編集 [2]=＋ 録音を追加(録音未登録のため他のボタンは無い)
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Contains("録音を聴く", cut.Markup);
        Assert.Contains("★ 注目に設定", cut.Markup);
    }

    [Fact]
    public void 録音に名前を付けて追加すると名前が表示される()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        js.SetupSequence(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>()))
            .ReturnsAsync("本番前通し")
            .ReturnsAsync("https://example.com/rec");

        var cut = Render();
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Contains("本番前通し", cut.Markup);
        Assert.DoesNotContain("録音を聴く", cut.Markup);
    }

    [Fact]
    public void 録音リンクを追加しても各コレクションの再取得は発生しない()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        js.SetupSequence(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>()))
            .ReturnsAsync("")
            .ReturnsAsync("https://example.com/rec");

        var cut = Render();
        var callsAfterLoad = client.Calls.Count;
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Equal(callsAfterLoad, client.Calls.Count);
    }

    [Fact]
    public void 不正な形式の録音リンクを入力するとエラーが表示される()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        js.SetupSequence(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>()))
            .ReturnsAsync("")
            .ReturnsAsync("不正なリンク");

        var cut = Render();
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Contains("リンクの形式が正しくありません", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 同じ曲に2件目の録音を追加すると両方表示される()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [new() { Id = "r1", Url = "https://example.com/rec1", IsFeatured = false }] }
        ]));
        js.SetupSequence(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>()))
            .ReturnsAsync("")
            .ReturnsAsync("https://example.com/rec2");

        var cut = Render();
        // [0]=タイムスケジュール編集 [1]=演奏予定曲編集 [2]=★注目に設定 [3]=編集 [4]=＋録音を追加
        cut.FindAll("button.iyk-btn-text")[4].Click();

        var links = cut.FindAll("a").Where(a => a.TextContent.Contains("録音を聴く")).ToList();
        Assert.Equal(2, links.Count);
    }

    [Fact]
    public void 録音済みの曲を注目に設定すると表示が切り替わる()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [new() { Id = "r1", Url = "https://example.com/rec", IsFeatured = false }] }
        ]));

        var cut = Render();
        Assert.Contains("★ 注目に設定", cut.Markup);

        // [0]=タイムスケジュール編集 [1]=演奏予定曲編集 [2]=★ 注目に設定
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Contains("★ 注目解除", cut.Markup);
        Assert.DoesNotContain("★ 注目に設定", cut.Markup);
    }

    [Fact]
    public void 注目中の曲を注目解除すると表示が切り替わる()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [new() { Id = "r1", Url = "https://example.com/rec", IsFeatured = true }] }
        ]));

        var cut = Render();
        Assert.Contains("★ 注目解除", cut.Markup);

        cut.FindAll("button.iyk-btn-text-star")[0].Click();

        Assert.Contains("★ 注目に設定", cut.Markup);
        Assert.DoesNotContain("★ 注目解除", cut.Markup);
    }

    [Fact]
    public void 注目設定を切り替えても各コレクションの再取得は発生しない()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [new() { Id = "r1", Url = "https://example.com/rec", IsFeatured = false }] }
        ]));

        var cut = Render();
        var callsAfterLoad = client.Calls.Count;
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Equal(callsAfterLoad, client.Calls.Count);
    }

    [Fact]
    public void 録音を編集して空欄で保存するとその録音だけ削除される()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [
                new() { Id = "r1", Url = "https://example.com/rec1", IsFeatured = false },
                new() { Id = "r2", Url = "https://example.com/rec2", IsFeatured = false }
            ] }
        ]));
        js.Setup(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>())).ReturnsAsync("");

        var cut = Render();
        // [0]=タイムスケジュール編集 [1]=演奏予定曲編集 [2]=1件目★注目に設定 [3]=1件目編集 [4]=2件目★注目に設定 [5]=2件目編集 [6]=＋録音を追加
        cut.FindAll("button.iyk-btn-text")[3].Click();

        var links = cut.FindAll("a").Where(a => a.TextContent.Contains("録音を聴く")).ToList();
        Assert.Single(links);
    }

    [Fact]
    public void 管理者が鍵の受け取り状況を切り替えられる()
    {
        var (client, profile, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), requiresKeyPickup: true));

        var cut = Render();
        Assert.Contains("未受け取り", cut.Markup);

        cut.Find("button.iyk-btn-text-primary").Click();

        Assert.Contains("受け取り済み", cut.Markup);
        Assert.Contains(profile.Name, cut.Markup);
    }

    [Fact]
    public void 鍵の受け取り状況を切り替えても各コレクションの再取得は発生しない()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), requiresKeyPickup: true));

        var cut = Render();
        var callsAfterLoad = client.Calls.Count;
        cut.Find("button.iyk-btn-text-primary").Click();

        Assert.Equal(callsAfterLoad, client.Calls.Count);
    }

    [Fact]
    public void 鍵の受け取りが不要な練習では案内メッセージが表示される()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), requiresKeyPickup: false));

        var cut = Render();

        Assert.Contains("この練習では鍵の受け取りは不要です", cut.Markup);
    }

    [Fact]
    public void 削除確認で承認すると練習が削除され予定一覧へ遷移する()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        js.Setup(j => j.InvokeAsync<bool>("confirm", It.IsAny<object?[]>())).ReturnsAsync(true);

        var cut = Render();
        cut.Find("button.iyk-btn-outline-danger").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("schedule", nav.Uri);
        Assert.False(client.Contains("practices", "p1"));
    }

    [Fact]
    public void 削除確認で拒否すると練習は削除されない()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        js.Setup(j => j.InvokeAsync<bool>("confirm", It.IsAny<object?[]>())).ReturnsAsync(false);

        var cut = Render();
        cut.Find("button.iyk-btn-outline-danger").Click();

        Assert.True(client.Contains("practices", "p1"));
    }

    [Fact]
    public void 読み込みに失敗した場合はエラーメッセージが表示される()
    {
        var (client, _, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.FailNextCall("Get", "practices");

        var cut = Render();

        Assert.Contains("読み込みに失敗しました", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 出欠更新に失敗した場合はエラーメッセージが表示される()
    {
        var (client, _, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.Seed("members", "me", Seed.Member("自分"));
        client.FailNextCall("Upsert", "attendances");

        var cut = Render();
        cut.Find("button.attend-yes").Click();

        Assert.Contains("更新に失敗しました", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void タイムスケジュールの保存に失敗した場合はエラーメッセージが表示される()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.FailNextCall("Upsert", "practices");

        var cut = Render();
        cut.Find("button.iyk-btn-text").Click();
        cut.FindAll("input[type=time]")[0].Input("10:00:00");
        cut.FindAll("input[type=time]")[1].Input("12:00:00");
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Contains("タイムスケジュールの保存に失敗しました", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 演奏予定曲の保存に失敗した場合はエラーメッセージが表示される()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.Seed("pieces", "pc1", Seed.Piece("編集後の曲"));
        client.FailNextCall("Upsert", "practices");

        var cut = Render();
        var editButtons = cut.FindAll("button.iyk-btn-text");
        editButtons[1].Click();
        cut.Find("input[type=checkbox]").Change(true);
        cut.Find("button.iyk-btn-primary").Click();

        Assert.Contains("演奏予定曲の保存に失敗しました", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 録音リンクの保存に失敗した場合はエラーメッセージが表示される()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        js.Setup(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>())).ReturnsAsync("https://example.com/rec");
        client.FailNextCall("Upsert", "practices");

        var cut = Render();
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Contains("録音リンクの保存に失敗しました", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 注目設定の更新に失敗した場合はエラーメッセージが表示される()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", Recordings = [new() { Id = "r1", Url = "https://example.com/rec", IsFeatured = false }] }
        ]));
        client.FailNextCall("Upsert", "practices");

        var cut = Render();
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Contains("更新に失敗しました", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 鍵の受け取り状況の更新に失敗した場合はエラーメッセージが表示される()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), requiresKeyPickup: true));
        client.FailNextCall("Upsert", "practices");

        var cut = Render();
        cut.Find("button.iyk-btn-text-primary").Click();

        Assert.Contains("更新に失敗しました", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 削除に失敗した場合はエラーメッセージが表示される()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        js.Setup(j => j.InvokeAsync<bool>("confirm", It.IsAny<object?[]>())).ReturnsAsync(true);
        client.FailNextCall("Delete", "practices");

        var cut = Render();
        cut.Find("button.iyk-btn-outline-danger").Click();

        Assert.Contains("削除に失敗しました", cut.Find("p.error-text").TextContent);
        Assert.True(client.Contains("practices", "p1"));
    }

    [Fact]
    public void 登録済みのタイムスケジュールは開始時刻順で一覧表示される()
    {
        var (client, _, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), timeline:
        [
            new PracticeTimelineItem { StartTime = "19:00", EndTime = "19:30", Content = "自由合奏" },
            new PracticeTimelineItem { StartTime = "18:00", EndTime = "19:00", Content = "基礎合奏" }
        ]));

        var cut = Render();

        var titles = cut.FindAll("p.list-card-title").Select(e => e.TextContent).ToList();
        Assert.Equal(["18:00 〜 19:00", "19:00 〜 19:30"], titles);
        Assert.Contains("基礎合奏", cut.Markup);
    }

    [Fact]
    public void タイムスケジュール編集で項目を追加すると入力欄が増え削除ボタンで減らせる()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = Render();
        cut.Find("button.iyk-btn-text").Click();
        cut.Find("button.iyk-btn-block").Click();
        cut.Find("button.iyk-btn-block").Click();

        Assert.Equal(2, cut.FindAll("input[type=text][placeholder='例：基礎合奏']").Count);

        cut.FindAll("button.iyk-btn-text-danger")[0].Click();

        Assert.Single(cut.FindAll("input[type=text][placeholder='例：基礎合奏']"));
    }

    [Fact]
    public void 演奏予定曲編集パネルを開くと登録済みの曲にチェックが入っている()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        client.Seed("pieces", "pc1", Seed.Piece("曲A"));

        var cut = Render();
        cut.FindAll("button.iyk-btn-text")[1].Click();

        Assert.True(cut.Find("input[type=checkbox]").HasAttribute("checked"));
    }

    [Fact]
    public void 分割方式のある曲では参加者に上下ラベルが表示される()
    {
        var (client, _, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        client.Seed("pieces", "pc1", Seed.Piece("曲A", partAssignments:
        [
            new PiecePartAssignment { Part = PartType.Soprano, Division = PartDivision.UpperLower }
        ]));
        client.Seed("members", "me", Seed.Member("自分", PartType.Soprano, pieceParts:
        [
            new MemberPiecePart { PieceId = "pc1", SubPart = "ソプラノ上" }
        ]));
        client.Seed("attendances", "p1_me", Seed.Attendance("p1", "me", "自分", PartType.Soprano, AttendanceStatus.Attending));

        var cut = Render();

        var dotTexts = cut.FindAll("div.dot").Select(e => e.TextContent).ToList();
        Assert.Contains("上", dotTexts);
    }
}
