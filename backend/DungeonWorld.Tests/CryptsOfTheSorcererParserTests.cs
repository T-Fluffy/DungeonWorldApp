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
/// FF26's scan garbles too many exit numbers for <c>TurnToRepair</c> to resolve safely, so the
/// parser attaches the complete Outspaced flowchart to every section as a
/// <see cref="Section.ReferenceOverride"/>. These tests pin the embedded graph itself and the
/// cleaner's override semantics; the refs golden gates the assembled book when CleanedData is
/// present.
/// </summary>
public sealed class CryptsOfTheSorcererParserTests
{
    private const string EdgesResource =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff26_edges.json";

    private const string ManifestResource =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff26.json";

    [Fact]
    public void EdgeGraph_CoversEverySectionWithTheFlowchartEdges()
    {
        var edges = LoadEdges();

        edges.Keys.Should().BeEquivalentTo(Enumerable.Range(1, 400));
        edges.Values.Sum(targets => targets.Count).Should().Be(605);
    }

    [Theory]
    [InlineData(5, new[] { 108, 303 })]
    [InlineData(14, new int[0])]
    [InlineData(15, new int[0])]
    [InlineData(16, new[] { 100, 171 })]
    [InlineData(17, new[] { 30 })]
    [InlineData(36, new[] { 178 })]
    [InlineData(43, new[] { 132, 201 })]
    [InlineData(165, new[] { 289 })]
    [InlineData(234, new[] { 92, 316 })]
    [InlineData(249, new[] { 13, 346, 388 })]
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
    public void CanHandle_MatchesBothStoredSpellings()
    {
        var parser = new CryptsOfTheSorcererParser(Options.Create(new FileStorageOptions()));

        parser.CanHandle("x.pdf", "FF26 Crypts of the Sorcerer").Should().BeTrue();
        parser.CanHandle("x.pdf", "Crypt of the Sorcerer").Should().BeTrue();
        parser.CanHandle("x.pdf", "FF25 Beneath Nightmare Castle").Should().BeFalse();
    }

    [Fact]
    public void Manifest_PinsEverySectionTheScanContains()
    {
        // The scan jumps from the 12-13 spread (PDF p17) to section 18 (p18); the
        // page carrying 14-17 is absent. Those four bodies stay MissingText, but the
        // flowchart still supplies their edges, so the graph remains complete.
        LoadManifestSections().Should().BeEquivalentTo(
            Enumerable.Range(1, 400).Except(new[] { 14, 15, 16, 17 }));
    }

    [Fact]
    public void ContentAnalyzer_OverrideReplacesGarbledRefsAndKeepsMatchingLabel()
    {
        var section = new Section
        {
            SectionNumber = 43,
            Content = "If you are Lucky, turn to 132.\nIf you are Unlucky, tum to 20m.",
            ReferenceOverride = new List<int> { 132, 201 },
        };

        var cleaned = ContentAnalyzer.Analyze(section);

        cleaned.References.Should().Equal(132, 201);
        cleaned.Choices.Select(c => c.Target).Should().Equal(132, 201);
        cleaned.Choices.Single(c => c.Target == 132).Label.Should().Be("If you are Lucky,");
        cleaned.Choices.Single(c => c.Target == 201).Label.Should().BeNull();
        cleaned.Features.IsEnd.Should().BeFalse();
    }

    [Fact]
    public void ContentAnalyzer_EmptyOverrideClearsFalseExitAndMarksEnd()
    {
        var section = new Section
        {
            SectionNumber = 14,
            Content = "You fall screaming to your death. Your adventure ends here. Turn to 99.",
            ReferenceOverride = new List<int>(),
        };

        var cleaned = ContentAnalyzer.Analyze(section);

        cleaned.References.Should().BeEmpty();
        cleaned.Choices.Should().BeEmpty();
        cleaned.Features.IsEnd.Should().BeTrue();
        cleaned.Features.DeathEnd.Should().BeTrue();
    }

    private static Dictionary<int, List<int>> LoadEdges()
    {
        using var stream = typeof(CryptsOfTheSorcererParser).Assembly
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
        using var stream = typeof(CryptsOfTheSorcererParser).Assembly
            .GetManifestResourceStream(ManifestResource);
        stream.Should().NotBeNull($"embedded resource {ManifestResource} must ship with the parser");

        using var doc = JsonDocument.Parse(stream!);
        return doc.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => entry.GetProperty("n").GetInt32())
            .ToList();
    }
}
