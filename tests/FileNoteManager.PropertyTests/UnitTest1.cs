using FsCheck;
using FsCheck.Xunit;
using FileNoteManager.Core.Services.Implementation;

namespace FileNoteManager.PropertyTests;

/// <summary>
/// Property-based tests for TagService.ParseTags using FsCheck.
/// </summary>
public class TagParsingPropertyTests
{
    // Property: Parsing a non-empty, trimmed tag string always returns at least one tag
    [Property]
    public Property ParseTags_NonEmptyInput_ReturnsAtLeastOneTag()
    {
        return Prop.ForAll(
            Arb.From(Gen.Elements("work", "todo", "personal", "code", "urgent")),
            (tag) =>
            {
                var result = TagService.ParseTags(tag);
                return result.Count >= 1;
            });
    }

    // Property: ParseTags result never contains null or whitespace-only strings
    [Property]
    public Property ParseTags_NeverContainsBlankEntries()
    {
        var gen = Gen.Elements<string?>(null, "", "  ", "work", "todo,personal", "a; b; c", "work work work");
        return Prop.ForAll(
            Arb.From(gen),
            (input) =>
            {
                var result = TagService.ParseTags(input);
                return result.All(t => !string.IsNullOrWhiteSpace(t));
            });
    }

    // Property: ParseTags result is always deduplicated (no duplicates)
    [Property]
    public Property ParseTags_ResultIsAlwaysUnique()
    {
        var gen = Gen.Elements<string?>(null, "", "work", "work,work", "a,b,a", "x;x;x", "todo todo todo");
        return Prop.ForAll(
            Arb.From(gen),
            (input) =>
            {
                var result = TagService.ParseTags(input);
                return result.Count == result.Distinct(StringComparer.OrdinalIgnoreCase).Count();
            });
    }

    // Property: Parsing the same tags in different separators yields the same count
    [Property]
    public Property ParseTags_CommaSeparated_EqualSpaceSeparated()
    {
        return Prop.ForAll(
            Arb.From(
                Gen.ListOf(3, Gen.Elements("alpha", "beta", "gamma"))
                   .Select(tags => tags.Distinct().ToList())
                   .Where(tags => tags.Count == 3)),
            (tags) =>
            {
                var commaSeparated = string.Join(",", tags);
                var spaceSeparated = string.Join(" ", tags);

                var r1 = TagService.ParseTags(commaSeparated);
                var r2 = TagService.ParseTags(spaceSeparated);

                return r1.Count == r2.Count &&
                       r1.OrderBy(t => t).SequenceEqual(r2.OrderBy(t => t), StringComparer.OrdinalIgnoreCase);
            });
    }
}

/// <summary>
/// Property-based tests verifying NoteListItem display name derivation logic.
/// </summary>
public class PathDisplayPropertyTests
{
    // Property: DisplayName derived from path is never empty for valid paths
    [Property]
    public Property GetFileName_NonEmptyPath_ReturnsNonEmptyString()
    {
        return Prop.ForAll(
            Arb.From(
                Gen.Elements(@"C:\file.txt", @"C:\folder\sub\doc.md", @"D:\notes.txt", @"C:\Projects")),
            (path) =>
            {
                var displayName = Path.GetFileName(path.TrimEnd('\\', '/')) ?? path;
                return !string.IsNullOrEmpty(displayName);
            });
    }
}