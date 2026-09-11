using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
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
        Services.AddSingleton(new PracticeService(client));
        Services.AddSingleton(FakeMemberService.Create(client));
        Services.AddSingleton(new AttendanceService(client));
        Services.AddSingleton(new PieceService(client));

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
    public void 管理者が録音リンクを登録すると音源リンクが表示される()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        js.Setup(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>())).ReturnsAsync("https://example.com/rec");

        var cut = Render();
        // [0]=タイムスケジュール編集 [1]=演奏予定曲編集 [2]=この曲の録音リンク編集(録音未登録のため注目ボタンは無い)
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Contains("録音を聴く", cut.Markup);
        Assert.Contains("★ 注目に設定", cut.Markup);
    }

    [Fact]
    public void 不正な形式の録音リンクを入力するとエラーが表示される()
    {
        var (client, _, js) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A" }
        ]));
        js.Setup(j => j.InvokeAsync<string?>("prompt", It.IsAny<object?[]>())).ReturnsAsync("不正なリンク");

        var cut = Render();
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Contains("リンクの形式が正しくありません", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 録音済みの曲を注目に設定すると表示が切り替わる()
    {
        var (client, _, _) = RegisterServices(Role.Admin);
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), pieces:
        [
            new() { PieceId = "pc1", Title = "曲A", RecordingUrl = "https://example.com/rec", IsFeatured = false }
        ]));

        var cut = Render();
        Assert.Contains("★ 注目に設定", cut.Markup);

        // [0]=タイムスケジュール編集 [1]=演奏予定曲編集 [2]=★ 注目に設定
        cut.FindAll("button.iyk-btn-text")[2].Click();

        Assert.Contains("★ 注目解除", cut.Markup);
        Assert.DoesNotContain("★ 注目に設定", cut.Markup);
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
}
