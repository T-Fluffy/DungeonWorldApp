using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF27 Star Strider (Ian Livingstone, 1987). Rebuilt from a manual reconstruction manifest
/// (300 dpi line transcripts, 400 sections across PDF pages 14-115; each PDF page is a two-up
/// spread split into L/R halves). Sections print in ascending numeric order, so bounds run from
/// one section number to the next. The intro uses the cover blurb plus THE MISSION; the curated
/// intro text ships in Intros/ff27.txt.
///
/// <para><b>Exit graph.</b> This scan's embedded text layer is unusable and the OCR garbles exit
/// numbers heavily, so the authoritative edges come from the complete Outspaced flowchart
/// (ff27.svgz, archived 2020-01-22: 400 nodes, 550 edges, all reachable) and are attached to
/// every section as <see cref="Section.ReferenceOverride"/>. Text extraction is unchanged; only
/// the cleaner's References/Choices come from the graph. A terminal section gets an empty
/// override, which clears any false OCR reference.</para>
/// </summary>
public sealed class StarStriderParser : ManifestDungeonWorldParser
{
    private const string EdgesResourceName =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff27_edges.json";

    public StarStriderParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "StarStrider";

    protected override string TitleMatch => "Star Strider";

    protected override string Slug => "ff27_star_strider";

    protected override string ManifestResourceName =>
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff27.json";

    /// <summary>Cover blurb (book p1) plus THE MISSION (book pp9-11); the rules pages are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 2, 6, 7 };

    /// <summary>FF27 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Written by Ian Livingstone (print: p2/p3 credits).</summary>
    protected override string Author => "Ian Livingstone";

    /// <summary>
    /// Attaches the authoritative flowchart edges to every section after gap filling, so even any
    /// section absent from this scan carries the correct exits. A terminal section gets an empty
    /// override, which clears any false OCR reference.
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
        using var stream = typeof(StarStriderParser).Assembly
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
