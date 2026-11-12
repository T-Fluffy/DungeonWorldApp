namespace DungeonWorld.Core.Entities;

/// <summary>A navigable exit from a <see cref="StoredSection"/> to another section number.</summary>
public class StoredChoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SectionId { get; set; }
    public StoredSection? Section { get; set; }

    public int Target { get; set; }
    public string? Label { get; set; }
    public string? Text { get; set; }
    public string Kind { get; set; } = "choice";
    public bool IsDiceRoll { get; set; }
}
