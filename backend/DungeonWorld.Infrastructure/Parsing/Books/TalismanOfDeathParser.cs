using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF11 Talisman of Death (Jamie Thomson & Mark Smith, 1984). Rebuilt from a
/// manual reconstruction manifest (300 dpi line transcripts, 400 sections
/// across pages 23-263; single-surface scans). Intro uses the cover/title
/// pages (1-2) plus the BACKGROUND narrative (18-21); the curated intro text
/// ships in Intros/ff11.txt. Section exits adjudicated against the
/// arek.bdmonkeys.net walkthrough (sol10). Open items: S26/S28/S38/S77/S204
/// lost their bodies (ghosts, exits unknown); misread headers restored as
/// S90/S313/S314/S319; S25 keeps the Willow text (S26 might own it, but S25
/// is the only reachable carrier); S278 aid-branch reads 286, not 279
/// (S279 is the library — narrative and sol10 playthrough confirm).
/// </summary>
public sealed class TalismanOfDeathParser : ManifestDungeonWorldParser
{
    public TalismanOfDeathParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "TalismanOfDeath";
    protected override string TitleMatch => "Talisman of Death";
    protected override string Slug => "ff11_talisman_of_death";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff11.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 18, 19, 20, 21 };

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return sectionNumber switch
            {
                // Bodies lost (art pages / page-break headers); exits unknown.
                _ => content,
            };
        }
        // True exits missing from the slice (tail cut into the next slice).
        if (sectionNumber == 6 && !content.Contains("turn to 64", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 64).";  // set out to find the Thieves' Guild (sol10)
        if (sectionNumber == 48 && !content.Contains("turn to 35", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 35).";  // lid of the wood box -> leave and climb (sol10)
        if (sectionNumber == 131 && !content.Contains("turn to 58", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 58).";  // leave the Sage -> try the door (sol10)
        if (sectionNumber == 368 && content.Contains("Tyutchev", StringComparison.OrdinalIgnoreCase)
            && !content.Contains("turn to 353", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 353)."; // reduce Tyutchev -> onward (dump p246; number cut)
        if (sectionNumber == 276 && content.TrimEnd().EndsWith("Turn", StringComparison.OrdinalIgnoreCase))
            content += " to 241.";        // guard resolution (dump p200; "to 241" noise-dropped)
        return sectionNumber switch
        {
            8 => content.Replace("Turnto31y", "Turn to 31", StringComparison.OrdinalIgnoreCase), // east-then-south route (dump p30)
            144 => content.Replace("turn, to 396", "turn to 396", StringComparison.OrdinalIgnoreCase), // skull win (dump p120)
            143 => content.Replace("turn to ¢8", "turn to 98", StringComparison.OrdinalIgnoreCase), // whole story (sol10; ¢->9)
            278 => content.Replace("(turn to 279", "(turn to 286).", StringComparison.OrdinalIgnoreCase), // aid-attack -> thieves fight (sol10; S279 is the library, narrative + playthrough confirm 286)
            _ => content,
        };
    }
}
