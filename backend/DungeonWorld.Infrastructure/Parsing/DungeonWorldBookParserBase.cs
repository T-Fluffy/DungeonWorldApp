using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Abstract rule-based parser shared by every book. The full pipeline is generic
/// (extract blocks, detect section headers, extract choices, export images, fill
/// gaps, persist), and each book can override the small hooks that differ per scan:
/// header detection, footer band, choice phrasing, section count, etc.
/// </summary>
public abstract class DungeonWorldBookParserBase : IBookParser
{
    protected static readonly Regex TurnToRegex = new(
        @"(?i)turn\s+to\s+(?:the\s+)?(\d{1,4})",
        RegexOptions.Compiled);

    protected static readonly Regex HeaderRegex = new(
        @"^\W*(\d{1,4})\W*$",
        RegexOptions.Compiled);

    private readonly IPdfTextExtractor _textExtractor;

    protected readonly FileStorageOptions _storageOptions;

    protected DungeonWorldBookParserBase(
        IPdfTextExtractor textExtractor,
        IOptions<FileStorageOptions> storageOptions)
    {
        _textExtractor = textExtractor;
        _storageOptions = storageOptions.Value;
    }

    // ---- Per-book hooks -----------------------------------------------------

    /// <summary>Highest section number in this book (Fighting Fantasy defaults to 400).</summary>
    protected virtual int MaxSectionNumber => 400;

    /// <summary>Fraction of page top and bottom treated as headers/footers and dropped.</summary>
    protected virtual double HeaderFooterBand => 0.05;

    /// <summary>
    /// Visual test for a section header: the block is a standalone number that is
    /// bold or noticeably larger than the body font. Sequential acceptance is
    /// handled separately by the pipeline.
    /// </summary>
    protected virtual bool MatchSectionHeader(TextBlock block, double averageFontSize)
    {
        if (block.FontSize <= averageFontSize * 1.25 && !block.IsBold) return false;
        return HeaderRegex.IsMatch(block.Text.Trim());
    }

    /// <summary>Extracts navigation choices from a section's body text.</summary>
    protected virtual List<Choice> BuildChoices(string text)
    {
        bool hasDiceRoll = text.Contains("roll", StringComparison.OrdinalIgnoreCase) ||
                           text.Contains("dice", StringComparison.OrdinalIgnoreCase);

        return TurnToRegex.Matches(text)
            .Select(m => new Choice
            {
                TargetSectionNumber = int.Parse(m.Groups[1].Value),
                Description = $"Turn to {m.Groups[1].Value}",
                IsDiceRoll = hasDiceRoll,
            })
            .ToList();
    }

    /// <summary>Optional per-book introduction/rule text placed before section 1.</summary>
    protected virtual string BuildIntroduction(IReadOnlyList<TextBlock> blocks) => "";

    // ---- IBookParser --------------------------------------------------------

    public abstract string ParserId { get; }
    public abstract bool CanHandle(string filePath, string bookTitle);

    public virtual async Task<Book> ParseAsync(string filePath)
    {
        string fullPdfPath = ResolvePath(filePath);
        string bookTitle = Path.GetFileNameWithoutExtension(fullPdfPath);
        string slug = CreateSlug(bookTitle);
        string imageFolder = EnsureImageFolder(slug);

        var book = new Book
        {
            Title = bookTitle,
            Author = "Steve Jackson and Ian Livingstone",
        };

        var blocks = _textExtractor.Extract(fullPdfPath);
        var body = blocks
            .Where(b => !IsHeaderOrFooter(b))
            .OrderBy(b => b.LogicalPage)
            .ThenBy(b => b.TopFraction)
            .ToList();

        double averageFontSize = body.Count > 0
            ? body.Average(b => b.FontSize > 0 ? b.FontSize : 10.0)
            : 10.0;

        ExtractImages(fullPdfPath, imageFolder, slug, book);
        book.Introduction = BuildIntroduction(body);

        var buffer = new StringBuilder();
        int currentNumber = 0;
        int currentPhysicalPage = 1;
        int expectedNext = 1;

        void FlushSection()
        {
            if (currentNumber <= 0 || currentNumber > MaxSectionNumber) return;

            string content = buffer.ToString().Trim();
            book.Sections.Add(new Section
            {
                SectionNumber = currentNumber,
                Content = content,
                ImagePath = $"/assets/game-art/{slug}/p{currentPhysicalPage}_i0.png",
                Choices = BuildChoices(content),
                HasCombat = content.Contains("SKILL") && content.Contains("STAMINA"),
            });
        }

        foreach (var block in body)
        {
            int? headerNumber = TryParseSectionNumber(block.Text);

            if (headerNumber.HasValue)
            {
                bool sequential = headerNumber.Value >= expectedNext - 1 &&
                                  headerNumber.Value <= expectedNext + 5;
                bool visual = MatchSectionHeader(block, averageFontSize);

                if (sequential || visual)
                {
                    FlushSection();
                    currentNumber = headerNumber.Value;
                    expectedNext = headerNumber.Value + 1;
                    currentPhysicalPage = block.PhysicalPage;
                    buffer.Clear();
                    continue;
                }
            }

            if (currentNumber > 0)
            {
                buffer.AppendLine(block.Text).AppendLine();
            }
        }

        FlushSection();
        FillGaps(book);
        await PersistBookAsync(book, bookTitle);

        return book;
    }

    // ---- Shared helpers -----------------------------------------------------

    protected int? TryParseSectionNumber(string text)
    {
        var match = HeaderRegex.Match(text.Trim());
        if (!match.Success) return null;
        if (!int.TryParse(match.Groups[1].Value, out var number)) return null;
        if (number <= 0 || number > MaxSectionNumber) return null;
        return number;
    }

    private bool IsHeaderOrFooter(TextBlock block) =>
        block.TopFraction < HeaderFooterBand || block.TopFraction > 1 - HeaderFooterBand;

    private void FillGaps(Book book)
    {
        var existing = book.Sections.Select(s => s.SectionNumber).ToHashSet();
        for (int i = 1; i <= MaxSectionNumber; i++)
        {
            if (existing.Contains(i)) continue;
            book.Sections.Add(new Section
            {
                SectionNumber = i,
                Content = "[Text missing or unreadable in PDF]",
                Choices = new List<Choice>(),
            });
        }
        book.Sections = book.Sections.OrderBy(s => s.SectionNumber).ToList();
    }

    private void ExtractImages(string pdfPath, string folder, string slug, Book book)
    {
        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
        {
            var images = page.GetImages().ToList();
            for (int i = 0; i < images.Count; i++)
            {
                try
                {
                    var img = images[i];
                    if (img.WidthInSamples < 50 || img.HeightInSamples < 50) continue;

                    var imgName = $"p{page.Number}_i{i}.png";
                    File.WriteAllBytes(Path.Combine(folder, imgName), img.RawBytes.ToArray());

                    // Large illustration early in the book is usually the map.
                    if (book.MapPath == null && page.Number < 20 && img.WidthInSamples > 400)
                        book.MapPath = $"/assets/game-art/{slug}/{imgName}";
                }
                catch
                {
                    // Skip corrupted images.
                }
            }
        }
    }

    private async Task PersistBookAsync(Book book, string title)
    {
        string outputDir = Path.Combine(
            Path.GetFullPath(_storageOptions.PdfUploadPath),
            "ProcessedBooks");

        Directory.CreateDirectory(outputDir);
        string jsonPath = GetUniqueFilePath(Path.Combine(outputDir, $"{title}.json"));

        await File.WriteAllTextAsync(jsonPath,
            JsonSerializer.Serialize(book, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>
    /// Returns a path that does not already exist, appending " (n)" before the
    /// extension when needed, so re-ingestion never overwrites earlier output.
    /// </summary>
    protected static string GetUniqueFilePath(string desiredPath)
    {
        if (!File.Exists(desiredPath)) return desiredPath;

        string dir = Path.GetDirectoryName(desiredPath)!;
        string name = Path.GetFileNameWithoutExtension(desiredPath);
        string ext = Path.GetExtension(desiredPath);

        for (int i = 1; ; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
    }

    private string ResolvePath(string fileName) =>
        Path.IsPathRooted(fileName)
            ? fileName
            : Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath),
                fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                    ? fileName
                    : $"{fileName}.pdf");

    private string CreateSlug(string title) =>
        title.Replace(" ", "_").ToLower();

    private string EnsureImageFolder(string slug)
    {
        var path = Path.Combine(Path.GetFullPath(_storageOptions.ImageOutputPath), slug);
        Directory.CreateDirectory(path);
        return path;
    }
}
