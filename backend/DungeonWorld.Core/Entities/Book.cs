namespace DungeonWorld.Core.Entities;

public class Book
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    
    // NEW: Front Matter Content
    public string Introduction { get; set; } = string.Empty;
    public string AdventureSheetPath { get; set; } // Path to the extracted image
    public string MapPath { get; set; } // Path to the extracted map image
    
    public List<Section> Sections { get; set; } = new();
    public string Author = "Steve Jackson and Ian Livingstone";
}