using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Services;
using IyokoraAttendanceWebAssembly.Tests.TestSupport;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class LocalProfileStoreTests
{
    private static LocalProfileStore CreateStore() => new(FakeLocalStorage.Create());

    [Fact]
    public void 未登録の端末ではIsRegisteredがfalseになる()
    {
        var profile = CreateStore();

        Assert.False(profile.IsRegistered);
    }

    [Fact]
    public void MemberIdとNameの両方が設定されるとIsRegisteredがtrueになる()
    {
        var profile = CreateStore();
        _ = profile.MemberId;
        profile.Name = "山田 太郎";

        Assert.True(profile.IsRegistered);
    }

    [Fact]
    public void MemberId未発行の端末では初回アクセス時に自動生成され以後同じ値が返る()
    {
        var profile = CreateStore();

        var first = profile.MemberId;
        var second = profile.MemberId;

        Assert.False(string.IsNullOrEmpty(first));
        Assert.Equal(first, second);
    }

    [Fact]
    public void LoginIdは未設定時は空文字を返す()
    {
        var profile = CreateStore();

        Assert.Equal(string.Empty, profile.LoginId);
    }

    [Fact]
    public void LoginIdは設定した値がそのまま読み取れる()
    {
        var profile = CreateStore();

        profile.LoginId = "IK1234";

        Assert.Equal("IK1234", profile.LoginId);
    }

    [Fact]
    public void Partは未設定時はSopranoが既定値になる()
    {
        var profile = CreateStore();

        Assert.Equal(PartType.Soprano, profile.Part);
    }

    [Fact]
    public void Partは設定した値がそのまま読み取れる()
    {
        var profile = CreateStore();

        profile.Part = PartType.Bass;

        Assert.Equal(PartType.Bass, profile.Part);
    }

    [Fact]
    public void Roleは未設定時はGeneralMemberが既定値になる()
    {
        var profile = CreateStore();

        Assert.Equal(Role.GeneralMember, profile.Role);
    }

    [Fact]
    public void Roleは設定した値がそのまま読み取れる()
    {
        var profile = CreateStore();

        profile.Role = Role.Admin;

        Assert.Equal(Role.Admin, profile.Role);
    }

    [Fact]
    public void PiecePartsは未設定時は空リストになる()
    {
        var profile = CreateStore();

        Assert.Empty(profile.PieceParts);
    }

    [Fact]
    public void PiecePartsはJSON化して保存され読み取り時に復元される()
    {
        var profile = CreateStore();
        var pieceParts = new List<MemberPiecePart>
        {
            new() { PieceId = "p1", SubPart = "上" },
            new() { PieceId = "p2", SubPart = "下" }
        };

        profile.PieceParts = pieceParts;
        var loaded = profile.PieceParts;

        Assert.Equal(2, loaded.Count);
        Assert.Equal("p1", loaded[0].PieceId);
        Assert.Equal("上", loaded[0].SubPart);
    }

    [Fact]
    public void Clearで保存済みのプロフィール情報が全て削除される()
    {
        var profile = CreateStore();
        profile.Name = "山田 太郎";
        profile.LoginId = "IK1234";
        profile.Part = PartType.Alto;
        profile.Role = Role.Admin;
        profile.PieceParts = [new() { PieceId = "p1", SubPart = "上" }];
        var originalMemberId = profile.MemberId;

        profile.Clear();

        Assert.Equal(string.Empty, profile.Name);
        Assert.Equal(string.Empty, profile.LoginId);
        Assert.Equal(PartType.Soprano, profile.Part);
        Assert.Equal(Role.GeneralMember, profile.Role);
        Assert.Empty(profile.PieceParts);
        Assert.NotEqual(originalMemberId, profile.MemberId);
    }
}
