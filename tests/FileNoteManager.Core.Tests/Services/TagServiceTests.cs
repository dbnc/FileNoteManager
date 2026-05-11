using Moq;
using FileNoteManager.Core.Repositories;
using FileNoteManager.Core.Repositories.Interfaces;
using FileNoteManager.Core.Services.Implementation;

namespace FileNoteManager.Core.Tests.Services;

public class TagServiceTests
{
    private readonly Mock<ITagRepository> _tagRepo = new();
    private readonly Mock<INoteTagRepository> _noteTagRepo = new();
    private readonly Mock<INoteRepository> _noteRepo = new();

    private TagService CreateService() =>
        new(_tagRepo.Object, _noteTagRepo.Object, _noteRepo.Object);

    // ── GetAllTags ─────────────────────────────────────────────

    [Fact]
    public void GetAllTags_ReturnsMappedTags()
    {
        _tagRepo.Setup(r => r.GetAll()).Returns(new[]
        {
            new TagEntity { Name = "work", UsageCount = 3, CreatedAt = DateTime.UtcNow },
            new TagEntity { Name = "personal", UsageCount = 1, CreatedAt = DateTime.UtcNow }
        });

        var tags = CreateService().GetAllTags().ToList();

        Assert.Equal(2, tags.Count);
        Assert.Contains(tags, t => t.Name == "work" && t.UsageCount == 3);
    }

    [Fact]
    public void GetAllTags_WhenEmpty_ReturnsEmptyCollection()
    {
        _tagRepo.Setup(r => r.GetAll()).Returns(Array.Empty<TagEntity>());

        Assert.Empty(CreateService().GetAllTags());
    }

    // ── GetTagByName ───────────────────────────────────────────

    [Fact]
    public void GetTagByName_WithExistingTag_ReturnsTag()
    {
        _tagRepo.Setup(r => r.GetByName("work"))
                .Returns(new TagEntity { Name = "work", UsageCount = 5 });

        var tag = CreateService().GetTagByName("work");

        Assert.NotNull(tag);
        Assert.Equal("work", tag.Name);
    }

    [Fact]
    public void GetTagByName_WithNonExistentTag_ReturnsNull()
    {
        _tagRepo.Setup(r => r.GetByName(It.IsAny<string>())).Returns((TagEntity?)null);

        Assert.Null(CreateService().GetTagByName("missing"));
    }

    // ── GetMostUsedTags ────────────────────────────────────────

    [Fact]
    public void GetMostUsedTags_ReturnsCorrectCount()
    {
        _tagRepo.Setup(r => r.GetMostUsed(3)).Returns(new[]
        {
            new TagEntity { Name = "a", UsageCount = 10 },
            new TagEntity { Name = "b", UsageCount = 7 },
            new TagEntity { Name = "c", UsageCount = 4 }
        });

        var result = CreateService().GetMostUsedTags(3).ToList();

        Assert.Equal(3, result.Count);
        _tagRepo.Verify(r => r.GetMostUsed(3), Times.Once);
    }

    // ── GetTagFrequency ────────────────────────────────────────

    [Fact]
    public void GetTagFrequency_DelegatesToNoteTagRepository()
    {
        var expected = new Dictionary<string, int> { ["work"] = 5, ["todo"] = 2 };
        _noteTagRepo.Setup(r => r.GetTagFrequency()).Returns(expected);

        var result = CreateService().GetTagFrequency();

        Assert.Equal(expected, result);
    }

    // ── ParseTags (static helper) ──────────────────────────────

    [Theory]
    [InlineData("work,personal,code", 3)]
    [InlineData("work personal code", 3)]
    [InlineData("work; personal; code", 3)]
    [InlineData("  work  ,  personal  ", 2)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void ParseTags_WithVariousInputs_ReturnsParsedCount(string? input, int expected)
    {
        var result = TagService.ParseTags(input);
        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public void ParseTags_DeduplicatesCaseInsensitive()
    {
        var result = TagService.ParseTags("Work,work,WORK");
        Assert.Single(result);
    }

    [Fact]
    public void ParseTags_IgnoresBlankEntries()
    {
        var result = TagService.ParseTags("work,,, ,personal");
        Assert.Equal(2, result.Count);
    }
}
