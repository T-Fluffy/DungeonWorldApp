using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF06 Deathtrap Dungeon (Ian Livingstone, 1984). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across
/// pages 31-222; single-page portraits, side M throughout). Intro uses the
/// cover/title pages (1, 3) + BACKGROUND narrative (22-28).
/// </summary>
public sealed class DeathtrapDungeonParser : ManifestDungeonWorldParser
{
    public DeathtrapDungeonParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "DeathtrapDungeon";
    protected override string TitleMatch => "Deathtrap Dungeon";
    protected override string Slug => "ff06_deathtrap_dungeon";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff06.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 3, 22, 23, 24, 25, 26, 27, 28 };

    /// <summary>
    /// S287's text runs to the page-bottom edge ("...before a white bolt of
    /// energy") and continues as S288 ("shoots out from the lock...") with no
    /// printed turn line recoverable at either 300 or 600 dpi, so the exit is
    /// restored as the page-turn continuation to 288. Round 2 restores tails
    /// the 300 dpi transcript dropped at line gaps, every one verified against
    /// 600 dpi crops: S97 ("Turn to 134"), S151 ("24o0" is 240 plus dirt),
    /// S161 ("Turn to 29"), S172 ("Turn to 278"), S178 ("Turn to 344"),
    /// S231 ("Turn to no." is a clean "Turn to 110" at 600 dpi), S346 ("Turn
    /// to 362"). S129's tail ("Rows of razor-sharp teeth... PIT FIEND 349")
    /// is fixed in the manifest instead (S130's entry moved past the spill).
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        if (sectionNumber == 287
            && content.TrimEnd().EndsWith("before a white bolt of energy", StringComparison.Ordinal)
            && !content.Contains("Turn to 288.", StringComparison.Ordinal))
            return content.TrimEnd() + " Turn to 288.";
        if (sectionNumber == 97 && content.TrimEnd().EndsWith("as fast as possible. Turn", StringComparison.Ordinal))
            return content.TrimEnd() + " to 134.";
        if (sectionNumber == 161 && content.TrimEnd().EndsWith("set off north again. Turn", StringComparison.Ordinal))
            return content.TrimEnd() + " to 29.";
        if (sectionNumber == 172 && content.TrimEnd().EndsWith("second Attack Round, turn", StringComparison.Ordinal))
            return content.TrimEnd() + " to 278.";
        if (sectionNumber == 178 && content.TrimEnd().EndsWith("set off north again. Turn", StringComparison.Ordinal))
            return content.TrimEnd() + " to 344.";
        if (sectionNumber == 346 && content.TrimEnd().EndsWith("continue your journey west. Turn", StringComparison.Ordinal))
            return content.TrimEnd() + " to 362.";
        return sectionNumber switch
        {
            151 => content.Replace("turn to 24o0.", "turn to 240.", StringComparison.Ordinal),
            231 => content.Replace("Turn to no.", "Turn to 110.", StringComparison.Ordinal),
            _ => content,
        };
    }

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));
}
