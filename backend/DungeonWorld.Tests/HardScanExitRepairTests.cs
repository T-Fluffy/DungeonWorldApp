// File: DungeonWorld.Tests/HardScanExitRepairTests.cs
using DungeonWorld.Infrastructure.Parsing;
using FluentAssertions;

namespace DungeonWorld.Tests;

/// <summary>
/// Guards the FF13/FF15/FF17-only exit repair. These tests exist because the repair is deliberately
/// isolated from <c>TurnToRepair</c>, which the DataCleaner also applies to every already-completed
/// book: an over-broad rule here would silently rewrite curated text in FF01-FF16, so the
/// must-not-change cases below are as important as the must-change ones.
/// </summary>
public class HardScanExitRepairTests
{
    private const int MaxSection = 440;

    [Theory]
    // Garbled verb + garbled preposition (the FF17 signature failure).
    [InlineData("If the resulting reference makes no sense, um to 220.", "turn to 220.")]
    [InlineData("You look past the guard. l'urn [0 340.", "turn to 340.")]
    [InlineData("A narrow street. {hom to 235), or (fur to 134)?", "turn to 235")]
    [InlineData("Defeat the Queen, rm bo o241.", "turn to 241.")]
    [InlineData("Investigate the bushes (umn to 54).", "turn to 54")]
    [InlineData("The engine room [him to 27], or the quarters", "turn to 27")]
    [InlineData("The concert is EVIE, tum in 122.", "turn to 122.")]
    // Correct verb, corrupted preposition or an intervening filler word.
    [InlineData("For this wrongful arrest. Turn now to 148.", "Turn to 148.")]
    [InlineData("You listen to it. Turn io 327.", "Turn to 327.")]
    [InlineData("For capturing him. Now turn ko 103.", "turn to 103.")]
    [InlineData("See what is happening (turn iD 434)", "turn to 434")]
    // A "burn to" whose preposition is corrupted; the genuine form is shielded and left alone.
    [InlineData("After the second round, burn tor 335.", "turn to 335.")]
    public void Repair_FixesGarbledExits(string garbled, string expected) =>
        HardScanExitRepair.Repair(garbled, MaxSection).Should().Contain(expected);

    [Theory]
    // An in-range exit must never be rewritten.
    [InlineData("If you are unlucky, turn to 12.")]
    [InlineData("Turn to 336.")]
    [InlineData("You must turn to 336 to continue.")]
    [InlineData("turn to the 5th reference")]
    // A genuine "burn to" is a real exit verb in these books and must survive verbatim.
    [InlineData("burn to 274")]
    // Ordinary prose must never be mistaken for an exit. The verb list is an explicit allow-list
    // precisely so these are untouched.
    [InlineData("go down to 5th Avenue")]
    [InlineData("walk in to the shop")]
    [InlineData("He falls down to the floor.")]
    [InlineData("return to the previous reference")]
    [InlineData("the vault is over a metre thick")]
    public void Repair_LeavesValidTextUnchanged(string input) =>
        HardScanExitRepair.Repair(input, MaxSection).Should().Be(input);

    [Theory]
    // OCR reads a longer number where the book printed a shorter one. Inventing section 2286 or
    // 3471 would create a link the book does not contain, so the branch is marked unreadable
    // instead: visible to a reader, and no dangling edge for the graph gate.
    [InlineData("Hero Points and turn to 2286,", "turn to [unclear]")]
    [InlineData("continue your journey to work turn to 3471)", "turn to [unclear]")]
    [InlineData("punts). Tum now to 0.", "turn to [unclear]")]
    public void Repair_NeutralizesOutOfRangeTargets(string input, string expected) =>
        HardScanExitRepair.Repair(input, MaxSection).Should().Contain(expected);

    [Fact]
    public void Repair_OutOfRangeIsBoundedByThisBooksSectionCount()
    {
        // 400 is a valid FF15 section but not a valid FF13 one; FF13 must not gain the edge.
        HardScanExitRepair.Repair("You turn to 400 now.", 380).Should().Contain("[unclear]");
        HardScanExitRepair.Repair("You turn to 400 now.", 400).Should().NotContain("[unclear]");
    }

    [Fact]
    public void Repair_IsIdempotent() =>
        HardScanExitRepair
            .Repair(HardScanExitRepair.Repair("You look past the guard. l'urn [0 340.", MaxSection), MaxSection)
            .Should().Be("You look past the guard. turn to 340.");

    [Fact]
    public void Repair_HandlesEmptyInput()
    {
        HardScanExitRepair.Repair("", MaxSection).Should().BeEmpty();
        HardScanExitRepair.Repair(null!, MaxSection).Should().BeEmpty();
    }

    [Fact]
    public void RestoreOnlyExit_AddsAnExitOnlyWhenNoneIsParseable()
    {
        HardScanExitRepair.RestoreOnlyExit("The path ends here.", 340, MaxSection)
            .Should().Be("The path ends here. (turn to 340).");
        // Never adds a second branch to a section that already reads correctly.
        HardScanExitRepair.RestoreOnlyExit("You go on. turn to 12.", 340, MaxSection)
            .Should().Be("You go on. turn to 12.");
        // Never points outside the book.
        HardScanExitRepair.RestoreOnlyExit("The path ends here.", 999, 380)
            .Should().Be("The path ends here.");
    }
}