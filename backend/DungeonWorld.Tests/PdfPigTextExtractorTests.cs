// File: DungeonWorld.Tests/PdfPigTextExtractorTests.cs
using DungeonWorld.Infrastructure.Parsing;
using FluentAssertions;

namespace DungeonWorld.Tests;

public class PdfPigTextExtractorTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Storage", "Books", "Seas of Blood.pdf");

    // --- PdfPig text extraction (real fixture) ---

    [Fact]
    public void PdfPigTextExtractor_ExtractsAllLogicalPages_FromFixture()
    {
        Assert.True(File.Exists(FixturePath), "The 'Seas of Blood.pdf' fixture must be present.");

        var extractor = new PdfPigTextExtractor();
        var blocks = extractor.Extract(FixturePath);

        // single-page scan, one logical page per PDF page; image-only pages are
        // skipped so a page with no text produces no block.
        blocks.GroupBy(b => b.LogicalPage).Count().Should().BeGreaterThan(200);
    }

    [Fact]
    public void PdfPigTextExtractor_ProducesNonEmptyText_FromFixture()
    {
        Assert.True(File.Exists(FixturePath), "The 'Seas of Blood.pdf' fixture must be present.");

        var extractor = new PdfPigTextExtractor();
        var blocks = extractor.Extract(FixturePath);

        var all = string.Join("\n", blocks.Select(b => b.Text));
        all.Trim().Length.Should().BeGreaterThan(1000);

        // Fighting Fantasy section markers should be present in readable scans.
        all.Should().Contain("SKILL");
        all.Should().Contain("turn to", "section choices use 'turn to N' phrasing");
    }
}
