using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class PiecesTests : TestContext
{
    private FakeFirestoreClient RegisterServices(Role role = Role.GeneralMember)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(new PieceService(client));
        var profile = new LocalProfileStore(FakeLocalStorage.Create());
        profile.Role = role;
        Services.AddSingleton(profile);
        return client;
    }

    [Fact]
    public void 一般団員には追加ボタンや操作ボタンが表示されない()
    {
        var client = RegisterServices(Role.GeneralMember);
        client.Seed("pieces", "p1", Seed.Piece("曲A"));

        var cut = RenderComponent<Pieces>();

        Assert.Empty(cut.FindAll("button.iyk-btn-primary"));
        Assert.Empty(cut.FindAll("button.iyk-btn-text"));
        Assert.Empty(cut.FindAll("button.iyk-btn-text-danger"));
    }

    [Fact]
    public void 曲が無い場合は案内メッセージが表示される()
    {
        RegisterServices(Role.Admin);

        var cut = RenderComponent<Pieces>();

        Assert.Contains("登録されている曲はありません", cut.Markup);
    }

    [Fact]
    public void 非表示の曲は既定では表示されずチェックを入れると表示される()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("pieces", "p1", Seed.Piece("表示曲"));
        client.Seed("pieces", "p2", Seed.Piece("非表示曲", isArchived: true));

        var cut = RenderComponent<Pieces>();
        Assert.DoesNotContain("非表示曲", cut.Markup);

        cut.Find("input#showArchived").Change(true);

        Assert.Contains("非表示曲", cut.Markup);
    }

    [Fact]
    public void 管理者が曲名のみで曲を追加すると一覧に反映される()
    {
        RegisterServices(Role.Admin);
        var cut = RenderComponent<Pieces>();

        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("input[type=text]").Input("新曲A");
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Contains("新曲A", cut.Markup);
        Assert.Contains("パート未設定", cut.Markup);
        Assert.Single(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void 曲名が未入力の場合はエラーが表示され登録されない()
    {
        RegisterServices(Role.Admin);
        var cut = RenderComponent<Pieces>();

        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Contains("曲名を入力してください", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 管理者が非表示にするとその曲に非表示の表示が付く()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("pieces", "p1", Seed.Piece("曲A"));

        var cut = RenderComponent<Pieces>();
        cut.Find("input#showArchived").Change(true);
        cut.Find("button.iyk-btn-text").Click();

        Assert.Contains("（非表示）", cut.Find("p.list-card-title").TextContent);
    }

    [Fact]
    public void 管理者が削除すると一覧から消える()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("pieces", "p1", Seed.Piece("削除対象"));

        var cut = RenderComponent<Pieces>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Empty(cut.FindAll("div.list-card"));
        Assert.False(client.Contains("pieces", "p1"));
    }
}
