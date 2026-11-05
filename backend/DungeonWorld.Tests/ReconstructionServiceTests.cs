using DungeonWorld.Infrastructure.Parsing.Reconstruction;
using Xunit;

namespace DungeonWorld.Tests;

public sealed class ReconstructionServiceTests
{
    [Fact]
    public void ApplyManifest_SkipsMissingNextPinWhenFindingSectionEnd()
    {
        var lines = new[]
        {
            new ReconstructionService.OcrLine(1, 0, "M", 0.1, "The first section begins on this line."),
            new ReconstructionService.OcrLine(1, 1, "M", 0.2, "It continues until the next section starts."),
            new ReconstructionService.OcrLine(1, 2, "M", 0.3, "The third section begins on this line."),
        };
        var entries = new[]
        {
            new ReconstructionService.ManifestEntry(1, 1, "M", 0, null),
            new ReconstructionService.ManifestEntry(2, 1, "M", 99, null),
            new ReconstructionService.ManifestEntry(3, 1, "M", 2, null),
        };

        var sections = ReconstructionService.ApplyManifest(lines, entries);

        Assert.Equal(2, sections.Count);
        Assert.Equal(1, sections[0].SectionNumber);
        Assert.Contains("It continues until the next section starts.", sections[0].Content);
        Assert.DoesNotContain("The third section begins on this line.", sections[0].Content);
        Assert.Equal(3, sections[1].SectionNumber);
    }

    [Fact]
    public void ApplyManifest_UsesSideLocalLineNumbersAndReadsLeftSideBeforeRight()
    {
        var lines = new[]
        {
            new ReconstructionService.OcrLine(1, 0, "R", 0.1, "Right side begins with its own first line."),
            new ReconstructionService.OcrLine(1, 1, "R", 0.2, "Right side continues on another line."),
            new ReconstructionService.OcrLine(1, 0, "L", 0.1, "Left side begins with its first line."),
            new ReconstructionService.OcrLine(1, 1, "L", 0.2, "Left side continues on another line."),
        };
        var entries = new[]
        {
            new ReconstructionService.ManifestEntry(1, 1, "L", 0, 1),
            new ReconstructionService.ManifestEntry(2, 1, "R", 0, null),
        };

        var sections = ReconstructionService.ApplyManifest(lines, entries);

        Assert.Equal(2, sections.Count);
        Assert.Contains("Left side begins", sections[0].Content);
        Assert.Contains("Left side continues", sections[0].Content);
        Assert.DoesNotContain("Right side", sections[0].Content);
        Assert.Contains("Right side begins", sections[1].Content);
        Assert.Contains("Right side continues", sections[1].Content);
        Assert.DoesNotContain("Left side", sections[1].Content);
    }

    [Fact]
    public void ApplyManifest_ResolvesLegacyPageGlobalLineNumbers()
    {
        var lines = new[]
        {
            new ReconstructionService.OcrLine(1, 0, "L", 0.1, "Left side first line."),
            new ReconstructionService.OcrLine(1, 1, "L", 0.2, "Left side second line."),
            new ReconstructionService.OcrLine(1, 0, "R", 0.1, "Right side first line."),
            new ReconstructionService.OcrLine(1, 1, "R", 0.2, "Right side second line."),
            new ReconstructionService.OcrLine(1, 2, "R", 0.3, "Right side third line."),
            new ReconstructionService.OcrLine(1, 3, "R", 0.4, "Right side fourth line."),
        };
        var entries = new[]
        {
            // Left-side pin keeps the L half in the transcript set (its entries define text halves).
            new ReconstructionService.ManifestEntry(2, 1, "L", 0, null),
            // Page-global numbering: two left lines, then the right side's second line.
            // Side-local line 3 also exists, so this pins that the global convention wins.
            new ReconstructionService.ManifestEntry(1, 1, "R", 3, null),
        };

        var sections = ReconstructionService.ApplyManifest(
            lines, entries, pageGlobalLineNumbers: true);

        Assert.Equal(2, sections.Count);
        var section = sections.Single(s => s.SectionNumber == 1);
        Assert.Contains("Right side second line.", section.Content);
        Assert.Contains("Right side fourth line.", section.Content);
    }

    [Fact]
    public void ApplyManifest_UsesTranscriptOrderWhenSectionNumbersAreOutOfOrder()
    {
        var lines = new[]
        {
            new ReconstructionService.OcrLine(1, 0, "L", 0.1, "Section 5 begins on the left side."),
            new ReconstructionService.OcrLine(1, 1, "L", 0.2, "Its body ends before the right side."),
            new ReconstructionService.OcrLine(1, 0, "R", 0.1, "Section 4 begins on the right side."),
            new ReconstructionService.OcrLine(1, 1, "R", 0.2, "Its body stays with section 4."),
        };
        var entries = new[]
        {
            new ReconstructionService.ManifestEntry(5, 1, "L", 0, null),
            new ReconstructionService.ManifestEntry(4, 1, "R", 0, null),
        };

        var sections = ReconstructionService.ApplyManifest(lines, entries);

        Assert.Equal(2, sections.Count);
        Assert.Equal(4, sections[0].SectionNumber);
        Assert.Contains("Section 4", sections[0].Content);
        Assert.Equal(5, sections[1].SectionNumber);
        Assert.Contains("Section 5", sections[1].Content);
        Assert.DoesNotContain("Section 4", sections[1].Content);
    }
}
