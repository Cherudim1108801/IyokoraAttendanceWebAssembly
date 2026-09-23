using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class SongParticipationTests
{
    private static SongParticipation Create(List<PracticeRecording> recordings) => new()
    {
        PieceId = "p1",
        Title = "曲A",
        Dots = [],
        Recordings = recordings
    };

    [Fact]
    public void 録音が未登録の場合はHasRecordingsがfalseになる()
    {
        var song = Create([]);

        Assert.False(song.HasRecordings);
    }

    [Fact]
    public void 録音が1件でも登録済みの場合はHasRecordingsがtrueになる()
    {
        var song = Create([new() { Id = "r1", Url = "https://example.com/rec", IsFeatured = false }]);

        Assert.True(song.HasRecordings);
    }

    [Fact]
    public void 同じ曲に複数件の録音を登録できる()
    {
        var song = Create([
            new() { Id = "r1", Url = "https://example.com/rec1", IsFeatured = false },
            new() { Id = "r2", Url = "https://example.com/rec2", IsFeatured = true }
        ]);

        Assert.Equal(2, song.Recordings.Count);
        Assert.Contains(song.Recordings, r => r is { Id: "r1", IsFeatured: false });
        Assert.Contains(song.Recordings, r => r is { Id: "r2", IsFeatured: true });
    }
}
