using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins the FF18 Rebel Planet per-section fixes: the arek-sol17 adjudicated exit targets, the
/// garbled "turn to goo." reading, and the code-puzzle edges restored because their parents say
/// "turn to the paragraph which has the same number as ..." rather than naming a target.
/// Complements the refs golden (which gates the full graph but skips when CleanedData is absent);
/// these run on pure strings with no book data.
/// </summary>
public sealed class RebelPlanetParserTests
{
    [Theory]
    [InlineData(222, "Turn to goo.", "Turn to 400.")]
    [InlineData(60, "you turn south. Turn to 99-", "you turn south. Turn to 99.")]
    public void ExitFix_RewritesTarget(int section, string exit, string expected)
    {
        var content = $"Some scene text. {exit} More text.";
        Assert.Contains(expected, RebelPlanetParser.ApplySectionFixes(section, content));
    }

    [Fact]
    public void S49_FixesCentSignFor98()
    {
        const string content = "If bitten by a Saw-toothed Rat, turn to ¢8; otherwise turn to 232.";
        var fixed_ = RebelPlanetParser.ApplySectionFixes(49, content);
        Assert.Contains("turn to 98", fixed_);
        Assert.DoesNotContain("¢8", fixed_);
    }

    [Fact]
    public void S48_DropsStrayPipeBeforeExit()
    {
        const string content = "The rest of the journey is uneventful.\r\n| Turn to 164.";
        var fixed_ = RebelPlanetParser.ApplySectionFixes(48, content);
        Assert.Contains("Turn to 164.", fixed_);
        Assert.DoesNotContain("| Turn to 164.", fixed_);
    }

    [Fact]
    public void S106_RelevantReferenceBecomesScabrokWeakness()
    {
        const string content = "If you know the Scabrok's weakness, turn to the relevant reference, Otherwise, just fight it. If you win, turn to 54.";
        var fixed_ = RebelPlanetParser.ApplySectionFixes(106, content);
        Assert.Contains("turn to 262", fixed_);
        Assert.DoesNotContain("relevant reference", fixed_, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void S297_OverrideJoinsThePageTurn()
    {
        var content = RebelPlanetParser.ApplySectionOverrides(297, "truncated");
        Assert.Contains("turn to 381", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("turn to 169", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Ree ore ar aes", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void S355_OverrideRestoresTheFissureTail()
    {
        var content = RebelPlanetParser.ApplySectionOverrides(355, "Apart from some useless chunks of metal");
        Assert.Contains("broken spy missile", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("turn to 14", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("turn to 10", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpeningBraceFromScan_BecomesParenthesis()
    {
        var fixed_ = RebelPlanetParser.ApplySectionFixes(29, "Will you settle yourself far away {turn to 60)?");
        Assert.Contains("(turn to 60)?", fixed_);
        Assert.DoesNotContain("{turn to 60)", fixed_);
    }

    /// <summary>
    /// Sections entered through a binary/decimal code puzzle have no literal target in the text,
    /// so no edge can be parsed. sol17 (or, for S239, the book's own internal chain) proves each
    /// of these parents really does reach that section.
    /// </summary>
    [Theory]
    [InlineData(382, 5)]   // southern club code -> way to the Fission Chips
    [InlineData(169, 110)] // arsenal code -> inside the arsenal
    [InlineData(255, 155)] // palindrome digits -> Dorado lets you go
    [InlineData(349, 231)] // guard leads you away -> arena door
    [InlineData(219, 297)] // computer code -> go down to the basement
    [InlineData(245, 294)] // left-hand cave -> enter the gloom
    [InlineData(222, 400)] // elmonite blast -> the Empire is finished
    [InlineData(101, 294)] // hotel search -> cave mouth
    [InlineData(258, 239)] // Arts Floor room-number puzzle -> Zacharias's door
    public void CodePuzzleEdge_AppendsSol17Edge(int section, int target)
    {
        var fixed_ = RebelPlanetParser.ApplySectionFixes(section, "Body text without exits.");
        Assert.Contains($"turn to {target}", fixed_);
    }

    /// <summary>
    /// The restoration guard must be digit-aware: this scan reads stray extra digits, and a plain
    /// substring check would think the exit already exists and skip the restoration.
    /// </summary>
    [Fact]
    public void CodePuzzleEdge_RestoresDespiteStrayDigit()
    {
        var fixed_ = RebelPlanetParser.ApplySectionFixes(382, "the code reads (turn to 55)");
        Assert.Contains("turn to 5", fixed_);
    }

    [Fact]
    public void CodePuzzleEdge_DoesNotDuplicateAnExistingExit()
    {
        const string withExit = "you take the east passage (turn to 5)";
        Assert.Equal(withExit, RebelPlanetParser.ApplySectionFixes(382, withExit));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(500)]
    public void UnrelatedSectionsAreLeftAlone(int section)
    {
        const string content = "A plain scene with no puzzle and no exits.";
        Assert.Equal(content, RebelPlanetParser.ApplySectionFixes(section, content));
    }

    /// <summary>
    /// FF18 keeps exit-fragment retention and the isolated exit repair but NOT the hardened second
    /// OCR pass. These pin the book-specific bound so an FF17 rule (440) cannot leak in.
    /// </summary>
    [Theory]
    [InlineData("the barrier lifts. Turn now to 341.", "Turn to 341.")]
    [InlineData("you squeeze past. urn to 132", "turn to 132")]
    public void RepairExits_FixesRecoverableGarble(string input, string expected) =>
        Assert.Contains(expected, RebelPlanetParser.RepairExits(input));

    [Fact]
    public void RepairExits_IsBoundedTo400Sections()
    {
        Assert.DoesNotContain("[unclear]", RebelPlanetParser.RepairExits("You turn to 380."));
        Assert.Contains("[unclear]", RebelPlanetParser.RepairExits("You turn to 428."));
    }

    [Fact]
    public void RepairExits_LeavesOrdinaryProseAlone()
    {
        const string prose = "You walk in to the lobby and slow down to 5 mph. Turn to 336.";
        Assert.Equal(prose, RebelPlanetParser.RepairExits(prose));
    }
}
