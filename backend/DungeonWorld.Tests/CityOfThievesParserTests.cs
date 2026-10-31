using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

public sealed class CityOfThievesParserTests
{
    [Fact]
    public void Section12_RepairsSecondExit()
    {
        var content = CityOfThievesParser.ApplySectionFixes(
            12,
            "Turn to 176. If you would rather leave, turn io 383.");

        Assert.Contains("turn to 176", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("turn to 383", content, StringComparison.OrdinalIgnoreCase);
    }
}
