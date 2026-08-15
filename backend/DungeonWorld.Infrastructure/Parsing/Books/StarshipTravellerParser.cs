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
}
