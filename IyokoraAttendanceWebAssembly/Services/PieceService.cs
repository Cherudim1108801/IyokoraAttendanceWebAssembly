using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>Firestore の <c>pieces</c> コレクションに対する練習曲（レパートリー）の取得・作成・削除を担う。</summary>
public class PieceService(FirestoreClient client)
{
    private const string Collection = "pieces";

    /// <summary>登録されている全曲を曲名順で取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    public async Task<List<Piece>> GetAllAsync(CancellationToken ct = default)
    {
        var docs = await client.ListDocumentsAsync(Collection, ct);
        return docs
            .Where(d => d.GetString("groupId") == FirebaseOptions.GroupId)
            .Select(ToPiece)
            .OrderBy(p => p.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>新しい曲を登録する。</summary>
    /// <param name="title">曲名。</param>
    /// <param name="partAssignments">使用するパートの割り振り。</param>
    /// <param name="ct">キャンセルトークン。</param>
    /// <returns>発行された曲ID。</returns>
    public async Task<string> CreateAsync(string title, IReadOnlyList<PiecePartAssignment> partAssignments, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N");
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
            ["createdAt"] = DateTime.UtcNow
        };
        await client.UpsertDocumentAsync(Collection, id, fields, ct);
        return id;
    }

    /// <summary>指定IDの曲を削除する。</summary>
    /// <param name="pieceId">曲ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
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
        CreatedAt = doc.GetDateTime("createdAt")
    };

    private static PiecePartAssignment ToAssignment(Dictionary<string, object?> fields) => new()
    {
        Part = Enum.TryParse<PartType>(fields.GetValueOrDefault("part") as string, out var part) ? part : PartType.Soprano,
        Division = Enum.TryParse<PartDivision>(fields.GetValueOrDefault("division") as string, out var division) ? division : PartDivision.None
    };
}
