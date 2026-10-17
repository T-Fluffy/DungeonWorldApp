using System.Linq;
using System.Text.RegularExpressions;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF25 Beneath Nightmare Castle (Peter Darvill-Evans, 1987). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 400 sections across PDF pages 13-131;
/// each PDF page is a two-up spread split into L/R halves, with the final page (131) a
/// single mid-column page). Sections print in ascending numeric order, so bounds run from one
/// section number to the next. Intro uses the cover blurb (2) plus BACKGROUND (12); the
/// curated intro text ships in Intros/ff25.txt.
///
/// <para><b>Layout notes.</b> Folio ranges head most halves ("93-94") with section headers
/// further down; a handful of pages (14, 37, 40) print only one column, so a spanning section
/// flows across the page break. Headers are frequently lost or garbled to two-letter pairs
/// ("Bix" = 60, "Bi" = 88, "8g" = 89, "ga" = 90, "us" = 95, "Jace" = 300), dropped hundreds
/// ("132" = 182, "26g" = 269, "F23" = 323) or dropped tens ("1a" = 10, "2" = 32, "33" = 38);
/// those pins sit on the garbled header or on the body start when the header is absent. S9,
/// S44, S64, S85, S87, S205, S243, S244, S284, S300 and S371 are absent from the scan. Each
/// placement was confirmed against the transcript's folio ranges and, where ambiguous, the
/// rendered PDF page.</para>
/// </summary>
public sealed class BeneathNightmareCastleParser : ManifestDungeonWorldParser
{
    public BeneathNightmareCastleParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "BeneathNightmareCastle";
    protected override string TitleMatch => "Beneath Nightmare Castle";
    protected override string Slug => "ff25_beneath_nightmare_castle";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff25.json";

    /// <summary>Cover blurb (2) plus BACKGROUND (12); the rules pages are not narrative.</summary>
    protected override IReadOnlyList<int> IntroPages => new[] { 2, 12 };

    /// <summary>FF25 runs to 400 sections.</summary>
    private const int MaxSections = 400;
    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Written by Peter Darvill-Evans (print: p2/p4 credits).</summary>
    protected override string Author => "Peter Darvill-Evans";

    /// <summary>FF25 keeps exit-fragment retention (exit numbers land on their own lines).</summary>
    protected override bool KeepExitFragments => true;

    /// <summary>Book-scoped exit repair, applied after the shared TurnToRepair.</summary>
    protected override string RepairExits(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, content);

    /// <summary>
    /// True when <paramref name="content"/> already carries a real exit to <paramref name="target"/>.
    /// The trailing (?!\d) matters: this scan reads stray extra digits, so a substring check would
    /// wrongly conclude the exit is present and skip the restoration.
    /// </summary>
    private static bool HasExit(string content, int target) =>
        Regex.IsMatch(content, $@"\bturn\s+to\s+{target}(?!\d)", RegexOptions.IgnoreCase);

    /// <summary>
    /// Turn-verb variants this scan produces (each seen in exit position before a target that maps
    /// unambiguously to a section number). The list is deliberately curated - narrative words that
    /// happen to sit before a number-looking token ("go to 315", "up to 4", "return to the inn")
    /// are excluded, so blind verb repair cannot plant a false graph edge. The shared
    /// <c>TurnToRepair</c> already covers turn/tum/furn/fum/fom/lum/lurn/rn; this adds the rest.
    /// </summary>
    private static readonly Regex TurnVerbGarble = new(
        @"\b(burning|turning|belurn|retum|return|teturn|furen|furm|fumn|fuen|lurn|tuen|tarm|barn|buen|horn|torn|furn|turn|burn|tum|tom|urn|fom|fum|fun|fur|fut|fam|fem|firm|frm|hum|hom|ham|hn|hur|corm|euen|ium|lum|lun|mam|orn|rn|tam|tien|ton|tun|tur|twn|tym|tw|um|umn|un|wm|wn|bem)\b(?:\s*(to|lo|io|fo|bo|te|ti|tu|ta|li|1o|o|b|w))?\s*\.?[&({[]?\s*([A-Za-z0-9]{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Dictionary<char, char> DigitMap = new()
    {
        ['o'] = '0', ['O'] = '0', ['l'] = '1', ['I'] = '1', ['z'] = '2', ['Z'] = '2',
        ['s'] = '5', ['S'] = '5', ['B'] = '8', ['b'] = '8', ['g'] = '9', ['G'] = '9',
    };

    /// <summary>Maps a garbled target token to digits; null when not unambiguously repairable.</summary>
    private static string? MapToken(string token)
    {
        if (int.TryParse(token, out var already) && already >= 1 && already <= MaxSections)
            return token;
        int start = 0;
        while (start < token.Length && start < 2 && !char.IsAsciiDigit(token[start]) && !DigitMap.ContainsKey(token[start]))
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
            else return null;
        }
        var repaired = new string(mapped).TrimStart('0');
        if (repaired.Length == 0) return null;
        return int.TryParse(repaired, out var n) && n >= 1 && n <= MaxSections ? repaired : null;
    }

    /// <summary>
    /// Rewrites garbled turn verbs ("tum to 12", "torn bo 183", "frm to 286") to clean "turn to N"
    /// so the reference extractor sees them. Runs before the per-section fixes so those can target
    /// post-normalization text; runs before the code-edge appends so <see cref="HasExit"/> observes
    /// the repaired verbs.
    /// </summary>
    public static string NormalizeTurnVerbs(string content) =>
        TurnVerbGarble.Replace(content, m =>
        {
            var token = m.Groups[3].Value;
            var prep = m.Groups[2].Success ? m.Groups[2].Value : "";
            // A bare garbled verb followed by a digit-free word is narrative
            // ("retum along", "turn toward"), never a reference.
            if (prep.Length == 0 && !token.Any(char.IsDigit)) return m.Value;
            var mapped = MapToken(token);
            if (mapped is null) return m.Value;
            return (char.IsUpper(m.Groups[1].Value[0]) ? "Turn to " : "turn to ") + mapped;
        });

    /// <summary>
    /// Per-section corrections for exit targets the scan lost or misread. Every rule below is
    /// confirmed against the arek sol24 solution chain or the rendered PDF page; anything
    /// unresolved is deliberately left verbatim rather than guessed.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content)
    {
        content = NormalizeTurnVerbs(content);

        // Restored only when absent, so a future better scan needs no edit. Every edge below
        // is a consecutive sol24 pair (the player physically turned from parent to target) or a
        // print-verified option whose verb garble defeats the cleaner.
        (int parent, int target, string note)[] codeEdges =
        {
            (115, 12, "second option target truncated at slice end"),
            (147, 7, "target dropped after 'Tums'"),
            (235, 312, "second option target truncated at slice end"),
            (251, 182, "target dropped after 'Turn to'"),
        };
        foreach (var (parent, target, _) in codeEdges)
            if (sectionNumber == parent && !HasExit(content, target))
                content += $" (turn to {target}).";

        return sectionNumber switch
        {
            // Garble fixes. NOTE: PostProcessContent runs shared TurnToRepair FIRST, so the text
            // matched here is already post-shared-repair. Plain string Replace is used (proven
            // in-pipeline); every target below was read verbatim from pipeline output and every
            // corrected number is sol24- or render-proven.
            // 3: p13R renders "turn to 396." but the scan reads "3986" (an out-of-range target).
            3 => content.Replace("turn to 3986", "turn to 396"),
            16 => content.Replace("turn to 52K", "turn to 52"),
            51 => content.Replace("Turn to 323", "Turn to 325")
                        .Replace("Turn to 233", "Turn to 235"),
            54 => content.Replace("turn tor 208", "turn to 298"),
            76 => content.Replace("urn tr 323", "turn to 323"),
            80 => content.Replace("turn to 260", "turn to 264"),
            85 => content.Replace("turn to 10 29", "turn to 29"),
            112 => content.Replace("turn to 343", "turn to 345"),
            114 => content.Replace("turn to 239", "turn to 259"),
            115 => content.Replace("turn to 50", "turn to 89"),
            131 => content.Replace("tom bo\n2887", "turn to 288"),
            134 => content.Replace("turn to 27", "turn to 271"),
            135 => content.Replace("Tum ko 377", "turn to 377"),
            137 => content.Replace("turn to 199", "turn to 195")
                        .Replace("tum to oo", "turn to 90"),
            155 => content.Replace("turn to 253", "turn to 293"),
            190 => content.Replace("um =\n359", "turn to 359")
                        .Replace("Torn toy 247", "turn to 247")
                        .Replace("Turn toy 128", "turn to 128"),
            206 => content.Replace("in fo 340", "turn to 340")
                        .Replace("hrm to 8537", "turn to 85"),
            227 => content.Replace("twen Lo 64", "turn to 64"),
            237 => content.Replace("turn to 285", "turn to 289"),
            258 => content.Replace("turn to 100", "turn to 109"),
            263 => content.Replace("turn to 28", "turn to 398"),
            273 => content.Replace("turn to 9.", "turn to 190."),
            288 => content.Replace("Turn to 333", "Turn to 335"),
            317 => content.Replace("=m lo zag", "turn to 204"),
            359 => content.Replace("turn to 208", "turn to 206"),
            372 => content.Replace("turn to 243", "turn to 245"),
            375 => content.Replace("Turn Lo 506", "Turn to 396"),
            389 => content.Replace("turn to zox", "turn to 201"),
            390 => content.Replace("turn to 130", "turn to 149"),
            399 => content.Replace("Turn to 114", "Turn to 119"),
            _ => content,
        };
    }
}
