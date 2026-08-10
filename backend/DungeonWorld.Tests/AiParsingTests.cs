// File: DungeonWorld.Tests/AiParsingTests.cs
using DungeonWorld.Infrastructure.Ai;
using DungeonWorld.Infrastructure.Parsing;
using FluentAssertions;

namespace DungeonWorld.Tests;

public class AiParsingTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Storage", "Uploads", "Seas of Blood.pdf");

    private static TextBlock Block(int logical, string text) =>
        new() { PhysicalPage = logical, LogicalPage = logical, Text = text };

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

    // --- Chunking ---

    [Fact]
    public void Chunk_SplitsIntoPageSizedGroups()
    {
        var pages = Enumerable.Range(1, 21).Select(i => Block(i, $"text {i}")).ToList();

        var chunks = SectionChunker.Chunk(pages, 8);

        chunks.Should().HaveCount(3);
        chunks[0].Should().HaveCount(8);
        chunks[1].Should().HaveCount(8);
        chunks[2].Should().HaveCount(5);
    }

    [Fact]
    public void Chunk_HandlesEmptyInput()
    {
        SectionChunker.Chunk(new List<TextBlock>(), 8).Should().BeEmpty();
    }

    // --- Merging ---

    [Fact]
    public void MergeChunks_ConcatenatesSectionSplitAcrossChunks()
    {
        var chunk1 = new List<LlmSection>
        {
            new() { Number = 1, Content = "The first half of section one." },
            new() { Number = 2, Content = "Beginning of section two." },
        };
        var chunk2 = new List<LlmSection>
        {
            new() { Number = 2, Content = "Rest of section two." },
            new() { Number = 3, Content = "Section three." },
        };

        var merged = SectionChunker.MergeChunks(new[] { chunk1, chunk2 });

        merged.Should().HaveCount(3);
        merged.Single(s => s.Number == 2).Content.Should()
            .Be("Beginning of section two.\n\nRest of section two.");
    }

    [Fact]
    public void MergeChunks_DropsDuplicateInNonConsecutiveChunk()
    {
        var chunk1 = new List<LlmSection> { new() { Number = 5, Content = "original" } };
        var chunk2 = new List<LlmSection> { new() { Number = 7, Content = "seven" } };
        var chunk3 = new List<LlmSection> { new() { Number = 5, Content = "duplicate" } };

        var merged = SectionChunker.MergeChunks(new[] { chunk1, chunk2, chunk3 });

        merged.Should().HaveCount(2);
        merged.Single(s => s.Number == 5).Content.Should().Be("original");
    }

    [Fact]
    public void MergeChunks_OrdersBySectionNumber()
    {
        var chunk = new List<LlmSection>
        {
            new() { Number = 3, Content = "three" },
            new() { Number = 1, Content = "one" },
            new() { Number = 2, Content = "two" },
        };

        SectionChunker.MergeChunks(new[] { chunk }).Select(s => s.Number)
            .Should().Equal(1, 2, 3);
    }

    [Fact]
    public void MergeChunks_IgnoresInvalidSectionNumbers()
    {
        var chunk = new List<LlmSection>
        {
            new() { Number = 0, Content = "invalid" },
            new() { Number = 1, Content = "valid" },
        };

        SectionChunker.MergeChunks(new[] { chunk }).Select(s => s.Number).Should().Equal(1);
    }

    // --- LLM response parsing ---

    [Fact]
    public void LlmSectionParser_ParsesObjectForm()
    {
        var json = """{"sections":[{"number":1,"content":"Hello"}]}""";

        var sections = LlmSectionParser.Parse(json);

        sections.Should().ContainSingle().Which.Number.Should().Be(1);
    }

    [Fact]
    public void LlmSectionParser_ParsesBareArray()
    {
        var json = """[{"number":12,"content":"Twelve"}]""";

        var sections = LlmSectionParser.Parse(json);

        sections.Should().ContainSingle().Which.Number.Should().Be(12);
    }

    [Fact]
    public void LlmSectionParser_ParsesWrappedInMarkdownFence()
    {
        var json = "```json\n{\"sections\":[{\"number\":7,\"content\":\"Seven\"}]}\n```";

        var sections = LlmSectionParser.Parse(json);

        sections.Should().ContainSingle().Which.Number.Should().Be(7);
    }

    [Fact]
    public void LlmSectionParser_FiltersOutOfRangeNumbers()
    {
        var json = """{"sections":[{"number":401,"content":"too high"},{"number":0,"content":"too low"},{"number":42,"content":"ok"}]}""";

        var sections = LlmSectionParser.Parse(json);

        sections.Should().ContainSingle().Which.Number.Should().Be(42);
    }

    [Fact]
    public void LlmSectionParser_ThrowsOnGarbage()
    {
        var act = () => LlmSectionParser.Parse("not json at all");

        act.Should().Throw<InvalidOperationException>();
    }
}
