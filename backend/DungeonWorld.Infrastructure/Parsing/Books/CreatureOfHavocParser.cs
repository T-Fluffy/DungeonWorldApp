using System.Text.RegularExpressions;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF24 Creature of Havoc (Steve Jackson, 1986). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 460 sections across PDF pages 19-165;
/// each PDF page is a two-up spread split into L/R halves). Sections print scrambled
/// (notably a p74/p75 inversion: 161-164 before 157-160), so bounds run pin-to-pin in
/// transcript order. Intro uses the cover blurb (1) plus TALES OF TROLLTOOTH PASS (9-18);
/// the curated intro text ships in Intros/ff24.txt.
///
/// <para><b>Layout notes.</b> Folio ranges head most spreads ("112-113") with section
/// headers further down. Headers are frequently lost or garbled to two-letter pairs or
/// dropped hundreds ("7" = 37, "1" = 141); those pins sit on the garbled header or body
/// start. Intrusive echo headers land mid-body when a section spans columns; the true
/// header follows the body end. S326 is absent from the scan. PDF pages 22-24 duplicate
/// pages 19-21 (scan artifact); S6 carries an explicit end so the duplicates fall in an
/// unclaimed gap and are dropped. Each placement was confirmed against the transcript
/// and, where ambiguous, the rendered PDF page.</para>
/// </summary>
public sealed class CreatureOfHavocParser : ManifestDungeonWorldParser
{
    public CreatureOfHavocParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "CreatureOfHavoc";
    protected override string TitleMatch => "Creature of Havoc";
    protected override string Slug => "ff24_creature_of_havoc";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff24.json";

    /// <summary>Cover blurb plus background; the rules pages are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };

    /// <summary>FF24 runs to 460 sections.</summary>
    private const int MaxSections = 460;
    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Written by Steve Jackson (print: p4 credits).</summary>
    protected override string Author => "Steve Jackson";

    /// <summary>FF24 prints sections scrambled, so bounds run pin-to-pin in the book.</summary>
    protected override bool ManifestInTranscriptOrder => true;

    /// <summary>FF24 keeps exit-fragment retention (exit numbers land on their own lines).</summary>
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
    /// confirmed against the arek sol23 solution chain or the rendered PDF page; anything
    /// unresolved is deliberately left verbatim rather than guessed.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Restored only when absent, so a future better scan needs no edit. Every edge below
        // is a consecutive sol23 pair (the player physically turned from parent to target) or a
        // print-verified option whose verb garble defeats the cleaner.
        (int parent, int target, string note)[] codeEdges =
        {
            (160, 292, "speech lost -> Knight (sol23: 160, 292)"),
            (446, 101, "packs tossed -> bodies (sol23: 446, 101)"),
            (147, 283, "Vapour gift -> code (sol23: 147, 283)"),
            (337, 283, "code translated -> gift (sol23: 337, 283)"),
            (125, 177, "Rosina coins -> crossroads (sol23: 125, 177)"),
            (215, 267, "Juja meal -> trail (sol23: 215, 267)"),
            (287, 235, "Toadmen death -> Grog (sol23: 287, 235)"),
            (28, 361, "club computed -> mirror (sol23: 28, 361)"),
            (222, 134, "witches fail -> village (sol23: 222, 134)"),
            (20, 40, "dead end -> room (sol23: 20, 40)"),
            (107, 55, "Coven trail -> Grog (sol23: 107, 55)"),
            (435, 62, "Rhino-Man door -> guardroom (sol23: 435, 62)"),
            (458, 110, "rock choice -> club (sol23: 458, 110)"),
            (177, 125, "copse hut -> Rosina (sol23: 177, 125)"),
            (90, 140, "mines slave -> ring (sol23: 90, 140)"),
            (92, 141, "dream witches -> forest (sol23: 92, 141)"),
            (7, 200, "human leave -> Hannicus (sol23: 7, 200)"),
            (267, 215, "trail plants -> Juja (sol23: 267, 215)"),
            (269, 219, "Whiteleaf woods -> Weaseltongue (sol23: 269, 219)"),
            (213, 233, "dead end -> pendant (sol23: 213, 233)"),
            (257, 237, "junction dark -> cavern (sol23: 257, 237)"),
            (307, 255, "bulrushes trail -> Grog (sol23: 307, 255)"),
            (422, 329, "mirror smash -> Marr (sol23: 422, 329)"),
        };
        foreach (var (parent, target, _) in codeEdges)
            if (sectionNumber == parent && !HasExit(content, target))
                content += $" (turn to {target}).";

        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair FIRST, so the text
            // matched here is already post-shared-repair. Plain string Replace is used (proven
            // in-pipeline); every target below was read verbatim from pipeline output and every
            // corrected number is sol23- or render-proven.
            257 => content.Replace("turn to 3049", "turn to 304",
                StringComparison.OrdinalIgnoreCase), // junction west (sol23: 257, 304)
            62 => content.Replace("Turn to 208.", "Turn to 298.",
                StringComparison.OrdinalIgnoreCase), // Rhino-Man Testing Grounds (sol23: 62, 298)
            208 => content.Replace("tum to 1223", "turn to 123",
                StringComparison.OrdinalIgnoreCase), // bridge east (sol23: 208, 123)
            _ => content,
        };
    }
}
