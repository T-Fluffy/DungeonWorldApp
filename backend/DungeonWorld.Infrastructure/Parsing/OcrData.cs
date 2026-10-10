namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Locates Tesseract language data. Prefers the repo-shipped best-accuracy
/// model plus FF vocabulary (<c>ocrdata/</c> next to the app), falling back
/// to the NuGet-provided <c>tessdata/</c> when absent (e.g. minimal deploys).
/// </summary>
public static class OcrData
{
    public static string DataPath() => DataPath(null);

    /// <summary>
    /// Resolves a Tesseract data directory. <paramref name="variant"/> selects an isolated
    /// folder so a book-specific vocabulary can be widened without altering the OCR output
    /// of books that are already complete — <c>OcrData.DataPath()</c> must keep returning the
    /// shared folder for every existing caller.
    /// </summary>
    public static string DataPath(string? variant)
    {
        string root = AppContext.BaseDirectory;
        if (!string.IsNullOrWhiteSpace(variant))
        {
            string scoped = Path.Combine(root, variant);
            if (File.Exists(Path.Combine(scoped, "eng.traineddata")))
                return scoped;
        }
        string best = Path.Combine(root, "ocrdata");
        if (File.Exists(Path.Combine(best, "eng.traineddata")))
            return best;
        return Path.Combine(root, "tessdata");
    }
}
