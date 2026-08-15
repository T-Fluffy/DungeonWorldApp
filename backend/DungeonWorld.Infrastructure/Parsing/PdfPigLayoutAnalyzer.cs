using UglyToad.PdfPig;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Layout detector for diagnostics. Uses the same landscape aspect-ratio heuristic
/// as <see cref="PdfPigTextExtractor"/> so the report matches what the parser will do.
/// </summary>
public class PdfPigLayoutAnalyzer
{
    // Keep in sync with PdfPigTextExtractor.DoublePageAspectThreshold.
    private const double DoublePageAspectThreshold = 1.15;

    public bool IsDoublePageLayout(string filePath)
    {
        using var document = PdfDocument.Open(filePath);

        // Analyze a few content pages (skip cover and front matter).
        var sample = document.GetPages().Skip(5).Take(3).ToList();
        if (sample.Count == 0) return false;

        int doubleCount = sample.Count(p => p.Width > p.Height * DoublePageAspectThreshold);
        return doubleCount >= Math.Max(1, sample.Count / 2);
    }

    public bool IsSinglePageLayout(string filePath) => !IsDoublePageLayout(filePath);
}
