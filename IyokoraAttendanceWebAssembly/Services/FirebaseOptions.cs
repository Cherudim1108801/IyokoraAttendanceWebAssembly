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

    public static string FirestoreBaseUrl =>
        $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents";
}
