using Moq;
using FileNoteManager.Core.Repositories;
using FileNoteManager.Core.Repositories.Interfaces;
using FileNoteManager.Core.Services.Implementation;

namespace FileNoteManager.Core.Tests.Services;

public class HistoryServiceTests
{
    private readonly Mock<IHistoryRepository> _historyRepo = new();
    private readonly Mock<INoteRepository> _noteRepo = new();

    private HistoryService CreateService() =>
        new(_historyRepo.Object, _noteRepo.Object);

    // ── GetHistory ─────────────────────────────────────────────

    [Fact]
    public void GetHistory_ReturnsMappedVersions()
    {
        _historyRepo.Setup(r => r.GetByPath("C:\\doc.txt")).Returns(new[]
        {
            new HistoryEntity { Id = 1, NotePath = "C:\\doc.txt", Content = "v1", VersionNumber = 1, SavedAt = DateTime.UtcNow },
            new HistoryEntity { Id = 2, NotePath = "C:\\doc.txt", Content = "v2", VersionNumber = 2, SavedAt = DateTime.UtcNow }
        });

        var history = CreateService().GetHistory("C:\\doc.txt").ToList();

        Assert.Equal(2, history.Count);
        Assert.Equal("v1", history[0].Content);
    }

    // ── GetVersion ─────────────────────────────────────────────

    [Fact]
    public void GetVersion_WithMatchingPathAndId_ReturnsVersion()
    {
        _historyRepo.Setup(r => r.GetById(5))
                    .Returns(new HistoryEntity { Id = 5, NotePath = "C:\\doc.txt", Content = "old" });

        var version = CreateService().GetVersion("C:\\doc.txt", 5);

        Assert.NotNull(version);
        Assert.Equal("old", version.Content);
    }

    [Fact]
    public void GetVersion_WhenPathMismatch_ReturnsNull()
    {
        _historyRepo.Setup(r => r.GetById(5))
                    .Returns(new HistoryEntity { Id = 5, NotePath = "C:\\other.txt" });

        var result = CreateService().GetVersion("C:\\doc.txt", 5);

        Assert.Null(result);
    }

    // ── SaveHistory ────────────────────────────────────────────

    [Fact]
    public void SaveHistory_SavesEntityWithCorrectVersionNumber()
    {
        _historyRepo.Setup(r => r.GetNextVersionNumber("C:\\a.txt")).Returns(3);

        CreateService().SaveHistory("C:\\a.txt", "content snapshot");

        _historyRepo.Verify(r => r.Save(It.Is<HistoryEntity>(h =>
            h.NotePath == "C:\\a.txt" &&
            h.Content == "content snapshot" &&
            h.VersionNumber == 3)), Times.Once);
    }

    // ── RestoreVersion ─────────────────────────────────────────

    [Fact]
    public void RestoreVersion_UpdatesNoteWithHistoricalContent()
    {
        var historyEntry = new HistoryEntity { Id = 2, NotePath = "C:\\r.txt", Content = "restored content", VersionNumber = 2 };
        var currentNote = new NoteEntity { Path = "C:\\r.txt", Content = "current content" };

        _historyRepo.Setup(r => r.GetById(2)).Returns(historyEntry);
        _noteRepo.Setup(r => r.GetById("C:\\r.txt")).Returns(currentNote);
        _historyRepo.Setup(r => r.GetNextVersionNumber("C:\\r.txt")).Returns(3);

        CreateService().RestoreVersion("C:\\r.txt", 2);

        _historyRepo.Verify(r => r.Save(It.Is<HistoryEntity>(h =>
            h.Content == "current content")), Times.Once, "Should save current content as history before restoring");

        _noteRepo.Verify(r => r.Save(It.Is<NoteEntity>(n =>
            n.Content == "restored content")), Times.Once);
    }

    [Fact]
    public void RestoreVersion_WhenHistoryNotFound_ThrowsInvalidOperationException()
    {
        _historyRepo.Setup(r => r.GetById(99)).Returns((HistoryEntity?)null);

        Assert.Throws<InvalidOperationException>(() =>
            CreateService().RestoreVersion("C:\\x.txt", 99));
    }

    // ── CleanupOldVersions ─────────────────────────────────────

    [Fact]
    public void CleanupOldVersions_DelegatesToRepository()
    {
        CreateService().CleanupOldVersions("C:\\clean.txt", 5);

        _historyRepo.Verify(r => r.DeleteOldVersions("C:\\clean.txt", 5), Times.Once);
    }
}
