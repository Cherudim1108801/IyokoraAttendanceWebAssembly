using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class PracticePieceMergerTests
{
    [Fact]
    public void 既存の演奏予定曲は録音リンクと強調表示の設定を維持したまま残る()
    {
        var existing = new List<PracticePieceRef>
        {
            new() { PieceId = "piece1", Title = "曲A", RecordingUrl = "https://example.com/rec", IsFeatured = true }
        };
        var selections = new List<PieceSelectionInput>
        {
            new() { PieceId = "piece1", Title = "曲A", IsSelected = true }
        };

        var merged = PracticePieceMerger.MergeSelectedPieces(existing, selections);

        var result = Assert.Single(merged);
        Assert.Equal("https://example.com/rec", result.RecordingUrl);
        Assert.True(result.IsFeatured);
    }

    [Fact]
    public void 新たに選択した曲は録音リンク未登録かつ強調表示なしで追加される()
    {
        var existing = new List<PracticePieceRef>();
        var selections = new List<PieceSelectionInput>
        {
            new() { PieceId = "piece1", Title = "曲A", IsSelected = true }
        };

        var merged = PracticePieceMerger.MergeSelectedPieces(existing, selections);

        var result = Assert.Single(merged);
        Assert.Equal("piece1", result.PieceId);
        Assert.Equal("曲A", result.Title);
        Assert.Null(result.RecordingUrl);
        Assert.False(result.IsFeatured);
    }

    [Fact]
    public void 選択が外された既存の曲は結果に含まれない()
    {
        var existing = new List<PracticePieceRef>
        {
            new() { PieceId = "piece1", Title = "曲A", RecordingUrl = "https://example.com", IsFeatured = true }
        };
        var selections = new List<PieceSelectionInput>
        {
            new() { PieceId = "piece1", Title = "曲A", IsSelected = false }
        };

        var merged = PracticePieceMerger.MergeSelectedPieces(existing, selections);

        Assert.Empty(merged);
    }

    [Fact]
    public void 選択されなかった曲を除き既存分と新規分が両方含まれる()
    {
        var existing = new List<PracticePieceRef>
        {
            new() { PieceId = "piece1", Title = "曲A", RecordingUrl = "https://example.com", IsFeatured = true }
        };
        var selections = new List<PieceSelectionInput>
        {
            new() { PieceId = "piece1", Title = "曲A", IsSelected = true },
            new() { PieceId = "piece2", Title = "曲B", IsSelected = true },
            new() { PieceId = "piece3", Title = "曲C", IsSelected = false }
        };

        var merged = PracticePieceMerger.MergeSelectedPieces(existing, selections);

        Assert.Equal(["piece1", "piece2"], merged.Select(p => p.PieceId));
        Assert.Equal("https://example.com", merged[0].RecordingUrl);
        Assert.Null(merged[1].RecordingUrl);
    }
}
