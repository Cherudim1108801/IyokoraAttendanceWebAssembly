using IyokoraAttendanceWebAssembly.Models;

namespace IyokoraAttendanceWebAssembly.Repositories;

/// <summary>Firestore の <c>practices</c> コレクションに対するデータアクセスの抽象。</summary>
public interface IPracticeRepository
{
    /// <summary>自団体（<see cref="Services.FirebaseOptions.GroupId"/>）の全練習予定を日付昇順で取得する。</summary>
    /// <param name="ct">キャンセルトークン。</param>
    Task<List<Practice>> GetAllAsync(CancellationToken ct = default);

    /// <summary>指定IDの練習予定を1件取得する。存在しない場合は null。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task<Practice?> GetByIdAsync(string practiceId, CancellationToken ct = default);

    /// <summary>新しい練習予定を登録する。鍵の受け取り状況は未受け取りとして初期化される。</summary>
    /// <param name="practiceId">発行済みの練習予定ID。</param>
    /// <param name="date">練習日（時刻情報は無視される）。</param>
    /// <param name="title">タイトル（任意）。</param>
    /// <param name="place">場所（任意）。</param>
    /// <param name="startTime">練習開始時刻（"HH:mm" 形式、任意）。</param>
    /// <param name="endTime">練習終了時刻（"HH:mm" 形式、任意）。</param>
    /// <param name="timelineItems">タイムスケジュール（10分単位の詳細な予定）。</param>
    /// <param name="pieces">演奏予定曲。</param>
    /// <param name="requiresKeyPickup">鍵の受け取りが必要かどうか。</param>
    /// <param name="createdAt">登録日時（UTC）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task CreateAsync(string practiceId, DateTime date, string title, string place, string startTime, string endTime, IReadOnlyList<PracticeTimelineItem> timelineItems, IReadOnlyList<PracticePieceRef> pieces, bool requiresKeyPickup, DateTime createdAt, CancellationToken ct = default);

    /// <summary>練習予定の開始・終了時刻とタイムスケジュールを更新する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="startTime">練習開始時刻（"HH:mm" 形式、任意）。</param>
    /// <param name="endTime">練習終了時刻（"HH:mm" 形式、任意）。</param>
    /// <param name="timelineItems">タイムスケジュール（10分単位の詳細な予定）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task UpdateScheduleAsync(string practiceId, string startTime, string endTime, IReadOnlyList<PracticeTimelineItem> timelineItems, CancellationToken ct = default);

    /// <summary>練習予定の演奏予定曲を更新する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="pieces">演奏予定曲（録音リンク・強調表示の設定を含む）。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task UpdatePiecesAsync(string practiceId, IReadOnlyList<PracticePieceRef> pieces, CancellationToken ct = default);

    /// <summary>
    /// 練習予定の場所を更新する。場所の変更に伴い鍵の受け取りが必要かどうかを設定し直し、
    /// 鍵の受け取り状況は未受け取りにリセットする（受け取ったメンバーの記録もクリアする）。
    /// </summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="place">場所（任意）。</param>
    /// <param name="requiresKeyPickup">鍵の受け取りが必要かどうか。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task UpdatePlaceAsync(string practiceId, string place, bool requiresKeyPickup, CancellationToken ct = default);

    /// <summary>指定IDの練習予定を削除する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task DeleteAsync(string practiceId, CancellationToken ct = default);

    /// <summary>指定の練習の鍵の受け取り状況を設定する。</summary>
    /// <param name="practiceId">練習予定ID。</param>
    /// <param name="keyPickedUp">受け取り済みかどうか。</param>
    /// <param name="keyPickedUpByName">受け取ったメンバーの表示名。未受け取りに戻す場合は null。</param>
    /// <param name="ct">キャンセルトークン。</param>
    Task SetKeyPickedUpAsync(string practiceId, bool keyPickedUp, string? keyPickedUpByName, CancellationToken ct = default);
}
