using IyokoraAttendanceWebAssembly.Services;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class LoginIdGeneratorTests
{
    [Fact]
    public void 数字4桁がゼロ埋めされてprefixの直後に付与される()
    {
        var id = LoginIdGenerator.Format("IK", 7);

        Assert.Equal("IK0007", id);
    }

    [Fact]
    public void 生成される候補IDが常にprefixと数字4桁の形式になる()
    {
        for (var i = 0; i < 100; i++)
        {
            var candidate = LoginIdGenerator.GenerateCandidate("IK");

            Assert.StartsWith("IK", candidate);
            Assert.Equal(6, candidate.Length);
            Assert.True(LoginIdGenerator.IsValidFormat(candidate));
        }
    }

    [Theory]
    [InlineData("IK0001", true)]
    [InlineData("ABCD9999", true)]
    [InlineData("IK001", false)]
    [InlineData("00001234", false)]
    [InlineData("IK12A4", false)]
    [InlineData("1234", false)]
    [InlineData("", false)]
    [InlineData("ik0001", false)]
    public void 形式判定が英字prefixと数字4桁の組み合わせのみを有効とする(string value, bool expected)
    {
        Assert.Equal(expected, LoginIdGenerator.IsValidFormat(value));
    }

    [Fact]
    public void 正規化で前後の空白が除去され英字が大文字化される()
    {
        var normalized = LoginIdGenerator.Normalize("  ik0001  ");

        Assert.Equal("IK0001", normalized);
    }

    [Theory]
    [InlineData(0, "IK0000")]
    [InlineData(9999, "IK9999")]
    public void 数字の境界値でも4桁ゼロ埋めが正しく行われる(int number, string expected)
    {
        Assert.Equal(expected, LoginIdGenerator.Format("IK", number));
    }

    [Fact]
    public void 一意な候補が既存IDと重複しない場合は最初の候補がそのまま採用される()
    {
        var attempts = 0;
        var result = LoginIdGenerator.ResolveUnique(
            candidateFactory: () => { attempts++; return "IK0001"; },
            isTaken: _ => false);

        Assert.Equal("IK0001", result);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public void 候補が既存IDと重複する間は使用済みでなくなるまで再生成される()
    {
        var candidates = new Queue<string>(["IK0001", "IK0002", "IK0003"]);
        var takenIds = new HashSet<string> { "IK0001", "IK0002" };

        var result = LoginIdGenerator.ResolveUnique(
            candidateFactory: candidates.Dequeue,
            isTaken: takenIds.Contains);

        Assert.Equal("IK0003", result);
    }

    [Fact]
    public void 最大試行回数に達しても一意なIDが見つからない場合は例外がスローされる()
    {
        Assert.Throws<InvalidOperationException>(() => LoginIdGenerator.ResolveUnique(
            candidateFactory: () => "IK0001",
            isTaken: _ => true,
            maxAttempts: 5));
    }
}
