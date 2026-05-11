using Moq;
using FileNoteManager.Core.Interfaces;
using FileNoteManager.Core.Models;
using FileNoteManager.Core.Repositories;
using FileNoteManager.Core.Repositories.Interfaces;
using FileNoteManager.Core.Services.Implementation;

namespace FileNoteManager.Core.Tests.Services;

public class NoteServiceTests
{
    private readonly Mock<INoteRepository> _noteRepo = new();
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<INoteTagRepository> _noteTagRepo = new();
    private readonly Mock<IHistoryRepository> _historyRepo = new();

    private NoteService CreateService() =>
        new(_noteRepo.Object, _tagRepo.Object, _noteTagRepo.Object, _historyRepo.Object);

    // ── GetNote ────────────────────────────────────────────────

    [Fact]
    public void GetNote_WithExistingPath_ReturnsNote()
    {
        _noteRepo.Setup(r => r.GetById("C:\\test.txt"))
                 .Returns(new NoteEntity { Path = "C:\\test.txt", Content = "Hello", IsDeleted = false });
        _noteTagRepo.Setup(r => r.GetTagsForNote("C:\\test.txt")).Returns(new[] { "tag1" });

        var result = CreateService().GetNote("C:\\test.txt");

        Assert.NotNull(result);
        Assert.Equal("Hello", result.Content);
        Assert.Contains("tag1", result.Tags);
    }

    [Fact]
    public void GetNote_WithDeletedNote_ReturnsNull()
    {
        _noteRepo.Setup(r => r.GetById("C:\\test.txt"))
                 .Returns(new NoteEntity { Path = "C:\\test.txt", IsDeleted = true });

        var result = CreateService().GetNote("C:\\test.txt");

        Assert.Null(result);
    }

    [Fact]
    public void GetNote_WithNonExistentPath_ReturnsNull()
    {
        _noteRepo.Setup(r => r.GetById(It.IsAny<string>())).Returns((NoteEntity?)null);

        var result = CreateService().GetNote("C:\\does_not_exist.txt");

        Assert.Null(result);
    }

    // ── CreateNote ─────────────────────────────────────────────

    [Fact]
    public void CreateNote_WithValidInput_SavesNoteAndTags()
    {
        _noteRepo.Setup(r => r.GetById(It.IsAny<string>())).Returns((NoteEntity?)null);
        _noteTagRepo.Setup(r => r.GetTagsForNote(It.IsAny<string>())).Returns(Array.Empty<string>());

        var result = CreateService().CreateNote("C:\\file.txt", "My note", new[] { "work" });

        Assert.NotNull(result);
        Assert.Equal("My note", result.Content);
        _noteRepo.Verify(r => r.Save(It.IsAny<NoteEntity>()), Times.Once);
        _noteTagRepo.Verify(r => r.AddTagToNote("C:\\file.txt", "work"), Times.Once);
    }

    [Fact]
    public void CreateNote_WithBlankContent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateService().CreateNote("C:\\file.txt", "   ", Array.Empty<string>()));
    }

    // ── UpdateNote ─────────────────────────────────────────────

    [Fact]
    public void UpdateNote_WithExistingNote_SavesHistoryThenUpdates()
    {
        var existing = new NoteEntity { Path = "C:\\doc.txt", Content = "Old", IsDeleted = false };
        _noteRepo.Setup(r => r.GetById("C:\\doc.txt")).Returns(existing);
        _historyRepo.Setup(r => r.GetNextVersionNumber("C:\\doc.txt")).Returns(2);
        _noteTagRepo.Setup(r => r.GetTagsForNote(It.IsAny<string>())).Returns(Array.Empty<string>());

        CreateService().UpdateNote("C:\\doc.txt", "New content", Array.Empty<string>());

        _historyRepo.Verify(r => r.Save(It.Is<HistoryEntity>(h => h.Content == "Old")), Times.Once);
        _noteRepo.Verify(r => r.Save(It.Is<NoteEntity>(n => n.Content == "New content")), Times.Once);
    }

    [Fact]
    public void UpdateNote_WithNonExistentNote_CreatesNewNote()
    {
        _noteRepo.Setup(r => r.GetById(It.IsAny<string>())).Returns((NoteEntity?)null);
        _noteTagRepo.Setup(r => r.GetTagsForNote(It.IsAny<string>())).Returns(Array.Empty<string>());

        CreateService().UpdateNote("C:\\new.txt", "Brand new", Array.Empty<string>());

        _noteRepo.Verify(r => r.Save(It.IsAny<NoteEntity>()), Times.Once);
        _historyRepo.Verify(r => r.Save(It.IsAny<HistoryEntity>()), Times.Never);
    }

    // ── DeleteNote ─────────────────────────────────────────────

    [Fact]
    public void DeleteNote_WithExistingNote_DeletesNoteAndTags()
    {
        _noteRepo.Setup(r => r.GetById("C:\\del.txt"))
                 .Returns(new NoteEntity { Path = "C:\\del.txt" });

        CreateService().DeleteNote("C:\\del.txt");

        _noteRepo.Verify(r => r.Delete("C:\\del.txt"), Times.Once);
        _noteTagRepo.Verify(r => r.RemoveAllTagsFromNote("C:\\del.txt"), Times.Once);
    }

    [Fact]
    public void DeleteNote_WithNonExistentNote_DoesNotThrow()
    {
        _noteRepo.Setup(r => r.GetById(It.IsAny<string>())).Returns((NoteEntity?)null);

        var ex = Record.Exception(() => CreateService().DeleteNote("C:\\ghost.txt"));

        Assert.Null(ex);
        _noteRepo.Verify(r => r.Delete(It.IsAny<string>()), Times.Never);
    }

    // ── UpdatePath ─────────────────────────────────────────────

    [Fact]
    public void UpdatePath_CallsRepositoryWithCorrectPaths()
    {
        CreateService().UpdatePath("C:\\old.txt", "C:\\new.txt");

        _noteRepo.Verify(r => r.UpdatePath("C:\\old.txt", "C:\\new.txt"), Times.Once);
    }

    // ── HasNote ────────────────────────────────────────────────

    [Fact]
    public void HasNote_WithExistingActiveNote_ReturnsTrue()
    {
        _noteRepo.Setup(r => r.GetById("C:\\a.txt"))
                 .Returns(new NoteEntity { IsDeleted = false });

        Assert.True(CreateService().HasNote("C:\\a.txt"));
    }

    [Fact]
    public void HasNote_WithDeletedNote_ReturnsFalse()
    {
        _noteRepo.Setup(r => r.GetById("C:\\a.txt"))
                 .Returns(new NoteEntity { IsDeleted = true });

        Assert.False(CreateService().HasNote("C:\\a.txt"));
    }

    // ── MarkAsDeleted ──────────────────────────────────────────

    [Fact]
    public void MarkAsDeleted_WithExistingNote_SetsIsDeletedTrue()
    {
        var entity = new NoteEntity { Path = "C:\\mark.txt", IsDeleted = false };
        _noteRepo.Setup(r => r.GetById("C:\\mark.txt")).Returns(entity);

        CreateService().MarkAsDeleted("C:\\mark.txt");

        _noteRepo.Verify(r => r.Save(It.Is<NoteEntity>(n => n.IsDeleted)), Times.Once);
    }
}
