using System.Text.RegularExpressions;
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
    /// Hand fixes for sections whose garble survives the shared repair, written
    /// against post-normalization text (the shared TurnToRepair pass runs first,
    /// so patterns match its output: clean verbs, remaining digit garbles and
    /// truncations). Targets verified against 600 dpi crops, transcriptions and
    /// walkthroughs (arek's solution validates 95→367; narrative coherence
    /// validates 100→276). Section 192's old "Turn to 3" arm was retired as a
    /// substring footgun; its "Turn to SET a EERE TT Re" garble needs the book.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        if (sectionNumber == 50) return "Turn to 164.";
        return sectionNumber switch
        {
            74 => content.Replace("counter-attack. Tum", "counter-attack. Turn to 377.", StringComparison.Ordinal),
            95 => content.Replace("turn to 357.", "Turn to 367.", StringComparison.Ordinal),
            100 => content.Replace("I not, turn to 107", "If not, turn to 276.", StringComparison.Ordinal)
                .Replace("1 not, turn to 107", "If not, turn to 276.", StringComparison.Ordinal),
            102 => content.Replace("turn to 0.", "turn to 270.", StringComparison.Ordinal),
            177 => content.Replace("down the steps (hum", "down the steps (turn to 344).", StringComparison.Ordinal),
            205 => content.Replace("tien Lo 300", "turn to 368", StringComparison.Ordinal),
            229 => content.Replace("(fur to 230", "(turn to 230).", StringComparison.Ordinal),
            330 => Regex.Replace(
                content.Replace("ten Lo 208", "turn to 208.", StringComparison.Ordinal),
                @"turn to 33\b", "turn to 120."),
            354 => Regex.Replace(content, @"turn to 355\b", "Turn to 188."),
            _ => content,
        };
    }

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));
}
