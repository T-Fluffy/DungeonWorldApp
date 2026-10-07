using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF14 Temple of Terror per-section fixes: arek-sol13 adjudicated
/// exit targets and restored noise-dropped tails. Complements the refs golden
/// (which gates the full graph but skips when CleanedData is absent); these
/// run on pure strings with no book data.
/// </summary>
public sealed class TempleOfTerrorParserTests
{
    [Theory]
    [InlineData(79, "mutant turn to 5 into the room", "lumbers into")]
    [InlineData(79, "backpack, turn to 3049.", "turn to 309")]
    [InlineData(119, "servant turn to 5 down the steps", "lumbers down")]
    [InlineData(119, "win, bum to 73.", "turn to 73")]
    [InlineData(198, "rod? Turn to 2g90", "Turn to 290")]
    [InlineData(296, "work, turn to 182.", "turn to 181")]
    [InlineData(302, "opposite. Turn to ¢3.", "Turn to 93.")]
    [InlineData(93, "Leesha, hum to 11.", "turn to 11")]
    [InlineData(393, "ceiling, turn to Sp.", "turn to 60.")]
    [InlineData(23, "smoke, turn to 526.", "turn to 52")]
    [InlineData(358, "grapes, turn to 122.", "turn to 112")]
    [InlineData(12, "Sheet. Return to 34,", "turn to 34,")]
    [InlineData(391, "Sheet, return to 34.", "turn to 34.")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        var fixed_ = TempleOfTerrorParser.ApplySectionFixes(section, content);
        Assert.Contains(expected, fixed_);
    }

    [Theory]
    [InlineData(213, "haggle, turn", "haggle, turn to 146).")]
    [InlineData(166, "ship (turn", "ship (turn to 238).")]
    [InlineData(21, "ahead. Tum", "ahead. Tum to 46).")]
    [InlineData(361, "protect you. Tum", "protect you. Tum to 340).")]
    [InlineData(219, "Leesha, turn", "Leesha, turn to 137).")]
    [InlineData(171, "disappeared. Tum", "disappeared. Tum to 314).")]
    public void TailRestore_AppendsDroppedExit(int section, string tail, string expectedTail)
    {
        var fixed_ = TempleOfTerrorParser.ApplySectionFixes(section, "Body text " + tail);
        Assert.EndsWith(expectedTail, fixed_);
    }

    [Theory]
    [InlineData(87, "(turn to 362).")]
    public void GhostRestore_EmptyContentGetsExit(int section, string expected)
    {
        var fixed_ = TempleOfTerrorParser.ApplySectionFixes(section, "   ");
        Assert.Equal(expected, fixed_);
    }

    [Theory]
    [InlineData(59, "turn to 280")]
    [InlineData(230, "turn to 278")]
    [InlineData(306, "turn to 339")]
    [InlineData(190, "turn to 40")]
    [InlineData(181, "turn to 376")]
    [InlineData(98, "turn to 300")]
    [InlineData(380, "turn to 400")]
    [InlineData(275, "turn to 164")]
    [InlineData(358, "turn to 237")]
    public void MissingExit_AppendsSol13Edge(int section, string expected)
    {
        var fixed_ = TempleOfTerrorParser.ApplySectionFixes(section, "Body text without exits.");
        Assert.Contains(expected, fixed_);
    }
}
