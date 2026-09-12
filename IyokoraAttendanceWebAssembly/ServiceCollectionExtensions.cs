using IyokoraAttendanceWebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace IyokoraAttendanceWebAssembly;

/// <summary>
/// アプリ本体が利用するサービスの DI 登録。
/// Program.cs は WebAssemblyHostBuilder に依存し実際のブラウザ (browser-wasm) 上でしか動作しないため
/// 単体テストで直接実行できない（<see cref="Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyHostBuilder"/> 参照）。
/// 登録内容自体は単体テストで検証できるよう、この拡張メソッドに切り出している。
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // このプロジェクトは Blazor WebAssembly (browser-wasm) 専用のため、IJSRuntime は常に
        // IJSInProcessRuntime（同期呼び出し可能）で実装される。Blazor Server 等、別ホスティングモデルへ
        // 移植する場合はこのキャストが失敗するため、LocalProfileStore の同期 JS 呼び出しを見直すこと。
        services.AddScoped(sp => (IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>());
        services.AddScoped<IFirestoreClient, FirestoreClient>();
        services.AddScoped<IAppCheckTokenProvider, AppCheckTokenProvider>();
        services.AddScoped<NameCipher>();
        services.AddScoped<LocalProfileStore>();
        services.AddScoped<MemberService>();
        services.AddScoped<PracticeService>();
        services.AddScoped<AttendanceService>();
        services.AddScoped<PieceService>();
        services.AddScoped<ScheduleCandidateService>();
        services.AddScoped<ScheduleVoteService>();
        return services;
    }
}
