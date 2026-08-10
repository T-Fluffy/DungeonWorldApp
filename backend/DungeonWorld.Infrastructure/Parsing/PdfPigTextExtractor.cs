using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DungeonWorld.Infrastructure.Parsing;

public interface IPdfTextExtractor
{
    /// <summary>
    /// Extracts paragraphs ("blocks") from a PDF in reading order, one block per
    /// logical page side. Double-page (2-up) scans are split into columns.
    /// </summary>
    List<TextBlock> Extract(string filePath);
}

/// <summary>
/// Layout-agnostic raw text extraction built on PdfPig. It recovers reading order
/// and enough layout metadata for section-header detection, without assuming any
/// particular book format.
/// </summary>
public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    // A landscape page with this width/height ratio is treated as a 2-up scan.
    private const double DoublePageAspectThreshold = 1.15;

    public List<TextBlock> Extract(string filePath)
    {
        var result = new List<TextBlock>();

        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            var isDouble = page.Width > page.Height * DoublePageAspectThreshold;

            if (isDouble)
            {
                var midX = page.Width / 2;
                ExtractColumn(page, 0, midX, page.Number * 2 - 1, result);
                ExtractColumn(page, midX, page.Width, page.Number * 2, result);
            }
            else
            {
                ExtractColumn(page, 0, page.Width, page.Number, result);
            }
        }

        return result;
    }

    private static void ExtractColumn(Page page, double minX, double maxX, int logicalPage, List<TextBlock> result)
    {
        var words = page.GetWords()
            .Where(w => w.BoundingBox.Left >= minX && w.BoundingBox.Right <= maxX)
            .ToList();

        var lines = GroupIntoLines(words);
        foreach (var para in GroupIntoParagraphs(lines, page.Height))
        {
            result.Add(new TextBlock
            {
                LogicalPage = logicalPage,
                PhysicalPage = page.Number,
                Text = para.Text,
                TopFraction = para.TopFraction,
                FontSize = para.FontSize,
                IsBold = para.IsBold,
            });
        }
    }

    /// <summary>Groups words into text lines using vertical clustering.</summary>
    private static List<TextLine> GroupIntoLines(IReadOnlyList<Word> words)
    {
        var lines = new List<TextLine>();
        var ordered = words
            .OrderByDescending(w => w.BoundingBox.Bottom)
            .ThenBy(w => w.BoundingBox.Left)
            .ToList();

        foreach (var word in ordered)
        {
            var box = word.BoundingBox;
            TextLine? best = null;
            double bestDist = double.MaxValue;

            foreach (var line in lines)
            {
                if (line.Top < box.Bottom || line.Bottom > box.Top) continue;
                var dist = Math.Abs(line.Base - box.Bottom);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = line;
                }
            }

            if (best == null)
            {
                lines.Add(new TextLine(box.Bottom, box.Top, box.Bottom, new List<Word> { word }));
            }
            else
            {
                best.Base = Math.Max(best.Base, box.Bottom);
                best.Top = Math.Max(best.Top, box.Top);
                best.Bottom = Math.Min(best.Bottom, box.Bottom);
                best.Words.Add(word);
            }
        }

        foreach (var line in lines)
            line.Words.Sort((a, b) => a.BoundingBox.Left.CompareTo(b.BoundingBox.Left));

        return lines.OrderByDescending(l => l.Base).ToList();
    }

    /// <summary>Groups lines into paragraphs, merging hyphen-broken words.</summary>
    private static List<Paragraph> GroupIntoParagraphs(List<TextLine> lines, double pageHeight)
    {
        var result = new List<Paragraph>();
        if (lines.Count == 0) return result;

        double avgHeight = lines.Average(l => l.Top - l.Bottom);

        var sb = new StringBuilder();
        double firstTop = lines[0].Top;
        double fontTotal = 0;
        int fontCount = 0;
        bool bold = false;

        void Flush()
        {
            var text = sb.ToString().Trim();
            if (text.Length == 0) return;

            var topFraction = pageHeight > 0 ? Math.Clamp(1 - (firstTop / pageHeight), 0, 1) : 0;
            result.Add(new Paragraph(text, topFraction, fontCount > 0 ? fontTotal / fontCount : 0, bold));

            sb.Clear();
            fontTotal = 0;
            fontCount = 0;
            bold = false;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineText = string.Join(" ", line.Words.Select(w => w.Text));

            foreach (var w in line.Words)
            {
                fontTotal += w.BoundingBox.Height;
                fontCount++;
            }
            if (line.Words.Any(w => w.FontName?.Contains("Bold", StringComparison.OrdinalIgnoreCase) == true))
                bold = true;

            if (i > 0)
            {
                var prev = lines[i - 1];
                var gap = prev.Bottom - line.Top;

                // A hyphen-broken word continues the current paragraph without a break.
                if (sb.Length > 0 && sb[^1] == '-')
                {
                    sb.Length--;
                    sb.Append(lineText);
                    continue;
                }

                if (gap > avgHeight * 1.4)
                {
                    Flush();
                    firstTop = line.Top;
                    sb.Append(lineText);
                }
                else
                {
                    sb.Append('\n').Append(lineText);
                }
                continue;
            }

            firstTop = line.Top;
            sb.Append(lineText);
        }

        Flush();
        return result;
    }

    private sealed class Paragraph
    {
        public Paragraph(string text, double topFraction, double fontSize, bool isBold)
        {
            Text = text;
            TopFraction = topFraction;
            FontSize = fontSize;
            IsBold = isBold;
        }

        public string Text { get; }
        public double TopFraction { get; }
        public double FontSize { get; }
        public bool IsBold { get; }
    }

    private sealed class TextLine
    {
        public TextLine(double @base, double top, double bottom, List<Word> words)
        {
            Base = @base;
            Top = top;
            Bottom = bottom;
            Words = words;
        }

        public double Base { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
        public List<Word> Words { get; set; }
    }
}
