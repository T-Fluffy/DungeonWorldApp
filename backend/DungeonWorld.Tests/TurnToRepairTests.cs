// File: DungeonWorld.Tests/TurnToRepairTests.cs
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Text;
using DungeonWorld.Cleaning;
using DungeonWorld.Cleaning.Cleaner;
using DungeonWorld.Infrastructure.Parsing;
using FluentAssertions;

namespace DungeonWorld.Tests;

public class TurnToRepairTests
{
    [Theory]
    [InlineData("Tum o 327", "turn to 327")]
    [InlineData("furn te 221", "turn to 221")]
    [InlineData("fum to 1o", "turn to 10")]
    [InlineData("tumn to 32z", "turn to 322")]
    [InlineData("Turnto261", "turn to 261")]
    [InlineData("turmn to 268", "turn to 268")]
    [InlineData("Turn o261", "turn to 261")]
    [InlineData("turn o 148", "turn to 148")]
    [InlineData("Turn lo 367", "turn to 367")]
    [InlineData("turm fo 215", "turn to 215")]
    [InlineData("fum 1o", "turn to 10")]
    [InlineData("lum & 39", "turn to 39")]
    [InlineData("tum to.237", "turn to 237")]
    [InlineData("Tarn to 164", "turn to 164")]
    [InlineData("fom bo 301", "turn to 301")]
    [InlineData("tum to 8", "turn to 8")]
    [InlineData("Turm b 206", "turn to 206")]
    [InlineData("rn to 55", "turn to 55")]
    [InlineData("turn W307", "turn to 307")]
    public void RepairContent_NormalizesVerbVariantsAndDigits(string garbled, string expected)
    {
        TurnToRepair.RepairContent($"Choose {garbled}.").Should().Contain(expected);
    }

    [Theory]
    [InlineData("Turn to o261", "Turn to 261")]
    [InlineData("Turn to z10", "Turn to 210")]
    [InlineData("Turn to 8s", "Turn to 85")]
    [InlineData("turn to 1o3", "turn to 103")]
    [InlineData("Turn to 8o", "Turn to 80")]
    [InlineData("Turn to Go", "Turn to 90")]
    [InlineData("Turn bo b1", "turn to 81")]
    [InlineData("Turn to267", "Turn to267")]
    [InlineData("Turn to g9", "Turn to 99")]
    [InlineData("Turn to 2go", "Turn to 290")]
    [InlineData("turn to 26g", "turn to 269")]
    [InlineData("turn to zog", "turn to 209")]
    [InlineData("turn to x191", "turn to 191")]
    [InlineData("turn to agz", "turn to 92")]
    [InlineData("Turn to g4", "Turn to 94")]
    public void RepairContent_RepairsUnambiguousDigitConfusions(string garbled, string expected)
    {
        TurnToRepair.RepairContent(garbled).Should().Be(expected);
    }

    [Theory]
    [InlineData("Turn to agy")]  // out of range either way
    [InlineData("Turn to 497")]  // out of range for a 400-section book
    [InlineData("turn to 5x")]   // trailing junk never stripped (counts/ordinals stay text)
    public void RepairContent_LeavesAmbiguousTargetsUntouched(string input)
    {
        TurnToRepair.RepairContent(input, maxSection: 400).Should().Be(input);
    }

    [Theory]
    [InlineData("Turn to 12")]
    [InlineData("turn north along the pass")]
    [InlineData("turn of events")]
    [InlineData("If you win, turn to 4.")]
    [InlineData("His intimidating stare turns to an expression of pain.")]
    [InlineData("She turns to stone before your eyes.")]
    [InlineData("turn both 12")]
    [InlineData("the modern tower looms")]
    [InlineData("turn west along the path")]
    [InlineData("turn white with fear")]
    public void RepairContent_IsIdempotentOnCleanText(string input)
    {
        TurnToRepair.RepairContent(input).Should().Be(input);
    }

    [Fact]
    public void RepairContent_RespectsMaxSection()
    {
        TurnToRepair.RepairContent("Turn to o261", maxSection: 343).Should().Be("Turn to 261");
        TurnToRepair.RepairContent("Turn to 399", maxSection: 343).Should().Be("Turn to 399");
    }
}

public class ChoicesParityTests
{
    private static Section Sec(int n, string content) =>
        new() { SectionNumber = n, Content = content };

    [Fact]
    public void BareExitFooter_BecomesChoice()
    {
        var cleaned = ContentAnalyzer.Analyze(Sec(1, "You stop and listen but hear nothing.\n\nTurn to 85."));

        cleaned.References.Should().Equal(85);
        cleaned.Choices.Should().ContainSingle();
        cleaned.Choices[0].Target.Should().Be(85);
        cleaned.Choices[0].Label.Should().BeNull();
    }

    [Fact]
    public void ParenthesisedPair_BecomesTwoChoices()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(1, "Will you investigate (turn to 272) or threaten him (turn to 127)?"));

        cleaned.References.Should().BeEquivalentTo(new[] { 127, 272 });
        cleaned.Choices.Select(c => c.Target).Should().BeEquivalentTo(new[] { 272, 127 });
    }

    [Fact]
    public void GoToReference_BecomesChoice()
    {
        var cleaned = ContentAnalyzer.Analyze(Sec(1, "You head back.\nGo to 14."));

        cleaned.References.Should().Equal(14);
        cleaned.Choices.Should().ContainSingle();
        cleaned.Choices[0].Target.Should().Be(14);
    }

    [Fact]
    public void GarbledTurnTo_RepairedBeforeChoiceExtraction()
    {
        var cleaned = ContentAnalyzer.Analyze(Sec(1, "If you win, Tum o 150."));

        cleaned.References.Should().Equal(150);
        cleaned.Choices.Should().ContainSingle();
        cleaned.Choices[0].Target.Should().Be(150);
        cleaned.Clean.Should().NotContain("Tum o");
    }

    [Fact]
    public void ContinuationTail_StillNotAChoice()
    {
        // Single-ref section whose choice line is really the tail of a narrative
        // sentence: lowercase label, blank line before, previous line unpunctuated.
        var cleaned = ContentAnalyzer.Analyze(
            Sec(1, "You hear something move in the dark\nand hold your breath\n\nand wait quietly Turn to 9."));

        cleaned.References.Should().Equal(9);
        cleaned.Choices.Should().BeEmpty();
        cleaned.Clean.Should().Contain("and wait quietly");
    }

    [Fact]
    public void DuplicateTargets_Deduplicated()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(1, "Turn to 5.\nYou wait. Turn to 5."));

        cleaned.Choices.Select(c => c.Target).Should().Equal(5);
    }

    [Fact]
    public void ChoiceLineLabel_DropsEarlierInlineRef()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(1, "If you possess a Glove of Missile Dexterity, turn to 55. if you do not, turn to 10."));

        cleaned.References.Should().BeEquivalentTo(new[] { 10, 55 });
        cleaned.Choices.Should().HaveCount(2);
        cleaned.Choices.Single(c => c.Target == 10).Label.Should().Be("if you do not,");
        cleaned.Choices.Single(c => c.Target == 55).Label.Should().Be("If you possess a Glove of Missile Dexterity");
    }

    [Fact]
    public void WrappedTurnTo_JoinedBeforeExtraction()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(1, "If you possess the glove, turn to\n55. if you do not, turn to 10."));

        cleaned.References.Should().BeEquivalentTo(new[] { 10, 55 });
        cleaned.Choices.Single(c => c.Target == 55).Label.Should().Be("If you possess the glove");
        cleaned.Choices.Single(c => c.Target == 10).Label.Should().Be("if you do not,");
    }

    [Fact]
    public void WrappedGarbledVerb_JoinedBeforeExtraction()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(1, "You may either return to the ship (tum to\n184) or press onwards."));

        cleaned.References.Should().Equal(184);
    }

    [Fact]
    public void WarlockSectionFix_MapsNumberlessTurntoExit()
    {
        WarlockOfFiretopMountainParser.ApplySectionFixes(
            31, "leave through the north door. Turnto")
            .Should().Be("leave through the north door. Turn to 90.");
    }

    [Theory]
    [InlineData(296, "Turn to 4z2.", "Turn to 42.")]
    public void WarlockSectionFix_MapsOutOfRangeToken(int section, string garbled, string expected)
    {
        WarlockOfFiretopMountainParser.ApplySectionFixes(section, $"prefix {garbled} suffix")
            .Should().Be($"prefix {expected} suffix");
    }

    [Theory]
    [InlineData(182, "turn to 7e.", "turn to 70.")]
    [InlineData(221, "(urn to 19g).", "(turn to 199).")]
    [InlineData(355, "turn to iyo.", "turn to 340.")]
    [InlineData(358, "man trap. Turn to go.", "man trap. Turn to 40.")]
    public void ForestSectionFix_MapsEvidenceBackedTargets(int section, string garbled, string expected)
    {
        ForestOfDoomParser.ApplySectionFixes(section, $"prefix {garbled} suffix")
            .Should().Be($"prefix {expected} suffix");
    }

    [Theory]
    [InlineData(95, "turn to 357.", "Turn to 367.")]
    [InlineData(100, "1 not, turn to 107", "If not, turn to 276.")]
    [InlineData(74, "counter-attack. Tum", "counter-attack. Turn to 377.")]
    [InlineData(177, "down the steps (hum", "down the steps (turn to 344).")]
    [InlineData(205, "tien Lo 300", "turn to 368")]
    [InlineData(229, "(fur to 230", "(turn to 230).")]
    [InlineData(330, "ten Lo 208", "turn to 208.")]
    [InlineData(330, "turn to 33", "turn to 120.")]
    [InlineData(354, "turn to 355", "Turn to 188.")]
    public void CitadelSectionFix_MapsCurrentPipelineForms(int section, string garbled, string expected)
    {
        CitadelOfChaosParser.ApplySectionFixes(section, $"prefix {garbled} suffix")
            .Should().Be($"prefix {expected} suffix");
    }

    [Fact]
    public void CitadelSectionFix_DoesNotRewriteLongerNumbers()
    {
        CitadelOfChaosParser.ApplySectionFixes(330, "turn to 330 and turn to 335")
            .Should().Be("turn to 330 and turn to 335");
    }

    [Theory]
    [InlineData("Your adventure is over.")]
    [InlineData("Your adventure is at an end.")]
    [InlineData("This is the end of your adventure.")]
    [InlineData("bringing your adventure to a gory and untimely end.")]
    [InlineData("Shipwrecked, you cannot continue your adventure.")]
    public void DeathPhrasingVariants_FlagDeathEnd(string ending)
    {
        var cleaned = ContentAnalyzer.Analyze(Sec(1, "The wheel crushes you. " + ending));

        cleaned.Features.IsEnd.Should().BeTrue();
        cleaned.Features.DeathEnd.Should().BeTrue();
    }

    [Fact]
    public void TrailingPageRangeFooter_StrippedFromClean()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(309, "If you fight on and defeat the warship, turn to 135. 310-311"));

        cleaned.References.Should().Equal(135);
        cleaned.Clean.Should().NotContain("310-311");
        cleaned.Clean.Should().EndWith("turn to 135.");
    }

    [Fact]
    public void TriumphantlyForAnother_IsNotPlayerVictory()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(350, "He declares triumphantly himself the winner. You have lost the wager."));

        cleaned.Features.IsEnd.Should().BeTrue();
        cleaned.Features.VictoryEnd.Should().BeFalse();
    }

    [Fact]
    public void TriumphantPlayer_StillVictory()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(400, "I am the victor. I am triumphant. You have won."));

        cleaned.Features.VictoryEnd.Should().BeTrue();
    }

    [Fact]
    public void BackCoverAdvert_StrippedFromClean_RawKept()
    {
        var cleaned = ContentAnalyzer.Analyze(
            Sec(400, "I am the victor. You have won. Send check or money order-no cash. City/State Zip"));

        cleaned.Clean.Should().NotContain("money order");
        cleaned.Clean.Should().Contain("You have won");
        cleaned.Raw.Should().Contain("money order");
        cleaned.Features.VictoryEnd.Should().BeTrue();
    }

    [Fact]
    public void CuratedIntroOverride_ReplacesParsedIntroduction()
    {
        var book = new Book
        {
            Title = "FF02 Citadel of Chaos",
            Introduction = "(frl garbled ocr junk",
            Sections = new List<Section> { new() { SectionNumber = 1, Content = "Turn to 2." } },
        };

        var cleaned = BookCleaner.Clean(book, "FF02 Citadel of Chaos.json");

        cleaned.Meta.Introduction.Should().Contain("HISTORY");
        cleaned.Meta.Introduction.Should().Contain("Balthus Dire");
        cleaned.Meta.Introduction.Should().NotContain("frl garbled");
    }

    [Fact]
    public void ExplicitIntroOverride_WinsOverCurated()
    {
        var book = new Book
        {
            Title = "FF02 Citadel of Chaos",
            Introduction = "(frl garbled ocr junk",
            Sections = new List<Section> { new() { SectionNumber = 1, Content = "Turn to 2." } },
        };

        var cleaned = BookCleaner.Clean(book, "FF02 Citadel of Chaos.json", "Custom intro.");

        cleaned.Meta.Introduction.Should().Be("Custom intro.");
    }

    [Fact]
    public void BookWithoutCuratedIntro_KeepsParsedIntroduction()
    {
        var book = new Book
        {
            Title = "FF16 Seas of Blood",
            Introduction = "The seaport of Tak.",
            Sections = new List<Section> { new() { SectionNumber = 1, Content = "Turn to 2." } },
        };

        var cleaned = BookCleaner.Clean(book, "FF16 Seas of Blood.json");

        cleaned.Meta.Introduction.Should().Be("The seaport of Tak.");
    }
}
