// File: DungeonWorld.Infrastructure/Parsers/SinglePageParser.cs
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Helpers;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsers;

/// <summary>
/// Parser for single-page layout PDFs (standard book format)
/// Example: Later Fighting Fantasy books, digital editions
/// </summary>
public class SinglePageParser : BaseDungeonWorldParser
{
    public override string ParserId => "SinglePage";
    
    public SinglePageParser(IOptions<FileStorageOptions> storageOptions) 
        : base(storageOptions) { }

    public override bool CanHandle(string filePath, string bookTitle)
    {
        // Use layout analyzer to confirm single-page
        var analyzer = new PdfPigLayoutAnalyzer();
        return analyzer.IsSinglePageLayout(filePath);
    }

    protected override List<LineInfo> ExtractLinesFromPage(UglyToad.PdfPig.Content.Page page)
    {
        // Full-width extraction for single-page layout
        return ExtractLinesFromArea(page, 0, page.Width);
    }
}