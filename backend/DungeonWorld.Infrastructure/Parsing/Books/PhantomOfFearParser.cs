using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF28 Phantom of Fear (Steve Jackson and Ian Livingstone, 1985). Rebuilt from a manual reconstruction
/// manifest (300 dpi line transcripts, 400 sections across PDF pages 14-139; each PDF page is a two-up
/// portrait spread split into L/R halves). Sections print in ascending numeric order, so bounds run from
/// one section number to the next. The introduction is assembled from the cover and introduction pages.
///
/// <para><b>Exit graph.</b> The graph resource is currently an empty placeholder. Section exits
/// visually verified against the book are applied as overrides; all other sections retain their
/// OCR-derived references as provisional candidates.</para>
/// </summary>
public sealed class PhantomOfFearParser : ManifestDungeonWorldParser
{
    private const string EdgesResourceName =
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff28_edges.json";

    public PhantomOfFearParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "PhantomOfFear";

    public override bool CanHandle(string filePath, string bookTitle) =>
        bookTitle.Contains("Phantom of Fear", StringComparison.OrdinalIgnoreCase) ||
        bookTitle.Contains("Phantoms of Fear", StringComparison.OrdinalIgnoreCase);

    protected override string TitleMatch => "Phantoms of Fear";

    protected override string Slug => "ff28_phantom_of_fear";

    protected override string ManifestResourceName =>
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff28.json";

    /// <summary>Cover blurb (book p1) plus INTRODUCTION (book p7); the rules pages are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 2, 7 };

    /// <summary>FF28 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    private static readonly IReadOnlyDictionary<int, int[]> VerifiedReferenceOverrides =
        new Dictionary<int, int[]>
        {
            [5] = [337, 374],
            [2] = [123, 310],
            [10] = [311],
            [11] = [217],
            [14] = [92],
            [33] = [],
            [22] = [217, 359],
            [24] = [13, 274],
            [26] = [82, 312, 353],
            [28] = [383],
            [36] = [119, 330],
            [41] = [380],
            [42] = [100, 161],
            [43] = [14, 189],
            [44] = [217, 280],
            [51] = [148, 227, 349],
            [53] = [73, 364],
            [54] = [],
            [55] = [217],
            [56] = [79, 103, 273],
            [57] = [239],
            [59] = [209, 236],
            [62] = [161],
            [67] = [380, 395],
            [66] = [],
            [70] = [14, 189],
            [71] = [157, 185],
            [74] = [135, 383],
            [76] = [25],
            [79] = [314],
            [84] = [281, 333],
            [85] = [168],
            [89] = [146, 240],
            [93] = [57],
            [73] = [],
            [94] = [52, 242],
            [100] = [130, 317],
            [102] = [195, 388],
            [96] = [],
            [107] = [8, 155, 223],
            [111] = [246, 284],
            [115] = [177, 210],
            [113] = [379],
            [117] = [3, 174, 219],
            [118] = [133, 335],
            [119] = [221, 233],
            [122] = [277, 321],
            [128] = [],
            [133] = [335],
            [132] = [123, 310],
            [134] = [147, 391],
            [144] = [91, 341],
            [141] = [203, 363],
            [142] = [188],
            [149] = [105],
            [147] = [371],
            [151] = [27, 304],
            [152] = [86, 321],
            [163] = [143],
            [164] = [253],
            [171] = [383],
            [172] = [83, 150],
            [159] = [260, 283],
            [178] = [18, 300],
            [177] = [288],
            [187] = [81, 118],
            [183] = [89],
            [194] = [],
            [196] = [85, 272],
            [186] = [397],
            [201] = [400],
            [198] = [289, 389],
            [211] = [61],
            [224] = [115, 163],
            [229] = [257, 336],
            [238] = [75, 149],
            [244] = [134, 297],
            [246] = [],
            [247] = [85],
            [261] = [357, 390],
            [263] = [54, 145],
            [267] = [87, 394],
            [273] = [314],
            [274] = [112, 143, 288],
            [278] = [117, 165],
            [277] = [206],
            [281] = [193, 385],
            [282] = [101, 383],
            [287] = [116, 215],
            [290] = [380],
            [289] = [20, 49],
            [291] = [18, 178, 300],
            [299] = [269, 330],
            [302] = [76],
            [303] = [224, 356],
            [304] = [107, 245],
            [307] = [146],
            [309] =
            [
                66, 92, 105, 109, 111, 121, 131, 137, 147, 150, 153, 154, 159, 160, 168, 169, 171, 175,
                176, 177, 179, 181, 183, 185, 187, 188, 189, 192, 193, 195, 197, 198, 201, 202, 204, 205,
                209, 211, 214, 215, 216, 217, 218, 219, 220, 221, 223, 227, 230, 232, 233, 234, 237, 240,
                242, 243, 245, 246, 248, 250, 251, 252, 253, 254, 256, 257, 259, 260, 262, 266, 267, 268,
                269, 270, 271, 272, 273, 274, 275, 276, 277, 279, 280, 281, 282, 283, 284, 286, 288, 290,
                291, 292, 294, 295, 296, 297, 299, 300, 302, 305, 307, 308, 312, 313, 314, 319, 321, 322,
                324, 325, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 337, 338, 341, 342, 344, 345,
                346, 347, 349, 350, 352, 353, 354, 356, 357, 359, 360, 361, 362, 363, 365, 367, 368, 370,
                371, 372, 373, 374, 377, 378, 379, 380, 381, 382, 383, 384, 386, 389, 390, 392, 395, 396,
                398, 399, 400
            ],
            [312] = [161, 353],
            [313] = [174, 213],
            [317] = [184, 393],
            [318] = [],
            [319] = [35, 114],
            [321] = [384],
            [323] = [19],
            [326] = [197, 265],
            [330] = [],
            [339] = [222, 275],
            [342] = [362],
            [333] = [26, 345],
            [353] = [42],
            [346] = [212],
            [361] = [105, 238],
            [363] = [29, 127, 154, 256],
            [369] = [281, 333],
            [374] = [90, 132, 237],
            [378] = [136],
            [381] = [204, 348],
            [386] = [7, 373],
            [389] = [360],
            [391] = [331],
            [392] = [350],
            [397] = [85],
            [203] = [29],
            [207] = [394],
            [213] = [278],
            [215] = [183, 116],
            [223] = [],
            [226] = [108, 369],
            [239] = [298],
            [250] = [167, 34, 128],
            [253] = [341, 172],
            [260] = [46, 142],
            [266] = [192],
            [268] = [],
            [269] = [379, 113],
            [272] = [368],
            [275] = [332],
            [276] = [317],
            [279] = [124, 114],
            [280] = [217],
            [285] = [116],
            [286] = [244, 297],
            [293] = [361],
            [294] = [129, 298],
            [298] = [376, 50],
            [301] = [152, 324],
            [311] = [267],
            [314] = [181],
            [325] = [],
            [360] = [65, 334],
            [362] = [78, 151],
            [368] = [85],
            [371] = [331],
        };

    /// <summary>Written by Steve Jackson and Ian Livingstone.</summary>
    protected override string Author => "Steve Jackson and Ian Livingstone";

    /// <summary>
    /// Applies a complete graph when available, then overlays exits visually verified against the
    /// book. With the current empty placeholder, all other OCR-derived references remain candidates.
    /// </summary>
    protected override void PostProcessBook(Book book) => ApplyReferenceFixes(book);

    public static void ApplyReferenceFixes(Book book)
    {
        var edges = LoadEdges();
        bool hasCompleteGraph = edges.Count == MaxSections &&
            edges.Keys.All(section => section >= 1 && section <= MaxSections) &&
            edges.Values.Any(targets => targets.Count > 0);
        if (hasCompleteGraph)
        {
            foreach (var section in book.Sections)
            {
                section.ReferenceOverride = edges.TryGetValue(section.SectionNumber, out var targets)
                    ? targets
                    : new List<int>();
            }
        }

        foreach (var section in book.Sections)
        {
            if (VerifiedReferenceOverrides.TryGetValue(section.SectionNumber, out var targets))
                section.ReferenceOverride = targets.ToList();
        }
    }

    private static Dictionary<int, List<int>> LoadEdges()
    {
        using var stream = typeof(PhantomOfFearParser).Assembly
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
