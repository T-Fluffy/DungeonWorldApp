using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Finds the illustration region(s) inside a full-page scan that mixes text with artwork.
///
/// The heuristic is ink density: a scanned text page is mostly white with sparse thin
/// strokes (~1-6% dark pixels per row band), while an illustration is a dense contiguous
/// ink block (bands of 20-90%). The detector downscales the page for speed, computes a
/// per-row-band density profile, merges dense bands into vertical runs, keeps runs that
/// are tall enough and strong enough to be artwork (not a bold heading), then locates the
/// horizontal extent of each run by column density. Text-only pages yield no regions.
/// </summary>
public class ArtRegionDetector
{
    /// <summary>Analysis bitmap target width in pixels (height follows aspect ratio).</summary>
    public const int AnalysisWidth = 160;

    /// <summary>Rows of the analysis bitmap grouped into one band.</summary>
    public const int BandHeight = 2;

    /// <summary>Band density above this is "dense" (text sits far below this).</summary>
    public const double DenseBandThreshold = 0.10;

    /// <summary>A run must contain a band at least this dense to count as art, not a heading.</summary>
    public const double StrongBandThreshold = 0.18;

    /// <summary>A run must span at least this fraction of the page height.</summary>
    public const double MinArtHeightFraction = 0.07;

    /// <summary>Dense bands closer than this many bands apart are merged into one run.</summary>
    public const int MaxGapBands = 2;

    /// <summary>Column density above this inside the run counts toward the horizontal extent.</summary>
    public const double ColumnDensityThreshold = 0.10;

    /// <summary>Extra margin in source pixels added around each cropped region.</summary>
    public const int MarginPx = 4;

    /// <summary>Returns crop rectangles in source-image coordinates, empty for text-only pages.</summary>
    public IReadOnlyList<Rectangle> Detect(Bitmap source)
    {
        if (source.Width <= 0 || source.Height <= 0)
            return Array.Empty<Rectangle>();

        using var small = Downscale(source);
        double scaleX = (double)source.Width / small.Width;
        double scaleY = (double)source.Height / small.Height;

        int bandCount = small.Height / BandHeight;
        var densities = new double[bandCount];
        for (int b = 0; b < bandCount; b++)
        {
            int y0 = b * BandHeight;
            int y1 = Math.Min(y0 + BandHeight, small.Height);
            int dark = 0, total = 0;
            for (int y = y0; y < y1; y++)
            {
                for (int x = 0; x < small.Width; x++)
                {
                    total++;
                    if (IsDark(small.GetPixel(x, y))) dark++;
                }
            }
            densities[b] = total > 0 ? (double)dark / total : 0;
        }

        // Build a dense-band mask.
        var dense = new bool[bandCount];
        double runMax = 0;
        for (int b = 0; b < bandCount; b++)
        {
            dense[b] = densities[b] >= DenseBandThreshold;
            if (dense[b]) runMax = Math.Max(runMax, densities[b]);
        }
        if (runMax < StrongBandThreshold)
            return Array.Empty<Rectangle>();

        // Merge dense bands into runs (allow small gaps).
        var runs = new List<(int StartBand, int EndBand)>();
        int i = 0;
        while (i < bandCount)
        {
            if (!dense[i]) { i++; continue; }
            int start = i;
            int end = i;
            int gap = 0;
            while (end + 1 < bandCount)
            {
                if (dense[end + 1]) { end++; gap = 0; }
                else if (gap < MaxGapBands) { end++; gap++; }
                else break;
            }
            i = end + 1;
            if (end - start + 1 >= 0) runs.Add((start, end));
        }

        var result = new List<Rectangle>();
        foreach (var (startBand, endBand) in runs)
        {
            // Skip runs that are not tall enough to be an illustration.
            double runHeightFraction = (double)((endBand - startBand + 1) * BandHeight) / small.Height;
            if (runHeightFraction < MinArtHeightFraction)
                continue;

            // The run must actually contain a strong band (re-check against merged runs).
            double strong = 0;
            for (int b = startBand; b <= endBand; b++)
                strong = Math.Max(strong, densities[b]);
            if (strong < StrongBandThreshold)
                continue;

            int topPx = startBand * BandHeight;
            int bottomPx = Math.Min((endBand + 1) * BandHeight, small.Height);

            // Horizontal extent: columns within the run with meaningful density.
            int colMin = int.MaxValue, colMax = -1;
            for (int x = 0; x < small.Width; x++)
            {
                int dark = 0, total = 0;
                for (int y = topPx; y < bottomPx; y++)
                {
                    total++;
                    if (IsDark(small.GetPixel(x, y))) dark++;
                }
                double cd = total > 0 ? (double)dark / total : 0;
                if (cd >= ColumnDensityThreshold)
                {
                    colMin = Math.Min(colMin, x);
                    colMax = Math.Max(colMax, x);
                }
            }

            if (colMax < 0) continue;

            // Map back to source coordinates with a small margin.
            int sx = (int)Math.Round(colMin * scaleX);
            int sy = (int)Math.Round(topPx * scaleY);
            int sw = (int)Math.Round((colMax - colMin + 1) * scaleX);
            int sh = (int)Math.Round((bottomPx - topPx) * scaleY);

            sx = Math.Clamp(sx - MarginPx, 0, source.Width - 1);
            sy = Math.Clamp(sy - MarginPx, 0, source.Height - 1);
            sw = Math.Clamp(sw + 2 * MarginPx, 1, source.Width - sx);
            sh = Math.Clamp(sh + 2 * MarginPx, 1, source.Height - sy);

            result.Add(new Rectangle(sx, sy, sw, sh));
        }

        return result;
    }

    private static bool IsDark(Color c)
    {
        double lum = c.R * 0.299 + c.G * 0.587 + c.B * 0.114;
        return lum < 100;
    }

    private static Bitmap Downscale(Bitmap source)
    {
        int newHeight = Math.Max(1, (int)Math.Round(AnalysisWidth * (double)source.Height / source.Width));
        var bmp = new Bitmap(AnalysisWidth, newHeight, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(source, 0, 0, AnalysisWidth, newHeight);
        }
        return bmp;
    }
}
