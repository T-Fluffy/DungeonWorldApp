using System.Text.RegularExpressions;

namespace DungeonWorld.Core.Text;

/// <summary>
/// Last-pass repair for OCR typography that survives the exit repair: <c>y</c> read as
/// <c>v</c>/<c>r</c>, digits read for letters, and stat labels whose letter case was mangled
/// by the scan. Applied to the readable body text only.
///
/// <para><b>Safety.</b> Every rule is whole-token and none of them rewrites a digit that is
/// part of a number, so a section's exits can never move: the graph goldens are unaffected by
/// construction. Text that is already correct is left byte-for-byte alone.</para>
/// </summary>
public static class OcrTypography
{
    // Whole tokens whose OCR reading is certain in this scan family. Kept deliberately short:
    // a wrong "fix" here would corrupt a book silently, so only forms seen repeatedly across
    // several books are listed.
    private static readonly Dictionary<string, string> WordFixes = new(StringComparer.Ordinal)
    {
        ["vou"] = "you",     // "vou grasp the rope"
        ["fou"] = "you",     // "fou have failed"
        ["rour"] = "your",   // "draw rour sword"
        ["vour"] = "your",   // "yoor" is the same misread with v/w
        ["yoor"] = "your",
        ["yout"] = "your",   // "on yout journey"
        ["wonld"] = "would",
        ["tum"] = "turn",    // "tum to", "tum and flee"
        ["furn"] = "turn",
        ["1s"] = "is",
        ["1t"] = "it",
        ["i5"] = "is",
        ["1he"] = "the",
        ["s00n"] = "soon",
        ["Tesi"] = "Test",
        ["sk1LL"] = "SKILL",
        ["sx1LL"] = "SKILL",
        ["sxiLL"] = "SKILL",
        ["sxILL"] = "SKILL",
    };

    // Stat labels the scan regularly re-cases ("sTAMINA", "skiLL", "LUck"). Only fires when the
    // token is visibly case-corrupted (two or more capital letters), so ordinary prose such as
    // "Skill" at a sentence start or the verb "speed" is never touched.
    private static readonly Dictionary<string, string> StatLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["stamina"] = "STAMINA",
        ["skill"] = "SKILL",
        ["luck"] = "LUCK",
    };

    private static readonly Regex TokenRegex = new(@"[A-Za-z0-9]+", RegexOptions.Compiled);

    /// <summary>Repairs the body text; safe to run twice.</summary>
    public static string Repair(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;

        return TokenRegex.Replace(text, m =>
        {
            var token = m.Value;
            if (WordFixes.TryGetValue(token, out var fixedWord))
                return fixedWord;

            if (token is "1f")
                return IsSentenceStart(text, m.Index) ? "If" : "if";

            if (StatLabels.TryGetValue(token, out var label) && UppercaseCount(token) >= 2)
                return label;

            return token;
        });
    }

    private static bool IsSentenceStart(string text, int index)
    {
        for (int i = index - 1; i >= 0; i--)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c)) continue;
            return c is '.' or '!' or '?' or '"' or '\u201c' or '\u201d';
        }
        return true;
    }

    private static int UppercaseCount(string token)
    {
        int count = 0;
        foreach (char c in token)
            if (char.IsUpper(c)) count++;
        return count;
    }
}
