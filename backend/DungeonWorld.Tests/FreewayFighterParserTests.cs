using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF13 Freeway Fighter per-section fixes: arek-sol12 adjudicated
/// exit targets and restored noise-dropped tails. Complements the refs golden
/// (which gates the full graph but skips when CleanedData is absent); these
/// run on pure strings with no book data.
/// </summary>
public sealed class FreewayFighterParserTests
{
    [Theory]
    [InlineData(34, "talk to her, turn to soz.", "turn to 302")]
    [InlineData(40, "greater, tum to #1.", "turn to 81")]
    [InlineData(49, "combat, turn to ga.", "turn to 91")]
    [InlineData(94, "oil left, urn to 284.", "turn to 284")]
    [InlineData(131, "east (turn bo za).", "(turn to 22)")]
    [InlineData(158, "combat, turn to 65.", "turn to 67.")]
    [InlineData(164, "hijacker, urn to 132.", "turn to 132")]
    [InlineData(175, "metres (tuen to 340).", "(turn to 340).")]
    [InlineData(180, "again (hern to 243).", "(turn to 243)")]
    [InlineData(218, "skirt, urn to 52.", "turn to 52")]
    [InlineData(225, "recently, turn to 297.", "turn to 197")]
    [InlineData(235, "crossfire (turn to 90).", "(turn to 40)")]
    [InlineData(252, "left, turn to 285.", "turn to 185")]
    [InlineData(372, "after them, turn to 99.", "turn to 95.")]
    [InlineData(376, "gang (turn to ga).", "(turn to 90)")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        var fixed_ = FreewayFighterParser.ApplySectionFixes(section, content);
        Assert.Contains(expected, fixed_);
    }

    [Theory]
    [InlineData(175, "fifty metres (tuen to", "fifty metres (turn to 340).")]
    [InlineData(156, "head south {turn to", "head south {turn to 207).")]
    public void TailRestore_AppendsDroppedExit(int section, string tail, string expectedTail)
    {
        var fixed_ = FreewayFighterParser.ApplySectionFixes(section, "Body text " + tail);
        Assert.EndsWith(expectedTail, fixed_);
    }

    [Theory]
    [InlineData(1, "(turn to 126).")]
    [InlineData(3, "(turn to 354).")]
    [InlineData(21, "(turn to 221).")]
    [InlineData(67, "(turn to 200).")]
    [InlineData(88, "(turn to 271).")]
    [InlineData(189, "(turn to 24).")]
    [InlineData(254, "(turn to 101).")]
    [InlineData(341, "(turn to 267).")]
    public void GhostRestore_EmptyContentGetsExit(int section, string expected)
    {
        var fixed_ = FreewayFighterParser.ApplySectionFixes(section, "   ");
        Assert.Equal(expected, fixed_);
    }

    [Theory]
    [InlineData(13, "turn to 361")]
    [InlineData(14, "turn to 217")]
    [InlineData(22, "turn to 203")]
    [InlineData(63, "turn to 334")]
    [InlineData(188, "turn to 66")]
    [InlineData(34, "turn to 167")]
    [InlineData(73, "turn to 198")]
    [InlineData(114, "turn to 92")]
    [InlineData(153, "turn to 225")]
    [InlineData(192, "turn to 104")]
    [InlineData(216, "turn to 243")]
    [InlineData(241, "turn to 218")]
    [InlineData(300, "turn to 223")]
    [InlineData(363, "turn to 207")]
    [InlineData(370, "turn to 229")]
    [InlineData(372, "turn to 304")]
    public void MissingExit_AppendsSol12Edge(int section, string expected)
    {
        var fixed_ = FreewayFighterParser.ApplySectionFixes(section, "Body text without exits.");
        Assert.Contains(expected, fixed_);
    }
}
