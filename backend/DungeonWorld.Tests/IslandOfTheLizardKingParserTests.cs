using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF07 Island of the Lizard King per-section fixes: arek-adjudicated
/// exit targets, restored noise-dropped tails, and the neutralized S36 phantom.
/// Complements the refs golden (which gates the full graph but skips when
/// CleanedData is absent); these run on pure strings with no book data.
/// </summary>
public sealed class IslandOfTheLizardKingParserTests
{
    [Theory]
    [InlineData(4, "turn to 10.", "turn to 101.")]
    [InlineData(7, "urn to 317.", "turn to 317.")]
    [InlineData(21, "turn to 22a.", "turn to 222.")]
    [InlineData(92, "turn to 22a.", "turn to 222.")]
    [InlineData(160, "turn tor 141.", "turn to 141.")]
    [InlineData(243, "Turntoy.", "turn to 7.")]
    [InlineData(317, "turn to agh.", "turn to 296.")]
    [InlineData(327, "Turn to anh.", "Turn to 206.")]
    [InlineData(79, "Tuen to 17.", "turn to 17.")]
    [InlineData(311, "furmn to 222.", "turn to 222.")]
    [InlineData(26, "Tuen to og.", "turn to 94.")]
    [InlineData(39, "turn to 20.", "turn to 207.")]
    [InlineData(46, "turn to 99.", "turn to 69.")]
    [InlineData(63, "turn to 320.", "turn to 329.")]
    [InlineData(68, "Tum to Fo.", "turn to 70.")]
    [InlineData(73, "turn to zip.", "turn to 217.")]
    [InlineData(77, "burn toga.", "turn to 92.")]
    [InlineData(79, "Turn to gy.", "Turn to 97.")]
    [InlineData(85, "turn to 90.", "turn to 60.")]
    [InlineData(86, "turn to 2035.", "turn to 203.")]
    [InlineData(93, "turn to zag.", "turn to 214.")]
    [InlineData(139, "turn to 99.", "turn to 95.")]
    [InlineData(166, "burn bo 318.", "turn to 318.")]
    [InlineData(192, "turn to 570.", "turn to 57.")]
    [InlineData(193, "turn to 130.", "turn to 139.")]
    [InlineData(206, "Turntoy.", "turn to 7.")]
    [InlineData(208, "turn to 194.", "turn to 199.")]
    [InlineData(213, "turn to 63.", "turn to 68.")]
    [InlineData(244, "turn Lo Joo.", "turn to 400.")]
    [InlineData(251, "turn to 20.", "turn to 201.")]
    [InlineData(295, "burn to gh.", "turn to 96.")]
    [InlineData(297, "Turn to 99.", "Turn to 94.")]
    [InlineData(305, "turn to gg.", "turn to gg.")] // deliberately untouched: shared g->9 maps it to 99 (ogre aftermath), unlike the boots gg's
    [InlineData(311, "Turn to gq.", "Turn to 94.")]
    [InlineData(317, "turn to agh.", "turn to 296.")]
    [InlineData(327, "Turn to anh.", "Turn to 206.")]
    [InlineData(329, "turn to seg.", "turn to 309.")]
    [InlineData(334, "turn to 143.", "turn to 145.")]
    [InlineData(341, "turn to 1eg.", "turn to 109.")]
    [InlineData(352, "turn to 309.", "turn to 399.")]
    [InlineData(359, "turn to EYED.", "turn to 373.")]
    [InlineData(362, "turn to 104.", "turn to 194.")]
    [InlineData(368, "turn to 47.", "turn to 147.")]
    [InlineData(384, "turn to goo.", "turn to 400.")]
    [InlineData(391, "turn to 8a.", "turn to 81.")]
    [InlineData(394, "turn to 19a.", "turn to 191.")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. If you wish, {exit}";
        var fixed_ = IslandOfTheLizardKingParser.ApplySectionFixes(section, content);
        Assert.Contains(expected, fixed_);
    }

    [Theory]
    [InlineData(58, "keep heading west, tum", "keep heading west, turn to 37.")]
    [InlineData(152, "trees (turn to", "trees (turn to 391).")]
    [InlineData(200, "trees (burn to", "trees (turn to 391).")]
    [InlineData(238, "the hut {turn", "the hut (turn to 152).")]
    [InlineData(292, "gorge bo continue west (turn\nto 11g).", "gorge bo continue west (turn to 11g).")] // join only; shared map completes 11g->119
    [InlineData(86, "eat them, turn", "eat them, turn to 203.")]
    [InlineData(193, "ravine (turn", "ravine (turn to 139).")]
    [InlineData(223, "Rounds, turn to", "Rounds, turn to 3.")]
    [InlineData(297, "grassy plain", "grassy plain (turn to 223).")]
    [InlineData(334, "west, turn ta", "west, turn to 114,")]
    [InlineData(359, "alive, turn to", "alive, turn to 373.")]
    [InlineData(368, "mines (turn to", "mines (turn to 147).")]
    [InlineData(203, "without a monkey, turn", "without a monkey, turn to 36.")]
    [InlineData(2, "Unlucky, turn", "Unlucky, turn to 326.")]
    public void TailRestore_AppendsDroppedExit(int section, string tail, string expectedTail)
    {
        var fixed_ = IslandOfTheLizardKingParser.ApplySectionFixes(section, "Body text " + tail);
        Assert.EndsWith(expectedTail, fixed_);
    }

    [Fact]
    public void SplitAgreeExit_JoinsTo397()
    {
        var content = "If you agree to the Shaman's terms, turn to 3\n397. If you would rather face";
        var fixed_ = IslandOfTheLizardKingParser.ApplySectionFixes(199, content);
        Assert.Contains("turn to 397.", fixed_);
        Assert.DoesNotContain("turn to 3\n397.", fixed_);
    }

    [Fact]
    public void WrappedTerExit_JoinsAndFixes()
    {
        var content = "If you are Lucky, turn\nter 334. If you are Unlucky, turn to 281,";
        var fixed_ = IslandOfTheLizardKingParser.ApplySectionFixes(83, content);
        Assert.Contains("turn to 334.", fixed_);
    }

    [Fact]
    public void FireSwordPhantom_Neutralized()
    {
        var content = "If vou are, turn to 1, If\nvou must fight the Lizard King with an ordinary";
        var fixed_ = IslandOfTheLizardKingParser.ApplySectionFixes(36, content);
        Assert.DoesNotContain("turn to 1,", fixed_);
        Assert.Contains("TODO 600dpi p20", fixed_);
    }

    [Fact]
    public void CutRightBranch_StrippedWithTodo()
    {
        var content = "If you wish to turn right, turn to 214";
        var fixed_ = IslandOfTheLizardKingParser.ApplySectionFixes(213, content);
        Assert.DoesNotContain("turn to 214", fixed_);
        Assert.Contains("TODO 600dpi p60", fixed_);
    }
}
