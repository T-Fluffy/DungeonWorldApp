using DungeonWorld.Infrastructure.Parsing;
using FluentAssertions;
using System.Text.RegularExpressions;
using Xunit;

namespace DungeonWorld.Tests;

/// <summary>
/// Pins FF17 Appointment With F.E.A.R. behaviour: the per-section fix hook, the hard-scan exit
/// repair as it applies to this book's 440 sections, and - just as important - the scans whose
/// reference the OCR destroyed beyond safe reconstruction. Those must not yield an exit: inventing
/// a target would fabricate a link the printed book does not contain.
/// Complements the refs golden (which gates the whole graph but skips when CleanedData is absent).
/// </summary>
public sealed class AppointmentWithFearParserTests
{
    /// <summary>Matches an exit the graph extractor would actually turn into a reference.</summary>
    private static readonly Regex ParseableExit = new(
        @"\b(?:turn|go|burn)\s+(?:to\s+)?\.?\s*(?:the\s+)?\d{1,4}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Theory]
    [InlineData(1, "As you walk the ten blocks to work that morning. Turn to 12.")]
    [InlineData(7, "After the second round of combat, tum to 359.")]
    [InlineData(440, "You have earned 10 Hero Points for this victory.")]
    public void ApplySectionFixes_IsCurrentlyAPassThrough(int section, string content) =>
        // No hand-written exit corrections were needed for FF17: every exit is either legible or
        // genuinely unrecoverable. The hook stays so future evidence can be added without touching
        // the shared pipeline.
        AppointmentWithFearParser.ApplySectionFixes(section, content).Should().Be(content);

    [Theory]
    // Garbled verbs and prepositions are the dominant FF17 failure mode.
    [InlineData("Tuen to 79.", "turn to 79.")]
    [InlineData("hand them over. l'urn [0 340.", "turn to 340.")]
    [InlineData("For this wrongful arrest. Turn now to 148.", "Turn to 148.")]
    [InlineData("See what is happening (turn iD 434)", "turn to 434")]
    [InlineData("The concert is EVIE, tum in 122.", "turn to 122.")]
    [InlineData("After the second round, burn tor 335.", "turn to 335.")]
    // Exit fragments the noise filter used to delete, recovered from the 300 dpi dump.
    [InlineData("leave this affair in the hands of the police. Tum to 73-", "turn to 73-")]
    [InlineData("explore the harbour as soon as you can. Now tum to157.", "turn to 157.")]
    public void RepairExits_FixesRecoverableGarble(string input, string expected) =>
        AppointmentWithFearParser.RepairExits(input).Should().Contain(expected);

    [Fact]
    public void RepairExits_AllowsSectionsAbove380ThatFF13Lacks()
    {
        // 420 exists only because FF17 runs to 440; the bound must permit it.
        AppointmentWithFearParser.RepairExits("You find the file. Turn to 420.")
            .Should().Contain("Turn to 420");
    }

    [Theory]
    // The scan rendered these targets unrecoverably. The shared TurnToRepair deliberately refuses
    // ambiguous tokens, and so must this book: no reference is invented, and the section is
    // reported as a dead end for later manual review instead.
    [InlineData("You fold the map up and put it back in your pocket. Turn to Ba.")]
    [InlineData("He is none other than Olga Karpov. You may add 5 Hero Points; then tom to Jo.")]
    [InlineData("somewhere on 3rd Avenue! With your device trained on her. Tum lo2q1.")]
    [InlineData("return to Radd Square (tum to 71k")]
    [InlineData("the alarm does not work. Turn now to 2h.")]
    public void RepairExits_DoesNotInventAnExitForDestroyedTargets(string input)
    {
        string repaired = AppointmentWithFearParser.RepairExits(input);
        ParseableExit.IsMatch(repaired).Should().BeFalse(
            "an unreadable target must not become a reference, but got: " + repaired);
    }

    [Theory]
    // Out-of-range targets are neutralised so the graph gate cannot see a dangling edge.
    [InlineData("Award yourself 3 Hero Points and turn to 2286,", "turn to [unclear]")]
    [InlineData("continue your original journey to work turn to 3471)", "turn to [unclear]")]
    public void RepairExits_NeutralisesOutOfRangeTargets(string input, string expected) =>
        AppointmentWithFearParser.RepairExits(input).Should().Contain(expected);

    [Fact]
    public void RepairExits_LeavesOrdinaryProseAlone()
    {
        const string prose = "You walk in to the shop and go down to 5th Avenue. Turn to 336.";
        AppointmentWithFearParser.RepairExits(prose).Should().Be(prose);
    }
}