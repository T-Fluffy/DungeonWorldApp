using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF25 Beneath Nightmare Castle per-section fixes: sol24/render-adjudicated
/// garble rewrites, code edges restored when absent, and the tightened turn-verb
/// normalizer that must not bite into an ordinary following word. Complements the
/// refs golden (which gates the full graph but skips when CleanedData is absent).
/// </summary>
public sealed class BeneathNightmareCastleParserTests
{
    [Theory]
    [InlineData(3, "turn to 3986.", "turn to 396.")]
    [InlineData(16, "turn to 52K", "turn to 52")]
    [InlineData(51, "Turn to 323", "Turn to 325")]
    [InlineData(51, "Turn to 233", "Turn to 235")]
    [InlineData(54, "turn tor 208.", "turn to 298.")]
    [InlineData(76, "urn tr 323", "turn to 323")]
    [InlineData(80, "turn to 260", "turn to 264")]
    [InlineData(85, "turn to 10 29.", "turn to 29.")]
    [InlineData(112, "turn to 343", "turn to 345")]
    [InlineData(114, "turn to 239", "turn to 259")]
    [InlineData(115, "turn to 50", "turn to 89")]
    [InlineData(131, "tom bo\n2887", "turn to 288")]
    [InlineData(134, "turn to 27", "turn to 271")]
    [InlineData(135, "Tum ko 377", "turn to 377")]
    [InlineData(137, "turn to 199", "turn to 195")]
    [InlineData(137, "tum to oo", "turn to 90")]
    [InlineData(155, "turn to 253", "turn to 293")]
    [InlineData(190, "um =\n359", "turn to 359")]
    [InlineData(190, "Torn toy 247", "turn to 247")]
    [InlineData(190, "Turn toy 128", "turn to 128")]
    [InlineData(206, "in fo 340", "turn to 340")]
    [InlineData(206, "hrm to 8537", "turn to 85")]
    [InlineData(227, "twen Lo 64", "turn to 64")]
    [InlineData(237, "turn to 285", "turn to 289")]
    [InlineData(258, "turn to 100", "turn to 109")]
    [InlineData(263, "turn to 28", "turn to 398")]
    [InlineData(273, "urn tog.", "turn to 190.")]
    [InlineData(288, "Turn to 333", "Turn to 335")]
    [InlineData(317, "=m lo zag", "turn to 204")]
    [InlineData(359, "turn to 208", "turn to 206")]
    [InlineData(372, "turn to 243", "turn to 245")]
    [InlineData(375, "Turn Lo 506", "Turn to 396")]
    [InlineData(389, "turn to zox", "turn to 201")]
    [InlineData(390, "turn to 130", "turn to 149")]
    [InlineData(399, "Turn to 114", "Turn to 119")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        Assert.Contains(expected, BeneathNightmareCastleParser.ApplySectionFixes(section, content));
    }

    /// <summary>Every code edge is restored when absent.</summary>
    [Theory]
    [InlineData(115, 12)]
    [InlineData(147, 7)]
    [InlineData(235, 312)]
    [InlineData(251, 182)]
    public void CodeEdge_RestoredWhenAbsent(int section, int target)
    {
        var content = "Body text without exits.";
        var fixed_ = BeneathNightmareCastleParser.ApplySectionFixes(section, content);
        Assert.Contains($"(turn to {target}).", fixed_);
    }

    /// <summary>A code edge already present is not appended a second time.</summary>
    [Theory]
    [InlineData(115, 12)]
    [InlineData(147, 7)]
    [InlineData(235, 312)]
    [InlineData(251, 182)]
    public void CodeEdge_NotDuplicatedWhenPresent(int section, int target)
    {
        var withExit = $"Body text (turn to {target}).";
        Assert.Equal(withExit, BeneathNightmareCastleParser.ApplySectionFixes(section, withExit));
    }

    /// <summary>
    /// The widened normalizer must not mistake a garbled verb followed by an
    /// ordinary word for a reference. Regression: "retum along" parsed as junk
    /// "alo" + token "ng" -> a false edge to 9; "wn this corridor" -> a false 5.
    /// </summary>
    [Theory]
    [InlineData("Otherwise, there seems to be no other choice but to retum along the side-tunnel.")]
    [InlineData("i you prefer to slip away back to the entrance wn this corridor and await developments.")]
    public void Normalize_LeavesNarrativeAlone(string content)
    {
        Assert.Equal(content, BeneathNightmareCastleParser.NormalizeTurnVerbs(content));
    }

    [Theory]
    [InlineData("tum to 12", "turn to 12")]
    [InlineData("Turn 1o 29", "Turn to 29")]
    [InlineData("furn te 221", "turn to 221")]
    public void Normalize_RepairsGarbledVerb(string content, string expected)
    {
        Assert.Equal(expected, BeneathNightmareCastleParser.NormalizeTurnVerbs(content));
    }

    [Fact]
    public void ApplySectionFixes_LeavesOrdinaryProseAlone()
    {
        const string content = "You stride into the centre of the town. Turn to 53.";
        Assert.Equal(content, BeneathNightmareCastleParser.ApplySectionFixes(1, content));
    }
}
