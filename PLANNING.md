# Book-by-book completion plan (resume tomorrow)

Strict rule agreed with the user: **complete each book to 400/400 (all data extracted)
before moving to the next.** Only document a ceiling when a page is genuinely unreadable
after recovery attempts.

## Current state (as of 2026-08-15)

| Book | Status |
|------|--------|
| FF01 The Warlock of Firetop Mountain | **400/400 — complete (manual reconstruction)** |
| FF02 Citadel of Chaos | **400/400 — complete** |
| FF03 Forest of Doom | 400/400 — complete |
| FF04 Starship Traveller | **343/343 — complete** (physical ceiling: no sections 344–400 exist) |
| FF05 City of Thieves | **400/400 — complete** |
| FF16 Seas of Blood | **400/400 — complete** (reference book) |

FF02 sources: `C:\Users\Halloul\AppData\Local\Temp\opencode\ff02test\FF02 Citadel of Chaos.pdf`
FF01 sources: `backend\Storage\Books\tmp\FF01 The Warlock of Firetop Mountain.pdf`; dump/overrides in `C:\Users\Halloul\AppData\Local\Temp\opencode\ff01reconstruct\`
FF04 sources: `C:\Users\Halloul\AppData\Local\Temp\opencode\pw2up\FF04 Starship Traveller.pdf` (also `testsingle`)
FF05 sources: `C:\Users\Halloul\AppData\Local\Temp\opencode\ff05reconstruct\` (300 dpi dump; also `backend\Storage\Books\tmp\FF05 City of Thieves.pdf`)
FF16: `backend\Storage\Books\Seas of Blood.pdf` (protected: CleanPreviousOutput skips it)

## Task 1 — FF02 Citadel of Chaos: DONE (400/400)

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