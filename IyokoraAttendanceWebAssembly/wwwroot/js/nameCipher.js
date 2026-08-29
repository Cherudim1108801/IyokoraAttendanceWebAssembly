// AES-256-CBC を Web Crypto API (SubtleCrypto) 経由で行う。
// .NET の System.Security.Cryptography.Aes は Blazor WebAssembly (browser-wasm) では
// ネイティブ実装が存在せずサポートされないため、ブラウザ標準の SubtleCrypto を利用する。

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

async function importKey(base64Key, usage) {
    const keyBytes = base64ToBytes(base64Key);
    return crypto.subtle.importKey('raw', keyBytes, 'AES-CBC', false, [usage]);
}

window.nameCipherEncrypt = async function (base64Key, plainText) {
    const key = await importKey(base64Key, 'encrypt');
    const iv = crypto.getRandomValues(new Uint8Array(16));
    const plainBytes = new TextEncoder().encode(plainText);
    const cipherBuf = await crypto.subtle.encrypt({ name: 'AES-CBC', iv }, key, plainBytes);

    const combined = new Uint8Array(iv.length + cipherBuf.byteLength);
    combined.set(iv, 0);
    combined.set(new Uint8Array(cipherBuf), iv.length);
    return bytesToBase64(combined);
};

window.nameCipherDecryptOrPlain = async function (base64Key, value) {
    try {
        const buffer = base64ToBytes(value);
        if (buffer.length <= 16) {
            return value;
        }

        const iv = buffer.slice(0, 16);
        const cipherBytes = buffer.slice(16);
        const key = await importKey(base64Key, 'decrypt');
        const plainBuf = await crypto.subtle.decrypt({ name: 'AES-CBC', iv }, key, cipherBytes);
        return new TextDecoder().decode(plainBuf);
    } catch {
        return value;
    }
};
