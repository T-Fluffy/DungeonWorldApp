// File: DungeonWorld.Tests/LayoutDetectionTests.cs
using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Parsing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DungeonWorld.Tests;

public class LayoutDetectionTests
{
    // The fixture PDF is copied to the test output directory (see .csproj).
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Storage", "Uploads", "Seas of Blood.pdf");

    private static IOptions<FileStorageOptions> StorageOptions() =>
        Options.Create(new FileStorageOptions
        {
            PdfUploadPath = "Storage/Uploads",
            ImageOutputPath = "Storage/GameArt",
            AvatarPath = "Storage/Avatars"
        });

    private static IOptions<LlmOptions> UnconfiguredLlm() =>
        Options.Create(new LlmOptions { ApiKey = "", Endpoint = "https://api.openai.com/v1" });

    // Layout-agnostic stub so parser construction needs no real PDF extraction.
    private sealed class FakeTextExtractor : IPdfTextExtractor
    {
        public List<TextBlock> Extract(string filePath) => new();
    }

    // A parser whose CanHandle claim is controlled by the test, so we can exercise
    // factory ordering without manufacturing real books.
    private sealed class StubParser : IBookParser
    {
        public StubParser(bool claims) => Claims = claims;
        public bool Claims { get; }
        public string ParserId => "Stub";
        public bool CanHandle(string filePath, string bookTitle) => Claims;
        public Task<DungeonWorld.Core.Entities.Book> ParseAsync(string filePath) =>
            throw new NotImplementedException();
    }

    private static DungeonWorldParserFactory Factory(params IBookParser[] parsers)
    {
        var defaultParser = new DefaultDungeonWorldParser(new FakeTextExtractor(), StorageOptions());
        return new DungeonWorldParserFactory(
            parsers, aiParser: null, defaultParser, UnconfiguredLlm(),
            NullLogger<DungeonWorldParserFactory>.Instance);
    }

    // --- Real-fixture tests: the bundled "Seas of Blood" is a single-page scan ---

    [Fact]
    public void Analyze_SeasOfBloodScan_IsSinglePage()
    {
        Assert.True(File.Exists(FixturePath), "The 'Seas of Blood.pdf' fixture must be present.");

        var analyzer = new PdfPigLayoutAnalyzer();

        Assert.False(analyzer.IsDoublePageLayout(FixturePath));
        Assert.True(analyzer.IsSinglePageLayout(FixturePath));
    }

    [Fact]
    public void SeasOfBloodParser_CanHandle_ClaimsSeasOfBloodScan()
    {
        var parser = new SeasOfBloodParser(new FakeTextExtractor(), StorageOptions());

        Assert.True(parser.CanHandle(FixturePath, "Seas of Blood"));
        Assert.Equal("SeasOfBlood", parser.ParserId);
    }

    [Fact]
    public void DefaultParser_CanHandle_AnyBook()
    {
        var parser = new DefaultDungeonWorldParser(new FakeTextExtractor(), StorageOptions());

        Assert.True(parser.CanHandle("anything.pdf", "Some Other Book"));
        Assert.Equal("RuleBased", parser.ParserId);
    }

    [Fact]
    public void Factory_SelectsSeasOfBlood_ForSeasOfBloodScan()
    {
        var seas = new SeasOfBloodParser(new FakeTextExtractor(), StorageOptions());
        var factory = Factory(seas);

        var parser = factory.CreateParser(FixturePath, "Seas of Blood");

        Assert.Same(seas, parser);
    }

    // --- Factory ordering unit tests ---

    [Fact]
    public void Factory_PrefersSpecificParser_OverDefault()
    {
        IBookParser specific = new StubParser(claims: true);
        var factory = Factory(specific);

        var parser = factory.CreateParser("any.pdf", "Any Book");

        Assert.Same(specific, parser);
    }

    [Fact]
    public void Factory_FallsBackToDefault_WhenNothingMatches()
    {
        IBookParser claiming = new StubParser(claims: false);
        var factory = Factory(claiming);

        var parser = factory.CreateParser("any.pdf", "Any Book");

        Assert.IsType<DefaultDungeonWorldParser>(parser);
    }

    [Fact]
    public void Factory_SurvivesParserCanHandleExceptions()
    {
        // A parser that blows up during CanHandle must not mask the other parsers.
        IBookParser exploding = new ExplodingParser();
        IBookParser claiming = new StubParser(claims: true);
        var factory = Factory(exploding, claiming);

        var parser = factory.CreateParser("any.pdf", "Any Book");

        Assert.Same(claiming, parser);
    }

    private sealed class ExplodingParser : IBookParser
    {
        public string ParserId => "Exploding";
        public Task<DungeonWorld.Core.Entities.Book> ParseAsync(string filePath) =>
            throw new NotImplementedException();
        public bool CanHandle(string filePath, string bookTitle) =>
            throw new InvalidOperationException("boom");
    }
}
