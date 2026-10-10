using System.Text;
using System.Text.RegularExpressions;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Exit-repair rules for the hard scans (FF13/FF15/FF17) that are deliberately <em>not</em> part of
/// <c>DungeonWorld.Core.Text.TurnToRepair</c>.
///
/// <para><b>Why this is separate.</b> <c>TurnToRepair</c> is invoked by <c>ContentAnalyzer.Analyze</c>,
/// which <c>BookCleaner</c> runs over <em>every</em> book, and by every manifest parser. Widening it
/// with the aggressive verb list these scans need ("tum to", "buen to 148", "l'urn [0 340") would
/// rewrite curated prose in books that are already complete and fully tested. Rules therefore live
/// here, are reached only from parsers that override
/// <c>ManifestDungeonWorldParser.RepairExits</c>, and are range-checked against that book's own
/// section count so a rule valid for FF17 (440) can never resolve to a number FF13 (380) lacks.</para>
///
/// <para><b>Guarantees.</b> The verb list is an explicit allow-list, so ordinary prose such as
/// "go down to 5th Avenue" or "walk in to the shop" is never rewritten. A genuine "burn to" is
/// shielded before the sweep. Only the verb, an intervening filler word and up to three stray
/// non-digit characters are removed; the reference <em>number</em> itself is never altered, because
/// silently changing "turn to 2688" into "turn to 268" would invent a link the book does not contain.
/// Anything still unresolved is left verbatim for later manual review.</para>
/// </summary>
public static class HardScanExitRepair
{
    /// <summary>Garbled verb followed by a mangled preposition: "um to 220", "l'urn [0 340".</summary>
    private static readonly Regex GarbledVerb = new(
        // (?<![A-Za-z]) is essential: without it "urn" would match inside "Turn".
        @"(?<![A-Za-z])[\(\[\{<']*" +
        @"(?:tum|tuen|tuirn|turh|tuwm|twm|tw|thm|urn|um|nrn|nim|bm|bn|rm|om|umn|him|" +
        @"l'urn|iurn|ium|lurn|lun|luen|fum|furn|furm|fern|fem|fen|fun|fur|" +
        @"hum|mum|hem|hurn|hym|ham|hun|han|bum|bom|buen|bem|barn|barm|purn|" +
        @"lum|tom|tin|ten|tim|tumm|lumm|hom|hon|tur|tucning|taen|murm)" +
        @"\s+(?:now\s+|then\s+|also\s+|just\s+)?" +
        // The garbled verb is already known-bad, so a wider preposition set is safe here
        // ("tum in 122" is "turn to 122"; a plain "walk in to the shop" never matches).
        @"(?:to|fo|io|bo|bn|tn|tor|tir|ta|ko|tr|go|fom|fo0|f0|t0|0|rn|ir|tao|too|" +
        @"in|on|at|up|down|over|" +
        @"\[0|\(0|\{0|£0|£o|\[o|\(o|\{o" +
        @")\s*[^\d\r\n]{0,3}(?=\d)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Correct verb, but a mangled preposition and/or an intervening filler the extractor cannot
    /// bridge: "Turn now to 148" and "Turn io 327" both fail the shared extractor's
    /// <c>turn\s*to\s*N</c> pattern purely because of the extra word.
    /// </summary>
    private static readonly Regex GarbledPreposition = new(
        @"(?<![A-Za-z])(?:turn|burn)\s+(?:now\s+|then\s+|also\s+|just\s+)?" +
        @"(?:to|io|ko|tr|fo|ta|tir|tor|tn|bo|id|ld|lo|is|0|\[0|\(0|£0)\s*[^\d\r\n]{0,3}(?=\d)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// A "burn to" whose preposition is itself garbled ("burn tor 335"). A genuine "burn to" is
    /// shielded earlier, so only the corrupted form reaches this rule; it is rewritten to the plain
    /// form the shared repair already understands.
    /// </summary>
    private static readonly Regex GarbledBurn = new(
        @"(?<![A-Za-z])burn\s+(?:tor|tr|ta|tir|tn|t0|torn)\s*[^\d\r\n]{0,3}(?=\d)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>A "turn to" whose target is outside this book.</summary>
    private static readonly Regex OutOfRangeExit = new(
        @"(?<![A-Za-z])turn\s+to\s*[^\d\r\n]{0,3}(\d{1,4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>A real "burn to" exit; shielded so the sweep cannot rewrite it.</summary>
    private static readonly Regex ProtectedBurn = new(@"\bburn\s+to\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const string BurnSentinel = "burnProtection";

    /// <summary>
    /// Repairs garbled exit verbs and prepositions for one book. <paramref name="maxSection"/> bounds
    /// the book, keeping any digit handling inside this book's own section range.
    /// </summary>
    public static string Repair(string content, int maxSection)
    {
        if (string.IsNullOrEmpty(content)) return content ?? string.Empty;

        // Shield genuine "burn to" exits so the verb sweep cannot convert them to "turn to".
        string shielded = ProtectedBurn.Replace(content, BurnSentinel);

        string repaired = GarbledVerb.Replace(shielded, "turn to ");
        repaired = GarbledPreposition.Replace(repaired, Canonical);
        repaired = GarbledBurn.Replace(repaired, "turn to ");
        repaired = NeutralizeOutOfRange(repaired, maxSection);

        return repaired.Replace(BurnSentinel, "burn to");
    }

    /// <summary>
    /// Turns an exit whose target lies outside this book into visible prose instead of an edge.
    /// The scan reads digits like "turn to 2286" where the printed number was shorter; pointing at
    /// section 286 would be a guess, and leaving 2286 in place creates a dangling link that fails the
    /// graph gate. <c>[unclear]</c> keeps the branch visible to a reader and to later manual review
    /// while creating no invented connection.
    /// </summary>
    private static string NeutralizeOutOfRange(string content, int maxSection) =>
        OutOfRangeExit.Replace(content, m =>
        {
            if (!int.TryParse(m.Groups[1].Value, out int n)) return m.Value;
            if (n >= 1 && n <= maxSection) return m.Value;
            return char.IsUpper(m.Value[0]) ? "Turn to [unclear]" : "turn to [unclear]";
        });

    /// <summary>Keeps the original capitalisation of the verb so clean text is left byte-identical.</summary>
    private static string Canonical(Match m) =>
        char.IsUpper(m.Value[0]) ? "Turn to " : "turn to ";

    /// <summary>
    /// Restores a reference that OCR dropped entirely, for a book whose evidence for that specific
    /// target has been confirmed. Applies <em>only</em> when the section has no parseable exit, so it
    /// can never add a second invented branch to a section that already reads correctly.
    /// </summary>
    public static string RestoreOnlyExit(string content, int? target, int maxSection)
    {
        if (target is null) return content;
        if (target < 1 || target > maxSection) return content;
        if (HasParseableExit(content)) return content;
        string trimmed = content.TrimEnd();
        if (trimmed.Length == 0) return content;
        return trimmed + $" (turn to {target}).";
    }

    private static readonly Regex ExitProbe = new(
        @"\b(?:turn|go|burn)\s+(?:to\s+)?\.?\s*(?:the\s+)?\d{1,4}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool HasParseableExit(string content) => ExitProbe.IsMatch(content);
}