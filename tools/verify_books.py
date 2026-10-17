"""Book-graph verifier: per-book reachability / refs / choices report.

Reads backend/Storage/Books/CleanedData/*.json (gitignored book output) and
prints one summary block per book plus ALL CLEAN. Run from anywhere inside the
repo:

    python tools/verify_books.py

Metric definitions (kept stable so runs are comparable across sessions):
- reachable   = sections - len(Graph.Unreachable)
- dangling    = references outside 1..maxSection
- mismatch    = sections whose sorted choice targets != sorted references
- deadends    = len(Graph.DeadEnds); terminal = Graph.Terminal
- short_deadends = dead-end sections with < 150 chars of Clean text
- missing     = sections flagged Features.MissingText
- intro_len   = len(Meta.Introduction); combat/enemies from Meta counters

Body-structure guard (catches collapsed manifests that merge/blank sections):
- empty_raw   = sections with blank Raw body that are NOT flagged MissingText
                (a section the parser silently produced with no text)
- max_body    = longest Raw body in the book
- oversized   = max_body >= MAX_BODY_CHARS (a section that swallowed neighbours)

Exit code is always 0; this script reports, it does not gate. Gating lives in
backend/DungeonWorld.Tests/BookGraphGoldenTests.cs and BookStructureSanityTests.cs.
"""
import json
import glob
import os
import sys

# A genuine gamebook section is well under this; a merged/collapsed manifest
# produces a body an order of magnitude larger. Chosen above every healthy book
# (current healthy max ~3.5k) so only real corruption trips it.
MAX_BODY_CHARS = 5000


def find_cleaned_dir() -> str | None:
    d = os.path.abspath(os.path.dirname(__file__))
    while True:
        for cand in (os.path.join(d, "backend", "Storage", "Books", "CleanedData"),
                     os.path.join(d, "Storage", "Books", "CleanedData")):
            if os.path.isdir(cand):
                return cand
        parent = os.path.dirname(d)
        if parent == d:
            return None
        d = parent


def main() -> None:
    cleaned = find_cleaned_dir()
    if cleaned is None:
        print("CleanedData not found. Run from inside the repo.")
        sys.exit(1)
    files = sorted(glob.glob(os.path.join(cleaned, "*.json")))
    if not files:
        print("No cleaned books found.")
        sys.exit(1)
    structure_issues = []
    for f in files:
        d = json.load(open(f, encoding="utf-8"))
        secs = d["Sections"]
        by = {s["Number"]: s for s in secs}
        un = d["Graph"]["Unreachable"]
        mx = max(by)
        dang = sorted({r for s in secs for r in s["References"] if r < 1 or r > mx})
        mm = sum(1 for s in secs
                 if sorted(c["Target"] for c in s["Choices"]) != sorted(s["References"]))
        dead = d["Graph"].get("DeadEnds", [])
        term = d["Graph"].get("Terminal", [])
        short = sum(1 for n in dead if len(by[n].get("Clean", "")) < 150)
        missing = sum(1 for s in secs if s.get("Features", {}).get("MissingText"))
        empty_raw = sum(1 for s in secs
                        if not (s.get("Raw") or "").strip()
                        and not s.get("Features", {}).get("MissingText"))
        max_body = max((len(s.get("Raw") or "") for s in secs), default=0)
        meta = d.get("Meta", {})
        title = os.path.basename(f)[:-len(".json")]
        oversized = max_body >= MAX_BODY_CHARS
        if empty_raw or oversized:
            structure_issues.append(title)
        print(f"### {title} | sections={len(secs)} range=1-{mx} "
              f"contig={sorted(by) == list(range(1, mx + 1))}")
        print(f"reachable={len(secs) - len(un)} unreachable={len(un)} dangling={dang} "
              f"mismatch={mm} short_deadends={short} deadends={len(dead)} terminal={term} "
              f"combat={meta.get('CombatSectionCount')} enemies={meta.get('EnemyCount')} "
              f"missing={missing} intro_len={len(meta.get('Introduction') or '')}")
        print(f"structure empty_raw={empty_raw} max_body={max_body} oversized={oversized}")
    print("ALL CLEAN")
    if structure_issues:
        print("STRUCTURE ISSUES: " + ", ".join(structure_issues))
    else:
        print("STRUCTURE OK")


if __name__ == "__main__":
    main()
