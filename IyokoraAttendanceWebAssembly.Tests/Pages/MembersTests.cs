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

public class MembersTests : BunitContext
{
    private (FakeFirestoreClient client, LocalProfileStore profile) RegisterServices(Role role = Role.Admin, bool confirmDelete = true)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(FakeMemberService.Create(client));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        profile.Role = role;
        Services.AddSingleton(profile);

        var jsMock = new Mock<IJSRuntime>();
        jsMock
            .Setup(js => js.InvokeAsync<bool>("confirm", It.IsAny<object?[]>()))
            .ReturnsAsync(confirmDelete);
        Services.AddSingleton(jsMock.Object);

        return (client, profile);
    }

    [Fact]
    public void 一般団員がアクセスするとホームへリダイレクトされ内容は表示されない()
    {
        RegisterServices(Role.GeneralMember);

        var cut = Render<Members>();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("localhost/", nav.Uri);
        Assert.Empty(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void 団員が登録されていない場合は案内メッセージが表示される()
    {
        RegisterServices(Role.Admin);

        var cut = Render<Members>();

        Assert.Contains("登録されている団員はいません", cut.Markup);
    }

    [Fact]
    public void 管理者には団員のログインIDと名前と最終更新日時が一覧表示される()
    {
        var (client, _) = RegisterServices(Role.Admin);
        var updatedAt = new DateTime(2026, 1, 2, 3, 4, 0, DateTimeKind.Utc);
        var fields = Seed.Member("山田 太郎", loginId: "IK0001");
        fields["updatedAt"] = updatedAt;
        client.Seed("members", "m1", fields);

        var cut = Render<Members>();

        Assert.Contains("山田 太郎", cut.Markup);
        Assert.Contains("IK0001", cut.Markup);
        Assert.Contains(updatedAt.ToString("yyyy/MM/dd HH:mm"), cut.Markup);
    }

    [Fact]
    public void 削除ボタンで確認ダイアログを承認すると団員が削除される()
    {
        var (client, _) = RegisterServices(Role.Admin, confirmDelete: true);
        client.Seed("members", "m1", Seed.Member("削除対象", loginId: "IK0001"));

        var cut = Render<Members>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Empty(cut.FindAll("div.list-card"));
        Assert.False(client.Contains("members", "m1"));
    }

    [Fact]
    public void 削除ボタンで確認ダイアログを拒否すると団員は削除されない()
    {
        var (client, _) = RegisterServices(Role.Admin, confirmDelete: false);
        client.Seed("members", "m1", Seed.Member("残す対象", loginId: "IK0001"));

        var cut = Render<Members>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Single(cut.FindAll("div.list-card"));
        Assert.True(client.Contains("members", "m1"));
    }

    [Fact]
    public void 読み込みに失敗した場合はエラーメッセージが表示される()
    {
        var (client, _) = RegisterServices(Role.Admin);
        client.FailNextCall("Query", "members");

        var cut = Render<Members>();

        Assert.Contains("読み込みに失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Empty(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void 削除に失敗した場合はエラーメッセージが表示され一覧に残る()
    {
        var (client, _) = RegisterServices(Role.Admin, confirmDelete: true);
        client.Seed("members", "m1", Seed.Member("曲A", loginId: "IK0001"));
        client.FailNextCall("Delete", "members");

        var cut = Render<Members>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Contains("削除に失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Single(cut.FindAll("div.list-card"));
        Assert.True(client.Contains("members", "m1"));
    }
}
