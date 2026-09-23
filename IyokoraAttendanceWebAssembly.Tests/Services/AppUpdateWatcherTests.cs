using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class AppUpdateWatcherTests
{
    [Fact]
    public async Task 起動時と同じ内容のままなら更新は検知されない()
    {
        var fetcher = new FakeBootManifestFetcher { Manifest = "v1" };
        var watcher = new AppUpdateWatcher(fetcher);
        await watcher.CaptureCurrentVersionAsync();

        await watcher.CheckForUpdateAsync();

        Assert.False(watcher.IsUpdateAvailable);
    }

    [Fact]
    public async Task 起動後に内容が変わると更新が検知されイベントが発火する()
    {
        var fetcher = new FakeBootManifestFetcher { Manifest = "v1" };
        var watcher = new AppUpdateWatcher(fetcher);
        await watcher.CaptureCurrentVersionAsync();

        var raised = false;
        watcher.UpdateAvailable += () => raised = true;
        fetcher.Manifest = "v2";
        await watcher.CheckForUpdateAsync();

        Assert.True(watcher.IsUpdateAvailable);
        Assert.True(raised);
    }

    [Fact]
    public async Task 取得に失敗した場合は更新なしとして扱われる()
    {
        var fetcher = new FakeBootManifestFetcher { Manifest = "v1" };
        var watcher = new AppUpdateWatcher(fetcher);
        await watcher.CaptureCurrentVersionAsync();

        fetcher.Manifest = null;
        await watcher.CheckForUpdateAsync();

        Assert.False(watcher.IsUpdateAvailable);
    }

    [Fact]
    public async Task 起動時の取得に失敗した場合はその後の確認も行われない()
    {
        var fetcher = new FakeBootManifestFetcher { Manifest = null };
        var watcher = new AppUpdateWatcher(fetcher);
        await watcher.CaptureCurrentVersionAsync();

        fetcher.Manifest = "v2";
        await watcher.CheckForUpdateAsync();

        Assert.False(watcher.IsUpdateAvailable);
    }
}
