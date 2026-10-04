using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF08 Scorpion Swamp per-section fixes: arek-adjudicated exit
/// targets and restored noise-dropped tails. Complements the refs golden
/// (which gates the full graph but skips when CleanedData is absent); these
/// run on pure strings with no book data.
/// </summary>
public sealed class ScorpionSwampParserTests
{
    [Theory]
    [InlineData(191, "Friendship? Turn to 204", "Friendship? Turn to 294")]
    [InlineData(137, "Turn back to 336", "Turn to 336")]
    [InlineData(118, "turn to yo", "turn to 70")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. If you wish, {exit}";
        var fixed_ = ScorpionSwampParser.ApplySectionFixes(section, content);
        Assert.Contains(expected, fixed_);
    }

    [Theory]
    [InlineData(1, "his\nproperty!", "turn to 95. TODO: possible second tavern branch.]")]
    [InlineData(195, "soft part, turn", "soft part, turn to 91.")]
    [InlineData(280, "keep on reading. A few", "turn to 78.]")]
    [InlineData(127, "seeking. Turn", "seeking. Turn to 104.")]
    [InlineData(21, "rather not know, turn", "rather not know, turn to 390.")]
    public void TailRestore_AppendsDroppedExit(int section, string tail, string expectedTail)
    {
        var fixed_ = ScorpionSwampParser.ApplySectionFixes(section, "Body text " + tail);
        Assert.EndsWith(expectedTail, fixed_);
    }

}
