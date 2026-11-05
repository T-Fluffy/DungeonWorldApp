using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Parsing;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// FF02's OCR garbles too many exit numbers for <c>TurnToRepair</c> to resolve safely, so the
/// parser attaches the complete Outspaced flowchart to every section as a
/// <see cref="Section.ReferenceOverride"/>. Every chart-vs-scan conflict was page-checked at
/// 300 dpi and the chart was correct in all of them, so no corrections are layered on top.
/// These tests pin the embedded graph itself and the override semantics; the refs golden gates
/// the assembled book when CleanedData is present.
/// </summary>
public sealed class CitadelOfChaosParserTests
{
    private const string EdgesResource =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff02_edges.json";

    private const string ManifestResource =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff02.json";

    [Fact]
    public void EdgeGraph_CoversEverySectionWithTheFlowchartEdges()
    {
        var edges = LoadEdges();

        edges.Keys.Should().BeEquivalentTo(Enumerable.Range(1, 400));
        edges.Values.Sum(targets => targets.Count).Should().Be(715);
    }

    [Theory]
    [InlineData(1, new[] { 20, 230, 261 })]
    [InlineData(3, new[] { 59, 236, 258, 286, 327, 366 })]
    [InlineData(25, new[] { 133, 206, 239 })]
    [InlineData(60, new[] { 33, 213, 295 })]
    [InlineData(99, new[] { 38, 52 })]
    [InlineData(100, new[] { 79, 276 })]
    [InlineData(205, new[] { 309, 368 })]
    [InlineData(228, new[] { 88, 170, 296 })]
    [InlineData(229, new[] { 50, 164, 217 })]
    [InlineData(235, new[] { 245, 269 })]
    [InlineData(245, new[] { 47, 161, 390 })]
    [InlineData(273, new[] { 49, 255, 371 })]
    [InlineData(292, new[] { 42, 64, 376 })]
    [InlineData(330, new[] { 119, 208 })]
    [InlineData(331, new[] { 265 })]
    [InlineData(400, new int[0])]
    public void EdgeGraph_PinsPageVerifiedChartExits(int section, int[] expected)
    {
        LoadEdges()[section].Should().Equal(expected);
    }

    [Fact]
    public void EdgeGraph_ReachesEverySectionFromEntry()
    {
        var edges = LoadEdges();
        var seen = new HashSet<int> { 1 };
        var queue = new Queue<int>();
        queue.Enqueue(1);
        while (queue.Count > 0)
        {
            foreach (var target in edges[queue.Dequeue()])
            {
                if (seen.Add(target)) queue.Enqueue(target);
            }
        }

        seen.Should().BeEquivalentTo(Enumerable.Range(1, 400));
    }

    [Fact]
    public void EdgeGraph_PinsTheTwentyTerminals()
    {
        var edges = LoadEdges();

        edges.Where(pair => pair.Value.Count == 0).Select(pair => pair.Key)
            .Should().Equal(61, 65, 72, 82, 103, 119, 148, 153, 164, 184, 194, 199, 202, 208,
                250, 283, 342, 369, 376, 400);
    }

    [Fact]
    public void ApplyReferenceFixes_AttachesCompleteGraphAndClearsFalseOcrReferences()
    {
        var book = new Book
        {
            Sections = Enumerable.Range(1, 400)
                .Select(number => new Section
                {
                    SectionNumber = number,
                    Content = "Turn to 999.",
                })
                .ToList(),
        };

        CitadelOfChaosParser.ApplyReferenceFixes(book);

        book.Sections.Should().OnlyContain(section => section.ReferenceOverride != null);
        book.Sections.SelectMany(section => section.ReferenceOverride!).Should().NotContain(999);

        // Page-verified exits where the OCR read the wrong number.
        book.Sections.Single(s => s.SectionNumber == 25).ReferenceOverride.Should().Equal(133, 206, 239);
        book.Sections.Single(s => s.SectionNumber == 60).ReferenceOverride.Should().Equal(33, 213, 295);
        book.Sections.Single(s => s.SectionNumber == 99).ReferenceOverride.Should().Equal(38, 52);
        book.Sections.Single(s => s.SectionNumber == 100).ReferenceOverride.Should().Equal(79, 276);
        book.Sections.Single(s => s.SectionNumber == 205).ReferenceOverride.Should().Equal(309, 368);
        book.Sections.Single(s => s.SectionNumber == 235).ReferenceOverride.Should().Equal(245, 269);
        book.Sections.Single(s => s.SectionNumber == 245).ReferenceOverride.Should().Equal(47, 161, 390);
        book.Sections.Single(s => s.SectionNumber == 273).ReferenceOverride.Should().Equal(49, 255, 371);
        book.Sections.Single(s => s.SectionNumber == 292).ReferenceOverride.Should().Equal(42, 64, 376);
        book.Sections.Single(s => s.SectionNumber == 330).ReferenceOverride.Should().Equal(119, 208);
        book.Sections.Single(s => s.SectionNumber == 331).ReferenceOverride.Should().Equal(265);

        // The dotted hidden option (knowing the combination) survives.
        book.Sections.Single(s => s.SectionNumber == 229).ReferenceOverride.Should().Equal(50, 164, 217);

        // Terminals get an empty override, clearing false OCR references.
        book.Sections.Single(s => s.SectionNumber == 400).ReferenceOverride.Should().BeEmpty();
        book.Sections.Single(s => s.SectionNumber == 61).ReferenceOverride.Should().BeEmpty();
    }

    [Fact]
    public void CanHandle_MatchesCitadelOfChaos()
    {
        var parser = new CitadelOfChaosParser(Options.Create(new FileStorageOptions()));

        parser.CanHandle("x.pdf", "FF02 Citadel of Chaos").Should().BeTrue();
        parser.CanHandle("x.pdf", "Citadel of Chaos").Should().BeTrue();
        parser.CanHandle("x.pdf", "FF03 Forest of Doom").Should().BeFalse();
    }

    [Fact]
    public void Manifest_PinsAll400Sections()
    {
        LoadManifestSections().Should().BeEquivalentTo(Enumerable.Range(1, 400));
    }

    private static Dictionary<int, List<int>> LoadEdges()
    {
        using var stream = typeof(CitadelOfChaosParser).Assembly
            .GetManifestResourceStream(EdgesResource);
        stream.Should().NotBeNull($"embedded resource {EdgesResource} must ship with the parser");

        using var doc = JsonDocument.Parse(stream!);
        return doc.RootElement.GetProperty("edges").EnumerateObject()
            .ToDictionary(
                property => int.Parse(property.Name),
                property => property.Value.EnumerateArray().Select(e => e.GetInt32()).ToList());
    }

    private static List<int> LoadManifestSections()
    {
        using var stream = typeof(CitadelOfChaosParser).Assembly
            .GetManifestResourceStream(ManifestResource);
        stream.Should().NotBeNull($"embedded resource {ManifestResource} must ship with the parser");

        using var doc = JsonDocument.Parse(stream!);
        return doc.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => entry.GetProperty("n").GetInt32())
            .ToList();
    }
}
