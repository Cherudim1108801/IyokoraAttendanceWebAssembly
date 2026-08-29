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
builder.Services.AddScoped(sp => (IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>());
builder.Services.AddScoped<FirestoreClient>();
builder.Services.AddScoped<NameCipher>();
builder.Services.AddScoped<LocalProfileStore>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<PracticeService>();
builder.Services.AddScoped<AttendanceService>();
builder.Services.AddScoped<PieceService>();

await builder.Build().RunAsync();
