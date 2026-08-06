namespace DungeonWorld.Core.Entities;

public class Section
{
    public int SectionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public List<Choice> Choices { get; set; } = new();
    
    // NEW: Flag to tell the frontend this section triggers a fight
    public bool HasCombat { get; set; }
}