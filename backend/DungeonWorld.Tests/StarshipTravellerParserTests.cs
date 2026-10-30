using DungeonWorld.Core.Entities;
using DungeonWorld.Infrastructure.Parsing;
using Xunit;

namespace DungeonWorld.Tests;

public sealed class StarshipTravellerParserTests
{
    [Fact]
    public void Section5_UsesVisuallyVerifiedExits()
    {
        var book = new Book
        {
            Sections = new List<Section>
            {
                new() { SectionNumber = 5, Content = "Turn to 120." },
            },
        };

        StarshipTravellerParser.ApplyReferenceFixes(book);

        Assert.Equal(new[] { 15, 85, 143, 148 }, Assert.Single(book.Sections).ReferenceOverride);
    }
}
