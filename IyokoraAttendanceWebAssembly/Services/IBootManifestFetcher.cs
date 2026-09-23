namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>現在サーバーに配置されている Blazor WebAssembly のビルド内容を取得する。<see cref="AppUpdateWatcher"/> が利用する。</summary>
public interface IBootManifestFetcher
{
    /// <summary>
    /// ビルドの内容を一意に表す文字列（<c>_framework/blazor.boot.json</c> の内容）を取得する。
    /// 取得に失敗した場合は null を返す。
    /// </summary>
    Task<string?> FetchAsync(CancellationToken ct = default);
}
