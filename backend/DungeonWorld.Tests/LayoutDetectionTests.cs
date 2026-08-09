// File: DungeonWorld.Tests/LayoutDetectionTests.cs
using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Helpers;
using DungeonWorld.Infrastructure.Parsers;
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

    // A parser whose layout claim is controlled by the test, so we can exercise
    // factory ordering without manufacturing a real 2-up PDF.
    private sealed class StubDoublePageParser : DoublePageParser
    {
        public StubDoublePageParser(bool claims) : base(StorageOptions()) => Claims = claims;
        public bool Claims { get; }
        public override bool CanHandle(string filePath, string bookTitle) => Claims;
    }

    private sealed class StubSinglePageParser : SinglePageParser
    {
        public StubSinglePageParser(bool claims) : base(StorageOptions()) => Claims = claims;
        public bool Claims { get; }
        public override bool CanHandle(string filePath, string bookTitle) => Claims;
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
    public void SinglePageParser_CanHandle_ClaimsSeasOfBloodScan()
    {
        var parser = new SinglePageParser(StorageOptions());

        Assert.True(parser.CanHandle(FixturePath, "Seas of Blood"));
        Assert.Equal("SinglePage", parser.ParserId);
    }

    [Fact]
    public void DoublePageParser_CanHandle_RejectsSeasOfBloodScan()
    {
        var parser = new DoublePageParser(StorageOptions());

        Assert.False(parser.CanHandle(FixturePath, "Seas of Blood"));
        Assert.Equal("DoublePage", parser.ParserId);
    }

    [Fact]
    public void Factory_SelectsSinglePage_ForSeasOfBloodScan()
    {
        IBookParser single = new SinglePageParser(StorageOptions());
        IBookParser doublePage = new DoublePageParser(StorageOptions());
        var factory = new DungeonWorldParserFactory(new[] { single, doublePage });

        var parser = factory.CreateParser(FixturePath, "Seas of Blood");

        Assert.Same(single, parser);
    }

    // --- Factory ordering unit tests ---

    [Fact]
    public void Factory_PrefersDoublePage_WhenBothClaimTheFile()
    {
        IBookParser single = new StubSinglePageParser(claims: true);
        IBookParser doublePage = new StubDoublePageParser(claims: true);
        var factory = new DungeonWorldParserFactory(new[] { single, doublePage });

        var parser = factory.CreateParser("any.pdf", "Any Book");

        Assert.Same(doublePage, parser);
    }

    [Fact]
    public void Factory_FallsBackToDoublePage_WhenNothingMatches()
    {
        IBookParser single = new StubSinglePageParser(claims: false);
        IBookParser doublePage = new StubDoublePageParser(claims: false);
        var factory = new DungeonWorldParserFactory(new[] { single, doublePage });

        // No parser claims the file, so the documented FF default kicks in.
        var parser = factory.CreateParser("any.pdf", "Any Book");

        Assert.Same(doublePage, parser);
    }

    [Fact]
    public void Factory_SurvivesParserCanHandleExceptions()
    {
        // A parser that blows up during CanHandle must not mask the other parsers.
        IBookParser exploding = new ExplodingParser();
        IBookParser single = new StubSinglePageParser(claims: true);
        var factory = new DungeonWorldParserFactory(new[] { exploding, single });

        var parser = factory.CreateParser("any.pdf", "Any Book");

        Assert.Same(single, parser);
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
