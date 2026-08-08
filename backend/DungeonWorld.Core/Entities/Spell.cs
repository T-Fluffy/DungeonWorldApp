namespace DungeonWorld.Core.Entities;

/// <summary>
/// A spell the player can cast during an adventure if their stats meet the requirements.
/// </summary>
public class Spell
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "arcane"; // arcane | divine | eldritch | elemental
    public string? Description { get; set; }
    public string? Effects { get; set; }

    public string? BookTitle { get; set; }         // book where the spell is learned
    public int? SectionNumber { get; set; }

    // Requirements to cast
    public int RequiredLevel { get; set; }
    public int? RequiredSkill { get; set; }
    public int? RequiredStamina { get; set; }
    public int? RequiredLuck { get; set; }
}
