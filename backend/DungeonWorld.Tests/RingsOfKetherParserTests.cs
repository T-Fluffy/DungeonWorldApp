using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF15 Rings of Kether per-section fixes: arek-sol14 adjudicated
/// exit targets and restored noise-dropped tails. Complements the refs golden
/// (which gates the full graph but skips when CleanedData is absent); these
/// run on pure strings with no book data.
/// </summary>
public sealed class RingsOfKetherParserTests
{
    [Theory]
    [InlineData(51, "follow him (arn to 2)", "turn to 2")]
    [InlineData(11, "heliport {lun to 206)", "turn to 206")]
    [InlineData(11, "navigator (fun to 284)?", "turn to 284)")]
    [InlineData(2, "meet him (fur to 395)", "turn to 395")]
    [InlineData(236, "room {hum to 158)", "turn to 158)")]
    [InlineData(236, "room {hum to 158)", "turn to 158)")]
    [InlineData(41, "address {iurn tn 256)", "turn to 256)")]
    [InlineData(217, "outside (turn to 288)7", "turn to 188)")]
    [InlineData(188, "the left (146)", "the left (turn to 146)")]
    [InlineData(354, "says (turn to Loo)", "turn to 100)")]
    [InlineData(384, "starport (burn tor 348)", "turn to 345)")]
    [InlineData(345, "block (turn to 346", "block (turn to 150)")]
    [InlineData(228, "city (mm to 13g)", "turn to 189)")]
    [InlineData(238, "going (hum io 19g).", "turn to 199).")]
    [InlineData(326, "tomorrow (furmn fo 44}", "turn to 44}")]
    [InlineData(44, "grounds (turn to 135)", "turn to 15)")]
    [InlineData(327, "sniper (burn fo 288)", "turn to 288)")]
    [InlineData(132, "beast (horn Bo 54)", "turn to 54)")]
    [InlineData(54, "beast {turn ti 356)", "turn to 356)")]
    [InlineData(337, "officer (fur to 308)", "turn to 308)")]
    [InlineData(308, "depot {furm to 231}", "turn to 211}")]
    [InlineData(211, "coat {turn to 83)7", "{turn to 84)")]
    [InlineData(320, "left {turn to 332)", "turn to 331)")]
    [InlineData(45, "tower (urn to 320)", "(turn to 320)")]
    [InlineData(377, "walk (turn rl ee a", "turn to 399")]
    [InlineData(125, "island. Tam to 330.", "Turn to 330.")]
    [InlineData(292, "around. Turn in 28.", "to 28.")]
    [InlineData(370, "off. Tom to 312.", "Turn to 312.")]
    [InlineData(332, "tunnel {form to 293)", "turn to 293)")]
    [InlineData(293, "spheres {furm to 137)", "turn to 137)")]
    [InlineData(137, "SKILL, turn to 9.", "turn to 59.")]
    [InlineData(127, "pit. Torn to gg.", "Turn to 49.")]
    [InlineData(313, "room. Turn tor 186.", "Turn to 186.")]
    [InlineData(381, "action (um to 137)", "turn to 147)")]
    [InlineData(147, "him, tum to goo.", "turn to 400.")]
    [InlineData(55, "bribe (turn to 538)?", "turn to 53)?")]
    [InlineData(23, "smoke, turn to 526.", "turn to 52.")]
    [InlineData(343, "woman (turn to 269)", "turn to 265)")]
    [InlineData(229, "room (fun to 171)", "turn to 171)")]
    [InlineData(230, "door {hm fo 298)", "{turn to 298}")]
    [InlineData(135, "road (urn to 359)", "turn to 359)")]
    [InlineData(388, "distance (Tum tr 37)", "Turn to 37)")]
    [InlineData(371, "tunnel (fur 0 203).", "turn to 203).")]
    [InlineData(342, "arm. Feturn Lo 186.", "turn to 186.")]
    [InlineData(340, "way (urn lo 253)7", "turn to 253)7")]
    [InlineData(346, "pistol (lun kr 307)", "turn to 307)")]
    [InlineData(348, "speed (hum to 309)", "turn to 309)")]
    [InlineData(202, "corner (bum to 163)?", "turn to 163)?")]
    [InlineData(356, "beast (turn tor 54)", "turn to 54)")]
    [InlineData(360, "Zera (furn fo 282)", "turn to 282)")]
    [InlineData(361, "exit (fum fo 244)", "turn to 244)")]
    [InlineData(59, "Torus (Turn £0 44)", "Turn to 44)")]
    [InlineData(166, "wins (Turn to 38x)", "Turn to 388)")]
    [InlineData(380, "shock (fom to 224)", "turn to 224)")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        var fixed_ = RingsOfKetherParser.ApplySectionFixes(section, content);
        Assert.Contains(expected, fixed_);
    }

    [Theory]
    [InlineData(213, "haggle, turn", "haggle, turn to 146).")]
    [InlineData(166, "ship (turn", "ship (turn to 238).")]
    [InlineData(21, "ahead. Tum", "ahead. Tum to 46).")]
    [InlineData(361, "protect you. Tum", "protect you. Tum to 340).")]
    [InlineData(219, "Leesha, turn", "Leesha, turn to 137).")]
    [InlineData(171, "disappeared. Tum", "disappeared. Tum to 314).")]
    [InlineData(229, "conditioning (tum to", "conditioning (turn to 190).")]
    [InlineData(290, "speed {(fum fo", "speed {(turn to 271).")]
    [InlineData(87, "instead (turn to", "instead (turn to 19).")]
    [InlineData(283, "corridor [turn to", "corridor [turn to 205).")]
    [InlineData(3, "inquiries (turn to", "inquiries (turn to 345). (turn to 354).")]
    [InlineData(292, "around. Turn", "around. Turn to 28).")]
    [InlineData(313, "room. Turn", "room. Turn to 186).")]
    [InlineData(53, "shop. Turn to", "shop. Turn to 316).")]
    [InlineData(227, "traffic {tum to", "traffic {turn to 384).")]
    [InlineData(300, "smash, tum to", "smash, tum to 359).")]
    public void TailRestore_AppendsDroppedExit(int section, string tail, string expectedTail)
    {
        var fixed_ = RingsOfKetherParser.ApplySectionFixes(section, "Body text " + tail);
        Assert.EndsWith(expectedTail, fixed_);
    }

    [Theory]
    [InlineData(39, "(turn to 341).")]
    [InlineData(181, "(turn to 376).")]
    [InlineData(221, "(turn to 320).")]
    [InlineData(255, "(turn to 297).")]
    [InlineData(80, "(turn to 41).")]
    [InlineData(399, "(turn to 260).")]
    [InlineData(49, "(turn to 352).")]
    [InlineData(186, "(turn to 381).")]
    public void GhostRestore_EmptyContentGetsExit(int section, string expected)
    {
        var fixed_ = RingsOfKetherParser.ApplySectionFixes(section, "   ");
        Assert.Equal(expected, fixed_);
    }

    [Theory]
    [InlineData(73, "turn to 229")]
    [InlineData(341, "turn to 185")]
    [InlineData(328, "turn to 289")]
    [InlineData(350, "turn to 389")]
    [InlineData(126, "turn to 87")]
    [InlineData(380, "turn to 400")]
    [InlineData(260, "turn to 221")]
    [InlineData(205, "turn to 166")]
    [InlineData(275, "turn to 164")]
    [InlineData(358, "turn to 237")]
    public void MissingExit_AppendsSol14Edge(int section, string expected)
    {
        var fixed_ = RingsOfKetherParser.ApplySectionFixes(section, "Body text without exits.");
        Assert.Contains(expected, fixed_);
    }
}
