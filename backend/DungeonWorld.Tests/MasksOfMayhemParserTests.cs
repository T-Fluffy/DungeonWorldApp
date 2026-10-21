using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF23 Masks of Mayhem per-section fixes: sol22-adjudicated exit targets
/// (appended when absent) and render/sol-proven garble rewrites. Complements the refs
/// golden (which gates the full graph but skips when CleanedData is absent); these run
/// on pure strings with no book data.
/// </summary>
public sealed class MasksOfMayhemParserTests
{
    [Theory]
    [InlineData("tuorm to 288", "turn to 288")]
    [InlineData("tum to 47", "turn to 47")]
    [InlineData("urn io 213", "turn to 213")]
    [InlineData("tom to 85", "turn to 85")]
    [InlineData("turn lo 336", "turn to 336")]
    [InlineData("fum to 27", "turn to 27")]
    [InlineData("born to 203", "turn to 203")]
    public void NormalizeTurnVerbs_RepairsExitVerbs(string garbled, string expected)
    {
        var content = $"Some scene text. If you flee, {garbled}. More text.";
        Assert.Contains(expected, MasksOfMayhemParser.NormalizeTurnVerbs(content));
    }

    [Theory]
    [InlineData("him to 74", "him to 74")]
    [InlineData("up to 6", "up to 6")]
    [InlineData("turn to 18a", "turn to 18a")]
    public void NormalizeTurnVerbs_LeavesNonExitsAlone(string input, string expected)
    {
        var content = $"Some scene text {input} more text.";
        Assert.Contains(expected, MasksOfMayhemParser.NormalizeTurnVerbs(content));
    }

    [Theory]
    [InlineData(133, "ave a rope, turn to 1588.", "turn to 158")]
    [InlineData(316, "sleep straight away {turn to 2268)?", "turn to 226")]
    [InlineData(291, "inue north (turn to 2435)?", "turn to 243")]
    [InlineData(388, "ou take it {turn to 3235)", "turn to 325")]
    [InlineData(322, "with your sword (turn to 209)?", "turn to 207")]

    [InlineData(171, "If vou win, turn to 206.", "turn to 106.")]
    [InlineData(334, "make a break for it {turn to 13a),", "turn to 130")]
    [InlineData(334, "vou are soins firm Lo 2857", "turn to 285")]
    [InlineData(169, "you feel yvoursell foating. Tum in 382.", "Turn to 382.")]
    [InlineData(362, "have a look around (turn to 81}.", "turn to 181}")]
    [InlineData(54, "If you are Lucky, tun tu 124;", "turn to 124")]
    [InlineData(92, "Then vou leave: tum tn 233.", "turn to 231.")]
    [InlineData(386, "continue north through the village {lum to Bq),", "turn to 84")]
    [InlineData(23, "shock her out of the hysteria (turn to 163},", "turn to 164}")]
    [InlineData(326, "A gamet ning? Turn tor 44", "Turn to 44")]
    [InlineData(44, "her what happened to the village (turn to 239)", "turn to 249")]
    [InlineData(202, "with something valuable {turn to 2), or ignore", "turn to 92),")]
    [InlineData(10, "west (turn to 10 7a) along the cliff?", "turn to 70")]
    [InlineData(287, "call out to the distant figure (lun to 143}, or carry on (tum ko 341)?", "turn to 341")]
    [InlineData(40, "If vou win, tum to 300.", "turn to 400.")]
    [InlineData(293, "dive underwater (burn bo 38a)?", "turn to 380")]
    [InlineData(148, "VOUr SKILL score, turn to 208.", "turn to 206.")]
    [InlineData(132, "Hack Tum tn zo.", "Turn to 20.")]
    [InlineData(114, "there is no bridge here, Tum oy 10.", "Turn to 10.")]
    [InlineData(399, "press on stealthily (turn to sa),", "turn to 51")]
    [InlineData(399, "sprint for the open air (hom fo 147)?", "turn to 147")]
    [InlineData(30, "Return to132 and choose again,", "turn to 112")]
    [InlineData(112, "None of these turn to 21", "None of these turn to 121")]
    [InlineData(222, "in the mine, turn to 132; otherwise", "turn to 112; otherwise")]
    [InlineData(222, "otherwise, turn to21,", "turn to 121,")]
    [InlineData(223, "north (turn to 338) or north-east", "turn to 336) or north-east")]
    [InlineData(226, "you are Lucky, turn to 357; if", "turn to 257; if")]
    [InlineData(227, "instead of the usual 4, Turn lo \u00a328.", "Turn to 128.")]
    [InlineData(52, "at least you are safe. Tum to rag.", "Turn to 114.")]
    [InlineData(152, "hospitality will du vou good. Tum toag.", "Turn to 24.")]
    [InlineData(335, "gauntlet. If vom wan, tum ta Fz.", "turn to 72.")]
    [InlineData(390, "run away {turn to a4), or", "turn to 94), or")]
    [InlineData(390, "advantage (turn to 31837", "turn to 318")]
    [InlineData(29, "In any case, urn ioa73.", "turn to 173.")]
    [InlineData(175, "in the area. Turn \u00a30 343.", "Turn to 343.")]
    [InlineData(182, "graze vou. Retum fo 142 and continue", "turn to 142 and continue")]
    [InlineData(190, "in danger of collapsing. Tum bo", "Turn to 272.")]
    [InlineData(270, "of the forest.\u201d Tum 10134.", "Turn to 154.")]
    [InlineData(310, "further down the slope. Turn to 8a.", "Turn to 89.")]
    [InlineData(375, "if vou survive, turn \u00a3013.", "turn to 13.")]
    [InlineData(392, "Lucky, burn toa8z; if you are", "turn to 182; if you are")]
    [InlineData(392, "Unlucky, tun to bo.", "turn to 60.")]
    [InlineData(85, "that you can call on Galrin, turn to veo.", "turn to 100.")]
    [InlineData(87, "If your hands are tied, turn to aga;", "turn to 192;")]
    [InlineData(98, "continue on your way (tum to 18a)?", "(turn to 180)?")]
    [InlineData(104, "or more, turn toas.", "turn to 15.")]
    [InlineData(131, "score is 12 or less, burn to 15371 fFitis", "turn to 153; if it is")]
    [InlineData(172, "Doragar family, tum\nvasttoags,", "turn to 141,")]
    [InlineData(180, "down the trail {burn tira17)?", "turn to 117)?")]
    [InlineData(218, "If your hands are tied, turn \u00a9 314;", "turn to 319;")]
    [InlineData(245, "camp here by the lakeside {tum tor 230),", "turn to 230),")]
    [InlineData(248, "look in the mirror (turn {0 209),", "turn to 209),")]
    [InlineData(261, "crown {tum to 7a),", "turn to 74),")]
    [InlineData(283, "travel east (tum lo", "travel east (turn to 388)?")]
    [InlineData(303, "closer look (hurn to Bg).", "turn to 89)")]
    [InlineData(338, "lair (turn tr 238)?", "turn to 238)?")]
    [InlineData(356, "otherwise, turn tn zsz.", "turn to 252.")]
    [InlineData(383, "otherwise, lum to\n1494.", "turn to 194.")]
    [InlineData(385, "press on (tum\ntor a7)?", "press on (turn to 171)?")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        Assert.Contains(expected, MasksOfMayhemParser.ApplySectionFixes(section, content));
    }

    /// <summary>
    /// Every code edge is restored when absent. Covers the sol22 chain pairs whose
    /// verb garble or truncation defeats the cleaner.
    /// </summary>
    [Theory]
    [InlineData(231, 343)]
    [InlineData(289, 173)]
    [InlineData(289, 54)]
    [InlineData(282, 173)]
    [InlineData(59, 100)]
    [InlineData(208, 88)]
    [InlineData(151, 104)]
    [InlineData(301, 112)]
    [InlineData(26, 149)]
    [InlineData(320, 74)]
    [InlineData(258, 40)]
    [InlineData(374, 126)]
    [InlineData(168, 114)]
    [InlineData(215, 167)]
    [InlineData(387, 22)]
    [InlineData(22, 208)]
    [InlineData(202, 105)]
    [InlineData(370, 151)]
    [InlineData(320, 161)]
    [InlineData(345, 165)]
    [InlineData(200, 178)]
    [InlineData(123, 198)]
    public void CodeEdge_RestoredWhenAbsent(int section, int target)
    {
        var content = "Body text without exits.";
        var fixed_ = MasksOfMayhemParser.ApplySectionFixes(section, content);
        Assert.Contains($"(turn to {target}).", fixed_);
    }

    /// <summary>
    /// A code edge already present in the content is not appended a second time.
    /// Parents with exactly one code edge keep the assertion exact.
    /// </summary>
    [Theory]
    [InlineData(231, 343)]
    public void CodeEdge_NotDuplicatedWhenPresent(int section, int target)
    {
        var withExit = $"Body text (turn to {target}).";
        Assert.Equal(withExit, MasksOfMayhemParser.ApplySectionFixes(section, withExit));
    }

    [Fact]
    public void ApplySectionFixes_LeavesOrdinaryProseAlone()
    {
        const string content = "You set out through the Gate of Skulls. Turn to 53.";
        Assert.Equal(content, MasksOfMayhemParser.ApplySectionFixes(1, content));
    }
}
