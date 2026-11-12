namespace DungeonWorld.Core.Entities;

/// <summary>One playable section of a <see cref="StoredBook"/>.</summary>
public class StoredSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookId { get; set; }
    public StoredBook? Book { get; set; }

    public int Number { get; set; }
    public string? ImagePath { get; set; }

    /// <summary>OCR source text as extracted (kept for auditing and fallback rendering).</summary>
    public string Raw { get; set; } = string.Empty;

    /// <summary>Reader-facing text with exits stripped and OCR repaired.</summary>
    public string Clean { get; set; } = string.Empty;

    public bool HasCombat { get; set; }
    public bool MissingText { get; set; }
    public bool IsEnd { get; set; }
    public bool DeathEnd { get; set; }
    public bool VictoryEnd { get; set; }

    /// <summary>
    /// The remaining <c>CleanedFeatures</c> payload (enemies, combat note, stat changes, booty,
    /// dice instructions, item mentions) as JSON: they are display-only lists, and a table per
    /// list would add noise without enabling any query.
    /// </summary>
    public string? FeaturesJson { get; set; }

    /// <summary>Comma-separated outgoing targets, kept in sync with <see cref="Choices"/>.</summary>
    public string ReferencesCsv { get; set; } = string.Empty;

    public List<StoredChoice> Choices { get; set; } = new();
}
