using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>Firestore の <c>members</c> コレクションに対するメンバー情報の取得・保存を担う。</summary>
public class MemberService(FirestoreClient client, NameCipher nameCipher)
{
    private const string Collection = "members";

    /// <summary>登録されている全メンバーを、パート → 氏名の順で取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<Member>> GetAllAsync(CancellationToken ct = default)
    {
        var docs = await client.ListDocumentsAsync(Collection, ct);
        var filtered = docs.Where(d => d.GetString("groupId") == FirebaseOptions.GroupId).ToList();

        var members = new List<Member>(filtered.Count);
        foreach (var doc in filtered)
            members.Add(await ToMemberAsync(doc));

        return members
            .OrderBy(m => m.Part)
            .ThenBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>メンバーの名前・パート・役割・曲ごとの内部パート担当を新規登録または更新する。</summary>
    /// <param name="memberId">端末で発行された MemberId。</param>
    /// <param name="name">表示名。</param>
    /// <param name="part">所属パート。</param>
    /// <param name="role">役割（管理者／一般団員）。</param>
    /// <param name="pieceParts">曲ごとの内部パート（分割）担当。</param>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task SaveAsync(string memberId, string name, PartType part, Role role, IReadOnlyList<MemberPiecePart> pieceParts, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["name"] = await nameCipher.EncryptAsync(name),
            ["part"] = part.ToString(),
            ["role"] = role.ToString(),
            ["pieceParts"] = pieceParts
                .Select(p => new Dictionary<string, object?>
                {
                    ["pieceId"] = p.PieceId,
                    ["subPart"] = p.SubPart
                })
                .Cast<object?>()
                .ToList(),
            ["updatedAt"] = DateTime.UtcNow
        };
        await client.UpsertDocumentAsync(Collection, memberId, fields, ct);
    }

    private async Task<Member> ToMemberAsync(FirestoreDocument doc) => new()
    {
        Id = doc.Id,
        Name = await nameCipher.DecryptOrPlainAsync(doc.GetString("name")),
        Part = Enum.TryParse<PartType>(doc.GetString("part"), out var part) ? part : PartType.Soprano,
        Role = Enum.TryParse<Role>(doc.GetString("role"), out var role) ? role : Role.GeneralMember,
        PieceParts = doc.GetList("pieceParts")
            .OfType<Dictionary<string, object?>>()
            .Select(ToPiecePart)
            .ToList(),
        UpdatedAt = doc.GetDateTime("updatedAt")
    };

    private static MemberPiecePart ToPiecePart(Dictionary<string, object?> fields) => new()
    {
        PieceId = fields.GetValueOrDefault("pieceId") as string ?? string.Empty,
        SubPart = fields.GetValueOrDefault("subPart") as string ?? string.Empty
    };
}
