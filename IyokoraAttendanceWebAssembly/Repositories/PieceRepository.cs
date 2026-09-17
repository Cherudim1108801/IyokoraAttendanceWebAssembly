using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>pieces</c> コレクションに対する <see cref="IPieceRepository"/> の実装。</summary>
public class PieceRepository(IFirestoreClient client) : IPieceRepository
{
    private const string Collection = "pieces";

    /// <summary>
    /// サーバー側で絞り込んだ結果のみを取得する（コレクション全体は転送しない）。
    /// </summary>
    public async Task<List<Piece>> GetAllAsync(CancellationToken ct = default)
    {
        var filters = new Dictionary<string, object?> { ["groupId"] = FirebaseOptions.GroupId };
        var docs = await client.QueryDocumentsAsync(Collection, filters, ct);
        return docs.Select(ToPiece).ToList();
    }

    public Task SetArchivedAsync(string pieceId, bool isArchived, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, object?> { ["isArchived"] = isArchived };
        return client.UpsertDocumentAsync(Collection, pieceId, fields, ct);
    }

    public Task CreateAsync(string pieceId, string title, IReadOnlyList<PiecePartAssignment> partAssignments, DateTime createdAt, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, object?>
        {
            ["groupId"] = FirebaseOptions.GroupId,
            ["title"] = title,
            ["parts"] = partAssignments
                .Select(a => new Dictionary<string, object?>
                {
                    ["part"] = a.Part.ToString(),
                    ["division"] = a.Division.ToString()
                })
                .Cast<object?>()
                .ToList(),
            ["createdAt"] = createdAt
        };
        return client.UpsertDocumentAsync(Collection, pieceId, fields, ct);
    }

    public Task DeleteAsync(string pieceId, CancellationToken ct = default) =>
        client.DeleteDocumentAsync(Collection, pieceId, ct);

    private static Piece ToPiece(FirestoreDocument doc) => new()
    {
        Id = doc.Id,
        Title = doc.GetString("title"),
        PartAssignments = doc.GetList("parts")
            .OfType<Dictionary<string, object?>>()
            .Select(ToAssignment)
            .ToList(),
        CreatedAt = doc.GetDateTime("createdAt"),
        IsArchived = doc.GetBool("isArchived")
    };

    private static PiecePartAssignment ToAssignment(Dictionary<string, object?> fields) => new()
    {
        Part = Enum.TryParse<PartType>(fields.GetValueOrDefault("part") as string, out var part) ? part : PartType.Soprano,
        Division = Enum.TryParse<PartDivision>(fields.GetValueOrDefault("division") as string, out var division) ? division : PartDivision.None
    };
}
