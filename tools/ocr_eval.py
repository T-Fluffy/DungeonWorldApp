"""Per-book OCR/graph quality report, including the exit-integrity metrics that
``verify_books.py`` does not surface.

Reads ``backend/Storage/Books/CleanedData/*.json`` and reports, for every book:

  sections           section count, and whether the range is contiguous 1..N
  no_exit            sections with zero usable references (the FF17 failure mode:
                     a garbled "tum to"/"fo" verb leaves no parseable exit)
  dangling           references outside 1..N
  mismatch           sections whose sorted choice targets differ from references
  reachable / unreach  BFS from Graph.EntrySection over references
  deadends/short_deadends
  garbled_exits      "verb + preposition + number" tokens whose verb is not a
                     plain "turn"/"go", i.e. exits the extractor will miss
  avg_len            mean Clean length

Read-only: this script never writes to the repo or to Storage. Run it before
and after a parser change and diff the output to prove nothing regressed.

    python tools/ocr_eval.py            # all books
    python tools/ocr_eval.py FF17       # substring filter
"""
import glob
import json
import os
import re
import sys

# A real exit: an explicit navigation verb followed by a preposition and a number.
# Anything the graph extractor cannot see shows up here as a garbled exit.
EXIT = re.compile(r"\b([A-Za-z]{2,7})\s+(?:to|fo|io|bo|tn|tor|tir|ta|ko|go)\s*[^\d\r\n]{0,3}(\d{1,4})")
GOOD_VERBS = {"turn", "go"}


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


def evaluate(path: str) -> str:
    d = json.load(open(path, encoding="utf-8"))
    secs = d["Sections"]
    by = {s["Number"]: s for s in secs}
    mx = max(by)
    refs = {n: [r for r in (s.get("References") or []) if 1 <= r <= mx] for n, s in by.items()}

    start = (d.get("Graph") or {}).get("EntrySection") or 1
    seen, stack = {start}, [start]
    while stack:
        n = stack.pop()
        for r in refs.get(n, []):
            if r not in seen:
                seen.add(r)
                stack.append(r)

    no_exit = [n for n in sorted(by) if not refs[n]]
    dangling = sorted({r for s in secs for r in (s.get("References") or []) if r < 1 or r > mx})
    mismatch = sum(1 for s in secs
                   if sorted(c["Target"] for c in (s.get("Choices") or [])) != sorted(s.get("References") or []))

    garbled = 0
    for s in secs:
        for m in EXIT.finditer(s.get("Clean") or ""):
            if m.group(1).lower() not in GOOD_VERBS:
                garbled += 1

    dead = (d.get("Graph") or {}).get("DeadEnds") or []
    short_dead = sum(1 for n in dead if n in by and len(by[n].get("Clean") or "") < 150)
    avg = sum(len(s.get("Clean") or "") for s in secs) / len(secs)

    name = os.path.basename(path)[:-len(".json")]
    return (f"{name:<34} sections={len(secs):<4} contig={sorted(by) == list(range(1, mx + 1))!s:<5} "
            f"reachable={len(seen):<4} no_exit={len(no_exit):<4} garbled={garbled:<4} "
            f"dangling={len(dangling):<3} mismatch={mismatch:<3} deadends={len(dead):<4} "
            f"short_dead={short_dead:<4} avg_len={avg:.0f}")


def main() -> None:
    cleaned = find_cleaned_dir()
    if cleaned is None:
        print("CleanedData not found. Run from inside the repo.")
        sys.exit(1)
    needle = sys.argv[1] if len(sys.argv) > 1 else ""
    files = sorted(glob.glob(os.path.join(cleaned, "*.json")))
    if not files:
        print("No cleaned books found.")
        sys.exit(1)
    for f in files:
        if needle and needle.lower() not in os.path.basename(f).lower():
            continue
        print(evaluate(f))


if __name__ == "__main__":
    main()