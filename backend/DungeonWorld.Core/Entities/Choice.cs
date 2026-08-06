namespace DungeonWorld.Core.Entities;

public class Choice
{
    public string Description { get; set; } = string.Empty;
    public int TargetSectionNumber { get; set; }
    public bool IsDiceRoll { get; set; }
}