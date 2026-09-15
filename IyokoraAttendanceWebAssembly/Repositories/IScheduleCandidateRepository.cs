using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>scheduleCandidates</c> コレクションに対するデータアクセスの抽象。</summary>
public interface IScheduleCandidateRepository
{
    /// <summary>自団体（<see cref="Services.FirebaseOptions.GroupId"/>）に登録されている全候補日を取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    Task<List<ScheduleCandidate>> GetAllAsync(CancellationToken ct = default);

    /// <summary>新しい候補日を登録する。</summary>
    /// <param name="candidateId">発行済みの候補日ID。</param>
    /// <param name="date">候補日（時刻情報は無視される）。</param>
    /// <param name="timeOfDay">候補日の時間帯区分（午前／午後／夜間）。</param>
    /// <param name="createdAt">登録日時（UTC）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task CreateAsync(string candidateId, DateTime date, TimeOfDay timeOfDay, DateTime createdAt, CancellationToken ct = default);

    /// <summary>指定IDの候補日を削除する。</summary>
    /// <param name="candidateId">候補日ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task DeleteAsync(string candidateId, CancellationToken ct = default);
}
