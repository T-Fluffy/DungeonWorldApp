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
    /// Hand-verified turn-to targets the shared repair cannot reach (non-digit
    /// garbles or verified corrections): 182 "7e"→70 (600dpi clean read; sole
    /// parent of unreachable 70; both period and paren wrappings observed
    /// across re-parse runs, so both map); 204 "4o00"→400 (final section, zero-incoming
    /// isolate); 221 "(urn to 19g)"→199 (walkthrough explicit; "urn" is not a
    /// known verb form); 346 "11%"→111 (old-model reading confirms);
    /// 355 "iyo"→340 (600dpi clean read); 358 "Turn to go."→40 (walkthrough
    /// explicit; S40's text matches the post-trap cursing — overrides the
    /// shared go→90 reading).
    /// Wobble note: re-parses OCR the man-trap line as "go" or "90" on
    /// different runs, so both variants map to the walkthrough-verified 40. Everything else (8g/9, go/90, lo/367-style
    /// digit confusions) is handled by the shared TurnToRepair g→9 mapping.
    /// Book answers (user-read, overriding clean-OCR digits): S172's ladder
    /// branch "Turn to 256" is 255 and S17's west branch "Turn to 238" is 236
    /// (both single-digit small-print flips; S89/S387 verified matching).
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content) =>
        sectionNumber switch
        {
            17 => content.Replace("head west Turn to 238", "head west Turn to 236", StringComparison.Ordinal),
            172 => content.Replace("the tunnel Turn to 256", "the tunnel Turn to 255", StringComparison.Ordinal),
            182 => content.Replace("turn to 7e.", "turn to 70.", StringComparison.Ordinal)
                .Replace("turn to 7e)", "turn to 70)", StringComparison.Ordinal),
            204 => content.Replace("turn to 4o00.", "turn to 400.", StringComparison.Ordinal),
            221 => content.Replace("(urn to 19g).", "(turn to 199).", StringComparison.Ordinal),
            346 => content.Replace("turn to 11%.", "turn to 111.", StringComparison.Ordinal),
            355 => content.Replace("turn to iyo.", "turn to 340.", StringComparison.Ordinal),
            358 => content.Replace("man trap. Turn to go.", "man trap. Turn to 40.", StringComparison.Ordinal)
                .Replace("man trap. Turn to 90.", "man trap. Turn to 40.", StringComparison.Ordinal),
            _ => content,
        };

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));
}
