using Bunit;
using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Shared;

namespace IyokoraAttendanceWebAssembly.Tests.Shared;

public class PartSummaryPanelTests : BunitContext
{
    private static PartSummary CreateSummary(string label = "ソプラノ", IReadOnlyList<string>? attendees = null) => new()
    {
        Part = PartType.Soprano,
        Label = label,
        ColorHex = "#FF6B9D",
        CardBackgroundColorHex = "#FFCCFF",
        AttendingCount = attendees?.Count ?? 0,
        MemberCount = 5,
        AttendeeNames = attendees ?? []
    };

    [Fact]
    public void パートの数だけカードが表示され初期状態ではモーダルは表示されない()
    {
        var cut = Render<PartSummaryPanel>(p => p.Add(x => x.Summaries, [CreateSummary("ソプラノ"), CreateSummary("アルト")]));

        Assert.Equal(2, cut.FindAll("div.part-card").Count);
        Assert.Empty(cut.FindAll("div.iyk-modal-backdrop"));
    }

    [Fact]
    public void カードをタップすると参加者一覧のモーダルが表示される()
    {
        var cut = Render<PartSummaryPanel>(p => p.Add(x => x.Summaries, [CreateSummary(attendees: ["田中", "佐藤"])]));

        cut.Find("div.part-card").Click();

        var items = cut.FindAll("ul.attendee-list li");
        Assert.Equal(["田中", "佐藤"], items.Select(i => i.TextContent));
    }

    [Fact]
    public void 参加予定メンバーがいない場合は案内メッセージが表示される()
    {
        var cut = Render<PartSummaryPanel>(p => p.Add(x => x.Summaries, [CreateSummary(attendees: [])]));

        cut.Find("div.part-card").Click();

        Assert.Contains("参加予定のメンバーはいません", cut.Find("div.iyk-modal-panel").TextContent);
    }

    [Fact]
    public void 閉じるボタンをタップするとモーダルが閉じる()
    {
        var cut = Render<PartSummaryPanel>(p => p.Add(x => x.Summaries, [CreateSummary(attendees: ["田中"])]));
        cut.Find("div.part-card").Click();

        cut.Find("button.iyk-btn-block").Click();

        Assert.Empty(cut.FindAll("div.iyk-modal-backdrop"));
    }

    [Fact]
    public void タイトル未指定時は既定のタイトルが表示される()
    {
        var cut = Render<PartSummaryPanel>(p => p.Add(x => x.Summaries, [CreateSummary()]));

        Assert.Equal("パート別 参加予定人数", cut.Find("h3.section-title").TextContent);
    }
}
