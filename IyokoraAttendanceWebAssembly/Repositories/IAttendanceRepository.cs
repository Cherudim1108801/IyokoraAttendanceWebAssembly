using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>attendances</c> コレクションに対するデータアクセスの抽象。</summary>
public interface IAttendanceRepository
{
    /// <summary>指定練習に対する自団体分の全メンバーの出欠を取得する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task<List<Attendance>> GetForPracticeAsync(string practiceId, CancellationToken ct = default);

    /// <summary>指定練習における、指定メンバー1人分の出欠を取得する。未回答の場合は null。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="memberId">メンバーID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task<Attendance?> GetForMemberAsync(string practiceId, string memberId, CancellationToken ct = default);

    /// <summary>指定練習・指定メンバーの出欠状態を登録または更新する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="memberId">メンバーID。</param>
    /// <param name="memberName">メンバー名（非正規化して保存）。</param>
    /// <param name="part">所属パート（非正規化して保存）。</param>
    /// <param name="status">出欠状態。</param>
    /// <param name="updatedAt">更新日時（UTC）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task SetStatusAsync(string practiceId, string memberId, string memberName, PartType part, AttendanceStatus status, DateTime updatedAt, CancellationToken ct = default);
}
