namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// A paragraph of extracted text with enough layout metadata for a rule-based
/// parser to distinguish section headers and page furniture from body text.
/// </summary>
public sealed class TextBlock
{
    /// <summary>1-based logical page (for 2-up scans each half counts as a page).</summary>
    public int LogicalPage { get; init; }

    /// <summary>1-based PDF page index (physical page).</summary>
    public int PhysicalPage { get; init; }

    public string Text { get; init; } = "";

    /// <summary>Vertical position of the block top as a fraction of page height, 0 = top, 1 = bottom.</summary>
    public double TopFraction { get; init; }

    /// <summary>Average font size (glyph height) across the block.</summary>
    public double FontSize { get; init; }

    /// <summary>True if any word in the block uses a bold font face.</summary>
    public bool IsBold { get; init; }
}
