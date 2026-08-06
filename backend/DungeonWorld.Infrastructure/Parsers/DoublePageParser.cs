using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Helpers;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsers;

/// <summary>
/// Parser for double-page layout PDFs (2-up format)
/// Example: "Seas of Blood" and many scanned FF books
/// </summary>
public class DoublePageParser : BaseDungeonWorldParser
{
    public override string ParserId => "DoublePage";
    
    public DoublePageParser(IOptions<FileStorageOptions> storageOptions) 
        : base(storageOptions) { }

    public override bool CanHandle(string filePath, string bookTitle)
    {
        var analyzer = new PdfPigLayoutAnalyzer();
        return analyzer.IsDoublePageLayout(filePath);
    }

    protected override List<LineInfo> ExtractLinesFromPage(UglyToad.PdfPig.Content.Page page)
    {
        var allLines = new List<LineInfo>();
        var midpoint = page.Width / 2;
        
        // Extract LEFT column first (logical reading order)
        allLines.AddRange(ExtractLinesFromArea(page, 0, midpoint));
        
        // Then extract RIGHT column
        allLines.AddRange(ExtractLinesFromArea(page, midpoint, page.Width));
        
        return allLines;
    }
}