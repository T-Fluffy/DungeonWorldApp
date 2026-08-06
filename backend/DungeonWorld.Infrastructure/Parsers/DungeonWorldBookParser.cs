using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DungeonWorld.Tests")]
namespace DungeonWorld.Infrastructure.Parsers;

public class DungeonWorldBookParser : IBookParser
{
    private readonly FileStorageOptions _storageOptions;
    private static readonly Regex TurnToRegex = new Regex(@"(?i)turn\s+to\s+(\d+)", RegexOptions.Compiled);

    public string ParserId => "DungeonWorldBookParser";

    public DungeonWorldBookParser(IOptions<FileStorageOptions> storageOptions)
    {
        _storageOptions = storageOptions.Value;
    }

    public async Task<Book> ParseAsync(string fileName)
    {
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) fileName += ".pdf";
        string fullPdfPath = Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath), fileName);
        string bookTitle = Path.GetFileNameWithoutExtension(fileName);
        string bookSlug = bookTitle.Replace(" ", "_").ToLower();
        string imageFolder = Path.Combine(Path.GetFullPath(_storageOptions.ImageOutputPath), bookSlug);
        
        if (!Directory.Exists(imageFolder)) Directory.CreateDirectory(imageFolder);

        var book = new Book { Title = bookTitle, Author = "Steve Jackson and Ian Livingstone" };
        var allLines = new List<LineInfo>();

        using var document = PdfDocument.Open(fullPdfPath);

        // --- STEP 1: EXTRACT LINES & IMAGES ---
        foreach (var page in document.GetPages())
        {
            // Extract and save images found on the page
            var images = page.GetImages().ToList();
            for (int i = 0; i < images.Count; i++)
            {
                try {
                    string imgName = $"p{page.Number}_i{i}.png";
                    string fullPath = Path.Combine(imageFolder, imgName);
                    File.WriteAllBytes(fullPath, images[i].RawBytes.ToArray());

                    // Heuristic to find Map/Adventure Sheet early in the book
                    if (book.MapPath == null && page.Number < 10 && images[i].WidthInSamples > 400)
                        book.MapPath = $"/assets/game-art/{bookSlug}/{imgName}";
                } catch { /* Skip corrupted image streams */ }
            }

            var lines = page.GetWords()
                .Where(w => w.BoundingBox.Top < page.Height * 0.95 && w.BoundingBox.Bottom > 40)
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom / 3) * 3)
                .OrderByDescending(g => g.Key)
                .Select(g => new LineInfo {
                    Text = string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)),
                    Page = page.Number,
                    IsCentered = IsCentered(g, page.Width)
                });

            allLines.AddRange(lines);
        }

        // --- STEP 2: PARSING STATE MACHINE ---
        bool isGameStarted = false;
        bool isBookFinished = false; 
        int currentSectionNum = 0;
        int sectionStartPage = 0;
        StringBuilder contentBuffer = new StringBuilder();
        StringBuilder introBuffer = new StringBuilder();

        for (int i = 0; i < allLines.Count; i++)
        {
            if (isBookFinished) break; 

            var line = allLines[i];
            string cleanText = line.Text.Trim();
            if (string.IsNullOrWhiteSpace(cleanText)) continue;

            // A. Handle Introduction
            if (!isGameStarted)
            {
                if (cleanText.Contains("TURN OVER", StringComparison.OrdinalIgnoreCase))
                {
                    isGameStarted = true;
                    book.Introduction = introBuffer.ToString().Trim();
                    continue;
                }
                introBuffer.AppendLine(cleanText);
                continue;
            }

            // B. Identify Section Headers
            int expectedNext = currentSectionNum + 1;
            int foundNumber = -1;
            bool isHeader = false;

            if (int.TryParse(cleanText.TrimEnd('.'), out int val))
            {
                if (line.IsCentered || val == expectedNext) foundNumber = val;
            }
            else if (cleanText.StartsWith($"{expectedNext} ") || cleanText.StartsWith($"{expectedNext}."))
            {
                foundNumber = expectedNext;
            }

            // Section 50 Patch for "Seas of Blood"
            if (currentSectionNum == 49 && foundNumber == -1)
            {
                if (cleanText.Contains("'Come now,'") || cleanText.Contains("lost the wager"))
                    foundNumber = 50;
            }

            if (foundNumber > 0 && foundNumber <= 400)
            {
                // Gap Jump Protection: Only accept headers within a reasonable range
                if (foundNumber == expectedNext || (foundNumber > expectedNext && foundNumber < expectedNext + 5))
                    isHeader = true;
            }

            if (isHeader)
            {
                if (currentSectionNum > 0) 
                    SaveSection(book, currentSectionNum, contentBuffer.ToString(), sectionStartPage, bookSlug);

                currentSectionNum = foundNumber;
                sectionStartPage = line.Page;
                contentBuffer.Clear();

                string remainder = RemoveHeaderNumber(cleanText, foundNumber);
                if (!string.IsNullOrWhiteSpace(remainder)) contentBuffer.AppendLine(remainder);
            }
            else
            {
                contentBuffer.AppendLine(cleanText);
                
                // C. Victory Terminator
                if (currentSectionNum == 400 && cleanText.Contains("You have won."))
                    isBookFinished = true;
            }
        }

        // --- STEP 3: THE FINAL FLUSH ---
        // Save the very last section (400) which is still in the buffer
        if (currentSectionNum > 0)
        {
            SaveSection(book, currentSectionNum, contentBuffer.ToString(), sectionStartPage, bookSlug);
        }

        // Fill gaps for any sections that were completely unreadable
        for (int i = 1; i <= 400; i++)
        {
            if (!book.Sections.Any(s => s.SectionNumber == i))
            {
                book.Sections.Add(new Section { 
                    SectionNumber = i, 
                    Content = "[Text missing or unreadable in PDF]",
                    Choices = new List<Choice>()
                });
            }
        }
        book.Sections = book.Sections.OrderBy(s => s.SectionNumber).ToList();

        // --- STEP 4: PERSIST TO JSON ---
        string outputDir = Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath), "ProcessedBooks");
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        
        string jsonPath = Path.Combine(outputDir, $"{bookTitle}.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(book, new JsonSerializerOptions { WriteIndented = true }));

        return book;
    }

    private bool IsCentered(IEnumerable<Word> words, double pageWidth)
    {
        double left = words.Min(w => w.BoundingBox.Left);
        double right = words.Max(w => w.BoundingBox.Right);
        return Math.Abs(((left + right) / 2) - (pageWidth / 2)) < 50;
    }

    internal string RemoveHeaderNumber(string text, int number)
    {
        string s = number.ToString();
        if (text == s || text == s + ".") return "";
        if (text.StartsWith(s)) return text.Substring(s.Length).TrimStart('.', ' ');
        return text;
    }

    private void SaveSection(Book book, int number, string content, int page, string slug)
    {
        if (number > 400 || number <= 0) return;
        
        var section = new Section {
            SectionNumber = number,
            Content = content.Trim(),
            ImagePath = $"/assets/game-art/{slug}/p{page}_i0.png"
        };
        section.Choices = ExtractChoices(section.Content);
        book.Sections.Add(section);
    }

    internal List<Choice> ExtractChoices(string text)
    {
        return TurnToRegex.Matches(text).Select(m => new Choice {
            TargetSectionNumber = int.Parse(m.Groups[1].Value),
            Description = $"Turn to {m.Groups[1].Value}",
            IsDiceRoll = text.ToLower().Contains("roll") || text.ToLower().Contains("dice")
        }).ToList();
    }

    public bool CanHandle(string filePath, string bookTitle)
    {
        // This parser carries the specialized patches (e.g. the "Section 50"
        // fix for Seas of Blood), so it only claims the books it was written for.
        return bookTitle?.Contains("seas of blood", StringComparison.OrdinalIgnoreCase) == true;
    }

    private class LineInfo { public string Text { get; set; } = ""; public int Page { get; set; } public bool IsCentered { get; set; } }
}