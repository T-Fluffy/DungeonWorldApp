using System.Text.RegularExpressions;

namespace DungeonWorld.Core.Text;

/// <summary>
/// Repairs OCR-garbled "turn to N" navigation references shared by the parsing
/// pipeline (parse time) and the cleaning pipeline (clean time).
///
/// Two passes, both idempotent (already-valid "turn to N" is never rewritten):
/// <list type="number">
/// <item>Verb-variant normalization: OCR misreads of "turn"
///   (tum/fum/tumn/turm/furn/lum/turnto/turnin/...) followed by a numeric
///   target, including garbled prepositions ("Tum o 327", "furn te 221",
///   "turn bo b1" verb part), become "turn to N".</item>
/// <item>Digit-confusion repair: unambiguous single-character OCR confusions in
///   the target token (o/O→0, l/I→1, z/Z→2, s/S→5, B→8) are mapped back, but
///   only when every character of the token maps and the result falls inside
///   1..<paramref name="maxSection"/>. Ambiguous confusions (g→6/9,
///   a/e/x) are deliberately left for per-book hand fixes — a wrong guess
///   would plant a false graph edge, worse than a missing one.</item>
/// </list>
/// </summary>
public static partial class TurnToRepair
{
    // Verb misreads observed across FF01-FF05 scans. Bare "turn" is excluded on
    // purpose: "turn to 12" must pass through untouched (idempotency), and
    // narrative "turn north / turn of events" must never match (no digits).
    // The token after the verb may itself be garbled ("tumn to 32z",
    // "fum to 1o") and is repaired by the same digit map.
    [GeneratedRegex(@"\b(turnto|turmn\s*to|turm\s*to|tumn\s*to|tum\s*to|furn\s*to|fum\s*to|lurn\s*to|lum\s*to|hurmn\s*to|turnin\s*to|tuma?\s*to|turn\s*bo|furn\s*te|fum\s*bo|tum\s*o|turmn?\s*o)\s*([A-Za-z0-9]{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TurnVerbVariantRegex();

    // Candidate target token after a (possibly already normalized) "turn to".
    [GeneratedRegex(@"\bturn\s+to\s+(?:the\s+)?([A-Za-z0-9]{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TurnTargetRegex();

    private static readonly Dictionary<char, char> DigitMap = new()
    {
        ['o'] = '0', ['O'] = '0',
        ['l'] = '1', ['I'] = '1',
        ['z'] = '2', ['Z'] = '2',
        ['s'] = '5', ['S'] = '5',
        ['B'] = '8',
    };

    /// <summary>
    /// Applies both repair passes. Safe to run on already-clean text.
    /// </summary>
    public static string RepairContent(string content, int maxSection = 400)
    {
        if (string.IsNullOrEmpty(content)) return content;
        var step1 = TurnVerbVariantRegex().Replace(content, m => RepairVerb(m, maxSection));
        return TurnTargetRegex().Replace(step1, m => RepairTarget(m, maxSection));
    }

    private static string RepairVerb(Match m, int maxSection)
    {
        var token = m.Groups[2].Value;
        var repaired = MapToken(token, maxSection);
        // Unrepairable token (e.g. "turn both 12" misfire): leave verbatim.
        return repaired is null ? m.Value : $"turn to {repaired}";
    }

    /// <summary>Maps a garbled target token to digits; null when not unambiguously repairable.</summary>
    private static string? MapToken(string token, int maxSection)
    {
        if (int.TryParse(token, out var already) && already >= 1 && already <= maxSection)
            return token;
        var mapped = new char[token.Length];
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (c is >= '0' and <= '9') mapped[i] = c;
            else if (DigitMap.TryGetValue(c, out var d)) mapped[i] = d;
            else return null; // ambiguous char (g/a/e/x/...) — leave for hand fix
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
