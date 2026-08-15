using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF03 The Forest of Doom (Ian Livingstone, 1983). Rebuilt from a manual reconstruction
/// manifest (300 dpi line transcripts, 400 sections across pages 15-103, entry 400 capped
/// at line 57 before the back matter). Intro uses cover/title (1-3) + BACKGROUND (12-14).
/// </summary>
public sealed class ForestOfDoomParser : ManifestDungeonWorldParser
{
    public ForestOfDoomParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "ForestOfDoom";
    protected override string TitleMatch => "Forest of Doom";
    protected override string Slug => "ff03_forest_of_doom";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff03.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 3, 12, 13, 14 };
    protected override bool NormalizeTurnTos => true;

    protected override string PostProcessContent(string content) =>
        content.Replace("turn to 1710", "turn to 171", StringComparison.Ordinal);
}
