using System.Text.RegularExpressions;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF20 Sword of the Samurai (Mark Smith and Jamie Thomson, 1986). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across PDF pages 12-130;
/// each PDF page is a two-up spread split into L/R halves). Intro uses the cover blurb (1)
/// plus BACKGROUND (11); the curated intro text ships in Intros/ff20.txt.
///
/// <para><b>Layout notes.</b> Folio ranges head most spreads ("54-55") with section headers
/// further down, so headers are not at side tops. Systematic digit confusion: 9-&gt;0/5
/// ("295"-&gt;"205", "296"-&gt;"256", "297"-&gt;"207", "298"-&gt;"208", "305"-&gt;"375"
/// for S375, "305"-&gt;"395" for S395), 5-&gt;3 ("59"-&gt;"39" style), 3-&gt;1 ("373"-&gt;"173"),
/// 1-&gt;7, 8-&gt;6 ("184"-&gt;"164"), single letters ("a3" = 13, "zg" = 29, "z0" = 20,
/// "2z" = 22, "2%" = 25, "g1" = 91, "g2" = 92, "a5" = 95/96, "a7" = 97, "B3" = 83,
/// "By" = 85, "Ha" = 81, "Eo" = 80, "go" = 90, "br" = 64, "c#" = 60, "Ls" = 115,
/// "Fi" = 74, "To" = 70, "bg" = 69, "ay" = 197, "gs" = 198, "aB1" = 281, "27F" = 275,
/// "a7i" = 271, "28g" = 289, "2bg" = 266, "21%" = 71, "zbo" = 260, "z1R" = 218,
/// "T1211" = 121, "3bz" = 362, "37a" = 379, "37L" = 372, "10x" = 109, "I55" = 155,
/// "1I2"/"2q0" folio noise), and headers lost in illustration gaps (S5, S6, S9, S14,
/// S18, S32, S43, S44, S66, S79, S81, S82, S84, S89, S148, S157, S181, S199, S221,
/// S227, S268, S272, S287, S299, S300, S301, S304, S306, S307, S313, S343, S354,
/// S369, S379, S380, S385, S388, S389, S395, S396, S398, S400). The book reprints
/// whole scenarios under new numbers (S15/S187 Rokuro-Kubi, S28/S218/S244-tail/S304/
/// S396 cell captures, S14/S284/S288 Shikome guards, S129/S183 Kappa measures,
/// S54/S74 Dai-Oni asks, S93/S281 charcoal ambushes, S271/S357-Seventh-Seal legion
/// text). Each placement was confirmed against the transcript and, where ambiguous,
/// the rendered PDF page.</para>
/// </summary>
public sealed class SwordOfTheSamuraiParser : ManifestDungeonWorldParser
{
    public SwordOfTheSamuraiParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "SwordOfTheSamurai";
    protected override string TitleMatch => "Sword of the Samurai";
    protected override string Slug => "ff20_sword_of_the_samurai";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff20.json";

    /// <summary>Cover blurb plus BACKGROUND; the rules pages (5-10) are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 11 };

    /// <summary>FF20 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Written by Mark Smith and Jamie Thomson, not Jackson/Livingstone.</summary>
    protected override string Author => "Mark Smith and Jamie Thomson";

    /// <summary>FF20 prints sections scrambled, so bounds run pin-to-pin in the book.</summary>
    protected override bool ManifestInTranscriptOrder => true;

    /// <summary>FF20 keeps exit-fragment retention (exit numbers land on their own lines).</summary>
    protected override bool KeepExitFragments => true;

    /// <summary>Book-scoped exit repair, applied after the shared TurnToRepair.</summary>
    protected override string RepairExits(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, content);

    /// <summary>
    /// True when <paramref name="content"/> already carries a real exit to <paramref name="target"/>.
    /// The trailing (?!\d) matters: this scan reads stray extra digits, so a substring check would
    /// wrongly conclude the exit is present and skip the restoration.
    /// </summary>
    private static bool HasExit(string content, int target) =>
        Regex.IsMatch(content, $@"\bturn\s+to\s+{target}(?!\d)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Per-section corrections for exit targets the scan lost or misread. Every rule below is
    /// confirmed against the arek sol19 solution chain or the rendered PDF page; anything
    /// unresolved is deliberately left verbatim rather than guessed.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Restored only when absent, so a future better scan needs no edit. Every edge below
        // is a consecutive sol19 pair (the player physically turned from parent to target) or a
        // print-verified option whose verb garble defeats the cleaner (S59's 111/123/70,
        // S74's verb-only trio, S233's "um two 357").
        (int parent, int target, string note)[] codeEdges =
        {
            (1, 10, "fork west -> Forest of Shadows (sol19: 1, 10)"),
            (1, 29, "fork east -> ford (sol19: 1, 29)"),
            (5, 71, "peasants hospitality -> mend house (sol19: 5, 71)"),
            (5, 310, "blaze a trail -> Tsietsin chase (print: S5 options)"),
            (13, 31, "Kappa win -> surrounded (sol19: 13, 31)"),
            (17, 107, "map -> scarlet pagoda route (sol19: 17, 107)"),
            (17, 125, "west dyke -> S125 (print: S17 options)"),
            (21, 49, "commit to water -> S49 (render p18: 21, 49)"),
            (22, 292, "fight Dai-Oni alone (sol19: 22, 292)"),
            (22, 394, "send Ki-Rin (print: S22 options)"),
            (29, 43, "east road -> insulting youth (sol19: 29, 43)"),
            (35, 173, "run -> trident throw (print: S35 options)"),
            (35, 183, "fight -> Kappa measures (print: S35 options)"),
            (35, 129, "basin discovery -> flat-tops (sol19: 41, 35, 129)"),
            (41, 35, "leap -> mend (sol19: 41, 35)"),
            (43, 59, "punish youth (sol19: 43, 59)"),
            (43, 77, "walk on (print: S43 options)"),
            (59, 111, "abase -> oath (sol19: 59, 111)"),
            (59, 123, "arrow -> S123 (print: S59 options)"),
            (59, 70, "follow her -> trail (print: S59 options)"),
            (66, 340, "Ki-Rin desc -> gaze (print: S66, S340)"),
            (75, 95, "fountain open -> fill ewer (sol19: 75, 95)"),
            (76, 152, "know secret -> shadow (sol19: 76, 152)"),
            (78, 348, "Phoenix -> Ruby path (sol19: 78, 348)"),
            (78, 360, "no Ruby -> Phoenix fight (print: S78 options)"),
            (95, 243, "ascend stronghold (sol19: 95, 243)"),
            (96, 8, "Eleanor ally -> Hub (sol19: 96, 8)"),
            (98, 256, "swamp serpent -> touch choice (sol19: 98, 258 as 256)"),
            (112, 2, "forest Tatsu -> riddle (print: S112, S2)"),
            (119, 37, "leg hack -> trap (print: S119, S37)"),
            (191, 70, "helmet follow -> trail (print: S191, S70)"),
            (191, 8, "helmet flight -> Hub (print: S191)"),
            (220, 76, "Jizo banish -> secret (print: S220, S76)"),
            (126, 48, "vacant woman -> item use (sol19: 126, 48)"),
            (131, 199, "reel back -> advance (sol19: 131, 199)"),
            (152, 174, "Honour<5 -> Shadow Demon (sol19: 152, 174)"),
            (163, 195, "journey on -> Ginsei (sol19: 163, 195)"),
            (172, 278, "send Ki-Rin (print: S172 options)"),
            (174, 212, "demon dead -> stride (sol19: 174, 212)"),
            (175, 15, "quit village -> Rokuro (sol19: 175, 15)"),
            (189, 220, "Tatsu amulet (print: S189 options)"),
            (189, 270, "no amulet (print: S189 options)"),
            (199, 292, "advance -> fight Dai-Oni (print: S199)"),
            (206, 270, "no amulet -> Shura (sol19: 206, 270)"),
            (206, 220, "have amulet (print: S206 options)"),
            (208, 172, "send Tiger (print: S208 options)"),
            (208, 254, "no allies left (print: S208 options)"),
            (219, 5, "Vale -> peasants (sol19: 219, 5)"),
            (228, 266, "no allies left (print: S228 options)"),
            (233, 357, "Iaijutsu -> far bank (sol19: 233, 357)"),
            (235, 400, "Ikiru destroyed -> victory (sol19: 235, 400)"),
            (27, 400, "Ikiru defeat -> victory (print: S27 win)"),
            (290, 8, "serpent rod -> Hub (print: S290)"),
            (299, 137, "garden -> gatekeeper (sol19: 299, 137)"),
            (241, 275, "grins -> duel (sol19: 241, 275)"),
            (255, 218, "slow win -> cell (print: S255 options)"),
            (270, 76, "Shura fades -> secret (sol19: 270, 76)"),
            (273, 329, "cave -> trembling stops (sol19: 273, 329)"),
            (101, 163, "oath -> clemency (print: S101 tail, S163)"),
            (399, 383, "hut -> chest (print: S399, S383)"),
            (383, 350, "hovel -> cupboard chest (print: S383, S350)"),
            (350, 359, "chest -> puppet (print: S350, S359)"),
            (320, 22, "send Eleanor (sol19: 320, 22)"),
            (340, 8, "Ki-Rin ally -> Hub (print: S340)"),
            (348, 8, "Phoenix ally -> Hub (print: S349)"),
            (352, 8, "back at Hub (print: S352)"),
            (369, 299, "pagoda -> garden (sol19: 369, 299)"),
            (379, 341, "accept duel (sol19: 379, 341)"),
            (379, 342, "run right (print: S379 gaoler options)"),
            (381, 227, "hills -> ford (sol19: 381, 227)"),
            (391, 381, "antler -> hills (sol19: 391, 381)"),
            (54, 150, "help quest (render p30: 54, 150)"),
        };
        foreach (var (parent, target, _) in codeEdges)
            if (sectionNumber == parent && !HasExit(content, target))
                content += $" (turn to {target}).";

        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair FIRST, so the text
            // matched here is already post-shared-repair. Plain string Replace is used (proven
            // in-pipeline); every target below was read verbatim from pipeline output and every
            // corrected number is sol19- or render-proven.
            15 => content.Replace("turn to 10 153.", "turn to 153.",
                StringComparison.OrdinalIgnoreCase), // Rokuro win (sol19: 15, 153)
            20 => content.Replace("Lucky, turn to 283", "Lucky, turn to 282",
                StringComparison.OrdinalIgnoreCase) // gaoler Lucky (render p18: 20, 282)
                .Replace("Unlucky, turn to 206", "Unlucky, turn to 296",
                StringComparison.OrdinalIgnoreCase), // gaoler Unlucky (render p18: 20, 296)
            39 => content.Replace("(turn to 3807", "(turn to 380",
                StringComparison.OrdinalIgnoreCase), // grab weapon and run (print: S39, S380)
            48 => content.Replace("Waters of Knowledge? Turn to gh", "Waters of Knowledge? Turn to 96",
                StringComparison.OrdinalIgnoreCase), // Waters -> Eleanor (sol19: 48, 96)
            54 => content.Replace("Turn to 208", "Turn to 206",
                StringComparison.OrdinalIgnoreCase) // Secret (render p30: 54, 206)
                .Replace("Tum tn 138", "Turn to 188",
                StringComparison.OrdinalIgnoreCase), // slay Ikiru (render p30: 54, 188)
            74 => content.Replace("Turn lo 206", "Turn to 206",
                StringComparison.OrdinalIgnoreCase) // Secret (sol19: 74, 206; verb-only garble)
                .Replace("Turn io 188", "Turn to 188",
                StringComparison.OrdinalIgnoreCase) // slay Ikiru (verb-only garble)
                .Replace("Turn lo 150", "Turn to 150",
                StringComparison.OrdinalIgnoreCase), // help quest (verb-only garble)
            75 => content.Replace("Turn kegs.", "Turn to 95.",
                StringComparison.OrdinalIgnoreCase), // fountain open (sol19: 75, 95)
            27 => content.Replace("turn to 3 30g.", "turn to 309.",
                StringComparison.OrdinalIgnoreCase), // Ikiru Unlucky (print: S27, S309)
            8 => content.Replace("Tum io zo", "Turn to 30",
                StringComparison.OrdinalIgnoreCase) // Pinnacle (print: S8 doors, 30)
                .Replace("Endless Sands of Akhon? turn to 5", "Endless Sands of Akhon? turn to 78",
                StringComparison.OrdinalIgnoreCase), // Endless Sands (sol19: 8, 78)
            121 => content.Replace("or less, turn to22.", "or less, turn to 222.",
                StringComparison.OrdinalIgnoreCase), // low Honour (print: S121, S222)
            172 => content.Replace("The Tatsu? Turn to 204", "The Tatsu? Turn to 208",
                StringComparison.OrdinalIgnoreCase), // send Tatsu (print: S172, S208)
            195 => content.Replace("Turn to 204,", "Turn to 209,",
                StringComparison.OrdinalIgnoreCase), // Ginsei face (sol19: 195, 209)
            203 => content.Replace("turn to 1687.", "turn to 168.",
                StringComparison.OrdinalIgnoreCase), // Rokuro win (print: S203, S168)
            208 => content.Replace("The Golden Company? Turn to 183", "The Golden Company? Turn to 184",
                StringComparison.OrdinalIgnoreCase), // send Company (print: S208, S184)
            212 => content.Replace("{turn to 208)7", "{turn to 298)7",
                StringComparison.OrdinalIgnoreCase), // stride boldly (sol19: 212, 298)
            213 => content.Replace("(turn to 3137", "(turn to 31",
                StringComparison.OrdinalIgnoreCase), // ford battle -> surrounded (print: S213, S31)
            228 => content.Replace("Tum to 3130", "Turn to 330",
                StringComparison.OrdinalIgnoreCase), // send Eleanor (print: S228, S330)
            255 => content.Replace("tired bul victorious, Turn to 303.", "tired bul victorious, Turn to 393.",
                StringComparison.OrdinalIgnoreCase), // barge win -> talk (sol19: 255, 393)
            275 => content.Replace("turn to 390.", "turn to 399.",
                StringComparison.OrdinalIgnoreCase), // duel win -> hut (sol19: 275, 399)
            295 => content.Replace("Turn to 7.", "Turn to 27.",
                StringComparison.OrdinalIgnoreCase), // crest descent (sol19: 295, 27)
            298 => content.Replace("Turn to 22.", "Turn to 121.",
                StringComparison.OrdinalIgnoreCase), // vaporize -> throne (sol19: 298, 121)
            320 => content.Replace("turn to 5 forward", "lumbers forward",
                StringComparison.OrdinalIgnoreCase), // de-garble "lumbers forward" (print p105)
            303 => content.Replace("turn to 287.", "turn to 251.",
                StringComparison.OrdinalIgnoreCase), // brass key -> find (sol19 287 as 251)
            357 => content.Replace("turn to 295.", "turn to 395.",
                StringComparison.OrdinalIgnoreCase), // far bank -> mound (sol19: 357, 395)
            397 => content.Replace("turn to 31.", "turn to 391.",
                StringComparison.OrdinalIgnoreCase) // elk win -> antler (sol19: 397, 391)
                .Replace("(turn to 338)", "(turn to 398)",
                StringComparison.OrdinalIgnoreCase), // mission -> circle castle (print: S398)
            _ => content,
        };
    }
}
