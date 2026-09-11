// AES-256-GCM を Web Crypto API (SubtleCrypto) 経由で行う。
// .NET の System.Security.Cryptography.Aes は Blazor WebAssembly (browser-wasm) では
// ネイティブ実装が存在せずサポートされないため、ブラウザ標準の SubtleCrypto を利用する。
//
// 以前のバージョンは AES-CBC（改ざん検知なし）を使用していたため、GCM 移行後も
// Firestore に残っている旧形式（CBC・16byte IV）を復号できるようフォールバックを残している。
// 新規の暗号化は常に GCM（12byte IV、認証タグ付き）で行う。

const GCM_IV_LENGTH = 12;
const CBC_IV_LENGTH = 16;

function base64ToBytes(base64) {
    const binStr = atob(base64);
    const bytes = new Uint8Array(binStr.length);
    for (let i = 0; i < binStr.length; i++) {
        bytes[i] = binStr.charCodeAt(i);
    }
    return bytes;
}

function bytesToBase64(bytes) {
    let binStr = '';
    for (let i = 0; i < bytes.length; i++) {
        binStr += String.fromCharCode(bytes[i]);
    }
    return btoa(binStr);
}

async function importKey(base64Key, algorithmName, usage) {
    const keyBytes = base64ToBytes(base64Key);
    return crypto.subtle.importKey('raw', keyBytes, algorithmName, false, [usage]);
}

window.nameCipherEncrypt = async function (base64Key, plainText) {
    const key = await importKey(base64Key, 'AES-GCM', 'encrypt');
    const iv = crypto.getRandomValues(new Uint8Array(GCM_IV_LENGTH));
    const plainBytes = new TextEncoder().encode(plainText);
    const cipherBuf = await crypto.subtle.encrypt({ name: 'AES-GCM', iv }, key, plainBytes);

    const combined = new Uint8Array(iv.length + cipherBuf.byteLength);
    combined.set(iv, 0);
    combined.set(new Uint8Array(cipherBuf), iv.length);
    return bytesToBase64(combined);
};

async function tryDecryptGcm(base64Key, buffer) {
    if (buffer.length <= GCM_IV_LENGTH) return null;
    try {
        const iv = buffer.slice(0, GCM_IV_LENGTH);
        const cipherBytes = buffer.slice(GCM_IV_LENGTH);
        const key = await importKey(base64Key, 'AES-GCM', 'decrypt');
        const plainBuf = await crypto.subtle.decrypt({ name: 'AES-GCM', iv }, key, cipherBytes);
        return new TextDecoder().decode(plainBuf);
    } catch {
        return null;
    }
}

// 旧バージョン（AES-CBC）で暗号化された既存データ用のフォールバック。
async function tryDecryptLegacyCbc(base64Key, buffer) {
    if (buffer.length <= CBC_IV_LENGTH) return null;
    try {
        const iv = buffer.slice(0, CBC_IV_LENGTH);
        const cipherBytes = buffer.slice(CBC_IV_LENGTH);
        const key = await importKey(base64Key, 'AES-CBC', 'decrypt');
        const plainBuf = await crypto.subtle.decrypt({ name: 'AES-CBC', iv }, key, cipherBytes);
        return new TextDecoder().decode(plainBuf);
    } catch {
        return null;
    }
}

window.nameCipherDecryptOrPlain = async function (base64Key, value) {
    let buffer;
    try {
        buffer = base64ToBytes(value);
    } catch {
        return value;
    }

    const gcmResult = await tryDecryptGcm(base64Key, buffer);
    if (gcmResult !== null) return gcmResult;

    const cbcResult = await tryDecryptLegacyCbc(base64Key, buffer);
    if (cbcResult !== null) return cbcResult;

    // 暗号化導入前に保存された平文データの可能性があるため、そのまま返す。
    return value;
};
