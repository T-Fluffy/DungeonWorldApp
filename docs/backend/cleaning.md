# Cleaning pipeline

`DungeonWorld.Cleaning` turns a raw parsed `Book` into the structured `CleanedBook`
document the game API serves. Raw content is always preserved.

```
Book ──▶ BookCleaner.Clean ──▶ CleanedBook
                                ├── Meta      (title, counts, paths, introduction)
                                ├── Rules     (RulesExtractor)
                                ├── Sections  (ContentAnalyzer per section)
                                └── Graph     (GraphAnalyzer)
```

## Model (`Model/CleanedBook.cs`)

| Type | Purpose |
| --- | --- |
| `CleanedBook` | Top-level document: `SchemaVersion` (1), `GeneratedAtUtc`, `Meta`, `Rules`, `Graph`, `Sections` |
| `CleanedMeta` | Title, author, section counts, combat/enemy counts, map/adventure-sheet paths, introduction |
| `CleanedRule` | A stat's roll formula, e.g. `SKILL = 1d6+6` |
| `CleanedGraph` | Outgoing/incoming links per section, dead ends, terminal, unreachable, orphan links, max depth |
| `CleanedSection` | One section: `Number`, `ImagePath`, `Raw` (verbatim), `Clean` (choices stripped), `Choices`, `References`, `Features` |
| `CleanedChoice` | A navigable option: `Kind`, `Label`, `Target`, `Text`, `IsDiceRoll` |
| `CleanedFeatures` | Combat flags, enemies, luck tests, stat changes, booty, dice instructions, item mentions, end/death/victory |
| `CleanedEnemy` | Enemy with SKILL/STAMINA (or CREW STRIKE/STRENGTH) |

## `BookCleaner.Clean(book, sourceFile)`

1. Builds `Meta` from the raw book.
2. `RulesExtractor.Extract(book.Introduction)` — regexes that find the character-creation
   instructions ("Roll one die. Add 6 to the result. … your SKILL score") and derive
   formulas like `1d6+6`, plus LOG (time limit) and BOOTY (starting gold) rules.
3. Maps every section through `ContentAnalyzer.Analyze`.
4. Counts missing sections, combat sections, enemies.
5. `GraphAnalyzer.Build(cleaned)`.

## `ContentAnalyzer.Analyze(section)`

- **References** — every `turn to N` / `go to N` number in the raw text (sorted set).
- **Choices** — `ChoiceLineRe` matches a line that is a label followed by `turn to N`
  on the same line. A "continuation tail" (single lowercase label that is really the
  end of a narrative sentence pushed onto its own line) is kept as narrative and only
  the redundant "turn to N" is dropped (`IsContinuationLine`).
- **Combat** — `AnalyzeCombat` normalizes plural stat words (SKILLS→SKILL, …), detects
  individual (`SKILL`+`STAMINA`) and large-scale (`STRIKE`+`STRENGTH`) encounter boxes,
  and parses enemies in inline / header+rows / bare-stats formats.
- **Features** — luck tests, stat changes, LOG days, booty, dice instructions, item
  mentions, missing-text flag, and end/death/victory classification.
- **Clean** — the raw text with choice lines stripped (`StripChoiceLines`).

## `GraphAnalyzer.Build(cleaned)`

- Builds `Outgoing` (per section, from `References`) and `Incoming` maps; duplicate
  section numbers are tolerated by merging their references.
- `DeadEnds` = present sections with no references and no missing text.
- `Terminal` = dead ends that are explicitly death/victory.
- `OrphanLinks` = references pointing outside `1..SectionCount`.
- `Unreachable` = present sections not reachable from the entry section (BFS).
- `MaxDepthFromEntry` = longest shortest-path from the entry (BFS, bounded).

## Writers

- `BookCleaner.WriteCleanedBook(cleaned, outputDir)` — writes `{Title}.json`, never
  overwriting (a ` (n)` suffix is added if needed, keeping earlier extractions for
  comparison).

## Consumers

- **API**: `GameController` reads `Storage/Books/CleanedData/<Title>.json` directly.
- **Ingestor CLI**: runs `BookCleaner.Clean` + `WriteCleanedBook` after each parse.
- **DataCleaner CLI**: re-cleanes every file in `ProcessedBooks/` into `CleanedData/`.
