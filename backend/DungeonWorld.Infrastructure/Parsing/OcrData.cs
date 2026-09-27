namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Locates Tesseract language data. Prefers the repo-shipped best-accuracy
/// model plus FF vocabulary (<c>ocrdata/</c> next to the app), falling back
/// to the NuGet-provided <c>tessdata/</c> when absent (e.g. minimal deploys).
/// </summary>
public static class OcrData
{
    public static string DataPath()
    {
        string best = Path.Combine(AppContext.BaseDirectory, "ocrdata");
        if (File.Exists(Path.Combine(best, "eng.traineddata")))
            return best;
        return Path.Combine(AppContext.BaseDirectory, "tessdata");
    }
}
