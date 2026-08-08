namespace DungeonWorld.Core.Entities;

/// <summary>
/// A command the player can type in the Chronicle chat box to interact with the game.
/// </summary>
public class GameCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;        // e.g. "GO"
    public string[] Aliases { get; set; } = Array.Empty<string>();
    public string Description { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;       // e.g. "GO 42"
    public string Category { get; set; } = "navigation";    // navigation | inventory | combat | system | lore
}
