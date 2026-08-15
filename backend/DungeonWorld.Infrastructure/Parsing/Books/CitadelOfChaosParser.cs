using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF02 The Citadel of Chaos (Steve Jackson, 1983). Rebuilt from a manual reconstruction
/// manifest (300 dpi line transcripts, 400 sections across pages 17-109). Front matter is
/// pages 1-16; the intro uses the cover, title/background and HISTORY pages.
/// </summary>
public sealed class CitadelOfChaosParser : ManifestDungeonWorldParser
{
    public CitadelOfChaosParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "CitadelOfChaos";
    protected override string TitleMatch => "Citadel of Chaos";
    protected override string Slug => "ff02_citadel_of_chaos";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff02.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 4, 5, 16 };

    /// <summary>
    /// Enriches 15 sections whose final "turn to" line was dropped by the line-noise filter and
    /// whose turn-to number was then swallowed from the following section header (or lost entirely,
    /// as in section 50). The true targets were verified against 600 dpi OCR crops of the original
    /// page spreads plus clean transcriptions (Scribd) and published walkthroughs. The shared OCR
    /// prefix of each section is preserved byte-for-byte; only the truncated tail is corrected.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        if (sectionNumber == 50) return "Turn to 164.";
        return sectionNumber switch
        {
            40 => content.Replace("(turn 41", "(turn to 41)", StringComparison.Ordinal),
            41 => content.Replace("Tum b 257", "Turn to 257.", StringComparison.Ordinal),
            74 => content.Replace("Tum o 377", "Turn to 377.", StringComparison.Ordinal),
            95 => content.Replace("Tum o 357,", "Turn to 367.", StringComparison.Ordinal),
            100 => content.Replace("I not, turn 107", "If not, turn to 276.", StringComparison.Ordinal),
            102 => content.Replace("turn to 0.", "turn to 270.", StringComparison.Ordinal),
            177 => content.Replace("(fum 178", "(turn to 344)?", StringComparison.Ordinal),
            192 => content.Replace("Turn to 3", "Turn to 29.", StringComparison.Ordinal),
            205 => content.Replace("turn b 368", "turn to 368.", StringComparison.Ordinal),
            219 => content.Replace("Tum o 220", "Turn to 220.", StringComparison.Ordinal),
            223 => content.Replace("Turn to 138,", "Turn to 138.", StringComparison.Ordinal),
            229 => content.Replace("(fum to 230", "(turn to 230).", StringComparison.Ordinal),
            330 => content.Replace("tum 33", "turn to 120.", StringComparison.Ordinal),
            354 => content.Replace("Tumn 355", "Turn to 188.", StringComparison.Ordinal),
            _ => content,
        };
    }

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, content);
}
