using System.Text.RegularExpressions;
using DungeonWorld.Core.Entities;
using DungeonWorld.DataCleaner.Model;

namespace DungeonWorld.DataCleaner.Cleaner;

public static class ContentAnalyzer
{
    private static readonly Regex TurnToRe = new(
        @"\bturn\s+to\s+(?:the\s+)?(\d{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GoToRe = new(
        @"\bgo\s+to\s+(?:the\s+)?(\d{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // A choice line ends with "Turn to N" (optionally prefixed by a label).
    private static readonly Regex ChoiceLineRe = new(
        @"^\s*(?<label>.+?)\s*\bturn\s+to\s+(?:the\s+)?(?<n>\d{1,4})\s*[.!]?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

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
        @"(?i)(your\s+(?:adventure|story)\s+ends|you\s+(?:have\s+)?died|your\s+life\s+(?:is\s+)?over|meet\s+your\s+(?:untimely\s+)?end|perish|you\s+are\s+dead\s*[.!]?$|killed\s+you\s*[.!]?$)",
        RegexOptions.Compiled);

    private static readonly Regex VictoryEndRe = new(
        @"(?i)(you\s+have\s+won|you\s+win\s+the|king\s+of\s+the\s+pirates|crowned\s+the\s+new|triumph|victory\s+is\s+yours|you\s+succeed\s*[.!]?$)",
        RegexOptions.Compiled);

    private static readonly Regex CombatNoteRe = new(
        @"(?i)(whenever[^.!?\n]{0,160}\.)|(score\s+a\s+hit[^.!?\n]{0,120}\.)|(hits\s+you\s+during\s+the\s+battle[^.!?\n]{0,120}\.)|(roll\s+one\s+die[^.!?\n]{0,120}\.)",
        RegexOptions.Compiled);

    public static CleanedSection Analyze(Section section)
    {
        var raw = section.Content ?? "";
        var clean = new CleanedSection
        {
            Number = section.SectionNumber,
            ImagePath = string.IsNullOrWhiteSpace(section.ImagePath) ? null : section.ImagePath,
            Raw = raw,
        };

        var features = clean.Features;
        features.MissingText = MissingTextRe.IsMatch(raw);

        var refs = new SortedSet<int>();
        foreach (Match m in TurnToRe.Matches(raw))
        {
            if (int.TryParse(m.Groups[1].Value, out var n)) refs.Add(n);
        }
        foreach (Match m in GoToRe.Matches(raw))
        {
            if (int.TryParse(m.Groups[1].Value, out var n)) refs.Add(n);
        }
        clean.References = refs.ToList();

        foreach (Match m in ChoiceLineRe.Matches(raw))
        {
            if (!int.TryParse(m.Groups["n"].Value, out var target)) continue;
            var label = m.Groups["label"].Value.Trim();
            clean.Choices.Add(new CleanedChoice
            {
                Kind = "choice",
                Label = label.Length > 0 ? label : null,
                Target = target,
                Text = $"Turn to {target}",
            });
        }

        AnalyzeCombat(raw, features);
        features.HasLuckTest = LuckTestRe.IsMatch(raw);

        foreach (Match m in StatChangeRe.Matches(raw))
        {
            var text = Normalize($"{m.Groups[1].Value} {m.Groups["n"].Value} {m.Groups["stat"].Value}".ToUpperInvariant());
            if (!features.StatChanges.Contains(text)) features.StatChanges.Add(text);
        }

        features.LogDays = ParseLogDays(raw);

        foreach (Match m in BootyRe.Matches(raw))
        {
            var text = Normalize($"{m.Groups[1].Value} {m.Groups["n"].Value} {m.Groups[3].Value}");
            if (!features.Booty.Contains(text)) features.Booty.Add(text);
        }

        foreach (Match m in DiceRollRe.Matches(raw))
        {
            var sentence = FindSentence(raw, m.Index);
            if (sentence != null && !features.DiceInstructions.Contains(sentence)) features.DiceInstructions.Add(sentence);
            if (features.DiceInstructions.Count >= 3) break;
        }

        foreach (Match m in ItemMentionRe.Matches(raw))
        {
            var text = Normalize(m.Groups[1].Value);
            if (!features.ItemMentions.Contains(text)) features.ItemMentions.Add(text);
            if (features.ItemMentions.Count >= 3) break;
        }

        var hasOutgoing = clean.References.Count > 0 || clean.Choices.Count > 0;
        features.IsEnd = hasOutgoing == false && !features.MissingText;
        if (features.IsEnd)
        {
            features.DeathEnd = DeathEndRe.IsMatch(raw);
            features.VictoryEnd = VictoryEndRe.IsMatch(raw);
        }

        var note = CombatNoteRe.Match(raw);
        if (note.Success) features.CombatNote = Normalize(note.Value);

        clean.Clean = StripChoiceLines(raw);
        return clean;
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
        return Normalize(ChoiceLineRe.Replace(raw, ""));
    }

    public static string Normalize(string s)
    {
        return Regex.Replace(s.Trim(), @"\s+", " ");
    }
}
