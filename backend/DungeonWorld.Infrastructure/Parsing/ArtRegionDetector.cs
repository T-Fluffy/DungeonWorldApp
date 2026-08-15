using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Finds the illustration region(s) inside a full-page scan that mixes text with artwork.
///
/// The heuristic is ink density at full resolution: a scanned text page is mostly white
/// with sparse thin strokes, so the longest contiguous run of dense row bands is short
/// (typically under 40px), while an illustration is a dense contiguous ink block whose
/// run is tall (typically over 90px). The detector:
///
///   1. Computes a per-row-band density profile on the full-resolution page.
///   2. Gates the page: only pages with a gap-free dense run tall enough (&gt;= 96px) and
///      containing a strong band (&gt;= 15% density) are treated as art pages. This cleanly
///      rejects text-only pages and bold headings.
///   3. Finds art runs on gated pages allowing small gaps so that internal whitespace in
///      a drawing does not split one illustration into several crops.
///   4. Grows each crop's bounds to the surrounding light-ink extent (down to ~3% row
///      density) so light pen strokes at the art's edges are not cut off.
///   5. Merges overlapping crops that belong to the same illustration.
///
/// Text-only pages yield no regions.
/// </summary>
public class ArtRegionDetector
{
    /// <summary>Height in pixels of one analysis row band.</summary>
    public const int BandHeight = 8;

    /// <summary>Row-band density above this counts as "content".</summary>
    public const double DenseBandThreshold = 0.08;

    /// <summary>A run must contain a band at least this dense to count as art, not a heading.</summary>
    public const double StrongBandThreshold = 0.15;

    /// <summary>A gap-free run must span at least this many source pixels to gate a page as art.</summary>
    public const int MinArtRunPx = 96;

    /// <summary>When locating art regions inside a gated page, allow gaps up to this many bands.</summary>
    public const int MaxGapBands = 1;

    /// <summary>Row-band density used when growing a crop to capture light art edges.</summary>
    public const double EdgeGrowDensity = 0.03;

    /// <summary>While growing, allow up to this many consecutive below-edge bands before stopping.</summary>
    public const int EdgeGrowMaxGap = 3;

    /// <summary>Column density (percent) used when growing the horizontal extent.</summary>
    public const int EdgeGrowColumnPercent = 1;

    /// <summary>Column density above this inside a run counts toward its horizontal extent.</summary>
    public const double ColumnDensityThreshold = 0.05;

    /// <summary>Extra margin in source pixels added around each cropped region.</summary>
    public const int MarginPx = 6;

    /// <summary>Returns crop rectangles in source-image coordinates, empty for text-only pages.</summary>
    public IReadOnlyList<Rectangle> Detect(Bitmap source)
    {
        if (source.Width <= 0 || source.Height <= 0)
            return Array.Empty<Rectangle>();

        var dark = ToDarkMask(source);
        var densities = RowBandDensities(dark, source.Width, BandHeight);

        // Gate: page must contain a tall, gap-free dense run with a strong band.
        if (!HasArtRun(densities, maxGap: 0))
            return Array.Empty<Rectangle>();

        // Locate art regions allowing small gaps (internal whitespace within a drawing).
        var regions = new List<Rectangle>();
        foreach (var (startBand, endBand) in FindRuns(densities, MaxGapBands))
        {
            int runPx = (endBand - startBand + 1) * BandHeight;
            if (runPx < MinArtRunPx) continue;

            double strong = 0;
            for (int b = startBand; b <= endBand; b++)
                strong = Math.Max(strong, densities[b]);
            if (strong < StrongBandThreshold) continue;

            int topPx = startBand * BandHeight;
            int bottomPx = Math.Min((endBand + 1) * BandHeight, source.Height);

            int colMin = int.MaxValue, colMax = -1;
            for (int x = 0; x < source.Width; x++)
            {
                int darkCount = 0, total = 0;
                for (int y = topPx; y < bottomPx; y += 2)
                {
                    total++;
                    if (dark[y, x]) darkCount++;
                }
                if (total > 0 && (double)darkCount / total >= ColumnDensityThreshold)
                {
                    colMin = Math.Min(colMin, x);
                    colMax = Math.Max(colMax, x);
                }
            }
            if (colMax < 0) continue;

            var grown = GrowToContent(
                dark, source.Width, source.Height,
                new Rectangle(colMin, topPx, colMax - colMin + 1, bottomPx - topPx),
                EdgeGrowDensity, EdgeGrowMaxGap, EdgeGrowColumnPercent, MarginPx);
            regions.Add(grown);
        }

        return MergeOverlapping(regions);
    }

    private static bool HasArtRun(double[] densities, int maxGap)
    {
        foreach (var (start, end) in FindRuns(densities, maxGap))
        {
            int runPx = (end - start + 1) * BandHeight;
            if (runPx < MinArtRunPx) continue;
            double strong = 0;
            for (int b = start; b <= end; b++)
                strong = Math.Max(strong, densities[b]);
            if (strong >= StrongBandThreshold)
                return true;
        }
        return false;
    }

    private static List<(int StartBand, int EndBand)> FindRuns(double[] densities, int maxGap)
    {
        var runs = new List<(int, int)>();
        int i = 0;
        int bandCount = densities.Length;
        while (i < bandCount)
        {
            if (densities[i] < DenseBandThreshold) { i++; continue; }
            int start = i, end = i, gap = 0;
            while (end + 1 < bandCount)
            {
                if (densities[end + 1] >= DenseBandThreshold) { end++; gap = 0; }
                else if (gap < maxGap) { end++; gap++; }
                else break;
            }
            i = end + 1;
            runs.Add((start, end));
        }
        return runs;
    }

    private static double[] RowBandDensities(bool[,] dark, int width, int bandHeight)
    {
        int height = dark.GetLength(0);
        int bandCount = height / bandHeight;
        var densities = new double[bandCount];
        for (int b = 0; b < bandCount; b++)
        {
            int y0 = b * bandHeight;
            int y1 = Math.Min(y0 + bandHeight, height);
            int darkCount = 0, total = 0;
            for (int y = y0; y < y1; y += 2)
            {
                for (int x = 0; x < width; x += 2)
                {
                    total++;
                    if (dark[y, x]) darkCount++;
                }
            }
            densities[b] = total > 0 ? (double)darkCount / total : 0;
        }
        return densities;
    }

    private static Rectangle GrowToContent(bool[,] dark, int width, int height, Rectangle box, double lowTh, int lowGap, int colPercent, int margin)
    {
        int topPx = box.Y, bottomPx = box.Y + box.Height;
        int colMin = Math.Max(0, box.X), colMax = Math.Min(width - 1, box.X + box.Width);

        double RowD(int y)
        {
            int d = 0, t = 0;
            for (int x = colMin; x <= colMax; x += 2)
            {
                t++;
                if (dark[y, x]) d++;
            }
            return t > 0 ? (double)d / t : 0;
        }

        int ngap = 0;
        while (topPx - BandHeight >= 0)
        {
            double d = RowD(topPx - BandHeight);
            if (d >= lowTh) { topPx -= BandHeight; ngap = 0; }
            else { topPx -= BandHeight; ngap++; if (ngap > lowGap) break; }
        }
        ngap = 0;
        while (bottomPx + BandHeight <= height)
        {
            double d = RowD(bottomPx);
            if (d >= lowTh) { bottomPx += BandHeight; ngap = 0; }
            else { bottomPx += BandHeight; ngap++; if (ngap > lowGap) break; }
        }

        int ColD(int x)
        {
            int d = 0, t = 0;
            for (int y = topPx; y < bottomPx; y += 2)
            {
                t++;
                if (dark[y, x]) d++;
            }
            return t > 0 ? (int)(d * 100.0 / t) : 0;
        }

        int gleft = colMin; ngap = 0;
        while (gleft - 2 >= 0)
        {
            int f = ColD(gleft - 2);
            if (f >= colPercent) { gleft -= 2; ngap = 0; }
            else { gleft -= 2; ngap++; if (ngap > 3) break; }
        }
        int gright = colMax; ngap = 0;
        while (gright + 2 < width)
        {
            int f = ColD(gright + 2);
            if (f >= colPercent) { gright += 2; ngap = 0; }
            else { gright += 2; ngap++; if (ngap > 3) break; }
        }

        int sx = Math.Max(0, gleft - margin);
        int sy = Math.Max(0, topPx - margin);
        int sw = Math.Min(width - sx, gright - gleft + 1 + 2 * margin);
        int sh = Math.Min(height - sy, bottomPx - topPx + 2 * margin);
        return new Rectangle(sx, sy, sw, sh);
    }

    private static List<Rectangle> MergeOverlapping(List<Rectangle> rects)
    {
        var merged = new List<Rectangle>();
        foreach (var r in rects.OrderByDescending(r => r.Width * r.Height))
        {
            bool absorbed = false;
            foreach (var m in merged)
            {
                int ix = Math.Max(0, Math.Min(r.Right, m.Right) - Math.Max(r.Left, m.Left));
                int iy = Math.Max(0, Math.Min(r.Bottom, m.Bottom) - Math.Max(r.Top, m.Top));
                if ((double)(ix * iy) / (r.Width * r.Height) > 0.5)
                {
                    absorbed = true;
                    break;
                }
            }
            if (!absorbed) merged.Add(r);
        }
        return merged;
    }

    private static bool[,] ToDarkMask(Bitmap source)
    {
        int w = source.Width, h = source.Height;
        var mask = new bool[h, w];
        var rect = new Rectangle(0, 0, w, h);
        var data = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            int stride = data.Stride;
            int rowBytes = Math.Min(stride, w * 3);
            byte[] buffer = new byte[stride * h];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            for (int y = 0; y < h; y++)
            {
                int row = y * stride;
                for (int x = 0; x < w; x++)
                {
                    int i = row + x * 3;
                    byte b = buffer[i], g = buffer[i + 1], r = buffer[i + 2];
                    double lum = r * 0.299 + g * 0.587 + b * 0.114;
                    mask[y, x] = lum < 100;
                }
            }
        }
        finally
        {
            source.UnlockBits(data);
        }
        return mask;
    }
}
