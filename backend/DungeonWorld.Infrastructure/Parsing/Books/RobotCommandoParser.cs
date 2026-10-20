using System.Text.RegularExpressions;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF22 Robot Commando (Steve Jackson, 1986). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across PDF pages 16-109;
/// each PDF page is a two-up spread split into L/R halves). Sections print in numeric
/// order, so bounds run from one section number to the next. Intro uses the cover blurb (1)
/// plus BACKGROUND (14-15); the curated intro text ships in Intros/ff22.txt.
///
/// <para><b>Layout notes.</b> Folio ranges head most spreads ("112-113") with section
/// headers further down; single-number folios in the front matter collide with section
/// numbers and are excluded by page. Headers are frequently lost or garbled to two-letter
/// pairs ("Bs" = 85, "G3" = 81, "18g" = 185/189) or dropped hundreds ("7" = 37, "1" = 141);
/// those pins sit on the garbled header or body start. S355 is absent from the scan.
/// Each placement was confirmed against the transcript and, where ambiguous, the rendered
/// PDF page.</para>
/// </summary>
public sealed class RobotCommandoParser : ManifestDungeonWorldParser
{
    public RobotCommandoParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "RobotCommando";
    protected override string TitleMatch => "Robot Commando";
    protected override string Slug => "ff22_robot_commando";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff22.json";

    /// <summary>Cover blurb plus BACKGROUND; the rules pages are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 14, 15 };

    /// <summary>FF22 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Written by Steve Jackson (print: p4 credits).</summary>
    protected override string Author => "Steve Jackson";

    /// <summary>FF22 keeps exit-fragment retention (exit numbers land on their own lines).</summary>
    protected override bool KeepExitFragments => true;

    /// <summary>Book-scoped exit repair, applied after the shared TurnToRepair.</summary>
    protected override string RepairExits(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, content);

    /// <summary>
    /// True when <paramref name="content"/> already carries a real exit to <paramref name="target"/>.
    /// The trailing (?!\d) matters: this scan reads stray extra digits, so a substring check would
    /// wrongly conclude the exit is present and skip the restoration.
    /// </summary>
    private static bool HasExit(string content, int target) =>
        Regex.IsMatch(content, $@"\bturn\s+to\s+{target}(?!\d)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Per-section corrections for exit targets the scan lost or misread. Every rule below is
    /// confirmed against the arek sol21 solution chain or the rendered PDF page; anything
    /// unresolved is deliberately left verbatim rather than guessed.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Restored only when absent, so a future better scan needs no edit. Every edge below
        // is a consecutive sol21 pair (the player physically turned from parent to target) or a
        // print-verified option whose verb garble defeats the cleaner.
        (int parent, int target, string note)[] codeEdges =
        {
            (392, 333, "explore further -> lobby (sol21: 392, 333)"),
            (79, 288, "simulator dark -> arcade (sol21: 79, 288)"),
            (41, 165, "Wasp take -> Pleasure (sol21: 41, 165)"),
            (168, 14, "Hall Beta back -> museum (sol21: 168, 14)"),
            (352, 166, "victory trumpets -> Worship (sol21: 352, 166)"),
            (107, 166, "well-being -> Worship (sol21: 107, 166)"),
            (17, 53, "model number -> Cloak (sol21: 17, 53)"),
            (359, 200, "Wasp model -> handling (sol21: 359, 200)"),
            (103, 111, "champions reference -> duel (sol21: 103, 111)"),
            (102, 250, "Amulet immediate -> alarm (sol21: 102, 250)"),
            (200, 378, "arcade handling -> Industry (sol21: 200, 378)"),
            (206, 265, "robot back -> Industry (sol21: 206, 265)"),
            (148, 91, "Luck pass -> surgeon (sol21: 148, 91)"),
            (178, 94, "tunnel stairway (render p59: 178, 94)"),
        };
        foreach (var (parent, target, _) in codeEdges)
            if (sectionNumber == parent && !HasExit(content, target))
                content += $" (turn to {target}).";

        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair FIRST, so the text
            // matched here is already post-shared-repair. Plain string Replace is used (proven
            // in-pipeline); every target below was read verbatim from pipeline output and every
            // corrected number is sol21- or render-proven.
            5 => content.Replace("turn to 935.", "turn to 235.",
                StringComparison.OrdinalIgnoreCase), // swamp leave (sol21: 5, 235)
            155 => content.Replace("turn to 635,", "turn to 335,",
                StringComparison.OrdinalIgnoreCase), // smashed robot (sol21: 155, 335)
            14 => content.Replace("Turn to 3¢", "Turn to 36")
                .Replace("Turn to Be", "Turn to 80")
                .Replace("Turn to sf", "Turn to 58",
                StringComparison.OrdinalIgnoreCase), // museum info (sol21: 14, 36/80/58)
            45 => content.Replace("turn to 99.", "turn to 159.",
                StringComparison.OrdinalIgnoreCase), // run for cover (sol21: 45, 159)
            49 => content.Replace("The Temple of Glory? Turn to 330", "The Temple of Glory? Turn to 339",
                StringComparison.OrdinalIgnoreCase), // Glory (sol21: 49, 339)
            53 => content.Replace("Return to 8s,", "turn to 85,",
                StringComparison.OrdinalIgnoreCase), // Cloak (sol21: 53, 85)
            17 => content.Replace("retum to 8g.", "turn to 89.",
                StringComparison.OrdinalIgnoreCase), // uniform bluff (print: S17, S89)
            102 => content.Replace("turn immediately to 25a", "turn immediately to 250",
                StringComparison.OrdinalIgnoreCase), // Amulet number (sol21: 102, 250)
            165 => content.Replace("Visit the airfield? Turn to 42", "Visit the airfield? Turn to 41",
                StringComparison.OrdinalIgnoreCase), // airfield is S41 (sol21: 165, 41)
            67 => content.Replace("turn toxz", "turn to 12",
                StringComparison.OrdinalIgnoreCase), // fly away (render p32: 67, 12)
            87 => content.Replace("turn toyz", "turn to 177",
                StringComparison.OrdinalIgnoreCase), // enemy uniform (render p36: 87, 177)
            177 => content.Replace("Turn to \u00e95.", "Turn to 65.",
                StringComparison.OrdinalIgnoreCase), // uniform bluff (render p59: 177, 65)
            194 => content.Replace("turn back to 308", "turn to 308",
                StringComparison.OrdinalIgnoreCase), // construction robot (print: 194, 308)
            199 => content.Replace("Turn to fi.", "Turn to 65.",
                StringComparison.OrdinalIgnoreCase), // uniform bluff (render p64: 199, 65)
            _ => content,
        };
    }
}
