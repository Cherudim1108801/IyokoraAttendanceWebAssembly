using System.Security.Cryptography;
using System.Text;
using Microsoft.JSInterop;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>
/// Firestore に保存するメンバー氏名を AES-256-CBC で暗号化・復号する。
/// このアプリはログイン機能を持たず全端末が同じデータへアクセスするため、
/// 鍵は端末間で共有できるようアプリ内に固定で埋め込んでいる
/// （Firestore コンソール等で氏名が平文表示されるのを防ぐ難読化目的であり、
/// アプリのソースやビルド成果物を解析できる相手からの秘匿は保証しない）。
///
/// Blazor WebAssembly (browser-wasm) には System.Security.Cryptography.Aes の
/// ネイティブ実装が存在しないため、実際の暗号化・復号はブラウザ標準の
/// SubtleCrypto (wwwroot/js/nameCipher.js) 経由で行う。
/// </summary>
public class NameCipher(IJSRuntime js)
{
    private static readonly string KeyBase64 =
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("IyokoraAttendanceApp:Member.Name:v1")));

    /// <summary>氏名を暗号化し、IV を先頭に付与した Base64 文字列を返す。</summary>
    public ValueTask<string> EncryptAsync(string plainText) =>
        js.InvokeAsync<string>("nameCipherEncrypt", KeyBase64, plainText);

    /// <summary>
    /// <see cref="EncryptAsync"/> で生成された文字列を復号する。
    /// 暗号化導入前に保存された平文データが残っている場合に備え、
    /// 復号できない値はそのまま平文として返す。
    /// </summary>
    public ValueTask<string> DecryptOrPlainAsync(string value) =>
        string.IsNullOrEmpty(value)
            ? ValueTask.FromResult(value)
            : js.InvokeAsync<string>("nameCipherDecryptOrPlain", KeyBase64, value);
}
