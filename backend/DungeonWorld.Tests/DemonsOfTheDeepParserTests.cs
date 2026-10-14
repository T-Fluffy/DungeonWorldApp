using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF19 Demons of the Deep per-section fixes: sol18-adjudicated exit targets
/// (appended when absent) and render/sol-proven garble rewrites. Complements the refs
/// golden (which gates the full graph but skips when CleanedData is absent); these run
/// on pure strings with no book data.
/// </summary>
public sealed class DemonsOfTheDeepParserTests
{
    [Theory]
    [InlineData(86, "wards the light. turn to 20a.", "turn to 202")]
    [InlineData(86, "wards the light. Tum to 20a.", "Turn to 202")]
    [InlineData(62, "adventure? turn to 9", "adventure? turn to 7")]
    [InlineData(7, "its reed? turn to 5\n[13 hatred", "turn to 178. Its hatred")]
    [InlineData(173, "conlronl the watchers, turn to 231. If", "turn to 211.")]
    [InlineData(57, "Offer gold? Turn to 232", "Turn to 132")]
    [InlineData(57, "Turn 10373 Offer", "Turn to 373 Offer")]
    [InlineData(57, "turn to 9\nDecline", "turn to 76\nDecline")]
    [InlineData(57, "Turn ty 329", "Turn to 329")]
    [InlineData(5, "Pearl? turn to 327\nA Toolfish", "turn to 127")]
    [InlineData(5, "sword? Tum to Ba\nI you", "turn to 81")]
    [InlineData(8, "If\nee turn to 5.", "turn to 118.")]
    [InlineData(180, "towards vou. Tucn lo 236.", "Turn to 256.")]
    [InlineData(92, "yourself? turn to 10 400 Investrate", "turn to 400 Investrate")]
    [InlineData(31, "The Axetish? turn to 219 Fhe", "turn to 214 Fhe")]
    [InlineData(11, "turn to 22\nFlee", "turn to 232\nFlee")]
    [InlineData(202, "Sea-homse? Turn Eo 166", "Turn to 166")]
    [InlineData(304, "entered? Turn to 3\nSwim", "Turn to 161\nSwim")]
    [InlineData(99, "sword? turn to 10.253 Boone", "turn to 253 Boone")]
    [InlineData(99, "Toolfish? Inmto3z Attack", "turn to 31 Attack")]
    [InlineData(342, "different, return to gg.", "return to 99.")]
    [InlineData(59, "vanishes. Tam to 6.", "Turn to 6.")]
    [InlineData(56, "chest, tun\nto 388.", "turn to 388.")]
    [InlineData(82, "pomon, bom\nLo 250.", "turn to 250.")]
    [InlineData(284, "door, Tum\n2347.", "turn to 234.")]
    [InlineData(190, "baths, fu to 266, If", "turn to 266, If")]
    [InlineData(190, "room, tum to 164.", "turn to 164.")]
    [InlineData(191, "pictures, tum to 367.", "turn to 367.")]
    [InlineData(117, "away from it {turf to 78)", "{turn to 78)")]
    [InlineData(243, "this item, fun lo 275.", "turn to 275.")]
    [InlineData(178, "UH nol, rn io 391.", "turn to 391.")]
    [InlineData(138, "Sprite? Tumtn 363", "turn to 363")]
    [InlineData(368, "disobey, Turn ro 162,", "Turn to 162,")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        Assert.Contains(expected, DemonsOfTheDeepParser.ApplySectionFixes(section, content));
    }

    /// <summary>
    /// Every sol18 chain edge is restored when absent. The (10,1) pair from the solution
    /// text is deliberately excluded: it is the "10 Provisions" equipment header, not an edge.
    /// </summary>
    [Theory]
    [InlineData(173, 211)]
    [InlineData(211, 233)]
    [InlineData(233, 270)]
    [InlineData(270, 75)]
    [InlineData(328, 374)]
    [InlineData(374, 16)]
    [InlineData(304, 161)]
    [InlineData(57, 373)]
    [InlineData(316, 150)]
    [InlineData(196, 321)]
    [InlineData(101, 360)]
    [InlineData(360, 184)]
    [InlineData(280, 246)]
    [InlineData(340, 11)]
    [InlineData(11, 232)]
    [InlineData(257, 90)]
    [InlineData(271, 12)]
    [InlineData(258, 384)]
    [InlineData(384, 345)]
    [InlineData(372, 327)]
    [InlineData(327, 115)]
    [InlineData(310, 42)]
    [InlineData(197, 221)]
    [InlineData(221, 307)]
    [InlineData(307, 254)]
    [InlineData(162, 49)]
    [InlineData(245, 53)]
    [InlineData(53, 64)]
    [InlineData(64, 103)]
    [InlineData(35, 265)]
    [InlineData(265, 296)]
    [InlineData(296, 347)]
    [InlineData(348, 97)]
    [InlineData(192, 40)]
    [InlineData(40, 222)]
    [InlineData(222, 2)]
    [InlineData(174, 38)]
    [InlineData(293, 234)]
    [InlineData(234, 135)]
    [InlineData(225, 376)]
    [InlineData(376, 175)]
    [InlineData(175, 241)]
    [InlineData(241, 285)]
    [InlineData(285, 154)]
    [InlineData(261, 96)]
    [InlineData(96, 172)]
    [InlineData(297, 325)]
    [InlineData(212, 195)]
    [InlineData(263, 381)]
    [InlineData(381, 6)]
    [InlineData(180, 256)]
    [InlineData(226, 256)]
    [InlineData(163, 248)]
    [InlineData(148, 219)]
    [InlineData(92, 400)]
    [InlineData(389, 62)]
    [InlineData(62, 7)]
    [InlineData(334, 28)]
    [InlineData(215, 28)]
    [InlineData(202, 166)]
    [InlineData(108, 9)]
    [InlineData(9, 269)]
    [InlineData(269, 339)]
    [InlineData(339, 191)]
    [InlineData(191, 367)]
    [InlineData(367, 244)]
    [InlineData(244, 335)]
    [InlineData(335, 267)]
    [InlineData(267, 22)]
    [InlineData(22, 176)]
    [InlineData(176, 111)]
    [InlineData(111, 34)]
    [InlineData(34, 205)]
    [InlineData(205, 87)]
    [InlineData(87, 99)]
    [InlineData(99, 31)]
    [InlineData(31, 214)]
    [InlineData(214, 31)]
    [InlineData(31, 342)]
    [InlineData(342, 99)]
    [InlineData(99, 253)]
    [InlineData(253, 125)]
    [InlineData(358, 100)]
    [InlineData(100, 236)]
    [InlineData(28, 86)]
    [InlineData(28, 215)]
    [InlineData(86, 202)]
    [InlineData(59, 6)]
    [InlineData(219, 92)]
    [InlineData(130, 188)]
    [InlineData(292, 47)]
    [InlineData(289, 232)]
    [InlineData(306, 47)]
    [InlineData(117, 78)]
    [InlineData(243, 275)]
    [InlineData(178, 391)]
    [InlineData(138, 363)]
    [InlineData(368, 162)]
    public void CodePuzzleEdge_AppendsSol18Edge(int section, int target)
    {
        var fixed_ = DemonsOfTheDeepParser.ApplySectionFixes(section, "Body text without exits.");
        Assert.Contains($"turn to {target}", fixed_);
    }

    [Fact]
    public void CodePuzzleEdge_DoesNotDuplicateAnExistingExit()
    {
        const string withExit = "you leave the cavern (turn to 202)";
        Assert.Equal(withExit, DemonsOfTheDeepParser.ApplySectionFixes(86, withExit));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(500)]
    public void UnrelatedSectionsAreLeftAlone(int section)
    {
        const string content = "A plain scene with no puzzle and no exits.";
        Assert.Equal(content, DemonsOfTheDeepParser.ApplySectionFixes(section, content));
    }

    /// <summary>
    /// FF19 keeps exit-fragment retention and the isolated exit repair but NOT the hardened
    /// second OCR pass. These pin the book-specific bound so an FF17 rule (440) cannot leak in.
    /// </summary>
    [Theory]
    [InlineData("the barrier lifts. Turn now to 341.", "Turn to 341.")]
    [InlineData("you squeeze past. urn to 132", "turn to 132")]
    public void RepairExits_FixesRecoverableGarble(string input, string expected) =>
        Assert.Contains(expected, DemonsOfTheDeepParser.RepairExits(input));

    [Fact]
    public void RepairExits_IsBoundedTo400Sections()
    {
        Assert.DoesNotContain("[unclear]", DemonsOfTheDeepParser.RepairExits("You turn to 380."));
        Assert.Contains("[unclear]", DemonsOfTheDeepParser.RepairExits("You turn to 428."));
    }

    [Fact]
    public void RepairExits_LeavesOrdinaryProseAlone()
    {
        const string prose = "You walk in to the lobby and slow down to 5 mph. Turn to 336.";
        Assert.Equal(prose, DemonsOfTheDeepParser.RepairExits(prose));
    }
}
