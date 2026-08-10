using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Parser tuned for "Seas of Blood" (a single-page scan). Overrides go here as
/// quirks are discovered; the base heuristics are used until then.
/// </summary>
public sealed class SeasOfBloodParser : DungeonWorldBookParserBase
{
    public SeasOfBloodParser(
        IPdfTextExtractor textExtractor,
        IOptions<FileStorageOptions> storageOptions)
        : base(textExtractor, storageOptions)
    {
    }

    public override string ParserId => "SeasOfBlood";

    public override bool CanHandle(string filePath, string bookTitle) =>
        bookTitle.Contains("Seas of Blood", StringComparison.OrdinalIgnoreCase);
}
