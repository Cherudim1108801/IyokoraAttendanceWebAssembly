using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Pages;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace IyokoraAttendanceWebAssembly.Tests.Pages;

public class PiecesTests : BunitContext
{
    private FakeFirestoreClient RegisterServices(Role role = Role.GeneralMember)
    {
        var client = new FakeFirestoreClient();
        Services.AddSingleton(new PieceService(new PieceRepository(client)));
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

        var cut = Render<Pieces>();

        Assert.Empty(cut.FindAll("button.iyk-btn-primary"));
        Assert.Empty(cut.FindAll("button.iyk-btn-text"));
        Assert.Empty(cut.FindAll("button.iyk-btn-text-danger"));
    }

    [Fact]
    public void 曲が無い場合は案内メッセージが表示される()
    {
        RegisterServices(Role.Admin);

        var cut = Render<Pieces>();

        Assert.Contains("登録されている曲はありません", cut.Markup);
    }

    [Fact]
    public void 非表示の曲は既定では表示されずチェックを入れると表示される()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("pieces", "p1", Seed.Piece("表示曲"));
        client.Seed("pieces", "p2", Seed.Piece("非表示曲", isArchived: true));

        var cut = Render<Pieces>();
        Assert.DoesNotContain("非表示曲", cut.Markup);

        cut.Find("input#showArchived").Change(true);

        Assert.Contains("非表示曲", cut.Markup);
    }

    [Fact]
    public void 管理者が曲名のみで曲を追加すると一覧に反映される()
    {
        RegisterServices(Role.Admin);
        var cut = Render<Pieces>();

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
        var cut = Render<Pieces>();

        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Contains("曲名を入力してください", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 管理者が非表示にするとその曲に非表示の表示が付く()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("pieces", "p1", Seed.Piece("曲A"));

        var cut = Render<Pieces>();
        cut.Find("input#showArchived").Change(true);
        cut.Find("button.iyk-btn-text").Click();

        Assert.Contains("（非表示）", cut.Find("p.list-card-title").TextContent);
    }

    [Fact]
    public void 管理者が削除すると一覧から消える()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("pieces", "p1", Seed.Piece("削除対象"));

        var cut = Render<Pieces>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Empty(cut.FindAll("div.list-card"));
        Assert.False(client.Contains("pieces", "p1"));
    }

    [Fact]
    public void パートを選択して曲を追加するとパート情報が一覧に反映される()
    {
        RegisterServices(Role.Admin);
        var cut = Render<Pieces>();

        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("input[type=text]").Input("新曲B");
        cut.Find("input[type=checkbox][id^=part_]").Change(true);
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.DoesNotContain("パート未設定", cut.Markup);
    }

    [Fact]
    public void 非表示の曲を再表示すると非表示の表示が消える()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("pieces", "p1", Seed.Piece("曲A", isArchived: true));

        var cut = Render<Pieces>();
        cut.Find("input#showArchived").Change(true);
        cut.Find("button.iyk-btn-text").Click();

        Assert.DoesNotContain("（非表示）", cut.Find("p.list-card-title").TextContent);
    }

    [Fact]
    public void 読み込みに失敗した場合はエラーメッセージが表示される()
    {
        var client = RegisterServices();
        client.FailNextCall("List", "pieces");

        var cut = Render<Pieces>();

        Assert.Contains("読み込みに失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Empty(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void 曲の追加に失敗した場合はエラーメッセージが表示される()
    {
        var client = RegisterServices(Role.Admin);
        client.FailNextCall("Upsert", "pieces");

        var cut = Render<Pieces>();
        cut.Find("button.iyk-btn-primary").Click();
        cut.Find("input[type=text]").Input("新曲A");
        cut.Find("div.highlight-card button.iyk-btn-primary").Click();

        Assert.Contains("曲の追加に失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Empty(cut.FindAll("div.list-card"));
    }

    [Fact]
    public void 非表示切り替えに失敗した場合はエラーメッセージが表示される()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("pieces", "p1", Seed.Piece("曲A"));
        client.FailNextCall("Upsert", "pieces");

        var cut = Render<Pieces>();
        cut.Find("button.iyk-btn-text").Click();

        Assert.Contains("更新に失敗しました", cut.Find("p.error-text").TextContent);
    }

    [Fact]
    public void 削除に失敗した場合はエラーメッセージが表示され一覧に残る()
    {
        var client = RegisterServices(Role.Admin);
        client.Seed("pieces", "p1", Seed.Piece("曲A"));
        client.FailNextCall("Delete", "pieces");

        var cut = Render<Pieces>();
        cut.Find("button.iyk-btn-text-danger").Click();

        Assert.Contains("削除に失敗しました", cut.Find("p.error-text").TextContent);
        Assert.Single(cut.FindAll("div.list-card"));
        Assert.True(client.Contains("pieces", "p1"));
    }
}
