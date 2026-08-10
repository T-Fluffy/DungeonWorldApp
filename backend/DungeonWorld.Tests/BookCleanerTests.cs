using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Cleaning;
using DungeonWorld.Cleaning.Cleaner;
using DungeonWorld.Cleaning.Model;

namespace DungeonWorld.Tests;

public class BookCleanerTests
{
    [Fact]
    public void RulesExtractor_ExtractsCoreStatFormulas()
    {
        var intro = "Roll one die. Add 6 to the result. Enter this total as your SKILL score. " +
                    "Roll two dice. Add 12 to the result. Enter this total as your STAMINA score.";

        var rules = RulesExtractor.Extract(intro);

        Assert.Contains(rules, r => r.Stat == "SKILL" && r.Formula == "1d6+6");
        Assert.Contains(rules, r => r.Stat == "STAMINA" && r.Formula == "2d6+12");
    }

    [Fact]
    public void ContentAnalyzer_DetectsCombatAndEnemies()
    {
        var section = new Section
        {
            SectionNumber = 12,
            Content = "Before you stands a slavering Hound.\nHound SKILL 8 STAMINA 9\nIf you win, turn to 4."
        };

        var cleaned = ContentAnalyzer.Analyze(section);

        Assert.True(cleaned.Features.HasCombat);
        var hound = Assert.Single(cleaned.Features.Enemies);
        Assert.Equal("Hound", hound.Name);
        Assert.Equal(8, hound.Skill);
        Assert.Equal(9, hound.Stamina);
        Assert.True(hound.HasStats);
        Assert.Contains(4, cleaned.References);
        Assert.Single(cleaned.Choices);
    }

    [Fact]
    public void ContentAnalyzer_FlagsDeathEnd_WhenSectionHasNoReferences()
    {
        var section = new Section
        {
            SectionNumber = 300,
            Content = "The pit swallows you whole. Your adventure ends here."
        };

        var cleaned = ContentAnalyzer.Analyze(section);

        Assert.Empty(cleaned.References);
        Assert.True(cleaned.Features.IsEnd);
        Assert.True(cleaned.Features.DeathEnd);
        Assert.False(cleaned.Features.VictoryEnd);
    }

    [Fact]
    public void Clean_EndToEnd_ProducesExpectedGraphAndSummary()
    {
        var book = new Book
        {
            Title = "Test Dungeon",
            Introduction = "Roll one die. Add 6 to the result. Enter this total as your SKILL score. " +
                           "Roll two dice. Add 12 to the result. Enter this total as your STAMINA score.",
            Sections = new List<Section>
            {
                new() { SectionNumber = 1, Content = "Left: you wade into the swamp.\nTurn to 2.\nRight: the tower looms.\nTurn to 3." },
                new() { SectionNumber = 2, Content = "Before you stands a slavering Hound.\nHound SKILL 8 STAMINA 9\nIf you win, turn to 4." },
                new() { SectionNumber = 3, Content = "The tower is empty. Your adventure ends here." },
                new() { SectionNumber = 4, Content = "You find a chest. Add 5 gold pieces to your pouch. Turn to 5." },
                new() { SectionNumber = 5, Content = "Nothing here. Your adventure ends here." },
            }
        };

        var cleaned = BookCleaner.Clean(book, "Test Dungeon.json");

        Assert.Equal("Test Dungeon", cleaned.Meta.Title);
        Assert.Equal(5, cleaned.Meta.SectionCount);
        Assert.Equal(0, cleaned.Meta.MissingSectionCount);

        Assert.Equal(1, cleaned.Meta.CombatSectionCount);
        Assert.Equal(1, cleaned.Meta.EnemyCount);

        Assert.Equal(new[] { 3, 5 }, cleaned.Graph.DeadEnds);
        Assert.Equal(new[] { 3, 5 }, cleaned.Graph.Terminal);
        Assert.Empty(cleaned.Graph.Unreachable);
        Assert.Empty(cleaned.Graph.OrphanLinks);
        Assert.Equal(3, cleaned.Graph.MaxDepthFromEntry);

        Assert.Contains(cleaned.Rules, r => r.Stat == "SKILL" && r.Formula == "1d6+6");
        Assert.Contains(cleaned.Rules, r => r.Stat == "STAMINA" && r.Formula == "2d6+12");

        var chest = cleaned.Sections.Single(s => s.Number == 4);
        Assert.Contains(chest.Features.Booty, b => b.Contains("5"));
    }

    [Fact]
    public void CleanedBook_SerializesToCamelCase_AndRoundTrips()
    {
        var cleaned = new CleanedBook
        {
            Meta = new CleanedMeta { Title = "Round Trip", SectionCount = 1, PresentSectionCount = 1 },
            Rules = { new CleanedRule { Stat = "SKILL", Formula = "1d6+6" } },
            Sections =
            {
                new CleanedSection
                {
                    Number = 1,
                    Raw = "Hound SKILL 8 STAMINA 9",
                    Features = new CleanedFeatures { HasCombat = true }
                }
            }
        };

        var json = JsonSerializer.Serialize(cleaned, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        Assert.Contains("\"schemaVersion\"", json);
        Assert.Contains("\"sectionCount\"", json);
        Assert.Contains("\"hasCombat\"", json);
        Assert.DoesNotContain("\"SchemaVersion\"", json);

        var roundTripped = JsonSerializer.Deserialize<CleanedBook>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(roundTripped);
        Assert.Equal("Round Trip", roundTripped!.Meta.Title);
        Assert.Equal(1, roundTripped.Sections[0].Number);
        Assert.True(roundTripped.Sections[0].Features.HasCombat);
    }
}
