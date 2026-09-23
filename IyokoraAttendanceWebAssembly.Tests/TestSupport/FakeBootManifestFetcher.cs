using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Tests.TestSupport;

/// <summary>
/// 実際に <c>_framework/blazor.boot.json</c> を取得せず、テストから内容を差し替えられる
/// <see cref="IBootManifestFetcher"/> の実装。デプロイによってビルド内容が変わる様子を模倣する。
/// </summary>
internal class FakeBootManifestFetcher : IBootManifestFetcher
{
    public string? Manifest { get; set; } = "v1";

    public Task<string?> FetchAsync(CancellationToken ct = default) => Task.FromResult(Manifest);
}
