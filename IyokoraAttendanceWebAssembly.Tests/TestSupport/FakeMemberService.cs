using IyokoraAttendanceWebAssembly.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.TestSupport;

/// <summary>
/// JS Interop への依存（氏名の暗号化）をモックした MemberService を組み立てる（<see cref="MemberService"/> 自体はモック化しない）。
/// 画面（Pages）のテストで、氏名の暗号化ロジック自体は対象外としつつ MemberService の実際の挙動を検証するために使用する。
/// </summary>
internal static class FakeMemberService
{
    public static MemberService Create(IFirestoreClient? client = null)
    {
        var jsRuntimeMock = new Mock<IJSRuntime>();
        jsRuntimeMock
            .Setup(js => js.InvokeAsync<string>(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string _, object?[] args) => ValueTask.FromResult((string)args[1]!));
        jsRuntimeMock
            .Setup(js => js.InvokeAsync<NameDecryptResult>(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string _, object?[] args) => ValueTask.FromResult(new NameDecryptResult((string)args[1]!, WasLegacyFormat: false)));

        return new MemberService(client ?? new FakeFirestoreClient(), new NameCipher(jsRuntimeMock.Object), NullLogger<MemberService>.Instance);
    }
}
