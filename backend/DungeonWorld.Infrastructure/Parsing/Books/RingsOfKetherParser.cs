using System.Text.RegularExpressions;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF15 Rings of Kether (Andrew Chapman, 1985). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across pages
/// 11-120; wide pages split into two-up L/R halves, front matter on narrow
/// single-column pages). Intro uses the cover/title pages (1-2) plus the
/// MISSION BRIEFING (10); the curated intro text ships in Intros/ff15.txt.
/// Section exits adjudicated against the arek.bdmonkeys.net walkthrough
/// (sol14). Ghost slices (empty in ff15.json) carry sol14-restored exits;
/// dup-slot and range-header slices keep the foreign body verbatim.
/// Misread headers restored via MANUAL walk entries (see ff15_walk.py);
/// S4/S6 (bar-miner/Fed-ID) unattributable: ghosts with bodies preserved
/// in S3/S5 slices. Known inversions: S82/S83 (S82 empty, accepted),
/// S260/S259 (end-capped), S280/S281 (S280 empty, S281 end-capped).
/// </summary>
public sealed class RingsOfKetherParser : ManifestDungeonWorldParser
{
    public RingsOfKetherParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "RingsOfKether";
    protected override string TitleMatch => "Rings of Kether";
    protected override string Slug => "ff15_rings_of_kether";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff15.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 10 };

    /// <summary>FF15 runs to 400 sections.</summary>
    private const int MaxSections = 400;

    /// <summary>
    /// FF15 opts into exit-fragment retention and the isolated exit repair, but deliberately
    /// <em>not</em> into <see cref="UseHardScanOcr"/>: the hardened second OCR pass was measured
    /// on this book and gained 38 reachable sections while losing 19 that were already reachable,
    /// because merging the transcript breaks the "dangling turn-to + orphaned number fragment"
    /// pattern several of the sol14 fixes below depend on. Fragment retention is additive and
    /// cost nothing, so it is kept; the pass that rewrote lines is not.
    /// </summary>
    protected override bool KeepExitFragments => true;

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    /// <summary>FF15-scoped exit repair, bounded to this book's 400 sections.</summary>
    protected override string RepairExits(int sectionNumber, string content) => RepairExits(content);

    /// <summary>Exposed for tests.</summary>
    public static string RepairExits(string content) =>
        HardScanExitRepair.Repair(content, MaxSections);

    /// <summary>
    /// True when <paramref name="content"/> already carries a real exit to <paramref name="target"/>.
    /// The trailing (?!\d) matters: this scan reads a stray extra digit ("turn to 3999"), so a
    /// substring check would wrongly conclude the exit is already present and skip the restoration.
    /// </summary>
    private static bool HasExit(string content, int target) =>
        Regex.IsMatch(content, $@"\bturn\s+to\s+{target}(?!\d)", RegexOptions.IgnoreCase);

    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Ghost sections whose slices resolve empty: restore the sol14-proven
        // exit so the graph connects. Fires only on empty content.
if (string.IsNullOrWhiteSpace(content))
        {
            return sectionNumber switch
            {
                39 => "(turn to 341).",   // dive past it -> head to the exit (sol14)
                181 => "(turn to 376).",  // Murkegg handshake -> handle/door (sol14; body lost)
                221 => "(turn to 320).",  // Isosceles Tower located -> wood-paneled wall (sol14)
                255 => "(turn to 297).",  // proctor takes you -> restaurant (sol14)
                265 => "(turn to 70).",   // woman unaware -> watch apartment (sol14)
                70 => "(turn to 51).",    // watch apartment -> follow Arthur (sol14; body lost)
                245 => "(turn to 343).",  // eye on the woman -> follow her (sol14)
                249 => "(turn to 210).",  // aid Mrs Torus -> new clue (sol14)
                244 => "(turn to 283).",  // other exit -> corridor (sol14)
                243 => "(turn to 126).",  // dazed -> production coords (sol14)
                242 => "(turn to 125).",  // vidilink goldmine -> coordinates (sol14)
                268 => "(turn to 337).",  // sit with Keeper -> visit Customs (sol14)
                80 => "(turn to 41).",    // old vidinews -> Babbet address (sol14; body lost)
                256 => "(turn to 217).",  // run-down warehouse -> outside (sol14; body lost)
                288 => "(turn to 249).",  // sniper caught -> aid Mrs Torus (sol14; body lost)
                399 => "(turn to 260).",  // jets half-way -> reach satellite (sol14; body in S260 slice)
                49 => "(turn to 352).",   // duel win -> left junction (sol14; body lost)
                186 => "(turn to 381).",  // left button -> alternative (sol14; body lost)
                _ => content,
            };
        }
        // "Return to N" is a true exit with an unparsed verb (FF14 precedent).
        content = content.Replace("Return to 26", "turn to 26", StringComparison.OrdinalIgnoreCase); // loop-back room (S40)
        // True exits missing from the slice (cut choices). Sol14-grounded appends.
        if (sectionNumber == 73 && !content.Contains("turn to 229", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 229).";    // vent route -> push out the grille (sol14)
        if (sectionNumber == 341 && !content.Contains("turn to 185", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 185).";    // crossroads: left tunnel (sol14)
        if (sectionNumber == 328 && !content.Contains("turn to 289", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 289).";    // give up and head for Kether (sol14)
        if (sectionNumber == 350 && !content.Contains("turn to 389", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 389).";    // leave the room -> corridor (sol14)
        if (sectionNumber == 126 && !content.Contains("turn to 87", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 87).";     // take out the production (sol14)
        if (sectionNumber == 380 && !content.Contains("turn to 400", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 400).";    // Malbordus dead -> victory (sol14)
        if (sectionNumber == 260 && !content.Contains("turn to 221", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 221).";    // reach satellite -> tower located (sol14; same-text flow)
        if (sectionNumber == 205 && !content.Contains("turn to 166", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 166).";    // back through laboratory (sol14; lab branch cut)
        if (sectionNumber == 3 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 345).";             // starport inquiries (number dropped as noise)
        if (sectionNumber == 3 && !content.Contains("turn to 354", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 354).";    // explore warehouse (dump p12R[46] "354)" dropped as noise; sol14)
        if (sectionNumber == 292 && content.TrimEnd().EndsWith("Turn", StringComparison.OrdinalIgnoreCase))
            content += " to 28).";           // dismount (number dropped as noise; sol14)
        if (sectionNumber == 313 && content.TrimEnd().EndsWith("Turn", StringComparison.OrdinalIgnoreCase))
            content += " to 186).";          // left door button (sol14; exit dropped as noise)
        if (sectionNumber == 395 && !content.Contains("turn to 236", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 236).";    // force the room (sol14; slice starts late)
        if (sectionNumber == 395 && !content.Contains("turn to 295", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 295).";    // follow the man (slice starts late)
        // Truncated tails: the slice ends mid-exit, so complete it.
        // Suffix-checked per section, never fires on whole text.
        if (sectionNumber == 213 && content.TrimEnd().EndsWith(", turn", StringComparison.OrdinalIgnoreCase))
            content += " to 146).";          // haggle with the captain (sol14)
        if (sectionNumber == 166 && content.TrimEnd().EndsWith("(turn", StringComparison.OrdinalIgnoreCase))
            content += " to 238).";          // step aboard the ship (sol14)
        if (sectionNumber == 21 && content.TrimEnd().EndsWith("Tum", StringComparison.OrdinalIgnoreCase))
            content += " to 46).";           // walk directly ahead (sol14)
        if (sectionNumber == 361 && content.TrimEnd().EndsWith("Tum", StringComparison.OrdinalIgnoreCase))
            content += " to 340).";          // run past the Eye Stinger (sol14)
        if (sectionNumber == 219 && content.TrimEnd().EndsWith("turn", StringComparison.OrdinalIgnoreCase))
            content += " to 137).";          // run after Leesha (sol14)
        if (sectionNumber == 171 && content.TrimEnd().EndsWith("Tum", StringComparison.OrdinalIgnoreCase))
            content += " to 314).";          // run down the passageway (sol14)
        if (sectionNumber == 229 && content.TrimEnd().EndsWith("(tum to", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"(tum to".Length] + "(turn to 190)."; // continue through air-con (dump p74R[39])
        if (sectionNumber == 290 && content.TrimEnd().EndsWith("(fum fo", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"(fum fo".Length] + "(turn to 271)."; // shoulders at speed (dump + S271 identity)
        if (sectionNumber == 281 && !content.Contains("turn to 301", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 301).";    // lesser entrances (text-true "or 301")
        if (sectionNumber == 281 && content.TrimEnd().EndsWith("{turn to", StringComparison.OrdinalIgnoreCase))
            content += " 67).";              // freight entrance (dump p85R[26])
        if (sectionNumber == 275 && !content.Contains("turn to 164", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 164).";    // press on south (dump p79R[25])
        if (sectionNumber == 358 && !content.Contains("turn to 237", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 237).";    // continue without eating (dump p97L[13])
        if (sectionNumber == 53 && content.TrimEnd().EndsWith("Turn to", StringComparison.OrdinalIgnoreCase))
            content += " 316).";             // lead out back (dump p26R[55] "3168." dropped as noise)
        if (sectionNumber == 227 && content.TrimEnd().EndsWith("{tum to", StringComparison.OrdinalIgnoreCase))
            content += " 384).";             // space industry -> starport (number dropped as noise)
        if (sectionNumber == 300 && content.TrimEnd().EndsWith("tum to", StringComparison.OrdinalIgnoreCase))
            content += " 359).";             // survive smash (dump "359-" dropped as noise)
        if (sectionNumber == 87 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 19).";              // manoeuvre through -> made it safely (sol14)
        if (sectionNumber == 283 && content.TrimEnd().EndsWith("[turn to", StringComparison.OrdinalIgnoreCase))
            content += " 205).";             // corridor -> laboratory (sol14)
        // Restorations for edges the committed golden graph proves are real but that the hard-scan
        // pass no longer yields: S53's target is destroyed by the scan (rendered out-of-range and
        // neutralised), and in S278/S377 exit-fragment retention keeps the number on its own line
        // so it no longer merges into the preceding dangling "turn to".
        // Confirmed against ff15_rings_of_kether.refs.json; sol14 cross-checks where available.
        if (sectionNumber == 53 && !HasExit(content, 316))
            content += " (turn to 316).";   // elevator ambush -> escape (golden; target unreadable)
        if (sectionNumber == 278 && !HasExit(content, 282))
            content += " (turn to 282).";   // manor hub -> side branch (golden)
        if (sectionNumber == 278 && !HasExit(content, 287))
            content += " (turn to 287).";   // manor hub -> side branch (golden)
        if (sectionNumber == 377 && !HasExit(content, 399))
            content += " (turn to 399).";   // space-walk -> satellite (golden)
        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair
            // FIRST, so targets below are post-shared text.
            51 => content.Replace("arn to 2", "turn to 2", StringComparison.OrdinalIgnoreCase), // follow Arthur (sol14)
            11 => content.Replace("lun to 206", "turn to 206", StringComparison.OrdinalIgnoreCase) // heliport questions
                .Replace("fun to 284", "turn to 284", StringComparison.OrdinalIgnoreCase), // meet navigator (sol14)
            3 => content.Replace("turn Lo 354)", "turn to 354)", StringComparison.OrdinalIgnoreCase), // explore warehouse (sol14)
            2 => content.Replace("fur to 395", "turn to 395", StringComparison.OrdinalIgnoreCase), // meet at hotel (sol14)
            395 => content.Replace("furn to 236", "turn to 236", StringComparison.OrdinalIgnoreCase) // force the room (sol14)
                .Replace("turn to 2g95", "turn to 295", StringComparison.OrdinalIgnoreCase) // follow the man
                .Replace("turn to B", "turn to 8", StringComparison.OrdinalIgnoreCase) // car leap -> crash
                .Replace("tum to 57", "turn to 57", StringComparison.OrdinalIgnoreCase), // leap cars
            236 => content.Replace("hum to 158", "turn to 158", StringComparison.OrdinalIgnoreCase), // force way in (sol14)
            41 => content.Replace("iurn tn 256", "turn to 256", StringComparison.OrdinalIgnoreCase) // look over address (sol14)
                .Replace("tum to 23617", "turn to 236", StringComparison.OrdinalIgnoreCase), // computer centre
            217 => content.Replace("turn to 288)7", "turn to 188)", StringComparison.OrdinalIgnoreCase), // outside -> right door (sol14; S288 independently parented)
            188 => content.Replace("the left (146)", "the left (turn to 146)", StringComparison.OrdinalIgnoreCase)
                .Replace("the night (3)", "the night (turn to 3)", StringComparison.OrdinalIgnoreCase), // warehouse doors (sol14 takes 3)
            354 => content.Replace("turn to Loo", "turn to 100", StringComparison.OrdinalIgnoreCase), // do as gunman says (sol14)
            384 => content.Replace("burn tor 348", "turn to 345", StringComparison.OrdinalIgnoreCase) // starport inquiries -> admin block (sol14; "348" misread)
                .Replace("bom to 152", "turn to 152", StringComparison.OrdinalIgnoreCase), // city inquiries
            345 => content.Replace("(turn to 346", "(turn to 150)", StringComparison.OrdinalIgnoreCase) // admin block -> hangars (sol14; header bleed)
                .Replace("turn £0 308", "turn to 308", StringComparison.OrdinalIgnoreCase), // hangars branch
            228 => content.Replace("mm to 13g", "turn to 189", StringComparison.OrdinalIgnoreCase), // verify info -> confronted (sol14; "13g" double misread)
            238 => content.Replace("hum io 19g", "turn to 199", StringComparison.OrdinalIgnoreCase), // double back -> roof (sol14)
            326 => content.Replace("furmn fo", "turn to", StringComparison.OrdinalIgnoreCase), // set-up meeting (sol14; split-line robust)
            44 => content.Replace("turn to 135", "turn to 15", StringComparison.OrdinalIgnoreCase) // grounds -> search reveals (sol14; "135" misread)
                .Replace("(fun to 376)7", "(turn to 376)", StringComparison.OrdinalIgnoreCase), // meet her
            327 => content.Replace("burn fo 288", "turn to 288", StringComparison.OrdinalIgnoreCase), // take out the sniper (sol14)
            132 => content.Replace("horn Bo 54", "turn to 54", StringComparison.OrdinalIgnoreCase), // speak -> shout question (sol14)
            54 => content.Replace("turn ti 356", "turn to 356", StringComparison.OrdinalIgnoreCase), // shout question (sol14)
            337 => content.Replace("fur to 308", "turn to 308", StringComparison.OrdinalIgnoreCase) // hide in Customs (sol14)
                .Replace("fun to142", "turn to 142", StringComparison.OrdinalIgnoreCase), // threaten for info
            308 => content.Replace("tum fu 264", "turn to 264", StringComparison.OrdinalIgnoreCase) // arrivals block
                .Replace("furm to 231", "turn to 211", StringComparison.OrdinalIgnoreCase), // freight depot -> lockers (sol14; "231" misread)
            211 => content.Replace("{turn to 83)7", "{turn to 84)", StringComparison.OrdinalIgnoreCase), // stay -> guard strolls off (sol14; "83" misread)
            320 => content.Replace("turn to 332", "turn to 331", StringComparison.OrdinalIgnoreCase), // left wall -> files (sol14; "332" misread)
            45 => content.Replace("(urn to 320)", "(turn to 320)", StringComparison.OrdinalIgnoreCase) // Isosceles Tower
                .Replace("(fur to 36)", "(turn to 36)", StringComparison.OrdinalIgnoreCase), // satellite (sol14)
            377 => content.Replace("rl ee a", "to 399", StringComparison.OrdinalIgnoreCase), // space-walk -> jets half-way (sol14; split-line robust)
            125 => content.Replace("Tam to 330.", "Turn to 330.", StringComparison.OrdinalIgnoreCase), // island coords (sol14)
            292 => content.Replace("in 28.", "to 28.", StringComparison.OrdinalIgnoreCase), // dismount (sol14; split-line form)
            370 => content.Replace("Tom to 312.", "Turn to 312.", StringComparison.OrdinalIgnoreCase), // through minefield (sol14)
            332 => content.Replace("form to 293", "turn to 293", StringComparison.OrdinalIgnoreCase) // tunnel continuation (sol14)
                .Replace("ium to 146", "turn to 146", StringComparison.OrdinalIgnoreCase), // right passage
            293 => content.Replace("furm to 137", "turn to 137", StringComparison.OrdinalIgnoreCase), // jump spheres (sol14)
            137 => content.Replace("SKILL, turn to 9.", "SKILL, turn to 59.", StringComparison.OrdinalIgnoreCase), // second sphere (sol14; truncated digit)
            127 => content.Replace("Torn to gg.", "Turn to 49.", StringComparison.OrdinalIgnoreCase), // left at junction (sol14; "gg" double misread)
            313 => content.Replace("Turn tor 186.", "Turn to 186.", StringComparison.OrdinalIgnoreCase), // left door button (sol14)
            381 => content.Replace("um to 137", "turn to 147", StringComparison.OrdinalIgnoreCase), // alternative -> Babbet (sol14; "137" misread)
            147 => content.Replace("tum to goo.", "turn to 400.", StringComparison.OrdinalIgnoreCase), // tackle Babbet -> victory (sol14)
            55 => content.Replace("turn to 538", "turn to 53", StringComparison.OrdinalIgnoreCase), // bribe backfires (stray digit)
            76 => content.Replace("(fom fo", "(turn to 27).", StringComparison.OrdinalIgnoreCase), // ram from behind -> ram scene (dump + narrative)
            191 => content.Replace("(tum to", "(turn to 113).", StringComparison.OrdinalIgnoreCase), // flag down -> flag car (dump + narrative)
            227 => content.Replace("tum to 384", "turn to 384", StringComparison.OrdinalIgnoreCase), // space industry -> starport
            364 => content.Replace("fom fo 384", "turn to 384", StringComparison.OrdinalIgnoreCase), // verify source -> starport
            300 => content.Replace("tum to 359-", "turn to 359", StringComparison.OrdinalIgnoreCase), // survive smash (strip dash)
            310 => content.Replace("(burn to", "(turn to 252).", StringComparison.OrdinalIgnoreCase), // side-swipe -> pushed (dump + narrative)
            348 => content.Replace("hum to 309", "turn to 309", StringComparison.OrdinalIgnoreCase) // crest at speed
                .Replace("(hum to", "(turn to 163).", StringComparison.OrdinalIgnoreCase), // slow for corner (dump; end-anchored by content)
            23 => content.Replace("turn to 526", "turn to 52", StringComparison.OrdinalIgnoreCase), // investigate smoke (stray digit)
            343 => content.Replace("turn to 269", "turn to 265", StringComparison.OrdinalIgnoreCase), // follow woman -> unaware (sol14; "269" misread)
            229 => content.Replace("fun to 171", "turn to 171", StringComparison.OrdinalIgnoreCase), // push out grille (sol14)
            230 => content.Replace("{hm fo 298)", "{turn to 298}", StringComparison.OrdinalIgnoreCase), // bribe Customs
            135 => content.Replace("urn to 359", "turn to 359", StringComparison.OrdinalIgnoreCase), // ram sends off road
            388 => content.Replace("Tum tr 37", "Turn to 37", StringComparison.OrdinalIgnoreCase), // steady distance -> ram chance
            371 => content.Replace("fur 0 203", "turn to 203", StringComparison.OrdinalIgnoreCase), // tunnel continuation
            342 => content.Replace("Feturn Lo 186.", "turn to 186.", StringComparison.OrdinalIgnoreCase), // X-ray lock
            340 => content.Replace("urn lo 253", "turn to 253", StringComparison.OrdinalIgnoreCase), // passageway
            346 => content.Replace("lun kr 307", "turn to 307", StringComparison.OrdinalIgnoreCase), // draw pistol
            202 => content.Replace("bum to 163", "turn to 163", StringComparison.OrdinalIgnoreCase), // anticipate corner
            356 => content.Replace("turn tor 54", "turn to 54", StringComparison.OrdinalIgnoreCase), // speak with beast
            360 => content.Replace("furn fo 282", "turn to 282", StringComparison.OrdinalIgnoreCase), // hand-to-hand with Zera
            361 => content.Replace("fum fo 244", "turn to 244", StringComparison.OrdinalIgnoreCase), // other exit
            59 => content.Replace("Turn £0 44", "Turn to 44", StringComparison.OrdinalIgnoreCase), // meet Mrs Torus
            166 => content.Replace("Turn to 38x", "Turn to 388", StringComparison.OrdinalIgnoreCase), // Pterodactyl wins (x->8)
            380 => content.Replace("fom to 224", "turn to 224", StringComparison.OrdinalIgnoreCase), // shock exit
            _ => content,
        };
    }
}
