using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>
/// Firebase App Check（<c>wwwroot/js/appCheck.js</c> 経由）のトークンを取得する。
/// 初期化・トークン取得のいずれも失敗しうる（reCAPTCHA 未設定、ネットワーク障害等）が、
/// その場合は例外を投げず <c>null</c> を返し、呼び出し元をヘッダー省略にフォールバックさせる
/// （App Check はあくまで追加の防御層であり、これ単体の不調でアプリ全体を止めない）。
/// </summary>
public class AppCheckTokenProvider(IJSRuntime js, ILogger<AppCheckTokenProvider> logger) : IAppCheckTokenProvider
{
    private Task? _initializeTask;

    public async ValueTask<string?> GetTokenAsync()
    {
        try
        {
            await EnsureInitializedAsync();
            return await js.InvokeAsync<string?>("appCheckGetToken");
        }
        catch (JSException ex)
        {
            logger.LogWarning(ex, "App Check トークンの取得に失敗したため、ヘッダーなしでリクエストします。");
            return null;
        }
    }

    private Task EnsureInitializedAsync() => _initializeTask ??= InitializeAsync();

    private Task InitializeAsync() => js.InvokeVoidAsync(
        "appCheckInit",
        new { apiKey = FirebaseOptions.ApiKey, projectId = FirebaseOptions.ProjectId, appId = FirebaseOptions.AppId },
        FirebaseOptions.RecaptchaSiteKey).AsTask();
}
