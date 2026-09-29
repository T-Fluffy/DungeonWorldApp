using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF04 Starship Traveller (Steve Jackson &amp; Ian Livingstone, 1983). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 343 sections across pages 12-105;
/// the book ends at section 343). Intro uses cover/title (1-2) + mission/begin pages (6, 11).
/// </summary>
public sealed class StarshipTravellerParser : ManifestDungeonWorldParser
{
    public StarshipTravellerParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "StarshipTraveller";
    protected override string TitleMatch => "Starship Traveller";
    protected override string Slug => "ff04_starship_traveller";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff04.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 6, 11 };
    protected override int MaxSectionNumber => 343;

    /// <summary>
    /// Hand fixes for sections whose garble survives the shared repair, written
    /// against post-normalization text. S225's "Turn ko 57" (ko never a number)
    /// is fixed inline; S216/S238/S253 lost their exit lines to truncation, so
    /// their targets come from the walkthrough transcription (arek: 216→138,
    /// 238→160, 253→109). S216's tail is additionally de-truncated ("towards
    /// yo" → "towards you"). All four sources were verified against 600 dpi
    /// crops before applying.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content) =>
        sectionNumber switch
        {
            225 => content.Replace("Turn ko 57.", "Turn to 57.", StringComparison.Ordinal),
            216 => content.Replace("setting off towards yo", "setting off towards you. Turn to 138.", StringComparison.Ordinal),
            238 => content.Contains("Turn to 160.", StringComparison.Ordinal)
                ? content
                : content.TrimEnd() + " Turn to 160.",
            253 => content.Contains("Turn to 109.", StringComparison.Ordinal)
                ? content
                : content.TrimEnd() + " Turn to 109.",
            _ => content,
        };

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));
}
