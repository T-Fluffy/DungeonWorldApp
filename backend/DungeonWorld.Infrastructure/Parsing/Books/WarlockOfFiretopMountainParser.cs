using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// FF01 The Warlock of Firetop Mountain (Steve Jackson and Ian Livingstone, 1982).
/// The embedded manual reconstruction manifest pins all 400 sections from the scan.
/// </summary>
public sealed class WarlockOfFiretopMountainParser : ManifestDungeonWorldParser
{
    public WarlockOfFiretopMountainParser(IOptions<FileStorageOptions> storageOptions)
        : base(storageOptions)
    {
    }

    public override string ParserId => "WarlockOfFiretopMountain";

    public override bool CanHandle(string filePath, string bookTitle) =>
        bookTitle.Contains("Warlock of Firetop Mountain", StringComparison.OrdinalIgnoreCase);

    protected override string TitleMatch => "Warlock of Firetop Mountain";
    protected override string Slug => "ff01_the_warlock_of_firetop_mountain";
    protected override string ManifestResourceName =>
        "DungeonWorld.Infrastructure.Parsing.Manifests.ff01.json";
    protected override IReadOnlyList<int> IntroPages => new[] { 1, 2, 3, 19, 20 };

    public static string ApplySectionFixes(int sectionNumber, string content) =>
        sectionNumber switch
        {
            31 => content.Replace("north door. Turnto", "north door. Turn to 90.", StringComparison.Ordinal),
            220 => content.Replace("dead-end passage. Turnto", "dead-end passage. Turn to 171.", StringComparison.Ordinal),
            296 => content.Replace("Turn to 4z2.", "Turn to 42.", StringComparison.Ordinal),
            _ => content,
        };

    protected override string RepairExits(int sectionNumber, string content) =>
        ApplySectionFixes(sectionNumber, content);
}
