using System.Drawing;
using System.Drawing.Imaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Extracts only the illustration artwork from a gamebook PDF, skipping text pages.
///
/// Two kinds of source are handled automatically per page image:
///   - Embedded art (digital PDFs such as FF16): the image's bounding box covers only a
///     fraction of the page, so the image is already a standalone illustration -> export as-is.
///   - Full-page scans (FF01, FF08, spread scans, ...): the image fills the page and mixes
///     text with art. An <see cref="ArtRegionDetector"/> locates the dense ink blocks that
///     form the illustration(s) and crops them out.
///
/// Output goes to <c>{outputDir}/{slug}/p{page}.png</c> (and <c>p{page}_1.png</c> ... when a
/// page contains several illustrations). Text-only pages produce no file.
/// </summary>
public class MediaArtParser
{
    /// <summary>A page image whose bounds cover at least this fraction of the page is a full-page scan.</summary>
    public const double FullPageScanCoverage = 0.85;

    /// <summary>Ignore embedded images below this size — they are noise, not artwork. Not applied to full-page scans.</summary>
    public const int MinArtDimension = 100;

    private readonly ArtRegionDetector _detector;

    public MediaArtParser(ArtRegionDetector? detector = null)
    {
        _detector = detector ?? new ArtRegionDetector();
    }

    /// <summary>Outcome for one page: either embedded art exported whole, or scan crops.</summary>
    public sealed class PageResult
    {
        public required int PageNumber { get; init; }
        public required int FileCount { get; init; }
        public required bool WasEmbeddedArt { get; init; }
    }

    /// <summary>
    /// Renders the artwork for a whole PDF into <c>{outputDir}/{slug}</c> and returns a per-page
    /// report. <see cref="PageResult.FileCount"/> is 0 for text-only pages (skipped).
    /// </summary>
    public IReadOnlyList<PageResult> Extract(string pdfPath, string outputDir, string slug)
    {
        string bookDir = Path.Combine(outputDir, slug);
        Directory.CreateDirectory(bookDir);

        var results = new List<PageResult>();
        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
        {
            int written = 0;
            bool embedded = false;

            foreach (var image in page.GetImages())
            {
                if (image.IsImageMask)
                    continue;

                double boundsArea = image.Bounds.Width * image.Bounds.Height;
                double pageArea = page.Width * page.Height;
                double coverage = boundsArea / pageArea;

                if (coverage >= FullPageScanCoverage)
                {
                    // Full-page scan: the page itself is art or mixes text with art, so its
                    // size is whatever the scan is; do not apply the tiny-noise filter.
                    if (!TryDecode(image, out var bitmap))
                        continue;

                    try
                    {
                        var regions = _detector.Detect(bitmap);
                        for (int i = 0; i < regions.Count; i++)
                        {
                            var fileName = i == 0 ? $"p{page.Number}.png" : $"p{page.Number}_{i}.png";
                            using var crop = Crop(bitmap, regions[i]);
                            SavePng(crop, Path.Combine(bookDir, fileName));
                            written++;
                        }
                    }
                    finally
                    {
                        bitmap.Dispose();
                    }
                }
                else
                {
                    // Embedded standalone art: ignore tiny noise images, export the rest whole.
                    if (image.WidthInSamples < MinArtDimension || image.HeightInSamples < MinArtDimension)
                        continue;

                    embedded = true;
                    if (!TryDecode(image, out var bitmap))
                        continue;

                    try
                    {
                        var fileName = written == 0 ? $"p{page.Number}.png" : $"p{page.Number}_{written}.png";
                        SavePng(bitmap, Path.Combine(bookDir, fileName));
                        written++;
                    }
                    finally
                    {
                        bitmap.Dispose();
                    }
                }
            }

            if (written > 0)
            {
                results.Add(new PageResult
                {
                    PageNumber = page.Number,
                    FileCount = written,
                    WasEmbeddedArt = embedded,
                });
            }
        }

        return results;
    }

    private static bool TryDecode(IPdfImage image, out Bitmap bitmap)
    {
        bitmap = null!;
        try
        {
            // TryGetPng/TryGetBytes are unreliable in this PdfPig build, but RawBytes
            // carries the full encoded stream (JPEG for these scans), which decodes fine.
            var raw = image.RawBytes.ToArray();
            using var ms = new MemoryStream(raw);
            using var loaded = new Bitmap(ms);
            // Materialize a standalone copy: a Bitmap created from a stream keeps the
            // stream open, and saving later throws a generic GDI+ error once it is disposed.
            bitmap = new Bitmap(loaded);
            return true;
        }
        catch
        {
            // Un-decodable or corrupt image — skip.
        }
        return false;
    }

    private static Bitmap Crop(Bitmap source, Rectangle rect)
    {
        int x = Math.Clamp(rect.X, 0, source.Width - 1);
        int y = Math.Clamp(rect.Y, 0, source.Height - 1);
        int w = Math.Clamp(rect.Width, 1, source.Width - x);
        int h = Math.Clamp(rect.Height, 1, source.Height - y);

        // GDI+ Clone throws OutOfMemory on grayscale/ICC JPEGs, so draw into a fresh
        // 32bpp bitmap instead (works for every source format).
        var crop = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(crop))
        {
            g.DrawImage(source,
                new Rectangle(0, 0, w, h),
                new Rectangle(x, y, w, h),
                GraphicsUnit.Pixel);
        }
        return crop;
    }

    private static void SavePng(Bitmap bitmap, string path)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        File.WriteAllBytes(path, ms.ToArray());
    }
}
