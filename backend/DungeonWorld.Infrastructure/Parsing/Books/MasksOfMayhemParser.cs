using System.Text.RegularExpressions;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF23 Masks of Mayhem (Robin Waterfield, 1986). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across PDF pages 13-105;
/// each PDF page is a two-up spread split into L/R halves). Sections print in numeric
/// order, so bounds run from one section number to the next. Intro uses the cover blurb (2)
/// plus BACKGROUND (12-13); the curated intro text ships in Intros/ff23.txt.
///
/// <para><b>Layout notes.</b> Folio ranges head most spreads ("112-113") with section
/// headers further down; single-number folios in the front matter collide with section
/// numbers and are excluded by page. Headers are frequently lost or garbled to two-letter
/// pairs ("Bs" = 85, "G3" = 81, "18g" = 185/189), dropped hundreds ("7" = 37, "1" = 141)
/// or dropped tens ("1" = 71, "4" = 74); those pins sit on the garbled header or body
/// start. Intrusive echo headers land mid-body when a section spans columns (S106/S107,
/// S178, S44/S45); the true header follows the body end. S34 and S222 are absent from
/// the scan. Each placement was confirmed against the transcript and, where ambiguous,
/// the rendered PDF page.</para>
/// </summary>
public sealed class MasksOfMayhemParser : ManifestDungeonWorldParser
{
    public MasksOfMayhemParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "MasksOfMayhem";
    protected override string TitleMatch => "Masks of Mayhem";
    protected override string Slug => "ff23_masks_of_mayhem";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff23.json";

    /// <summary>Cover blurb plus BACKGROUND; the rules pages are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 2, 12, 13 };

    /// <summary>FF23 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Written by Robin Waterfield (print: p3 credits).</summary>
    protected override string Author => "Robin Waterfield";

    /// <summary>FF23 keeps exit-fragment retention (exit numbers land on their own lines).</summary>
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
    /// confirmed against the arek sol22 solution chain or the rendered PDF page; anything
    /// unresolved is deliberately left verbatim rather than guessed.
    /// </summary>
    /// <summary>
    /// Turn-verb variants this scan produces, each seen in exit position before a clean
    /// section number. Applied only when the number has no trailing letter (garbled numbers
    /// like "18a" need per-section fixes, since blind verb repair would lock in a wrong
    /// target). Pronouns and prepositions ("him", "up", "on") are deliberately excluded.
    /// </summary>
    private static readonly Regex TurnVerbGarble = new(
        @"\b(turn|um|tom|urn|ham|torn|tum|fun|bum|tuen|lun|fur|tien|born|tam|tuorm|hirn|hin|frm|fumn|returm|hom|burn|form|fr|tirn|fwm|fum|hum|tuming|hun|lien|barn|bam|orn|luen|fm|han|firm|bom|hurn|fern|fam|lum|fim|fen)\s+(to|lo|io)\s+(\d{1,3})(?![\dA-Za-z])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Rewrites garbled turn verbs ("tum to 12", "turn lo 34") to clean "turn to N" so the
    /// reference extractor sees them. Runs before the per-section fixes so those can target
    /// post-normalization text; runs before the code-edge appends so <see cref="HasExit"/>
    /// observes the repaired verbs.
    /// </summary>
    public static string NormalizeTurnVerbs(string content) =>
        TurnVerbGarble.Replace(content, m =>
            char.IsUpper(m.Groups[1].Value[0])
                ? $"Turn to {m.Groups[3].Value}"
                : $"turn to {m.Groups[3].Value}");

    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Restored only when absent, so a future better scan needs no edit. Every edge below
        // is a consecutive sol22 pair (the player physically turned from parent to target) or a
        // print-verified option whose verb garble defeats the cleaner.
        content = NormalizeTurnVerbs(content);
        (int parent, int target, string note)[] codeEdges =
        {
            (231, 343, "leave area -> Fallow Dale (sol22: 231, 343)"),
            (289, 173, "ladder climb -> tribesmen (sol22: 289, 173)"),
            (289, 54, "ladder drop -> Luck (sol22: 289, 54)"),
            (282, 173, "tribesmen win -> snow-shoes (sol22: 282, 173)"),
            (59, 100, "Galrin call -> throne (sol22: 59, 100)"),
            (208, 88, "seeds dragons -> oaks (sol22: 208, 88)"),
            (151, 104, "Snake struggle -> pool (sol22: 151, 104)"),
            (301, 112, "tusk seeds -> items (sol22: 301, 112)"),
            (26, 149, "Hever otherwise -> test (sol22: 26, 149)"),
            (320, 74, "helmet lost -> lab (sol22: 320, 74)"),
            (258, 40, "suspicions -> traitor (sol22: 258, 40)"),
            (374, 126, "copse around -> oaks (sol22: 374, 126)"),
            (168, 114, "fire six -> canyon (sol22: 168, 114)"),
            (215, 167, "Juja else -> doors (sol22: 215, 167)"),
            (387, 22, "oaks computed -> Vashti (sol22: 387, 22)"),
            (22, 208, "Vashti page -> seeds (sol22: 22, 208)"),
            (202, 105, "ants north -> trail (sol22: 202, 105)"),
            (370, 151, "pool wade -> Snake (sol22: 370, 151)"),
            (320, 161, "helmet lost -> surgeon (sol22: 320, 161)"),
            (345, 165, "marsh mounds -> Elves (sol22: 345, 165)"),
            (200, 178, "Dale uneventful -> village (sol22: 200, 178)"),
            (123, 198, "snow pass -> Chion (sol22: 123, 198)"),
        };
        foreach (var (parent, target, _) in codeEdges)
            if (sectionNumber == parent && !HasExit(content, target))
                content += $" (turn to {target}).";

        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair FIRST, so the text
            // matched here is already post-shared-repair. Plain string Replace is used (proven
            // in-pipeline); every target below was read verbatim from pipeline output and every
            // corrected number is sol22- or render-proven.
            133 => content.Replace("turn to 1588", "turn to 158",
                StringComparison.OrdinalIgnoreCase), // rocks south (sol22: 133, 158)
            316 => content.Replace("turn to 2268", "turn to 226",
                StringComparison.OrdinalIgnoreCase), // sleep straight (sol22: 316, 226)
            291 => content.Replace("turn to 2435", "turn to 243",
                StringComparison.OrdinalIgnoreCase), // ant trail north (sol22: 291, 243)
            388 => content.Replace("turn to 3235", "turn to 325",
                StringComparison.OrdinalIgnoreCase), // side-passage (sol22: 388, 325)
            362 => content.Replace("turn to 81}", "turn to 181}",
                StringComparison.OrdinalIgnoreCase), // mine look (sol22: 362, 181)
            54 => content.Replace("tun tu 124", "turn to 124",
                StringComparison.OrdinalIgnoreCase), // Luck pass (sol22: 54, 124)
            92 => content.Replace("tum tn 233.", "turn to 231.",
                StringComparison.OrdinalIgnoreCase), // sceptre leave (sol22: 92, 231)
            132 => content.Replace("Tum tn zo.", "Turn to 20.",
                StringComparison.OrdinalIgnoreCase), // leave Dale (sol22: 132, 20)
            114 => content.Replace("Tum oy 10.", "Turn to 10.",
                StringComparison.OrdinalIgnoreCase), // canyon north (sol22: 114, 10)
            399 => content.Replace("turn to sa", "turn to 51")
                .Replace("hom fo 147", "turn to 147",
                StringComparison.OrdinalIgnoreCase), // tunnel spring (sol22: 399, 51/147)
            30 => content.Replace("Return to132", "turn to 112",
                StringComparison.OrdinalIgnoreCase), // Cholumbara (sol22: 30, 112)
            112 => content.Replace("None of these turn to 21", "None of these turn to 121",
                StringComparison.OrdinalIgnoreCase), // Doragar none (sol22: 112, 121)
            386 => content.Replace("lum to Bq", "turn to 84",
                StringComparison.OrdinalIgnoreCase), // Skeletons north (sol22: 386, 84)
            23 => content.Replace("turn to 163}", "turn to 164}",
                StringComparison.OrdinalIgnoreCase), // slap face (sol22: 23, 164)
            326 => content.Replace("Turn tor 44", "Turn to 44",
                StringComparison.OrdinalIgnoreCase), // garnet ring (sol22: 326, 44)
            44 => content.Replace("turn to 239", "turn to 249",
                StringComparison.OrdinalIgnoreCase), // village ask (sol22: 44, 249)
            202 => content.Replace("turn to 2),", "turn to 92),",
                StringComparison.OrdinalIgnoreCase), // ants wait (sol22: 202, 92)
            10 => content.Replace("turn to 10 7a", "turn to 70",
                StringComparison.OrdinalIgnoreCase), // canyon west (sol22: 10, 70)
            287 => content.Replace("tum ko 341", "turn to 341",
                StringComparison.OrdinalIgnoreCase), // figure call (sol22: 287, 341)
            40 => content.Replace("turn to 300.", "turn to 400.",
                StringComparison.OrdinalIgnoreCase), // Traitor win (sol22: 40, 400)
            293 => content.Replace("burn bo 38a", "turn to 380",
                StringComparison.OrdinalIgnoreCase), // Alligator dive (sol22: 293, 380)
            148 => content.Replace("turn to 208.", "turn to 206.",
                StringComparison.OrdinalIgnoreCase), // dice low (sol22: 148, 206)
            322 => content.Replace("turn to 209", "turn to 207",
                StringComparison.OrdinalIgnoreCase), // branches (sol22: 322, 207)
            171 => content.Replace("turn to 206.", "turn to 106.",
                StringComparison.OrdinalIgnoreCase), // Spriggans win (sol22: 171, 106)
            334 => content.Replace("turn to 13a", "turn to 130")
                .Replace("firm Lo 2857", "turn to 285",
                StringComparison.OrdinalIgnoreCase), // Elves break/taken (sol22: 334, 130/285)
            169 => content.Replace("Tum in 382.", "Turn to 382.",
                StringComparison.OrdinalIgnoreCase), // village dissolve (sol22: 169, 382)
            222 => content.Replace("turn to 132", "turn to 112",
                StringComparison.OrdinalIgnoreCase) // met Doragar -> fight on (render p62: 222, 112)
                .Replace("turn to21", "turn to 121",
                StringComparison.OrdinalIgnoreCase), // else -> Fallow Dale (render p62: 222, 121)
            223 => content.Replace("turn to 338", "turn to 336",
                StringComparison.OrdinalIgnoreCase), // Juja north (render p62: 223, 336)
            226 => content.Replace("turn to 357", "turn to 257",
                StringComparison.OrdinalIgnoreCase), // Lucky sleep -> Kraken (render p63: 226, 257)
            227 => content.Replace("Turn lo \u00a328.", "Turn to 128.",
                StringComparison.OrdinalIgnoreCase), // snow Provisions (render p63: 227, 128)
            52 => content.Replace("Tum to rag.", "Turn to 114.",
                StringComparison.OrdinalIgnoreCase), // fire-break (render p24: 52, 114)
            152 => content.Replace("Tum toag.", "Turn to 24.",
                StringComparison.OrdinalIgnoreCase), // healer leave (render p46: 152, 24)
            335 => content.Replace("tum ta Fz.", "turn to 72.",
                StringComparison.OrdinalIgnoreCase), // Hands win (render p89: 335, 72)
            390 => content.Replace("turn to a4", "turn to 94",
                StringComparison.OrdinalIgnoreCase) // run away (render p103: 390, 94)
                .Replace("turn to 31837", "turn to 318",
                StringComparison.OrdinalIgnoreCase), // fight invisible (render p103: 390, 318)
            29 => content.Replace("urn ioa73.", "turn to 173.",
                StringComparison.OrdinalIgnoreCase), // snow-shoes (render p19: 29, 173)
            175 => content.Replace("Turn \u00a30 343.", "Turn to 343.",
                StringComparison.OrdinalIgnoreCase), // horsemen (render p51: 175, 343)
            182 => content.Replace("Retum fo 142", "turn to 142",
                StringComparison.OrdinalIgnoreCase), // grazed -> fight on (render p53: 182, 142)
            190 => content.Replace("Tum bo", "Turn to 272.",
                StringComparison.OrdinalIgnoreCase), // bridge collapse (render p54: 190, 272)
            270 => content.Replace("Tum 10134.", "Turn to 154.",
                StringComparison.OrdinalIgnoreCase), // shaman ring (render p75: 270, 154)
            310 => content.Replace("Turn to 8a.", "Turn to 89.",
                StringComparison.OrdinalIgnoreCase), // helmet slope (render p83: 310, 89)
            375 => content.Replace("turn \u00a3013.", "turn to 13.",
                StringComparison.OrdinalIgnoreCase), // Mordida survive (render p99: 375, 13)
            392 => content.Replace("burn toa8z", "turn to 182",
                StringComparison.OrdinalIgnoreCase) // Lucky (render p103: 392, 182)
                .Replace("tun to bo", "turn to 60",
                StringComparison.OrdinalIgnoreCase), // Unlucky (render p103: 392, 60)
            85 => content.Replace("turn to veo", "turn to 100",
                StringComparison.OrdinalIgnoreCase), // Galrin summon (render p32: 85, 100)
            87 => content.Replace("turn to aga", "turn to 192",
                StringComparison.OrdinalIgnoreCase), // hands tied (render p32: 87, 192)
            98 => content.Replace("tum to 18a", "turn to 180",
                StringComparison.OrdinalIgnoreCase), // giant helmet (render p35: 98, 180)
            104 => content.Replace("turn toas.", "turn to 15.",
                StringComparison.OrdinalIgnoreCase), // STAMINA pass (render p36: 104, 15)
            131 => content.Replace("burn to 15371 fFitis", "turn to 153; if it is",
                StringComparison.OrdinalIgnoreCase), // snow pass (render p42: 131, 153)
            172 => content.Replace("tum\nvasttoags,", "turn to 141,",
                StringComparison.OrdinalIgnoreCase), // Doragar leave (render p51: 172, 141)
            180 => content.Replace("burn tira17", "turn to 117",
                StringComparison.OrdinalIgnoreCase), // rope bridge (render p52: 180, 117)
            218 => content.Replace("turn \u00a9 314", "turn to 319",
                StringComparison.OrdinalIgnoreCase), // hands tied (render p61: 218, 319)
            245 => content.Replace("tum tor 230", "turn to 230",
                StringComparison.OrdinalIgnoreCase), // lakeside camp (render p69: 245, 230)
            248 => content.Replace("turn {0 209", "turn to 209",
                StringComparison.OrdinalIgnoreCase), // crystal mirror (render p70: 248, 209)
            261 => content.Replace("tum to 7a", "turn to 74",
                StringComparison.OrdinalIgnoreCase), // throne crown (render p73: 261, 74)
            283 => content.Replace("travel east (tum lo", "travel east (turn to 388)?",
                StringComparison.OrdinalIgnoreCase), // mine east (render p78: 283, 388)
            303 => content.Replace("hurn to Bg", "turn to 89",
                StringComparison.OrdinalIgnoreCase), // camp-fire (render p81: 303, 89)
            338 => content.Replace("turn tr 238", "turn to 238",
                StringComparison.OrdinalIgnoreCase), // Morgana lair (render p90: 338, 238)
            356 => content.Replace("turn tn zsz", "turn to 252",
                StringComparison.OrdinalIgnoreCase), // thieves (render p95: 356, 252)
            383 => content.Replace("lum to\n1494.", "turn to 194.",
                StringComparison.OrdinalIgnoreCase), // tiger claws (render p101: 383, 194)
            385 => content.Replace("tum\ntor a7)?", "turn to 171)?",
                StringComparison.OrdinalIgnoreCase), // press on (render p101: 385, 171)
            _ => content,
        };
    }
}
