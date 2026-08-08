namespace DungeonWorld.Core.Entities;

/// <summary>
/// A catalog entry for every item found across the adventure books.
/// The player must meet the requirements to actually use it.
/// </summary>
public class GameItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "artifact"; // weapon | armour | consumable | quest | artifact
    public string? Description { get; set; }
    public string Rarity { get; set; } = "common"; // common | rare | legendary

    public string? BookTitle { get; set; }          // the book this item appears in
    public int? SectionNumber { get; set; }         // where it is found

    // Requirements to use the item
    public int RequiredLevel { get; set; }
    public int? RequiredSkill { get; set; }
    public int? RequiredStamina { get; set; }
    public int? RequiredLuck { get; set; }

    public string? Effects { get; set; }            // what the item does when used
}
