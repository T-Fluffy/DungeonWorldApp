using System.Text.RegularExpressions;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF18 Rebel Planet (Robin Waterfield, 1985). Rebuilt from a manual reconstruction manifest
/// (300 dpi line transcripts, 400 sections across pages 17-120; wide pages split into two-up
/// L/R halves, front matter on narrow single-column pages). Intro uses the cover blurb (1) plus
/// MISSION BACKGROUND (8-9); the curated intro text ships in Intros/ff18.txt.
///
/// <para><b>Layout notes.</b> Unlike FF13-FF17 this book prints a decorative range marker at the
/// top of each half ("39-41") followed by the individual section numbers further down the column,
/// so headers are NOT always at the top of a half. The manifest was derived by walking the headers
/// in ascending order - the book prints sections in order, which is the reliable anchor. 21 headers
/// were unreadable in the transcript and were placed by hand from the gap between their placed
/// neighbours (garbled examples: "2" for 27, "Zz" for 17, "28g" for 285, "33" for 331, "95" for 96,
/// "=I 320", "| 386", "_ 309", "375%:"). S39 starts after the p26 range marker because S38's body
/// runs off the foot of p25, and S400 carries an end cap so the back-cover advertising on pages
/// 120-122 is not absorbed into the final section.</para>
///
/// <para>This is one of the "hard scans": it opts into the isolated <see cref="HardScanExitRepair"/>
/// rules and exit-fragment retention, without the hardened second OCR pass.</para>
/// </summary>
public sealed class RebelPlanetParser : ManifestDungeonWorldParser
{
    public RebelPlanetParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "RebelPlanet";
    protected override string TitleMatch => "Rebel Planet";
    protected override string Slug => "ff18_rebel_planet";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff18.json";

    /// <summary>Cover blurb plus MISSION BACKGROUND; the rules pages (10-16) are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 8, 9 };

    /// <summary>FF18 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Written by Robin Waterfield, not the usual series authors.</summary>
    protected override string Author => "Robin Waterfield";

    /// <summary>
    /// FF18 keeps exit-fragment retention and the isolated exit repair but stays off the hardened
    /// second OCR pass, which on FF15 cost reachable sections by breaking the "dangling turn-to +
    /// orphaned number fragment" pattern its curated fixes rely on.
    /// </summary>
    protected override bool KeepExitFragments => true;

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    /// <summary>FF18-scoped exit repair, bounded to this book's 400 sections.</summary>
    protected override string RepairExits(int sectionNumber, string content) => RepairExits(content);

    /// <summary>Exposed for tests.</summary>
    public static string RepairExits(string content) =>
        HardScanExitRepair.Repair(content, MaxSections);

    /// <summary>
    /// True when <paramref name="content"/> already carries a real exit to <paramref name="target"/>.
    /// The trailing (?!\d) matters: this scan reads stray extra digits, so a substring check would
    /// wrongly conclude the exit is present and skip the restoration.
    /// </summary>
    private static bool HasExit(string content, int target) =>
        Regex.IsMatch(content, $@"\bturn\s+to\s+{target}(?!\d)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Per-section corrections for exit targets the scan read as a different but valid number.
    /// Every rule below is confirmed against the arek sol17 solution chain; anything unresolved is
    /// deliberately left verbatim rather than guessed.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // FF18 routes a lot of play through binary/decimal code puzzles whose sections are entered
        // by "turn to the paragraph whose number looks the same as this binary number" rather than
        // by a literal target. The number is a reader-computed value, so no exit can be parsed and
        // the section looks orphaned. sol17 proves each of these parents really does reach that
        // section, so the edge is restored rather than left dangling.
        // Restored only when absent, so a future better scan needs no edit.
        (int parent, int target, string note)[] codeEdges =
        {
            (382, 5, "southern club code -> way to Fission Chips (sol17: 382, 5)"),
            (169, 110, "arsenal code -> inside the arsenal (sol17: 169, 110)"),
            (255, 155, "palindrome digits -> Dorado lets you go (sol17: 255, 155)"),
            (349, 231, "guard leads you away -> arena door (sol17: 349, 231)"),
            (219, 297, "computer code -> go down to the basement (sol17: 219, 297)"),
            (245, 294, "left-hand cave -> enter the gloom (sol17: 245, 294)"),
            (222, 400, "elmonite blast -> the Empire is finished (sol17: 222, 400)"),
            (101, 294, "hotel search -> cave mouth (sol17: 101, 294)"),
            // S98 has no sol17 coverage (the solution visits 173 of 400 sections). Its parent is
            // S49 "climb back down the path": S358 sends you to S10, S10 splits on carrying the
            // staff, and S10's staff branch reaches S49, which is the climb-down hub whose cliff
            // descent is S232 (no staff) or S98 (staff breaks). Only S98 was orphaned.
            (49, 98, "climb down the path -> cliff descent with the staff (book: S358,S10,S49,S98)"),
            // S239 likewise: S97 tells you to note Zacharias's room number, S202 sends you to the
            // Arts Floor, and S258 says "turn to the paragraph which has the same number as the
            // room you need" - the room is 239.
            (258, 239, "Arts Floor room-number puzzle -> knock on Zacharias's door (book: S97,S202,S258,S239)"),
        };
        foreach (var (parent, target, _) in codeEdges)
            if (sectionNumber == parent && !HasExit(content, target))
                content += $" (turn to {target}).";

        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair FIRST, so the text
            // matched here is already post-shared-repair.
            222 => content.Replace("Turn to goo.", "Turn to 400.", StringComparison.OrdinalIgnoreCase), // elmonite (sol17: 222, 400)
            60 => content.Replace("turn south. Turn to 99-", "turn south. Turn to 99.", StringComparison.OrdinalIgnoreCase), // head south (sol17)
            _ => content,
        };
    }
}