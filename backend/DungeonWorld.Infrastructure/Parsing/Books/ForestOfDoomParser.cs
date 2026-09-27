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

    /// <summary>
    /// Hand-verified turn-to targets for sections whose OCR garble is ambiguous
    /// to rules (g→6/9 confusions, stray specks). Each target was chosen by
    /// graph evidence: the selected section is currently unreachable with no
    /// other incoming edge, which the fix explains; alternatives are noted.
    /// section 17 "8g"→89 (not 86, already reachable); 85 "g."→9 (single char,
    /// most parsimonious); 87 "go"→90 (zero-incoming isolate); 137 "1e"→16;
    /// 204 "4o00"→400 (final section, zero-incoming isolate); 230 "2go"→290
    /// (unreachable); 306 "3g1"→391 (zero-incoming isolate); 355 "3qo"→390
    /// (only numeric reading, q→9). The shared repair handles "rn to 55".
    /// </summary>
    protected override string PostProcessSection(int sectionNumber, string content) =>
        sectionNumber switch
        {
            17 => content.Replace("Turn to 8g", "Turn to 89.", StringComparison.Ordinal),
            85 => content.Replace("turn to g.", "turn to 9.", StringComparison.Ordinal),
            87 => content.Replace("Turn to go.", "Turn to 90.", StringComparison.Ordinal),
            137 => content.Replace("turn to 1e.", "turn to 16.", StringComparison.Ordinal),
            204 => content.Replace("turn to 4o00.", "turn to 400.", StringComparison.Ordinal),
            230 => content.Replace("Turn to 2go", "Turn to 290", StringComparison.Ordinal),
            306 => content.Replace("turn to 3g1.", "turn to 391.", StringComparison.Ordinal),
            355 => content.Replace("turn to 3qo.", "turn to 390.", StringComparison.Ordinal),
            _ => PostProcessContent(content),
        };
}
