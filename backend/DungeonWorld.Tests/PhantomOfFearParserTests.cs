using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Parsing;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// FF28's scan garbles too many exit numbers for <c>TurnToRepair</c> to resolve safely, so the
/// parser attaches the complete Outspaced flowchart to every section as a
/// <see cref="Section.ReferenceOverride"/>, with a handful of page-verified corrections. These
/// tests pin the embedded graph itself and the override semantics; the refs golden gates the
/// assembled book when CleanedData is present.
/// </summary>
public sealed class PhantomOfFearParserTests
{
    private const string EdgesResource =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff28_edges.json";

    private const string ManifestResource =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff28.json";

    [Fact]
    public void EdgeGraph_CoversEverySectionWithTheFlowchartEdges()
    {
        var edges = LoadEdges();

        edges.Keys.Should().BeEquivalentTo(Enumerable.Range(1, 400));
        edges.Values.Sum(targets => targets.Count).Should().Be(618);
    }

    [Theory]
    [InlineData(1, new[] { 67, 306 })]
    [InlineData(53, new[] { 364 })]
    [InlineData(190, new[] { 80 })]
    [InlineData(238, new[] { 75, 149, 288 })]
    [InlineData(333, new[] { 313 })]
    [InlineData(328, new int[0])]
    [InlineData(342, new int[0])]
    [InlineData(400, new int[0])]
    public void EdgeGraph_PinsChartExits(int section, int[] expected)
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
    public void ApplyReferenceFixes_AttachesCompleteGraphAndPageVerifiedCorrections()
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

        PhantomOfFearParser.ApplyReferenceFixes(book);

        book.Sections.Should().OnlyContain(section => section.ReferenceOverride != null);
        book.Sections.SelectMany(section => section.ReferenceOverride!).Should().NotContain(999);

        // Corrections where the book disagrees with the chart.
        book.Sections.Single(s => s.SectionNumber == 53).ReferenceOverride.Should().Equal(73, 364);
        book.Sections.Single(s => s.SectionNumber == 190).ReferenceOverride.Should().Equal(50);
        book.Sections.Single(s => s.SectionNumber == 328).ReferenceOverride.Should().Equal(348);
        book.Sections.Single(s => s.SectionNumber == 333).ReferenceOverride.Should().Equal(26, 345);
        book.Sections.Single(s => s.SectionNumber == 342).ReferenceOverride.Should().Equal(362);

        // Chart edges that survive unchanged, including dotted hidden options.
        book.Sections.Single(s => s.SectionNumber == 1).ReferenceOverride.Should().Equal(67, 306);
        book.Sections.Single(s => s.SectionNumber == 238).ReferenceOverride.Should().Equal(75, 149, 288);
        book.Sections.Single(s => s.SectionNumber == 400).ReferenceOverride.Should().BeEmpty();
    }

    [Fact]
    public void CanHandle_MatchesPhantomsOfFear()
    {
        var parser = new PhantomOfFearParser(Options.Create(new FileStorageOptions()));

        parser.CanHandle("x.pdf", "FF28 Phantoms of Fear").Should().BeTrue();
        parser.CanHandle("x.pdf", "Phantoms of Fear").Should().BeTrue();
        parser.CanHandle("x.pdf", "FF27 Star Strider").Should().BeFalse();
    }

    [Fact]
    public void Manifest_PinsAll400Sections()
    {
        LoadManifestSections().Should().BeEquivalentTo(Enumerable.Range(1, 400));
    }

    private static Dictionary<int, List<int>> LoadEdges()
    {
        using var stream = typeof(PhantomOfFearParser).Assembly
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
        using var stream = typeof(PhantomOfFearParser).Assembly
            .GetManifestResourceStream(ManifestResource);
        stream.Should().NotBeNull($"embedded resource {ManifestResource} must ship with the parser");

        using var doc = JsonDocument.Parse(stream!);
        return doc.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => entry.GetProperty("n").GetInt32())
            .ToList();
    }
}
