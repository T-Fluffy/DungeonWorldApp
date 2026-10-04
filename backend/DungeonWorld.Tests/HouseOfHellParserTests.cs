using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF10 House of Hell per-section fixes: arek-sol9 adjudicated exit
/// targets and restored noise-dropped tails. Complements the refs golden
/// (which gates the full graph but skips when CleanedData is absent); these
/// run on pure strings with no book data.
/// </summary>
public sealed class HouseOfHellParserTests
{
    [Theory]
    [InlineData(64, "disappeared. Turn\nnow to 375.", "Turn\nto 375.")]
    [InlineData(93, "passageway or 166 to\nlook around.", "passageway or turn to 166 to")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        var fixed_ = HouseOfHellParser.ApplySectionFixes(section, content);
        Assert.Contains(expected, fixed_);
    }

    [Theory]
    [InlineData(352, "lose a point of STAMINA. Then turn", "Then turn to 57.")]
    [InlineData(315, "high enough. Now turn", "Now turn to 235.")]
    [InlineData(194, "other choice. Now turn", "Now turn to 130.")]
    [InlineData(130, "other choice. Now turn", "Now turn to 297.")]
    [InlineData(141, "other choices. Now turn", "Now turn to 280.")]
    [InlineData(300, "defeat the Master (turn to", "defeat the Master (turn to 105).")]
    [InlineData(145, "towards the window (turn", "towards the window (turn to 64).")]
    [InlineData(355, "able to help you (turn to", "able to help you (turn to 263).")]
    [InlineData(83, "return to the landing (turn", "return to the landing (turn to 233).")]
    [InlineData(353, "decanter (turn", "decanter (turn to 292).")]
    [InlineData(295, "room by turning to", "room by turning to 159.")]
    [InlineData(323, "hallway. Turn", "hallway. Turn to 118. (turn to 296).")]
    [InlineData(144, "then try elsewhere (turn", "then try elsewhere (turn to 278).")]
    public void TailRestore_AppendsDroppedExit(int section, string tail, string expectedTail)
    {
        var fixed_ = HouseOfHellParser.ApplySectionFixes(section, "Body text " + tail);
        Assert.EndsWith(expectedTail, fixed_);
    }

    [Theory]
    [InlineData(320, "turn to 310")]
    [InlineData(321, "turn to 88")]
    [InlineData(323, "turn to 296")]
    public void MissingExit_AppendsSol9Edge(int section, string expected)
    {
        var fixed_ = HouseOfHellParser.ApplySectionFixes(section, "Body text without exits.");
        Assert.Contains(expected, fixed_);
    }
}
