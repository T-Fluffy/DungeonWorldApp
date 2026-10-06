using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF12 Space Assassin per-section fixes: arek-sol11 adjudicated
/// exit targets and restored noise-dropped tails. Complements the refs golden
/// (which gates the full graph but skips when CleanedData is absent); these
/// run on pure strings with no book data.
/// </summary>
public sealed class SpaceAssassinParserTests
{
    [Theory]
    [InlineData(95, "If you defeat him, tum to goo.", "turn to 400")]
    [InlineData(117, "cylinder examined. Turn to 3a.", "turn to 30")]
    [InlineData(120, "counts down: B...5...4...Tumto1sy.", "turn to 189")]
    [InlineData(121, "north (turn tor 159)", "turn to 159")]
    [InlineData(140, "left (turn to 38237)", "turn to 382")]
    [InlineData(152, "neither of these, turn to gs.", "turn to 69")]
    [InlineData(153, "side (fum to zz27)?", "turn to 227")]
    [InlineData(158, "environs (buen bo 25)", "(turn to 25)")]
    [InlineData(158, "east (turn to 21417)", "turn to 214")]
    [InlineData(164, "revive — the first (buen to 33)", "turn to 31")]
    [InlineData(167, "exposure (burn fo 66)", "turn to 66")]
    [InlineData(201, "door {turn to Jog)?", "turn to 209")]
    [InlineData(303, "standard door (turn ter 249)", "turn to 249")]
    [InlineData(322, "south river (burn fo 160)", "turn to 160")]
    [InlineData(376, "blue (harn to 354)", "turn to 354")]
    [InlineData(397, "south (fun to 121)", "turn to 121")]
    [InlineData(253, "west (bern to 288)", "turn to 288")]
    [InlineData(253, "east (iturn to 28g)", "turn to 289")]
    [InlineData(395, "up to the man (turn to 29a)", "turn to 291")]
    [InlineData(243, "Lucky, turn to2gy.", "turn to 297")]
    [InlineData(284, "other door. Tuer to 356.", "Turn to 356")]
    [InlineData(285, "explosives, urn to 321;", "turn to 321")]
    [InlineData(257, "to eat (bum to\n293)", "turn to\n293")]
    [InlineData(327, "branch. Turn too.", "Turn to 70.")]
    [InlineData(281, "tunnel {turn to 3537", "turn to 353")]
    [InlineData(162, "packet Turn to 1085", "Turn to 108")]
    [InlineData(33, "packet Turn to 1085", "Turn to 108")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        var fixed_ = SpaceAssassinParser.ApplySectionFixes(section, content);
        Assert.Contains(expected, fixed_);
    }

    [Theory]
    [InlineData(164, "revive — the second (turn to", "revive — the second (turn to 274).")]
    [InlineData(151, "other door. Turn to", "other door. Turn to 208).")]
    [InlineData(148, "fuss (furn to", "fuss (furn to 251).")]
    public void TailRestore_AppendsDroppedExit(int section, string tail, string expectedTail)
    {
        var fixed_ = SpaceAssassinParser.ApplySectionFixes(section, "Body text " + tail);
        Assert.EndsWith(expectedTail, fixed_);
    }

    [Theory]
    [InlineData(16, "(turn to 381).")]
    [InlineData(52, "(turn to 14). [Also turn to 332.]")]
    [InlineData(190, "(turn to 52).")]
    [InlineData(29, "(turn to 12).")]
    [InlineData(51, "(turn to 29).")]
    public void GhostRestore_EmptyContentGetsExit(int section, string expected)
    {
        var fixed_ = SpaceAssassinParser.ApplySectionFixes(section, "   ");
        Assert.Equal(expected, fixed_);
    }

    [Theory]
    [InlineData(320, "turn to 310")]
    [InlineData(321, "turn to 88")]
    [InlineData(240, "turn to 394")]
    [InlineData(373, "turn to 295")]
    [InlineData(326, "turn to 15")]
    [InlineData(14, "turn to 381")]
    [InlineData(66, "turn to 365")]
    [InlineData(332, "turn to 255")]
    [InlineData(181, "turn to 200")]
    [InlineData(31, "turn to 107")]
    public void MissingExit_AppendsSol11Edge(int section, string expected)
    {
        var fixed_ = SpaceAssassinParser.ApplySectionFixes(section, "Body text without exits.");
        Assert.Contains(expected, fixed_);
    }
}
