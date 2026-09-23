using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Tests.Models;

public class PracticeRecordingTests
{
    [Fact]
    public void 名前が未設定の場合は既定の文言がDisplayNameになる()
    {
        var recording = new PracticeRecording { Id = "r1", Url = "https://example.com/rec" };

        Assert.Equal("録音を聴く", recording.DisplayName);
    }

    [Fact]
    public void 名前が設定されている場合はその名前がDisplayNameになる()
    {
        var recording = new PracticeRecording { Id = "r1", Name = "本番前通し", Url = "https://example.com/rec" };

        Assert.Equal("本番前通し", recording.DisplayName);
    }
}
