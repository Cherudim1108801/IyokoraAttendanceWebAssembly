using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>members</c> コレクションに対する <see cref="IMemberRepository"/> の実装。</summary>
public class MemberRepository(IFirestoreClient client) : IMemberRepository
{
    private const string Collection = "members";

    public async Task<List<Member>> GetAllAsync(CancellationToken ct = default)
    {
        var docs = await client.ListDocumentsAsync(Collection, ct);
        return docs
            .Where(d => d.GetString("groupId") == FirebaseOptions.GroupId)
            .Select(ToMember)
            .ToList();
    }

    public async Task<Member?> FindByLoginIdAsync(string normalizedLoginId, CancellationToken ct = default)
    {
        var filters = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["loginId"] = normalizedLoginId
        };
        var docs = await client.QueryDocumentsAsync(Collection, filters, ct);
        var doc = docs.FirstOrDefault();
        return doc is null ? null : ToMember(doc);
    }

    public Task UpsertAsync(string memberId, string storedName, PartType part, Role role, string loginId, IReadOnlyList<MemberPiecePart> pieceParts, DateTime updatedAt, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["name"] = storedName,
            ["part"] = part.ToString(),
            ["role"] = role.ToString(),
            ["loginId"] = loginId,
            ["pieceParts"] = pieceParts
                .Select(p => new Dictionary<string, object?>
                {
                    ["pieceId"] = p.PieceId,
                    ["subPart"] = p.SubPart
                })
                .Cast<object?>()
                .ToList(),
            ["updatedAt"] = updatedAt
        };
        return client.UpsertDocumentAsync(Collection, memberId, fields, ct);
    }

    public Task UpdateLoginIdAsync(string memberId, string loginId, DateTime updatedAt, CancellationToken ct = default) =>
        client.UpsertDocumentAsync(Collection, memberId, new Dictionary<string, object?>
        {
            ["loginId"] = loginId,
            ["updatedAt"] = updatedAt
        }, ct);

    public Task UpdateNameAsync(string memberId, string storedName, CancellationToken ct = default) =>
        client.UpsertDocumentAsync(Collection, memberId, new Dictionary<string, object?> { ["name"] = storedName }, ct);

    private static Member ToMember(FirestoreDocument doc) => new()
    {
        Id = doc.Id,
        Name = doc.GetString("name"),
        LoginId = doc.GetString("loginId"),
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
