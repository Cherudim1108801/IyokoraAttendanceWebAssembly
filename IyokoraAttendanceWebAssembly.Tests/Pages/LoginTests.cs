using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class LoginTests : TestContext
{
    private static FirestoreDocument CreateMemberDoc(string id, string name, string loginId) => new()
    {
        Id = id,
        Fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["name"] = name,
            ["part"] = PartType.Soprano.ToString(),
            ["role"] = Role.GeneralMember.ToString(),
            ["loginId"] = loginId,
            ["pieceParts"] = new List<object?>(),
            ["updatedAt"] = DateTime.UtcNow
        }
    };

    private LocalProfileStore RegisterServices(IEnumerable<FirestoreDocument>? docs = null)
    {
        Services.AddSingleton(FakeMemberService.Create(docs));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        Services.AddSingleton(profile);
        return profile;
    }

    [Fact]
    public void 形式が不正なログインIDを入力すると通信せずにエラーメッセージが表示される()
    {
        RegisterServices();
        var cut = RenderComponent<Login>();

        cut.Find("input").Input("不正な値");
        cut.Find("button").Click();

        Assert.Contains("IDの形式が正しくありません", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 該当するログインIDが無い場合はエラーメッセージが表示される()
    {
        RegisterServices();
        var cut = RenderComponent<Login>();

        cut.Find("input").Input("IK9999");
        cut.Find("button").Click();

        Assert.Contains("該当するIDが見つかりませんでした", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 有効なログインIDを入力すると端末にプロフィールが保存されトップページへ遷移する()
    {
        var docs = new[] { CreateMemberDoc("m1", "山田 太郎", "IK1234") };
        var profile = RegisterServices(docs);
        var cut = RenderComponent<Login>();

        cut.Find("input").Input("ik1234");
        cut.Find("button").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.Equal("山田 太郎", profile.Name);
        Assert.Equal("m1", profile.MemberId);
        Assert.Equal("IK1234", profile.LoginId);
        Assert.EndsWith("localhost/", nav.Uri);
    }
}
