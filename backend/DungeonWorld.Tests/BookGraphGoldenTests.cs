// File: DungeonWorld.Tests/BookGraphGoldenTests.cs
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Guards the parsed book graphs against silent edge loss: every section's
/// outgoing references must exactly match the committed golden snapshot, with
/// no dangling targets and choices identical to references. Skips (not fails)
/// when the gitignored CleanedData output is absent, so CI without book data
/// stays green while local re-parses are fully gated.
/// </summary>
public class BookGraphGoldenTests
{
    /// <summary>
    /// Dangling refs that are known-open book-batch items, not regressions.
    /// Keyed by cleaned-data file name, then section number. Remove entries
    /// here as the book answers land (FF02 §77 Creature-Copy number).
    /// </summary>
    private static readonly Dictionary<string, Dictionary<int, int[]>> KnownDangling = new()
    {
        ["FF02 Citadel of Chaos.json"] = new() { [77] = new[] { 940 } },
    };
    public static IEnumerable<object[]> GoldenResources()
    {
        var asm = typeof(BookGraphGoldenTests).Assembly;
        return asm.GetManifestResourceNames()
            .Where(n => n.EndsWith(".refs.json", StringComparison.OrdinalIgnoreCase))
            .Select(n => new object[] { n });
    }

    [Theory]
    [MemberData(nameof(GoldenResources))]
    public void BookGraph_MatchesGoldenSnapshot(string resourceName)
    {
        var asm = typeof(BookGraphGoldenTests).Assembly;
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing resource {resourceName}.");
        using var goldenDoc = JsonDocument.Parse(stream);
        var goldenRoot = goldenDoc.RootElement;
        string fileName = goldenRoot.GetProperty("file").GetString()!;
        int maxSection = goldenRoot.GetProperty("maxSection").GetInt32();
        var goldenRefs = goldenRoot.GetProperty("sections").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.EnumerateArray().Select(e => e.GetInt32()).ToList());

        string? cleanedPath = FindCleanedBook(fileName);
        // xUnit 2.5 has no dynamic skip: without local (gitignored) book output
        // there is nothing to compare, so pass vacuously. Local re-parses with
        // data present run the full assertion set below.
        if (cleanedPath is null)
            return;

        using var cleanedDoc = JsonDocument.Parse(File.ReadAllText(cleanedPath));
        var sections = cleanedDoc.RootElement.GetProperty("Sections").EnumerateArray().ToList();

        sections.Should().HaveCount(goldenRefs.Count, $"{fileName} section count changed");
        sections.Select(s => s.GetProperty("Number").GetInt32()).OrderBy(n => n)
            .Should().Equal(Enumerable.Range(1, maxSection), $"{fileName} must stay contiguous 1..{maxSection}");

        foreach (var s in sections)
        {
            string key = s.GetProperty("Number").GetInt32().ToString();
            var refs = s.GetProperty("References").EnumerateArray().Select(e => e.GetInt32()).OrderBy(n => n).ToList();
            refs.Should().Equal(goldenRefs[key], $"{fileName} section {key} refs changed");
            var allowed = KnownDangling.TryGetValue(fileName, out var bySection) &&
                          bySection.TryGetValue(int.Parse(key), out var targets)
                ? targets.ToHashSet()
                : new HashSet<int>();
            refs.Where(r => r < 1 || r > maxSection).Should().BeEquivalentTo(allowed,
                $"{fileName} section {key} has new dangling ref");
            var choices = s.GetProperty("Choices").EnumerateArray()
                .Select(c => c.GetProperty("Target").GetInt32()).OrderBy(n => n).ToList();
            choices.Should().Equal(refs, $"{fileName} section {key} choices != refs");
        }
    }

    private static string? FindCleanedBook(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(dir.FullName, "Storage", "Books", "CleanedData", fileName),
                Path.Combine(dir.FullName, "backend", "Storage", "Books", "CleanedData", fileName),
            })
            {
                if (File.Exists(candidate)) return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
