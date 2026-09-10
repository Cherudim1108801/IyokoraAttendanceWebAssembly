namespace IyokoraAttendanceWebAssembly.Models;

/// <summary>合唱団のメンバー1人分のプロフィール（Firestore の <c>members</c> ドキュメントに対応）。</summary>
public class Member
{
    /// <summary>端末で発行された MemberId（Firestore のドキュメントID）。</summary>
    public required string Id { get; set; }

    /// <summary>表示名。</summary>
    public required string Name { get; set; }

    /// <summary>
    /// 複数端末から同じアカウントを使うためのログインID（団体ごとのアルファベット prefix + 数字4桁）。
    /// <see cref="Services.LoginIdGenerator"/> 参照。
    /// </summary>
    public required string LoginId { get; set; }

    /// <summary>所属パート。</summary>
    public required PartType Part { get; set; }

    /// <summary>役割（管理者／一般団員）。</summary>
    public required Role Role { get; set; }

    /// <summary>曲ごとの内部パート（分割）担当。</summary>
    public List<MemberPiecePart> PieceParts { get; set; } = [];

    /// <summary>最終更新日時（UTC）。</summary>
    public DateTime UpdatedAt { get; set; }
}
