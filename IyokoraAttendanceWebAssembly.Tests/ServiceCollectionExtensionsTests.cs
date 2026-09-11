using IyokoraAttendanceWebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests;

/// <summary>
/// Program.cs（グローバル名前空間のトップレベルステートメント）は WebAssemblyHostBuilder に依存し
/// 実際のブラウザ (browser-wasm) 上でしか実行できないため、そこから切り出した
/// <see cref="ServiceCollectionExtensions.AddAppServices"/> の DI 登録内容を検証する。
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new HttpClient());
        // IJSInProcessRuntime へのキャストが行われるため、両方を実装するモックを IJSRuntime として登録する。
        services.AddSingleton<IJSRuntime>(new Mock<IJSInProcessRuntime>().Object);

        services.AddAppServices();

        return services.BuildServiceProvider();
    }

    [Fact]
    public void IFirestoreClientはFirestoreClientとして解決される()
    {
        var provider = BuildProvider();

        var client = provider.GetRequiredService<IFirestoreClient>();

        Assert.IsType<FirestoreClient>(client);
    }

    [Fact]
    public void IJSRuntimeからIJSInProcessRuntimeへキャストされて解決される()
    {
        var provider = BuildProvider();

        var jsRuntime = provider.GetRequiredService<IJSInProcessRuntime>();

        Assert.NotNull(jsRuntime);
    }

    [Theory]
    [InlineData(typeof(NameCipher))]
    [InlineData(typeof(LocalProfileStore))]
    [InlineData(typeof(MemberService))]
    [InlineData(typeof(PracticeService))]
    [InlineData(typeof(AttendanceService))]
    [InlineData(typeof(PieceService))]
    [InlineData(typeof(ScheduleCandidateService))]
    [InlineData(typeof(ScheduleVoteService))]
    public void アプリで使用する各サービスが解決できる(Type serviceType)
    {
        var provider = BuildProvider();

        var service = provider.GetRequiredService(serviceType);

        Assert.NotNull(service);
    }

    [Fact]
    public void 登録されるサービスは全てScopedとして登録される()
    {
        var services = new ServiceCollection().AddAppServices();

        Assert.All(services, descriptor => Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));
    }
}
