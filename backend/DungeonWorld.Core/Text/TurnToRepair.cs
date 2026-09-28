using System.Text.RegularExpressions;

namespace DungeonWorld.Core.Text;

/// <summary>
/// Repairs OCR-garbled "turn to N" navigation references shared by the parsing
/// pipeline (parse time) and the cleaning pipeline (clean time).
///
/// Three passes, all idempotent (already-valid "turn to N" is never rewritten):
/// <list type="number">
/// <item>Wrapped references are joined first ("turn to\n55.").</item>
/// <item>Verb-variant normalization: OCR misreads of "turn"
///   (tum/fum/tumn/turm/furn/lum/turnto/turnin/...) followed by a numeric
///   target, including garbled prepositions ("Tum o 327", "furn te 221",
///   "turn bo b1" verb part), become "turn to N".</item>
/// <item>Digit-confusion repair on the target token: every character must map
///   through o/O→0, l/I→1, z/Z→2, s/S→5, B/b→8, g/G→9 (or be a digit), with
///   leading junk letters dropped ("x191"→191, "agz"→92), and the result
///   must fall inside 1..<paramref name="maxSection"/>. The g→9 mapping is
///   established by 20+ verified instances across FF01/FF03 (never g→6);
///   remaining confusions (a/e/x/j/q) stay for per-book hand fixes — a wrong
///   guess would plant a false graph edge, worse than a missing one.</item>
/// </list>
/// </summary>
public static partial class TurnToRepair
{
    // Verb misreads observed across FF01-FF05 scans, with an optional garbled
    // preposition (o/te/bo/fo/lo). Bare "turn" is included so "turn o 261" and
    // "turn lo 367" repair; narrative "turn north / turn of events" can never
    // match because the token that follows never maps to a number, and the
    // whole match is then left verbatim. The token itself may be garbled
    // ("tumn to 32z", "fum to 1o"). Missing spaces ("to267"), stray periods
    // ("to.237") and separators ("&", "(") are tolerated. Every alternative is
    // safe by construction: the match is rewritten only when the token maps
    // unambiguously to an in-range number.
    [GeneratedRegex(@"\b(turnto|turmn|turm|tumn|tum|furn|fum|fom|mur|tarn|lurn|lum|hurmn|turnin|tuma?|turn|rn)(?:\s*(to|bo|te|o|lo|fo|b|w))?\s*\.?[&({[]?\s*([A-Za-z0-9]{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TurnVerbVariantRegex();

    // Candidate target token after a (possibly already normalized) "turn to".
    [GeneratedRegex(@"\bturn\s*to\s*\.?\s*(?:the\s+)?([A-Za-z0-9]{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TurnTargetRegex();

    // A "turn to N" wrapped across a line break ("turn to\n55.", also with
    // garbled verbs). Joining first keeps references, choices and labels
    // whole; without it the orphaned number fragment pollutes the next
    // choice's label.
    [GeneratedRegex(@"\b(?:turnto|turmn|turm|tumn|tum|furn|fum|fom|mur|tarn|lurn|lum|hurmn|turnin|tuma?|turn|rn)\s*(?:to|bo|te|o|lo|fo|b|w)?\s*\.?[&({[]?\s*\r?\n\s*(\d{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex WrappedTurnToRegex();

    private static readonly Dictionary<char, char> DigitMap = new()
    {
        ['o'] = '0', ['O'] = '0',
        ['l'] = '1', ['I'] = '1',
        ['z'] = '2', ['Z'] = '2',
        ['s'] = '5', ['S'] = '5',
        ['B'] = '8', ['b'] = '8',
        ['g'] = '9', ['G'] = '9',
    };

    /// <summary>
    /// Applies both repair passes. Safe to run on already-clean text.
    /// </summary>
    public static string RepairContent(string content, int maxSection = 400)
    {
        if (string.IsNullOrEmpty(content)) return content;
        var step0 = WrappedTurnToRegex().Replace(content, "turn to $1");
        var step1 = TurnVerbVariantRegex().Replace(step0, m => RepairVerb(m, maxSection));
        return TurnTargetRegex().Replace(step1, m => RepairTarget(m, maxSection));
    }

    private static string RepairVerb(Match m, int maxSection)
    {
        var token = m.Groups[3].Value;
        var prep = m.Groups[2].Success ? m.Groups[2].Value : "";
        // Bare verb with no preposition and a digit-free token is narrative, not
        // a reference — notably the English plural "turns to ...", where the "s"
        // would otherwise map to 5 ("stare turns to an" -> "turn to 5").
        if (prep.Length == 0 && !token.Any(char.IsDigit)) return m.Value;
        var repaired = MapToken(token, maxSection);
        // Unrepairable token: leave verbatim (e.g. narrative "turn both 12").
        if (repaired is null) return m.Value;
        // Already-canonical "turn to" keeps its original casing/spacing; only
        // the token is spliced.
        var verb = m.Groups[1].Value;
        if (verb.Equals("turn", StringComparison.OrdinalIgnoreCase) &&
            prep.Equals("to", StringComparison.OrdinalIgnoreCase))
        {
            int idx = m.Groups[3].Index - m.Index;
            return m.Value.Remove(idx, token.Length).Insert(idx, repaired);
        }
        return $"turn to {repaired}";
    }

    /// <summary>Maps a garbled target token to digits; null when not unambiguously repairable.</summary>
    private static string? MapToken(string token, int maxSection)
    {
        if (int.TryParse(token, out var already) && already >= 1 && already <= maxSection)
            return token;
        // Strip up to two leading junk letters ("x191"→191, "agz"→92): dirt
        // specks OCR'd as letters in front of the real number. Trailing junk
        // is NOT stripped ("turn to 5x", ordinals like "21st" must not become
        // references). Empty remainders stay unrepaired.
        int start = 0;
        while (start < token.Length && start < 2 && !char.IsAsciiDigit(token[start])
               && !DigitMap.ContainsKey(token[start]))
            start++;
        if (start > 0)
        {
            if (start >= token.Length) return null;
            token = token[start..];
        }
        var mapped = new char[token.Length];
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (c is >= '0' and <= '9') mapped[i] = c;
            else if (DigitMap.TryGetValue(c, out var d)) mapped[i] = d;
            else return null; // ambiguous char (a/e/x/j/...) — leave for hand fix
        }
        var repaired = new string(mapped).TrimStart('0');
        if (repaired.Length == 0) return null;
        return int.TryParse(repaired, out var n) && n >= 1 && n <= maxSection ? repaired : null;
    }

    private static string RepairTarget(Match m, int maxSection)
    {
        var token = m.Groups[1].Value;
        var repaired = MapToken(token, maxSection);
        if (repaired is null || repaired == token) return m.Value;
        {
            // Splice by index (never string.Replace: the token chars may also
            // occur inside "turn to" itself, e.g. "turn to o").
            int idx = m.Groups[1].Index - m.Index;
            return m.Value.Remove(idx, token.Length).Insert(idx, repaired);
        }
    }
}
