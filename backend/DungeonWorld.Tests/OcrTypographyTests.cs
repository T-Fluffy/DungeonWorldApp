// File: DungeonWorld.Tests/OcrTypographyTests.cs
using DungeonWorld.Core.Text;
using FluentAssertions;

namespace DungeonWorld.Tests;

public class OcrTypographyTests
{
    [Theory]
    [InlineData("fou have failed in your mission", "you have failed in your mission")]
    [InlineData("vou grasp the rope", "you grasp the rope")]
    [InlineData("draw rour sword", "draw your sword")]
    [InlineData("yoor spell", "your spell")]
    [InlineData("continue on yout journey", "continue on your journey")]
    [InlineData("you wonld become lost", "you would become lost")]
    [InlineData("they furn and flee", "they turn and flee")]
    [InlineData("As s00n as you touch", "As soon as you touch")]
    [InlineData("1he swashbuckling fish", "the swashbuckling fish")]
    [InlineData("It i5 a Kroll", "It is a Kroll")]
    [InlineData("a tam-iliar junction 1s here", "a tam-iliar junction is here")]
    [InlineData("you may Tes! again", "you may Tes! again")] // only the exact "Tesi" token is repaired
    public void Repair_FixesKnownOcrWords(string input, string expected)
    {
        OcrTypography.Repair(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("1f you are Lucky, turn to 40.", "If you are Lucky, turn to 40.")]
    [InlineData("a door. 1f you open it, turn to 7.", "a door. If you open it, turn to 7.")]
    [InlineData("wait and 1f it stops, go on", "wait and if it stops, go on")]
    public void Repair_CapitalizesSentenceInitialIf(string input, string expected)
    {
        OcrTypography.Repair(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Lose 2 sTAMINA points", "Lose 2 STAMINA points")]
    [InlineData("your skiLL is 10", "your SKILL is 10")]
    [InlineData("Lose 1 LUck point", "Lose 1 LUCK point")]
    [InlineData("sTaMINa falls to 2", "STAMINA falls to 2")]
    public void Repair_NormalizesMangledStatLabels(string input, string expected)
    {
        OcrTypography.Repair(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Your Skill is tested here.")]          // one capital = normal prose
    [InlineData("They speed along the road.")]          // lowercase verb, not the stat
    [InlineData("Turn to 42. If you are Lucky, turn to 103.")]
    [InlineData("SKILL 10 STAMINA 12 LUCK 8")]
    [InlineData("The CodeMatch device is ready.")]
    public void Repair_LeavesCorrectTextAlone(string input)
    {
        OcrTypography.Repair(input).Should().Be(input);
    }

    [Fact]
    public void Repair_IsIdempotent()
    {
        const string text = "1f vou win, turn to 240. Lose 1 sTAMINA point.";
        var once = OcrTypography.Repair(text);
        OcrTypography.Repair(once).Should().Be(once);
    }

    [Fact]
    public void Repair_NeverChangesExitNumbers()
    {
        const string text = "tum to 1a and turn to 45, then 1f vou are Lucky turn to 200.";

        OcrTypography.Repair(text).Should().Contain("turn to 45")
            .And.Contain("turn to 200")
            .And.Contain("If you are Lucky");
    }
}
