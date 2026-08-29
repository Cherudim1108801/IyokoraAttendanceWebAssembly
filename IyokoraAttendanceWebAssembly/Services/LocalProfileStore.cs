using System.Text.Json;
using IyokoraAttendanceWebAssembly.Models;
using Microsoft.JSInterop;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>
/// この端末を使っている本人のプロフィール（ログインなしの簡易識別）をブラウザの localStorage に保存する。
/// </summary>
public class LocalProfileStore(IJSInProcessRuntime js)
{
    private const string KeyMemberId = "profile.memberId";
    private const string KeyName = "profile.name";
    private const string KeyPart = "profile.part";
    private const string KeyRole = "profile.role";
    private const string KeyPieceParts = "profile.pieceParts";

    private string? Get(string key) => js.Invoke<string?>("localStorage.getItem", key);
    private void Set(string key, string value) => js.InvokeVoid("localStorage.setItem", key, value);
    private void Remove(string key) => js.InvokeVoid("localStorage.removeItem", key);

    /// <summary>名前が登録済みかどうか（オンボーディング完了の判定に使用）。</summary>
    public bool IsRegistered => !string.IsNullOrEmpty(MemberId) && !string.IsNullOrEmpty(Name);

    /// <summary>
    /// この端末に割り当てられた MemberId。未発行の場合は初回アクセス時に自動生成して永続化する。
    /// </summary>
    public string MemberId
    {
        get
        {
            var id = Get(KeyMemberId);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                Set(KeyMemberId, id);
            }
            return id;
        }
    }

    /// <summary>表示名。</summary>
    public string Name
    {
        get => Get(KeyName) ?? string.Empty;
        set => Set(KeyName, value);
    }

    /// <summary>所属パート。未設定時は <see cref="PartType.Soprano"/> を既定値として返す。</summary>
    public PartType Part
    {
        get => Enum.TryParse<PartType>(Get(KeyPart), out var part) ? part : PartType.Soprano;
        set => Set(KeyPart, value.ToString());
    }

    /// <summary>役割。未設定時は <see cref="Role.GeneralMember"/> を既定値として返す。</summary>
    public Role Role
    {
        get => Enum.TryParse<Role>(Get(KeyRole), out var role) ? role : Role.GeneralMember;
        set => Set(KeyRole, value.ToString());
    }

    /// <summary>曲ごとの内部パート（分割）担当。</summary>
    public List<MemberPiecePart> PieceParts
    {
        get
        {
            var json = Get(KeyPieceParts);
            if (string.IsNullOrEmpty(json))
                return [];
            return JsonSerializer.Deserialize<List<MemberPiecePart>>(json) ?? [];
        }
        set => Set(KeyPieceParts, JsonSerializer.Serialize(value));
    }

    /// <summary>端末に保存されたプロフィール情報（MemberId／名前／パート／曲ごとのパート担当）をすべて削除する。</summary>
    public void Clear()
    {
        Remove(KeyMemberId);
        Remove(KeyName);
        Remove(KeyPart);
        Remove(KeyRole);
        Remove(KeyPieceParts);
    }
}
