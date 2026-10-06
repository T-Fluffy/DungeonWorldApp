using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF11 Talisman of Death per-section fixes: arek-sol10 adjudicated
/// exit targets and restored noise-dropped tails. Complements the refs golden
/// (which gates the full graph but skips when CleanedData is absent); these
/// run on pure strings with no book data.
/// </summary>
public sealed class TalismanOfDeathParserTests
{
    [Theory]
    [InlineData(8, "East, then south Turnto31y", "Turn to 31")]
    [InlineData(144, "If you win turn, to 396.", "turn to 396")]
    [InlineData(143, "ask him for advice, turn to ¢8.", "turn to 98")]
    [InlineData(278, "attack the thieves (turn to 279", "(turn to 286).")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        var fixed_ = TalismanOfDeathParser.ApplySectionFixes(section, content);
        Assert.Contains(expected, fixed_);
    }

    [Theory]
    [InlineData(276, "You resolve to be on your guard. Turn", "Turn to 241.")]
    public void TailRestore_AppendsDroppedExit(int section, string tail, string expectedTail)
    {
        var fixed_ = TalismanOfDeathParser.ApplySectionFixes(section, "Body text " + tail);
        Assert.EndsWith(expectedTail, fixed_);
    }

    [Theory]
    [InlineData(6, "turn to 64")]
    [InlineData(48, "turn to 35")]
    [InlineData(131, "turn to 58")]
    [InlineData(368, "turn to 353")]
    public void MissingExit_AppendsSol10Edge(int section, string expected)
    {
        var fixed_ = TalismanOfDeathParser.ApplySectionFixes(section, "Tyutchev body text without exits.");
        Assert.Contains(expected, fixed_);
    }
}
