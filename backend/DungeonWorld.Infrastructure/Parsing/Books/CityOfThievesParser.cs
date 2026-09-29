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

    /// <summary>
    /// Hand fixes for the City of Thieves scan's "turn" misread family
    /// (burn/barn/harn/tun/tum/tion/cium with garbled prepositions), written
    /// against post-normalization text. Deliberately book-local rather than
    /// shared: barn/fun are dictionary words, so promoting them would raise
    /// the false-positive ceiling for every future book. Every numeric target
    /// below was verified against 600 dpi crops (S38/S206 read clean "turn to
    /// 296"; S84/S107 "turn to 78"; S12 "turn to 176"; S152 "turn to 20";
    /// S308 "turn to 189"; S196 "turn to 148"; S369 "turn to 216"; S65 "Turn
    /// to 319"/"Turn to 96") or against the arek/Beroli walkthrough chains
    /// (S14 no-ring 191; S108 lack-items 299; S239 have-all 201; S337 epilogue
    /// 400; S25 conversation 169; S398 walk-on 52; S180 barrel-watch 181).
    /// S239's "burn bo 204" is a 1→4 misread of 201 (its twin S108 and arek
    /// agree the have-all branch leads to the Ape Man at 201); S337's "goo"
    /// can only be 400 (900 is out of range). S230's "Be" is an o→e
    /// misread of "to" (the guardhouse at 54 follows the stair climb).
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content) =>
        sectionNumber switch
        {
            12 => content.Replace("turn to aye", "turn to 176", StringComparison.Ordinal)
                .Replace("'Abem!'", "'Ahem!'", StringComparison.Ordinal),
            14 => content.Replace("tun\nto 237", "turn to 237", StringComparison.Ordinal)
                .Replace("turn to aga", "turn to 191", StringComparison.Ordinal),
            25 => content.Replace("Turn ty 16g", "Turn to 169", StringComparison.Ordinal)
                .Replace("Turn tiv 323", "Turn to 323", StringComparison.Ordinal),
            38 => content.Replace("(barn to 296)", "(turn to 296)", StringComparison.Ordinal),
            65 => content.Replace("Turn to 31", "Turn to 319", StringComparison.Ordinal)
                .Replace("turn to 9", "turn to 96", StringComparison.Ordinal),
            84 => content.Replace("burn bo 78", "turn to 78", StringComparison.Ordinal),
            107 => content.Replace("burn bo Bo", "turn to 78", StringComparison.Ordinal),
            108 => content.Replace("tum to 2949", "turn to 299", StringComparison.Ordinal),
            152 => content.Replace("turn to ze.", "turn to 20.", StringComparison.Ordinal),
            180 => content.Replace("burn 181", "turn to 181", StringComparison.Ordinal),
            196 => content.Replace("tion to 148)", "turn to 148)", StringComparison.Ordinal),
            206 => content.Replace("(harn to 296)", "(turn to 296)", StringComparison.Ordinal),
            230 => content.Replace("{turn Be 54)", "{turn to 54)", StringComparison.Ordinal),
            239 => content.Replace("burn bo 204", "turn to 201", StringComparison.Ordinal),
            304 => content.Replace("cium fo 138)", "turn to 138)", StringComparison.Ordinal),
            308 => content.Replace("turn to 18a)", "turn to 189)", StringComparison.Ordinal),
            322 => content.Replace("(barn bo g3)", "(turn to 93)", StringComparison.Ordinal),
            337 => content.Replace("{turn to goo)", "{turn to 400)", StringComparison.Ordinal),
            369 => content.Replace("turn to 10 216", "turn to 216", StringComparison.Ordinal),
            376 => content.Replace("(fun 10 163", "(turn to 161", StringComparison.Ordinal),
            398 => content.Replace("tum me", "turn to 52", StringComparison.Ordinal),
            _ => content,
        };

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));
}
