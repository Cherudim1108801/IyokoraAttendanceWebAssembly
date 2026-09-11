using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Tests.TestSupport;

/// <summary>
/// <see cref="FakeFirestoreClient"/> に投入する各コレクションのフィールド辞書を組み立てる。
/// 各 Service の To*（ドキュメント→モデル変換）・作成メソッドが読み書きするフィールド名と一致させている。
/// </summary>
internal static class Seed
{
    public static Dictionary<string, object?> Member(string name, PartType part = PartType.Soprano, Role role = Role.GeneralMember, string loginId = "IK0001", IEnumerable<MemberPiecePart>? pieceParts = null, string groupId = FirebaseOptions.GroupId) => new()
    {
        ["groupId"] = groupId,
        ["name"] = name,
        ["part"] = part.ToString(),
        ["role"] = role.ToString(),
        ["loginId"] = loginId,
        ["pieceParts"] = (pieceParts ?? [])
            .Select(p => new Dictionary<string, object?> { ["pieceId"] = p.PieceId, ["subPart"] = p.SubPart })
            .Cast<object?>()
            .ToList(),
        ["updatedAt"] = DateTime.UtcNow
    };

    public static Dictionary<string, object?> Practice(DateTime date, string title = "", string place = "", string startTime = "", string endTime = "", IEnumerable<PracticePieceRef>? pieces = null, bool requiresKeyPickup = false, bool keyPickedUp = false, string? keyPickedUpByName = null, string groupId = FirebaseOptions.GroupId) => new()
    {
        ["groupId"] = groupId,
        ["date"] = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
        ["title"] = title,
        ["place"] = place,
        ["startTime"] = startTime,
        ["endTime"] = endTime,
        ["timeline"] = new List<object?>(),
        ["pieces"] = (pieces ?? [])
            .Select(p => new Dictionary<string, object?>
            {
                ["pieceId"] = p.PieceId,
                ["title"] = p.Title,
                ["recordingUrl"] = p.RecordingUrl,
                ["featured"] = p.IsFeatured
            })
            .Cast<object?>()
            .ToList(),
        ["requiresKeyPickup"] = requiresKeyPickup,
        ["keyPickedUp"] = keyPickedUp,
        ["keyPickedUpByName"] = keyPickedUpByName,
        ["createdAt"] = DateTime.UtcNow
    };

    public static Dictionary<string, object?> Attendance(string practiceId, string memberId, string memberName, PartType part, AttendanceStatus status, string groupId = FirebaseOptions.GroupId) => new()
    {
        ["groupId"] = groupId,
        ["practiceId"] = practiceId,
        ["memberId"] = memberId,
        ["memberName"] = memberName,
        ["part"] = part.ToString(),
        ["status"] = status.ToString(),
        ["updatedAt"] = DateTime.UtcNow
    };

    public static Dictionary<string, object?> Piece(string title, IEnumerable<PiecePartAssignment>? partAssignments = null, bool isArchived = false, string groupId = FirebaseOptions.GroupId) => new()
    {
        ["groupId"] = groupId,
        ["title"] = title,
        ["parts"] = (partAssignments ?? [])
            .Select(a => new Dictionary<string, object?> { ["part"] = a.Part.ToString(), ["division"] = a.Division.ToString() })
            .Cast<object?>()
            .ToList(),
        ["createdAt"] = DateTime.UtcNow,
        ["isArchived"] = isArchived
    };

    public static Dictionary<string, object?> ScheduleCandidate(DateTime date, TimeOfDay timeOfDay, string groupId = FirebaseOptions.GroupId) => new()
    {
        ["groupId"] = groupId,
        ["date"] = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
        ["timeOfDay"] = timeOfDay.ToString(),
        ["createdAt"] = DateTime.UtcNow
    };

    public static Dictionary<string, object?> ScheduleVote(string candidateId, string memberId, string memberName, AttendanceStatus status, string groupId = FirebaseOptions.GroupId) => new()
    {
        ["groupId"] = groupId,
        ["candidateId"] = candidateId,
        ["memberId"] = memberId,
        ["memberName"] = memberName,
        ["status"] = status.ToString(),
        ["updatedAt"] = DateTime.UtcNow
    };
}
