using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF16 Seas of Blood fixes: the printed "turn to no." typo at S70 and the
/// hidden post-combat target 311 noted in S298.
/// </summary>
public sealed class SeasOfBloodParserTests
{
    [Fact]
    public void S70_TurnToNo_BecomesHorrorCombat()
    {
        const string content = "Will you jump overboard (turn to 163) or fight (otherwise, turn to no.)?";
        var fixedContent = SeasOfBloodParser.ApplySectionFixes(70, content);
        Assert.Contains("turn to 110.", fixedContent);
        Assert.DoesNotContain("turn to no.", fixedContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void S298_AddsCyclopsVictoryExit()
    {
        var fixedContent = SeasOfBloodParser.ApplySectionFixes(298, "Note 311 for victory.");
        Assert.Contains("(turn to 311).", fixedContent);
    }

    [Fact]
    public void S298_DoesNotDuplicateExistingExit()
    {
        const string content = "If you win, turn to 311.";
        Assert.Equal(content, SeasOfBloodParser.ApplySectionFixes(298, content));
    }

    [Fact]
    public void UnrelatedSectionsAreLeftAlone()
    {
        const string content = "A plain scene with no fixes.";
        Assert.Equal(content, SeasOfBloodParser.ApplySectionFixes(1, content));
    }

    [Fact]
    public void S50_IsAbdulsVictoryNotThePassScene()
    {
        var content = SeasOfBloodParser.ApplySectionOverrides(50, string.Empty);
        Assert.Contains("lost the wager", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("turn to", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void S91_IsTheCyclopsCombatBlock()
    {
        var content = SeasOfBloodParser.ApplySectionOverrides(91, string.Empty);
        Assert.Contains("Cyclops", content);
        Assert.Contains("turn to 305", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void S92_IsTheRocAmbush()
    {
        var content = SeasOfBloodParser.ApplySectionOverrides(92, string.Empty);
        Assert.Contains("ROC", content);
        Assert.Contains("turn to 124", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void S313AndS314_AreSeparated()
    {
        Assert.Contains("turn to 261", SeasOfBloodParser.ApplySectionOverrides(313, string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("turn to 369", SeasOfBloodParser.ApplySectionOverrides(314, string.Empty), StringComparison.OrdinalIgnoreCase);
    }
}
