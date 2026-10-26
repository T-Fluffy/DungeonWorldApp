using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF26 Crypt of the Sorcerer (Ian Livingstone, 1987). Rebuilt from a manual reconstruction
/// manifest (300 dpi line transcripts, 396 sections across PDF pages 14-120; each PDF page is a
/// two-up spread split into L/R halves). Sections print in ascending numeric order, so bounds run
/// from one section number to the next. Intro uses the cover blurb (2) plus BACKGROUND (11-13);
/// the curated intro text ships in Intros/ff26.txt.
///
/// <para><b>Layout notes.</b> Folio ranges head most halves ("178-179") with section headers
/// further down; headers are frequently lost or garbled to short fragments ("+8" + "1" = 178,
/// "1g" = 189, "ga" = 190). Those pins sit on the garbled header or on the body start when the
/// header is absent. PDF pages 112 and 113 are duplicate scans of the same spread (367-372);
/// only page 113 is used, and S366 carries an explicit end at the last line of p111R so the
/// duplicate falls into an unclaimed gap. Every placement was confirmed against the transcript's
/// folio ranges and, where ambiguous, the rendered PDF page.</para>
///
/// <para><b>Exit graph.</b> This scan garbles exit numbers far beyond what <c>TurnToRepair</c>
/// can safely undo ("turn to 232" where the book prints 132, "Turn to 176" for 178, and so on).
/// The authoritative edges therefore come from the complete Outspaced flowchart
/// (ff26.svgz, archived 2020-01-21: 400 nodes, 605 edges, all reachable) and are attached to
/// every section as <see cref="Section.ReferenceOverride"/>. Text extraction is unchanged; only
/// the cleaner's References/Choices come from the graph. The scan itself is missing sections
/// 14-17 (between the 9-13 spread and 18), so those bodies remain flagged MissingText even
/// though their edges are known (14/15 terminal, 16 -> 100/171, 17 -> 30).</para>
/// </summary>
public sealed class CryptsOfTheSorcererParser : ManifestDungeonWorldParser
{
    private const string EdgesResourceName =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff26_edges.json";

    public CryptsOfTheSorcererParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "CryptsOfTheSorcerer";

    protected override string TitleMatch => "Crypt of the Sorcerer";

    /// <summary>The stored PDF is named "FF26 Crypts of the Sorcerer", so match both spellings.</summary>
    public override bool CanHandle(string filePath, string bookTitle) =>
        bookTitle.Contains("Crypt of the Sorcerer", StringComparison.OrdinalIgnoreCase) ||
        bookTitle.Contains("Crypts of the Sorcerer", StringComparison.OrdinalIgnoreCase);

    protected override string Slug => "ff26_crypts_of_the_sorcerer";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff26.json";

    /// <summary>Cover blurb (2) plus BACKGROUND (11-13); the rules pages are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 2, 11, 12, 13 };

    /// <summary>FF26 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Written by Ian Livingstone (print: p2/p3 credits).</summary>
    protected override string Author => "Ian Livingstone";

    /// <summary>
    /// Attaches the authoritative flowchart edges to every section after gap filling, so even the
    /// four sections absent from this scan carry the correct exits. A terminal section gets an
    /// empty override, which clears any false OCR reference.
    /// </summary>
    protected override void PostProcessBook(Book book)
    {
        var edges = LoadEdges();
        foreach (var section in book.Sections)
        {
            section.ReferenceOverride = edges.TryGetValue(section.SectionNumber, out var targets)
                ? targets
                : new List<int>();
        }
    }

    private static Dictionary<int, List<int>> LoadEdges()
    {
        using var stream = typeof(CryptsOfTheSorcererParser).Assembly
            .GetManifestResourceStream(EdgesResourceName)
            ?? throw new InvalidOperationException(
                $"Missing embedded graph resource {EdgesResourceName}.");

        using var doc = JsonDocument.Parse(stream);
        var edges = new Dictionary<int, List<int>>();
        foreach (var property in doc.RootElement.GetProperty("edges").EnumerateObject())
        {
            edges[int.Parse(property.Name)] = property.Value
                .EnumerateArray()
                .Select(e => e.GetInt32())
                .ToList();
        }
        return edges;
    }
}
