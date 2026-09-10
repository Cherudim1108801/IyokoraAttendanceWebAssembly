namespace IyokoraAttendanceWebAssembly.Services;

/// <summary>鍵の受け取り状態を更新する際に、記録として残すメンバー名を決定する純粋ロジック。</summary>
public static class KeyPickupRecorder
{
    /// <summary>
    /// 受け取り済みにする場合は <paramref name="memberName"/> を記録名として返し、
    /// 未受け取りに戻す場合は記録をクリアするため <c>null</c> を返す。
    /// </summary>
    public static string? ResolveRecordedName(bool keyPickedUp, string memberName) =>
        keyPickedUp ? memberName : null;
}
