using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class SchedulePollTests : BunitContext
{
    private (FakeFirestoreClient client, Mock<IJSRuntime> js) RegisterServices(Role role = Role.GeneralMember)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(new ScheduleCandidateService(new ScheduleCandidateRepository(client), new ScheduleVoteRepository(client)));
        Services.AddSingleton(new ScheduleVoteService(new ScheduleVoteRepository(client)));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        profile.MemberId = "me";
        profile.Name = "自分";
        profile.Role = role;
        Services.AddSingleton(profile);

        var jsMock = new Mock<IJSRuntime>();
        jsMock.Setup(j => j.InvokeAsync<bool>("confirm", It.IsAny<object?[]>())).ReturnsAsync(true);
        Services.AddSingleton(jsMock.Object);

        return (client, jsMock);
    }

    [Fact]
    public void 一般団員には追加ボタンや削除ボタンが表示されない()
    {
        var (client, _) = RegisterServices(Role.GeneralMember);
        client.Seed("scheduleCandidates", "c1", Seed.ScheduleCandidate(DateTime.Today.AddDays(3), TimeOfDay.Morning));

        var cut = Render<SchedulePoll>();

        Assert.Empty(cut.FindAll("button.iyk-btn-primary"));
        Assert.Empty(cut.FindAll("button.iyk-btn-text-danger"));
    }

    [Fact]
    public void 候補日が無い場合は案内メッセージが表示される()
    {
        RegisterServices(Role.Admin);

        var cut = Render<SchedulePoll>();

        Assert.Contains("登録されている日程候補はありません", cut.Markup);
    }

    [Fact]
    public void 管理者が候補日を追加すると一覧に反映される()
    {
        RegisterServices(Role.Admin);
        var cut = Render<SchedulePoll>();

        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Single(cut.FindAll("div.list-card"));
        Assert.Contains("午前", cut.Markup);
    }

    [Fact]
    public void 参加に投票すると集計と自分の投票状態が反映される()
    {
        var (client, _) = RegisterServices();
        client.Seed("scheduleCandidates", "c1", Seed.ScheduleCandidate(DateTime.Today.AddDays(3), TimeOfDay.Morning));

        var cut = Render<SchedulePoll>();
        cut.Find("button.attend-yes").Click();

        Assert.Contains("selected", cut.Find("button.attend-yes").GetAttribute("class"));
        Assert.Equal("1", cut.Find("p[style*='font-size:28px']").TextContent.Trim()[..1]);
        Assert.Contains("1 / 0", cut.Find("p.list-card-sub").TextContent);
        Assert.True(client.Contains("scheduleVotes", ScheduleVote.BuildId("c1", "me")));
    }

    [Fact]
    public void 管理者が削除すると一覧から消え投票も削除される()
    {
        var (client, js) = RegisterServices(Role.Admin);
        client.Seed("scheduleCandidates", "c1", Seed.ScheduleCandidate(DateTime.Today.AddDays(3), TimeOfDay.Morning));
        client.Seed("scheduleVotes", ScheduleVote.BuildId("c1", "m1"), Seed.ScheduleVote("c1", "m1", "メンバー1", AttendanceStatus.Attending));

        var cut = Render<SchedulePoll>();
        cut.Find("button.iyk-btn-text-danger").Click();

        js.Verify(j => j.InvokeAsync<bool>("confirm", It.IsAny<object?[]>()), Times.Once);
        Assert.Empty(cut.FindAll("div.list-card"));
        Assert.False(client.Contains("scheduleCandidates", "c1"));
        Assert.False(client.Contains("scheduleVotes", ScheduleVote.BuildId("c1", "m1")));
    }

    [Fact]
    public void 削除確認で拒否すると候補日も投票も削除されない()
    {
        var (client, js) = RegisterServices(Role.Admin);
        client.Seed("scheduleCandidates", "c1", Seed.ScheduleCandidate(DateTime.Today.AddDays(3), TimeOfDay.Morning));
        client.Seed("scheduleVotes", ScheduleVote.BuildId("c1", "m1"), Seed.ScheduleVote("c1", "m1", "メンバー1", AttendanceStatus.Attending));
        js.Setup(j => j.InvokeAsync<bool>("confirm", It.IsAny<object?[]>())).ReturnsAsync(false);

        var cut = Render<SchedulePoll>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Single(cut.FindAll("div.list-card"));
        Assert.True(client.Contains("scheduleCandidates", "c1"));
        Assert.True(client.Contains("scheduleVotes", ScheduleVote.BuildId("c1", "m1")));
    }

    [Fact]
    public void 読み込みに失敗した場合はエラーメッセージが表示される()
    {
        var (client, _) = RegisterServices();
        client.FailNextCall("Query", "scheduleCandidates");

        var cut = Render<SchedulePoll>();

        Assert.Contains("読み込みに失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Empty(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void 候補日の追加に失敗した場合はエラーメッセージが表示される()
    {
        var (client, _) = RegisterServices(Role.Admin);
        client.FailNextCall("Upsert", "scheduleCandidates");

        var cut = Render<SchedulePoll>();
        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Contains("候補日の追加に失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Empty(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void 削除に失敗した場合はエラーメッセージが表示され一覧に残る()
    {
        var (client, _) = RegisterServices(Role.Admin);
        client.Seed("scheduleCandidates", "c1", Seed.ScheduleCandidate(DateTime.Today.AddDays(3), TimeOfDay.Morning));
        client.FailNextCall("Delete", "scheduleCandidates");

        var cut = Render<SchedulePoll>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Contains("削除に失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Single(cut.FindAll("div.list-card"));
        Assert.True(client.Contains("scheduleCandidates", "c1"));
    }

    [Fact]
    public void 時間帯を選択して候補日を追加すると選択した時間帯で登録される()
    {
        RegisterServices(Role.Admin);
        var cut = Render<SchedulePoll>();

        cut.Find("button.iyk-btn-primary").Click();
        cut.FindAll("input[type=radio]")[1].Change(true);
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Contains("午後", cut.Markup);
        Assert.DoesNotContain("午前", cut.Markup);
    }

    [Fact]
    public void 投票の更新に失敗した場合はエラーメッセージが表示される()
    {
        var (client, _) = RegisterServices();
        client.Seed("scheduleCandidates", "c1", Seed.ScheduleCandidate(DateTime.Today.AddDays(3), TimeOfDay.Morning));
        client.FailNextCall("Upsert", "scheduleVotes");

        var cut = Render<SchedulePoll>();
        cut.Find("button.attend-yes").Click();

        Assert.Contains("投票の更新に失敗しました", cut.Find("p.error-text").TextContent);
    }
}
