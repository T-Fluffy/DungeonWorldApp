using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF24 Creature of Havoc per-section fixes: sol23-adjudicated exit targets
/// (appended when absent) and render/sol-proven garble rewrites. Complements the refs
/// golden (which gates the full graph but skips when CleanedData is absent); these run
/// on pure strings with no book data.
/// </summary>
public sealed class CreatureOfHavocParserTests
{
    [Theory]
    [InlineData(257, "turn to the west (turn to 3049).", "turn to 304")]
    [InlineData(62, "will know. Turn to 208.", "Turn to 298.")]
    [InlineData(208, "take the easterly passage (tum to 1223)?", "turn to 123")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        Assert.Contains(expected, CreatureOfHavocParser.ApplySectionFixes(section, content));
    }

    /// <summary>
    /// Every code edge is restored when absent. Covers the sol23 chain pairs whose
    /// truncated bodies or translated exits defeat the cleaner.
    /// </summary>
    [Theory]
    [InlineData(160, 292)]
    [InlineData(446, 101)]
    [InlineData(147, 283)]
    [InlineData(337, 283)]
    [InlineData(125, 177)]
    [InlineData(215, 267)]
    [InlineData(287, 235)]
    [InlineData(28, 361)]
    [InlineData(222, 134)]
    [InlineData(20, 40)]
    [InlineData(107, 55)]
    [InlineData(435, 62)]
    [InlineData(458, 110)]
    [InlineData(177, 125)]
    [InlineData(90, 140)]
    [InlineData(92, 141)]
    [InlineData(7, 200)]
    [InlineData(267, 215)]
    [InlineData(269, 219)]
    [InlineData(213, 233)]
    [InlineData(257, 237)]
    [InlineData(307, 255)]
    [InlineData(422, 329)]
    public void CodeEdge_RestoredWhenAbsent(int section, int target)
    {
        var content = "Body text without exits.";
        var fixed_ = CreatureOfHavocParser.ApplySectionFixes(section, content);
        Assert.Contains($"(turn to {target}).", fixed_);
    }

    /// <summary>
    /// A code edge already present in the content is not appended a second time.
    /// Parents with exactly one code edge keep the assertion exact.
    /// </summary>
    [Theory]
    [InlineData(160, 292)]
    [InlineData(446, 101)]
    [InlineData(147, 283)]
    public void CodeEdge_NotDuplicatedWhenPresent(int section, int target)
    {
        var withExit = $"Body text (turn to {target}).";
        Assert.Equal(withExit, CreatureOfHavocParser.ApplySectionFixes(section, withExit));
    }

    [Fact]
    public void ApplySectionFixes_LeavesOrdinaryProseAlone()
    {
        const string content = "You heave your great bulky body to its feet. Turn to 53.";
        Assert.Equal(content, CreatureOfHavocParser.ApplySectionFixes(1, content));
    }
}
