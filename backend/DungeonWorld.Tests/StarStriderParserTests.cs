using System.Text.Json;
using DungeonWorld.Cleaning.Cleaner;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Parsing;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// FF27's scan garbles too many exit numbers for <c>TurnToRepair</c> to resolve safely, so the
/// parser attaches the complete Outspaced flowchart to every section as a
/// <see cref="Section.ReferenceOverride"/>. These tests pin the embedded graph itself and the
/// cleaner's override semantics; the refs golden gates the assembled book when CleanedData is
/// present.
/// </summary>
public sealed class StarStriderParserTests
{
    private const string EdgesResource =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff27_edges.json";

    private const string ManifestResource =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff27.json";

    [Fact]
    public void EdgeGraph_CoversEverySectionWithTheFlowchartEdges()
    {
        var edges = LoadEdges();

        edges.Keys.Should().BeEquivalentTo(Enumerable.Range(1, 400));
        edges.Values.Sum(targets => targets.Count).Should().Be(550);
    }

    [Theory]
    [InlineData(1, new[] { 69, 199, 203 })]
    [InlineData(2, new[] { 7, 27 })]
    [InlineData(5, new[] { 112, 160 })]
    [InlineData(7, new[] { 21 })]
    [InlineData(8, new[] { 127, 169 })]
    [InlineData(14, new[] { 136 })]
    [InlineData(400, new int[0])]
    [InlineData(178, new int[0])]
    [InlineData(294, new int[0])]
    public void EdgeGraph_PinsOcrGarbledExits(int section, int[] expected)
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
    public void CanHandle_MatchesStarStrider()
    {
        var parser = new StarStriderParser(Options.Create(new FileStorageOptions()));

        parser.CanHandle("x.pdf", "FF27 Star Strider").Should().BeTrue();
        parser.CanHandle("x.pdf", "Star Strider").Should().BeTrue();
        parser.CanHandle("x.pdf", "FF26 Crypts of the Sorcerer").Should().BeFalse();
    }

    [Fact]
    public void Manifest_PinsAll400Sections()
    {
        LoadManifestSections().Should().BeEquivalentTo(Enumerable.Range(1, 400));
    }

    [Fact]
    public void Section396_HasEndPinAgainstDuplicatePage()
    {
        // PDF p114 is a duplicate scan of the final page (397-400). Section 396's manifest
        // pin must carry an explicit end so ApplyManifest stops at p113R line 37 instead of
        // swallowing all of p114 plus p115.
        using var stream = typeof(StarStriderParser).Assembly
            .GetManifestResourceStream(ManifestResource);
        stream.Should().NotBeNull();
        using var doc = JsonDocument.Parse(stream!);
        var entry = doc.RootElement.GetProperty("entries").EnumerateArray()
            .First(e => e.GetProperty("n").GetInt32() == 396);
        entry.GetProperty("page").GetInt32().Should().Be(113);
        entry.GetProperty("side").GetString().Should().Be("R");
        entry.TryGetProperty("end", out var end).Should().BeTrue();
        end.GetInt32().Should().Be(37);
    }

    [Fact]
    public void ContentAnalyzer_OverrideReplacesGarbledRefsAndKeepsMatchingLabel()
    {
        var section = new Section
        {
            SectionNumber = 7,
            Content = "Add 2 STAMINA points and lurn to 149.",
            ReferenceOverride = new List<int> { 21 },
        };

        var cleaned = ContentAnalyzer.Analyze(section);

        cleaned.References.Should().Equal(21);
        cleaned.Choices.Select(c => c.Target).Should().Equal(21);
        cleaned.Features.IsEnd.Should().BeFalse();
    }

    [Fact]
    public void ContentAnalyzer_EmptyOverrideClearsFalseExitAndMarksEnd()
    {
        var section = new Section
        {
            SectionNumber = 400,
            Content = "You have succeeded in your mission. Your adventure ends here.",
            ReferenceOverride = new List<int>(),
        };

        var cleaned = ContentAnalyzer.Analyze(section);

        cleaned.References.Should().BeEmpty();
        cleaned.Choices.Should().BeEmpty();
        cleaned.Features.IsEnd.Should().BeTrue();
    }

    private static Dictionary<int, List<int>> LoadEdges()
    {
        using var stream = typeof(StarStriderParser).Assembly
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
        using var stream = typeof(StarStriderParser).Assembly
            .GetManifestResourceStream(ManifestResource);
        stream.Should().NotBeNull($"embedded resource {ManifestResource} must ship with the parser");

        using var doc = JsonDocument.Parse(stream!);
        return doc.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => entry.GetProperty("n").GetInt32())
            .ToList();
    }
}
