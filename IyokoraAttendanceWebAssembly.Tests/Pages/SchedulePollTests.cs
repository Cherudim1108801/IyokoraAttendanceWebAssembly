using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class SchedulePollTests : TestContext
{
    private FakeFirestoreClient RegisterServices(Role role = Role.GeneralMember)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(new ScheduleCandidateService(client));
        Services.AddSingleton(new ScheduleVoteService(client));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        profile.MemberId = "me";
        profile.Name = "自分";
        profile.Role = role;
        Services.AddSingleton(profile);
        return client;
    }

    [Fact]
    public void 一般団員には追加ボタンや削除ボタンが表示されない()
    {
        var client = RegisterServices(Role.GeneralMember);
        client.Seed("scheduleCandidates", "c1", Seed.ScheduleCandidate(DateTime.Today.AddDays(3), TimeOfDay.Morning));

        var cut = RenderComponent<SchedulePoll>();

        Assert.Empty(cut.FindAll("button.iyk-btn-primary"));
        Assert.Empty(cut.FindAll("button.iyk-btn-text-danger"));
    }

    [Fact]
    public void 候補日が無い場合は案内メッセージが表示される()
    {
        RegisterServices(Role.Admin);

        var cut = RenderComponent<SchedulePoll>();

        Assert.Contains("登録されている日程候補はありません", cut.Markup);
    }

    [Fact]
    public void 管理者が候補日を追加すると一覧に反映される()
    {
        RegisterServices(Role.Admin);
        var cut = RenderComponent<SchedulePoll>();

        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Single(cut.FindAll("div.list-card"));
        Assert.Contains("午前", cut.Markup);
    }

    [Fact]
    public void 参加に投票すると集計と自分の投票状態が反映される()
    {
        var client = RegisterServices();
        client.Seed("scheduleCandidates", "c1", Seed.ScheduleCandidate(DateTime.Today.AddDays(3), TimeOfDay.Morning));

        var cut = RenderComponent<SchedulePoll>();
        cut.Find("button.attend-yes").Click();

        Assert.Contains("selected", cut.Find("button.attend-yes").GetAttribute("class"));
        Assert.Equal("1", cut.Find("p[style*='font-size:28px']").TextContent.Trim()[..1]);
        Assert.Contains("1 / 0", cut.Find("p.list-card-sub").TextContent);
        Assert.True(client.Contains("scheduleVotes", ScheduleVote.BuildId("c1", "me")));
    }

    [Fact]
    public void 管理者が削除すると一覧から消える()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("scheduleCandidates", "c1", Seed.ScheduleCandidate(DateTime.Today.AddDays(3), TimeOfDay.Morning));

        var cut = RenderComponent<SchedulePoll>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Empty(cut.FindAll("div.list-card"));
        Assert.False(client.Contains("scheduleCandidates", "c1"));
    }
}
