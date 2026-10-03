using System.Text.RegularExpressions;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF02 The Citadel of Chaos (Steve Jackson, 1983). Rebuilt from a manual reconstruction
/// manifest (300 dpi line transcripts, 400 sections across pages 17-109). Front matter is
/// pages 1-16; the intro uses the cover, title/background and HISTORY pages.
/// </summary>
public sealed class CitadelOfChaosParser : ManifestDungeonWorldParser
{
    public CitadelOfChaosParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "CitadelOfChaos";
    protected override string TitleMatch => "Citadel of Chaos";
    protected override string Slug => "ff02_citadel_of_chaos";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff02.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 4, 5, 16 };

    /// <summary>
    /// Hand fixes for sections whose garble survives the shared repair, written
    /// against post-normalization text (the shared TurnToRepair pass runs first,
    /// so patterns match its output: clean verbs, remaining digit garbles and
    /// truncations). Targets verified against 600 dpi crops, transcriptions and
    /// walkthroughs (arek's solution validates 95→367; narrative coherence
    /// validates 100→276). Section 192's old "Turn to 3" arm was retired as a
    /// substring footgun; its "Turn to SET a EERE TT Re" garble needs the book.
    /// Round 2 (600 dpi): S77's "lurm to 355" reads clean "turn to 355" (lurm
    /// stays book-local: single instance); S92's "Turn io 136" reads clean
    /// "Turn to 156" (courtyard-onward theme confirms 156 over 136); S99's
    /// left door "turn to gz" is 92 by dual-resolution agreement (g→9); S151's
    /// left staircase "lun lo 1g" reads clean "turn to 19"; S338's "Tum to ge"
    /// is the riverside 90 (g→9, o→0, dual-resolution agreement).
    /// Round 3 (arek's chain + themes): S77's E.S.P. branch "lam to 187" is
    /// the mind-read 187 (S187's theme is unmistakable; 600 dpi's "17" dropped
    /// a digit); S99's right-hand door is the passageway-room 38 (arek's
    /// chain is explicit and the theme fits; both OCRs misread trailing dirt
    /// differently). S77's Creature Copy number stays open (44/94?).
    /// Round 4 (arek's chain head + Kylltrog-name bluff): S1's herbalist
    /// branch reads "turn to 28", but S28 is a fireball scene while S261 is
    /// the Ape-Dog herb inspection, so the branch target lost its "61"
    /// (S28 keeps its 139/350 parents). S261's three name-bluff exits resolve
    /// by elimination against their aftermaths: Eylitrone "Turn to Ba" is 81
    /// (arek-explicit; dual-resolution-stable with single-instance a→1);
    /// Pincus "Twn te 175" is 175 (S175 names Pincus explicitly); Bla "Turn
    /// ter 394" is 394 (only number left, theme fits the familiar-name
    /// reaction). S1's shelter branch ("Turn lo ze") was resolved in round 10.
    /// Round 5 (Whirlwind decisions): S245's magic branch reads "turn to 37"
    /// at 300 dpi but clean "47" at 600 dpi, and S47 is the whirlwind
    /// spell-choice scene (S37's skin-and-hissing room is unrelated), so the
    /// 4→3 misread is corrected; S245's talk branch "(ELrm bo 390" resolves
    /// to 390 (bo-prep stable across resolutions, S390's torment scene fits
    /// talking to her, digits 3/9 stable); S235's "(tum fo 2451" is 245 plus
    /// a speck-digit on the arek-confirmed Citadel road.
    /// Round 6 (fireside-to-library chain): S339's "Turn tor 134" is
    /// arek-explicit (opening the shared-free "tor"-as-"to" class, kept
    /// book-local like barn/harn); S339's "Timm tor 140" stays open (theme
    /// clash with the tower stairs, needs the scan). S134's "(turn to 90)"
    /// reads clean "(tum to 60)" at 600 dpi on the arek-confirmed password
    /// branch (S90 keeps its 391 parent). S60's fight branch "(fumio 213)"
    /// reads "(tam to 213)" at 600 dpi on the arek-confirmed fight, and its
    /// Illusion branch "Turn ko 293" carries the verified ko-preposition to
    /// the trident-stopping aftermath S293 confirms. S293's own "Tam fo 374"
    /// reads "Torm to 374" at 600 dpi on arek's trident-drop link.
    /// Round 7 (brass-door landing): S68's brass branch "Turn io 207" reads
    /// clean "Turn to 207" at 600 dpi on the arek-confirmed door (io-prep as
    /// in S92), and its bronze branch "Turn tr 354" reads clean "Turn to 354"
    /// the same way. S188's slash branch "Turn loge" carries no recoverable
    /// digits at either resolution, but arek walks it to S51, S51's "slash
    /// about madly" continues S188's "slash out" verbatim, and S51's own
    /// 51→280 link is independently consistent — recorded as transcription
    /// evidence with the digit garble noted. S188's Strength branch and S30's
    /// "urn boo z33" were resolved in round 10 (301 and 241 respectively).
    /// Round 8 (mid-chain garbles): S156's "Turn tor 114" is arek-explicit
    /// (second instance of the book-local tor-class opened at S339); S273's
    /// "turn to 372" answers the Scimitar password with 371, whose door-open
    /// aftermath fits while S372's bottle aftermath does not; S323's "Turn io
    /// 144" carries the S92-verified io-preposition on arek's copper-door
    /// branch; S169's "Tum to 3317" is 317 plus a leading dirt digit (same
    /// family as "3857" and "2949") on arek's paintings branch; S228's Copper
    /// Key branch "Turn ty 2946" is 296 plus trailing dirt (ty-prep as in
    /// FF05 S25, S296's door-opens aftermath fits) and its Strength branch
    /// "lien te 170" resolves via shared "te" to S170's strength-surge scene;
    /// S25's ignore-the-box branch "Tum ts 206" is arek-explicit (shared "Tum"
    /// plus single-instance "ts"-as-"to").
    /// Round 9 (structural necessity): keeping S273 at 371 over dual-OCR 372
    /// because S371's door-opens scene has no other candidate parent, S372
    /// already has S24's Shielding setup, and S371's own "Tom to 177" exit
    /// (single-instance Tom-as-Turn) feeds S177's only live parent.
    /// Round 10 (book answers, user-read): S10's "Turn foagy" is the pantry
    /// 249; S77's Creature Copy "Turn to 940" is the Dire-duplicate 349
    /// (S349's theme is unmistakable; the 940/446 garbles wobble first and
    /// last digits); S151's right staircase "lum toagy" is the balcony 197;
    /// S1's shelter branch "Turn lo ze" is the refused-shelter 20 (S20's
    /// scene matches verbatim); S188's Strength branch "Turn mo 3m" is 301;
    /// S30's fourth-hit "urn boo z33" is the sword-tangled 241; S140's
    /// right-hand door "urn iozog" is 104, double-confirmed by arek.
    /// Round 11 (Spikes walkthrough chain): S206's "Turn to asa." is the
    /// Gangees room 182 (Spikes walks it; S182's torch-out theme fits; the
    /// digits are unmappable, same class as S188's "loge"); S91's
    /// "(tum io 140)" is the sneak-out-far-door 140 (Spikes walks it, io-prep
    /// as in S92/S323, digits clean); S292's gift branch "Lien to gz" is the
    /// hairbrush-offer 42 (Spikes ties gift explicitly to 42, S42's offer
    /// scene fits while S92's gremlin fluid clashes — overriding the naive
    /// gz→92 map). S292's leave-branch number is genuinely absent (book).
    /// Round 12 (Nostalgic Bookshelf playthrough): S230's gold-nugget branch
    /// "turn to 10 96" is 96 with a stuttered "to" misread as "10" (same
    /// digit-letter class as FF05 S376's "fun 10 163"; S96's accepted-offering
    /// scene fits the Fool's Gold rock exactly), and its battle branch
    /// "(furn [ny 288)" is 288 (S288's Ape-Dog attack fits preparing for
    /// battle). S10 keeps its other parents.
    /// Round 13 (review): S339's join branch "Timm tor 140" resolves to 140
    /// after all — S140's leave-room-to-tower-stairs text continues the
    /// fireside-joining scene naturally, tor-class matches the sibling fix,
    /// digits clean, slot certain.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        if (sectionNumber == 50) return "Turn to 164.";
        return sectionNumber switch
        {
            1 => content.Replace("pose as a herbalist? turn to 28", "pose as a herbalist? turn to 261", StringComparison.Ordinal)
                .Replace("Turn lo ze", "Turn to 20", StringComparison.Ordinal),
            10 => content.Replace("Turn foagy.", "Turn to 249.", StringComparison.Ordinal),
            25 => content.Replace("Tum ts 206", "Turn to 206", StringComparison.Ordinal),
            30 => content.Replace("urn boo z33.", "turn to 241.", StringComparison.Ordinal),
            60 => content.Replace("Tusion Spell Turn ko 293", "Tusion Spell Turn to 293", StringComparison.Ordinal)
                .Replace("(fumio 213).", "(turn to 213).", StringComparison.Ordinal),
            68 => content.Replace("Turn io 207", "Turn to 207", StringComparison.Ordinal)
                .Replace("Turn tr 354", "Turn to 354", StringComparison.Ordinal),
            74 => content.Replace("counter-attack. Tum", "counter-attack. Turn to 377.", StringComparison.Ordinal),
            77 => content.Replace("lurm to 355", "turn to 355", StringComparison.Ordinal)
                .Replace("lam to 187", "turn to 187", StringComparison.Ordinal)
                .Replace("Turn to 940", "Turn to 349", StringComparison.Ordinal),
            91 => content.Replace("leave anyway (tum io 140).", "leave anyway (turn to 140).", StringComparison.Ordinal),
            92 => content.Replace("Turn io 136", "Turn to 156", StringComparison.Ordinal),
            95 => content.Replace("turn to 357.", "Turn to 367.", StringComparison.Ordinal),
            99 => content.Replace("(turn to gz)", "(turn to 92)", StringComparison.Ordinal)
                .Replace("(turn to 3857", "(turn to 38", StringComparison.Ordinal),
            100 => content.Replace("I not, turn to 107", "If not, turn to 276.", StringComparison.Ordinal)
                .Replace("1 not, turn to 107", "If not, turn to 276.", StringComparison.Ordinal),
            102 => content.Replace("turn to 0.", "turn to 270.", StringComparison.Ordinal),
            134 => content.Replace("{turn to 90), leave them", "{turn to 60), leave them", StringComparison.Ordinal),
            140 => content.Replace("[urn iozog)?", "[turn to 104)?", StringComparison.Ordinal),
            151 => content.Replace("{lun lo 1g)", "{turn to 19)", StringComparison.Ordinal)
                .Replace("(lum toagy).", "(turn to 197).", StringComparison.Ordinal),
            156 => content.Replace("Turn tor 114", "Turn to 114", StringComparison.Ordinal),
            169 => content.Replace("Tum to 3317", "Turn to 317", StringComparison.Ordinal),
            177 => content.Replace("down the steps (hum", "down the steps (turn to 344).", StringComparison.Ordinal),
            188 => content.Replace("Turn loge", "Turn to 51", StringComparison.Ordinal)
                .Replace("Turn mo 3m", "Turn to 301", StringComparison.Ordinal),
            205 => content.Replace("tien Lo 300", "turn to 368", StringComparison.Ordinal),
            206 => content.Replace("Turn to asa.", "Turn to 182.", StringComparison.Ordinal),
            228 => content.Replace("Turn ty 2946", "Turn to 296", StringComparison.Ordinal)
                .Replace("lien te 170", "turn to 170", StringComparison.Ordinal),
            229 => content.Replace("(fur to 230", "(turn to 230).", StringComparison.Ordinal),
            230 => content.Replace("(turn to 10 96),", "(turn to 96),", StringComparison.Ordinal)
                .Replace("(furn [ny 288).", "(turn to 288).", StringComparison.Ordinal),
            235 => content.Replace("(tum fo 2451", "(turn to 245", StringComparison.Ordinal),
            245 => content.Replace("{turn to 37)?", "{turn to 47)?", StringComparison.Ordinal)
                .Replace("(ELrm bo 390,", "(turn to 390,", StringComparison.Ordinal),
            261 => content.Replace("Eylitrone Turn to Ba", "Eylitrone Turn to 81", StringComparison.Ordinal)
                .Replace("Pincus Twn te 175", "Pincus Turn to 175", StringComparison.Ordinal)
                .Replace("Bla Turn ter 394", "Bla Turn to 394", StringComparison.Ordinal),
            273 => content.Replace("Scimitar? turn to 372", "Scimitar? turn to 371", StringComparison.Ordinal),
            292 => content.Replace("gift for her? Lien to gz", "gift for her? turn to 42", StringComparison.Ordinal),
            293 => content.Replace("Tam fo 374.", "turn to 374.", StringComparison.Ordinal),
            323 => content.Replace("Turn io 144", "Turn to 144", StringComparison.Ordinal),
            330 => Regex.Replace(
                content.Replace("ten Lo 208", "turn to 208.", StringComparison.Ordinal),
                @"turn to 33\b", "turn to 120."),
            338 => content.Replace("Tum to ge.", "Turn to 90.", StringComparison.Ordinal),
            339 => content.Replace("Turn tor 134", "Turn to 134", StringComparison.Ordinal)
                .Replace("Timm tor 140", "Turn to 140", StringComparison.Ordinal),
            354 => Regex.Replace(content, @"turn to 355\b", "Turn to 188."),
            371 => content.Replace("Tom to 177.", "Turn to 177.", StringComparison.Ordinal),
            _ => content,
        };
    }

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));
}
