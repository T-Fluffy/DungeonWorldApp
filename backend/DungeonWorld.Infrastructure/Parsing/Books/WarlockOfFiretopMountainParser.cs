using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF01 The Warlock of Firetop Mountain (Steve Jackson &amp; Ian Livingstone, 1982).
/// A pure scan with no embedded text: headers are detected from OCR and the generic
/// rule pipeline handles the rest. The only book-specific quirk is that section
/// headers can sit as low as ~90% of the page height (there are no bottom-of-page
/// numbers in this edition, only a section-range folio at the top), so the default
/// PageNumberBand would wrongly discard them.
/// </summary>
public sealed class WarlockOfFiretopMountainParser : DungeonWorldBookParserBase
{
    public WarlockOfFiretopMountainParser(
        IPdfTextExtractor textExtractor,
        IOptions<FileStorageOptions> storageOptions)
        : base(textExtractor, storageOptions)
    {
    }

    public override string ParserId => "WarlockOfFiretopMountain";

    public override bool CanHandle(string filePath, string bookTitle) =>
        bookTitle.Contains("Warlock of Firetop Mountain", StringComparison.OrdinalIgnoreCase);

    protected override double PageNumberBand => 0.97;

    /// <summary>
    /// Hand-verified turn-to targets from the published book text (Gamebookuino
    /// transcription cross-checked against walkthroughs). OCR garbles resolved:
    /// 16 "26g"→269, 25/45/388 "go"→90, 31 numberless "Turnto"→90, 34 "g6"→96,
    /// 162 "6g"→69, 296 "4z2"→42 (stray z), 343 "g2"→92, 359 "g4"→94.
    /// Section 220 ends with a numberless "Turnto" whose target is lost, and
    /// 400's victory tail is truncated mid-word; both need the physical book.
    /// Public and static so mappings are unit-testable without the full
    /// OCR-merge pipeline.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content) =>
        sectionNumber switch
        {
            16 => content.Replace("(turn to 26g).", "(turn to 269).", StringComparison.Ordinal),
            25 => content.Replace("straightaway (turn to go)", "straightaway (turn to 90)", StringComparison.Ordinal),
            31 => content.Replace("north door. Turnto", "north door. Turn to 90.", StringComparison.Ordinal),
            34 => content.Replace("Turn to g6.", "Turn to 96.", StringComparison.Ordinal),
            45 => content.Replace("Turn to go.", "Turn to 90.", StringComparison.Ordinal),
            162 => content.Replace("(turn to 6g).", "(turn to 69).", StringComparison.Ordinal),
            296 => content.Replace("Turn to 4z2.", "Turn to 42.", StringComparison.Ordinal),
            343 => content.Replace("Turn to g2.", "Turn to 92.", StringComparison.Ordinal),
            359 => content.Replace("Turn to g4", "Turn to 94.", StringComparison.Ordinal),
            388 => content.Replace("turn to go. Lose 1 more", "turn to 90. Lose 1 more", StringComparison.Ordinal),
            _ => content,
        };

    protected override void PostProcessSections(DungeonWorld.Core.Entities.Book book)
    {
        foreach (var section in book.Sections)
            section.Content = ApplySectionFixes(section.SectionNumber, section.Content);
    }
}
