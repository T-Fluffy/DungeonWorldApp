using DungeonWorld.Core.Entities;
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
    [InlineData(26, "TYRANNOSAURUS SKILL 11 STAMINA 18", "If you win turn to 14.")]
    [InlineData(28, "Silver Trinket", "Turn to 15.")]
    [InlineData(204, "slab of rock is slithering down", "Turn to 120.")]
    public void MissingBody_IsRestoredFromScan(int section, string body, string ending)
    {
        var content = TalismanOfDeathParser.ApplySectionFixes(section, string.Empty);

        Assert.Contains(body, content);
        Assert.EndsWith(ending, content);
    }

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

    [Theory]
    [InlineData(38, new[] { 43 })]
    [InlineData(77, new[] { 43 })]
    public void MissingSectionReferences_AreRestoredWithoutAddingBodyText(int sectionNumber, int[] expected)
    {
        var section = new Section { SectionNumber = sectionNumber };
        var book = new Book { Sections = new List<Section> { section } };

        TalismanOfDeathParser.ApplyReferenceFixes(book);

        Assert.Equal(expected, section.ReferenceOverride);
        Assert.True(string.IsNullOrEmpty(section.Content));
    }
}
