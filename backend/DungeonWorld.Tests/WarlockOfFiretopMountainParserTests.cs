using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

public sealed class WarlockOfFiretopMountainParserTests
{
    [Theory]
    [InlineData(31, "north door. Turnto", "north door. Turn to 90.")]
    [InlineData(220, "dead-end passage. Turnto", "dead-end passage. Turn to 171.")]
    [InlineData(296, "Turn to 4z2.", "Turn to 42.")]
    public void ExitFix_RewritesKnownTarget(int section, string content, string expected)
    {
        var fixedContent = WarlockOfFiretopMountainParser.ApplySectionFixes(section, content);

        Assert.Equal(expected, fixedContent);
    }
}
