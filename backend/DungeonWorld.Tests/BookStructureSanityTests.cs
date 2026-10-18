// File: DungeonWorld.Tests/BookStructureSanityTests.cs
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Guards against a collapse of section boundaries that the refs golden cannot
/// see: a manifest whose adjacent entries share a start pin makes the earlier
/// section assemble to an empty body and dumps its text into the following one.
/// That leaves the graph internally consistent (dangling 0, choices == refs) while
/// the readable text is destroyed, so it needs its own assertion:
///   * every section has a non-empty Raw body unless it is flagged MissingText;
///   * no section body exceeds <see cref="MaxBodyChars"/> (a merged neighbour).
/// Skips (passes vacuously) when the gitignored CleanedData output is absent.
/// The <see cref="KnownBad"/> allowlist is temporary - it shrinks as the affected
/// books are re-pinned (FF09-FF15 legacy manifests predate the pinned-verify gate).
/// </summary>
public class BookStructureSanityTests
{
    /// <summary>Longest plausible single section; mirrors verify_books.MAX_BODY_CHARS.</summary>
    private const int MaxBodyChars = 5000;

    /// <summary>
    /// Books known to have collapsed manifests at the time the gate was added.
    /// Each is removed here as its re-pin lands, so the gate becomes authoritative
    /// for the whole catalogue. Empty: FF09-FF15 and FF19 have all been re-pinned.
    /// </summary>
    private static readonly HashSet<string> KnownBad = new(StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<object[]> CleanedBooks()
    {
        var dir = FindCleanedDir();
        if (dir is null) yield break;
        foreach (var f in Directory.GetFiles(dir, "*.json").OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(f);
            if (KnownBad.Contains(name)) continue;
            yield return new object[] { name };
        }
    }

    [Theory]
    [MemberData(nameof(CleanedBooks))]
    public void BookBody_IsStructurallySane(string cleanedFileName)
    {
        string? path = FindCleanedBook(cleanedFileName);
        if (path is null) return; // no local book output: nothing to compare

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var sections = doc.RootElement.GetProperty("Sections").EnumerateArray().ToList();

        var empties = new List<int>();
        var oversize = new List<(int Number, int Length)>();
        foreach (var s in sections)
        {
            int number = s.GetProperty("Number").GetInt32();
            string raw = s.TryGetProperty("Raw", out var r) && r.ValueKind == JsonValueKind.String
                ? r.GetString() ?? string.Empty
                : string.Empty;
            bool missing = s.TryGetProperty("Features", out var f)
                           && f.TryGetProperty("MissingText", out var mt)
                           && mt.ValueKind == JsonValueKind.True;
            if (string.IsNullOrWhiteSpace(raw) && !missing) empties.Add(number);
            if (raw.Length >= MaxBodyChars) oversize.Add((number, raw.Length));
        }

        empties.Should().BeEmpty(
            $"{cleanedFileName} has section(s) with an empty body (missing/collapsed pin)");
        oversize.Should().BeEmpty(
            $"{cleanedFileName} has over-long body(ies) (>= {MaxBodyChars} chars, likely merged neighbours)");
    }

    private static string? FindCleanedDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            foreach (var candidate in new[]
            {
                Path.Combine(dir.FullName, "Storage", "Books", "CleanedData"),
                Path.Combine(dir.FullName, "backend", "Storage", "Books", "CleanedData"),
            })
            {
                if (Directory.Exists(candidate)) return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static string? FindCleanedBook(string fileName)
    {
        var dir = FindCleanedDir();
        if (dir is null) return null;
        var path = Path.Combine(dir, fileName);
        return File.Exists(path) ? path : null;
    }
}
