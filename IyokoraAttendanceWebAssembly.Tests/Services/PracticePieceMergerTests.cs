using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class PracticePieceMergerTests
{
    [Fact]
    public void 既存の演奏予定曲は録音一覧を維持したまま残る()
    {
        var existing = new List<PracticePieceRef>
        {
            new() { PieceId = "piece1", Title = "曲A", Recordings = [new() { Id = "r1", Url = "https://example.com/rec", IsFeatured = true }] }
        };
        var selections = new List<PieceSelectionInput>
        {
            new() { PieceId = "piece1", Title = "曲A", IsSelected = true }
        };

        var merged = PracticePieceMerger.MergeSelectedPieces(existing, selections);

        var result = Assert.Single(merged);
        var recording = Assert.Single(result.Recordings);
        Assert.Equal("https://example.com/rec", recording.Url);
        Assert.True(recording.IsFeatured);
    }

    [Fact]
    public void 新たに選択した曲は録音未登録で追加される()
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
        Assert.Empty(result.Recordings);
    }

    [Fact]
    public void 選択が外された既存の曲は結果に含まれない()
    {
        var existing = new List<PracticePieceRef>
        {
            new() { PieceId = "piece1", Title = "曲A", Recordings = [new() { Id = "r1", Url = "https://example.com", IsFeatured = true }] }
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
            new() { PieceId = "piece1", Title = "曲A", Recordings = [new() { Id = "r1", Url = "https://example.com", IsFeatured = true }] }
        };
        var selections = new List<PieceSelectionInput>
        {
            new() { PieceId = "piece1", Title = "曲A", IsSelected = true },
            new() { PieceId = "piece2", Title = "曲B", IsSelected = true },
            new() { PieceId = "piece3", Title = "曲C", IsSelected = false }
        };

        var merged = PracticePieceMerger.MergeSelectedPieces(existing, selections);

        Assert.Equal(["piece1", "piece2"], merged.Select(p => p.PieceId));
        Assert.Equal("https://example.com", merged[0].Recordings.Single().Url);
        Assert.Empty(merged[1].Recordings);
    }
}
