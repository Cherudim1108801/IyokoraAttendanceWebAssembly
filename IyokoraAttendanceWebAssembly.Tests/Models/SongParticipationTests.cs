using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class SongParticipationTests
{
    private static SongParticipation Create(string? recordingUrl, bool isFeatured) => new()
    {
        PieceId = "p1",
        Title = "曲A",
        Dots = [],
        RecordingUrl = recordingUrl,
        IsFeatured = isFeatured
    };

    [Fact]
    public void 録音URLが未登録の場合はHasRecordingUrlがfalseになる()
    {
        var song = Create(recordingUrl: null, isFeatured: false);

        Assert.False(song.HasRecordingUrl);
    }

    [Fact]
    public void 録音URLが登録済みの場合はHasRecordingUrlがtrueになる()
    {
        var song = Create(recordingUrl: "https://example.com/rec", isFeatured: false);

        Assert.True(song.HasRecordingUrl);
    }

    [Fact]
    public void 録音未登録の場合は強調表示のオンオフどちらも提示できない()
    {
        var song = Create(recordingUrl: null, isFeatured: false);

        Assert.False(song.CanToggleFeaturedOn);
        Assert.False(song.CanToggleFeaturedOff);
    }

    [Fact]
    public void 録音登録済みで未強調の場合は強調オンのみ提示できる()
    {
        var song = Create(recordingUrl: "https://example.com/rec", isFeatured: false);

        Assert.True(song.CanToggleFeaturedOn);
        Assert.False(song.CanToggleFeaturedOff);
    }

    [Fact]
    public void 録音登録済みで強調中の場合は強調オフのみ提示できる()
    {
        var song = Create(recordingUrl: "https://example.com/rec", isFeatured: true);

        Assert.False(song.CanToggleFeaturedOn);
        Assert.True(song.CanToggleFeaturedOff);
    }
}
