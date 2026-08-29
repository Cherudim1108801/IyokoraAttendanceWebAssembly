namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>アプリ内での利用者の役割。</summary>
public enum Role
{
    /// <summary>一般団員。出欠登録のみ行え、それ以外の情報は閲覧のみ。</summary>
    GeneralMember,

    /// <summary>管理者。練習曲・練習予定の追加や削除が行える。</summary>
    Admin
}

/// <summary><see cref="Role"/> の表示用ヘルパー。</summary>
public static class RoleExtensions
{
    /// <summary>UI表示用の日本語名を返す。</summary>
    public static string ToDisplayName(this Role role) => role switch
    {
        Role.Admin => "管理者",
        Role.GeneralMember => "一般団員",
        _ => role.ToString()
    };

    /// <summary>全役割を固定順（一般団員→管理者）で列挙する。</summary>
    public static readonly Role[] All =
    [
        Role.GeneralMember,
        Role.Admin
    ];
}
