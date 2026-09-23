namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>
/// Blazor WebAssembly はページをリロードしない限り新しいビルドを取得しない。そのため、タブを開いたまま
/// 新しいバージョンがデプロイされると、リロードするまで古い画面・古いロジックのまま動作し続けてしまう
/// （例：練習データの絞り込み方法やモデルが変わった場合、更新前のロジックのままサーバーへ問い合わせて
/// 意図しない結果になる）。このクラスは起動時に読み込んだビルドの内容を記録しておき、以降の内容と比較する
/// ことで新しいバージョンがデプロイされたことを検知し、利用者にリロードを促すために使う。
/// </summary>
public class AppUpdateWatcher(IBootManifestFetcher fetcher)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(10);

    private string? _initialManifest;
    private bool _checking;

    /// <summary>新しいバージョンの検知時に発火する。UI側はこれを購読して更新案内を表示する。</summary>
    public event Action? UpdateAvailable;

    /// <summary>新しいバージョンが検知済みかどうか。</summary>
    public bool IsUpdateAvailable { get; private set; }

    /// <summary>
    /// アプリ起動直後に一度だけ呼び出し、現在読み込まれているビルドの内容を記録する。
    /// 以降のバックグラウンド監視もここで開始する。
    /// </summary>
    public async Task CaptureCurrentVersionAsync()
    {
        if (_initialManifest is not null || IsUpdateAvailable)
            return;

        _initialManifest = await fetcher.FetchAsync();
        if (_initialManifest is not null)
            _ = RunPeriodicChecksAsync();
    }

    /// <summary>
    /// 新しいバージョンがデプロイされていないかを確認する。画面遷移のたびに呼び出される想定。
    /// 既に検知済み、または前回の確認が完了していない場合は何もしない。
    /// </summary>
    public async Task CheckForUpdateAsync()
    {
        if (IsUpdateAvailable || _initialManifest is null || _checking)
            return;

        _checking = true;
        try
        {
            var latest = await fetcher.FetchAsync();
            if (latest is not null && latest != _initialManifest)
            {
                IsUpdateAvailable = true;
                UpdateAvailable?.Invoke();
            }
        }
        finally
        {
            _checking = false;
        }
    }

    /// <summary>画面遷移が発生しない間も更新を検知できるよう、一定間隔でバックグラウンドで確認し続ける。</summary>
    private async Task RunPeriodicChecksAsync()
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (!IsUpdateAvailable && await timer.WaitForNextTickAsync())
            await CheckForUpdateAsync();
    }
}
