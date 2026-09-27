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
    /// Hand-verified turn-to target: section 359's south exit "Turn to g4" is
    /// section 94, currently unreachable with no other incoming edge (section
    /// 64, the only other reading, is already well connected). Public and
    /// static so the mapping is unit-testable without running the full
    /// OCR-merge pipeline.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content) =>
        sectionNumber switch
        {
            359 => content.Replace("Turn to g4", "Turn to 94.", StringComparison.Ordinal),
            _ => content,
        };

    protected override void PostProcessSections(DungeonWorld.Core.Entities.Book book)
    {
        foreach (var section in book.Sections)
            section.Content = ApplySectionFixes(section.SectionNumber, section.Content);
    }
}
