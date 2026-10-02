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
    /// Hand-verified turn-to targets the shared repair cannot reach: section 31
    /// ends with a numberless "Turnto" (the exit number, 90, comes from the
    /// published book text), and section 296's "Turn to 4z2." maps out of range
    /// either way (422), so the drop-z reading (42) is applied explicitly.
    /// Section 220's cut "Turnto" is the dead-end passage exit 171 (book-read).
    /// Everything else (26g/269, go/90, g6/96, 6g/69, g2/92, g4/94) is handled
    /// by the shared TurnToRepair g→9 mapping.
    /// Public and static so mappings are unit-testable without running the
    /// full OCR-merge pipeline.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content) =>
        sectionNumber switch
        {
            31 => content.Replace("north door. Turnto", "north door. Turn to 90.", StringComparison.Ordinal),
            220 => content.Replace("dead-end passage. Turnto", "dead-end passage. Turn to 171.", StringComparison.Ordinal),
            296 => content.Replace("Turn to 4z2.", "Turn to 42.", StringComparison.Ordinal),
            _ => content,
        };

    protected override void PostProcessSections(DungeonWorld.Core.Entities.Book book)
    {
        foreach (var section in book.Sections)
            section.Content = ApplySectionFixes(section.SectionNumber, section.Content);
    }
}
