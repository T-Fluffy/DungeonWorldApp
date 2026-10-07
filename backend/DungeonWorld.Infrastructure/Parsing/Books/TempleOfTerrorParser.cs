using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF14 Temple of Terror (Ian Livingstone, 1985). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across pages
/// 15-107; wide pages split into two-up L/R halves, front matter on narrow
/// single-column pages). Intro uses the cover/title pages (1-2) plus the
/// BACKGROUND (13-14); the curated intro text ships in Intros/ff14.txt.
/// Section exits adjudicated against the arek.bdmonkeys.net walkthrough
/// (sol13). Ghost slices (empty in ff14.json) carry sol13-restored exits;
/// dup-slot and range-header slices keep the foreign body verbatim.
/// Misread headers restored via MANUAL walk entries (S8 "B", S9/S10 under
/// the "9-11" range, S17 "I", S34 "M4", S60 "Ga", S66 "63", S82 "Ba",
/// S88 "BE", S89 "Bg", S90 "go", S98 "0B", S107 "x07", S120 "11g",
/// S163 "16", S203 "20%", S293 "203", S300 "tg. 300", S334 "3:",
/// S340 "30", S344 "F44", S364 "354", S371 "37L", S381 "361").
/// </summary>
public sealed class TempleOfTerrorParser : ManifestDungeonWorldParser
{
    public TempleOfTerrorParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "TempleOfTerror";
    protected override string TitleMatch => "Temple of Terror";
    protected override string Slug => "ff14_temple_of_terror";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff14.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 13, 14 };

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Ghost sections whose slices resolve empty: restore the sol13-proven
        // exit so the graph connects. Fires only on empty content.
        if (string.IsNullOrWhiteSpace(content))
        {
            return sectionNumber switch
            {
                87 => "(turn to 362).",  // war-hammer -> thunder from the pit (sol13)
                _ => content,
            };
        }
        // True exits missing from the slice (cut choices / OCR-garbled numbers).
        // The ten spell teachings all end "Return to 34" (unparsed verb);
        // normalize the verb (shared burn->turn precedent, sol13).
        content = content.Replace("Return to 34", "turn to 34", StringComparison.OrdinalIgnoreCase);
        if (sectionNumber == 59 && !content.Contains("turn to 280", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 280).";    // keep control of mind -> rummage (sol13; slice has S39's body)
        if (sectionNumber == 230 && !content.Contains("turn to 278", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 278).";    // tell the captain -> desert (sol13; no exit in slice)
        if (sectionNumber == 306 && !content.Contains("turn to 339", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 339).";    // investigate moving lights (sol13; verb cut off)
        if (sectionNumber == 190 && !content.Contains("turn to 40", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 40).";     // right towards tapestries (sol13; exit cut off)
        if (sectionNumber == 181 && !content.Contains("turn to 376", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 376).";    // shake hands, press on (sol13; exit cut off)
        if (sectionNumber == 98 && !content.Contains("turn to 300", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 300).";    // know the prize (300, per S181's "joo") -> destiny (sol13)
        if (sectionNumber == 380 && !content.Contains("turn to 400", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 400).";    // Malbordus dead -> victory (sol13; exit cut off)
        // Orphaned exit numbers: the slice ends on a dangling turn-to while the
        // number sits on a short line the pipeline drops as noise (dump-proven,
        // inside the section's own slice).
        if (sectionNumber == 275 && !content.Contains("turn to 164", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 164).";    // press on south (dump p79R[25])
        if (sectionNumber == 358 && !content.Contains("turn to 237", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 237).";    // continue without eating ("237-" dump p97L[13])
        // Truncated tails: the slice ends mid-exit, so complete it.
        // Suffix-checked per section, never fires on whole text.
        if (sectionNumber == 213 && content.TrimEnd().EndsWith(", turn", StringComparison.OrdinalIgnoreCase))
            content += " to 146).";          // haggle with the captain (sol13)
        if (sectionNumber == 166 && content.TrimEnd().EndsWith("(turn", StringComparison.OrdinalIgnoreCase))
            content += " to 238).";          // step aboard the ship (sol13)
        if (sectionNumber == 21 && content.TrimEnd().EndsWith("Tum", StringComparison.OrdinalIgnoreCase))
            content += " to 46).";           // walk directly ahead (sol13)
        if (sectionNumber == 361 && content.TrimEnd().EndsWith("Tum", StringComparison.OrdinalIgnoreCase))
            content += " to 340).";          // run past the Eye Stinger (sol13)
        if (sectionNumber == 219 && content.TrimEnd().EndsWith("turn", StringComparison.OrdinalIgnoreCase))
            content += " to 137).";          // run after Leesha (sol13)
        if (sectionNumber == 171 && content.TrimEnd().EndsWith("Tum", StringComparison.OrdinalIgnoreCase))
            content += " to 314).";          // run down the passageway (sol13)
        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair
            // FIRST, so targets below are post-shared text.
            79 => content.Replace("turn to 5 into", "lumbers into", StringComparison.OrdinalIgnoreCase) // mutant lumbers (debris edge)
                .Replace("turn to 3049", "turn to 309", StringComparison.OrdinalIgnoreCase), // backpack -> bolt of light (sol13)
            198 => content.Replace("Turn to 2g90", "Turn to 290", StringComparison.OrdinalIgnoreCase), // pick up silver rod (sol13)
            296 => content.Replace("turn to 182", "turn to 181", StringComparison.OrdinalIgnoreCase), // like the work -> Murkegg handshake ("181" misread; sol13)
            302 => content.Replace("Turn to ¢3.", "Turn to 93.", StringComparison.OrdinalIgnoreCase), // door opposite -> Dark Disciples ("93" misread; sol13)
            93 => content.Replace("hum to 11", "turn to 11", StringComparison.OrdinalIgnoreCase), // gift -> medallion (hum not shared; sol13)
            393 => content.Replace("turn to Sp.", "turn to 60.", StringComparison.OrdinalIgnoreCase), // cut bucket ("60" misread; sol13)
            23 => content.Replace("turn to 526", "turn to 52", StringComparison.OrdinalIgnoreCase), // investigate smoke (stray digit, FF09 precedent)
            119 => content.Replace("turn to 5 down", "lumbers down", StringComparison.OrdinalIgnoreCase) // servant lumbers (debris edge)
                .Replace("bum to 73", "turn to 73", StringComparison.OrdinalIgnoreCase), // win SERVANT fight (sol13)
            358 => content.Replace("turn to 122", "turn to 112", StringComparison.OrdinalIgnoreCase), // eat grapes -> walk on ("112" misread; S122 is the pit-fall scene, sol13)
            _ => content,
        };
    }
}
