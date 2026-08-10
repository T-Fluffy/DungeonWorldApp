using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Generic fallback parser that handles any book with the default heuristics.
/// A new PDF can be processed out of the box; add a dedicated parser subclass
/// once you know the scan's quirks.
/// </summary>
public sealed class DefaultDungeonWorldParser : DungeonWorldBookParserBase
{
    public DefaultDungeonWorldParser(
        IPdfTextExtractor textExtractor,
        IOptions<FileStorageOptions> storageOptions)
        : base(textExtractor, storageOptions)
    {
    }

    public override string ParserId => "RuleBased";

    public override bool CanHandle(string filePath, string bookTitle) => true;
}
