using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF07 Island of the Lizard King (Ian Livingstone, 1984). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across pages
/// 13-102; two-up L/R halves). Intro uses the cover/title pages (1, 3) plus the
/// BACKGROUND narrative (11-12); the curated intro text ships in Intros/ff07.txt.
/// Section exits below were adjudicated against the arek.bdmonkeys.net walkthrough
/// (sol6, full path 1..400 to victory): literal-print readings lose to arek's
/// continuous chain wherever they disagree. Shared TurnToRepair leaves tokens with
/// unmapped letters verbatim (a/e/y/q/x), so those fixes target the raw token;
/// fully-mapped tokens are fixed in their post-map form (og-&gt;09-&gt;9, Gg-&gt;99).
/// Open items (600 dpi re-OCR): S36 fire-sword branch "x1" (neutralized, would
/// otherwise strip to phantom "turn to 1"); S185 body lost in the p54R art gap
/// (S279 roll 1-2 dangles onto the empty slice); S213 right-branch and S240 exit
/// cut off mid-sentence (S241's header would merge into S240 as phantom 241).
/// </summary>
public sealed class IslandOfTheLizardKingParser : ManifestDungeonWorldParser
{
    public IslandOfTheLizardKingParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "IslandOfTheLizardKing";
    protected override string TitleMatch => "Island of the Lizard King";
    protected override string Slug => "ff07_island_of_the_lizard_king";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff07.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 3, 11, 12 };

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    // Joins a turn verb + preposition wrapped across a line break ("turn\nto agh",
    // "turn to\n22a") before the exit fixes below run: shared WrappedTurnToRegex only
    // joins wrapped refs with plain-numeric targets, so wrapped garbled tokens would
    // otherwise stay split and miss their single-space fix strings. Verb+prep only;
    // the token itself is untouched, and narrative joins ("turn\nto the door") parse
    // to nothing, so this is safe by construction.
    private static string JoinWrappedTurn(string content) =>
        System.Text.RegularExpressions.Regex.Replace(
            content,
            @"\b(turn|tum|fum|burn|tuen|urn)\s*\r?\n\s*(to|bo|te|o|lo)\b",
            "$1 $2",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        content = JoinWrappedTurn(content);
        // Cut tails whose dangling turn line would merge the NEXT section's header
        // into this body as a phantom reference (TrimContent join). Strip the merge
        // and mark the TODO; the true targets (S213 right-branch, S240 exit) need
        // 600 dpi recovery. Guarded by EndsWith so clean reprints are untouched.
        // All restores below ASSIGN (never return early) so the per-section switch
        // arms still run afterwards (e.g. S334 needs both its tail restore and its
        // hollow-target fix; S297 needs both its west-exit restore and boots fix).
        if (sectionNumber == 213 && content.TrimEnd().EndsWith("turn to 214", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"turn to 214".Length].TrimEnd()
                + " [right-branch target cut off in scan; TODO 600dpi p60].";
        if (sectionNumber == 86 && content.TrimEnd().EndsWith("eat them, turn", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"turn".Length] + "turn to 203.";
        if (sectionNumber == 193 && content.TrimEnd().EndsWith("(turn", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd() + " to 139).";
        if (sectionNumber == 223 && content.TrimEnd().EndsWith("turn to", StringComparison.OrdinalIgnoreCase))
            content += " 3.";
        if (sectionNumber == 244 && content.TrimEnd().EndsWith("(turn Lo", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"(turn Lo".Length] + "(turn to 400).";
        if (sectionNumber == 199)
            content = System.Text.RegularExpressions.Regex.Replace(content,
                @"turn to 3\s*\n\s*397\.",
                "turn to 397.",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (sectionNumber == 203 && content.TrimEnd().EndsWith("without a monkey, turn", StringComparison.OrdinalIgnoreCase))
            content += " to 36.";
        if (sectionNumber == 2 && content.TrimEnd().EndsWith("Unlucky, turn", StringComparison.OrdinalIgnoreCase))
            content += " to 326.";
        if (sectionNumber == 297 && content.TrimEnd().EndsWith("grassy plain", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 223).";
        if (sectionNumber == 334 && content.TrimEnd().EndsWith("turn ta", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"turn ta".Length] + "turn to 114,";
        if (sectionNumber == 359 && content.TrimEnd().EndsWith("turn to", StringComparison.OrdinalIgnoreCase))
            content += " 373.";
        if (sectionNumber == 368 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 147).";
        if (sectionNumber == 240 && content.TrimEnd().EndsWith("(turn to 241", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"(turn to 241".Length].TrimEnd()
                + " [exit cut off in scan (cross archway like S62, provisionally 139); TODO 600dpi p67].";
        // S36 fire-sword branch "turn to x1": shared repair strips the leading x to
        // phantom "turn to 1" (S1 boat), wrapped across the line break. No adjudicated
        // target exists, so neutralize the phantom instead of planting a false S36->S1 edge.
        if (sectionNumber == 36)
            content = System.Text.RegularExpressions.Regex.Replace(content,
                @"turn to 1,\s*If",
                "turn to [x1: section number illegible in scan; TODO 600dpi p20]. If",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        // Restored exit tails: short trailing fragments the transcript carries but
        // TrimContent drops as noise (low letter ratio / bare number), orphaning the
        // exit's head as a dangling verb. Strings verified against the 300 dpi dump
        // (FF06 Round-2 precedent). Guarded by EndsWith so clean reprints pass through.
        if (sectionNumber == 58 && content.TrimEnd().EndsWith("heading west, tum", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"tum".Length] + "turn to 37.";
        if (sectionNumber == 152 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 391).";
        if (sectionNumber == 200 && content.TrimEnd().EndsWith("(burn to", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"(burn to".Length] + "(turn to 391).";
        if (sectionNumber == 238 && content.TrimEnd().EndsWith("{turn", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"{turn".Length] + "(turn to 152).";
        if (sectionNumber == 292 && content.TrimEnd().EndsWith("(turn", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd() + " to 119).";
        if (sectionNumber == 371 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 57).";
        if (sectionNumber == 51 && content.TrimEnd().EndsWith("(tum to", StringComparison.OrdinalIgnoreCase))
            content += " 223),";
        if (sectionNumber == 32 && content.TrimEnd().EndsWith("(burn to", StringComparison.OrdinalIgnoreCase))
            content += " 201).";
        if (sectionNumber == 282 && content.TrimEnd().EndsWith("set off again, turn", StringComparison.OrdinalIgnoreCase))
            content += " to 27.";
        if (sectionNumber == 106 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 27g).";
        return sectionNumber switch
        {
            // Arek-proven chain fixes (sol6 visits both sides of each edge).
            4 => content.Replace("turn to 10", "turn to 101", StringComparison.OrdinalIgnoreCase), // T-junction left
            7 => content.Replace("urn to 317", "turn to 317", StringComparison.OrdinalIgnoreCase), // follow hopper
            26 => content.Replace("Tuen to og", "turn to 94", StringComparison.OrdinalIgnoreCase), // pouch boots (S259 prints 94)
            39 => content.Replace("turn to 20", "turn to 207", StringComparison.OrdinalIgnoreCase), // bitten -> poison kills Grannit
            63 => content.Replace("turn to 320", "turn to 329", StringComparison.OrdinalIgnoreCase), // guards (9->0 print garble)
            68 => content.Replace("Tum to Fo", "turn to 70", StringComparison.OrdinalIgnoreCase), // right branch
            166 => content.Replace("burn bo 318", "turn to 318", StringComparison.OrdinalIgnoreCase), // no-ring (burn+bo prep miss)
            193 => content.Replace("turn to 130", "turn to 139", StringComparison.OrdinalIgnoreCase), // password "What?" (0->9)
            208 => content.Replace("turn to 194", "turn to 199", StringComparison.OrdinalIgnoreCase), // feather -> Shaman listens
            213 => content.Replace("turn to 63", "turn to 68", StringComparison.OrdinalIgnoreCase), // junction left (3->6)
            223 => content.Replace("turn to 3-", "turn to 3", StringComparison.OrdinalIgnoreCase), // chamber -> lead Dwarfs deeper
            329 => content.Replace("turn to seg", "turn to 309", StringComparison.OrdinalIgnoreCase), // lucky charge (s->3,e->0,g->9)
            334 => content.Replace("turn to 143", "turn to 145", StringComparison.OrdinalIgnoreCase), // hollow -> mild bite (5->3)
            341 => content.Replace("turn to 1eg", "turn to 109", StringComparison.OrdinalIgnoreCase), // Horn -> blow it (e->0)
            352 => content.Replace("turn to 309", "turn to 399", StringComparison.OrdinalIgnoreCase), // flash -> sack (0->9)
            362 => content.Replace("turn to 104", "turn to 194", StringComparison.OrdinalIgnoreCase), // hill side (0->9)
            368 => content.Replace("turn to 47", "turn to 147", StringComparison.OrdinalIgnoreCase), // keys -> mines (dropped 1)
            384 => content.Replace("turn to goo", "turn to 400", StringComparison.OrdinalIgnoreCase), // slice -> victory
            391 => content.Replace("turn to 8a", "turn to 81", StringComparison.OrdinalIgnoreCase), // hack -> headhunters (a->1)
            // Content-proven fixes (arek skips these; parallel-section convergence).
            21 => content.Replace("turn to 22a", "turn to 222", StringComparison.OrdinalIgnoreCase), // west joins S26/S94/S134/S196
            46 => content.Replace("turn to 99", "turn to 69", StringComparison.OrdinalIgnoreCase), // threaten -> thief flees (G->6)
            73 => content.Replace("turn to zip", "turn to 217", StringComparison.OrdinalIgnoreCase), // non-sword-arm like S217
            77 => content.Replace("burn toga", "turn to 92", StringComparison.OrdinalIgnoreCase), // wait -> lab aftermath
            79 => content.Replace("Tuen to 17", "turn to 17", StringComparison.OrdinalIgnoreCase).Replace("Turn to gy", "Turn to 97", StringComparison.OrdinalIgnoreCase), // leave cave + swallow powder (y->7)
            83 => System.Text.RegularExpressions.Regex.Replace(content, @"turn\s+ter\s+334", "turn to 334", System.Text.RegularExpressions.RegexOptions.IgnoreCase), // lucky scree (te+r split, may wrap)
            85 => content.Replace("turn to 90", "turn to 60", StringComparison.OrdinalIgnoreCase), // dice 1-2 -> stop none (post-map form; G->6, cf. BG=66)
            86 => content.Replace("turn to 2035", "turn to 203", StringComparison.OrdinalIgnoreCase), // trailing-5 (S82 converges; join makes it single-line)
            92 => content.Replace("turn to 22a", "turn to 222", StringComparison.OrdinalIgnoreCase), // west joins the 222 plain
            93 => content.Replace("turn to zag", "turn to 214", StringComparison.OrdinalIgnoreCase).Replace("Turn to zz2o", "turn to 220", StringComparison.OrdinalIgnoreCase), // 3-tests hub + strength branch
            139 => content.Replace("turn to 99", "turn to 95", StringComparison.OrdinalIgnoreCase), // styrac win -> mutant (print 95 garbled gg; arek chain)
            192 => content.Replace("turn to 570", "turn to 57", StringComparison.OrdinalIgnoreCase), // trailing-0 junction
            206 => content.Replace("Turntoy", "turn to 7", StringComparison.OrdinalIgnoreCase), // leave west (y->7, cf. gy=97)
            243 => content.Replace("Turntoy", "turn to 7", StringComparison.OrdinalIgnoreCase), // leave west, same spaceless garble
            244 => content.Replace("turn Lo Joo", "turn to 400", StringComparison.OrdinalIgnoreCase), // skewer -> victory (mirrors 384)
            251 => content.Replace("turn to 20", "turn to 201", StringComparison.OrdinalIgnoreCase), // 63-followers join S32/S293
            295 => content.Replace("burn to gh", "turn to 96", StringComparison.OrdinalIgnoreCase), // duel win -> aftermath (h->6)
            297 => content.Replace("Turn to 99", "Turn to 94", StringComparison.OrdinalIgnoreCase), // ring boots are the red boots
            311 => content.Replace("Turn to gq", "Turn to 94", StringComparison.OrdinalIgnoreCase).Replace("furmn to 222", "turn to 222", StringComparison.OrdinalIgnoreCase), // clumsiness boots + west (furmn verb miss)
            317 => content.Replace("turn to agh", "turn to 296", StringComparison.OrdinalIgnoreCase), // alone west (a->2,g->9,h->6)
            327 => content.Replace("Turn to anh", "Turn to 206", StringComparison.OrdinalIgnoreCase), // give axe -> chant over it
            359 => content.Replace("turn to EYED", "turn to 373", StringComparison.OrdinalIgnoreCase), // darts -> tranquillized
            364 => content.Replace("turn to 1149", "turn to 114", StringComparison.OrdinalIgnoreCase), // trailing-9 gorge
            394 => content.Replace("turn to 19a", "turn to 191", StringComparison.OrdinalIgnoreCase), // bad throw -> miss (a->1)
            160 => System.Text.RegularExpressions.Regex.Replace(content, @"turn\s+tor\s+141", "turn to 141", System.Text.RegularExpressions.RegexOptions.IgnoreCase), // clean win (tor split, may wrap)
            50 => content.Replace("bern to 266", "turn to 266", StringComparison.OrdinalIgnoreCase), // door-shut (bern verb miss)
            138 => content.Replace("tusn to 11", "turn to 11", StringComparison.OrdinalIgnoreCase), // escape to Dwarfs (tusn verb miss)
            306 => content.Replace("burn Bo 263", "turn to 263", StringComparison.OrdinalIgnoreCase), // roll 6 cripple (burn+Bo prep miss)
            387 => content.Replace("burn tog", "turn to 9", StringComparison.OrdinalIgnoreCase), // iron bar (tog=to+g, cf. toga=to+92)
            57 => content.Replace("{to 361)", "{turn to 361)", StringComparison.OrdinalIgnoreCase).Replace("{to 19)", "{turn to 19)", StringComparison.OrdinalIgnoreCase), // paren exits lack verb
            397 => content.Replace("Turn to 79", "turn to 75", StringComparison.OrdinalIgnoreCase), // fear branch (print 75 garbled 7g; S59/108/255 use 79)
            _ => content,
        };
    }
}
