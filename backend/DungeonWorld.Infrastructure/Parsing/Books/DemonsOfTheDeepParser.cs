using System.Text.RegularExpressions;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF19 Demons of the Deep (Steve Jackson, 1986). Rebuilt from a manual reconstruction manifest
/// (300 dpi line transcripts, 400 sections across pages 14-122; wide pages split into two-up
/// L/R halves, front matter on narrow single-column pages). Intro uses the cover blurb (2) plus
/// BACKGROUND (12-13); the curated intro text ships in Intros/ff19.txt.
///
/// <para><b>Layout notes.</b> Like FF18 this book prints a decorative range marker at the top of
/// each half ("59-61") followed by the individual section numbers further down the column, so
/// headers are NOT always at the top of a half. The manifest was derived by walking the headers
/// in ascending order - the book prints sections in order, which is the reliable anchor. A later
/// audit found fifteen pins (S212, S241-243, S274-275, S307-309, S345, S364-365, S370, S373,
/// S377-378) collapsed onto a neighbouring section's slice - a few sat on a running head or a lost
/// header, and three had silently merged two sections - so they were re-anchored to their true
/// header/first body line against the rendered pages. This
/// scan also has systematic digit confusion (5-&gt;3: "59"-&gt;"39", "57"-&gt;"37", "55"-&gt;"35",
/// "40"-&gt;"30", "44"-&gt;"33"; 1-&gt;7: "41"-&gt;"47"; single letters: "T" = 1/7, "B" = 8,
/// "f1" = 61, "gb" = 96, "Bo" = 80/86, "Ba" = 84, "Bs" = 88, "bz" = 62, "11]" = 60, "a3" = 94,
/// "a6" = 66, "33]" = 68, "Bx" = 81, "+8" = 78, "Fy" = 97, "[Ve]" = 92, "[11]" = 98, "go" = 90),
/// truncated long numbers ("77" -&gt; "7", "73" -&gt; "3", "74" -&gt; "4"), and headers lost in
/// illustration gaps (S2, S6, S9, S42, S44, S58, S72, S76, S79, S89, S99, S100). Each placement
/// was confirmed against the transcript and, where ambiguous, the rendered PDF page.</para>
///
/// <para>This is one of the "hard scans": it opts into the isolated <see cref="HardScanExitRepair"/>
/// rules and exit-fragment retention, without the hardened second OCR pass.</para>
/// </summary>
public sealed class DemonsOfTheDeepParser : ManifestDungeonWorldParser
{
    public DemonsOfTheDeepParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "DemonsOfTheDeep";
    protected override string TitleMatch => "Demons of the Deep";
    protected override string Slug => "ff19_demons_of_the_deep";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff19.json";

    /// <summary>Cover blurb plus BACKGROUND; the rules pages (3-11) are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 2, 12, 13 };

    /// <summary>FF19 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Written by Steve Jackson (the American game designer), not Ian Livingstone.</summary>
    protected override string Author => "Steve Jackson";

    /// <summary>
    /// FF19 keeps exit-fragment retention and the isolated exit repair but stays off the hardened
    /// second OCR pass, which on FF15 cost reachable sections by breaking the "dangling turn-to +
    /// orphaned number fragment" pattern its curated fixes rely on.
    /// </summary>
    protected override bool KeepExitFragments => true;

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    /// <summary>FF19-scoped exit repair, bounded to this book's 400 sections.</summary>
    protected override string RepairExits(int sectionNumber, string content) =>
        ApplyPostRepairFixes(sectionNumber, RepairExits(content));

    /// <summary>Exposed for tests.</summary>
    public static string RepairExits(string content) =>
        HardScanExitRepair.Repair(content, MaxSections);

    /// <summary>
    /// Canonical-text follow-ups that run <em>after</em> <see cref="HardScanExitRepair"/>: two
    /// re-pin corrections whose garbled verb (<c>urn to 8.</c>, <c>tum fox 18g.</c>) is only
    /// repaired by that pass, so they are invisible to <see cref="ApplySectionFixes"/>, which
    /// runs earlier. Every target is read from the rendered page.
    /// </summary>
    public static string ApplyPostRepairFixes(int sectionNumber, string content) =>
        sectionNumber switch
        {
            213 => content.Replace("turn to 8.", "turn to 87.",
                StringComparison.OrdinalIgnoreCase), // larger coral formation (render p71; OCR dropped the 7)
            274 => content.Replace("turn to 18g", "turn to 188",
                StringComparison.OrdinalIgnoreCase), // kill all the pirates (render p87; the g is an 8)
            _ => content,
        };

    /// <summary>
    /// True when <paramref name="content"/> already carries a real exit to <paramref name="target"/>.
    /// The trailing (?!\d) matters: this scan reads stray extra digits, so a substring check would
    /// wrongly conclude the exit is present and skip the restoration.
    /// </summary>
    private static bool HasExit(string content, int target) =>
        Regex.IsMatch(content, $@"\bturn\s+to\s+{target}(?!\d)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Per-section corrections for exit targets the scan read as a different but valid number.
    /// Every rule below is confirmed against the arek sol18 solution chain or the rendered PDF
    /// page; anything unresolved is deliberately left verbatim rather than guessed.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        // Restored only when absent, so a future better scan needs no edit. Every edge below
        // is a consecutive sol18 pair (the player physically turned from parent to target).
        // The single non-sol pair (59,6) keeps a verb-only garble whose number is intact.
        (int parent, int target, string note)[] codeEdges =
        {
            // S28's "lum Lo Bb." line is dropped as noise before this point, so both of its
            // sol18-proven exits are restored: kill -> swim out (86), Swiftstinger -> hits (215).
            (28, 86, "kill Sea Dragon -> swim out of the cavern (sol18: 28, 86)"),
            (28, 215, "use Swiftstinger -> it hits and returns you (sol18: 28, 215, 28)"),
            // S86's "turn to 20a" garble is restored as an append (the Replace below covers
            // cleaner future scans); sol18 proves 86 -> 202.
            (86, 202, "swim out -> pick seaweed (sol18: 86, 202)"),
            (173, 211, "confront the watchers -> swim down the dark shaft (sol18: 173, 211)"),
            (211, 233, "large building -> go left in the hallway (sol18: 211, 233)"),
            (233, 270, "go left -> fight the glowing Skeletons (sol18: 233, 270)"),
            (270, 75, "destroy Skeletons -> destroy each (sol18: 270, 75)"),
            (328, 374, "leave the room -> swim back up and out (sol18: 328, 374)"),
            (374, 16, "swim back up -> visit the red dome (sol18: 374, 16)"),
            (304, 161, "which way did you enter -> blue phosphorescence (sol18: 304, 161)"),
            (57, 373, "offer Black Pearls -> choose a fish each (sol18: 57, 373)"),
            (316, 150, "pay and leave -> strange patch of sand (sol18: 316, 150)"),
            (196, 321, "listen to the voice -> help the Ghost (sol18: 196, 321)"),
            (101, 360, "led to the bottle -> open the bottle (sol18: 101, 360)"),
            (360, 184, "open the bottle -> placate the Genie (sol18: 360, 184)"),
            (280, 246, "stab the Grouper -> it flees with the harpoon (sol18: 280, 246)"),
            (340, 11, "lift the jade crown -> fight the Statue (sol18: 340, 11)"),
            (11, 232, "fight the Statue -> defeat it (sol18: 11, 232)"),
            (257, 90, "open the trapdoor -> it opens (sol18: 257, 90)"),
            (271, 12, "whirlpool sucks you down -> possess a Glowfish (sol18: 271, 12)"),
            (258, 384, "swim upstream -> press ahead (sol18: 258, 384)"),
            (384, 345, "press ahead -> tunnel opens (sol18: 384, 345)"),
            (372, 327, "parley -> have a Black Pearl (sol18: 372, 327)"),
            (327, 115, "have a Pearl -> give it to free it (sol18: 327, 115)"),
            (310, 42, "go back down -> tired of coral cuts (sol18: 310, 42)"),
            (197, 221, "continue downstream -> kick the door open (sol18: 197, 221)"),
            (221, 307, "burst through -> attack the Sea Hag (sol18: 221, 307)"),
            (307, 254, "sympathy but duty -> kill the SEA HAG (sol18: 307, 254)"),
            (162, 49, "go left in the tunnel -> left in the tunnel (sol18: 162, 49)"),
            (245, 53, "emerged from the cliff -> swim out over the city (sol18: 245, 53)"),
            (53, 64, "cliff face -> investigate the palace (sol18: 53, 64)"),
            (64, 103, "domed palace -> hail the guards (sol18: 64, 103)"),
            (35, 265, "offer the crown -> the king is impressed (sol18: 35, 265)"),
            (265, 296, "impressed -> thank the Deep Ones (sol18: 265, 296)"),
            (296, 347, "thank -> investigate the sunken ship (sol18: 296, 347)"),
            (348, 97, "kill the Clam -> kill the SEA SPIDER (sol18: 348, 97)"),
            (192, 40, "leave now -> study the maps (sol18: 192, 40)"),
            (40, 222, "leave -> swim to the gardens (sol18: 40, 222)"),
            (222, 2, "underwater gardens -> go right from the gate (sol18: 222, 2)"),
            (174, 38, "forward to the roar -> top of the seaweed trees (sol18: 174, 38)"),
            (293, 234, "baffled Lionfish swims away -> meet the old man (sol18: 293, 234)"),
            (234, 135, "go around -> have not heard of Greylock (sol18: 234, 135)"),
            (225, 376, "wait and see -> have some Black Pearls (sol18: 225, 376)"),
            (376, 175, "have Pearls -> have a Silver Pearl (sol18: 376, 175)"),
            (175, 241, "have Silver -> accept a Potion (sol18: 175, 241)"),
            (241, 285, "gate or ship -> take green liquid (sol18: 241, 285)"),
            (285, 154, "green liquid -> rather not meet the Dragon (sol18: 285, 154)"),
            (261, 96, "Glowfish appears -> smashed little house (sol18: 261, 96)"),
            (96, 172, "past it into the abyss -> cluster of Anemones (sol18: 96, 172)"),
            (297, 325, "seek to destroy Bloodaxe -> blade slices tentacles (sol18: 297, 325)"),
            (212, 195, "pull the chest away -> open the heavy chest (sol18: 212, 195)"),
            (263, 381, "thrill of magic -> leave for the dark chasm (sol18: 263, 381)"),
            (381, 6, "dark chasm -> do not possess a Glowfish (sol18: 381, 6)"),
            (180, 256, "huge shadow -> offer the Ogre a bribe (sol18: 180, 256)"),
            (226, 256, "offer does not interest -> shout insults (sol18: 226, 256)"),
            (163, 248, "take the ring -> leave along the abyss (sol18: 163, 248)"),
            (148, 219, "attack the Crabs -> kill each GIANT CRAB (sol18: 148, 219)"),
            (92, 400, "sample the crab meat -> leave the building (sol18: 92, 400)"),
            (389, 62, "towards a cliff -> tell the Sea Dragon (sol18: 389, 62)"),
            (62, 7, "tell your story -> appeal to good will (sol18: 62, 7)"),
            (334, 28, "only a snack -> use Swiftstinger (sol18: 334, 28)"),
            (215, 28, "hits and returns you -> kill the SEA DRAGON (sol18: 215, 28)"),
            (202, 166, "approach the Sea-horse -> try to jump on it (sol18: 202, 166)"),
            (108, 9, "stay on -> direct it to the cathedral (sol18: 108, 9)"),
            (9, 269, "towards the cathedral -> huge Gothic cathedral (sol18: 9, 269)"),
            (269, 339, "look around -> explore the interior (sol18: 269, 339)"),
            (339, 191, "explore further -> unusual pictures (sol18: 339, 191)"),
            (191, 367, "look at pictures -> drawn into the scene (sol18: 191, 367)"),
            (367, 244, "drawn in -> what is going on (sol18: 367, 244)"),
            (244, 335, "illusion vanish? -> price for a lesson (sol18: 244, 335)"),
            (335, 267, "have the price -> lesson from swordmaster (sol18: 335, 267)"),
            (267, 22, "take a lesson -> wound CYRANO (sol18: 267, 22)"),
            (22, 176, "wound CYRANO -> back in the open ocean (sol18: 22, 176)"),
            (176, 111, "open ocean -> offer Octopuses food (sol18: 176, 111)"),
            (111, 34, "offer food -> possess a Crab's claw (sol18: 111, 34)"),
            (34, 205, "have the claw -> huge coral formation (sol18: 34, 205)"),
            (205, 87, "coral formation -> hide in a niche (sol18: 205, 87)"),
            (87, 99, "hide in niche -> use a Toolfish (sol18: 87, 99)"),
            (99, 31, "use a Toolfish -> use the Axefish (sol18: 99, 31)"),
            (31, 214, "use the Axefish -> release another (sol18: 31, 214)"),
            (214, 31, "release another -> use the Sawfish (sol18: 214, 31)"),
            (31, 342, "use the Sawfish -> try something different (sol18: 31, 342)"),
            (342, 99, "try different -> attack the Kraken (sol18: 342, 99)"),
            (99, 253, "attack the Kraken -> kill the KRAKEN (sol18: 99, 253)"),
            (253, 125, "kill KRAKEN -> ocean turning blood-red (sol18: 253, 125)"),
            (358, 100, "learned the magic word -> throw down ten Pearls (sol18: 358, 100)"),
            (358, 47, "no magic word -> turn back (intact number, same option block)"),
            (100, 236, "ten Pearls -> sail into Port Blacksand (sol18: 100, 236)"),
            (59, 6, "trident vanishes -> swim down into the chasm (verb-only garble, number intact)"),
            (5, 254, "have none of these -> attack (line lost; render p16 proves 5 -> 254)"),
            (219, 92, "kill the Crabs -> sample the crab meat (sol18: 219, 92)"),
            (130, 188, "useless in battle -> return to the fight (verb-only garble)"),
            (289, 232, "fight to the finish -> return (verb-only garble)"),
            (306, 47, "try another tactic -> return (verb-only garble)"),
            (292, 47, "no answer but Toll -> return (verb-only garble)"),
            (117, 78, "jump away -> Devilfish escape (verb-only garble)"),
            (178, 391, "no coins -> explain without them (verb-only garble)"),
            (138, 363, "Water Sprite -> chasm house (verb-only garble, number intact)"),
            (368, 162, "disobey -> leave (verb-only garble)"),
            (118, 384, "shake off tormentors -> return (trailing junk, forced 384)"),
            (291, 232, "return and fight to the finish (verb-only garble)"),
            (295, 47, "call for the Sea Dragon -> return (verb-only garble)"),
            (337, 77, "cannot hear you -> return (verb-only garble)"),
            (243, 47, "cannot summon -> return (verb-only garble)"),
            (54, 253, "leave it alone -> horn (verb-only garble)"),
            (61, 245, "no Dolphin name -> horn (verb-only garble)"),
            (89, 103, "try the frond entrance -> return (verb-only garble)"),
            // Re-pin follow-ups: after S241-243/274/307-309/364-365/212-213/377-378 were
            // re-anchored to their true headers (they had collapsed onto a neighbour's slice),
            // the exits below - each read from the rendered PDF page - had to be restored.
            (211, 16, "leave the building -> explore elsewhere (render p70)"),
            (241, 318, "Vaporous Essence -> ice crystal (render p77)"),
            (242, 275, "agree to exchange a Black Pearl (render p77)"),
            (242, 85, "keep your Black Pearl (render p77; OCR read 83)"),
            (364, 111, "rocks and sunken timbers (render p109)"),
            (365, 74, "use your sword on the trapdoor (render p109)"),
            (365, 134, "use the Axefish on the trapdoor (render p109)"),
        };
        foreach (var (parent, target, _) in codeEdges)
            if (sectionNumber == parent && !HasExit(content, target))
                content += $" (turn to {target}).";

        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair FIRST, so the text
            // matched here is already post-shared-repair. Plain string Replace is used (proven
            // in-pipeline); every target below was read verbatim from pipeline output and every
            // corrected number is sol18- or render-proven.
            86 => Regex.Replace(content, @"turn\s+to\s+20a\b", "turn to 202",
                RegexOptions.IgnoreCase) // swim out -> pick seaweed (sol18: 86, 202)
                .Replace("Tum to 20a", "Turn to 202",
                StringComparison.OrdinalIgnoreCase), // same fix for the raw verb form
            62 => content.Replace("adventure? turn to 9", "adventure? turn to 7",
                StringComparison.OrdinalIgnoreCase), // story -> appeal to good will (sol18: 62, 7)
            7 => content.Replace("turn to 5\n[13 hatred", "turn to 178. Its hatred",
                StringComparison.OrdinalIgnoreCase), // greed -> pay the hag (render p16: 7, 178)
            173 => content.Replace("turn to 231.", "turn to 211.",
                StringComparison.OrdinalIgnoreCase), // confront -> dark shaft (sol18: 173, 211; S231 independently parented by S62)
            57 => content.Replace("Offer gold? Turn to 232", "Offer gold? Turn to 132",
                StringComparison.OrdinalIgnoreCase) // Merman gold -> 132 (render p30)
                .Replace("Turn 10373", "Turn to 373",
                StringComparison.OrdinalIgnoreCase) // Black Pearl -> choose a fish (sol18: 57, 373)
                .Replace("turn to 9\nDecline", "turn to 76\nDecline",
                StringComparison.OrdinalIgnoreCase) // harpoon -> 76 (render p30)
                .Replace("Turn ty 329", "Turn to 329",
                StringComparison.OrdinalIgnoreCase), // attack him -> 329 (render p30)
            5 => content.Replace("turn to 327", "turn to 127",
                StringComparison.OrdinalIgnoreCase) // Black Pearl -> 127 (render p16)
                .Replace("Tum to Ba", "turn to 81",
                StringComparison.OrdinalIgnoreCase), // magic sword -> 81 (render p16)
            8 => content.Replace("turn to 5.", "turn to 118.",
                StringComparison.OrdinalIgnoreCase), // flee -> 118 (render p16)
            180 => content.Replace("Tucn lo 236.", "Turn to 256.",
                StringComparison.OrdinalIgnoreCase), // huge shadow -> bribe the Ogre (sol18: 180, 256)
            92 => content.Replace("turn to 10 400", "turn to 400",
                StringComparison.OrdinalIgnoreCase), // sample crab meat -> leave (sol18: 92, 400)
            31 => content.Replace("The Axetish? turn to 219", "The Axetish? turn to 214",
                StringComparison.OrdinalIgnoreCase), // Axefish -> release another (sol18: 31, 214)
            11 => content.Replace("turn to 22\nFlee", "turn to 232\nFlee",
                StringComparison.OrdinalIgnoreCase), // fight the Statue -> defeat it (sol18: 11, 232)
            202 => content.Replace("Turn Eo 166", "Turn to 166",
                StringComparison.OrdinalIgnoreCase), // seaweed approach -> ride it (sol18: 202, 166)
            304 => content.Replace("Turn to 3\nSwim", "Turn to 161\nSwim",
                StringComparison.OrdinalIgnoreCase), // which way in -> phosphorescence (sol18: 304, 161)
            99 => content.Replace("turn to 10.253", "turn to 253",
                StringComparison.OrdinalIgnoreCase) // attack Kraken -> kill it (sol18: 99, 253)
                .Replace("Inmto3z", "turn to 31",
                StringComparison.OrdinalIgnoreCase), // use a Toolfish -> Axefish (sol18: 99, 31)
            342 => content.Replace("return to gg.", "return to 99.",
                StringComparison.OrdinalIgnoreCase) // try different -> Kraken (sol18: 342, 99)
                .Replace("horn to 380.", "turn to 380.",
                StringComparison.OrdinalIgnoreCase), // leave -> horn (verb-only garble)
            59 => content.Replace("Tam to 6.", "Turn to 6.",
                StringComparison.OrdinalIgnoreCase), // trident vanishes -> chasm (verb-only garble)
            227 => content.Replace("Tom tp 121", "Turn to 121",
                StringComparison.OrdinalIgnoreCase), // talk your way out (verb-only garble)
            288 => content.Replace("Turn ti 156", "Turn to 156",
                StringComparison.OrdinalIgnoreCase), // skull island choice (verb-only garble)
            65 => content.Replace("1urn lo 311", "turn to 311",
                StringComparison.OrdinalIgnoreCase), // investigate -> swim harder (verb-only garble)
            157 => content.Replace("Tam to 315", "turn to 315",
                StringComparison.OrdinalIgnoreCase), // three times then three (verb-only garble)
            241 => content.Replace("Tum tio 318", "turn to 318",
                StringComparison.OrdinalIgnoreCase), // Vaporous Essence -> ice crystal (render p77; option sits in S241's slice)
            242 => content.Replace("fun lo 275", "turn to 275",
                StringComparison.OrdinalIgnoreCase) // agree to exchange a Black Pearl (render p77)
                .Replace("turn to 83.", "turn to 85.",
                StringComparison.OrdinalIgnoreCase), // keep your Black Pearl (render p77; OCR read 83)
            300 => content.Replace("luen lo 335", "turn to 335",
                StringComparison.OrdinalIgnoreCase), // demand an explanation (verb-only garble)
            48 => content.Replace("Lien to 344", "turn to 344",
                StringComparison.OrdinalIgnoreCase), // keep the bottle (verb-only garble)
            56 => content.Replace("tun\nto 388.", "turn to 388.",
                StringComparison.OrdinalIgnoreCase), // examine the chest (verb-only garble)
            190 => content.Replace("fu to 266", "turn to 266",
                StringComparison.OrdinalIgnoreCase) // sauna baths (verb-only garble; option sits in S190's slice)
                .Replace("tum to 164.", "turn to 164.",
                StringComparison.OrdinalIgnoreCase), // games room (verb-only garble)
            191 => content.Replace("tum to 367.", "turn to 367.",
                StringComparison.OrdinalIgnoreCase), // stay for more pictures (verb-only garble)
            23 => content.Replace("Turi ko 250", "turn to 250",
                StringComparison.OrdinalIgnoreCase), // flee the lone fish (verb-only garble)
            82 => content.Replace("bom\nLo 250.", "turn to 250.",
                StringComparison.OrdinalIgnoreCase), // no ring -> dodge (verb-only garble)
            123 => content.Replace("horn ko 268", "turn to 268",
                StringComparison.OrdinalIgnoreCase), // kill the Sea Spider (verb-only garble)
            126 => content.Replace("turn to 284g.", "turn to 284.",
                StringComparison.OrdinalIgnoreCase), // keep it and fight (trailing junk)
            183 => content.Replace("tun lo 87.", "turn to 87.",
                StringComparison.OrdinalIgnoreCase), // coral formation (verb-only garble)
            216 => content.Replace("Tun to 154.", "Turn to 154.",
                StringComparison.OrdinalIgnoreCase), // square stone building (verb-only garble)
            85 => content.Replace("Tam to 210.", "Turn to 210.",
                StringComparison.OrdinalIgnoreCase), // refusal infuriates (verb-only garble)
            284 => content.Replace("Tum\n2347.", "turn to 234.",
                StringComparison.OrdinalIgnoreCase), // tossed out -> guards (4-digit overflow)
            117 => content.Replace("{turf to 78)", "{turn to 78)",
                StringComparison.OrdinalIgnoreCase), // jump away (verb-only garble)
            178 => content.Replace("rn io 391.", "turn to 391.",
                StringComparison.OrdinalIgnoreCase), // no coins to clink (verb-only garble)
            138 => content.Replace("Tumtn 363", "turn to 363",
                StringComparison.OrdinalIgnoreCase), // Water Sprite (verb-only garble)
            368 => content.Replace("Turn ro 162,", "Turn to 162,",
                StringComparison.OrdinalIgnoreCase), // disobey and leave (verb-only garble)
            // Re-pin follow-ups (see the codeEdges note above); every target read from the
            // rendered page. These Replaces keep the printed text clean so the edges derive
            // from the sentence itself rather than only from the restored codeEdge.
            211 => content.Replace("Turn to 1h", "Turn to 16",
                StringComparison.OrdinalIgnoreCase), // explore elsewhere (render p70)
            212 => content.Replace("Turn to 105", "Turn to 195",
                StringComparison.OrdinalIgnoreCase), // open the heavy chest (render p71; OCR read 105)
            213 => content.Replace("turn to 8.", "turn to 87.",
                StringComparison.OrdinalIgnoreCase), // larger coral formation (render p71; OCR dropped the 7)
            274 => content.Replace("turn to 18g", "turn to 188",
                StringComparison.OrdinalIgnoreCase), // kill all the pirates (render p87; g is an 8)
            364 => content.Replace("burn rar", "turn to 111",
                StringComparison.OrdinalIgnoreCase), // rocks and sunken timbers (render p109)
            365 => content.Replace("Iurmiozg", "turn to 74",
                StringComparison.OrdinalIgnoreCase) // sword on the trapdoor (render p109)
                .Replace("turn to 39", "turn to 134",
                StringComparison.OrdinalIgnoreCase), // use the Axefish (render p109)
            _ => content,
        };
    }
}
