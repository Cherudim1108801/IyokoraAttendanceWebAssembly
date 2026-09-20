using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using Microsoft.Extensions.Logging;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary><c>members</c> に対するメンバー情報の取得・保存を担う。氏名の暗号化・復号とログインIDの一意性管理を担当する。</summary>
public class MemberService(IMemberRepository repository, NameCipher nameCipher, ILogger<MemberService> logger)
{
    /// <summary>登録されている全メンバーを、パート → 氏名の順で取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<Member>> GetAllAsync(CancellationToken ct = default)
    {
        var raw = await repository.GetAllAsync(ct);
        var members = new List<Member>(raw.Count);
        foreach (var member in raw)
            members.Add(await DecryptNameAsync(member));

        return members
            .OrderBy(m => m.Part)
            .ThenBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// メンバーの名前・パート・役割・曲ごとの内部パート担当を新規登録または更新する。
    /// 登録済みのログインIDを渡すとそれを維持し、未指定（<c>null</c> または空文字）の場合は新規のログインIDを発行する。
    /// </summary>
    /// <param name="memberId">端末で発行された MemberId。</param>
    /// <param name="name">表示名。</param>
    /// <param name="part">所属パート。</param>
    /// <param name="role">役割（管理者／一般団員）。</param>
    /// <param name="pieceParts">曲ごとの内部パート（分割）担当。</param>
    /// <param name="existingLoginId">維持する既存のログインID。新規登録時は <c>null</c>。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>保存されたログインID（新規発行時はその値）。</returns>
    public async Task<string> SaveAsync(string memberId, string name, PartType part, Role role, IReadOnlyList<MemberPiecePart> pieceParts, string? existingLoginId, CancellationToken ct = default)
    {
        var loginId = string.IsNullOrEmpty(existingLoginId) ? await GenerateUniqueLoginIdAsync(ct) : existingLoginId;
        var storedName = await nameCipher.EncryptAsync(name);
        await repository.UpsertAsync(memberId, storedName, part, role, loginId, pieceParts, DateTime.UtcNow, ct);
        return loginId;
    }

    /// <summary>
    /// 指定ログインIDに一致するメンバーを取得する。見つからない場合は null。
    /// メンバー全件を取得せず、サーバー側でログインID一致の絞り込みを行う。
    /// </summary>
    /// <param name="loginId">ログインID（前後空白の有無や大文字小文字は問わない）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<Member?> FindByLoginIdAsync(string loginId, CancellationToken ct = default)
    {
        var normalized = LoginIdGenerator.Normalize(loginId);
        var raw = await repository.FindByLoginIdAsync(normalized, ct);
        return raw is null ? null : await DecryptNameAsync(raw);
    }

    /// <summary>
    /// 指定メンバーのログインIDを、現在の <see cref="FirebaseOptions.LoginIdPrefix"/> を使って新規に発行し直す。
    /// 団体プレフィックス変更後、旧プレフィックスのIDを使い続けているメンバーが
    /// 本人の操作で新プレフィックスへ移行するために使用する。
    /// </summary>
    /// <param name="memberId">対象メンバーの MemberId。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>新しく発行されたログインID。</returns>
    public async Task<string> ReissueLoginIdAsync(string memberId, CancellationToken ct = default)
    {
        var newLoginId = await GenerateUniqueLoginIdAsync(ct);
        await repository.UpdateLoginIdAsync(memberId, newLoginId, DateTime.UtcNow, ct);
        return newLoginId;
    }

    /// <summary>指定メンバーを削除する。</summary>
    /// <param name="memberId">対象メンバーの MemberId。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public Task DeleteAsync(string memberId, CancellationToken ct = default) =>
        repository.DeleteAsync(memberId, ct);

    private async Task<string> GenerateUniqueLoginIdAsync(CancellationToken ct)
    {
        var existing = await repository.GetAllAsync(ct);
        var existingLoginIds = existing
            .Select(m => m.LoginId)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToHashSet(StringComparer.Ordinal);

        return LoginIdGenerator.ResolveUnique(
            () => LoginIdGenerator.GenerateCandidate(FirebaseOptions.LoginIdPrefix),
            existingLoginIds.Contains);
    }

    /// <summary>
    /// メンバーの氏名（保存値）を復号する。旧 AES-CBC 形式で保存されていた場合は、画面の表示や読み込みを
    /// ブロックせずバックグラウンドで AES-GCM に再暗号化して保存し直す。
    /// </summary>
    private async Task<Member> DecryptNameAsync(Member raw)
    {
        var decrypted = await nameCipher.DecryptOrPlainAsync(raw.Name);
        if (decrypted.WasLegacyFormat)
            _ = ReencryptNameInBackgroundAsync(raw.Id, decrypted.Value);

        raw.Name = decrypted.Value;
        return raw;
    }

    /// <summary>
    /// 氏名が旧 AES-CBC 形式で暗号化されていた場合に、画面の表示や読み込みをブロックせず
    /// バックグラウンドで AES-GCM に再暗号化して保存し直す。ネットワーク障害等で失敗しても
    /// 画面表示には影響させず、次にこのメンバーの氏名が読み取られた際に再度移行を試みる。
    /// </summary>
    private async Task ReencryptNameInBackgroundAsync(string memberId, string plainName)
    {
        try
        {
            var reencrypted = await nameCipher.EncryptAsync(plainName);
            await repository.UpdateNameAsync(memberId, reencrypted);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "氏名の暗号化方式の移行(AES-CBC→AES-GCM)に失敗しました: memberId={MemberId}", memberId);
        }
    }
}
