using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class DashboardTests : TestContext
{
    private (FakeFirestoreClient client, LocalProfileStore profile) RegisterServices(Role role = Role.GeneralMember)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(FakeMemberService.Create(client));
        Services.AddSingleton(new PracticeService(client));
        Services.AddSingleton(new AttendanceService(client));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        profile.MemberId = "me";
        profile.Name = "自分";
        profile.Part = PartType.Soprano;
        profile.Role = role;
        Services.AddSingleton(profile);
        return (client, profile);
    }

    [Fact]
    public void 次回の練習が無い場合は案内が表示される()
    {
        RegisterServices();

        var cut = RenderComponent<Dashboard>();

        Assert.Contains("次回の練習予定が登録されていません", cut.Markup);
    }

    [Fact]
    public void 一般団員には練習予定登録ボタンが表示されない()
    {
        RegisterServices(Role.GeneralMember);

        var cut = RenderComponent<Dashboard>();

        Assert.Empty(cut.FindAll("button.iyk-btn-primary"));
    }

    [Fact]
    public void 管理者には練習予定登録ボタンが表示される()
    {
        RegisterServices(Role.Admin);

        var cut = RenderComponent<Dashboard>();

        Assert.Single(cut.FindAll("button.iyk-btn-primary"));
    }

    [Fact]
    public void 次回の練習と出欠集計が表示される()
    {
        var (client, profile) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3), title: "次回練習"));
        client.Seed("members", "me", Seed.Member("自分", PartType.Soprano));
        client.Seed("members", "m2", Seed.Member("相方", PartType.Soprano));
        client.Seed("attendances", "p1_me", Seed.Attendance("p1", "me", "自分", PartType.Soprano, AttendanceStatus.Attending));

        var cut = RenderComponent<Dashboard>();

        Assert.Contains("次回練習", cut.Markup);
        Assert.Contains("合計 参加予定: 1 人", cut.Markup);
        Assert.Contains("回答済み: 1 人 (登録メンバー全 2 人)", cut.Markup);
    }

    [Fact]
    public void 出欠ボタンをタップすると出欠が保存され集計が更新される()
    {
        var (client, profile) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));
        client.Seed("members", "me", Seed.Member("自分", PartType.Soprano));

        var cut = RenderComponent<Dashboard>();
        cut.Find("button.attend-yes").Click();

        Assert.Contains("合計 参加予定: 1 人", cut.Markup);
        Assert.True(client.Contains("attendances", Attendance.BuildId("p1", "me")));
    }

    [Fact]
    public void ハイライトカードをタップするとタイムスケジュールのモーダルが開く()
    {
        var (client, _) = RegisterServices();
        client.Seed("practices", "p1", Seed.Practice(DateTime.Today.AddDays(3)));

        var cut = RenderComponent<Dashboard>();
        cut.Find("div.highlight-card").Click();

        Assert.Contains("タイムスケジュールは登録されていません", cut.Find("div.iyk-modal-panel").TextContent);

        cut.Find("button.iyk-btn-block").Click();
        Assert.Empty(cut.FindAll("div.iyk-modal-backdrop"));
    }
}
