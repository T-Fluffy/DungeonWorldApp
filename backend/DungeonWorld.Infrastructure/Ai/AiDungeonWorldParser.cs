using System.Text;
using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

namespace DungeonWorld.Infrastructure.Ai;

/// <summary>
/// Layout-agnostic parser that uses an LLM to turn raw PDF text into Fighting
/// Fantasy sections. Raw text is recovered with PdfPig, then an OpenAI-compatible
/// model splits it into numbered sections. Downstream structural analysis
/// (graph, combat, features) is still performed deterministically by the Cleaning library.
/// </summary>
public sealed class AiDungeonWorldParser : IBookParser
{
    public const string ParserIdentifier = "AI";

    private static readonly System.Text.RegularExpressions.Regex TurnToRegex = new(
        @"(?i)turn\s+to\s+(?:the\s+)?(\d{1,4})",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly string SystemPrompt = """
        You are a meticulous document extraction engine for Fighting Fantasy gamebooks.

        A Fighting Fantasy book consists of numbered sections (normally 1 to 400). Each section
        is a block of prose that usually ends with instructions to turn to another section,
        e.g. "If you wish to take the sword, turn to 134." Some sections contain combat blocks
        with enemy statistics (SKILL / STAMINA) and some end the adventure.

        Your job is to split the provided raw OCR text into sections and reproduce each section's
        text as accurately as possible.

        Rules:
        - Reproduce the text VERBATIM. Do not paraphrase, summarize, reorder, or invent words.
        - Fix only OCR artifacts: words split across lines, stray hyphenation, spurious page
          numbers, running headers/footers, and irregular whitespace.
        - Preserve paragraph structure with single line breaks inside a paragraph and a blank
          line between paragraphs.
        - Preserve every digit exactly (section references, statistics, item numbers, dice rolls).
        - A section begins with a standalone number. Ignore page numbers, chapter headings,
          running headers, and anything before the first section.
        - The text may start or end in the middle of a section because it is a slice of a larger
          document. Include such partial sections exactly as they appear.
        - If the text contains no complete or partial sections, return an empty sections array.

        Return STRICT JSON only, with no markdown and no commentary, in exactly this shape:
        {"sections":[{"number":1,"content":"..."}]}
        """;

    private readonly IPdfTextExtractor _textExtractor;
    private readonly ILlmClient _llm;
    private readonly IOptions<LlmOptions> _llmOptions;
    private readonly IOptions<FileStorageOptions> _storageOptions;
    private readonly ILogger<AiDungeonWorldParser> _logger;

    public AiDungeonWorldParser(
        IPdfTextExtractor textExtractor,
        ILlmClient llm,
        IOptions<LlmOptions> llmOptions,
        IOptions<FileStorageOptions> storageOptions,
        ILogger<AiDungeonWorldParser> logger)
    {
        _textExtractor = textExtractor;
        _llm = llm;
        _llmOptions = llmOptions;
        _storageOptions = storageOptions;
        _logger = logger;
    }

    public string ParserId => ParserIdentifier;

    // The LLM handles any layout, so this parser is always a candidate.
    public bool CanHandle(string filePath, string bookTitle) => true;

    public async Task<Book> ParseAsync(string filePath)
    {
        var opts = _llmOptions.Value;
        if (!opts.IsConfigured)
            throw new InvalidOperationException(
                "The LLM parser is not configured. Set 'Llm:ApiKey' (and 'Llm:Endpoint' if not OpenAI).");

        var fullPdfPath = ResolvePath(filePath);
        var bookTitle = Path.GetFileNameWithoutExtension(fullPdfPath);
        var bookSlug = CreateSlug(bookTitle);

        var book = new Book
        {
            Title = bookTitle,
            Author = "Steve Jackson and Ian Livingstone",
        };

        _logger.LogInformation("[{ParserId}] Parsing {Title} via LLM ({Model})",
            ParserId, bookTitle, opts.Model);

        var pages = _textExtractor.Extract(fullPdfPath);
        var imageFolder = EnsureImageFolder(bookSlug);
        ExtractImages(fullPdfPath, imageFolder, bookSlug, book);

        var chunks = SectionChunker.Chunk(pages, opts.ChunkPageSize);
        var chunkResults = new List<IReadOnlyList<LlmSection>>(chunks.Count);

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var userPrompt = BuildUserPrompt(chunk);
            var raw = await _llm.CompleteAsync(SystemPrompt, userPrompt);
            var sections = LlmSectionParser.Parse(raw);
            chunkResults.Add(sections);

            _logger.LogInformation("[{ParserId}] Chunk {Chunk}/{Total}: extracted {Count} sections",
                ParserId, i + 1, chunks.Count, sections.Count);
        }

        var merged = SectionChunker.MergeChunks(chunkResults);
        foreach (var section in merged)
        {
            book.Sections.Add(new Section
            {
                SectionNumber = section.Number,
                Content = section.Content,
                ImagePath = $"/assets/game-art/{bookSlug}/p{FindImagePage(pages, section.Number)}_i0.png",
                Choices = ExtractChoices(section.Content),
                HasCombat = section.Content.Contains("SKILL") && section.Content.Contains("STAMINA"),
            });
        }

        FillGaps(book);
        await PersistBookAsync(book, bookTitle);

        return book;
    }

    private string BuildUserPrompt(List<TextBlock> chunk)
    {
        var firstPage = chunk[0].LogicalPage;
        var lastPage = chunk[^1].LogicalPage;
        var sb = new StringBuilder();
        sb.Append("Here is raw text extracted from logical pages ")
          .Append(firstPage).Append(" to ").Append(lastPage)
          .AppendLine(" of the book. Split it into sections.")
          .AppendLine().AppendLine("<text>");

        foreach (var pageGroup in chunk.GroupBy(b => b.LogicalPage))
        {
            foreach (var block in pageGroup)
            {
                sb.AppendLine(block.Text);
            }
            sb.AppendLine();
        }

        sb.AppendLine("</text>");
        return sb.ToString();
    }

    private static int FindImagePage(List<TextBlock> blocks, int sectionNumber)
    {
        // Best-effort mapping from section number to the physical page that likely
        // contained its header (the section typically begins near this position).
        var pages = blocks.GroupBy(b => b.LogicalPage)
            .Select(g => g.First().PhysicalPage)
            .ToList();
        var count = pages.Count;
        if (count == 0) return 1;
        var target = (int)Math.Ceiling(sectionNumber * (count / 400.0));
        return pages[Math.Clamp(target, 0, count - 1)];
    }

    private static List<Choice> ExtractChoices(string text)
    {
        var matches = TurnToRegex.Matches(text);
        return matches.Select(m => new Choice
        {
            TargetSectionNumber = int.Parse(m.Groups[1].Value),
            Description = $"Turn to {m.Groups[1].Value}",
            IsDiceRoll = text.Contains("roll", StringComparison.OrdinalIgnoreCase) ||
                         text.Contains("dice", StringComparison.OrdinalIgnoreCase),
        }).ToList();
    }

    private void ExtractImages(string pdfPath, string folder, string slug, Book book)
    {
        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
        {
            foreach (var img in page.GetImages())
            {
                try
                {
                    if (img.WidthInSamples < 50 || img.HeightInSamples < 50) continue;

                    var imgName = $"p{page.Number}_i0.png";
                    File.WriteAllBytes(Path.Combine(folder, imgName), img.RawBytes.ToArray());

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

    private void FillGaps(Book book)
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
                    Choices = new List<Choice>(),
                });
            }
        }
        book.Sections = book.Sections.OrderBy(s => s.SectionNumber).ToList();
    }

    private async Task PersistBookAsync(Book book, string title)
    {
        var outputDir = Path.Combine(Path.GetFullPath(_storageOptions.Value.PdfUploadPath), "ProcessedBooks");
        Directory.CreateDirectory(outputDir);

        var jsonPath = Path.Combine(outputDir, $"{title}.json");
        await File.WriteAllTextAsync(jsonPath,
            JsonSerializer.Serialize(book, new JsonSerializerOptions { WriteIndented = true }));
    }

    private string ResolvePath(string fileName) =>
        Path.Combine(Path.GetFullPath(_storageOptions.Value.PdfUploadPath),
            fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? fileName : $"{fileName}.pdf");

    private string CreateSlug(string title) =>
        title.Replace(" ", "_").ToLower();

    private string EnsureImageFolder(string slug)
    {
        var path = Path.Combine(Path.GetFullPath(_storageOptions.Value.ImageOutputPath), slug);
        Directory.CreateDirectory(path);
        return path;
    }
}
