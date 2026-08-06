using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DungeonWorld.Infrastructure.Helpers;

public class PdfPigLayoutAnalyzer : ILayoutAnalyzer
{
    public bool IsDoublePageLayout(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        
        // Analyze first few content pages (skip cover, front matter)
        var pagesToCheck = document.GetPages()
            .Skip(5) // Skip intro pages
            .Take(3);
            
        int doublePageCount = 0;
        
        foreach (var page in pagesToCheck)
        {
            if (AnalyzePageForDoubleColumn(page))
                doublePageCount++;
        }
        
        // If 2+ of 3 sample pages are double-column, classify as double-page
        return doublePageCount >= 2;
    }

    public bool IsSinglePageLayout(string filePath) => !IsDoublePageLayout(filePath);

    private bool AnalyzePageForDoubleColumn(Page page)
    {
        var centerX = page.Width / 2;
        var gutterWidth = page.Width * 0.08; // 8% gutter tolerance
        
        var leftColumnWords = page.GetWords()
            .Where(w => w.BoundingBox.Right < centerX - (gutterWidth / 2))
            .Count();
            
        var rightColumnWords = page.GetWords()
            .Where(w => w.BoundingBox.Left > centerX + (gutterWidth / 2))
            .Count();
            
        var centerWords = page.GetWords()
            .Where(w => Math.Abs((w.BoundingBox.Left + w.BoundingBox.Right) / 2 - centerX) < gutterWidth / 2)
            .Count();
        
        // Double-page layout: significant words in both columns, minimal in center gutter
        return leftColumnWords > 20 && 
               rightColumnWords > 20 && 
               centerWords < 5;
    }
}