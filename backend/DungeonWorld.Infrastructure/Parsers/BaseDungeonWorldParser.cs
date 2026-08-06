using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DungeonWorld.Infrastructure.Parsers;

/// <summary>
/// Abstract base class with common Fighting Fantasy parsing logic.
/// Derived classes implement layout-specific extraction via ExtractLinesFromPage.
/// </summary>
public abstract class BaseDungeonWorldParser : IBookParser
{
    protected readonly FileStorageOptions _storageOptions;
    
    protected static readonly Regex TurnToRegex = new(
        @"(?i)turn\s+to\s+(?:the\s+)?(\d+)", 
        RegexOptions.Compiled);
    
    protected static readonly Regex HeaderRegex = new(
        @"^\W*(\d{1,4})\W*$", 
        RegexOptions.Compiled);

    protected BaseDungeonWorldParser(IOptions<FileStorageOptions> storageOptions)
    {
        _storageOptions = storageOptions.Value;
    }

    public abstract string ParserId { get; }
    public abstract bool CanHandle(string filePath, string bookTitle);
    
    // Layout-specific extraction implemented by derived classes
    protected abstract List<LineInfo> ExtractLinesFromPage(Page page);

    public virtual async Task<Book> ParseAsync(string filePath)
    {
        string fullPdfPath = ResolvePath(filePath);
        string bookTitle = Path.GetFileNameWithoutExtension(filePath);
        string bookSlug = CreateSlug(bookTitle);
        string imageFolder = EnsureImageFolder(bookSlug);

        var book = new Book 
        { 
            Title = bookTitle, 
            Author = "Steve Jackson and Ian Livingstone" 
        };

        var allLines = new List<LineInfo>();

        Console.WriteLine($"[{ParserId}] Parsing: {bookTitle}");

        using var document = PdfDocument.Open(fullPdfPath);
        
        foreach (var page in document.GetPages())
        {
            // Extract images (common to all layouts)
            ExtractImages(page, imageFolder, bookSlug, book);
            
            // Layout-specific text extraction
            var pageLines = ExtractLinesFromPage(page);
            
            // Adjust page numbers for double-page layouts
            // (each physical page contains 2 logical pages)
            if (this is DoublePageParser)
            {
                var midpoint = page.Width / 2;
                foreach (var line in pageLines)
                {
                    // Determine which column this line belongs to
                    bool isLeftColumn = line.OriginalX < midpoint;
                    line.Page = (page.Number * 2) - (isLeftColumn ? 1 : 0);
                }
            }
            else
            {
                foreach (var line in pageLines)
                    line.Page = page.Number;
            }
            
            allLines.AddRange(pageLines);
        }

        // Common assembly logic
        AssembleSections(book, allLines, bookSlug);
        FillGaps(book);
        await PersistBookAsync(book, bookTitle);

        return book;
    }

    protected virtual void AssembleSections(Book book, List<LineInfo> allLines, string bookSlug)
    {
        int expectedNext = 1;
        int currentSectionNum = 0;
        int currentSectionPage = 0;
        StringBuilder contentBuffer = new();

        // Calculate baseline for header detection
        double avgFontSize = allLines.Any() ? allLines.Average(l => l.FontSize) : 10.0;

        foreach (var line in allLines.OrderBy(l => l.Page).ThenByDescending(l => l.Y))
        {
            string text = line.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            bool isVisualHeader = line.IsBold || line.FontSize > avgFontSize + 1.5;
            var match = HeaderRegex.Match(text);

            if (match.Success && int.TryParse(match.Groups[1].Value, out int sectionNum))
            {
                // Accept if visual header OR sequential match
                if (isVisualHeader || (sectionNum >= expectedNext && sectionNum <= expectedNext + 5))
                {
                    FlushCurrentSection(book, currentSectionNum, contentBuffer, bookSlug, currentSectionPage);
                    
                    currentSectionNum = sectionNum;
                    expectedNext = sectionNum + 1;
                    currentSectionPage = line.Page;
                    contentBuffer.Clear();
                    continue;
                }
            }

            if (currentSectionNum > 0)
            {
                contentBuffer.AppendLine(text);
                
                // Victory detection
                if (currentSectionNum >= 400 && 
                    (text.Contains("You have won", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("adventure ends", StringComparison.OrdinalIgnoreCase)))
                {
                    break;
                }
            }
        }

        FlushCurrentSection(book, currentSectionNum, contentBuffer, bookSlug, currentSectionPage);
    }

    protected virtual List<LineInfo> ExtractLinesFromArea(Page page, double minX, double maxX)
    {
        return page.GetWords()
            .Where(w => w.BoundingBox.Left >= minX && w.BoundingBox.Right <= maxX)
            .Where(w => w.BoundingBox.Top < page.Height * 0.95 && w.BoundingBox.Bottom > page.Height * 0.05)
            .GroupBy(w => Math.Round(w.BoundingBox.Bottom / 3.0) * 3.0)
            .OrderByDescending(g => g.Key)
            .Select(g => new LineInfo
            {
                Text = string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)),
                Y = g.Key,
                OriginalX = g.Average(w => w.BoundingBox.Left),
                FontSize = g.Average(w => w.BoundingBox.Height),
                IsBold = g.Any(w => w.FontName?.Contains("Bold", StringComparison.OrdinalIgnoreCase) == true),
                Page = page.Number
            })
            .ToList();
    }

    protected virtual void ExtractImages(Page page, string folder, string slug, Book book)
    {
        var images = page.GetImages().ToList();
        for (int i = 0; i < images.Count; i++)
        {
            try
            {
                var img = images[i];
                if (img.WidthInSamples < 50 || img.HeightInSamples < 50) continue;

                string imgName = $"p{page.Number}_i{i}.png";
                string fullPath = Path.Combine(folder, imgName);
                File.WriteAllBytes(fullPath, img.RawBytes.ToArray());

                // Map detection: large image in first 20 pages
                if (book.MapPath == null && page.Number < 20 && img.WidthInSamples > 400)
                    book.MapPath = $"/assets/game-art/{slug}/{imgName}";
            }
            catch { /* Skip corrupted images */ }
        }
    }

    protected virtual void FlushCurrentSection(Book book, int number, StringBuilder content, string slug, int page)
    {
        if (number <= 0 || number > 400) return;
        
        var section = new Section
        {
            SectionNumber = number,
            Content = content.ToString().Trim(),
            ImagePath = $"/assets/game-art/{slug}/p{page}_i0.png",
            Choices = ExtractChoices(content.ToString()),
            HasCombat = content.ToString().Contains("SKILL") && content.ToString().Contains("STAMINA")
        };
        
        book.Sections.Add(section);
    }

    protected virtual List<Choice> ExtractChoices(string text)
    {
        var matches = TurnToRegex.Matches(text);
        bool hasDiceRoll = text.Contains("roll", StringComparison.OrdinalIgnoreCase) || 
                          text.Contains("dice", StringComparison.OrdinalIgnoreCase);

        return matches.Select(m => new Choice
        {
            TargetSectionNumber = int.Parse(m.Groups[1].Value),
            Description = $"Turn to {m.Groups[1].Value}",
            IsDiceRoll = hasDiceRoll
        }).ToList();
    }

    protected virtual void FillGaps(Book book)
    {
        var existing = book.Sections.Select(s => s.SectionNumber).ToHashSet();
        
        for (int i = 1; i <= 400; i++)
        {
            if (!existing.Contains(i))
            {
                book.Sections.Add(new Section
                {
                    SectionNumber = i,
                    Content = "[Text missing or unreadable in PDF]",
                    Choices = new List<Choice>()
                });
            }
        }
        
        book.Sections = book.Sections.OrderBy(s => s.SectionNumber).ToList();
    }

    protected virtual async Task PersistBookAsync(Book book, string title)
    {
        string outputDir = Path.Combine(
            Path.GetFullPath(_storageOptions.PdfUploadPath), 
            "ProcessedBooks");
        
        Directory.CreateDirectory(outputDir);
        
        string jsonPath = Path.Combine(outputDir, $"{title}.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(book, options));
    }

    // Helper methods
    protected string ResolvePath(string fileName) => 
        Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath), 
            fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? fileName : $"{fileName}.pdf");

    protected string CreateSlug(string title) => 
        title.Replace(" ", "_").ToLower();

    protected string EnsureImageFolder(string slug)
    {
        string path = Path.Combine(Path.GetFullPath(_storageOptions.ImageOutputPath), slug);
        Directory.CreateDirectory(path);
        return path;
    }

    // LineInfo with additional metadata for layout handling
    protected class LineInfo
    {
        public string Text { get; set; } = "";
        public int Page { get; set; }
        public double Y { get; set; }
        public double OriginalX { get; set; } // For column detection
        public double FontSize { get; set; }
        public bool IsBold { get; set; }
    }
}