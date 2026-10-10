using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF17 Appointment With F.E.A.R. (Steve Jackson / Ian Livingstone, 1985). Rebuilt from a manual
/// reconstruction manifest (300 dpi line transcripts, 440 sections across pages 15-153; wide pages
/// split into two-up L/R halves, front matter on narrow single-column pages). Intro uses the
/// cover/title page (1) plus the BACKGROUND section (13-14); the curated intro text ships in
/// Intros/ff17.txt. Section exits adjudicated against the arek.bdmonkeys.net walkthrough (sol16).
/// The printed book runs in ascending section order, which was used to resolve garbled/missing
/// headers: a misOCR'd header such as p102 R41 "258" (really 278) had previously collapsed 20
/// sections onto one coordinate. All 440 slices carry body text and the manifest is strictly ordered.
///
/// <para>This is one of the three "hard scans": its exit verbs are mangled far more often than
/// FF13-FF15, so it opts into the isolated <see cref="HardScanExitRepair"/> rules and the
/// <c>HardScanReconstruction</c> second pass. Neither touches the shared pipeline.</para>
/// </summary>
public sealed class AppointmentWithFearParser : ManifestDungeonWorldParser
{
    public AppointmentWithFearParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "AppointmentWithFear";
    protected override string TitleMatch => "Appointment With F.E.A.R.";
    protected override string Slug => "ff17_appointment_with_fear";
    protected override string ManifestResourceName => "DungeonWorld.Infrastructure.Parsing.Manifests.ff17.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 13, 14 };
    /// <summary>FF17 runs to 440 sections — more than the shared 400 cap.</summary>
    private const int MaxSections = 440;

    protected override int MaxSectionNumber => MaxSections;

    /// <summary>Hard scan: run the hardened second OCR pass behind the 300 dpi index backbone.</summary>
    protected override bool UseHardScanOcr => true;

    protected override string PostProcessSection(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, PostProcessContent(content));

    /// <summary>
    /// FF17-only exit repair. Kept out of the shared <c>TurnToRepair</c> because the DataCleaner
    /// applies that to every book, and FF17's aggressive verb list would rewrite curated text
    /// elsewhere. Rules are bounded to this book's 440 sections.
    /// </summary>
    protected override string RepairExits(int sectionNumber, string content) => RepairExits(content);

    /// <summary>
    /// FF17-only exit repair, exposed for tests. Kept out of the shared <c>TurnToRepair</c> because
    /// the DataCleaner applies that to every book, and FF17's aggressive verb list would rewrite
    /// curated text elsewhere. Bounded to this book's 440 sections.
    /// </summary>
    public static string RepairExits(string content) =>
        HardScanExitRepair.Repair(content, MaxSections);

    /// <summary>
    /// Per-section corrections: exit targets OCR read as a different but valid number. Each entry is
    /// a printed number the scan mangled; evidence is the sol16 solution path or the surrounding
    /// dump text. Anything unresolved is deliberately left verbatim for manual review.
    /// </summary>
    public static string ApplySectionFixes(int sectionNumber, string content) => content;
}