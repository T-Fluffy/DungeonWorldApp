"""Golden refs generator for the BookGraphGoldenTests gate.

Writes backend/DungeonWorld.Tests/GoldenGraphs/<slug>.refs.json from a cleaned
book in backend/Storage/Books/CleanedData/. The golden is an embedded resource
that pins every section's outgoing reference set, so a re-parse that silently
loses an edge fails the build.

    python tools/make_golden.py "FF21 Trial of Champions"      # by cleaned name
    python tools/make_golden.py --all                          # every cleaned book
    python tools/make_golden.py "FF21 Trial of Champions" --out path/custom.refs.json

The slug is the cleaned file name lower-cased with runs of non-alphanumerics
collapsed to a single underscore ("FF21 Trial of Champions" -> ff21_trial_of_champions),
matching the committed FF10..FF20 goldens. Regenerate only after an intentional
graph change and confirm tools/verify_books.py still reports ALL CLEAN.
"""
from __future__ import annotations

import argparse
import glob
import json
import os
import re
import sys


def repo_root() -> str:
    d = os.path.abspath(os.path.dirname(__file__))
    while True:
        if os.path.isdir(os.path.join(d, "backend", "Storage", "Books", "CleanedData")):
            return d
        parent = os.path.dirname(d)
        if parent == d:
            raise SystemExit("Run from inside the repo (CleanedData not found).")
        d = parent


def slugify(name: str) -> str:
    return re.sub(r"[^0-9a-z]+", "_", name.lower()).strip("_")


def build_golden(cleaned_path: str) -> dict:
    data = json.load(open(cleaned_path, encoding="utf-8"))
    sections = data["Sections"]
    by_number = {s["Number"]: sorted(s["References"]) for s in sections}
    max_section = max(by_number)
    return {
        "file": os.path.basename(cleaned_path),
        "maxSection": max_section,
        "sections": {str(n): by_number[n] for n in range(1, max_section + 1)},
    }


def write_golden(cleaned_path: str, out_path: str) -> None:
    golden = build_golden(cleaned_path)
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with open(out_path, "w", encoding="utf-8", newline="") as f:
        json.dump(golden, f, indent=1)
        f.write("\n")
    edges = sum(len(v) for v in golden["sections"].values())
    print(f"{os.path.basename(cleaned_path)} -> {out_path} "
          f"({len(golden['sections'])} sections, {edges} edges)")


def main() -> None:
    ap = argparse.ArgumentParser(description="Generate BookGraphGoldenTests refs snapshots.")
    ap.add_argument("book", nargs="?", help="cleaned file name or path (without extension ok)")
    ap.add_argument("--all", action="store_true", help="regenerate every cleaned book")
    ap.add_argument("--out", help="explicit output path (single book only)")
    args = ap.parse_args()

    root = repo_root()
    cleaned_dir = os.path.join(root, "backend", "Storage", "Books", "CleanedData")
    golden_dir = os.path.join(root, "backend", "DungeonWorld.Tests", "GoldenGraphs")

    if args.all:
        if args.out:
            raise SystemExit("--out is only valid for a single book.")
        files = sorted(glob.glob(os.path.join(cleaned_dir, "*.json")))
        if not files:
            raise SystemExit("No cleaned books found.")
        for f in files:
            write_golden(f, os.path.join(golden_dir, f"{slugify(os.path.basename(f)[:-5])}.refs.json"))
        return

    if not args.book:
        ap.error("provide a book name or --all")
    name = os.path.basename(args.book)
    if not name.endswith(".json"):
        name += ".json"
    cleaned_path = args.book if os.path.isabs(args.book) else os.path.join(cleaned_dir, name)
    if not os.path.isfile(cleaned_path):
        raise SystemExit(f"cleaned book not found: {cleaned_path}")
    out_path = args.out or os.path.join(golden_dir, f"{slugify(name[:-5])}.refs.json")
    write_golden(cleaned_path, out_path)


if __name__ == "__main__":
    main()
