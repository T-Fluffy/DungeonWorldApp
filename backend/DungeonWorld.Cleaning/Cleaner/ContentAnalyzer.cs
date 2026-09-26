using System.Text.RegularExpressions;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Text;
using DungeonWorld.Cleaning.Model;

namespace DungeonWorld.Cleaning.Cleaner;

public static class ContentAnalyzer
{
    private static readonly Regex TurnToRe = new(
        @"\bturn\s*to\s*\.?\s*(?:the\s+)?(\d{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GoToRe = new(
        @"\bgo\s*to\s*\.?\s*(?:the\s+)?(\d{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // A choice line ends with "Turn to N" (optionally prefixed by a label). The label
    // and "turn to N" must sit on the same physical line: allowing whitespace between
    // them to cross newlines would swallow a narrative line that happens to precede a
    // standalone "Turn to N" footer (e.g. "...You stop and listen but can-\n\nnot hear
    // anything.\n\nTurn to 85." -> the "not hear anything" line would be stripped).
    private static readonly Regex ChoiceLineRe = new(
        @"^\s*(?<label>.+?)[ \t]*\bturn\s*to\s*\.?\s*(?:the\s+)?(?<n>\d{1,4})\s*[.!]?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

    // A bare "Turn to N" footer with no label (single-exit sections). ChoiceLineRe
    // requires a label, so these never became choices despite feeding References.
    private static readonly Regex BareExitRe = new(
        @"^\s*turn\s*to\s*\.?\s*(?:the\s+)?(?<n>\d{1,4})\s*[.!]?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

    // Parenthesised fragments inside mid-line choice pairs, stripped from labels.
    private static readonly Regex InlineRefRe = new(
        @"\(?\b(?:turn|go)\s+to\s+(?:the\s+)?\d{1,4}\)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Printed page footers ("310-311") leak into section tails during parsing.
    // A trailing bare page range is never narrative; refs are extracted before
    // this runs, so no navigation data is affected.
    private static readonly Regex PageRangeFooterRe = new(
        @"\s*\d{1,3}\s*[-–]\s*\d{1,3}\s*$",
        RegexOptions.Compiled);

    // Back-cover order-form bleed ("Send check or money order ... City/State
    // Zip") occasionally lands inside the last section during parsing. These
    // multi-word markers never occur in genuine narrative; cut Clean there.
    // Raw stays verbatim.
    private static readonly string[] AdvertMarkers =
    [
        "Send check or money order",
        "postage and handling",
        "City/State Zip",
        "Please allow 3-4 weeks for shipment",
    ];

    private static readonly Regex LuckTestRe = new(
        @"\btest\s+your\s+luck\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StatChangeRe = new(
        @"(?i)(lose|gain|add|deduct|restore|increase|decrease|take)\s+(?<n>\d+)\s+(?:points?\s+)?(?:of\s+|from\s+|in\s+)?(?<stat>SKILL|STAMINA|LUCK|CREW\s+(?:STRIKE|STRENGTH))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LogDaysRe = new(
        @"(?i)(add|gain|increase|deduct|lose|subtract)\s+(?<n>\d+)\s+day[s]?\s+(?:to|onto|into|from)?\s*(?:your\s+)?LOG",
        RegexOptions.Compiled);

    private static readonly Regex BootyRe = new(
        @"(?i)(add|gain|receive|find|deduct|lose|spend)\s+(?<n>\d+|one|two|three|four|five)\s+(gold\s+pieces?|gp[s]?|pieces?\s+of\s+gold|slaves?)",
        RegexOptions.Compiled);

    private static readonly Regex DiceRollRe = new(
        @"(?i)roll\s+(?:one\s+die|a\s+die|two\s+dice|\d+\s+dice|the\s+dice)",
        RegexOptions.Compiled);

    private static readonly Regex SentenceRe = new(
        @"(?i)[^.!?\n]+[.!?]?",
        RegexOptions.Compiled);

    private static readonly Regex ItemMentionRe = new(
        @"(?i)\b(you\s+(?:find|discover|obtain|receive|acquire)\s+(?:a\s+|an\s+|the\s+)?([a-z][a-z '.-]{2,20}))",
        RegexOptions.Compiled);

    private static readonly Regex MissingTextRe = new(
        @"text\s+missing\s+or\s+unreadable",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DeathEndRe = new(
        @"(?i)(your\s+(?:adventure|story)\s+ends|you\s+(?:have\s+)?died|your\s+life\s+(?:is\s+)?over|meet\s+your\s+(?:untimely\s+)?end|perish|you\s+are\s+dead\s*[.!]?$|killed\s+you\s*[.!]?$|your\s+adventure\s+is\s+(over|at\s+an\s+end)|(this\s+is\s+)?the\s+end\s+of\s+your\s+adventure|bringing\s+your\s+adventure\s+to\s+[^.!?]{0,40}\bend\b|cannot\s+continue\s+your\s+adventure)",
        RegexOptions.Compiled);

    private static readonly Regex VictoryEndRe = new(
        @"(?i)(you\s+have\s+won|you\s+win\s+the|king\s+of\s+the\s+pirates|crowned\s+the\s+new|triumph(?!antly)|victory\s+is\s+yours|you\s+succeed\s*[.!]?$)",
        RegexOptions.Compiled);

    private static readonly Regex CombatNoteRe = new(
        @"(?i)(whenever[^.!?\n]{0,160}\.)|(score\s+a\s+hit[^.!?\n]{0,120}\.)|(hits\s+you\s+during\s+the\s+battle[^.!?\n]{0,120}\.)|(roll\s+one\s+die[^.!?\n]{0,120}\.)",
        RegexOptions.Compiled);

    public static CleanedSection Analyze(Section section, int maxSection = 400)
    {
        var raw = section.Content ?? "";
        // Repair OCR-garbled turn references once; Raw stays verbatim while
        // references, choices and the displayed Clean text derive from this copy.
        var text = TurnToRepair.RepairContent(raw, maxSection);
        var clean = new CleanedSection
        {
            Number = section.SectionNumber,
            ImagePath = string.IsNullOrWhiteSpace(section.ImagePath) ? null : section.ImagePath,
            Raw = raw,
        };

        var features = clean.Features;
        features.MissingText = MissingTextRe.IsMatch(raw);

        var refs = new SortedSet<int>();
        foreach (Match m in TurnToRe.Matches(text))
        {
            if (int.TryParse(m.Groups[1].Value, out var n)) refs.Add(n);
        }
        foreach (Match m in GoToRe.Matches(text))
        {
            if (int.TryParse(m.Groups[1].Value, out var n)) refs.Add(n);
        }
        clean.References = refs.ToList();

        ExtractChoices(text, clean);

        AnalyzeCombat(text, features);
        features.HasLuckTest = LuckTestRe.IsMatch(text);

        foreach (Match m in StatChangeRe.Matches(text))
        {
            var entry = Normalize($"{m.Groups[1].Value} {m.Groups["n"].Value} {m.Groups["stat"].Value}".ToUpperInvariant());
            if (!features.StatChanges.Contains(entry)) features.StatChanges.Add(entry);
        }

        features.LogDays = ParseLogDays(raw);

        foreach (Match m in BootyRe.Matches(text))
        {
            var entry = Normalize($"{m.Groups[1].Value} {m.Groups["n"].Value} {m.Groups[3].Value}");
            if (!features.Booty.Contains(entry)) features.Booty.Add(entry);
        }

        foreach (Match m in DiceRollRe.Matches(text))
        {
            var sentence = FindSentence(text, m.Index);
            if (sentence != null && !features.DiceInstructions.Contains(sentence)) features.DiceInstructions.Add(sentence);
            if (features.DiceInstructions.Count >= 3) break;
        }

        foreach (Match m in ItemMentionRe.Matches(text))
        {
            var entry = Normalize(m.Groups[1].Value);
            if (!features.ItemMentions.Contains(entry)) features.ItemMentions.Add(entry);
            if (features.ItemMentions.Count >= 3) break;
        }

        var hasOutgoing = clean.References.Count > 0 || clean.Choices.Count > 0;
        features.IsEnd = hasOutgoing == false && !features.MissingText;
        if (features.IsEnd)
        {
            features.DeathEnd = DeathEndRe.IsMatch(text);
            features.VictoryEnd = VictoryEndRe.IsMatch(text);
        }

        var note = CombatNoteRe.Match(text);
        if (note.Success) features.CombatNote = Normalize(note.Value);

        clean.Clean = StripAdverts(PageRangeFooterRe.Replace(StripChoiceLines(text), ""));
        return clean;
    }

    private static string StripAdverts(string clean)
    {
        foreach (var marker in AdvertMarkers)
        {
            int i = clean.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (i >= 0) return Normalize(clean[..i]);
        }
        return clean;
    }

    /// <summary>
    /// Builds the choice list toward parity with <see cref="CleanedSection.References"/>:
    /// (a) labeled end-of-line choices, (b) bare "Turn to N" footers with no label,
    /// (c) mid-line / parenthesised references not already covered. Targets are
    /// deduplicated (first occurrence wins) since buttons must be unique.
    /// </summary>
    private static void ExtractChoices(string text, CleanedSection clean)
    {
        var lineStarts = LineStarts(text);
        var covered = new HashSet<(int Line, int Target)>();
        var added = new HashSet<int>();

        void Add(int line, int target, string? label)
        {
            if (!added.Add(target)) return;
            covered.Add((line, target));
            clean.Choices.Add(new CleanedChoice
            {
                Kind = "choice",
                Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim(),
                Target = target,
                Text = $"Turn to {target}",
            });
        }

        foreach (Match m in ChoiceLineRe.Matches(text))
        {
            if (!int.TryParse(m.Groups["n"].Value, out var target)) continue;
            // Line of the target number itself: the match may start on a preceding
            // blank line when ^\s* consumes it, which would poison coverage.
            int line = LineIndex(lineStarts, m.Groups["n"].Index);
            var label = m.Groups["label"].Value.Trim();
            // A vetoed continuation tail is narrative, not an option — but it still
            // covers the reference so the mid-line pass below must not resurrect it.
            covered.Add((line, target));
            if (label.Length > 0 && IsContinuationLine(text, m.Groups["label"].Index)) continue;
            Add(line, target, label.Length > 0 ? label : null);
        }

        foreach (Match m in BareExitRe.Matches(text))
        {
            if (!int.TryParse(m.Groups["n"].Value, out var target)) continue;
            Add(LineIndex(lineStarts, m.Groups["n"].Index), target, null);
        }

        foreach (var re in new[] { TurnToRe, GoToRe })
        {
            foreach (Match m in re.Matches(text))
            {
                if (!int.TryParse(m.Groups[1].Value, out var target)) continue;
                int line = LineIndex(lineStarts, m.Index);
                if (covered.Contains((line, target)) || added.Contains(target)) continue;
                Add(line, target, MidLineLabel(text, lineStarts, line, m.Index));
            }
        }
    }

    private static List<int> LineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '\n') starts.Add(i + 1);
        return starts;
    }

    private static int LineIndex(List<int> lineStarts, int index)
    {
        int i = lineStarts.BinarySearch(index);
        return i >= 0 ? i : ~i - 1;
    }

    /// <summary>
    /// Derives a button label from the text preceding a mid-line reference:
    /// strips other inline "(turn to N)" fragments and surrounding punctuation;
    /// null when nothing meaningful remains or the prefix is narrative-length.
    /// </summary>
    private static string? MidLineLabel(string text, List<int> lineStarts, int line, int matchIndex)
    {
        int lineStart = lineStarts[line];
        var prefix = text[lineStart..matchIndex];
        prefix = InlineRefRe.Replace(prefix, " ");
        prefix = prefix.Trim().Trim('(', ')', ',', ';', ':', '.', '!', '?', '-', '–', '—', '"', '\'', '“', '”');
        if (prefix.StartsWith("or ", StringComparison.OrdinalIgnoreCase))
            prefix = prefix[3..].TrimStart();
        if (prefix.Length == 0 || prefix.Length > 150) return null;
        return prefix;
    }

    private static void AnalyzeCombat(string raw, CleanedFeatures features)
    {
        var normalized = NormalizeCombatText(raw);
        var lines = normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var largeScale = lines.Any(l => l.Contains("STRIKE") && l.Contains("STRENGTH"));
        var individual = lines.Any(l => l.Contains("SKILL") && l.Contains("STAMINA"));
        if (!largeScale && !individual) return;

        features.HasCombat = true;
        features.LargeScaleCombat = largeScale;

        var enemies = new List<CleanedEnemy>();
        if (individual) ParseEnemyLines(lines, "SKILL", "STAMINA", crew: false, enemies);
        if (largeScale) ParseEnemyLines(lines, "STRIKE", "STRENGTH", crew: true, enemies);
        features.Enemies = enemies;
    }

    // Port of frontend/src/utils/combat.ts heuristics for SKILL/STAMINA and STRIKE/STRENGTH blocks.
    private static void ParseEnemyLines(List<string> lines, string attackWord, string hpWord, bool crew, List<CleanedEnemy> output)
    {
        var inlineRe = new Regex(
            $@"^(?<name>[A-Za-z0-9][A-Za-z0-9' .\-]{{1,50}}?)\s+{attackWord}\s*(?<attack>\d{{1,2}})\s*{hpWord}\s*(?<hp>\d{{1,3}})(?=[.,;!?]|\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var headerRe = new Regex(
            $@"^(?:(?<name>[A-Za-z0-9][A-Za-z0-9' .\-]{{1,40}})\s+)?{attackWord}\s+{hpWord}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var rowRe = new Regex(
            $@"^(?<name>[A-Za-z0-9][A-Za-z0-9' .\-]{{1,50}})\s+(?<attack>\d{{1,2}})\s+(?<hp>\d{{1,3}})$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var bareStatsRe = new Regex(
            $@"^{attackWord}\s*(?<attack>\d{{1,2}})\s*{hpWord}\s*(?<hp>\d{{1,3}})$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            var inline = inlineRe.Match(line);
            if (inline.Success)
            {
                output.Add(ToEnemy(inline.Groups["name"].Value.Trim(), inline, crew));
                continue;
            }

            var header = headerRe.Match(line);
            if (header.Success)
            {
                if (header.Groups["name"].Success && header.Groups["name"].Value.Trim().Length > 0)
                {
                    output.Add(ToEnemy(header.Groups["name"].Value.Trim(), header, crew, fallbackAttack: 0, fallbackHp: 0));
                }
                var j = i + 1;
                while (j < lines.Count)
                {
                    var rowInline = inlineRe.Match(lines[j]);
                    if (rowInline.Success)
                    {
                        output.Add(ToEnemy(rowInline.Groups["name"].Value.Trim(), rowInline, crew));
                        j++;
                        continue;
                    }
                    var row = rowRe.Match(lines[j]);
                    if (row.Success)
                    {
                        output.Add(ToEnemy(row.Groups["name"].Value.Trim(), row, crew));
                        j++;
                        continue;
                    }
                    break;
                }
                continue;
            }

            var bare = bareStatsRe.Match(line);
            if (bare.Success && i > 0)
            {
                var prev = lines[i - 1].TrimStart('-', '•', '*', ' ').Trim();
                if (prev.Length >= 2 && !Regex.IsMatch(prev, attackWord + "|" + hpWord, RegexOptions.IgnoreCase))
                {
                    output.Add(new CleanedEnemy
                    {
                        Name = prev,
                        Skill = int.Parse(bare.Groups["attack"].Value),
                        Stamina = int.Parse(bare.Groups["hp"].Value),
                        Crew = crew,
                    });
                }
            }
        }
    }

    private static CleanedEnemy ToEnemy(string name, Match m, bool crew, int fallbackAttack = 0, int fallbackHp = 0)
    {
        var hasStats = m.Groups["attack"].Success && m.Groups["hp"].Success;
        return new CleanedEnemy
        {
            Name = name,
            Skill = hasStats ? int.Parse(m.Groups["attack"].Value) : fallbackAttack,
            Stamina = hasStats ? int.Parse(m.Groups["hp"].Value) : fallbackHp,
            Crew = crew,
            HasStats = hasStats,
        };
    }

    private static string NormalizeCombatText(string raw)
    {
        return raw
            .Replace("SKILLS", "SKILL")
            .Replace("STAMINAS", "STAMINA")
            .Replace("STRIKES", "STRIKE")
            .Replace("STRENGTHS", "STRENGTH");
    }

    private static int? ParseLogDays(string raw)
    {
        var total = 0;
        var found = false;
        foreach (Match m in LogDaysRe.Matches(raw))
        {
            if (!int.TryParse(m.Groups["n"].Value, out var n)) continue;
            var verb = m.Groups[1].Value.ToLowerInvariant();
            var sign = verb is "add" or "gain" or "increase" ? +1 : -1;
            total += sign * n;
            found = true;
        }
        return found ? total : null;
    }

    private static string? FindSentence(string raw, int index)
    {
        foreach (Match m in SentenceRe.Matches(raw))
        {
            if (m.Index <= index && index < m.Index + m.Length)
            {
                return Normalize(m.Value);
            }
        }
        return null;
    }

    private static string StripChoiceLines(string raw)
    {
        var lines = raw.Split('\n');
        var kept = new List<string>();
        int offset = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            var m = ChoiceLineRe.Match(lines[i]);
            if (m.Success)
            {
                // A continuation tail (a single-choice footer whose label starts
                // lowercase and follows an unpunctuated line) is part of the narrative
                // sentence, not an option: keep the fragment and drop only the
                // redundant "turn to N" tail.
                if (m.Groups["label"].Value.Length > 0 && IsContinuationLine(raw, offset + m.Groups["label"].Index))
                    kept.Add(m.Groups["label"].Value);
            }
            // Bare "Turn to N" footers are navigation, not narrative: drop them.
            // (Single-line match only — narrative lines are never swallowed.)
            else if (!BareExitRe.IsMatch(lines[i]))
            {
                kept.Add(lines[i]);
            }
            offset += lines[i].Length + 1;
        }
        return Normalize(string.Join("\n", kept));
    }

    /// <summary>
    /// True when the matched choice line is really the tail of a narrative sentence
    /// that OCR pushed onto its own line, rather than a wrapped option label. Requires
    /// the section to contain exactly ONE "Turn to N" target and exactly one choice
    /// line, the label to start lowercase and not end with a comma/semicolon/question
    /// mark (conditional options such as "To the door," / "If you are Unlucky,"
    /// / "only on stars?" stay choices), the line to be preceded by a blank line (the
    /// previous line holds no content, robust to \n and \r\n), and the previous
    /// non-blank line to not end a sentence.
    /// </summary>
    private static bool IsContinuationLine(string raw, int labelStart)
    {
        int lineStart = labelStart > 0 ? raw.LastIndexOf('\n', labelStart) + 1 : 0;
        int lineEnd = raw.IndexOf('\n', labelStart);
        if (lineEnd < lineStart) lineEnd = raw.Length;

        var line = raw[lineStart..lineEnd];
        var m = ChoiceLineRe.Match(line);
        if (!m.Success) return false;
        var label = m.Groups["label"].Value;
        if (label.Length == 0 || !char.IsLower(label[0])) return false;
        if (label[^1] is ',' or ';' or '?') return false;

        if (TurnToRe.Matches(raw).Count != 1) return false;
        if (ChoiceLineRe.Matches(raw).Count != 1) return false;

        if (lineStart == 0) return false;
        int prevEnd = lineStart - 1;
        int prevStart = raw.LastIndexOf('\n', prevEnd - 1);
        if (prevStart + 1 > prevEnd) return false;
        string prevLine = raw[(prevStart + 1)..prevEnd];
        if (prevLine.Trim().Length > 0) return false;

        string before = raw[..lineStart];
        var prevLines = before.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
        if (prevLines.Count == 0) return false;
        char last = prevLines[^1][^1];
        return last is not ('.' or '!' or '?' or ':' or ';' or ')' or ']' or '"' or '”' or '’' or '»');
    }

    public static string Normalize(string s)
    {
        return Regex.Replace(s.Trim(), @"\s+", " ");
    }
}
