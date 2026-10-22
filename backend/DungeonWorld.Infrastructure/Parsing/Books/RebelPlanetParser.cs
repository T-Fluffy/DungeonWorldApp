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

    /// <summary>
    /// Sections whose printed body is split across a page turn or truncated by the OCR
    /// transcript. Verified against the 600-dpi scan.
    /// </summary>
    private static readonly Dictionary<int, string> SectionOverrides = new()
    {
        [297] = "297\nWell done! The door slides open easily and lets you into an ante-chamber before sliding shut again behind you. Only then do the next doors open - and there are four more sets of doors; this is to keep the building as free as possible from dust. Once inside, you can see the computer room ahead, but there are also some steps down to a basement. Will you go straight to the computer room (turn to 381), or down to the basement first (turn to 169)?",
        [355] = "355\nThe crack is deep and narrow, and you cannot reach too far. The strength of the bird, which you already knew from your fight with it, is further proved by the size of the objects you find - smaller things have presumably fallen into the inaccessible depths of the fissure. Apart from some useless chunks of metal and a broken spy missile, you find a staff with seven strips of different metals coiled around its wooden stem, a casket containing 180 credits, and a mind-probe. The probe will fit into your anti-grav pack, and you put the money in your money-belt, but if you take the staff, it will make your descent down the cliff more risky, since you will have to carry it by hand. Even more alarming is the fact that a faint power seems to be suggesting that you take the staff. You cannot tell whether the power is good or evil, but you are able to resist it, if you want to. Note down on your Equipment List what you take. Will you now explore the other fissure (turn to 14), or climb back down to the path to continue your journey (turn to 10)?",
    };

    /// <summary>Exposed for tests; returns the verified text for split/truncated sections.</summary>
    public static string ApplySectionOverrides(int sectionNumber, string content) =>
        SectionOverrides.TryGetValue(sectionNumber, out var overrideText) ? overrideText : content;

    /// <summary>Written by Robin Waterfield, not the usual series authors.</summary>
    protected override string Author => "Robin Waterfield";

    /// <summary>
    /// FF18 keeps exit-fragment retention and the isolated exit repair but stays off the hardened
    /// second OCR pass, which on FF15 cost reachable sections by breaking the "dangling turn-to +
    /// orphaned number fragment" pattern its curated fixes rely on.
    /// </summary>
    protected override bool KeepExitFragments => true;

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(ApplySectionOverrides(sectionNumber, content)));

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
        // The scan frequently reads an opening parenthesis as a brace; none are used as literal
        // braces in this book.
        content = content.Replace('{', '(');

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
            // S239: S97 tells you to note Zacharias's room number, S202 sends you to the
            // Arts Floor, and S258 says "turn to the paragraph which has the same number as the
            // room you need" - the room is 239.
            (258, 239, "Arts Floor room-number puzzle -> knock on Zacharias's door (book: S97,S202,S258,S239)"),
        };
        foreach (var (parent, target, _) in codeEdges)
            if (sectionNumber == parent && !HasExit(content, target))
                content += $" (turn to {target}).";

        return sectionNumber switch
        {
            // The new manifest merges the first printed 47 into 46; drop its header line.
            46 => Regex.Replace(content, @"(?m)^\s*47\s*\r?\n?", string.Empty),
            // S48's printed footer is read as "| Turn to 164."; drop the stray pipe.
            48 => Regex.Replace(content, @"(?m)^\s*\|\s*Turn to 164\.\s*$", "Turn to 164.", RegexOptions.IgnoreCase),
            // S49 prints "bitten by a Saw-toothed Rat, turn to 98"; the scan reads 98 as "¢8".
            49 => Regex.Replace(content, @"\bturn\s+to\s*[^\d\r\n]{0,2}8\b", "turn to 98", RegexOptions.IgnoreCase),
            // S106: "If you know the Scabrok's weakness, turn to the relevant reference" - S262 is
            // that weakness, and sol17 confirms 106, 262.
            106 => Regex.Replace(content, @"turn\s+to\s+the\s+relevant\s+reference", "turn to 262", RegexOptions.IgnoreCase),
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair FIRST, so the text
            // matched here is already post-shared-repair.
            222 => content.Replace("Turn to goo.", "Turn to 400.", StringComparison.OrdinalIgnoreCase), // elmonite (sol17: 222, 400)
            60 => content.Replace("turn south. Turn to 99-", "turn south. Turn to 99.", StringComparison.OrdinalIgnoreCase), // head south (sol17)
            _ => content,
        };
    }
}