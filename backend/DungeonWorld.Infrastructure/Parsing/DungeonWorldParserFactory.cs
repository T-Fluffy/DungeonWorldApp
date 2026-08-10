using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Ai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Infrastructure.Parsing;

/// <summary>
/// Parser factory for the block-based pipeline. Selection order:
///   1. A hand-tuned per-book parser that claims the title (e.g. <see cref="SeasOfBloodParser"/>);
///   2. the AI parser, when an LLM backend is configured;
///   3. <see cref="DefaultDungeonWorldParser"/> as a universal rule-based fallback.
/// </summary>
public sealed class DungeonWorldParserFactory : IParserFactory
{
    private readonly List<IBookParser> _specificParsers;
    private readonly AiDungeonWorldParser? _aiParser;
    private readonly IBookParser _defaultParser;
    private readonly IOptions<LlmOptions> _llmOptions;
    private readonly ILogger<DungeonWorldParserFactory>? _logger;

    public DungeonWorldParserFactory(
        IEnumerable<IBookParser> parsers,
        AiDungeonWorldParser? aiParser,
        DefaultDungeonWorldParser defaultParser,
        IOptions<LlmOptions> llmOptions,
        ILogger<DungeonWorldParserFactory>? logger = null)
    {
        _specificParsers = parsers
            .Where(p => p is not AiDungeonWorldParser && p is not DefaultDungeonWorldParser)
            .ToList();
        _aiParser = aiParser;
        _defaultParser = defaultParser;
        _llmOptions = llmOptions;
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

        if (_llmOptions.Value.IsConfigured)
        {
            _logger?.LogInformation(
                "No specific parser matched {BookTitle}; using AI parser", bookTitle);
            return _aiParser ?? throw new InvalidOperationException(
                "The LLM parser is not registered in dependency injection.");
        }

        _logger?.LogWarning(
            "No specific parser matched {BookTitle}; using default rule-based parser", bookTitle);
        return _defaultParser;
    }
}
