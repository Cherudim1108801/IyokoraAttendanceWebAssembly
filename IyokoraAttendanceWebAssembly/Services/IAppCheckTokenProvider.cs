namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>
/// <see cref="AppCheckTokenProvider"/> の抽象。<see cref="FirestoreClient"/> がこれに依存することで、
/// 単体テストで Moq 等によるモックに差し替えられるようにする。
/// </summary>
public interface IAppCheckTokenProvider
{
    /// <summary>
    /// Firebase App Check のトークンを取得する。App Check が未設定（<see cref="FirebaseOptions.RecaptchaSiteKey"/>
    /// が空）の場合や取得に失敗した場合は <c>null</c> を返す。呼び出し元はその場合、
    /// トークンなしでリクエストを送る（従来どおりの挙動）。
    /// </summary>
    ValueTask<string?> GetTokenAsync();
}
