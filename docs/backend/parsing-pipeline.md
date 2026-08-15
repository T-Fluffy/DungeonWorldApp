# Parsing pipeline

This is the heart of the engine: turning a Fighting Fantasy-style gamebook PDF into
a structured `Book` (raw sections) that the cleaner turns into `CleanedBook`.

There are **two parser families** and a **factory** that picks between them, plus a
**batch CLI** that can additionally merge multiple OCR passes for scan-heavy books.

```
                    ┌──────────────────────────────────────────────┐
   PDF ────────────▶│ IParserFactory.CreateParser(title)           │
                    │  → ManifestDungeonWorldParser (FF02–FF05)    │
                    │  → rule-based DungeonWorldBookParserBase     │
                    │  → DefaultDungeonWorldParser (fallback)      │
                    └──────────────────────────────────────────────┘
                                        │
                                        ▼
                                   ParseAsync
                    ┌──────────────────────────────────────────────┐
                    │  TextExtractor (PdfPig or OCR)               │
                    │  → sections detected (rules or manifest)     │
                    │  → FillGaps → Book                           │
                    └──────────────────────────────────────────────┘
```

## Layout of the code

After the reorganization, the parser files live in
`backend/DungeonWorld.Infrastructure/Parsing/`:

| File | Role |
| --- | --- |
| `DungeonWorldParserFactory.cs` | Chooses a parser by title (`IParserFactory`) |
| `DungeonWorldBookParserBase.cs` | Abstract rule-based parser shared by the generic + legacy book parsers |
| `DefaultDungeonWorldParser.cs` | Fallback: handles any book with the rule heuristics |
| `ManifestDungeonWorldParser.cs` | Abstract parser for books rebuilt from a manual reconstruction manifest |
| `PdfPigTextExtractor.cs` | Extracts text blocks from a PDF's embedded text layer |
| `PdfPigLayoutAnalyzer.cs` | Diagnostic layout detector (single vs 2-up page) |
| `MediaArtParser.cs` | Art-only extraction: crops full-page scans, exports embedded art |
| `ArtRegionDetector.cs` | Locates dense ink illustration blocks in a full-page scan |
| `TextBlock.cs` | Text-paragraph DTO with layout metadata |
| `Books/SeasOfBloodParser.cs` | Legacy rule-based parser for *Seas of Blood* |
| `Books/WarlockOfFiretopMountainParser.cs` | Rule-based parser for FF01 |
| `Books/CitadelOfChaosParser.cs` | Manifest parser for FF02 + section tail fixes |
| `Books/ForestOfDoomParser.cs` | Manifest parser for FF03 |
| `Books/StarshipTravellerParser.cs` | Manifest parser for FF04 (343 sections) |
| `Books/CityOfThievesParser.cs` | Manifest parser for FF05 |
| `Manifests/ff02.json … ff05.json` | Embedded reconstruction manifests |
| `Reconstruction/ReconstructionService.cs` | OCR + manifest application shared with the batch CLI |

## 1. Text extraction

Two extractors implement `IPdfTextExtractor.Extract(path)` → `List<TextBlock>`:

- **`PdfPigTextExtractor`** (API path) reads the PDF's embedded text layer with
  PdfPig, groups words into lines, lines into paragraphs, and marks each block with
  `LogicalPage`, `PhysicalPage`, `TopFraction`, `FontSize`, `IsBold`. Landscape pages
  (width/height ≥ 1.15) are treated as 2-up scans and split into two logical pages.
- **`OcrPdfTextExtractor`** (Ingestor path) renders pages to PNG with `pdftoppm`
  and OCRs them with Tesseract. Supports `Flat`, `ColumnCentre`, and `RegionSplit`
  two-up modes. Produces one `TextBlock` per OCR line.

`TextBlock` layout metadata is what lets the rule parser tell section headers apart
from body text.

## 2. Rule-based parsing (`DungeonWorldBookParserBase`)

Used by `DefaultDungeonWorldParser` and the legacy per-book parsers. The pipeline:

1. Filter blocks to non-header/footer/page-number content, order by logical page then
   vertical position.
2. Compute the average font size.
3. Walk blocks; when a standalone number is seen, decide whether it is a **section
   header** using three signals:
   - **sequential** — within the expected range (`expectedNext − 1 .. +5`);
   - **visual** — bold or ≥ 1.25× body font (`MatchSectionHeader`);
   - **resync** — a clean increasing number whose jump is bounded by how many
     physical pages could fit since the last accepted header (`ResyncMaxJump`).
4. On a header, flush the buffered section, start a new buffer.
5. At the end: `RecoverOrphanSections` + `RebalanceShiftedSections` repair
   missed-header parses, then `FillGaps` fills any missing section numbers with a
   placeholder.

Per-book hooks override behavior: `MaxSectionNumber` (FF04 = 343), `HeaderFooterBand`,
`MatchSectionHeader`, `ResyncMaxJump`, `BuildIntroduction`, `PageNumberBand`.

## 3. Manifest reconstruction (`ManifestDungeonWorldParser`)

For scan-heavy books (FF02–FF05) the automated rule pipeline cannot recover 400/400
sections, so the books were rebuilt **manually**: an operator reviewed OCR line
transcripts page by page and produced a manifest of body-start points.

A manifest entry is `{ n, page, side, line, [end] }`:

```json
{ "n": 1, "page": 17, "side": "L", "line": 8 },
{ "n": 2, "page": 17, "side": "L", "line": 20 },
...
```

`ReconstructionService`:

- `OcrPdf(page, dpi, onlyPages)` — renders + OCRs pages to line transcripts
  (`OcrLine { Page, N, Side, Top, Text }`), numbering lines per page half.
- `ApplyManifest(lines, entries)` — for each entry, the section is the text from its
  start line to the next entry's start line (or the entry's `end` line, or EOF).
  `TrimContent` removes noise lines (folios, headers, symbol-heavy garbage) and merges
  a dangling "Turn to" with its short target line on the next line.
- `BuildIntroduction(lines, introPages)` — concatenates the transcripts of the
  cover/title/BACKGROUND pages as the book introduction.

The manifest JSON ships as an **embedded resource** (csproj:
`<EmbeddedResource Include="Parsing\Manifests\*.json" />`), so each parser is
self-contained and reproducible.

Per-book overrides on the manifest parsers:

| Book | Quirk handled in the parser |
| --- | --- |
| FF02 Citadel of Chaos | `ApplySectionFixes` re-attaches 15 truncated "turn to" tails (the line-noise filter had dropped the real tail line, so the printed next-header number was swallowed; targets verified against 600 dpi crops + walkthroughs) |
| FF03 Forest of Doom | `NormalizeTurnTos = true` (normalizes garbled "Turnto261"→"turn to 261"); fixes section 193's "turn to 1710"→"turn to 171" |
| FF04 Starship Traveller | `MaxSectionNumber = 343` (the book physically has 343 sections) |
| FF05 City of Thieves | no code fix — its quirks (duplicate page 23≈24, page 64 header, mid-column starts) were resolved entirely inside the manifest |

### Section-aware post-processing

`ManifestDungeonWorldParser` exposes a `PostProcessSection(sectionNumber, content)`
virtual that defaults to `PostProcessContent(content)`. FF02 overrides it to run the
tail fixes, so the parser reproduces the curated `ProcessedBooks/FF02` byte-for-byte.

## 4. Parser selection (`DungeonWorldParserFactory`)

`CreateParser(filePath, title)`:

1. Iterate the registered specific parsers in order; the first whose
   `CanHandle(filePath, title)` returns true wins.
2. Otherwise return `DefaultDungeonWorldParser`.

Registration in `backend/DungeonWorld.API/Program.cs`:

```csharp
builder.Services.AddScoped<IBookParser, SeasOfBloodParser>();
builder.Services.AddScoped<IBookParser, WarlockOfFiretopMountainParser>();
builder.Services.AddScoped<IBookParser, DefaultDungeonWorldParser>();
builder.Services.AddScoped<IBookParser, CitadelOfChaosParser>();
builder.Services.AddScoped<IBookParser, ForestOfDoomParser>();
builder.Services.AddScoped<IBookParser, StarshipTravellerParser>();
builder.Services.AddScoped<IBookParser, CityOfThievesParser>();
```

The Ingestor builds the same list manually (it is a console app, not DI).

## 5. Batch Ingestor merge strategy

`backend/DungeonWorld.Ingestor/Program.cs`:

1. For each PDF, pick the parser via the factory and `ParseAsync`.
2. **Manifest parsers are authoritative**: if the selected parser is a
   `ManifestDungeonWorldParser`, the OCR merge is skipped (`present < MergeThreshold
   && parser is not ManifestDungeonWorldParser`).
3. Otherwise, if fewer than 400 sections are present, run extra OCR passes
   (`OcrPdfTextExtractor`) at several dpis × two-up modes, and `MergeBooks` picks the
   best content per section across candidates (longest non-placeholder wins).
4. Clean with `BookCleaner` and write to `CleanedData`.

## Diagnostics

The Ingestor has several debug flags used during reconstruction:

- `--ocr-extract <pdf>` — dump blocks from OCR.
- `--ocr-dump <page> <pdf>` — whole-page and left/right half OCR line dump.
- `--ocr-test <png> [--psm-sparse|--psm-line]` — single image OCR test.
- `--probe <pdf>` — PdfPig block probe with layout metadata.
- `--reconstruct <pdf> --out <dir> [--dpi] [--pages|--start/--end]` — produce the
  per-page line transcripts a manifest is written against.
- `--reconstruct-apply <dumpDir> <overrides.json> --out <sections.json>` — assemble
  sections from a manifest (the same logic `ReconstructionService.ApplyManifest` uses).

## 6. Media-art extraction

`GameArt` holds whatever the base parser's `ExtractImages` pulled out of a PDF —
for scanned books that is a full-page image per page (text and art mixed). When we
only want the illustrations, the **MediaArt CLI** (`backend/DungeonWorld.MediaArt`)
reads the same PDFs and writes **art-only** crops to `Storage/FFArt/<slug>/`:

| File | Role |
| --- | --- |
| `Parsing/MediaArtParser.cs` | Per page: full-page scan → crop art regions; embedded image → export as-is |
| `Parsing/ArtRegionDetector.cs` | Finds the illustration block(s) inside a full-page scan |

Two source kinds are classified by **page coverage** (`image.Bounds` area ÷ page area):

- `coverage ≥ 0.85` — a full-page scan (FF01–FF05). `ArtRegionDetector` locates the
  dense ink blocks and `MediaArtParser.Crop` extracts them; text-only pages yield
  nothing.
- otherwise — an embedded standalone illustration (digital PDFs such as FF16). The
  image is exported whole, re-encoded as a real PNG.

### ArtRegionDetector heuristic

`MediaArtParser.Extract` decodes each image (via `RawBytes` — `TryGetPng`/`TryGetBytes`
are unreliable in the custom PdfPig build) and runs the detector on a downscaled copy
(`AnalysisWidth = 160`) for speed:

1. Per-band ink density: the page is split into `BandHeight = 2`-row bands; a pixel
   counts as dark when luminance `< 100`. Text rows sit at ~1–6% per band while
   illustration blocks run 20–90%.
2. A band is *dense* when density `≥ DenseBandThreshold (0.10)`; adjacent dense bands
   (gaps `≤ MaxGapBands = 2`) merge into vertical runs.
3. A run is art only if it spans `≥ MinArtHeightFraction (0.07)` of the page height
   **and** contains a band `≥ StrongBandThreshold (0.18)` — this rejects bold
   headings and short decorative marks. (These thresholds were tuned against FF01:
   a text page never exceeds ~6% per band, while real illustrations keep long
   contiguous dense runs.)
4. Horizontal extent: within the run, columns with density `≥ ColumnDensityThreshold
   (0.10)` bound the crop; a `MarginPx = 4` frame is added.

The result is `IReadOnlyList<Rectangle>` in source coordinates; crops are drawn into
a fresh 32bpp bitmap (GDI+ `Clone` throws `OutOfMemoryException` on grayscale/ICC
JPEGs) and saved as PNG.
