using IyokoraAttendanceWebAssembly;
using IyokoraAttendanceWebAssembly.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Services
// このプロジェクトは Blazor WebAssembly (browser-wasm) 専用のため、IJSRuntime は常に
// IJSInProcessRuntime（同期呼び出し可能）で実装される。Blazor Server 等、別ホスティングモデルへ
// 移植する場合はこのキャストが失敗するため、LocalProfileStore の同期 JS 呼び出しを見直すこと。
builder.Services.AddScoped(sp => (IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>());
builder.Services.AddScoped<IFirestoreClient, FirestoreClient>();
builder.Services.AddScoped<NameCipher>();
builder.Services.AddScoped<LocalProfileStore>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<PracticeService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<PieceService>();
builder.Services.AddScoped<ScheduleCandidateService>();
builder.Services.AddScoped<ScheduleVoteService>();

await builder.Build().RunAsync();
