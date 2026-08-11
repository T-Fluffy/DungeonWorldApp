using DungeonWorld.Core.Interfaces;
using DungeonWorld.Infrastructure.Parsing;
using Microsoft.Extensions.Logging;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Parser factory for the block-based pipeline. Selection order:
///   1. A hand-tuned per-book parser that claims the title (e.g. <see cref="SeasOfBloodParser"/>);
///   2. <see cref="DefaultDungeonWorldParser"/> as a universal rule-based fallback.
/// </summary>
public sealed class DungeonWorldParserFactory : IParserFactory
{
    private readonly List<IBookParser> _specificParsers;
    private readonly IBookParser _defaultParser;
    private readonly ILogger<DungeonWorldParserFactory>? _logger;

    public DungeonWorldParserFactory(
        IEnumerable<IBookParser> parsers,
        DefaultDungeonWorldParser defaultParser,
        ILogger<DungeonWorldParserFactory>? logger = null)
    {
        _specificParsers = parsers
            .Where(p => p is not DefaultDungeonWorldParser)
            .ToList();
        _defaultParser = defaultParser;
        _logger = logger;
    }

    public IBookParser CreateParser(string filePath, string bookTitle)
    {
        foreach (var parser in _specificParsers)
        {
            try
            {
                if (parser.CanHandle(filePath, bookTitle))
                {
                    _logger?.LogInformation(
                        "Selected parser {ParserId} for {BookTitle}", parser.ParserId, bookTitle);
                    return parser;
                }
            }
            catch (Exception ex)
            {
                // A broken CanHandle must not mask the other parsers.
                _logger?.LogWarning(ex,
                    "Parser {ParserType} failed CanHandle check", parser.GetType().Name);
            }
        }

        _logger?.LogWarning(
            "No specific parser matched {BookTitle}; using default rule-based parser", bookTitle);
        return _defaultParser;
    }
}
