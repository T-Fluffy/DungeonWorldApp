using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF10 House of Hell (Steve Jackson, 1984). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across pages
/// 22-221; single-surface scans). Intro uses the cover/title pages (1-2) plus
/// the BACKGROUND narrative (17-20); the curated intro text ships in
/// Intros/ff10.txt. Section exits adjudicated against the arek.bdmonkeys.net
/// walkthrough (sol9): all 142 path edges resolve, choices match references,
/// no dangling targets. Page-top running-head echoes moved to true bare
/// headers so overflow tails stay with their section. Open items: S107's
/// body was lost to full-page art (ghost, exits unknown); misread headers
/// restored as S147/S194/S313/S314/S319/S397; S220/S400 verified against
/// sol9 summaries; ~17 off-path sections stay unreachable (lost bodies or
/// parents).
/// </summary>
public sealed class HouseOfHellParser : ManifestDungeonWorldParser
{
    public HouseOfHellParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "HouseOfHell";
    protected override string TitleMatch => "House of Hell";
    protected override string Slug => "ff10_house_of_hell";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff10.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 17, 18, 19, 20 };

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return sectionNumber switch
            {
                // S107's body was lost to full-page art (ghost, exits unknown).
                _ => content,
            };
        }
        // Truncated tails: the slice ends mid-exit (number lives in the next
        // slice), so complete it. Suffix-checked, never fires on whole text.
        if (sectionNumber == 352 && content.TrimEnd().EndsWith("Then turn", StringComparison.OrdinalIgnoreCase))
            content += " to 57.";            // Orville 'A' -> answer Kelnor (sol9)
        if (sectionNumber == 315 && content.TrimEnd().EndsWith("Now turn", StringComparison.OrdinalIgnoreCase))
            content += " to 235.";           // torture scoring -> Drumer answer (sol9)
        if (sectionNumber == 194 && content.TrimEnd().EndsWith("Now turn", StringComparison.OrdinalIgnoreCase))
            content += " to 130.";           // Abaddon score -> Kelnor answer (sol9)
        if (sectionNumber == 130 && content.TrimEnd().EndsWith("Now turn", StringComparison.OrdinalIgnoreCase))
            content += " to 297.";           // Kelnor score -> Mordana answer (sol9)
        if (sectionNumber == 141 && content.TrimEnd().EndsWith("Now turn", StringComparison.OrdinalIgnoreCase))
            content += " to 280.";           // Shekou score -> apologies (sol9)
        if (sectionNumber == 300 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 105).";             // defeat the Master -> follow him out (sol9)
        if (sectionNumber == 145 && content.TrimEnd().EndsWith("(turn", StringComparison.OrdinalIgnoreCase))
            content += " to 64).";           // back to window -> hurl soil (sol9)
        if (sectionNumber == 355 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 263).";             // wait for figure -> leap and attack (sol9)
        if (sectionNumber == 83 && content.TrimEnd().EndsWith("(turn", StringComparison.OrdinalIgnoreCase))
            content += " to 233).";          // leave by entry door -> landing (sol9)
        if (sectionNumber == 144 && content.TrimEnd().EndsWith("(turn", StringComparison.OrdinalIgnoreCase))
            content += " to 278).";          // mumble apologies -> try elsewhere (dump p96)
        if (sectionNumber == 353 && content.TrimEnd().EndsWith("(turn", StringComparison.OrdinalIgnoreCase))
            content += " to 292).";          // tipple -> corner shelf (sol9)
        if (sectionNumber == 295 && content.TrimEnd().EndsWith("turning to", StringComparison.OrdinalIgnoreCase))
            content += " 159.";              // leave room -> creep downstairs (sol9; number cut)
        if (sectionNumber == 323 && content.TrimEnd().EndsWith("Turn", StringComparison.OrdinalIgnoreCase))
            content += " to 118.";           // no key -> door opposite (dump p184; "to 118" noise-dropped)
        if (sectionNumber == 323 && !content.Contains("turn to 296", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 296).";    // cast-iron key #27 math: 323-27 (sol9)
        // True exits missing from the slice (noted-reference mechanic / lost line).
        if (sectionNumber == 320 && !content.Contains("turn to 310", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 310).";    // in position -> look for secret doors (sol9)
        if (sectionNumber == 321 && !content.Contains("turn to 88", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 88).";     // noted Mordana reference -> secret rooms (sol9)
        return sectionNumber switch
        {
            64 => content.Replace("now to 375.", "to 375.", StringComparison.OrdinalIgnoreCase), // soil douses Sprites (sol9; "Turn" separated by newline in slice)
            93 => content.Replace("passageway or 166 to", "passageway or turn to 166 to", StringComparison.OrdinalIgnoreCase), // look around (sol9)
            _ => content,
        };
    }
}
