using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF05 The City of Thieves (Ian Livingstone, 1983). Rebuilt from a manual reconstruction
/// manifest (300 dpi line transcripts, 400 sections across pages 15-110). Intro uses the
/// cover/blurb (1-2) + BACKGROUND narrative (12-14).
/// </summary>
public sealed class CityOfThievesParser : ManifestDungeonWorldParser
{
    public CityOfThievesParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "CityOfThieves";
    protected override string TitleMatch => "City of Thieves";
    protected override string Slug => "ff05_city_of_thieves";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff05.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 12, 13, 14 };
}
