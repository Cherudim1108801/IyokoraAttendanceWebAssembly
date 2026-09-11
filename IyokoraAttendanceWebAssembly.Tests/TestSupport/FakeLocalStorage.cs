using Microsoft.JSInterop;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.TestSupport;

/// <summary>
/// localStorage をメモリ上の辞書で模倣する IJSInProcessRuntime のモックを作る。
/// iOS/Android のブラウザ端末を想定した LocalProfileStore の永続化ロジックをテストするために使用する。
/// </summary>
internal static class FakeLocalStorage
{
    public static IJSInProcessRuntime Create(Dictionary<string, string>? initial = null)
    {
        var backing = initial ?? [];
        var jsMock = new Mock<IJSInProcessRuntime>();
        jsMock
            .Setup(js => js.Invoke<string?>(It.Is<string>(id => id == "localStorage.getItem"), It.IsAny<object?[]>()))
            .Returns((string _, object?[] args) => backing.TryGetValue((string)args[0]!, out var v) ? v : null);
        jsMock
            .Setup(js => js.Invoke<object>(It.Is<string>(id => id == "localStorage.setItem"), It.IsAny<object?[]>()))
            .Returns((string _, object?[] args) =>
            {
                backing[(string)args[0]!] = (string)args[1]!;
                return null!;
            });
        jsMock
            .Setup(js => js.Invoke<object>(It.Is<string>(id => id == "localStorage.removeItem"), It.IsAny<object?[]>()))
            .Returns((string _, object?[] args) =>
            {
                backing.Remove((string)args[0]!);
                return null!;
            });

        return jsMock.Object;
    }
}
