using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Parsing.Reconstruction;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Base parser for books rebuilt from a manual reconstruction manifest. Renders the PDF at the
/// book's verified dpi, OCRs per-page line transcripts, applies the book's embedded overrides
/// manifest ({n, page, side, line, [end]} body-start points), and assembles the sections plus
/// front-matter introduction. The manifest JSON ships as an embedded resource so the parser is
/// self-contained and reproducible; the intro pages and title/slug are the only per-book hooks.
/// </summary>
public abstract class ManifestDungeonWorldParser : IBookParser
{
    /// <summary>Book title used to match the parser (e.g. "Citadel of Chaos").</summary>
    protected abstract string TitleMatch { get; }

    /// <summary>Slug used for image URLs (e.g. "ff02_citadel_of_chaos").</summary>
    protected abstract string Slug { get; }

    /// <summary>Embedded manifest resource name, e.g. "DungeonWorld.Infrastructure.Parsing.Manifests.ff02.json".</summary>
    protected abstract string ManifestResourceName { get; }

    /// <summary>Pages whose transcripts form the book's Introduction (cover/title/BACKGROUND).</summary>
    protected abstract IReadOnlyList<int> IntroPages { get; }

    /// <summary>Render dpi at which the manifest line numbers were produced (all books verified at 300).</summary>
    protected virtual int Dpi => 300;

    /// <summary>Highest section number in this book.</summary>
    protected virtual int MaxSectionNumber => 400;

    /// <summary>Page the map artwork sits on (used for MapPath).</summary>
    protected virtual int MapPage => 1;

    /// <summary>
    /// Replaces OCR-garbled "turn to" variants in assembled section content. FF03's curated
    /// reconstruction normalized these ("Turnto261" -> "turn to 261"), so the parser must apply
    /// the same fix to reproduce it. Left off for the other manifest books to keep their curated
    /// output byte-for-byte (a shared normalization is a separate follow-up).
    /// </summary>
    protected virtual bool NormalizeTurnTos => false;

    private static readonly System.Text.RegularExpressions.Regex TurnToVariantRegex = new(
        @"(?i)\b(turnto|turmn\s*to|turm\s*to|tumn\s*to|tum\s*to|furn\s*to|fum\s*to|lurn\s*to|hurmn\s*to|turnin\s*to|on\s*to)\s*(\d{1,3})",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Optional per-book content correction applied after the manifest + normalization. FF03's
    /// curated output fixes section 193's erroneous reference "turn to 1710" to "turn to 171"
    /// (no such section), so the parser reproduces that correction here.
    /// </summary>
    protected virtual string PostProcessContent(string content) => content;

    /// <summary>
    /// Section-aware variant of <see cref="PostProcessContent"/>. Some books need corrections that
    /// depend on the section number (e.g. a turn-to number swallowed as the next section's header,
    /// or an entire section lost to line-noise filtering). Defaults to <see cref="PostProcessContent"/>.
    /// </summary>
    protected virtual string PostProcessSection(int sectionNumber, string content) =>
        PostProcessContent(content);

    /// <summary>Static JSON serializer options mirroring the block pipeline's persistence format.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly FileStorageOptions _storageOptions;

    protected ManifestDungeonWorldParser(IOptions<FileStorageOptions> storageOptions)
    {
        _storageOptions = storageOptions.Value;
    }

    public abstract string ParserId { get; }

    public virtual bool CanHandle(string filePath, string bookTitle) =>
        bookTitle.Contains(TitleMatch, StringComparison.OrdinalIgnoreCase);

    public async Task<Book> ParseAsync(string filePath)
    {
        string fullPdfPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath),
                filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? filePath : $"{filePath}.pdf");

        var entries = LoadManifest();
        int firstPage = Math.Min(IntroPages.DefaultIfEmpty(MapPage).Min(), entries.Min(e => e.Page));
        int lastPage = Math.Max(IntroPages.DefaultIfEmpty(MapPage).Max(), entries.Max(e => e.Page));

        var lines = ReconstructionService.OcrPdf(fullPdfPath, Dpi, Enumerable.Range(firstPage, lastPage - firstPage + 1).ToList());
        var sections = ReconstructionService.ApplyManifest(lines, entries);
        string intro = ReconstructionService.BuildIntroduction(lines, IntroPages);

        var book = new Book
        {
            Title = Path.GetFileNameWithoutExtension(fullPdfPath),
            Introduction = intro,
            AdventureSheetPath = string.Empty,
            MapPath = $"/assets/game-art/{Slug}/p{MapPage}_i0.png",
            Author = "Steve Jackson and Ian Livingstone",
            Sections = sections.Select(s => new Section
            {
                SectionNumber = s.SectionNumber,
                Content = NormalizeTurnTos
                    ? TurnToVariantRegex.Replace(s.Content, "turn to $2")
                    : s.Content,
                ImagePath = $"/assets/game-art/{Slug}/{s.ImagePath}_i0.png",
                Choices = new List<Choice>(),
                HasCombat = false,
            }).ToList(),
        };

        foreach (var s in book.Sections)
            s.Content = PostProcessSection(s.SectionNumber, s.Content);

        FillGaps(book);
        await PersistBookAsync(book);
        return book;
    }

    private List<ReconstructionService.ManifestEntry> LoadManifest()
    {
        using var stream = typeof(ManifestDungeonWorldParser).Assembly
            .GetManifestResourceStream(ManifestResourceName)
            ?? throw new InvalidOperationException($"Missing embedded manifest resource {ManifestResourceName}.");
        using var doc = JsonDocument.Parse(stream);
        var entries = new List<ReconstructionService.ManifestEntry>();
        foreach (var e in doc.RootElement.GetProperty("entries").EnumerateArray())
            entries.Add(new ReconstructionService.ManifestEntry(
                e.GetProperty("n").GetInt32(),
                e.GetProperty("page").GetInt32(),
                e.GetProperty("side").GetString()!,
                e.GetProperty("line").GetInt32(),
                e.TryGetProperty("end", out var en) && en.ValueKind == JsonValueKind.Number ? en.GetInt32() : null));
        return entries.OrderBy(e => e.Number).ToList();
    }

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

    private async Task PersistBookAsync(Book book)
    {
        string outputDir = Path.Combine(
            Path.GetFullPath(_storageOptions.PdfUploadPath),
            "ProcessedBooks");
        Directory.CreateDirectory(outputDir);
        string jsonPath = GetUniqueFilePath(Path.Combine(outputDir, $"{book.Title}.json"));
        await File.WriteAllTextAsync(jsonPath,
            JsonSerializer.Serialize(book, JsonOptions));
    }

    private static string GetUniqueFilePath(string desiredPath)
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
}
