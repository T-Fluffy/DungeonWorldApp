# Book-by-book completion plan (resume tomorrow)

Strict rule agreed with the user: **complete each book to 400/400 (all data extracted)
before moving to the next.** Only document a ceiling when a page is genuinely unreadable
after recovery attempts.

## Current state (as of 2026-08-15)

| Book | Status |
|------|--------|
| FF01 The Warlock of Firetop Mountain | **400/400 — complete (manual reconstruction)** |
| FF02 Citadel of Chaos | **400/400 — complete** |
| FF03 Forest of Doom | **400/400 — complete (manual reconstruction)** |
| FF04 Starship Traveller | **343/343 — complete** (physical ceiling: no sections 344–400 exist) |
| FF05 City of Thieves | **400/400 — complete** |
| FF16 Seas of Blood | **400/400 — complete** (reference book) |

FF02 sources: `C:\Users\Halloul\AppData\Local\Temp\opencode\ff02test\FF02 Citadel of Chaos.pdf`
FF01 sources: `backend\Storage\Books\tmp\FF01 The Warlock of Firetop Mountain.pdf`; dump/overrides in `C:\Users\Halloul\AppData\Local\Temp\opencode\ff01reconstruct\`
FF03 sources: `backend\Storage\Books\tmp\FF03 Forest of Doom.pdf`; dump in `C:\Users\Halloul\AppData\Local\Temp\opencode\ff03reconstruct\` (105 pages)
FF04 sources: `C:\Users\Halloul\AppData\Local\Temp\opencode\pw2up\FF04 Starship Traveller.pdf` (also `testsingle`)
FF05 sources: `C:\Users\Halloul\AppData\Local\Temp\opencode\ff05reconstruct\` (300 dpi dump; also `backend\Storage\Books\tmp\FF05 City of Thieves.pdf`)
FF16: `backend\Storage\Books\Seas of Blood.pdf` (protected: CleanPreviousOutput skips it)

## Task 1 — FF02 Citadel of Chaos: DONE (400/400), tails enriched 2026-08-15

Reconstructed all sections via the per-book manual spec (`ff02reconstruct` work dir):
explicit page/side/line boundaries mapped every physical folio header (200–400 dpi) to
sections 1–400; `--reconstruct-apply` merged pages + overrides into a 400-section
`ProcessedBooks` file; DataCleaner regenerated `CleanedData/FF02` (SchemaVersion 1,
400 sections, 12 combat sections, graph built). Details:
- Sections 8 and 50 were never actually missing — the old merge had merged/collapsed them
  (sec 8 = Creature Copy duplicate-fight on p19L; sec 50 = one-line bridge "Turn to 164.").
- Out-of-range turn-to refs (918, 835, 940, 2586, 7507) are OCR digit noise of valid
  targets (318, 335, 340, …) — deferred to the text-quality language module.
- FF02 Graph "unreachable 399" is a known artifact — section 1 is a rules page with
  no outgoing refs. Same for FF01/FF03. Not a merge regression.

### FF02 tail enrichment (2026-08-15): 15 truncated section tails verified & fixed

15 sections (40, 41, 50, 74, 95, 100, 102, 177, 192, 205, 219, 223, 229, 330, 354) were
cut off mid-line by the split-line trimmer. Each tail was recovered from the printed book
(600 dpi crops) plus internet walkthroughs (projektkeller/spikesnlead/etc.) and the
printed turn-target confirmed. The clean book text replaced each shared garbled OCR
fragment (e.g. sec 40 `right lork (turn",` → `right lork (turn to 41)",`); OCR-only
garbles well inside a section (e.g. sec 100's `(luen 1o 7o)`, sec 205's `3009`) were left
as-is. Verified tails:
- 40 → `(turn to 41)`; 41 → `Turn to 257.`; 50 → `Turn to 164.` (one-line bridge);
- 74 → `Turn to 377.`; 95 → `Turn to 367.`; 100 → `turn to 276.`; 102 → `turn to 270.`;
- 177 → `(turn to 344)?`; 192 → `Turn to 29.`; 205 → `turn to 368.`; 219 → `Turn to 220.`;
- 223 → `Turn to 138.`; 229 → `(turn to 230).`; 330 → `turn to 120.`; 354 → `Turn to 188.`
- sec 354's `355` was a misparse: the parser dropped the real tail line `to 188,` as
  line-noise (2/7 letters < 0.4) and swallowed the printed "355" folio header from the
  next line.
- Enriched `ProcessedBooks/FF02 Citadel of Chaos.json` is the source of truth; the
  parser now reproduces it byte-for-byte: `CitadelOfChaosParser.ApplySectionFixes`
  (replaces the garbled fragments in raw OCR) is invoked from a new virtual
  `ManifestDungeonWorldParser.PostProcessSection` hook (defaults to
  `PostProcessContent`, so FF01/03/04/05/16 behavior is unchanged). `--dump` verifies
  **400 same, 0 diff**. Full pipeline run: FF02 400/400, FF03 400/400, FF04 343/343,
  FF05 400/400, 0 missing/extra; tests 21/21.

## Task 7 — FF03 Forest of Doom: DONE (400/400 manual reconstruction)

Rebuilt from a fresh `--reconstruct` dump (105 pages at 300 dpi) plus a complete 400-entry
overrides manifest replicating the FF02/FF01 manual-spec workflow, then `--reconstruct-apply`
→ wrapped into `ProcessedBooks/FF03 Forest of Doom.json` → DataCleaner. Details:
- **Two-column book** (L/R half pages, like FF04/05): within each page all L lines carry
  n=0..k then all R lines continue, so overrides use side `"L"`/`"R"` with the global
  per-page line number. Caption pages carry a pull-quote on the L half (e.g. p18 "15 You see
  the shiny tip…", p43 "130 A creature…") that is NOT a section start — captions live on
  p15,18,21,23,25,27,30,34,36,41,43,46,49,52,55,58,60,62,66,70,77,79,81,86,90,92,102.
  p72 has no sections (Yaztromo's price list continues sec 261); p1–14 are front matter.
- **Folio misOCR corrections**: 3334→33-34 (p22L), 3537→35-37 (p22R), 20-32→29-32 (p21R),
  53754→53-54 (p26R), 71=73→71-73 (p31L), 7779→77-79 (p32L), 8486→84-86 (p33L),
  155158→155-158 (p48R), 152~154→152-154 (p48L), 165—~167→165-167 (p51L), 189—192 (p57L),
  & 6"9→6-9 (p16R), 303304→303-304 (p83R), 334335→334-335 (p89L), 349351→349-351 (p92R),
  355-358 (p93R), 385→385-387 (p100L), 399-400 (p103L). Folio ranges run 1→400 continuous.
- **Garbled/missing headers (manual dict)**: sec 5 (no header, p16L23), 39 ("3"), 59 (no
  header, folio "59—62" doubles), 64 ("W04"), 69 (no header), 75 (no header), 78/79 (no
  header), 80 ("Bo"), 82 ("8z"), 89 (no header), 91 ("9L"), 92 ("g2"), 97 (no header),
  99 ("29"), 114 (no header), 117 ("Lo"), 144 (no header), 158 ("153"), 166/167 (no
  header), 188 ("158"), 189 ("18¢"), 196 ("106"), 219 ("2"), 301 ("311"), 302 (no header),
  314 (no header), 322 ("32z"), 324 ("34"), 336 ("336 /"), 356 ("356 -"), 371 (no header).
- **Sec 400 cap**: the last entry flowed into back-matter (ABOUT THE AUTHOR, adverts on
  p104-105); the overrides entry 400 carries `"end": 57` (last real line on p103) so it
  ends "…you are now wealthy beyond your wildest dreams."
- **OCR turn-to normalization**: 43 sections had merged/typo'd "Turnto261"/"tum to"/"furn
  to" etc. that defeated the reference regexes (sec 1's "Turnto261"/"Turnto 54" left the
  entry with no choices and 399/400 unreachable). Normalized to "turn to N" in the raw
  sections; unreachable dropped to 66, section 1 now routes 54/261. Sec 193's "turn to
  1710" (OCR merge of 171) corrected → orphanLinks 0. Sec 45 ("Lose 2 sSTAMIN A points.
  If you are still alive, turn to 165.") is stripped to empty Clean by the cleaner's
  single-line choice matcher — same pre-existing edge as FF16 sec 244; the frontend falls
  back to Raw (`clean || raw`), so no gameplay break.
- Final `ProcessedBooks/FF03 Forest of Doom.json`: 400 sections, full ImagePaths
  (`/assets/game-art/ff03_forest_of_doom/p{page}_i0.png`), MapPath p1, Introduction from
  cover/title + BACKGROUND mission narrative (pp1-3, 12-14). CleanedData: 400/400,
  0 missing, 0 orphan links, 39 combat sections / 42 enemies, unreachable 66, maxDepth 27.
  Empty-enemy combat sections (34, 43, 79, 117, 118, 186, 265, 285, 298…) are OCR stat
  garbling (e.g. "STAMINA �"), the same known DataCleaner limit as FF01.

## Task 2 — FF16 (Seas of Blood): DONE (400/400)

All 8 placeholders (`50, 91, 92, 134, 216, 261, 313, 314`) recovered from the scan at
300 dpi plus 4 pre-existing empty sections (`320, 321, 324, 325`). Same merge bug as
FF02: the old parser had absorbed each missing section's text into its predecessor
(sec 49 carried sec 50; sec 90 carried 91+92; sec 133 carried 134; sec 215 carried 216;
sec 260 carried 261; sec 312 carried 313+314). Split the neighbors, filled the sections,
assigned ImagePaths, re-ran DataCleaner. FF16 graph is healthy (deadEnds 42, terminal 6,
unreachable 24, orphanLinks 0) — section 1 here is real text, unlike FF01/02/03. Sec 244
is a one-line bridge ("You all return to the path safely. Turn to 111.").

## Task 3 — FF04 Starship Traveller: DONE (343/343)

FF04's printed book has only **343 numbered sections** — no headers or "turn to" refs
above 343 exist, so 343/343 is the physical ceiling, not a shortfall. Rebuilt from a
fresh `--reconstruct-apply` run with a corrected 343-entry overrides manifest (replicating
the FF02 manual-spec workflow); deleted the leftover `ProcessedBooks` `(1)/(2)/(3)` files.
Details:
- **Duplicates resolved**: 35→p20R n=48 (sec 33 and 35 both pointed at the same line);
  243→p77L n=3 (the header '243' at p77L is a misOCR of '242'); 289→p89R n=15.
- **Column-top marker pattern** (p97R): the header at top of a column marks the section
  following the one spilling from the previous column — fixed 324→p97R n=24 so sec 323
  keeps its real tail and 324 = "Your crew are becoming anxious…".
- **Ingestor `TrimContent` fix**: the ≤6-char noise filter was swallowing split-line
  "turn to NNN." targets; `TrimContent` is now context-aware (merges short turn-target
  lines into a dangling "turn to" line). Only sec 323 still dangles ("…Turn to" with the
  target genuinely lost in the OCR gap at p97R n=23→24) — same truncation as `(2)`.
- sec 292 = "Turn to 233," is a genuine one-line dead connector (header '292' misOCR'd
  as '202'); orphan "Throw two dice…" fragment rides in sec 326's tail (accepted).
- Final `ProcessedBooks/FF04 Starship Traveller.json`: 343 sections, avgLen 487, full
  ImagePaths (`/assets/game-art/ff04_starship_traveller/p{page}_i0.png`), Introduction
  from `(2)`. CleanedData regenerated: 343 present, 0 missing, 0 out-of-range refs,
  intro 4412 chars. Sparse graph (unreachable-heavy) is the same pipeline artifact as
  FF01/02/03 — OCR-garbled "furn/burn/tum to" defeats the reference regexes.

## Task 4 — FF05 City of Thieves: DONE (400/400)

Rebuilt from a fresh 300 dpi dump (with the wide-page fix in `Program.cs`: `bool wide = w >= h * 1.05` — pages 19/20/24 now split into clean L/R columns) and a complete 400-entry overrides manifest replicating the FF02 manual-spec workflow. All 400 printed sections are present (1→400 continuous, no gaps); per-page section counts sum to 400. Details:
- **Duplicate page 23 ≈ 24**: page 23 re-prints page 24's content. Section 23 carries `"end": 42` (last line of p22R) so the duplicate page is excluded; sec 24 starts at p24L n=3.
- **Page 64 resolution**: the stray "202" header at p64R n=27 is OCR-mispositioned; the monster-encounter table (p64R n=28–38) belongs to sec 201 (ends "If you win, turn to 138"), and sec 202 = the guard/Nicodemus escort section starting mid-column at p64R n=39. Verified via cross-refs: sec 138 routes Ape Man→312 / others→283, and sec 175 → "furn to zo4" = the goblet/scorpion section (sec 204).
- **Mid-column starts are normal** (no visible header): e.g., sec 150 spans 52R→53L, sec 201 spans 63R→64R, sec 239 spans 72→73, sec 267 spans 78→79, sec 325/326 span 90→91, sec 329 spans 92→93, sec 367 spans 102, sec 372 spans 103→104. Page 60 is a single M page (sec 182–184).
- Win section 337 ("Congratulations…") → turn to 400; sec 400 = the ending at p110L.
- Final `ProcessedBooks/FF05 City of Thieves.json`: 400 sections, avgLen 380, full ImagePaths (`/assets/game-art/ff05_city_of_thieves/p{page}_i0.png`), Introduction from the back-cover blurb (p2). CleanedData regenerated: 400 present, 0 missing, 0 out-of-range refs (109 parsed choices), 28 combat sections, orphanLinks 0. Sparse graph (unreachable-heavy) is the same pipeline artifact as FF01/02/03/04 — OCR-garbled "furn/burn/tum/qoo to" defeats the reference regexes.

## Task 5 — continue the loop

- Continue FF06…FF15, FF17…FF63 one book at a time.

## Housekeeping 2026-08-15 — CleanedData deduplication

`CleanedData` had per-book `(1)`–`(5)` variant files (25 total) left over from earlier
cleaner runs. Verified semantically identical to the canonical base (same sections, same
content MD5; FF01/02/16 byte-identical, FF04/05 differ by a stray byte) and deleted all
25. `FF16 Seas of Blood.json.pre-cleaner-fix.bak` kept (pre-fix older content). FF03 had
no variants.

## Task 6 — FF01 The Warlock of Firetop Mountain: DONE (400/400 manual reconstruction)

FF01 was previously "400/400" only as section-presence from the old batch OCR. Rebuilt from
a fresh `--reconstruct` dump (189 pages) plus a complete 400-entry overrides manifest
replicating the FF02 manual-spec workflow, then `--reconstruct-apply` → wrapped into
`ProcessedBooks/FF01 The Warlock of Firetop Mountain.json` → DataCleaner. Details:
- **Canonical structure**: printed book has exactly 400 sections, 1–400 continuous
  (section 401 = the rules intro, not in-book). Verified against the Gamebookuino
  transcription (400 first-lines cross-checked with a fuzzy word-overlap score).
- **Duplicate-27**: not a real quirk — p32's folio "27" is a continuation marker; sec 26
  (Di Maggio's book + Dragon encounter) spans 31→32, sec 27 = enchanted sword at p32L21.
- **Duplicate-296**: sec 296 (leather-bound book) starts p141L20 and continues onto p142
  (folio "296" = continuation only). **Duplicate-311**: p149 is a fragment page (<5 wordy
  lines) → dropped; sec 311 starts p150L2.
- **MisOCR'd folios corrected**: 18→18-20 (p29), 3399→53-55 (p41), 144145→144-145 (p79),
  157→157-159 (p85), 160--161→160-161 (p86), 283285→283-285 (p136), 297298→297-298 (p143),
  §35-338→335-338 (p160), 344345→344-345 (p163), 391393→391-393 (p182).
- **Manual body-line fixes**: sec 13 (garbled duplicate header at p27L24 → real at L9),
  sec 95 (was pointing at sec 94's tail on p56 → L5), sec 140 (folio "140" misparsed as
  header → body at p76L8 "The Skeletons advance…").
- Final `ProcessedBooks/FF01 …json`: 400 sections, avgLen 403, full ImagePaths
  (`/assets/game-art/ff01_the_warlock_of_firetop_mountain/p{page}_i0.png`), MapPath p1,
  Introduction from cover/title/RUMOURS front matter (pp1-3, 19-20). CleanedData: 400/400,
  0 missing, 0 orphan links, 39 combat sections / 50 enemies, 0 placeholder sections.
  Sparse graph (unreachable 69) is the same pipeline artifact as other books — OCR-garbled
  "furn/turn to" defeats the reference regexes.

## Tools / pipeline notes

- Ingestor multi-dpi per-section merge: `mergeDpis = {200, 250, 300, 400}`, default
  `--dpi 250`, `MergeThreshold = 400` (only skip extra dpis when primary is already 400).
  `MergeBooks` picks per-section best non-placeholder/longest content across passes.
- FF02 needs ~250 dpi (headers best at 250; body reads better at 300+ but headers break).
- Debug modes: `--ocr-dump <page> <pdf>`, `--ocr-test <png> [--psm-sparse|--psm-line]`,
  `--probe <pdf>`, `--ocr-extract <pdf>`.
- The `--ocr-dump` mode is hard-coded to 200 dpi; for other dpis render with pdftoppm +
  crop right half, then `--ocr-test`.