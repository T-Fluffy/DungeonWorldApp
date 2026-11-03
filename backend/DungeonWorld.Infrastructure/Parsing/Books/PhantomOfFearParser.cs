using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF28 Phantom of Fear (Steve Jackson and Ian Livingstone, 1985). Rebuilt from a manual reconstruction
/// manifest (300 dpi line transcripts, 400 sections across PDF pages 14-139; each PDF page is a two-up
/// portrait spread split into L/R halves). Section bounds follow page/side/line order because some
/// section numbers appear out of numeric order. The introduction is assembled from the cover and introduction pages.
///
/// <para><b>Exit graph.</b> The graph resource is currently an empty placeholder. Section exits
/// visually verified against the book are applied as overrides; all other sections retain their
/// OCR-derived references as provisional candidates.</para>
/// </summary>
public sealed class PhantomOfFearParser : ManifestDungeonWorldParser
{
    private const string EdgesResourceName =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff28_edges.json";

    public PhantomOfFearParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "PhantomOfFear";

    public override bool CanHandle(string filePath, string bookTitle) =>
        bookTitle.Contains("Phantom of Fear", StringComparison.OrdinalIgnoreCase) ||
        bookTitle.Contains("Phantoms of Fear", StringComparison.OrdinalIgnoreCase);

    protected override string TitleMatch => "Phantoms of Fear";

    protected override string Slug => "ff28_phantom_of_fear";

    protected override string ManifestResourceName =>
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff28.json";

    protected override bool ManifestInTranscriptOrder => true;

    /// <summary>Cover blurb (book p1) plus INTRODUCTION (book p7); the rules pages are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 2, 7 };

    /// <summary>FF28 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    protected override string PostProcessSection(int sectionNumber, string content)
    {
        if (TranscribedSections.TryGetValue(sectionNumber, out var transcription))
            return transcription;

        return sectionNumber switch
        {
            1 => content.Replace("urn to 67", "turn to 67", StringComparison.OrdinalIgnoreCase),
            13 => content
                .Replace("turn 0 43", "turn to 43", StringComparison.OrdinalIgnoreCase)
                .Replace("torn to 70", "turn to 70", StringComparison.OrdinalIgnoreCase),
            25 => content.Replace("turn to 1697", "turn to 169)?", StringComparison.OrdinalIgnoreCase),
            83 => content.Replace("turn to 1713", "turn to 171", StringComparison.OrdinalIgnoreCase),
            179 => content.Replace("turn to 3135", "turn to 315", StringComparison.OrdinalIgnoreCase),
            33 => content.Replace("turn to 27)", "turn to 217)", StringComparison.OrdinalIgnoreCase),
            120 => content.Replace("16g", "169", StringComparison.OrdinalIgnoreCase),
            140 => content
                .Replace("bum to 193", "turn to 193", StringComparison.OrdinalIgnoreCase)
                .Replace("tom to 388", "turn to 388", StringComparison.OrdinalIgnoreCase),
            189 => content.Replace("fun to ga.", "turn to 92.", StringComparison.OrdinalIgnoreCase),
            190 => content.Replace("point and turn to", "point and turn to 50.", StringComparison.OrdinalIgnoreCase),
            168 => "If your POWER score is 18 or more, turn to 243; otherwise, turn to 94.",
            150 => content.Replace("¢5", "65", StringComparison.OrdinalIgnoreCase),
            228 => content.Replace("3257", "325", StringComparison.OrdinalIgnoreCase),
            230 => content.Replace("Shit the Weevil Man", "fight the Weevil Man", StringComparison.OrdinalIgnoreCase),
            234 => content.Replace("Turn to goo.", "Turn to 400.", StringComparison.OrdinalIgnoreCase),
            262 => content.Replace("Tam to 372.", "Turn to 372.", StringComparison.OrdinalIgnoreCase),
            345 => content
                .Replace("turn to Toe", "turn to 100", StringComparison.OrdinalIgnoreCase)
                .Replace("barn to 42", "turn to 42", StringComparison.OrdinalIgnoreCase),
            315 => content.Replace("tum to 3a", "turn to 310", StringComparison.OrdinalIgnoreCase),
            348 => content
                .Replace("furn fo 5", "turn to 5", StringComparison.OrdinalIgnoreCase)
                .Replace("fun fo 90", "turn to 90", StringComparison.OrdinalIgnoreCase),
            238 => content
                .Replace("turn to 75", "turn to 73", StringComparison.OrdinalIgnoreCase)
                .Replace("1496", "149", StringComparison.OrdinalIgnoreCase),
            295 => content
                .Replace("turn to v0", "turn to 110", StringComparison.OrdinalIgnoreCase)
                .Replace("18a", "182", StringComparison.OrdinalIgnoreCase),
            374 => content.Replace("turn to 90", "turn to 99", StringComparison.OrdinalIgnoreCase),
            191 => TranscribedSection191,
            _ => base.PostProcessSection(sectionNumber, content),
        };
    }

    private static readonly IReadOnlyDictionary<int, string> TranscribedSections =
        new Dictionary<int, string>
        {
            [30] = """
                The kitchen does not lie behind you, so your bluff does not work. The Goblins make short work of you and then return to their meal - with an extra item on the menu!
                """,
            [31] = """
                You harden your resolve and spurn the offer of the fairies; your destiny lies in more direct action. You awaken from the dream and find that the glade has vanished. You are right on the border of the blighted land; all that is left of the enchanted glade is a single elder tree. The tree is obviously magical: your attention is drawn to its branches in particular, because of their regular arrangement. You notice that there are exactly twenty-two branches (note this on your Adventure Sheet). All but one are normal, leafy boughs; but the other is silver and gleams with its own inner light. You pluck it, stow it in your backpack, and take your first step into Ishtra's cruel kingdom. Turn to 259.
                """,
            [32] = """
                'Oh, very clever,' says the chief of the Pixies, as the net dissolves, and you wade to the bank of the pool. 'All right, we'll have to help you. The first thing you should know is that the entrance to Ishtra's pit is more or less due north of here - though why you should choose such a complicated way of killing yourself, I don't know. Secondly, the Riddling Reaver, the majestic jester, allows you to take any one of these items. The Pixie makes a gesture in the air, and several items appear on the ground in front of him: a pouch of medicinal herbs; a twelve-branched stag's antler, hollowed and carved into a horn; a net just like the one you were caught in; and a pair of boar's tusks. Choose one of these items and turn to 109.
                """,
            [33] = """
                The door is open. Although it is cool throughout Ishira's underground kingdom, you are met with a blast of cold as soon as you enter the room. This is the cook's freezer; the walls are solid ice, and carcasses are hanging from rows of hooks. You can identify some of the carcasses, but wish you couldn't. There is nothing here to interest you, unless you want to take some ice. A fair-sized lump of this would keep for quite a while, wrapped in a strip torn off your clothing. For some reason, the room number, 33, sticks in your mind. Next you can either try another door by rolling another double on your dice, or you could walk on up the corridor (turn to 217).
                """,
            [34] = """
                When you become invisible, you do not become immaterial. The Lizard Man platoon still know where you are, and are still able to cut you to pieces. To their satisfaction, your dying, bleeding body becomes visible again as it drops to the ground.
                """,
            [46] = """
                The Drake takes you swiftly to the top of a hill. A single tree crowns the hill; it stands stark and leafless against a grey sky. In the fork of the two main branches of the tree a large, untidy nest has been built by some bird. The Drake deposits you on the ground next to the tree and flies off, its duty done. You hear a loud squawk; a bird takes off from the nest and swoops down towards you to investigate. As soon as you see it, you know what to expect. It is a Hoatzin, a mythical bird with a two-metre wingspan, a vicious beak and claws projecting from its wing-tips. You may gain some POWER here, certainly, but only if you win the fight.

                The Hoatzin dives in to the attack. It has a POWER score of 12. Roll two dice. If you roll 2-7, the Hoatzin reduces your POWER score by 2 points; if you roll 8-12, you reduce its POWER score by 2 points. Continue the fight until either its or your POWER score is zero. If you win, turn to 340; if you lose, turn to 293.
                """,
            [47] = """
                The cracking of a twig to your right warns you that someone or something is approaching. Before you can react, a heavy foot presses down on your neck and immobilizes you. A gruff voice commands you to reach slowly for your sword and throw it away from you. Will you do as he says (turn to 375) or resist (turn to 125)?
                """,
            [48] = """
                There is a path of sorts between ruined houses, some of which are still smoking. The smell of death hangs heavy in the air. In a crater in the ground you see two bloated corpses, which are being ripped at by scavenging Mordidas. You know that you are walking through a scene from the future of Titan, if Ishtra's plans of conquest succeed. You are determined that they shall not. The path forks to bypass a hill. Will you go left (turn to 208) or right (turn to 186)?
                """,
            [49] = """
                You know the virtue of staying with a dream, however strange or lethal it may appear: after all, it is happening in your mind, so you can come to little actual harm. Although the dream is frightening, you manage to stay asleep. When your eyes are pierced, it is true that you are blinded to that dream world, but instead your eyes are opened to another world, and the dream abruptly changes. Turn to 144.
                """,
            [104] = """
                Although it grieves you to do so, you must defend yourself against these creatures, if you are to have some of their food. You fight the many squirrels as a single opponent. They are difficult to defeat, because there are so many and they are so fast they dart in at you, bite and then scurry away, like miniature guerrilla fighters.

                SQUIRRELS
                SKILL 7
                STAMINA 8

                If you defeat them, the ground is littered with small corpses, and the rest of them take to high branches and hurl squirrel insults at you. But you find just enough nuts to satisfy your hunger and keep your energy up.
                Turn to 332.
                """,
            [105] = """
                You walk along, with the river on your left. The river goes underground after a while, but there is still room to walk beside it. Will you follow the river underground (turn to 106) or remain overground (turn to 368)?
                """,
            [106] = """
                The water is clear and refreshingly cool. You may add 2 STAMINA points. After drinking, you sit on the bank and gaze in wonder around you. Somehow, to your surprise, you find that your head is resting on the soft turf. You drift off to sleep.
                Turn to 160.
                """,
            [107] = """
                The Orc captain halts his soldiers and accosts you. The Orcs stand in an unruly mass and hurl insults at you: they take you for a Dark Elf and there is no love lost between the various evil races. The captain asks you to which clan you belong. You rack your brains for the names of Dark Elf clans. Will you answer:

                Mirisgroth? Turn to 155
                Tesarath? Turn to 223
                Camcameyar? Turn to 8
                """,
            [108] = """
                The other Pygmy Orc has recovered enough to put a blowpipe to his mouth and shoot a poisoned dart at you. Test your Luck.
                If you are Lucky, turn to 38; if you are Unlucky, turn to 96.
                """,
            [109] = """
                If you chose the herbs, these will restore up to 6 STAMINA points; you can use them all at once, or in two doses. If you chose the net, you can use this to resolve any one battle; you will ensnare your opponent and be able to avoid the fight, or dispatch him with ease, but then the magical properties of the net will disappear and it cannot be re-used. If you chose the antler-horn, you note particularly that it has twelve branches.

                The Pixies chuckle at your choice. You make your way quickly out of their domain and head north. You will later find that your dunking in the pool has damaged your Provisions. Roll one die. If you roll 1-3, one meal's worth of Provisions have been ruined; if you roll 4-6, two meals' worth have been ruined.
                Turn to 171.
                """,
            [110] = """
                The only item you can find is a pendant around the skeleton's neck. The chain is made of white gold, and supports a green stone, whose nature you do not know. The stone is hanging inside the skeleton's rib-cage. You can remove the pendant, if you like, and put it on yourself. Then you can change your mind and roll away the boulder after all (turn to 379), or carry on across the small cavern and down the continuation of the tunnel (turn to 61).
                """,
            [111] = """
                The further west you travel along the creek, the less of a creek it becomes, and the more of a bog. Although Wood Elves can tread more lightly than any other race, even your feet are beginning to sink into the brown mud, which is covered with an oily film. To make matters worse, night is beginning to draw in. Will you plough on through the mire (turn to 246) or turn south for a while to avoid it (turn to 284)?
                """,
            [112] = """
                The passage takes you, after a while, to a door. You can see nothing through it, and can only hear, from beyond it, occasional snuffles and snorts. You are wondering whether to retrace your steps, when you hear the tramp of marching feet behind you. This puts you off the idea of going back and, with your heart in your mouth, you open the door and enter the room beyond.
                Turn to 258.
                """,
            [113] = """
                You find the cook in a foul mood. Everything has gone wrong for her today: a Dark Elf raiding party failed to return from the outside world with fresh meat; the food from the ice store is taking ages to thaw; her assistant put too much bat's blood in the stew (a crime for which the assistant quickly became the stew!); and so on. When you walk into the kitchen, she thinks of a neat solution to her problems. Her day has been ruined by Dark Elves: why should a Dark Elf not make things better by joining her former assistant in the pot? The cook is a large half-human, specially brought from Corda by Ishtra's spies. She attacks you with a cleaver.

                COOK
                SKILL 6
                STAMINA 10

                If you win, you find another door in the south wall of the kitchen, but you can hear feet coming up the tunnel towards it. You bar the door. It will not take them long to break through it, but in an evil place like this, the death of the cook is probably not too remarkable, and you don't expect to be pursued. You return to the dining-hall.
                Turn to 379.
                """,
            [114] = """
                The last stretches of the hill are hard going. The trees are thinner; scree covers the slope and provides little or no soil for roots, and little or no safe footing for a traveller. The trees which line the ridge are bent into weird shapes as if by a constant westerly wind. Eventually, you arrive, panting, at the crest itself, and you lie down on the ground to survey the scene below you. The western slope of the range of hills descends fairly gently into a valley, where all seems to be peace and harmony. The trees there seem still to be clothed in spring green. But this fresh copse ends with shocking abruptness in terrain which is unlike anything you have seen before, awake or in your worst nightmares. You realize that you are looking out over the blighted part of the forest, which your dream revealed to you. It is considerably larger than you thought it would be: it stretches for kilometres.
                Turn to 386.
                """,
            [116] = """
                The closer you get to Ishtra's pit - or Ishtra's mouth, as you are beginning to call it - the worse the pestilence becomes which has ravaged the forest. Even the brown and swollen leaves, which had given the trees some sort of covering, die out, and you walk silently among the dead shells of once magnificent oaks and beeches.
                Turn to 89.
                """,
            [6] = """
                You continue through the cursed forest. The leaves are brown and swollen beyond their normal size; there is a constant dripping of vile liquid from the leaves on to the soggy ground. Late in the afternoon - or so you guess it to be, but the sun is usually invisible beyond the seething brown mist - you come across a definite trail, tending to your left. You dread to think what manner of creatures may have made a trail in this doomed place. Will you follow the trail (turn to 69) or continue through the trackless trees (turn to 321)?
                """,
            [7] = """
                Unfortunately, a Bhorket, which was nearby, has the same ideas as you. You and this shaggy ape meet head on in a frantic dash for cover. Bhorket are usually timid creatures, but this one is terrified and will fight for its life. It kicks and gouges and pummels with its powerful arms and legs. Its high SKILL score reflects its ability to leap out of your reach.

                BHORKET
                SKILL 8
                STAMINA 11

                If the Bhorket ever wins two consecutive Attack Rounds against you, you lose your footing and tumble some way down the slope. You must deduct 1 extra STAMINA point before resuming the fight. If you win, you know that your destiny is waiting for you, and you must go to face it. Turn to 373.
                """,
            [8] = """
                You have chosen not only the name of a clan which has sent troops to aid Ishtra, but also the one whose insignia you are wearing. The Orc captain stands aside and lets you pass. Once the patrol has disappeared up the tunnel, you duck down behind the pile of bones to quieten your racing heart. Turn to 27.
                """,
            [9] = """
                The vision immediately changes and you are no longer walking beside a river, but seem to be pushing your way through a huge crowd. Elves like yourself jostle humans and Dwarfs; Giants brush aside Trolls and Ogres as if they were not there. Many of the faces seem to be familiar, though another part of your mind tells you that you have never seen them before. Will you try to talk to somebody (turn to 254) or not (turn to 235)?
                """,
            [35] = """
                Will you cast a Finding spell, to see if you can detect any danger up the tunnel (turn to 218), or preserve your POWER and risk exploring it without foreknowledge (turn to 279)?
                """,
            [36] = """
                The Goblin grunts, 'Huh! Carry on!', and lets you pass. Will you make for the door to your right (turn to 119) or the one to your left (turn to 330)?
                """,
            [37] = """
                Deduct 1 POWER point. When you rise into the air above the Weevil Man, he howls in surprise and runs away. Your ruse has worked and you may continue safely with your journey. Turn to 308.
                """,
            [38] = """
                Your fight with the first Pygmy Orc has you circling around each other, looking for an opening. The Orc makes a quick move to the right - and is struck in the neck by his comrade's dart! A look of surprise flits across his face; then he drops down dead. You turn to engage the second Pygmy Orc before he has time to reload his blowpipe. Turn to 369.
                """,
            [39] = """
                You can find nothing useful to take among the foul and barbarous instruments of torture which the Ogre dropped. Will you now take the passage north (turn to 351) or turn right and walk east (turn to 255)?
                """,
            [40] = """
                Tears well in your eyes as you raise your sword against this nightmare-spawned monster, which has taken over your village. As soon as you strike the first blow, the image changes and you see the peaceful village you left, where Elves go about their daily business as if they had no cares in the world. Again, you are viewing the scene from above, not as part of it, and the village recedes as if you were floating away. Gain 1 POWER point and turn to 231.
                """,
            [135] = """
                You take only one step towards the ancient pile of deer bones. Your intention alone is enough to galvanize the two stags into action. With a silent cry of 'Desecration!', they drive you effortlessly from the place. You lose 4 STAMINA points for the injuries they inflict, and 1 LUCK point for your foolish action. You pass through the rest of the valley and continue on your way. Turn to 383.
                """,
            [136] = """
                Roll one die.
                If you roll 1-4, turn to 220; if you roll 5-6, turn to 316.
                """,
            [137] = """
                Howling horribly, the Snow Ghost pummels you to the ground with frozen fists. You wake up with a start, sit up and crack your head on a thick root of the tree which is sheltering you: lose 1 STAMINA point. Restore your POWER score to what it was before the illusory combat took place. Turn to 372.
                """,
            [138] = """
                You gingerly push away the boulder with your feet, so that no lichen touches any bare skin. No sooner has the rock cleared the hand than all the light of the lichen around the cavern seems to converge on a ring the skeleton is wearing on that hand. The light then flashes from the ring to the pendant around your neck. The green stone burns icy-cold through your body and finds its way unerringly to your heart. Once your flesh has rotted, there will be two skeletons on the floor of the cavern.
                """,
            [139] = """
                A stately woman, dressed in robes which constantly change colour, greets you in the garden. She smiles upon you, introduces herself as Galana, goddess of plants and fertility, and gives you a message: "This garden may be the last haven you find, but you must persevere, even when all seems hopeless. There are two ways you may defeat Ishtra; both require considerable POWER, but one also requires physical artefacts to channel your POWER." You decide. You try to ask her what she means, but she is gone. Add 1 LUCK and 1 POWER point, and turn to 303.
                """,
            [216] = """
                You fly for an eternity over pocked earth, with steam and foul vapours rising from vents and polluting the air. The ride is exhilarating, despite the grim landscape below. Add 1 POWER point. The Drake sets you down in the shattered remnants of what was once a village, and then flies off. Turn to 48.
                """,
            [217] = """
                Soon you reach a point where you could either turn left or carry straight on. You can hear the harsh laughter of some cruel being up ahead, but to the left you sense a feeling of terrible doom. This is not an easy decision to make. Will you turn left (turn to 73) or carry straight on (turn to 156)?
                """,
            [218] = """
                Deduct 1 POWER point. The spell does not indicate the presence of any danger up the tunnel; perhaps there is none, or perhaps it is too far away for the spell to detect. At any rate, you enter the narrow mouth of the tunnel. Turn to 279.
                """,
            [219] = """
                At the foot of the mountain you meet a river of molten lava. As a salamander, you have no difficulty in crossing it, but on the other side you change back into an Elf and enter an entirely different aspect of the dream world. Add 1 POWER point and turn to 80.
                """,
        };

    private const string TranscribedSection191 = """
        The rot which has created the clearing has also started to affect the trees on the edge of the clearing. You step on something squelchy, and a foul smell rises; when you look down, you see that it was a root, rotted from the inside by the Evil in the soil. And then... was it a trick of the shifting half-light of this place, or did that root move slightly? No, it was not your imagination, for now several roots are squirming towards you, dripping noxious slime.

        And now they are joined by a dozen more, and now more. You must hack your way through these roots. If you have a hatchet, you can add 1 to your Attack Strength for each round, as you wield your sword in one hand and the hatchet in the other.

        ROOTS
        SKILL 6
        STAMINA 12

        If you win, you leave a foul mess behind you and break away, further from the clearing, where the rot has not yet warped the trees so much. Turn to 6.
        """;

    private const string TranscribedSection338 = """
        You still seem to be walking along a peaceful forest track; and you see another Wood Elf walking towards you. At first you think that this Wood Elf is just like yourself, but then you realize that it is yourself. You move to the left, and your double moves to its right, you step backwards and so does your double. You will have to fight your double! It has the same POWER score as you. Roll two dice. If you roll 2-7, your double has reduced your POWER by 2; if you roll 8-12, you have reduced its POWER by 2. If you lose, you have killed yourself; if you win, turn to 286.
        """;

    private static readonly IReadOnlyDictionary<int, int[]> VerifiedReferenceOverrides =
        new Dictionary<int, int[]>
        {
            [30] = [],
            [31] = [259],
            [32] = [109],
            [33] = [217],
            [34] = [],
            [1] = [67, 306],
            [5] = [337, 374],
            [6] = [69, 321],
            [7] = [373],
            [8] = [27],
            [9] = [254, 235],
            [2] = [123, 310],
            [10] = [311],
            [11] = [217],
            [35] = [218, 279],
            [13] = [43, 70],
            [14] = [92],
            [33] = [217],
            [37] = [308],
            [38] = [369],
            [39] = [351, 255],
            [40] = [231],
            [22] = [359, 217],
            [23] = [215, 116],
            [24] = [13, 274],
            [25] = [120, 169],
            [26] = [82, 312, 353],
            [28] = [383],
            [36] = [119, 330],
            [41] = [380],
            [42] = [100, 161],
            [43] = [14, 189],
            [44] = [217, 280],
            [46] = [340, 293],
            [47] = [375, 125],
            [48] = [208, 186],
            [49] = [144],
            [51] = [148, 227, 349],
            [53] = [73, 364],
            [54] = [],
            [55] = [217],
            [56] = [79, 103, 273],
            [57] = [239],
            [59] = [209, 236],
            [62] = [161],
            [67] = [380, 395],
            [66] = [],
            [70] = [14, 189],
            [71] = [157, 185],
            [74] = [135, 383],
            [76] = [25],
            [79] = [314],
            [83] = [171],
            [84] = [281, 333],
            [85] = [168],
            [89] = [146, 240],
            [93] = [57],
            [73] = [],
            [94] = [52, 242],
            [100] = [130, 317],
            [102] = [195, 388],
            [96] = [],
            [104] = [332],
            [105] = [106, 368],
            [106] = [160],
            [107] = [8, 155, 223],
            [108] = [38, 96],
            [109] = [171],
            [110] = [379, 61],
            [111] = [246, 284],
            [112] = [258],
            [114] = [386],
            [115] = [177, 210],
            [113] = [379],
            [116] = [89],
            [117] = [3, 174, 219],
            [118] = [133, 335],
            [119] = [21, 233],
            [120] = [169],
            [140] = [193, 388],
            [122] = [277, 321],
            [123] = [176, 320],
            [124] = [295, 61],
            [128] = [],
            [133] = [335],
            [132] = [123, 310],
            [134] = [147, 391],
            [135] = [383],
            [136] = [220, 316],
            [137] = [372],
            [138] = [],
            [139] = [303],
            [144] = [91, 341],
            [141] = [203, 363],
            [142] = [188],
            [149] = [105],
            [168] = [243, 94],
            [150] = [65, 282, 383],
            [189] = [92],
            [190] = [50],
            [147] = [371],
            [151] = [27, 304],
            [152] = [86, 321],
            [163] = [143],
            [164] = [253],
            [171] = [383],
            [172] = [83, 150],
            [159] = [260, 283],
            [178] = [18, 300],
            [177] = [288],
            [179] = [122, 315],
            [187] = [81, 118],
            [183] = [89],
            [194] = [],
            [196] = [272, 85],
            [197] = [265],
            [234] = [400],
            [262] = [372],
            [338] = [286],
            [345] = [100, 42],
            [186] = [397],
            [201] = [400],
            [198] = [289, 389],
            [211] = [61],
            [216] = [48],
            [217] = [73, 156],
            [218] = [279],
            [219] = [80],
            [224] = [115, 163],
            [229] = [257, 336],
            [228] = [263, 325],
            [238] = [73, 149],
            [244] = [134, 297],
            [246] = [],
            [247] = [85],
            [261] = [357, 390],
            [263] = [54, 145],
            [267] = [87, 394],
            [273] = [314],
            [274] = [112, 143, 288],
            [278] = [117, 165],
            [277] = [206],
            [281] = [193, 385],
            [282] = [383, 101],
            [287] = [116, 215],
            [290] = [380],
            [289] = [20, 49],
            [291] = [176, 300, 181],
            [292] = [346],
            [299] = [269, 330],
            [302] = [76],
            [303] = [224, 356],
            [304] = [107, 245],
            [307] = [146],
            [309] =
            [
                66, 92, 105, 109, 111, 121, 131, 137, 147, 150, 153, 154, 159, 160, 168, 169, 171, 175,
                176, 177, 179, 181, 183, 185, 187, 188, 189, 192, 193, 195, 197, 198, 201, 202, 204, 205,
                209, 211, 214, 215, 216, 217, 218, 219, 220, 221, 223, 227, 230, 232, 233, 234, 237, 240,
                242, 243, 245, 246, 248, 250, 251, 252, 253, 254, 256, 257, 259, 260, 262, 266, 267, 268,
                269, 270, 271, 272, 273, 274, 275, 276, 277, 279, 280, 281, 282, 283, 284, 286, 288, 290,
                291, 292, 294, 295, 296, 297, 299, 300, 302, 305, 307, 308, 312, 313, 314, 319, 321, 322,
                324, 325, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 337, 338, 341, 342, 344, 345,
                346, 347, 349, 350, 352, 353, 354, 356, 357, 359, 360, 361, 362, 363, 365, 367, 368, 370,
                371, 372, 373, 374, 377, 378, 379, 380, 381, 382, 383, 384, 386, 389, 390, 392, 395, 396,
                398, 399, 400
            ],
            [312] = [161, 353],
            [313] = [174, 213],
            [317] = [184, 393],
            [318] = [],
            [319] = [35, 114],
            [321] = [384],
            [348] = [5, 90],
            [323] = [19],
            [326] = [197, 265],
            [330] = [],
            [339] = [222, 275],
            [342] = [362],
            [333] = [26, 345],
            [353] = [42],
            [346] = [212],
            [361] = [105, 238],
            [363] = [127, 154, 256, 29],
            [369] = [281, 333],
            [374] = [237, 99, 132],
            [378] = [136],
            [381] = [204, 348],
            [382] = [98],
            [383] = [181, 56],
            [384] = [99, 252],
            [385] = [39],
            [315] = [123, 310],
            [386] = [7, 373],
            [389] = [360],
            [391] = [331],
            [392] = [350],
            [397] = [85],
            [203] = [29],
            [207] = [394],
            [213] = [278],
            [215] = [183, 116],
            [223] = [],
            [226] = [108, 369],
            [239] = [106, 199, 355],
            [250] = [167, 346, 128],
            [253] = [341, 172],
            [260] = [46, 142],
            [266] = [192],
            [268] = [214],
            [269] = [379, 113],
            [272] = [368],
            [275] = [332],
            [276] = [317],
            [279] = [124, 114],
            [280] = [217],
            [285] = [116],
            [286] = [244, 297],
            [293] = [361],
            [294] = [129, 298],
            [295] = [110, 182],
            [298] = [376, 50],
            [301] = [152, 324],
            [311] = [267],
            [314] = [181],
            [325] = [],
            [360] = [65, 334],
            [362] = [78, 151],
            [283] = [264, 216],
            [284] = [335, 133],
            [368] = [85],
            [371] = [331],
        };

    /// <summary>Written by Steve Jackson and Ian Livingstone.</summary>
    protected override string Author => "Steve Jackson and Ian Livingstone";

    /// <summary>
    /// Applies a complete graph when available, then overlays exits visually verified against the
    /// book. With the current empty placeholder, all other OCR-derived references remain candidates.
    /// </summary>
    protected override void PostProcessBook(Book book) => ApplyReferenceFixes(book);

    public static void ApplyReferenceFixes(Book book)
    {
        var edges = LoadEdges();
        bool hasCompleteGraph = edges.Count == MaxSections &&
            edges.Keys.All(section => section >= 1 && section <= MaxSections) &&
            edges.Values.Any(targets => targets.Count > 0);
        if (hasCompleteGraph)
        {
            foreach (var section in book.Sections)
            {
                section.ReferenceOverride = edges.TryGetValue(section.SectionNumber, out var targets)
                    ? targets
                    : new List<int>();
            }
        }

        foreach (var section in book.Sections)
        {
            if (VerifiedReferenceOverrides.TryGetValue(section.SectionNumber, out var targets))
                section.ReferenceOverride = targets.ToList();
        }
    }

    private static Dictionary<int, List<int>> LoadEdges()
    {
        using var stream = typeof(PhantomOfFearParser).Assembly
            .GetManifestResourceStream(EdgesResourceName)
            ?? throw new InvalidOperationException(
                $"Missing embedded graph resource {EdgesResourceName}.");

        using var doc = JsonDocument.Parse(stream);
        var edges = new Dictionary<int, List<int>>();
        foreach (var property in doc.RootElement.GetProperty("edges").EnumerateObject())
        {
            edges[int.Parse(property.Name)] = property.Value
                .EnumerateArray()
                .Select(e => e.GetInt32())
                .ToList();
        }
        return edges;
    }
}
