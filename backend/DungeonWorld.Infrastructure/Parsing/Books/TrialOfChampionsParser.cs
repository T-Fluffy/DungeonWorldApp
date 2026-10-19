using System.Text.RegularExpressions;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF21 Trial of Champions (Ian Livingstone, 1986). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across PDF pages 13-104;
/// each PDF page is a two-up spread split into L/R halves). Sections print scrambled,
/// so bounds run pin-to-pin in transcript order. Intro uses the cover blurb (1) plus
/// BACKGROUND (10-11); the curated intro text ships in Intros/ff21.txt.
///
/// <para><b>Layout notes.</b> Folio ranges head most spreads ("113-114") with section
/// headers further down. Systematic digit confusion (9-&gt;0/5: "295"-&gt;"205",
/// "296"-&gt;"256", "297"-&gt;"207", "298"-&gt;"208"; 5-&gt;3; 3-&gt;1; 8-&gt;6;
/// single letters: "a1" = 91, "gz" = 92, "oH" = 78, "10h" = 106, "26H" = 268/269,
/// "2z"/"2%"/"g1"/"g2"/"a5"/"a7"/"B3" style). Headers lost in illustration gaps are
/// pinned at body starts. The book reprints whole scenarios under new numbers
/// (S2/S391 centipede-cave). Each placement was confirmed against the transcript
/// and, where ambiguous, the rendered PDF page. One audit fix: S119 had been pinned
/// inside S156 on p50 (a stale pointing line); it was re-anchored to its own header
/// on p41, which restored S156's printed exits 199/222 and removed a phantom S118->288.</para>
/// </summary>
public sealed class TrialOfChampionsParser : ManifestDungeonWorldParser
{
    public TrialOfChampionsParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "TrialOfChampions";
    protected override string TitleMatch => "Trial of Champions";
    protected override string Slug => "ff21_trial_of_champions";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff21.json";

    /// <summary>Cover blurb plus BACKGROUND; the rules pages are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 11, 12 };

    /// <summary>FF21 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Written by Ian Livingstone.</summary>
    protected override string Author => "Ian Livingstone";

    /// <summary>FF21 prints sections scrambled, so bounds run pin-to-pin in the book.</summary>
    protected override bool ManifestInTranscriptOrder => true;

    /// <summary>FF21 keeps exit-fragment retention (exit numbers land on their own lines).</summary>
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
    /// confirmed against the arek sol20 solution chain or the rendered PDF page; anything
    /// unresolved is deliberately left verbatim rather than guessed.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Restored only when absent, so a future better scan needs no edit. Every edge below
        // is a consecutive sol20 pair (the player physically turned from parent to target) or a
        // print-verified option whose verb garble defeats the cleaner.
        (int parent, int target, string note)[] codeEdges =
        {
            (1, 292, "escape guard (print: S1 options)"),
            (16, 10, "cave walk -> armour (print: S16 options)"),
            (16, 88, "cave climb -> rope (sol20: 16, 88)"),
            (18, 57, "storeroom loaf -> file (sol20: 18, 57)"),
            (18, 81, "storeroom loaf -> brass (print: S18 options)"),
            (18, 305, "storeroom nothing -> steps (print: S18 options)"),
            (19, 70, "medallion neck -> nothing (sol20: 19, 70)"),
            (19, 324, "climb rope (print: S19, truncated exit)"),
            (21, 50, "leap steps (print: S21 options)"),
            (35, 82, "rings gold -> junction (print: S35 options)"),
            (35, 249, "rings instruct -> wizard (sol20: 35, 249)"),
            (35, 286, "rings blank -> net (print: S35 options)"),
            (38, 8, "fall -> boards (print: S38, S8)"),
            (37, 118, "fall sprain -> Spider (sol20: 37, 118)"),
            (37, 397, "abandon man -> funnel (render: p23 'tom to 397')"),
            (43, 159, "runes -> box (print: S43 options)"),
            (43, 187, "armour -> silver (sol20: 43, 187)"),
            (43, 222, "door -> tunnel (print: S43 options)"),
            (43, 287, "shield -> door (print: S43 options)"),
            (44, 155, "hole walk -> nuts (sol20: 44, 155)"),
            (44, 340, "hole climb -> warrior (print: S44 options)"),
            (48, 9, "lever three -> lever (print: S48 options)"),
            (48, 57, "lever one -> loaf (print: S48 options)"),
            (48, 97, "lever two -> fruit (print: S48 options)"),
            (48, 114, "lever four -> float (print: S48 options)"),
            (48, 377, "levers pull -> clock (sol20: 48, 377)"),
            (52, 36, "straw -> junction (sol20: 52, 36)"),
            (52, 82, "leave -> junction (print: S52 options)"),
            (57, 184, "loaf beef -> beef (sol20: 57, 184)"),
            (57, 305, "climb -> steps (print: S57 options)"),
            (58, 106, "talisman lose -> file (sol20: 58, 106)"),
            (59, 91, "web -> dice (sol20: 59, 91)"),
            (65, 201, "food eat -> junction (render: p29)"),
            (65, 211, "food walk -> tunnel (print: S65 options)"),
            (60, 263, "food -> dark (sol20: 60, 263)"),
            (66, 189, "net -> duel (print: S66 options)"),
            (66, 255, "net -> fail (print: S66 options)"),
            (61, 261, "net -> tunnel (sol20: 61, 261)"),
            (68, 227, "back -> cave (print: S68, truncated exit)"),
            (68, 289, "down -> spikes (print: S68 options)"),
            (68, 318, "door -> Liche (sol20: 68, 318)"),
            (68, 338, "open -> boards (print: S68 options)"),
            (69, 109, "fang walk -> idol (print: S69 options)"),
            (69, 182, "fang open -> idol (sol20: 69, 182)"),
            (70, 328, "climb -> pit (sol20: 70, 328)"),
            (71, 188, "daylight -> victory (sol20: 71, 188)"),
            (77, 132, "fence fail -> slip (print: S77 options)"),
            (77, 245, "fence pass -> ring (sol20: 77, 245)"),
            (85, 135, "tongue back -> heal (print: S85 options)"),
            (85, 364, "tongue down -> Siren (sol20: 85, 364)"),
            (88, 95, "rope axe -> fountain (print: S88 options)"),
            (88, 137, "rope unlucky -> tail (print: S88 options)"),
            (88, 149, "rope cut -> box (sol20: 88, 149)"),
            (88, 333, "rope dove -> rock (print: S88 options)"),
            (91, 332, "dice pass -> door (print: S91 options)"),
            (91, 357, "dice fail -> door (print: S91 options)"),
            (93, 146, "stairs -> rats (sol20: 93, 146)"),
            (94, 9, "right -> lever (print: S94 options)"),
            (94, 297, "left -> arena (sol20: 94, 297)"),
            (96, 213, "rocks clear -> throw (print: S96 options)"),
            (96, 215, "rocks doorway -> horn (sol20: 96, 215)"),
            (96, 375, "rocks walk -> cry (print: S96 options)"),
            (112, 178, "champion refuse -> gold (sol20: 112, 178)"),
            (112, 323, "champion obey -> gold (print: S112 options)"),
            (115, 168, "tunnel -> sunshine (print: S115 options)"),
            (115, 173, "pot -> gas (sol20: 115, 173)"),
            (115, 175, "stay -> faint (print: S115 options)"),
            (117, 224, "bridge -> silver (print: S117, garbled exit)"),
            (118, 64, "Spider slow -> lair (print: S118 options)"),
            (118, 348, "Spider fast -> web (sol20: 118, 348)"),
            (119, 288, "forest trail -> Zombies (sol20: 119, 288)"),
            (125, 11, "Lexus odd -> wise (sol20: 125, 11)"),
            (125, 367, "Lexus even -> liars (print: S125 options)"),
            (126, 28, "run -> junction (print: S126 options)"),
            (126, 100, "fight -> shadow (print: S126 options)"),
            (126, 281, "helmet -> armour (sol20: 126, 281)"),
            (127, 303, "Demon win -> hands (sol20: 127, 303)"),
            (127, 305, "Demon win -> steps (print: S127 options)"),
            (129, 264, "pit back -> rope (sol20: 129, 264)"),
            (129, 269, "pit rope -> axe (print: S129 options)"),
            (129, 351, "pit back -> grate (print: S129 options)"),
            (131, 148, "ball lucky -> counter (print: S131 options)"),
            (131, 210, "ball unlucky -> pit (print: S131 options)"),
            (100, 19, "Strider win -> medallion (print: S100 options)"),
            (141, 208, "Strider win -> cloak (sol20: 141, 208)"),
            (142, 29, "box -> Devil (print: S142 options)"),
            (142, 172, "dust -> leg (print: S142 options)"),
            (142, 300, "phial -> brew (print: S142 options)"),
            (142, 346, "corridor -> bird (print: S142 options)"),
            (146, 15, "rats flawless -> courage (print: S146 options)"),
            (146, 18, "rats win -> storeroom (sol20: 146, 18)"),
            (146, 165, "rats lose -> Luck (print: S146 options)"),
            (149, 388, "box crash -> little (sol20: 149, 388)"),
            (152, 117, "bridge -> tablet (sol20: 152, 117)"),
            (168, 77, "sunshine walk -> fence (sol20: 168, 77)"),
            (173, 68, "gas -> door (sol20: 173, 68)"),
            (173, 150, "tunnel -> branch (print: S173 options)"),
            (173, 307, "cave -> Coldclaw (print: S173 options)"),
            (175, 189, "faint -> duel (sol20: 175, 189)"),
            (176, 127, "wizard win -> Demon (sol20: 176, 127)"),
            (176, 237, "wizard lose -> rest (print: S176 options)"),
            (176, 247, "wizard draw -> frenzy (print: S176 options)"),
            (178, 247, "gold refuse -> Chaos (sol20: 178, 247)"),
            (179, 2, "button one -> steps (print: S179 options)"),
            (179, 12, "buttons -> broom (sol20: 179, 12)"),
            (179, 214, "button two -> mind (print: S179 options)"),
            (186, 142, "Bats win -> brew (sol20: 186, 142)"),
            (187, 126, "shield -> helmet (sol20: 187, 126)"),
            (187, 236, "helmet -> dive (print: S187 options)"),
            (187, 257, "runes -> lever (print: S187 options)"),
            (187, 273, "door -> potion (print: S187 options)"),
            (188, 400, "victory wish -> prize (sol20: 188, 400)"),
            (189, 113, "duel win -> easterner (sol20: 189, 113)"),
            (189, 13, "net win -> riddle (print: S189 options)"),
            (190, 279, "net throw -> Tusker (sol20: 190, 279)"),
            (191, 21, "Siren run -> steps (print: S191 options)"),
            (195, 112, "Siren song -> forest (sol20: 195, 112)"),
            (203, 21, "Siren run -> steps (print: S203 options)"),
            (204, 32, "old man ribs -> initiative (print: S204 options)"),
            (208, 9, "cloak wear -> lever (print: S208 options)"),
            (208, 53, "cloak leave -> wood (sol20: 208, 53)"),
            (208, 89, "cloak helmet -> Orc (print: S208 options)"),
            (208, 97, "cloak runes -> fruit (print: S208 options)"),
            (208, 303, "cloak door -> Demon (print: S208 options)"),
            (208, 351, "cloak six -> sleep (print: S208 options)"),
            (209, 129, "T-junction -> pit (sol20: 209, 129)"),
            (109, 143, "glow -> garden (print: S109 options)"),
            (212, 82, "gauntlet fail -> junction (print: S212 options)"),
            (213, 155, "horn climb -> nuts (print: S213 options)"),
            (213, 259, "horn blow -> horn (print: S213 options)"),
            (215, 394, "horn sling -> torch (sol20: 215, 394)"),
            (219, 252, "win -> Coldclaw (sol20: 219, 252)"),
            (227, 37, "cave -> fall (sol20: 227, 37)"),
            (227, 397, "walk -> funnel (print: S227 options)"),
            (228, 381, "Troll -> sneak (print: S228 options)"),
            (232, 28, "hunchback right -> junction (print: S232 options)"),
            (232, 29, "hunchback advice -> Devil (print: S232 options)"),
            (233, 150, "boots walk -> branch (print: S233 options)"),
            (233, 272, "boots rummage -> skull (print: S233 options)"),
            (235, 378, "teleport -> stairs (print: S235 options)"),
            (236, 21, "dive -> steps (print: S236 options)"),
            (237, 94, "rest -> blind (sol20: 237, 94)"),
            (237, 95, "pole pass -> fountain (print: S237 options)"),
            (238, 125, "pole pass -> Lexus (sol20: 238, 125)"),
            (237, 324, "pole fail -> pit (print: S237, truncated exit)"),
            (239, 248, "pole block -> strike (render: p69)"),
            (239, 238, "pole block -> deflect (render: p69)"),
            (245, 8, "ring walk -> boards (sol20: 245, 8)"),
            (299, 23, "ring walk -> archway (print: S299 options)"),
            (247, 150, "champion frenzy -> branch (print: S247 options)"),
            (247, 180, "champion fight -> box (sol20: 247, 180)"),
            (248, 320, "body -> block (print: S248 options)"),
            (248, 385, "strike -> triumph (print: S248 options)"),
            (249, 286, "wizard blank -> net (print: S249 options)"),
            (249, 358, "wizard second -> last (sol20: 249, 358)"),
            (252, 5, "pot -> crossbow (print: S252 options)"),
            (252, 50, "leave -> steps (print: S252 options)"),
            (252, 115, "room -> pot (sol20: 252, 115)"),
            (252, 269, "cave -> Coldclaw (print: S252 options)"),
            (255, 66, "net pass -> wall (print: S255 options)"),
            (255, 334, "net fail -> grab (print: S255 options)"),
            (258, 133, "humanoid walk -> chase (print: S258 options)"),
            (258, 333, "humanoid open -> rock (print: S258 options)"),
            (259, 14, "horn -> spiders (print: S259 options)"),
            (260, 61, "phial -> net (print: S260 options)"),
            (264, 190, "rope follow -> net (sol20: 264, 190)"),
            (264, 351, "rope climb -> grate (print: S264 options)"),
            (261, 73, "southerner -> sand (print: S261 options)"),
            (59, 94, "charge -> trap (print: S59 options)"),
            (59, 150, "charge -> tunnel (print: S59 options)"),
            (269, 172, "axe win -> leg (print: S269 options)"),
            (269, 253, "axe slow -> leg (print: S269 options)"),
            (270, 2, "Centipede win -> steps (print: S270 options)"),
            (271, 8, "steps land -> stairs (render: p75 'Turn to 8')"),
            (272, 205, "skull lift -> medallion (render: p75)"),
            (272, 150, "skull leave -> tunnel (render: p75)"),
            (273, 232, "potion -> bones (print: S273 options)"),
            (274, 72, "defeat -> servant (print: S274 options)"),
            (275, 56, "arena sword -> tactics (print: S275 options)"),
            (275, 175, "arena trident -> faint (sol20: 275, 175)"),
            (277, 227, "fence left -> cave (print: S277 options)"),
            (277, 285, "fence right -> chest (print: S277 options)"),
            (278, 367, "rings -> bench (print: S278 options)"),
            (279, 330, "Tusker win -> tomb (print: S279 options)"),
            (279, 339, "Tusker win -> pig (sol20: 279, 339)"),
            (280, 398, "graze -> bridge (print: S280 options)"),
            (281, 3, "armour -> Noy (sol20: 281, 3)"),
            (282, 34, "ring -> trunk (print: S282 options)"),
            (282, 44, "ring -> hole (print: S282 options)"),
            (284, 141, "climb -> wall (print: S284 options)"),
            (285, 47, "chest walk -> urns (print: S285 options)"),
            (285, 194, "chest open -> veg (print: S285 options)"),
            (287, 156, "door open -> corridor (print: S287 options)"),
            (287, 222, "door walk -> pit (print: S287 options)"),
            (288, 185, "Zombies fight -> death (print: S288 options)"),
            (288, 226, "Zombies jump -> plummet (print: S288 options)"),
            (288, 347, "Zombies fight -> pit (sol20: 288, 347)"),
            (289, 87, "spikes phial -> spiders (print: S289 options)"),
            (289, 231, "spikes none -> river (print: S289 options)"),
            (290, 391, "spear win -> cave (print: S290 options)"),
            (292, 22, "flog -> Arena (print: S292 options)"),
            (294, 217, "dog win -> whistle (print: S294 options)"),
            (295, 34, "trunk -> Orc (print: S295 options)"),
            (295, 133, "trunk -> chase (print: S295 options)"),
            (296, 149, "axe cut -> rope (print: S296 options)"),
            (296, 388, "axe walk -> little (print: S296 options)"),
            (297, 131, "arena -> ball (sol20: 297, 131)"),
            (297, 368, "arena left -> idol (print: S297 options)"),
            (297, 381, "arena swing -> ruby (print: S297 options)"),
            (298, 362, "triumph -> grate (print: S298 options)"),
            (300, 124, "box -> trapping (print: S300 options)"),
            (300, 346, "corridor -> bird (sol20: 300, 346)"),
            (301, 30, "tripwire mid -> steps (print: S301 options)"),
            (301, 89, "tripwire dice -> Orc (print: S301 options)"),
            (301, 280, "tripwire six -> sleep (print: S301 options)"),
            (303, 20, "Demon door -> movement (print: S303 options)"),
            (303, 81, "Demon hands -> clock (sol20: 303, 81)"),
            (303, 94, "Demon lever -> horn (print: S303 options)"),
            (303, 220, "Demon wizard -> Jizo (print: S303 options)"),
            (304, 29, "bridge pay -> Devil (print: S304 options)"),
            (304, 192, "bridge walk -> Orc (print: S304 options)"),
            (304, 355, "bridge climb -> ledge (sol20: 304, 355)"),
            (305, 43, "stairs door -> armour (print: S305 options)"),
            (305, 373, "stairs lid -> rope (sol20: 305, 373)"),
            (306, 85, "tongue win -> fence (print: S306 options)"),
            (307, 120, "Coldclaw walk -> pit (print: S307 options)"),
            (307, 222, "Coldclaw else -> pit (print: S307 options)"),
            (307, 233, "Coldclaw boots -> skull (print: S307 options)"),
            (308, 72, "loss -> servant (print: S308 options)"),
            (309, 58, "nuts -> file (sol20: 309, 58)"),
            (310, 78, "trip lucky -> race (print: S310 options)"),
            (310, 225, "trip unlucky -> trip (print: S310 options)"),
            (311, 55, "lamp lose -> idol (print: S311 options)"),
            (311, 262, "lamp win -> passed (print: S311 options)"),
            (312, 221, "runes fail -> box (print: S312 options)"),
            (313, 20, "crossbow walk -> movement (print: S313 options)"),
            (313, 104, "crossbow dodge -> fruit (print: S313 options)"),
            (314, 228, "painting -> Troll (sol20: 314, 228)"),
            (315, 2, "steps movement -> riddle (print: S315 options)"),
            (315, 195, "steps contestant -> talk (print: S315 options)"),
            (317, 379, "bracelet -> junction (print: S317 options)"),
            (318, 212, "Liche glove -> pole (sol20: 318, 212)"),
            (318, 363, "Liche attack -> panel (print: S318 options)"),
            (319, 98, "tunnel door -> swamp (print: S319 options)"),
            (319, 232, "tunnel walk -> hunchback (print: S319 options)"),
            (320, 140, "block pass -> triumph (print: S320 options)"),
            (320, 274, "block fail -> deflect (print: S320 options)"),
            (321, 133, "breastplate -> chase (print: S321 options)"),
            (323, 39, "gold helmet -> bolt (print: S323 options)"),
            (323, 72, "gold bare -> servant (print: S323 options)"),
            (325, 326, "bite -> hack (print: S325 options)"),
            (325, 123, "hack win -> lair (print: S325 options)"),
            (326, 150, "Devil win -> valley (print: S326 options)"),
            (328, 152, "pit dive -> bridge (sol20: 328, 152)"),
            (328, 329, "pit pay -> King (print: S328 options)"),
            (328, 331, "pit back -> antidote (print: S328 options)"),
            (329, 209, "King win -> junction (print: S329 options)"),
            (331, 315, "antidote -> steps (print: S331 options)"),
            (332, 219, "door burst -> tunnel (print: S332 options)"),
            (333, 96, "rock investigate -> doorway (print: S333 options)"),
            (333, 375, "rock door -> cry (print: S333 options)"),
            (335, 188, "acid run -> victory (print: S335 options)"),
            (336, 76, "boards unlucky -> Arena (print: S336 options)"),
            (336, 122, "boards lucky -> door (print: S336 options)"),
            (337, 198, "wind hold -> horn (print: S337 options)"),
            (339, 167, "pig win -> person (sol20: 339, 167)"),
            (341, 13, "pockets -> riddle (print: S341 options)"),
            (342, 141, "steps land -> swim (print: S342 options)"),
            (344, 221, "rune -> box (print: S344 options)"),
            (345, 35, "rings nine -> place (sol20: 345, 35)"),
            (345, 392, "rings eleven -> liars (print: S345 options)"),
            (346, 59, "bird open -> web (sol20: 346, 59)"),
            (346, 150, "bird walk -> branch (print: S346 options)"),
            (347, 114, "pit helmet -> float (print: S347 options)"),
            (347, 226, "pit bare -> plummet (print: S347 options)"),
            (348, 397, "Billybob -> funnel (sol20: 348, 397)"),
            (349, 196, "grapple win -> listen (print: S349 options)"),
            (350, 44, "blow walk -> hole (print: S350 options)"),
            (351, 74, "grate lift -> worm (print: S351 options)"),
            (351, 174, "grate walk -> rope (print: S351 options)"),
            (352, 25, "caveman win -> Trialmaster (print: S352 options)"),
            (352, 72, "caveman wrong -> servant (print: S352 options)"),
            (353, 19, "Orc unlucky -> medallion (print: S353 options)"),
            (353, 139, "Orc lucky -> trunk (print: S353 options)"),
            (354, 38, "barrier dust -> straw (print: S354 options)"),
            (354, 238, "barrier dust -> pole (sol20: 354, 238)"),
            (354, 330, "barrier none -> tomb (print: S354 options)"),
            (355, 100, "ledge fall -> shadow (sol20: 355, 100)"),
            (355, 200, "ledge pass -> swim (print: S355 options)"),
            (355, 335, "ledge fail -> acid (print: S355 options)"),
            (356, 236, "Tentallus win -> dive (print: S356 options)"),
            (357, 219, "door burst -> tunnel (print: S357 options)"),
            (358, 176, "sequence known -> wizard (sol20: 358, 176)"),
            (358, 284, "sequence blank -> climb (print: S358 options)"),
            (359, 60, "slime trunk -> trap (print: S359 options)"),
            (359, 263, "slime passage -> dark (print: S359 options)"),
            (360, 136, "race reserve -> rest (sol20: 360, 136)"),
            (361, 30, "tripwire unlucky -> pit (print: S361 options)"),
            (361, 164, "tripwire lucky -> tripwire (print: S361 options)"),
            (362, 329, "grate win -> King (print: S362 options)"),
            (363, 54, "attack unlucky -> stake (print: S363 options)"),
            (363, 216, "attack lucky -> pit (print: S363 options)"),
            (364, 103, "Siren low -> Siren (print: S364 options)"),
            (364, 203, "Siren high -> song (sol20: 364, 203)"),
            (365, 151, "phial low -> dome (print: S365 options)"),
            (365, 260, "phial high -> antidote (print: S365 options)"),
            (366, 47, "feast -> urns (print: S366 options)"),
            (369, 99, "tests lose -> swamp (print: S369 options)"),
            (369, 352, "tests win -> caveman (print: S369 options)"),
            (368, 73, "walk unlucky -> sand (print: S368 options)"),
            (368, 166, "walk lucky -> stone (print: S368 options)"),
            (369, 309, "walk listen -> nuts (sol20: 369, 309)"),
            (368, 352, "walk heave -> caveman (print: S368 options)"),
            (371, 45, "key walk -> door (print: S371 options)"),
            (372, 141, "heal red -> dog (print: S372 options)"),
            (372, 142, "heal step -> brew (print: S372 options)"),
            (373, 43, "rope open -> armour (sol20: 373, 43)"),
            (374, 52, "Hellhound win -> straw (sol20: 374, 52)"),
            (375, 138, "cry walk -> ladder (print: S375 options)"),
            (375, 168, "cry open -> sunshine (sol20: 375, 168)"),
            (375, 268, "cry walk -> charge (print: S375 options)"),
            (376, 97, "drop eat -> fruit (print: S376 options)"),
            (376, 163, "drop door -> humanoid (print: S376 options)"),
            (377, 232, "clock left -> hunchback (sol20: 377, 232)"),
            (377, 319, "clock right -> tunnel (print: S377 options)"),
            (378, 3, "bend open -> Noy (print: S378 options)"),
            (379, 251, "junction right -> archway (print: S379 options)"),
            (379, 362, "junction left -> grate (print: S379 options)"),
            (380, 120, "gash left -> pit (print: S380 options)"),
            (380, 159, "gash stay -> swing (print: S380 options)"),
            (381, 211, "Troll sneak -> tunnel (sol20: 381, 211)"),
            (383, 180, "dagger win -> box (print: S383 options)"),
            (384, 29, "Devil win -> Devil (print: S384 options)"),
            (384, 263, "Devil win -> dark (print: S384 options)"),
            (385, 362, "triumph -> grate (print: S385 options)"),
            (388, 309, "little walk -> nuts (print: S388 options)"),
            (389, 146, "rat win -> rats (print: S389 options)"),
            (390, 5, "spears walk -> crossbow (print: S390 options)"),
            (390, 110, "spears pull -> spear (print: S390 options)"),
            (391, 89, "cave enter -> medallion (print: S391 options)"),
            (391, 161, "cave walk -> door (print: S391 options)"),
            (392, 180, "liars dagger -> box (print: S392 options)"),
            (393, 120, "gash left -> pit (print: S393 options)"),
            (393, 139, "gash stay -> Orcs (print: S393 options)"),
            (394, 375, "torch fetch -> cry (print: S394 options)"),
            (395, 20, "fruit walk -> movement (print: S395 options)"),
            (396, 124, "cage exit -> trapping (print: S396 options)"),
            (397, 93, "funnel up -> stairs (sol20: 397, 93)"),
            (397, 305, "funnel down -> steps (print: S397 options)"),
            (398, 193, "idol walk -> swim (print: S398 options)"),
            (398, 209, "idol gold -> junction (print: S398 options)"),
            (399, 242, "witch up -> floor (print: S399 options)"),
            (399, 375, "witch down -> cry (print: S399 options)"),
            // Solution-path exits whose verb was garbled beyond the cleaner's reach (render-verified).
            (11, 278, "rings one/three -> pit (render: p15)"),
            (11, 345, "rings nine/eleven -> Trialmaster (render: p15)"),
            (177, 68, "junction left -> iron door (render: p54)"),
            (222, 7, "broadsword leave -> boards (render: p65)"),
            (240, 7, "broadsword walk -> boards (render: p69)"),
        };
        foreach (var (parent, target, _) in codeEdges)
            if (sectionNumber == parent && !HasExit(content, target))
                content += $" (turn to {target}).";

        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair FIRST, so the text
            // matched here is already post-shared-repair. Plain string Replace is used (proven
            // in-pipeline); every target below was read verbatim from pipeline output and every
            // corrected number is sol20- or render-proven.
            21 => content.Replace("turn to 572", "turn to 372",
                StringComparison.OrdinalIgnoreCase), // red steps walk (sol20: 21, 372)
            370 => content.Replace("turn to 701", "turn to 101",
                StringComparison.OrdinalIgnoreCase), // no cloak -> youth (print: S370, S101)
            19 => content.Replace("lum fo oo", "turn to 70",
                StringComparison.OrdinalIgnoreCase), // medallion neck (sol20: 19, 70)
            155 => content.Replace("turn to 139", "turn to 119",
                StringComparison.OrdinalIgnoreCase) // nuts eat -> set off again (render p49; OCR 13g)
                .Replace("turn GD 2RH.", "turn to 288.",
                StringComparison.OrdinalIgnoreCase), // nuts walk on (render p49; OCR 2RH)
            156 => content.Replace("tum 0 199.", "turn to 199.",
                StringComparison.OrdinalIgnoreCase) // dome lift -> little man (render p50)
                .Replace("form braze.", "turn to 222.",
                StringComparison.OrdinalIgnoreCase), // dome walk -> tunnel (render p50)
            204 => content.Replace("turn to 113", "turn to 111",
                StringComparison.OrdinalIgnoreCase), // head strike -> anticipate (render p59; OCR 113)
            63 => content.Replace("turn to 328", "turn to 326",
                StringComparison.OrdinalIgnoreCase), // Bone Devil win -> return (render p29)
            122 => content.Replace("turn to 123-124", "turn to 13",
                StringComparison.OrdinalIgnoreCase), // boards -> "He is your son" (render p42)
            222 => content.Replace("turn to 290", "turn to 240",
                StringComparison.OrdinalIgnoreCase), // broadsword use -> excellent weapon (render p65)
            _ => content,
        };
    }
}
