# Book-by-book completion plan (resume tomorrow)

Strict rule agreed with the user: **complete each book to 400/400 (all data extracted)
before moving to the next.** Only document a ceiling when a page is genuinely unreadable
after recovery attempts.

## Current state (as of 2026-08-14)

| Book | Status |
|------|--------|
| FF01 The Warlock of Firetop Mountain | 400/400 — complete |
| FF02 Citadel of Chaos | **400/400 — complete** |
| FF03 Forest of Doom | 400/400 — complete |
| FF04 Starship Traveller | Not run (aborted) — 3 leftover ProcessedBooks files to clean |
| FF16 Seas of Blood | Reference book — 8 placeholders to fix |

FF02 sources: `C:\Users\Halloul\AppData\Local\Temp\opencode\ff02test\FF02 Citadel of Chaos.pdf`
FF04 sources: `C:\Users\Halloul\AppData\Local\Temp\opencode\pw2up\FF04 Starship Traveller.pdf` (also `testsingle`)
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

## Task 2 — FF16 (Seas of Blood): fix 8 placeholders

Missing: `50, 91, 92, 134, 216, 261, 313, 314`
Same recovery approach as Task 1. It is the protected reference — extra care; verify it
still matches the reference where applicable.

## Task 3 — FF04 cleanup + continue the loop

- Delete leftover `backend/Storage/Books/ProcessedBooks/FF04 Starship Traveller*.json`
  (base, (1), (2), (3)) from the aborted run.
- Run FF04 through the ingestor (per-book parser or merge), verify 400/400 before
  moving on.
- Continue FF05…FF15, FF17…FF63 one book at a time.

## Tools / pipeline notes

- Ingestor multi-dpi per-section merge: `mergeDpis = {200, 250, 300, 400}`, default
  `--dpi 250`, `MergeThreshold = 400` (only skip extra dpis when primary is already 400).
  `MergeBooks` picks per-section best non-placeholder/longest content across passes.
- FF02 needs ~250 dpi (headers best at 250; body reads better at 300+ but headers break).
- Debug modes: `--ocr-dump <page> <pdf>`, `--ocr-test <png> [--psm-sparse|--psm-line]`,
  `--probe <pdf>`, `--ocr-extract <pdf>`.
- The `--ocr-dump` mode is hard-coded to 200 dpi; for other dpis render with pdftoppm +
  crop right half, then `--ocr-test`.