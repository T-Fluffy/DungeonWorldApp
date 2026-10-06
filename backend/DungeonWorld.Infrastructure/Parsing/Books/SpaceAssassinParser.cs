using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF12 Space Assassin (Andrew Chapman, 1985). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across pages
/// 13-114; two-up L/R halves). Intro uses the cover/title pages (1-2) plus the
/// MISSION BRIEFING (12); the curated intro text ships in Intros/ff12.txt.
/// Section exits adjudicated against the arek.bdmonkeys.net walkthrough
/// (sol11): all 128 path edges resolve, choices match references, no dangling
/// targets. Ghost slices (empty in ff12.json) carry sol11-restored exits;
/// dup-slot slices keep the foreign body verbatim. Misread headers restored
/// as S33/S139/S147/S169/S189/S191/S194/S218/S227/S251/S260/S269/S294/S306/
/// S309/S314/S319/S343/S382/S388/S389/S397; S123 keeps the Status-vehicle in
/// its slice. Known scan losses: 18 off-path sections stay unreachable
/// (lost bodies or parents, documented per precedent). Known sol11 errata:
/// the walkthrough mislabels S21's ball-bearings section as 29 (twice);
/// the graph routes 162->21->181 instead. S31 keeps the old-man cell text
/// with sol11's spider edge appended; S278's aid-branch reads 286, not 279.
/// </summary>
public sealed class SpaceAssassinParser : ManifestDungeonWorldParser
{
    public SpaceAssassinParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "SpaceAssassin";
    protected override string TitleMatch => "Space Assassin";
    protected override string Slug => "ff12_space_assassin";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff12.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 12 };

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Ghost sections whose slices resolve empty: restore the sol11-proven
        // exit so the graph connects. Fires only on empty content.
        if (string.IsNullOrWhiteSpace(content))
        {
            return sectionNumber switch
            {
                16 => "(turn to 381).",   // rack -> vehicle controls (sol11)
                52 => "(turn to 14). [Also turn to 332.]", // bomb collapses; floor tiles (sol11 52->332)
                190 => "(turn to 52).",  // gravity bomb into maw (sol11)
                77 => "(turn to 103).",  // open the hatch (sol11)
                63 => "(turn to 134).",  // leave via tunnel (sol11)
                175 => "(turn to 266).",  // open side hatch (sol11)
                344 => "(turn to 254).",  // leave via other exit (sol11)
                296 => "(turn to 187).",  // convince the chief (sol11)
                187 => "(turn to 368).",  // pieces plug together (sol11)
                182 => "(turn to 309).",  // surface from submarine (sol11)
                31 => "(turn to 107).",  // communicate with spider (sol11)
                219 => "(turn to 395).",  // enter aluminum cube (sol11)
                291 => "(turn to 363).",  // tell assassin (sol11)
                70 => "(turn to 184).",  // overcome guards (sol11)
                293 => "(turn to 109).",  // leave security exit (sol11)
                109 => "(turn to 185).",  // cross room by path (sol11)
                255 => "(turn to 131).",  // stride across floor (sol11)
                280 => "(turn to 262).",  // grab him (sol11)
                262 => "(turn to 370).",  // go for him (sol11)
                394 => "(turn to 373).",  // run to exit (sol11)
                331 => "(turn to 53).",  // move east to point C (sol11)
                53 => "(turn to 66).",  // vehicle at C facing south (sol11)
                29 => "(turn to 12).",  // attack old man -> portal (sol11; ball-bearings belong to S21)
                51 => "(turn to 29).",  // RED DRAGON win -> old man (sol11; fight text lost)
                85 => "(turn to 182).",  // defeat BIVALVE -> surface (sol11)
                _ => content,
            };
        }
        // Truncated tails: the slice ends mid-exit (number lives in the next
        // slice), so complete it. Suffix-checked, never fires on whole text.
        // (FF12 tails verified from CleanedData; more added as diffs dictate.)
        if (sectionNumber == 164 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 274).";             // revive second capsule (dump p52)
        if (sectionNumber == 151 && content.TrimEnd().EndsWith("Turn to", StringComparison.OrdinalIgnoreCase))
            content += " 208).";             // go through other door (sol11)
        if (sectionNumber == 148 && content.TrimEnd().EndsWith("(furn to", StringComparison.OrdinalIgnoreCase))
            content += " 251).";             // steal device -> natives fuss (dump p49)
        if (sectionNumber == 151 && content.TrimEnd().EndsWith("Turn to", StringComparison.OrdinalIgnoreCase))
            content += " 208).";             // go through other door (sol11)
        if (sectionNumber == 148 && content.TrimEnd().EndsWith("(furn to", StringComparison.OrdinalIgnoreCase))
            content += " 251).";             // steal device -> natives fuss (dump p49)
        // True exits missing from the slice (cut choices / computed mechanics).
        if (sectionNumber == 320 && !content.Contains("turn to 310", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 310).";    // in position -> look for secret doors (sol11)
        if (sectionNumber == 321 && !content.Contains("turn to 88", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 88).";     // noted Mordana reference -> secret rooms (sol11)
        if (sectionNumber == 240 && !content.Contains("turn to 394", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 394).";    // use Anti-Mollusk -> run to exit (sol11)
        if (sectionNumber == 373 && !content.Contains("turn to 295", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 295).";    // look for other means -> employ book (sol11; keeps textual 293)
        if (sectionNumber == 326 && !content.Contains("turn to 15", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 15). [Also turn to 383.]"; // Zark answer/fight (S327 pattern; sol11 326->15)
        if (sectionNumber == 15 && !System.Text.RegularExpressions.Regex.IsMatch(content, @"turn to 140\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            content += " (turn to 140).";    // computed letter answer -> center door (sol11; \b avoids matching 1400s)
        if (sectionNumber == 14 && !content.Contains("turn to 381", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 381).";    // back for left door -> controls (sol11)
        if (sectionNumber == 66 && !content.Contains("turn to 365", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 365).";    // move ahead west to point B (sol11)
        if (sectionNumber == 176 && !content.Contains("turn to 365", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 365).";    // move ahead west to point B (sol11)
        if (sectionNumber == 332 && !content.Contains("turn to 255", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 255).";    // stride across tiles (sol11)
        if (sectionNumber == 181 && !content.Contains("turn to 200", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 200).";    // go back down left-hand path (sol11)
        if (sectionNumber == 31 && !content.Contains("turn to 107", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 107).";    // communicate with spider (sol11; p31 cell belongs elsewhere)
        if (sectionNumber == 181 && !content.Contains("turn to 200", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 200).";    // go back down left-hand path (sol11)
        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair
            // FIRST, so targets below are post-shared text.
            78 => content.Replace("burn bo 158", "turn to 158", StringComparison.OrdinalIgnoreCase), // east into hills (dump p34)
            95 => content.Replace("tum to goo", "turn to 400", StringComparison.OrdinalIgnoreCase), // defeat Cyrus (sol11; goo->400)
            117 => content.Replace("turn to 3a", "turn to 30", StringComparison.OrdinalIgnoreCase), // cylinder examined (dump p42)
            120 => content.Replace("Tumto1sy", "turn to 189", StringComparison.OrdinalIgnoreCase), // countdown escape (dump p43)
            121 => content.Replace("turn tor 159", "turn to 159", StringComparison.OrdinalIgnoreCase), // north into forest (dump p43)
            140 => content.Replace("turn to 38237", "turn to 382", StringComparison.OrdinalIgnoreCase), // left door (dump p47; stray digits)
            152 => content.Replace("turn to gs", "turn to 69", StringComparison.OrdinalIgnoreCase), // neither item -> sleep capsule (early-ship theme; S95 has its 114-parent)
            153 => content.Replace("fum to zz27", "turn to 227", StringComparison.OrdinalIgnoreCase), // southern chasm side (dump p50)
            158 => content.Replace("(buen bo 25)", "(turn to 25)", StringComparison.OrdinalIgnoreCase)
                .Replace("turn to 21417", "turn to 214", StringComparison.OrdinalIgnoreCase), // hills environs (dump p50; stray digits)
            164 => content.Replace("buen to 33", "turn to 31", StringComparison.OrdinalIgnoreCase), // revive first capsule (sol11 164->31; 33->31)
            162 => content.Replace("Turn to 29", "Turn to 21", StringComparison.OrdinalIgnoreCase) // plastic cylinder -> ball-bearings info (S21; "29" misread "21")
                .Replace("Turn to 1085", "Turn to 108", StringComparison.OrdinalIgnoreCase), // red-heart packet (stray digit; FF09 precedent)
            181 => content.Replace("turn back to 162", "turn to 162", StringComparison.OrdinalIgnoreCase), // second item lookup (sol11 181->162)
            167 => content.Replace("burn fo 66", "turn to 66", StringComparison.OrdinalIgnoreCase), // Status-B-west exposure (dump p53)
            201 => content.Replace("turn to Jog", "turn to 209", StringComparison.OrdinalIgnoreCase), // investigate door (dump p60; J->2)
            303 => content.Replace("turn ter 249", "turn to 249", StringComparison.OrdinalIgnoreCase), // standard door (dump p84)
            322 => content.Replace("burn fo 160", "turn to 160", StringComparison.OrdinalIgnoreCase), // south river (dump p89)
            376 => content.Replace("harn to 354", "turn to 354", StringComparison.OrdinalIgnoreCase), // blue safe button (dump p103)
            397 => content.Replace("fun to 121", "turn to 121", StringComparison.OrdinalIgnoreCase), // south to forest (dump p110)
            253 => content.Replace("bern to 288", "turn to 288", StringComparison.OrdinalIgnoreCase)
                .Replace("iturn to 28g", "turn to 289", StringComparison.OrdinalIgnoreCase), // river compass (dump p72)
            257 => content.Replace("bum to", "turn to", StringComparison.OrdinalIgnoreCase), // stay and eat (sol11 257->293; newline variant)
            // Stray-digit merges shared across overlapping slices (FF09 precedent: drop last digit).
            33 => content.Replace("1085", "108", StringComparison.OrdinalIgnoreCase)
                .Replace("3537", "353", StringComparison.OrdinalIgnoreCase),
            36 => content.Replace("1085", "108", StringComparison.OrdinalIgnoreCase)
                .Replace("3537", "353", StringComparison.OrdinalIgnoreCase),
            135 => content.Replace("1085", "108", StringComparison.OrdinalIgnoreCase),
            146 => content.Replace("1085", "108", StringComparison.OrdinalIgnoreCase),
            327 => content.Replace("Turn too.", "Turn to 70.", StringComparison.OrdinalIgnoreCase), // commuter zip back (sol11 327->70; shared leaves "too")
            281 => content.Replace("turn to 3537", "turn to 353", StringComparison.OrdinalIgnoreCase), // other tunnel (stray digit; FF09 precedent)
            395 => content.Replace("turn to 29a", "turn to 291", StringComparison.OrdinalIgnoreCase), // tell assassin (sol11; a->1)
            243 => content.Replace("turn to2gy", "turn to 297", StringComparison.OrdinalIgnoreCase), // Lucky test (sol11; g->9 y->7)
            284 => content.Replace("Tuer to 356", "Turn to 356", StringComparison.OrdinalIgnoreCase), // leave through other door (sol11)
            285 => content.Replace("urn to 321", "turn to 321", StringComparison.OrdinalIgnoreCase), // choose explosives (sol11; urn not a shared verb)
            _ => content,
        };
    }
}
