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
}
