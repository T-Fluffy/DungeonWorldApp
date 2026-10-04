using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF09 Caverns of the Snow Witch (Ian Livingstone, 1984). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across pages
/// 14-118; two-up L/R halves). Intro uses the cover/title pages (1, 3) plus the
/// BACKGROUND narrative (12-13); the curated intro text ships in Intros/ff09.txt.
/// Section exits were adjudicated against the arek.bdmonkeys.net walkthrough
/// (sol8): all 169 path edges resolve, choices match references, no dangling
/// targets. Ghost slices (empty in ff09.json) carry restored exits; dup-slot
/// slices keep the foreign body verbatim with the true exit appended. Known
/// scan losses: ~29 off-path sections stay unreachable (lost bodies/parents,
/// e.g. 60/90/105), S33's Lucky exit number is cut, and S109 shares S111's
/// body with unrecoverable true exits.
/// </summary>
public sealed class CavernsOfTheSnowWitchParser : ManifestDungeonWorldParser
{
    public CavernsOfTheSnowWitchParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "CavernsOfTheSnowWitch";
    protected override string TitleMatch => "Caverns of the Snow Witch";
    protected override string Slug => "ff09_caverns_of_the_snow_witch";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff09.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 3, 12, 13 };

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Ghost sections: the manifest slice resolves empty (M-side pointers,
        // p57L8, or dup-position collapse), so restore the arek-proven (sol8) or
        // dump-verified exit. Fires only on empty content.
        if (string.IsNullOrWhiteSpace(content))
        {
            return sectionNumber switch
            {
                9 => "(turn to 195).",    // swamp-entry trail north (body lost to p16 misprint)
                33 => "(turn to 236).",   // wild dagger Unlucky (dump p28L; Lucky number lost)
                34 => "(turn to 4).",     // stake -> Witch dies (sol8)
                36 => "(turn to 118).",   // hut stew -> Healer (sol8)
                41 => "(turn to 212).",   // villagers -> Snow Wolves (sol8)
                42 => "(turn to 201). [Also turn to 280.]", // backpack off -> head-above-water test (p36R gap)
                46 => "(turn to 312). [Also turn to 304.] [Also turn to 119.]", // vine / climb down / walk on (p26L dump)
                48 => "(turn to 275).",   // keep orb -> tunnel on (sol8)
                52 => "(turn to 297).",   // sarcophagus -> garlic (sol8)
                68 => "(turn to 19).",    // pass by Banshee unharmed (sol8)
                69 => "(turn to 348).",   // farewell -> sword drawn in time (sol8)
                71 => "(turn to 390). [Also turn to 149].", // threaten -> green pills (sol8); pay-herbs -> 149 (p32R gap)
                72 => "(turn to 288).",   // skull tunnel -> Frost Giant (sol8)
                74 => "(turn to 317).",   // rose -> leave cave (sol8)
                75 => "(turn to 258).",   // still alive -> Healer mask ritual (sol8)
                96 => "(turn to 110).",   // set off for Stonebridge (sol8)
                110 => "(turn to 399). [Also turn to 257.]", // rest night -> morning; Unlucky -> 257 (p47R gap)
                146 => "(turn to 400).",  // cured of Death Spell -> beautiful day (sol8)
                147 => "(turn to 101).",  // red pot -> opposite door (sol8)
                148 => "(turn to 368).",  // junction right -> locked door (sol8)
                162 => "(turn to 50).",   // reach far bank -> Wild Hill Man (sol8)
                163 => "(turn to 363).",  // avalanche -> ice tunnel right (sol8)
                166 => "(turn to 259).",  // tunnel draw straws (sol8)
                235 => "(turn to 171).",  // Sentinel win -> torchlit tunnel (sol8)
                275 => "(turn to 166).",  // tunnel left (sol8)
                280 => "(turn to 50).",   // inlet -> east to the fire (p89L gap)
                281 => "(turn to 169).",  // crawl out -> wooden hut (sol8)
                282 => "(turn to 193).",  // dive -> bolt hits wall (sol8)
                285 => "(turn to 298).",  // rest -> ornate shield (sol8)
                288 => "(turn to 112).",  // Frost Giant win -> sling test (sol8)
                289 => "(turn to 158).",  // green liquid -> revitalized (sol8)
                291 => "(turn to 3). [Also turn to 358.]", // star-win escape, mirrors S15
                346 => "(turn to 205).",  // river leap -> far bank (sol8)
                50 => "(turn to 320).",   // Wild Hill Man win -> back over rocks (sol8)
                _ => content,
            };
        }
        // Truncated tails: the slice ends mid-exit (number lives in the next
        // slice), so complete it. Suffix-checked, never fires on whole text.
        if (sectionNumber == 4 && content.TrimEnd().EndsWith("(burn", StringComparison.OrdinalIgnoreCase))
            content = content.TrimEnd()[..^"(burn".Length] + "(turn to 235)."; // investigate shape (sol8; tail cut)
        if (sectionNumber == 97 && content.TrimEnd().EndsWith("(tum to", StringComparison.OrdinalIgnoreCase))
            content += " 198).";          // leave cave (mirrors S317; number cut)
        if (sectionNumber == 130 && content.TrimEnd().EndsWith("(tum to", StringComparison.OrdinalIgnoreCase))
            content += " 338).";          // next tunnel (dump p52L; number cut)
        if (sectionNumber == 253 && content.TrimEnd().EndsWith("{turn to", StringComparison.OrdinalIgnoreCase))
            content += " 135).";          // past last junction (sol8; number cut)
        if (sectionNumber == 257 && content.TrimEnd().EndsWith("(tum to", StringComparison.OrdinalIgnoreCase))
            content += " 363).";          // climb again (dump p83L; number cut)
        if (sectionNumber == 260 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 370).";          // tunnel right (sol8; brace cut)
        if (sectionNumber == 295 && content.TrimEnd().EndsWith("(fun", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 137)."; // other tunnel (sol8; number cut)
        if (sectionNumber == 317 && content.TrimEnd().EndsWith("{turn to", StringComparison.OrdinalIgnoreCase))
            content += " 198).";          // leave cave (sol8; number cut)
        if (sectionNumber == 356 && content.TrimEnd().EndsWith("(turn to", StringComparison.OrdinalIgnoreCase))
            content += " 198).";          // leave cave, mirrors S317 (number cut)
        // Non-empty slots missing their true exit (dup body or cut choices).
        if (sectionNumber == 5 && !content.Contains("turn to 68", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 68).";  // resist Banshee -> pass by unharmed (sol8; slot holds S12 chest)
        if (sectionNumber == 100 && !content.Contains("turn to 273", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 273). [Also turn to 181.]"; // bridge Lucky/Unlucky (p44L gap)
        if (sectionNumber == 113 && !System.Text.RegularExpressions.Regex.IsMatch(content, @"turn to 15\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            content += " (turn to 15).";  // square disc wins (sol8; choices cut from slice; \b avoids matching 152)
        if (sectionNumber == 204 && !content.Contains("turn to 372", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 372)."; // Elemental slam (p93L dump)
        if (sectionNumber == 339 && !content.Contains("turn to 216", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 216)."; // fire iron ball at globe (sol8; slice truncated)
        if (sectionNumber == 370 && !content.Contains("turn to 33", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 33).";  // run for the tunnel (dump p110R; guards truncation)
        if (sectionNumber == 371 && !content.Contains("turn to 259", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 259)."; // Lucky -> casket (dump p111L; tail garbled)
        if (sectionNumber == 56 && !content.Contains("turn to 395", StringComparison.OrdinalIgnoreCase))
            content += " (turn to 395)."; // nod at Mountain Elf (sol8)
        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair
            // FIRST, so targets below are post-shared text (e.g. 7g->79).
            391 => content.Replace("turn to 244", "turn to 249", StringComparison.OrdinalIgnoreCase), // no-frostbite spear throw (4->9)
            249 => content.Replace("turn to 210", "turn to 219", StringComparison.OrdinalIgnoreCase), // spear hits -> kill Yeti (0->9)
            54 => content.Replace("turn to gx", "turn to 91", StringComparison.OrdinalIgnoreCase), // log walk (x->1)
            60 => content.Replace("furm Eo 116", "turn to 116", StringComparison.OrdinalIgnoreCase), // gaze resisted (dump p29R)
            63 => content.Replace("turn to gé", "turn to 96", StringComparison.OrdinalIgnoreCase), // not-drink -> set off again (sol8; é->6)
            77 => content.Replace("tum io 378", "turn to 378", StringComparison.OrdinalIgnoreCase), // no spear (dump p35L)
            97 => content.Replace("Turn to 79", "turn to 74", StringComparison.OrdinalIgnoreCase), // flute, mirrors S194/317/356
            111 => content.Replace("turn to joo", "turn to 300", StringComparison.OrdinalIgnoreCase), // pretend worship (J->3)
            131 => content.Replace("turn 0 26", "turn to 26", StringComparison.OrdinalIgnoreCase)
                .Replace("turn to 280", "turn to 289", StringComparison.OrdinalIgnoreCase), // row -> Dark Elf; wait -> green liquid (sol8; 0->9)
            158 => content.Replace("Turn to 273", "turn to 173", StringComparison.OrdinalIgnoreCase), // revitalized -> south (2->1)
            159 => content.Replace("3381", "338", StringComparison.OrdinalIgnoreCase), // cursed ring -> next tunnel (stray digit)
            189 => content.Replace("turn to 304", "turn to 309", StringComparison.OrdinalIgnoreCase), // win -> gray pot (0->9)
            216 => content.Replace("turn to 3735", "turn to 373", StringComparison.OrdinalIgnoreCase), // miss -> Frost Giant (stray digit)
            19 => content.Replace("tum 0 206", "turn to 206", StringComparison.OrdinalIgnoreCase), // no silver (dump p19L)
            4 => content.Replace("(burn bo 235)", "(turn to 235)", StringComparison.OrdinalIgnoreCase), // investigate shape (sol8)
            356 => content.Replace("tum to oy", "turn to 97", StringComparison.OrdinalIgnoreCase), // open book (sol8; o->9,y->7)
            364 => content.Replace("(urn to 115)", "(turn to 115)", StringComparison.OrdinalIgnoreCase), // up the river valley (sol8)
            353 => content.Contains("turn to 379", StringComparison.OrdinalIgnoreCase) ? content : content + " (turn to 379).", // lead the way (sol8)
            _ => content,
        };
    }
}
