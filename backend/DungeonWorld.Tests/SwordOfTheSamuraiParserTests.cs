using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF20 Sword of the Samurai per-section fixes: sol19-adjudicated exit targets
/// (appended when absent) and render/sol-proven garble rewrites. Complements the refs
/// golden (which gates the full graph but skips when CleanedData is absent); these run
/// on pure strings with no book data.
/// </summary>
public sealed class SwordOfTheSamuraiParserTests
{
    [Theory]
    [InlineData(15, "If you win, turn to 10 153.", "turn to 153.")]
    [InlineData(20, "Lucky, turn to 283. If", "Lucky, turn to 282")]
    [InlineData(20, "Unlucky, turn to 206.", "Unlucky, turn to 296.")]
    [InlineData(40, "away (turn to 3807", "(turn to 380")]
    [InlineData(48, "Waters of Knowledge? Turn to gh", "Turn to 96")]
    [InlineData(54, "Death? Turn to 208", "Turn to 206")]
    [InlineData(54, "Shadows? Tum tn 138", "Turn to 188")]
    [InlineData(74, "Death? Turn lo 206", "Turn to 206")]
    [InlineData(74, "Shadows? Turn io 188", "Turn to 188")]
    [InlineData(74, "slay Ikiru. Turn lo 150", "Turn to 150")]
    [InlineData(75, "fountain is open. Turn kegs.", "Turn to 95.")]
    [InlineData(27, "Unlucky, turn to 3 30g.", "turn to 309.")]
    [InlineData(8, "Height? Tum io zo", "Turn to 30")]
    [InlineData(8, "Endless Sands of Akhon? turn to 5", "turn to 78")]
    [InlineData(121, "or less, turn to22.", "or less, turn to 222.")]
    [InlineData(172, "The Tatsu? Turn to 204", "Turn to 208")]
    [InlineData(195, "His hatred of Samurai is well known. Turn to 204,", "Turn to 209,")]
    [InlineData(203, "If you win, turn to 1687.", "turn to 168.")]
    [InlineData(208, "The Golden Company? Turn to 183", "Turn to 184")]
    [InlineData(212, "shadowy forms {turn to 208)7", "{turn to 298)7")]
    [InlineData(213, "ford cell (turn to 3137", "(turn to 31")]
    [InlineData(228, "Enchantrecs? Tum to 3130", "Turn to 330")]
    [InlineData(255, "tired bul victorious, Turn to 303.", "Turn to 393.")]
    [InlineData(275, "If vou win, turn to 390.", "turn to 399.")]
    [InlineData(295, "valley. Turn to 7.", "Turn to 27.")]
    [InlineData(298, "its touch. Turn to 22.", "Turn to 121.")]
    [InlineData(294, "goat-man turn to 5 forward. TF", "lumbers forward")]
    [InlineData(303, "head once more, turn to 287.", "turn to 251.")]
    [InlineData(357, "the way. turn to 295.", "turn to 395.")]
    [InlineData(397, "If vou win, turn to 31.", "turn to 391.")]
    [InlineData(28, "wo bo sleep. Turn to 31h.", "Turn to 316.")]
    [InlineData(66, "less than 5.\nurn fo zzz.", "turn to 332.")]
    [InlineData(114, "follow her, tum to To.", "turn to 70.")]
    [InlineData(191, "you eat and sleep,\nfurn to z2o3.", "turn to 203.")]
    [InlineData(199, "STAMINA, torn to 47;", "turn to 47")]
    [InlineData(199, "If you win, turn ti goo.", "turn to 400.")]
    [InlineData(293, "way [fom to 1935).", "(turn to 195)")]
    [InlineData(330, "the Phoenix (turn to 32a)", "turn to 320")]
    [InlineData(330, "the Ki-Rin (lun to 278)", "turn to 278")]
    [InlineData(350, "your way in (turn to $74)", "turn to 374")]
    [InlineData(350, "scale them (Lien Lo 388)", "turn to 388")]
    [InlineData(359, "leave (um to 327)", "turn to 327")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        Assert.Contains(expected, SwordOfTheSamuraiParser.ApplySectionFixes(section, content));
    }

    /// <summary>
    /// Every code edge is restored when absent. Mirrors the sol19 chain pairs plus the
    /// print-verified option targets the cleaner cannot read.
    /// </summary>
    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 29)]
    [InlineData(5, 71)]
    [InlineData(13, 31)]
    [InlineData(17, 107)]
    [InlineData(21, 49)]
    [InlineData(22, 292)]
    [InlineData(29, 43)]
    [InlineData(35, 129)]
    [InlineData(41, 35)]
    [InlineData(43, 59)]
    [InlineData(59, 111)]
    [InlineData(66, 340)]
    [InlineData(75, 95)]
    [InlineData(76, 152)]
    [InlineData(78, 348)]
    [InlineData(95, 243)]
    [InlineData(96, 8)]
    [InlineData(98, 256)]
    [InlineData(112, 2)]
    [InlineData(119, 37)]
    [InlineData(114, 70)]
    [InlineData(114, 8)]
    [InlineData(220, 76)]
    [InlineData(126, 48)]
    [InlineData(131, 199)]
    [InlineData(152, 174)]
    [InlineData(163, 195)]
    [InlineData(172, 278)]
    [InlineData(174, 212)]
    [InlineData(175, 15)]
    [InlineData(189, 220)]
    [InlineData(102, 292)]
    [InlineData(206, 270)]
    [InlineData(219, 5)]
    [InlineData(228, 266)]
    [InlineData(233, 357)]
    [InlineData(235, 400)]
    [InlineData(27, 400)]
    [InlineData(290, 8)]
    [InlineData(299, 137)]
    [InlineData(241, 275)]
    [InlineData(255, 218)]
    [InlineData(270, 76)]
    [InlineData(273, 329)]
    [InlineData(320, 22)]
    [InlineData(340, 8)]
    [InlineData(348, 8)]
    [InlineData(352, 8)]
    [InlineData(369, 299)]
    [InlineData(101, 163)]
    [InlineData(399, 383)]
    [InlineData(379, 341)]
    [InlineData(381, 227)]
    [InlineData(391, 381)]
    [InlineData(54, 150)]
    public void CodeEdge_RestoredWhenAbsent(int section, int target)
    {
        var content = "Body text without exits.";
        var fixed_ = SwordOfTheSamuraiParser.ApplySectionFixes(section, content);
        Assert.Contains($"(turn to {target}).", fixed_);
    }

    [Theory]
    [InlineData(13, 31)]
    [InlineData(270, 76)]
    [InlineData(320, 22)]
    public void CodeEdge_NotDuplicatedWhenPresent(int section, int target)
    {
        var withExit = $"Body text (turn to {target}).";
        Assert.Equal(withExit, SwordOfTheSamuraiParser.ApplySectionFixes(section, withExit));
    }
}
