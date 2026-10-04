using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF09 Caverns of the Snow Witch per-section fixes: arek-sol8
/// adjudicated exit targets, restored noise-dropped tails, and ghost restores
/// for slices the reconstruction left empty. Complements the refs golden
/// (which gates the full graph but skips when CleanedData is absent); these
/// run on pure strings with no book data.
/// </summary>
public sealed class CavernsOfTheSnowWitchParserTests
{
    [Theory]
    [InlineData(391, "If not, turn to 244.", "turn to 249")]
    [InlineData(249, "If you roll a 2 or greater, turn to 210.", "turn to 219")]
    [InlineData(54, "turn to gx.", "turn to 91")]
    [InlineData(60, "SKILL, furm Eo 116.", "turn to 116")]
    [InlineData(63, "the water (turn to gé),", "turn to 96")]
    [InlineData(77, "spear, tum io 378.", "turn to 378")]
    [InlineData(97, "Blow the fAute Turn to 79", "turn to 74")]
    [InlineData(111, "cavern, turn to joo.", "turn to 300")]
    [InlineData(131, "row across the river, turn 0 26.", "turn to 26")]
    [InlineData(131, "owner to arrive, turn to 280.", "turn to 289")]
    [InlineData(158, "Luck point. Turn to 273,", "turn to 173")]
    [InlineData(159, "next tunnel (turn to 3381.", "turn to 338")]
    [InlineData(189, "If you win, turn to 304.", "turn to 309")]
    [InlineData(216, "greater than your SKILL, turn to 3735.", "turn to 373")]
    [InlineData(19, "silver, tum 0 206.", "turn to 206")]
    [InlineData(364, "river valley (urn to 115).", "(turn to 115)")]
    [InlineData(371, "Lucky, turn ter 25%.", "turn to 259")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        var fixed_ = CavernsOfTheSnowWitchParser.ApplySectionFixes(section, content);
        Assert.Contains(expected, fixed_);
    }

    [Theory]
    [InlineData(130, "to the next tunnel (tum to", "to the next tunnel (tum to 338).")]
    [InlineData(253, "past the last junction {turn to", "past the last junction {turn to 135).")]
    [InlineData(257, "climb again (tum to", "climb again (tum to 363).")]
    [InlineData(260, "tunnel to your right (turn to", "tunnel to your right (turn to 370).")]
    [InlineData(295, "the other tunnel (fun", "the other tunnel (fun (turn to 137).")]
    [InlineData(317, "left into the tunnel {turn to", "left into the tunnel {turn to 198).")]
    [InlineData(356, "left into the tunnel (turn to", "left into the tunnel (turn to 198).")]
    [InlineData(4, "decide to investigate (burn", "decide to investigate (turn to 235).")]
    public void TailRestore_AppendsDroppedExit(int section, string tail, string expectedTail)
    {
        var fixed_ = CavernsOfTheSnowWitchParser.ApplySectionFixes(section, "Body text " + tail);
        Assert.EndsWith(expectedTail, fixed_);
    }

    [Theory]
    [InlineData(46, "(turn to 312). [Also turn to 304.] [Also turn to 119.]")]
    [InlineData(71, "(turn to 390). [Also turn to 149].")]
    [InlineData(110, "(turn to 399). [Also turn to 257.]")]
    [InlineData(42, "(turn to 201). [Also turn to 280.]")]
    public void GhostRestore_EmptyContentGetsExit(int section, string expected)
    {
        var fixed_ = CavernsOfTheSnowWitchParser.ApplySectionFixes(section, "   ");
        Assert.Equal(expected, fixed_);
    }

    [Theory]
    [InlineData(5, "turn to 68")]
    [InlineData(100, "turn to 273")]
    [InlineData(113, "turn to 15")]
    [InlineData(339, "turn to 216")]
    public void DupSlotAppend_RestoresTrueExit(int section, string expected)
    {
        var fixed_ = CavernsOfTheSnowWitchParser.ApplySectionFixes(section, "Foreign body text without exits.");
        Assert.Contains(expected, fixed_);
    }
}
