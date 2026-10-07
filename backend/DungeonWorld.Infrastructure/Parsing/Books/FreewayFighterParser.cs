using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF13 Freeway Fighter (Ian Livingstone, 1985). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 380 sections across pages
/// 6-103; wide pages split into two-up L/R halves). Intro uses the cover/title
/// pages (1-2) plus the VID NEWS BULLETIN and BACKGROUND (14-15); the curated
/// intro text ships in Intros/ff13.txt. Section exits adjudicated against the
/// arek.bdmonkeys.net walkthrough (sol12). Ghost slices (empty in ff13.json)
/// carry sol12-restored exits; off-path ghosts with lost bodies stay empty
/// (documented, never guessed).
/// </summary>
public sealed class FreewayFighterParser : ManifestDungeonWorldParser
{
    public FreewayFighterParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "FreewayFighter";
    protected override string TitleMatch => "Freeway Fighter";
    protected override string Slug => "ff13_freeway_fighter";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff13.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 14, 15 };
    protected override int MaxSectionNumber => 380;

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Ghost sections whose slices resolve empty: restore the sol12-proven
        // exit so the graph connects. Fires only on empty content.
        if (string.IsNullOrWhiteSpace(content))
        {
            return sectionNumber switch
            {
                1 => "(turn to 126).",   // investigate shotgun -> from New Hope (sol12)
                2 => "(turn to 13).",    // shoot snake -> oil spray (sol12)
                3 => "(turn to 354).",   // steer into skid -> accelerate (sol12)
                7 => "(turn to 319).",   // Amber hands you -> synthi-pills (sol12)
                13 => "(turn to 361).",  // oil spray -> cycle skids (sol12; slice has rules text)
                15 => "(turn to 169).",  // car repaired -> set off east (sol12)
                20 => "(turn to 111).",  // won Blitz Race -> Black Rats (sol12)
                21 => "(turn to 221).",  // repair car -> Amber turning (sol12)
                53 => "(turn to 78).",   // chase attackers -> farmhouse (sol12)
                67 => "(turn to 200).",  // avoid collision -> shoot it out (sol12)
                76 => "(turn to 198).",  // crawl to fence -> wire-cutters (sol12)
                81 => "(turn to 218).",  // sleep in tanker -> keep control (sol12)
                88 => "(turn to 271).",  // south at desert edge -> spares (sol12)
                90 => "(turn to 147).",  // citizens obey -> keep head low (sol12)
                91 => "(turn to 230).",  // east at crude sign -> repairs (sol12)
                95 => "(turn to 249).",  // destroy motor cycle -> side-pannier (sol12)
                96 => "(turn to 180).",  // full fuel can -> not enough (sol12)
                101 => "(turn to 303).", // journey south -> major junction (sol12)
                109 => "(turn to 49).",  // no crowbar -> CHARIOT (sol12)
                126 => "(turn to 274).", // from New Hope -> Joe's Garage (sol12)
                169 => "(turn to 259).", // set off east -> pistol duel (sol12)
                189 => "(turn to 24).",  // rough road -> race gate (sol12)
                249 => "(turn to 206).", // side-pannier -> change tire (sol12)
                254 => "(turn to 101).", // minefield -> journey south (sol12)
                259 => "(turn to 291).", // pistol duel -> DUELIST (sol12)
                267 => "(turn to 195).", // rear door -> hiding (sol12)
                306 => "(turn to 118).", // siphon fuel -> canister (sol12)
                321 => "(turn to 55).",  // sleep above cafe -> bed (sol12)
                341 => "(turn to 267).", // ambulance -> rear door (sol12)
                346 => "(turn to 157).", // south to Rockville -> bazooka (sol12)
                361 => "(turn to 96).",  // cycle skids -> fuel can (sol12)
                371 => "(turn to 225).", // south to San Anglo -> canister (sol12)
                _ => content,
            };
        }
        // Truncated tails: the slice ends mid-exit (number lives in the next
        // slice), so complete it. Suffix-checked, never fires on whole text.
        if (sectionNumber == 175 && content.TrimEnd().EndsWith("(tuen to", StringComparison.OrdinalIgnoreCase))
            content += " 340).";             // racing ahead -> iron spikes (tuen not shared; sol12)
        if (sectionNumber == 156 && content.TrimEnd().EndsWith("{turn to", StringComparison.OrdinalIgnoreCase))
            content += " 207).";             // south to main road -> modify engine (dump p56; sol12)
        // True exits missing from the slice (cut choices / OCR-garbled numbers).
        if (sectionNumber == 13 && !content.Contains("turn to 361", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 361).";    // oil spray -> cycle skids (sol12; slice has shooting rules)
        if (sectionNumber == 22 && !content.Contains("turn to 203", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 203).";    // cruising east -> road-block (sol12; slice has bulletin text)
        if (sectionNumber == 14 && !content.Contains("turn to 217", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 217).";    // chanting man -> robed man (sol12; slice has combat rules)
        if (sectionNumber == 63 && !content.Contains("turn to 334", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 334).";    // ram Ford -> accelerator down (sol12; exits cut off)
        if (sectionNumber == 188 && !content.Contains("turn to 66", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 66).";     // drive west over highway -> bridge (sol12; number cut off)
        if (sectionNumber == 34 && !content.Contains("turn to 167", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 167).";    // up onto highway -> RED CHEVROLET (sol12; number cut off)
        if (sectionNumber == 372 && !content.Contains("turn to 304", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 304).";    // let riders get away (dump p101 raw tail; off-path branch)
        // Orphaned exit numbers: the slice ends on a dangling turn-to while the
        // number sits on a short line the pipeline drops as noise (dump-proven
        // per section, inside the section's own slice).
        if (sectionNumber == 73 && !content.Contains("turn to 198", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 198).";    // crawl to fence -> wire-cutters (dump p34R[3])
        if (sectionNumber == 114 && !content.Contains("turn to 92", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 92).";     // ignore turning -> dust trails ("gz." dump p42R[55])
        if (sectionNumber == 153 && !content.Contains("turn to 225", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 225).";    // T-junction south -> fuel canister (dump p51R[33])
        if (sectionNumber == 192 && !content.Contains("turn to 104", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 104).";    // unconscious -> plastic tubing (dump p61R[29])
        if (sectionNumber == 216 && !content.Contains("turn to 243", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 243).";    // T-junction south -> police car (dump p67R[41])
        if (sectionNumber == 241 && !content.Contains("turn to 218", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 218).";    // sleep in cabin -> keep control (dump p73R[26])
        if (sectionNumber == 300 && !content.Contains("turn to 223", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 223).";    // spectate race (dump p87L[27])
        if (sectionNumber == 363 && !content.Contains("turn to 207", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 207).";    // main road south -> modify engine (dump p99R[42])
        if (sectionNumber == 370 && !content.Contains("turn to 229", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 229).";    // T-junction south ("22g." dump p101R[10])
        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair
            // FIRST, so targets below are post-shared text.
            34 => content.Replace("turn to soz", "turn to 302", StringComparison.OrdinalIgnoreCase), // talk to girl -> garage ambush (S302 girl scene; s/o/z for 3/0/2)
            40 => content.Replace("tum to #1", "turn to 81", StringComparison.OrdinalIgnoreCase), // SKILL wins -> sleep in tanker ("81" misread; sol12 flavor match)
            49 => content.Replace("turn to ga", "turn to 91", StringComparison.OrdinalIgnoreCase), // win CHARIOT -> head east ("91" misread; sol12)
            94 => content.Replace("urn to 284", "turn to 284", StringComparison.OrdinalIgnoreCase), // no spikes/oil -> E-type chase (urn not a shared verb)
            131 => content.Replace("(turn bo za)", "(turn to 22)", StringComparison.OrdinalIgnoreCase), // burn up road east -> cruising east (sol12)
            158 => content.Replace("turn to 65.", "turn to 67.", StringComparison.OrdinalIgnoreCase), // survive combat -> avoid collision ("67" misread as 65; sol12)
            164 => content.Replace("urn to 132", "turn to 132", StringComparison.OrdinalIgnoreCase), // win duel -> rat bite (urn not a shared verb; sol12)
            175 => content.Replace("(tuen to 340).", "(turn to 340).", StringComparison.OrdinalIgnoreCase), // racing ahead -> iron spikes (tuen not shared; sol12)
            180 => content.Replace("(hern to 243)", "(turn to 243)", StringComparison.OrdinalIgnoreCase), // not enough fuel -> police car (hern not shared; sol12)
            218 => content.Replace("urn to 52", "turn to 52", StringComparison.OrdinalIgnoreCase), // keep control -> biker duel (urn not shared; sol12)
            222 => content.Replace("Turn\nYou jack", "Turn to 53.\nYou jack", StringComparison.OrdinalIgnoreCase), // chase attackers (number cut off; sol12)
            225 => content.Replace("turn to 297", "turn to 197", StringComparison.OrdinalIgnoreCase), // have canister -> night building ("197" misread as 297; sol12)
            235 => content.Replace("(turn to 90)", "(turn to 40)", StringComparison.OrdinalIgnoreCase), // crossfire -> faster than raider ("40" misread as 90; sol12)
            252 => content.Replace("turn to 285", "turn to 185", StringComparison.OrdinalIgnoreCase), // left door -> leave house ("185" misread as 285; sol12)
            372 => content.Replace("turn to 99.", "turn to 95.", StringComparison.OrdinalIgnoreCase), // drive after riders -> MOTOR CYCLE ("95" misread as gg->99; sol12)
            376 => content.Replace("(turn to ga)", "(turn to 90)", StringComparison.OrdinalIgnoreCase), // optimistic -> citizens obey ("90" misread; sol12)
            _ => content,
        };
    }
}
