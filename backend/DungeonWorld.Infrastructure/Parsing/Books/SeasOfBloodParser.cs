using System.Text.RegularExpressions;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Parser tuned for "Seas of Blood" (a single-page scan). Overrides go here as
/// quirks are discovered; the base heuristics are used until then.
/// </summary>
public sealed class SeasOfBloodParser : DungeonWorldBookParserBase
{
    public SeasOfBloodParser(
        IPdfTextExtractor textExtractor,
        IOptions<FileStorageOptions> storageOptions)
        : base(textExtractor, storageOptions)
    {
    }

    public override string ParserId => "SeasOfBlood";

    public override bool CanHandle(string filePath, string bookTitle) =>
        bookTitle.Contains("Seas of Blood", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Sections whose text was dropped or assigned to a neighbour because the scan's
    /// header is missing or misprinted (e.g. p74 prints "12" for 92). These are the
    /// verified 600-dpi readings, kept here so a re-ingest is faithful.
    /// </summary>
    private static readonly Dictionary<int, string> SectionOverrides = new()
    {
        [50] = "'Come now,' says Abdul, looking at your markedly smaller pile of gold, 'it was hardly even a contest - and you call yourself a pirate?' You have been hopelessly trounced. Abdul is, without a doubt, the greater pirate. You have lost the wager.",
        [90] = "The Banshee, under full sail, pulls away from the\n\nwarship and leaves it far behind. Will you head back\n\ninto the Inland Sea, towards Enraki (turn to 76), or\n\ntravel down the coast of the Eastern Rim, towards\n\nthe Rivers of the Dead (turn to 197)?",
        [91] = "Your blow is completely ineffective and results only in leaving you open to a counter-attack. The Cyclops jerks its knee into your chest and then pushes you to the ground - lose 2 points of STAMINA. Leaning over your pain-racked body, it grabs you by the forearm and lifts you into the air. Will you:\nKick it in the armpit? Turn to 305.\nBite it on the wrist? Turn to 276.\nGive it a chop to the back of the elbow? Turn to 187.",
        [92] = "While you are sailing past the island, the ship suddenly falls into a very dark and sharply defined patch of shade. Looking up, you see an enormous bird, somewhat like a buzzard, only several hundred times bigger. It circles above the Banshee a couple of times, then descends in a steep dive to attack. Swooping, it catches you in its claws and carries you back towards the island. Drawing your scimitar, you try to fight your way free.\nROC SKILL 11 STAMINA 10.\nIf you defeat it, turn to 124.",
        [133] = "You wander around the island, completely circumnavigating it within a couple of hours. Not finding\nanything of any interest, you head back to the\nBanshee. Will you sail south, towards the Channel\nof Goth (turn to 4) or west, towards the Roc (turn\nto 257)?",
        [134] = "You manage to coax your crew into attempting the passage through the treacherous reefs. The Banshee enters a shallow passage surrounded by foaming and crashing water. Taking down most of your sail, you edge forward, eventually reaching what appears to be a split in the channel. Will you follow the wider branch (turn to 170) or the narrower one (turn to 198)?",
        [215] = "The cavalry lead you to an impressive castle, bedecked with banners and guarded by hundreds of\nmen-at-arms. Revelries are in progress in the central\ncourtyard. Leaving your crew here to partake of the\nwine and song, you are conducted into the main\nfeast-hall, where the King of the Four Winds sits\namong his retainers and courtiers. With a welcoming gesture, the King bids you be seated only a few\nplaces from his throne. He engages you in a rather\nloud conversation (as the feast-hall is noisy with the\nfestivities) about the weather and your journey. Will\nyou tell the King about the true nature of your trip\n(turn to 260) or not (turn to 312)?",
        [216] = "When you enter the hall, the flames in the fire-pit leap to the ceiling, and a groan creaks throughout the room. \"Wait,\" breathes a voice, \"wait.\" Looking into the flames, you see a tenuous lizard-like creature, armed with a short trident, beckoning you closer. Its almost human eyes twinkle as it asks, \"Where in Albion would you find that which lies between above and below?\"\nIf you know the answer to this, turn to 308.\nIf you don't, turn to 317.",
        [260] = "'Ah, yes,' says the King, 'Abdul the Butcher and his\nmen. I remember them well, as they were here but\nthree days ago. Very poor table manners but a lively\nwit!' The King waves a servant over, whispers in his\near and sends him away. When the servant returns,\nhe is bearing two very large sacks. These,' says the\nKing, 'contain the north and south winds.\nAs I gave the east and west winds to Abdul, I think\nit only fair I should give these to you. Open the\nbag containing the north wind on your journey to\nNippur and open the other bag when you wish to\nreturn.' At the end of the feast, you gather your men\nand prepare to return to the Banshee. Your crew are\nintensely interested in the sacks and don't believe\nyou when you tell them that they contain 'only air'.\nThey believe the King has filled them with treasure\nand that you are simply being greedy. 'Share it with\nus,' they say, 'or we won't go on. We'll stay here.'\nYou can't open the sacks now, so will you give your\ncrew 100 Gold Pieces to keep them quiet (turn to\n368), or simply insist to them that they are being\nstupid not to believe you (turn to 396)?",
        [261] = "Passing at a relatively safe distance, you are surprised when a missile from a ballista mounted on one of the galleys crashes into your mast, bringing the mainsail down on to the deck in a confusion of white canvas.\nRoll three dice.\nIf the result is less than your CREW STRENGTH, turn to 83.\nIf the result is equal to or greater than your CREW STRENGTH, turn to 51.",
        [312] = "With the feast over, the King bids you farewell. You\n\nreturn to the Banshee and leave the cove where you\n\nwere anchored. If you were given the sacks containing the north and south winds, then from now on,\n\nwhenever you are instructed to roll three dice and to\n\ncompare the result with your CREW STRENGTH,\n\ndeduct 4 points from the result of the dice roll. Which\n\nway will you head: south-east, towards the Channel\n\nof Goth (turn to 191), or south-west, towards the\n\nRoc (turn to 27)?",
        [313] = "Your ship smashes through the warship's oars, but receives several volleys of arrows from the Shurrupak marines crowding the other vessel's decks - lose 2 points of CREW STRENGTH. Breaking through the line of war galleys, you see a second, smaller squadron following up behind. Will you go with the wind, cut in front of these vessels and head for the shore (turn to 261), or try to break through, as you did with the first line (turn to 237)?",
        [314] = "You sail either to the volcanic island (turn to 369) or the palm-covered island (turn to 354).",
        [320] = "With the beast dead, you search the ship at leisure, and discover masses of frozen crates, but no treasure other than a flame-red jewel set in a gold ring. The ring is inscribed on one side with a riddle: Where in Albion would you find that which lies between above and below? And on the other side is what you presume is the answer: At the end, next to yourself. Odd. As you find nothing else, you return to the Banshee.\nTurn to 217.",
        [321] = "The lizards are released! They fly hungrily down the track towards the doomed goblin - Awkspeed beats Fazima by a tooth. If you backed the winner, collect as much gold (or as many slaves) as you initially bet. Will you bet on the Bataars again (turn to 202), or - if you haven't already - try punch-out (turn to 48) or dice (turn to 236)?",
        [324] = "Going ashore with most of your crew, you find yourselves being feted by the exuberant natives. Dancing around you all the while, they lead you into the centre of their village, where they set out a splendid feast. When you inquire of the elders whether they can assist you in stocking the Banshee, they nod and laugh, saying you will certainly get all the help you need. After the feast, the villagers ply you with wine and make you dance along to the sounds of vigorous drumming. Eventually, you all collapse from exhaustion. On awakening the next day, you find yourselves locked in a large open-air enclosure.\nTest your Luck.\nIf you are Lucky, turn to 286.\nIf you are Unlucky, turn to 345.",
        [325] = "Your men draw their swords and charge, while the Giants heft their tree-trunks in preparation. The battle begins.\nGIANTS STRIKE 9 STRENGTH 6.\nIf you defeat them, turn to 344.",
    };

    /// <summary>
    /// Sections whose header was misread on the preceding page, so the base extractor attached
    /// the wrong illustration. These page bindings were verified against the 600-dpi scan and are
    /// kept here so a re-ingest does not silently undo them.
    /// </summary>
    private static readonly Dictionary<int, string> ImagePathOverrides = new()
    {
        [50] = "/assets/game-art/ff16_seas_of_blood/p49_i0.png",
        [92] = "/assets/game-art/ff16_seas_of_blood/p74_i0.png",
        [134] = "/assets/game-art/ff16_seas_of_blood/p94_i0.png",
        [216] = "/assets/game-art/ff16_seas_of_blood/p139_i0.png",
        [261] = "/assets/game-art/ff16_seas_of_blood/p166_i0.png",
        [314] = "/assets/game-art/ff16_seas_of_blood/p195_i0.png",
    };

    /// <summary>
    /// S70 prints "otherwise, turn to no." - a typo for 110, the Horror combat that
    /// follows. S298 tells the reader to note 311 for defeating the Cyclops, but the
    /// instruction is not phrased as a normal exit, so the edge is added explicitly.
    /// S305's final choice wraps across two printed lines; joining it keeps the cleaner
    /// from dropping the first half of the label and truncating the body text.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content) => sectionNumber switch
    {
        70 => Regex.Replace(content, @"turn\s+to\s+no\.", "turn to 110.", RegexOptions.IgnoreCase),
        298 when !Regex.IsMatch(content, @"\bturn\s+to\s+311(?!\d)", RegexOptions.IgnoreCase) =>
            content + " (turn to 311).",
        305 => content.Replace(
            "Take a flying kick into the middle\nof its back?",
            "Take a flying kick into the middle of its back?"),
        _ => content,
    };

    /// <summary>Exposed for tests; returns the verified text for mis-scanned sections.</summary>
    public static string ApplySectionOverrides(int sectionNumber, string content) =>
        SectionOverrides.TryGetValue(sectionNumber, out var overrideText) ? overrideText : content;

    /// <summary>Exposed for tests; returns the verified image path for miscatalogued sections.</summary>
    public static string? ApplyImagePathOverride(int sectionNumber, string? imagePath) =>
        ImagePathOverrides.TryGetValue(sectionNumber, out var overridePath) ? overridePath : imagePath;

    protected override void PostProcessSections(Book book)
    {
        foreach (var section in book.Sections)
        {
            section.ImagePath = ApplyImagePathOverride(section.SectionNumber, section.ImagePath);

            string content = ApplySectionOverrides(section.SectionNumber, section.Content);
            content = ApplySectionFixes(section.SectionNumber, content);
            if (content == section.Content) continue;
            section.Content = content;
            section.Choices = BuildChoices(content);
        }
    }
}
