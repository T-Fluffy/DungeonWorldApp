using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF08 Scorpion Swamp (Steve Jackson, 1984). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across pages
/// 28-232; single-page portraits, side M throughout). Intro uses the cover/title
/// pages (1, 3) plus the BACKGROUND narrative (24-26); the curated intro text
/// ships in Intros/ff08.txt. Section exits were adjudicated against the
/// arek.bdmonkeys.net walkthrough (sol7). Open items: S171 body lost (only the
/// p121 range line survives; parent S337-fire); S36/S22 south-branch "go" and
/// similar single-letter garbles below need book-checks.
/// </summary>
public sealed class ScorpionSwampParser : ManifestDungeonWorldParser
{
    public ScorpionSwampParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "ScorpionSwamp";
    protected override string TitleMatch => "Scorpion Swamp";
    protected override string Slug => "ff08_scorpion_swamp";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff08.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 3, 24, 25, 26 };

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Restored exit tails: short trailing fragments the transcript carries but
        // TrimContent drops as noise, orphaning the exit's head. Strings verified
        // against the 300 dpi dump (FF06 Round-2 / FF07 precedent); S1/S280 append
        // arek-proven exits whose lines never survived the scan (marked TODO).
        if (sectionNumber == 1 && content.TrimEnd().EndsWith("property!", StringComparison.OrdinalIgnoreCase))
            content += " [If you politely explain that you are determined, turn to 95. TODO: possible second tavern branch.]";
        if (sectionNumber == 195 && content.TrimEnd().EndsWith("soft part, turn", StringComparison.OrdinalIgnoreCase))
            content += " to 91.";
        if (sectionNumber == 280 && content.TrimEnd().EndsWith("keep on reading. A few", StringComparison.OrdinalIgnoreCase))
            content += " [If you have been to Willowbend before, turn to 355. Otherwise, turn to 78.]";
        if (sectionNumber == 127 && content.TrimEnd().EndsWith("seeking. Turn", StringComparison.OrdinalIgnoreCase))
            content += " to 104.";
        if (sectionNumber == 21 && content.TrimEnd().EndsWith("rather not know, turn", StringComparison.OrdinalIgnoreCase))
            content += " to 390.";
        return sectionNumber switch
        {
            // Arek-proven chain fixes (sol7 visits both sides of each edge).
            191 => content.Replace("Friendship? Turn to 204", "Friendship? Turn to 294", StringComparison.OrdinalIgnoreCase), // print 294 garbled 204 (9->0)
            137 => content.Replace("Turn back to 336", "Turn to 336", StringComparison.OrdinalIgnoreCase), // back-to verb miss
            118 => content.Replace("turn to yo", "turn to 70", StringComparison.OrdinalIgnoreCase), // Lucky leap (y->7, o->0)
            _ => content,
        };
    }
}
