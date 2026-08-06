using DungeonWorld.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DungeonWorld.Infrastructure.Parsers;

public class DungeonWorldParserFactory : IParserFactory
{
    private readonly IEnumerable<IBookParser> _parsers;
    private readonly ILogger<DungeonWorldParserFactory>? _logger;

    // Constructor - ILogger is injected by DI, nullable to allow optional logging
    public DungeonWorldParserFactory(
        IEnumerable<IBookParser> parsers,
        ILogger<DungeonWorldParserFactory>? logger = null)
    {
        _parsers = parsers;
        _logger = logger;
    }

    public IBookParser CreateParser(string filePath, string bookTitle)
    {
        _logger?.LogInformation("Analyzing layout for: {BookTitle}", bookTitle);

        // Strategy: Find first parser that CanHandle this file
        // Order: DoublePage first (more common in FF scans)
        foreach (var parser in _parsers.OrderBy(p => p is DoublePageParser ? 0 : 1))
        {
            try
            {
                if (parser.CanHandle(filePath, bookTitle))
                {
                    _logger?.LogInformation("Selected parser: {ParserId} for {BookTitle}", 
                        parser.ParserId, bookTitle);
                    return parser;
                }
            }
            catch (Exception ex)
            {
                // Don't touch parser.ParserId here: a parser's id could itself
                // be broken, which would mask the original error.
                _logger?.LogWarning(ex, "Parser {ParserType} failed CanHandle check", parser.GetType().Name);
            }
        }

        // Fallback: Default to DoublePageParser (most FF scans are 2-up)
        _logger?.LogWarning("No specific parser matched for {BookTitle}, defaulting to DoublePage", bookTitle);
        
        var defaultParser = _parsers.FirstOrDefault(p => p is DoublePageParser);
        
        if (defaultParser == null)
        {
            throw new InvalidOperationException("No DoublePageParser registered in Dependency Injection.");
        }
        
        return defaultParser;
    }
}