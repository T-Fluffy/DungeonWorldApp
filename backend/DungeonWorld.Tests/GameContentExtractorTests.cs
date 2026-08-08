using DungeonWorld.API;

namespace DungeonWorld.Tests;

public class GameContentExtractorTests
{
    [Fact]
    public void Normalize_JoinsHyphenatedLineBreaks()
    {
        string text = "you take the Crossbow of Axillon, turn to 163; otherwise, turn to no. it is a wonderful\nlong-winded item";
        string norm = GameContentExtractor.Normalize(text);
        Assert.DoesNotContain("\n", norm);
        Assert.Contains("long-winded", norm);
    }

    [Fact]
    public void ExtractItems_FindsProperNounArtifact_AsLegendary()
    {
        string text = "Hunger burns in its eyes. If you have the Crossbow of Axillon, turn to 163.";
        var items = GameContentExtractor.ExtractItems(GameContentExtractor.Normalize(text));
        var crossbow = items.FirstOrDefault(i => i.Name.Contains("Crossbow of Axillon"));
        Assert.NotNull(crossbow);
        Assert.Equal("legendary", crossbow.Rarity);
        Assert.Equal("weapon", crossbow.Type);
    }

    [Fact]
    public void ExtractItems_FindsPotion_AsConsumable()
    {
        string text = "The Sprites give you a Magic Potion which enables you to stay underwater.";
        var items = GameContentExtractor.ExtractItems(GameContentExtractor.Normalize(text));
        var potion = items.FirstOrDefault(i => i.Name.ToLowerInvariant().Contains("potion"));
        Assert.NotNull(potion);
        Assert.Equal("Magic Potion", potion.Name);
        Assert.Equal("consumable", potion.Type);
        Assert.DoesNotContain("which", potion.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExtractItems_IgnoresPlaceNames()
    {
        string text = "You sail through the Channel of Goth towards the Isle of Volcanoes and past the Shoals of Trysta.";
        var items = GameContentExtractor.ExtractItems(GameContentExtractor.Normalize(text));
        Assert.DoesNotContain(items, i => i.Name.Contains("Channel of Goth"));
        Assert.DoesNotContain(items, i => i.Name.Contains("Isle of Volcanoes"));
        Assert.DoesNotContain(items, i => i.Name.Contains("Shoals of Trysta"));
    }

    [Fact]
    public void ExtractItems_FindsHelmet_AsArmour()
    {
        string text = "The Helmet of Ut-Napishtim protects you from the Brain-eaters.";
        var items = GameContentExtractor.ExtractItems(GameContentExtractor.Normalize(text));
        Assert.Contains(items, i => i.Name.Contains("Ut-Napishtim") && i.Type == "armour" && i.Rarity == "legendary");
    }

    [Fact]
    public void ExtractItems_KeepsEarliestSection()
    {
        string text = "Section one. take the Skull of Salt. ";
        var items = GameContentExtractor.ExtractItems(GameContentExtractor.Normalize(text), 10);
        Assert.Contains(items, i => i.Name.Contains("Skull of Salt") && i.SectionNumber == 10);
    }

    [Fact]
    public void ExtractSpells_FindsSpellMention()
    {
        string text = "The wizard teaches you the Word of Power Firebolt.";
        var spells = GameContentExtractor.ExtractSpells(GameContentExtractor.Normalize(text));
        Assert.NotEmpty(spells);
    }
}
