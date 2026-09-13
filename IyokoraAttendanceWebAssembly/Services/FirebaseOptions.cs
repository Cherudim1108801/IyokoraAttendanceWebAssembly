namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>
/// Firebase プロジェクトの接続設定。
/// Firebase コンソールでプロジェクトを作成し、Firestore を「テストモード」または
/// 下記ルールで有効化したうえで、ProjectId を書き換えてください。
///
/// このアプリはログイン機能を持たない前提のため、Firestore のセキュリティルールは
/// 認証なしでの読み書きを許可する必要があります（下記はその最小構成の例です）。
/// データは誰でも読み書きできる状態になる点に注意してください。
///
/// rules_version = '2';
/// service cloud.firestore {
///   match /databases/{database}/documents {
///     match /{document=**} {
///       allow read, write: if true;
///     }
///   }
/// }
/// </summary>
public static class FirebaseOptions
{
    /// <summary>Firebase コンソールの「プロジェクト設定」に表示されるプロジェクト ID。</summary>
    public const string ProjectId = "iyokoraattendanceapp";

    /// <summary>
    /// 同じ Firestore プロジェクトを複数の団体で使い回す場合に、データを分けるための任意の識別子。
    /// 単一団体での利用であれば既定値のままで問題ありません。
    /// </summary>
    public const string GroupId = "default";

    /// <summary>
    /// ログインID（<see cref="LoginIdGenerator"/> 参照）の先頭に付与する、団体ごとのアルファベット文字列。
    /// <see cref="GroupId"/> と同様、団体ごとに異なる値をビルド時に設定することを想定している。
    /// </summary>
    public const string LoginIdPrefix = "IC";

    /// <summary>
    /// Firebase Web アプリの apiKey。Firebase コンソール「プロジェクトの設定 &gt; 全般 &gt; マイアプリ」の
    /// SDK構成スニペットに表示される値（未登録の場合はまず Web アプリを追加すること）。
    /// この値自体は公開情報（クライアントに埋め込まれる想定のキー）であり、Firestore 側のアクセス制御は
    /// 引き続きセキュリティルール（および <see cref="RecaptchaSiteKey"/> による App Check）が担う。
    /// App Check の初期化にのみ使用する。
    /// </summary>
    public const string ApiKey = "AIzaSyBAh2Oqqe5IBz7qz4Aznnpmf2m41hOJ4Oc";

    /// <summary>SDK構成スニペットに表示される appId。<see cref="ApiKey"/> と同様に App Check の初期化にのみ使用する。</summary>
    public const string AppId = "1:646849019045:web:d735d9c583a66ad18ee14c";

    /// <summary>
    /// Firebase App Check（reCAPTCHA Enterprise プロバイダ）のサイトキー。
    /// classic の reCAPTCHA（v3）は Firebase コンソールでの新規登録が廃止されているため、
    /// このプロジェクトでは reCAPTCHA Enterprise を使用している（<c>wwwroot/js/appCheck.js</c> の
    /// <c>ReCaptchaEnterpriseProvider</c> 参照）。Google Cloud Console の reCAPTCHA セクションで
    /// 「Website」タイプのキーを作成し、Firebase コンソール「Build &gt; App Check」でアプリに登録して発行される。
    /// 空文字のままの場合、<c>wwwroot/js/appCheck.js</c> は初期化をスキップし、
    /// 従来どおり App Check トークンなしで Firestore にアクセスする（挙動に影響しない）。
    /// 値を設定したうえで、Firestore の App Check 適用（Enforce）を有効化して初めて防御として機能する。
    /// </summary>
    public const string RecaptchaSiteKey = "6Ldf37ctAAAAAJ1UpHZ6HDWePIbOMhPK3rcvc2PO";

    public static string FirestoreBaseUrl =>
        $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents";
}
