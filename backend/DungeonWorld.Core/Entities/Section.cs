namespace DungeonWorld.Core.Entities;

public class Section
{
    public int SectionNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public List<Choice> Choices { get; set; } = new();

    /// <summary>
    /// Authoritative exit list supplied by a parser when OCR cannot be trusted (e.g. FF26,
    /// whose scan garbles many "turn to" numbers). When non-null, the cleaner uses exactly
    /// these targets for <c>References</c> and <c>Choices</c> instead of regex extraction;
    /// an empty list therefore marks a genuine terminal section. Null keeps the normal
    /// text-derived behaviour for every other book.
    /// </summary>
    public List<int>? ReferenceOverride { get; set; }

    // NEW: Flag to tell the frontend this section triggers a fight
    public bool HasCombat { get; set; }
}
