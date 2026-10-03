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
    /// against post-normalization text. The "ko" preposition garble (S21, S186,
    /// S225) and S330's "Turn 0 202" carry clean numbers; S180's "#6" is the
    /// dull-blue planet. S216/S238/S253 lost their exit lines to truncation, so
    /// their targets come from the walkthrough transcription (arek: 216→138,
    /// 238→160, 253→109). S216's tail is additionally de-truncated ("towards
    /// yo" → "towards you"); S201's dice prose is corrected alongside its exit
    /// ("double 8" → "double 6", same 600 dpi read). S315's "gu" is the keep
    /// ("Turn to 91"). Every target below was verified against 600 dpi crops, where the independent read agrees on the number (S21 "tum bo
    /// 259", S186 "HET FO 336", S330 "Turn to 202", S180 "turning to 86").
    /// Round 2 (book answers, user-read): S323's cut "Turn to" is the
    /// onward journey 248; S26's "(fun to 202}" is the starbase (S202 freed);
    /// S16's "(turn Eo g3)" is 93 (Eo-as-to plus g→9, same class as FF05
    /// S322's "barn bo g3"); S178's "turn immediately to 271" resolves to the
    /// portal aftermath (adverb shape kept book-local; S144→S178→S271 forms
    /// the coherent portal track). S292 needs no change (refs already [233]).
    /// Round 3 (portal maze): when S322's tail is cut as "(turn to 323", the
    /// right fork resolves to 214 per the reader, arek, and the Steam
    /// walkthrough (a weapons-resupply scene mid-maze is incoherent; S323
    /// keeps its 69/123 parents). Anchored to the cut tail so a complete
    /// "(turn to 323)" print is never touched.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content) =>
        sectionNumber switch
        {
            16 => content.Replace("(turn Eo g3)-", "(turn to 93)-", StringComparison.Ordinal),
            21 => content.Replace("(turn ko 259)", "(turn to 259)", StringComparison.Ordinal),
            26 => content.Replace("(fun to 202}", "(turn to 202}", StringComparison.Ordinal),
            178 => content.Replace("turn immediately to 271.", "turn to 271.", StringComparison.Ordinal),
            186 => content.Replace("Turn ko 336.", "Turn to 336.", StringComparison.Ordinal),
            201 => content.Replace("double 8, your Engineering Officer has died.", "double 6, your Engineering Officer has died.", StringComparison.Ordinal)
                .Replace("to aya.", "to 172.", StringComparison.Ordinal)
                .Replace("Turn\nto 172.", "Turn to 172.", StringComparison.Ordinal),
            315 => content.Replace("Turn to gu.", "Turn to 91.", StringComparison.Ordinal),
            322 => content.TrimEnd().EndsWith("(turn to 323", StringComparison.Ordinal)
                ? content.TrimEnd()[..^3] + "214"
                : content,
            323 => content.Replace("You may now continue on your journey, Turn to", "You may now continue on your journey, Turn to 248.", StringComparison.Ordinal),
            330 => content.Replace("Turn 0 202.", "Turn to 202.", StringComparison.Ordinal),
            180 => content.Replace("turning to #6", "turning to 86", StringComparison.Ordinal),
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
