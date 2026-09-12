// Firebase App Check（reCAPTCHA Enterprise）の初期化とトークン取得。
//
// このアプリはログイン機能を持たず Firestore の REST API を直接叩く構成のため、
// Firestore セキュリティルールは未認証アクセスを許可せざるを得ない（FirebaseOptions.cs 参照）。
// App Check は「リクエスト元が本アプリ自身か」を検証する仕組みで、ログインなしのまま
// 外部からの無差別な読み書き（スクレイピング・自動化ツールからの直叩き等）を Firestore 側で拒否できる。
//
// classic の reCAPTCHA（v3）は Firebase コンソールでの新規登録が廃止されているため、
// reCAPTCHA Enterprise プロバイダを使用している（請求先アカウント未登録でも、スコアの精度が
// 4段階に制限される形で利用可能。詳細は Firebase 公式ドキュメント参照）。
//
// Firebase JS SDK は npm 依存を増やさないよう CDN (gstatic) から動的 import する。
const FIREBASE_SDK_VERSION = '10.14.1';

let tokenGetter = null;

// Blazor から呼び出す。FirebaseOptions.RecaptchaSiteKey が未設定（空文字）の間は
// 何もせず終了し、appCheckGetToken は常に null を返す（＝ヘッダーなしで従来どおり動作する）。
window.appCheckInit = async function (firebaseConfig, recaptchaSiteKey) {
    if (!recaptchaSiteKey) return;

    const { initializeApp } = await import(`https://www.gstatic.com/firebasejs/${FIREBASE_SDK_VERSION}/firebase-app.js`);
    const { initializeAppCheck, ReCaptchaEnterpriseProvider, getToken } =
        await import(`https://www.gstatic.com/firebasejs/${FIREBASE_SDK_VERSION}/firebase-app-check.js`);

    if (location.hostname === 'localhost' || location.hostname === '127.0.0.1') {
        // ローカル開発用のデバッグトークンを有効化する。実際のトークン文字列はブラウザの
        // コンソールに出力されるので、Firebase コンソール「App Check > アプリ > デバッグトークンを管理」で
        // 登録すると localhost からのアクセスも許可対象になる。
        self.FIREBASE_APPCHECK_DEBUG_TOKEN = true;
    }

    const app = initializeApp(firebaseConfig);
    const appCheck = initializeAppCheck(app, {
        provider: new ReCaptchaEnterpriseProvider(recaptchaSiteKey),
        isTokenAutoRefreshEnabled: true
    });

    tokenGetter = () => getToken(appCheck, /* forceRefresh */ false).then(r => r.token);
};

// Blazor から呼び出す。初期化されていない、または取得に失敗した場合は null を返す
// （呼び出し元の AppCheckTokenProvider がヘッダー省略にフォールバックする）。
window.appCheckGetToken = async function () {
    if (!tokenGetter) return null;
    try {
        return await tokenGetter();
    } catch {
        return null;
    }
};
