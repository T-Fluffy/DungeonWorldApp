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
    // Exit targets OCR read as a different but valid in-range number, or lost at a break. The raw
    // forms are what ApplySectionFixes sees, before HardScanExitRepair canonicalises the verb.
    [InlineData(60, "turn to 8.", "turn to 98.")]
    [InlineData(72, "Turn to 290", "Turn to 299")]
    [InlineData(77, "retum to gy", "turn to 97")]
    [InlineData(78, "turn\nto 2286,", "turn to 226,")]
    [InlineData(83, "Poo tlic to Bo.", "Turn to 60.")]
    [InlineData(99, "(lurn to", "(turn to 305).")]
    [InlineData(124, "Turn now to &7.", "Turn to 67.")]
    [InlineData(130, "bottom edge. Tum", "bottom edge. Turn to 298.")]
    [InlineData(182, "feet. Tum", "feet. Turn to 241.")]
    [InlineData(235, "Turn io 23.", "Turn to 25.")]
    [InlineData(271, "Tom fo 133.", "Turn to 143.")]
    [InlineData(296, "{form fo 423)", "(turn to 423)")]
    [InlineData(298, "urn to 150", "turn to 159")]
    [InlineData(299, "Turn to 204.", "Turn to 294.")]
    [InlineData(300, "turn to 5) 3277.", "turn to 327.")]
    [InlineData(311, "Turn te ro", "Turn to 70")]
    [InlineData(327, "Turn to9", "Turn to 4")]
    [InlineData(336, "Turn now", "Turn to 372.")]
    [InlineData(368, "Purn tog.", "Turn to 369.")]
    [InlineData(369, "(bn to 27817", "(turn to 278)")]
    [InlineData(369, "turn to [unclear]7", "turn to 278")]
    [InlineData(372, "7a}", "72")]
    [InlineData(391, "(turn to 308)", "(turn to 396)")]
    [InlineData(409, "{turn tr 285)", "(turn to 185)")]
    [InlineData(412, "(rum\nor 2687", "(turn to 382")]
    [InlineData(428, "(turn tir 18g)", "(turn to 189)")]
    [InlineData(428, "(lum to 30057", "(turn to 300)")]
    public void ApplySectionFixes_RewritesAdjudicatedExits(int section, string raw, string expected) =>
        AppointmentWithFearParser.ApplySectionFixes(section, raw).Should().Be(expected);

    [Fact]
    public void ApplySectionFixes_LeavesUnlistedSectionsAlone()
    {
        const string content = "You walk in to the shop and go down to 5th Avenue. Turn to 336.";
        AppointmentWithFearParser.ApplySectionFixes(1, content).Should().Be(content);
    }

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