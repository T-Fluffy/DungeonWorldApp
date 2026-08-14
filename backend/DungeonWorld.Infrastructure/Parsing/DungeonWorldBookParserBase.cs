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

    /// <summary>
    /// A resync candidate must be a clean number (optionally followed by a period),
    /// never a garbled line like "240)?" that HeaderRegex would still match.
    /// </summary>
    protected static readonly Regex CleanSectionNumberRegex = new(
        @"^\d{1,4}\.?\s*$",
        RegexOptions.Compiled);

    private const string PlaceholderText = "[Text missing or unreadable in PDF]";

    /// <summary>Line that closes a section: an ending "Turn to N" (optionally followed by direction words).</summary>
    private static readonly Regex TurnTerminatorRegex = new(
        @"(?i)\b(?:turn\s+to\s+\d{1,4})(?:\s+\w+){0,3}\s*\)?[.!?]?\s*$",
        RegexOptions.Compiled);

    /// <summary>Line that closes a section with a parenthesised choice, e.g. "(turn to 272) or threaten him (turn to 127)?"</summary>
    private static readonly Regex ChoiceTerminatorRegex = new(
        @"(?i)\(turn\s+to\s+\d{1,4}\)\s*(?:or\s+[^()]*\(turn\s+to\s+\d{1,4}\))?\s*[.!?]?\s*$",
        RegexOptions.Compiled);

    /// <summary>Line that closes a combat section, e.g. "BARBARIAN SKILL 7 STAMINA 6".</summary>
    private static readonly Regex StatTerminatorRegex = new(
        @"SKILL\s*\d+\s+STAMINA\s*\d+",
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

    /// <summary>
    /// Maximum section-number gap accepted for a resync. Scaled by the number of
    /// physical pages since the last accepted header (Fighting Fantasy fits well
    /// under a dozen sections per page), so a garbled far-ahead line cannot jump
    /// the sequence while a genuinely degraded scan can still recover.
    /// </summary>
    protected virtual int ResyncMaxJump(TextBlock block, int lastAcceptedPage) =>
        Math.Max(16, (block.PhysicalPage - lastAcceptedPage + 1) * 12);

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

    /// <summary>
    /// Optional per-book introduction/rule text placed before section 1. The default
    /// captures every block before the first detected section header, so front-matter
    /// (story hook, rule explanations) flows into the cleaned book and the rules extractor.
    /// </summary>
    protected virtual string BuildIntroduction(IReadOnlyList<TextBlock> blocks)
    {
        var sb = new StringBuilder();
        foreach (var block in blocks)
        {
            if (block.TopFraction > PageNumberBand) continue;
            if (TryParseSectionNumber(block.Text).HasValue) break;
            sb.Append(block.Text).Append("\n\n");
        }
        return sb.ToString().Trim();
    }

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
            .Where(b => !IsHeaderOrFooter(b) && !IsPageNumber(b))
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
        int lastAcceptedNumber = 0;

        // When a duplicate header is discarded its buffer may hold the REAL content of
        // that section (a prior misparse consumed the section under a wrong number).
        // Last occurrence wins: in a desync the final pass over a header is the true one.
        var discardedContent = new Dictionary<int, string>();

        void FlushSection()
        {
            if (currentNumber <= 0 || currentNumber > MaxSectionNumber) return;
            // A header that is matched a second time must not duplicate a section.
            if (book.Sections.Any(s => s.SectionNumber == currentNumber))
            {
                discardedContent[currentNumber] = buffer.ToString().Trim();
                return;
            }

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

                // Once inside the adventure, a mid-page standalone number that keeps the
                // sequence ascending is a section header. The tight sequential window
                // above can permanently desync when a run of headers is garbled or
                // missing in a degraded scan, so accept increasing numbers as a resync.
                // Guarded against false positives:
                //   - only a clean number (rejects garbled lines like "240)?")
                //   - jump bounded by how many pages could physically fit between the
                //     last accepted header and this one (<= ~12 sections per page)
                //   - front-matter tables are still rejected: currentNumber == 0
                bool resync = currentNumber > 0 &&
                              headerNumber.Value > lastAcceptedNumber &&
                              headerNumber.Value - lastAcceptedNumber <= ResyncMaxJump(block, currentPhysicalPage) &&
                              CleanSectionNumberRegex.IsMatch(block.Text.Trim());

                if (sequential || visual || resync)
                {
                    FlushSection();
                    currentNumber = headerNumber.Value;
                    expectedNext = headerNumber.Value + 1;
                    lastAcceptedNumber = headerNumber.Value;
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
        RecoverOrphanSections(book, discardedContent);
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

    /// <summary>Fraction of the page height at/below which a standalone number is page furniture.</summary>
    protected virtual double PageNumberBand => 0.9;

    private bool IsPageNumber(TextBlock block) =>
        block.TopFraction > PageNumberBand && TryParseSectionNumber(block.Text).HasValue;

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
                Content = PlaceholderText,
                Choices = new List<Choice>(),
            });
        }
        book.Sections = book.Sections.OrderBy(s => s.SectionNumber).ToList();
    }

    /// <summary>
    /// Recovers sections whose header digit was missed by OCR. When section n is
    /// present but n+1 is missing (single gap), the n+1 content flowed into n's
    /// buffer because no header was seen to start a new section. Fighting Fantasy
    /// sections end with a "Turn to N", a parenthesised choice or a combat stat
    /// line, so the trailing orphan can be split off at that boundary.
    /// </summary>
    private void RecoverOrphanSections(Book book, Dictionary<int, string> discardedContent)
    {
        var byNumber = book.Sections.ToDictionary(s => s.SectionNumber);
        for (int n = 1; n < MaxSectionNumber; n++)
        {
            if (!byNumber.TryGetValue(n, out var cur)) continue;
            if (cur.Content == PlaceholderText) continue;

            // n+1 already present: no gap here.
            if (byNumber.ContainsKey(n + 1)) continue;
            // A run of missing sections is still recoverable one at a time: the split
            // off n+1 is re-visited on the next loop pass, which can then split n+2 off
            // it. Only n+2's presence gate stays: nothing follows a missing tail.

            bool splitOk = TrySplitOrphan(cur.Content, n + 1, out var own, out var orphan);
            if (splitOk && IsPlausibleSection(orphan))
            {
                cur.Content = own;
                cur.Choices = BuildChoices(own);
                cur.HasCombat = own.Contains("SKILL") && own.Contains("STAMINA");

                var next = new Section
                {
                    SectionNumber = n + 1,
                    Content = orphan,
                    Choices = BuildChoices(orphan),
                    HasCombat = orphan.Contains("SKILL") && orphan.Contains("STAMINA"),
                    ImagePath = cur.ImagePath,
                };
                book.Sections.Add(next);
                byNumber[n + 1] = next;
                Console.WriteLine($"[orphan] recovered section {n + 1} from section {n}");
            }
        }

        RebalanceShiftedSections(book, discardedContent);
    }

    /// <summary>
    /// When a section header is misread by OCR, the whole following block of content
    /// shifts down one section number: section n+1 ends up inside the buffer of the
    /// section after it (n+2), preceded by the rejected "n+1" header line. If n+1 is
    /// missing but n+2 is present and its buffer contains a bare "n+1" number, the
    /// buffer is re-split so n+1 gets the content after that line and n gets the
    /// content before it (replacing the degraded content that was wrongly attributed
    /// to it). n+2's own content was consumed by the misparse, but if a later duplicate
    /// of n+2's header was discarded with real content, that is restored instead.
    /// </summary>
    private void RebalanceShiftedSections(Book book, Dictionary<int, string> discardedContent)
    {
        var byNumber = book.Sections.ToDictionary(s => s.SectionNumber);
        for (int n = 1; n + 2 <= MaxSectionNumber; n++)
        {
            if (!byNumber.TryGetValue(n, out var cur)) continue;
            if (cur.Content == PlaceholderText) continue;
            if (byNumber.ContainsKey(n + 1)) continue;
            if (!byNumber.TryGetValue(n + 2, out var shifted)) continue;
            if (shifted.Content == PlaceholderText) continue;

            var lines = shifted.Content.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            int boundary = -1;
            for (int i = 1; i < lines.Count - 1; i++)
            {
                if (lines[i] == (n + 1).ToString() ||
                    lines[i] == $"{n + 1}.")
                {
                    boundary = i;
                    break;
                }
            }
            if (boundary < 0) continue;

            var before = lines.Take(boundary).ToList();
            var after = lines.Skip(boundary + 1).ToList();
            while (before.Count > 0 && IsJunkLine(before[^1])) before.RemoveAt(before.Count - 1);
            while (after.Count > 0 && IsJunkLine(after[0])) after.RemoveAt(0);
            while (after.Count > 0 && IsJunkLine(after[^1])) after.RemoveAt(after.Count - 1);

            string beforeText = string.Join("\n\n", before);
            string afterText = string.Join("\n\n", after);
            if (!IsPlausibleSection(beforeText) || !IsPlausibleSection(afterText)) continue;

            // Only replace n's content when it is clearly degraded (short or leading junk);
            // otherwise the rebalance could clobber a healthy short section.
            var curLines = cur.Content.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            bool curDegraded = curLines.Count == 0 || cur.Content.Length < 250 ||
                               IsJunkLine(curLines[0]) ||
                               Regex.IsMatch(curLines[0], @"^\d{1,4}\s+\w");
            if (!curDegraded) continue;

            cur.Content = beforeText;
            cur.Choices = BuildChoices(beforeText);
            cur.HasCombat = beforeText.Contains("SKILL") && beforeText.Contains("STAMINA");

            var next = new Section
            {
                SectionNumber = n + 1,
                Content = afterText,
                Choices = BuildChoices(afterText),
                HasCombat = afterText.Contains("SKILL") && afterText.Contains("STAMINA"),
                ImagePath = shifted.ImagePath,
            };
            book.Sections.Add(next);
            byNumber[n + 1] = next;

            shifted.Content = PlaceholderText;
            shifted.Choices = new List<Choice>();
            shifted.HasCombat = false;

            if (discardedContent.TryGetValue(n + 2, out var restored) &&
                !string.IsNullOrWhiteSpace(restored) &&
                restored != PlaceholderText &&
                IsPlausibleSection(restored))
            {
                shifted.Content = restored;
                shifted.Choices = BuildChoices(restored);
                shifted.HasCombat = restored.Contains("SKILL") && restored.Contains("STAMINA");
                Console.WriteLine($"[orphan] restored section {n + 2} from discarded buffer");
            }

            Console.WriteLine($"[orphan] rebalanced section {n + 1} out of section {n + 2} (repaired section {n})");
        }
    }

    private static bool IsTerminatorLine(string line)
    {
        string t = line.Trim();
        if (t.Length == 0) return false;
        return TurnTerminatorRegex.IsMatch(t) || ChoiceTerminatorRegex.IsMatch(t) || StatTerminatorRegex.IsMatch(t);
    }

    /// <summary>Trailing folio ("44-48"), cover, garbled-header or OCR-garbage lines that are not section content.</summary>
    private static bool IsJunkLine(string line)
    {
        string t = line.Trim();
        if (t.Length == 0) return true;
        if (Regex.IsMatch(t, @"^\d{1,3}[-–—]\d{1,3}$")) return true;          // folio range
        if (Regex.IsMatch(t, @"^[^\w\s]{2,}$")) return true;                  // punctuation-only garbage
        if (Regex.IsMatch(t, @"^[A-Za-z]{1,2}$")) return true;                // stray header letter / fragment
        if (Regex.IsMatch(t, @"^\d{1,4}\s+\w")) return true;                  // header digit merged with content ("84 Seated...")
        // Illustration/back-cover OCR debris: mostly punctuation with a few scattered
        // letters (e.g. "'vf/.:!/", "I,yn.f/", "i'//r'ﬁ/\"riﬁ‘"). Prose lines keep a
        // high letter ratio, so a symbol-heavy short line is safe to drop. Pure short
        // words like "VAT" or "To go" are left alone (no symbols present).
        if (t.Length >= 2 && t.Any(c => !char.IsLetterOrDigit(c)) &&
            (double)t.Count(char.IsLetterOrDigit) / t.Length < 0.55) return true;
        if (t.StartsWith("Cover illustration by", StringComparison.OrdinalIgnoreCase)) return true;
        // OCR often inserts stray apostrophes/backticks or drops spaces ("F'IGHTING",
        // "greatgoal"), so match the marketing lines on a de-punctuated form too.
        string norm = Regex.Replace(t, @"['`’‘´´,\.\-]", "");
        if (t.Contains("FIGHTING FANTASY", StringComparison.OrdinalIgnoreCase) ||
            norm.Contains("FIGHTING FANTASY", StringComparison.OrdinalIgnoreCase)) return true;
        if (t.Contains("world-wide sensation", StringComparison.OrdinalIgnoreCase)) return true;
        // Back-cover marketing blurb (Fighting Fantasy edition blurbs on the final page).
        foreach (var phrase in new[]
        {
            "part story, part game",
            "you become the hero",
            "two dice, a pencil and an eraser",
            "a perilous quest to find the",
            "warlock's treasure",
            "warlocks treasure",
            "route to follow",
            "elaborate combat",
            "system given in the book",
            "you may not survive your first journey",
            "experience, skill and luck",
            "nearer to your",
        })
        {
            if (t.Contains(phrase, StringComparison.OrdinalIgnoreCase) ||
                norm.Contains(phrase, StringComparison.OrdinalIgnoreCase)) return true;
        }
        if (Regex.IsMatch(t, @"great\s*goal", RegexOptions.IgnoreCase)) return true;
        return false;
    }

    /// <summary>A garbled section header: a digit-led 1-2 character line ("7", "8o", "4").</summary>
    private static bool IsGarbledHeaderLine(string line) =>
        Regex.IsMatch(line.Trim(), @"^\d[0-9A-Za-z]?$");

    /// <summary>
    /// Splits "content" into the owning section and the orphaned next section that
    /// follows it. Boundary signals, in order of reliability:
    ///  1. a garbled header digit (a missed header whose number was truncated, e.g.
    ///     "8o" for 80) — the orphan is exactly everything after it;
    ///  2. a page-top folio range ("149-151") whose first number is the missing
    ///     section — it marks the page boundary where the missed header sat;
    ///  3. the owning section's closing terminator (turn/choice/stat line).
    /// Returns false when no boundary can be located with confidence.
    /// </summary>
    private static bool TrySplitOrphan(string content, int nextNumber, out string own, out string orphan)
    {
        own = content;
        orphan = string.Empty;

        var lines = content.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        if (lines.Count < 3) return false;

        // Drop trailing junk (folios, cover lines) so it can't hide the boundary.
        while (lines.Count > 0 && IsJunkLine(lines[^1])) lines.RemoveAt(lines.Count - 1);
        if (lines.Count < 3) return false;

        var terminators = Enumerable.Range(0, lines.Count)
            .Where(i => IsTerminatorLine(lines[i]))
            .ToList();
        var garbled = Enumerable.Range(0, lines.Count)
            .Where(i => IsGarbledHeaderLine(lines[i]))
            .ToList();
        var folio = Enumerable.Range(0, lines.Count)
            .Select(i => (Index: i, Match: Regex.Match(lines[i], @"^(\d{1,3})[-–—]\d{1,3}$")))
            .Where(x => x.Match.Success &&
                        int.TryParse(x.Match.Groups[1].Value, out var first) && first == nextNumber)
            .Select(x => x.Index)
            .ToList();

        // Signal 1: a garbled header marks the exact start of the orphan.
        if (garbled.Count > 0)
        {
            int g = garbled[^1];
            var orphanLines = lines.Skip(g + 1).ToList();
            while (orphanLines.Count > 0 && (IsJunkLine(orphanLines[0]) ||
                                             Regex.IsMatch(orphanLines[0], @"^\W*\d{1,4}\W*$")))
                orphanLines.RemoveAt(0);
            while (orphanLines.Count > 0 && IsJunkLine(orphanLines[^1])) orphanLines.RemoveAt(orphanLines.Count - 1);

            var ownLines = lines.Take(g).ToList();
            while (ownLines.Count > 0 && IsJunkLine(ownLines[^1])) ownLines.RemoveAt(ownLines.Count - 1);

            own = string.Join("\n\n", ownLines);
            orphan = string.Join("\n\n", orphanLines);
            if (IsPlausibleSection(orphan)) return true;
            // fall through to the folio / terminator signals if the orphan looks wrong
        }

        // Signal 2: a folio range whose first number is the missing section sits
        // right where the missed header should have been.
        if (folio.Count > 0)
        {
            int f = folio[^1];
            if (f < lines.Count - 1)
            {
                var orphanLines = lines.Skip(f + 1).ToList();
                while (orphanLines.Count > 0 && (IsJunkLine(orphanLines[0]) ||
                                                 Regex.IsMatch(orphanLines[0], @"^\W*\d{1,4}\W*$")))
                    orphanLines.RemoveAt(0);
                while (orphanLines.Count > 0 && IsJunkLine(orphanLines[^1])) orphanLines.RemoveAt(orphanLines.Count - 1);

                var ownLines2 = lines.Take(f).ToList();
                while (ownLines2.Count > 0 && IsJunkLine(ownLines2[^1])) ownLines2.RemoveAt(ownLines2.Count - 1);

                own = string.Join("\n\n", ownLines2);
                orphan = string.Join("\n\n", orphanLines);
                if (IsPlausibleSection(orphan)) return true;
            }
        }

        // Signal 3: the orphan sits after the owning section's closing terminator.
        if (terminators.Count == 0) return false;
        int boundary;
        if (terminators[^1] == lines.Count - 1)
        {
            if (terminators.Count < 2) return false;
            boundary = terminators[^2]; // buffer ends on the orphan's own turn
        }
        else
        {
            boundary = terminators[^1]; // orphan ends garbled; last clean turn closes the owner
        }

        var oLines = lines.Skip(boundary + 1).ToList();
        while (oLines.Count > 0 && (IsJunkLine(oLines[0]) ||
                                    Regex.IsMatch(oLines[0], @"^\W*\d{1,4}\W*$")))
            oLines.RemoveAt(0);
        while (oLines.Count > 0 && IsJunkLine(oLines[^1])) oLines.RemoveAt(oLines.Count - 1);

        var ownLines3 = lines.Take(boundary + 1).ToList();
        while (ownLines3.Count > 0 && IsJunkLine(ownLines3[^1])) ownLines3.RemoveAt(ownLines3.Count - 1);

        own = string.Join("\n\n", ownLines3);
        orphan = string.Join("\n\n", oLines);
        return true;
    }

    /// <summary>Heuristic guard so a mis-split cannot mangle a healthy section.</summary>
    private static bool IsPlausibleSection(string text)
    {
        if (text.Length < 60 || text.Length > 1200) return false;
        int lineCount = text.Split('\n').Count(l => l.Trim().Length > 0);
        if (lineCount < 2) return false; // a section spans at least two lines
        char first = text.TrimStart().FirstOrDefault();
        if (!char.IsUpper(first)) return false;
        if (lineCount > 24) return false;
        return true;
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
