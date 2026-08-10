using System.Text.RegularExpressions;
using DungeonWorld.DataCleaner.Model;

namespace DungeonWorld.DataCleaner.Cleaner;

public static class RulesExtractor
{
    // "Roll one die. Add 6 to the result. Enter this total as your SKILL score."
    private static readonly Regex RollStatRe = new(
        @"roll\s+(?<dice>one\s+die|two\s+dice|three\s+dice)\s*\.\s*add\s+(?<add>\d+)\s+to\s+the\s+result\s*\.\s*[^.]{0,140}\b(?<stat>SKILL|STAMINA|LUCK)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // "...under the CREW STRIKE section..." / "...in the CREW STRENGTH section."
    private static readonly Regex RollCrewRe = new(
        @"roll\s+(?<dice>one\s+die|two\s+dice|three\s+dice)\s*\.\s*add\s+(?<add>\d+)\s+to\s+the\s+result\s*\.\s*[^.]{0,140}\bCREW\s+(?<stat>STRIKE|STRENGTH)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LogRe = new(
        @"\bwithin\s+(?<n>fifty|forty|thirty|twenty|\d+)\s+days?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BootyRe = new(
        @"\bonly\s+(\d+)\s+gold\s+pieces?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static List<CleanedRule> Extract(string? introduction)
    {
        var rules = new List<CleanedRule>();
        if (string.IsNullOrWhiteSpace(introduction)) return rules;

        AddRollRules(rules, RollStatRe, introduction);
        AddRollRules(rules, RollCrewRe, introduction);

        var log = LogRe.Match(introduction);
        if (log.Success)
        {
            var limit = NumberWord(log.Groups["n"].Value);
            rules.Add(new CleanedRule { Stat = "LOG", Formula = limit != null ? $"{limit} days" : "", Description = "LOG records days elapsed; the journey must be completed within the time limit." });
        }

        var booty = BootyRe.Match(introduction);
        if (booty.Success)
        {
            rules.Add(new CleanedRule { Stat = "BOOTY", Formula = $"{booty.Groups[1].Value} Gold Pieces", Description = "Starting Booty (gold and slaves captured along the way)." });
        }

        return rules;
    }

    private static void AddRollRules(List<CleanedRule> rules, Regex regex, string introduction)
    {
        foreach (Match m in regex.Matches(introduction))
        {
            var dice = DiceNumber(m.Groups["dice"].Value);
            var add = int.Parse(m.Groups["add"].Value.Trim());
            var stat = m.Groups["stat"].Value.Trim();
            rules.Add(new CleanedRule
            {
                Stat = stat.StartsWith("STRIKE") || stat.StartsWith("STRENGTH") ? "CREW " + stat : stat,
                Formula = $"{dice}d6+{add}",
            });
        }
    }

    private static int DiceNumber(string dice)
    {
        var lower = dice.ToLowerInvariant();
        if (lower.Contains("one die")) return 1;
        if (lower.Contains("two dice")) return 2;
        if (lower.Contains("three dice")) return 3;
        return 0;
    }

    private static string? NumberWord(string word)
    {
        var lower = word.ToLowerInvariant();
        return lower switch
        {
            "fifty" => "50",
            "forty" => "40",
            "thirty" => "30",
            "twenty" => "20",
            _ => Regex.IsMatch(lower, @"^\d+$") ? lower : null,
        };
    }
}
