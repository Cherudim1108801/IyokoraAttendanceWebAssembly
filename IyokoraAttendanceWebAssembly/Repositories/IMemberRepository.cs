using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>
/// Firestore の <c>members</c> コレクションに対するデータアクセスの抽象。
/// <see cref="Models.Member.Name"/> は暗号化・復号を一切行わず、保存されている値をそのまま読み書きする
/// （暗号化・復号は呼び出し元の Services 層の責務）。
/// </summary>
public interface IMemberRepository
{
    /// <summary>自団体（<see cref="Services.FirebaseOptions.GroupId"/>）に属する全メンバーを取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    Task<List<Member>> GetAllAsync(CancellationToken ct = default);

    /// <summary>指定ログインID（正規化済み）に一致するメンバーを1件取得する。見つからない場合は null。</summary>
    /// <param name="normalizedLoginId">正規化済みのログインID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task<Member?> FindByLoginIdAsync(string normalizedLoginId, CancellationToken ct = default);

    /// <summary>メンバー情報を新規登録または更新する。</summary>
    /// <param name="memberId">端末で発行された MemberId。</param>
    /// <param name="storedName">保存する氏名（暗号化済みの値）。</param>
    /// <param name="part">所属パート。</param>
    /// <param name="role">役割。</param>
    /// <param name="loginId">ログインID。</param>
    /// <param name="pieceParts">曲ごとの内部パート（分割）担当。</param>
    /// <param name="updatedAt">更新日時（UTC）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task UpsertAsync(string memberId, string storedName, PartType part, Role role, string loginId, IReadOnlyList<MemberPiecePart> pieceParts, DateTime updatedAt, CancellationToken ct = default);

    /// <summary>指定メンバーのログインIDを更新する。</summary>
    /// <param name="memberId">対象メンバーの MemberId。</param>
    /// <param name="loginId">新しいログインID。</param>
    /// <param name="updatedAt">更新日時（UTC）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task UpdateLoginIdAsync(string memberId, string loginId, DateTime updatedAt, CancellationToken ct = default);

    /// <summary>指定メンバーの氏名（保存値）のみを更新する。</summary>
    /// <param name="memberId">対象メンバーの MemberId。</param>
    /// <param name="storedName">保存する氏名（暗号化済みの値）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task UpdateNameAsync(string memberId, string storedName, CancellationToken ct = default);
}
