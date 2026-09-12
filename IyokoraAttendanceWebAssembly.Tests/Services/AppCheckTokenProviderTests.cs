using IyokoraAttendanceWebAssembly.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class AppCheckTokenProviderTests
{
    private static (AppCheckTokenProvider provider, Mock<IJSRuntime> js) CreateProvider()
    {
        var jsMock = new Mock<IJSRuntime>();

        // appCheckInit（InvokeVoidAsync 経由）は内部的に InvokeAsync<IJSVoidResult> を呼び出す。
        jsMock
            .Setup(j => j.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns(ValueTask.FromResult<IJSVoidResult>(null!));

        return (new AppCheckTokenProvider(jsMock.Object, NullLogger<AppCheckTokenProvider>.Instance), jsMock);
    }

    [Fact]
    public async Task トークン取得に成功した場合はその値を返す()
    {
        var (provider, js) = CreateProvider();
        js.Setup(j => j.InvokeAsync<string?>("appCheckGetToken", It.IsAny<object?[]>()))
            .ReturnsAsync("dummy-token");

        var token = await provider.GetTokenAsync();

        Assert.Equal("dummy-token", token);
    }

    [Fact]
    public async Task 初期化は複数回GetTokenAsyncを呼んでも1回しか実行されない()
    {
        var (provider, js) = CreateProvider();
        js.Setup(j => j.InvokeAsync<string?>("appCheckGetToken", It.IsAny<object?[]>()))
            .ReturnsAsync("dummy-token");

        await provider.GetTokenAsync();
        await provider.GetTokenAsync();

        js.Verify(j => j.InvokeAsync<IJSVoidResult>("appCheckInit", It.IsAny<object?[]>()), Times.Once);
    }

    [Fact]
    public async Task 初期化appCheckInitが失敗した場合は例外を投げずnullを返す()
    {
        var jsMock = new Mock<IJSRuntime>();
        jsMock
            .Setup(j => j.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns(new ValueTask<IJSVoidResult>(Task.FromException<IJSVoidResult>(new JSException("init failed"))));
        var provider = new AppCheckTokenProvider(jsMock.Object, NullLogger<AppCheckTokenProvider>.Instance);

        var token = await provider.GetTokenAsync();

        Assert.Null(token);
    }

    [Fact]
    public async Task 初期化に失敗した場合その後の呼び出しでも例外を投げずnullを返し続ける()
    {
        var jsMock = new Mock<IJSRuntime>();
        jsMock
            .Setup(j => j.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns(new ValueTask<IJSVoidResult>(Task.FromException<IJSVoidResult>(new JSException("init failed"))));
        var provider = new AppCheckTokenProvider(jsMock.Object, NullLogger<AppCheckTokenProvider>.Instance);

        var first = await provider.GetTokenAsync();
        var second = await provider.GetTokenAsync();

        Assert.Null(first);
        Assert.Null(second);
    }

    [Fact]
    public async Task トークン取得appCheckGetTokenが失敗した場合は例外を投げずnullを返す()
    {
        var (provider, js) = CreateProvider();
        js.Setup(j => j.InvokeAsync<string?>("appCheckGetToken", It.IsAny<object?[]>()))
            .Returns(new ValueTask<string?>(Task.FromException<string?>(new JSException("token fetch failed"))));

        var token = await provider.GetTokenAsync();

        Assert.Null(token);
    }
}
