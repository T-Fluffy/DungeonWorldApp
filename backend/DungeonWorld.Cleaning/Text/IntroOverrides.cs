using System.Reflection;

namespace DungeonWorld.Cleaning.Text;

/// <summary>
/// Canonical adventure introductions, transcribed once from the books' intro
/// pages (each adventure's intro is unique) and shipped as embedded resources.
/// Applied automatically by <see cref="BookCleaner"/> so every pipeline entry
/// point (Ingestor, API ingest, DataCleaner) gets the clean text without
/// caller changes. An explicit override passed to
/// <see cref="BookCleaner.Clean"/> still wins.
/// </summary>
public static class IntroOverrides
{
    private static readonly (string Fragment, string Resource)[] Map =
    [
        ("warlock of firetop", "DungeonWorld.Cleaning.Text.Intros.ff01.txt"),
        ("citadel of chaos", "DungeonWorld.Cleaning.Text.Intros.ff02.txt"),
        ("forest of doom", "DungeonWorld.Cleaning.Text.Intros.ff03.txt"),
        ("starship traveller", "DungeonWorld.Cleaning.Text.Intros.ff04.txt"),
        ("city of thieves", "DungeonWorld.Cleaning.Text.Intros.ff05.txt"),
        ("lizard king", "DungeonWorld.Cleaning.Text.Intros.ff07.txt"),
        ("scorpion swamp", "DungeonWorld.Cleaning.Text.Intros.ff08.txt"),
        ("snow witch", "DungeonWorld.Cleaning.Text.Intros.ff09.txt"),
    ];

    /// <summary>Returns the canonical intro for the book, or null when none is curated.</summary>
    public static string? Get(string? bookTitle)
    {
        if (string.IsNullOrWhiteSpace(bookTitle)) return null;
        foreach (var (fragment, resource) in Map)
        {
            if (!bookTitle.Contains(fragment, StringComparison.OrdinalIgnoreCase)) continue;
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
            if (stream is null) return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Trim();
        }
        return null;
    }
}
