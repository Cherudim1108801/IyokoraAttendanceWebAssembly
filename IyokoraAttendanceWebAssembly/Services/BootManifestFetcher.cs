namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>
/// <c>_framework/blazor.boot.json</c>（アセンブリ一覧とその配置内容を記述するファイル。新しいビルドが
/// デプロイされるたびに内容が変わる）をホスト自身から取得する。<see cref="IBootManifestFetcher"/> の実装。
/// </summary>
public class BootManifestFetcher(HttpClient http) : IBootManifestFetcher
{
    public async Task<string?> FetchAsync(CancellationToken ct = default)
    {
        try
        {
            // ブラウザや中継サーバーに古い内容をキャッシュされたままにしないよう、毎回異なるクエリを付与する。
            using var request = new HttpRequestMessage(HttpMethod.Get, $"_framework/blazor.boot.json?v={DateTime.UtcNow.Ticks}");
            using var response = await http.SendAsync(request, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(ct) : null;
        }
        catch
        {
            // 更新確認はあくまで付加的な機能であり、失敗しても通常の操作を妨げてはならない。
            return null;
        }
    }
}
