using IyokoraAttendanceWebAssembly.Models;
using IyokoraAttendanceWebAssembly.Repositories;
using IyokoraAttendanceWebAssembly.Services;
using Moq;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class PieceServiceTests
{
    private static Piece CreatePiece(string id, string title, bool isArchived = false) => new()
    {
        Id = id,
        Title = title,
        IsArchived = isArchived
    };

    private static (PieceService service, Mock<IPieceRepository> repository) CreateService(IEnumerable<Piece>? pieces = null)
    {
        var repositoryMock = new Mock<IPieceRepository>();
        repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((pieces ?? []).ToList());

        return (new PieceService(repositoryMock.Object), repositoryMock);
    }

    [Fact]
    public async Task 既定では非表示にされた曲が一覧から除外される()
    {
        var pieces = new[] { CreatePiece("p1", "曲A"), CreatePiece("p2", "曲B", isArchived: true) };
        var (service, _) = CreateService(pieces);

        var result = await service.GetAllAsync();

        Assert.Equal(["曲A"], result.Select(p => p.Title));
    }

    [Fact]
    public async Task includeArchivedを指定すると非表示の曲も含まれる()
    {
        var pieces = new[] { CreatePiece("p1", "曲A"), CreatePiece("p2", "曲B", isArchived: true) };
        var (service, _) = CreateService(pieces);

        var result = await service.GetAllAsync(includeArchived: true);

        Assert.Equal(["曲A", "曲B"], result.Select(p => p.Title));
    }

    [Fact]
    public async Task 曲名の大文字小文字を無視した順で並び替えられる()
    {
        var pieces = new[] { CreatePiece("p1", "banana"), CreatePiece("p2", "Apple") };
        var (service, _) = CreateService(pieces);

        var result = await service.GetAllAsync();

        Assert.Equal(["Apple", "banana"], result.Select(p => p.Title));
    }
}
