using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class KeyPickupRecorderTests
{
    [Fact]
    public void 受け取り済みにする場合は受け取ったメンバー名がそのまま記録される()
    {
        var recorded = KeyPickupRecorder.ResolveRecordedName(keyPickedUp: true, memberName: "山田 太郎");

        Assert.Equal("山田 太郎", recorded);
    }

    [Fact]
    public void 未受け取りに戻す場合は記録がクリアされる()
    {
        var recorded = KeyPickupRecorder.ResolveRecordedName(keyPickedUp: false, memberName: "山田 太郎");

        Assert.Null(recorded);
    }
}
