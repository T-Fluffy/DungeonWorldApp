// File: DungeonWorld.Tests/TurnToRepairTests.cs
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Text;
using DungeonWorld.Cleaning;
using DungeonWorld.Cleaning.Cleaner;
using FluentAssertions;

namespace DungeonWorld.Tests;

public class TurnToRepairTests
{
    [Theory]
    [InlineData("Tum o 327", "turn to 327")]
    [InlineData("furn te 221", "turn to 221")]
    [InlineData("fum to 1o", "turn to 10")]
    [InlineData("tumn to 32z", "turn to 322")]
    [InlineData("Turnto261", "turn to 261")]
    [InlineData("turmn to 268", "turn to 268")]
    public void RepairContent_NormalizesVerbVariantsAndDigits(string garbled, string expected)
    {
        TurnToRepair.RepairContent($"Choose {garbled}.").Should().Contain(expected);
    }

    [Theory]
    [InlineData("Turn to o261", "Turn to 261")]
    [InlineData("Turn to z10", "Turn to 210")]
    [InlineData("Turn to 8s", "Turn to 85")]
    [InlineData("turn to 1o3", "turn to 103")]
    [InlineData("Turn to 8o", "Turn to 80")]
    public void RepairContent_RepairsUnambiguousDigitConfusions(string garbled, string expected)
    {
        TurnToRepair.RepairContent(garbled).Should().Be(expected);
    }

    [Theory]
    [InlineData("Turn to g6")]   // g -> 6 or 9: ambiguous, left for hand fix
    [InlineData("turn to go")]   // ambiguous, left for hand fix
    [InlineData("turn to zog")]  // ambiguous, left for hand fix
    [InlineData("Turn to agy")]  // out of range either way
    [InlineData("Turn to 497")]  // out of range for a 400-section book
    public void RepairContent_LeavesAmbiguousTargetsUntouched(string input)
    {
        TurnToRepair.RepairContent(input, maxSection: 400).Should().Be(input);
    }

    [Theory]
    [InlineData("Turn to 12")]
    [InlineData("turn north along the pass")]
    [InlineData("turn of events")]
    [InlineData("If you win, turn to 4.")]
    public void RepairContent_IsIdempotentOnCleanText(string input)
    {
        TurnToRepair.RepairContent(input).Should().Be(input);
    }

    [Fact]
    public void RepairContent_RespectsMaxSection()
    {
        TurnToRepair.RepairContent("Turn to o261", maxSection: 343).Should().Be("Turn to 261");
        TurnToRepair.RepairContent("Turn to 399", maxSection: 343).Should().Be("Turn to 399");
    }
}

public class ChoicesParityTests
{
    private static Section Sec(int n, string content) =>
        new() { SectionNumber = n, Content = content };

    [Fact]
    public void BareExitFooter_BecomesChoice()
    {
        var cleaned = ContentAnalyzer.Analyze(Sec(1, "You stop and listen but hear nothing.\n\nTurn to 85."));

        cleaned.References.Should().Equal(85);
        cleaned.Choices.Should().ContainSingle();
        cleaned.Choices[0].Target.Should().Be(85);
        cleaned.Choices[0].Label.Should().BeNull();
    }

    [Fact]
    public void ParenthesisedPair_BecomesTwoChoices()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(1, "Will you investigate (turn to 272) or threaten him (turn to 127)?"));

        cleaned.References.Should().BeEquivalentTo(new[] { 127, 272 });
        cleaned.Choices.Select(c => c.Target).Should().BeEquivalentTo(new[] { 272, 127 });
    }

    [Fact]
    public void GoToReference_BecomesChoice()
    {
        var cleaned = ContentAnalyzer.Analyze(Sec(1, "You head back.\nGo to 14."));

        cleaned.References.Should().Equal(14);
        cleaned.Choices.Should().ContainSingle();
        cleaned.Choices[0].Target.Should().Be(14);
    }

    [Fact]
    public void GarbledTurnTo_RepairedBeforeChoiceExtraction()
    {
        var cleaned = ContentAnalyzer.Analyze(Sec(1, "If you win, Tum o 150."));

        cleaned.References.Should().Equal(150);
        cleaned.Choices.Should().ContainSingle();
        cleaned.Choices[0].Target.Should().Be(150);
        cleaned.Clean.Should().NotContain("Tum o");
    }

    [Fact]
    public void ContinuationTail_StillNotAChoice()
    {
        // Single-ref section whose choice line is really the tail of a narrative
        // sentence: lowercase label, blank line before, previous line unpunctuated.
        var cleaned = ContentAnalyzer.Analyze(
            Sec(1, "You hear something move in the dark\nand hold your breath\n\nand wait quietly Turn to 9."));

        cleaned.References.Should().Equal(9);
        cleaned.Choices.Should().BeEmpty();
        cleaned.Clean.Should().Contain("and wait quietly");
    }

    [Fact]
    public void DuplicateTargets_Deduplicated()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(1, "Turn to 5.\nYou wait. Turn to 5."));

        cleaned.Choices.Select(c => c.Target).Should().Equal(5);
    }
}
