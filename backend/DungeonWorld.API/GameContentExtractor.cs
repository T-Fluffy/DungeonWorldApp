using System.Text.RegularExpressions;

namespace DungeonWorld.API;

/// <summary>
/// Best-effort heuristic extraction of item &amp; spell mentions from processed book text.
/// The OCR text in Fighting Fantasy PDFs is prose, so this uses a whitelist of item
/// keywords plus acquisition phrases, and flags proper-noun artifacts ("X of Y").
/// </summary>
public static class GameContentExtractor
{
    // Item nouns that mark a mention as a collectible item.
    private static readonly string[] ItemKeywords =
    {
        "potion", "sword", "dagger", "shield", "armour", "armor", "crossbow", "helmet",
        "helm", "staff", "ring", "amulet", "key", "scroll", "map", "compass", "skull",
        "idol", "gem", "jewel", "talisman", "spear", "axe", "mace", "cutlass", "pistol",
        "telescope", "lamp", "pouch", "gold", "silver", "crystal", "chalice",
        "goblet", "crown", "cape", "cloak", "mask", "lantern", "provisions", "treasure"
    };

    private static readonly Regex KeywordRegex = new(
        @"\b(?:" + string.Join("|", ItemKeywords) + @")s?\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Explicit red-flag words that mean the mention is NOT a collectible item.
    private static readonly string[] StopWords =
    {
        "swordfish", "sword-billed", "dice", "soup", "bread", "corridor", "door",
        "tunnel", "chimney", "ship", "boat", "vessel", "crew", "slave", "slaves",
        "statue is", "the sea", "the sky", "gold pieces (each", "gold pieces each",
        "booty", "statue", "statues", "shrine", "treasure of", "treasures of"
    };

    private static readonly Regex WrapJoin = new(@"-\s*\r?\n", RegexOptions.Compiled);
    private static readonly Regex NewlineJoin = new(@"\s*\r?\n\s*", RegexOptions.Compiled);
    private static readonly Regex Collapse = new(@"\s{2,}", RegexOptions.Compiled);

    // Acquisition phrase: verb + optional article + the item phrase.
    private static readonly Regex AcquireRegex = new(
        @"(?i)\b(?:you may take|take its|take the|take a|take an|you take the|you find the|you find a|you find an|you find a small|you obtain the|you obtain a|you receive the|you receive a|you gain the|you gain a|you discover the|you discover a|add the|add a|add to your|give you a|gives you a|hand you a|buy the|buy a|purchase the|purchase a|seize the|capture the|collect the|collect a)\s+([A-Z][A-Za-z''-]*(?:\s+(?:of\s+[A-Z][A-Za-z''-]*|[A-Z][A-Za-z''-]*|a\s+[A-Z][A-Za-z''-]*|the\s+[A-Z][A-Za-z''-]*)){0,3})",
        RegexOptions.Compiled);

    // "If you have the X" references a collectible already in the world.
    private static readonly Regex HaveRegex = new(
        @"(?i)\bif you have (?:already )?(?:the|a|an)\s+([A-Z][A-Za-z''-]*(?:\s+(?:of\s+[A-Z][A-Za-z''-]*|[A-Z][A-Za-z''-]*)){0,3})\b",
        RegexOptions.Compiled);

    // Proper-noun artifact: "X of Y" where both are capitalized.
    private static readonly Regex ArtifactRegex = new(
        @"\b([A-Z][a-zA-Z''-]*\s+of\s+[A-Z][a-zA-Z''-]*)\b",
        RegexOptions.Compiled);

    public static string Normalize(string text)
    {
        var t = WrapJoin.Replace(text, "");
        t = NewlineJoin.Replace(t, " ");
        return Collapse.Replace(t, " ").Trim();
    }

    public static List<ExtractedItem> ExtractItems(string normalizedText, int sectionNumber = 0)
    {
        var found = new Dictionary<string, (string name, string type, string rarity, int section)>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return;
            phrase = phrase.Trim().TrimEnd(',', '.', ';', ':');

            // Trim leading filler words that precede the item (e.g. "its staff", "the Crossbow of Axillon").
            phrase = TrimLeadingFiller(phrase);

            if (StopWords.Any(s => phrase.Contains(s, StringComparison.OrdinalIgnoreCase))) return;

            // Must contain at least one item keyword (word boundary aware).
            if (!KeywordRegex.IsMatch(phrase))
                return;

            // Trim trailing filler words that follow the item (e.g. "potion which enables ...").
            phrase = TrimTrailingFiller(phrase);

            if (StopWords.Any(s => phrase.Contains(s, StringComparison.OrdinalIgnoreCase))) return;
            if (phrase.Split(' ').Length > 5) return;

            // Legendary only when it is a named "X of Y" artifact whose X is an item noun.
            bool isArtifact = ArtifactRegex.IsMatch(phrase) && IsArtifactName(phrase);
            string rarity = isArtifact ? "legendary" : "common";
            string type = Classify(phrase);

            // Keep the best (legendary, then earliest) mention.
            if (!found.TryGetValue(phrase, out var existing) ||
                (rarity == "legendary" && existing.rarity != "legendary") ||
                (rarity == existing.rarity && sectionNumber < existing.section))
            {
                found[phrase] = (phrase, type, rarity, sectionNumber);
            }
        }

        foreach (Match m in AcquireRegex.Matches(normalizedText))
        {
            TryAdd(m.Groups[1].Value);
        }

        foreach (Match m in HaveRegex.Matches(normalizedText))
        {
            TryAdd(m.Groups[1].Value);
        }

        foreach (Match m in ArtifactRegex.Matches(normalizedText))
        {
            TryAdd(m.Groups[1].Value);
        }

        return found.Values.Select(v => new ExtractedItem(v.name, v.type, v.rarity, v.section)).ToList();
    }

    public static List<ExtractedSpell> ExtractSpells(string normalizedText, int sectionNumber = 0)
    {
        var found = new Dictionary<string, (string type, int section)>(StringComparer.OrdinalIgnoreCase);

        var spellRegex = new Regex(
            @"(?i)\b(?:spell of|called |known as |incantation of |word of power|cast the spell|the spell|learn the|learned the|teach you|taught you)\s+([A-Z][a-zA-Z''-]*(?:\s+[A-Z][a-zA-Z''-]*){0,2})\b",
            RegexOptions.Compiled);

        foreach (Match m in spellRegex.Matches(normalizedText))
        {
            var name = m.Groups[1].Value.Trim();
            if (name.Split(' ').Length <= 4)
            {
                if (!found.TryGetValue(name, out var existing) || sectionNumber < existing.section)
                    found[name] = ("arcane", sectionNumber);
            }
        }

        return found.Select(kv => new ExtractedSpell(kv.Key, kv.Value.type)).ToList();
    }

    private static string Classify(string phrase)
    {
        var lower = phrase.ToLowerInvariant();
        if (lower.Contains("potion") || lower.Contains("chalice") || lower.Contains("wine") || lower.Contains("provisions"))
            return "consumable";
        if (lower.Contains("sword") || lower.Contains("dagger") || lower.Contains("axe") ||
            lower.Contains("spear") || lower.Contains("mace") || lower.Contains("crossbow") ||
            lower.Contains("cutlass") || lower.Contains("pistol"))
            return "weapon";
        if (lower.Contains("shield") || lower.Contains("armour") || lower.Contains("armor") ||
            lower.Contains("helmet") || lower.Contains("helm") || lower.Contains("cloak") ||
            lower.Contains("cape") || lower.Contains("mask") || lower.Contains("boots"))
            return "armour";
        if (lower.Contains("key") || lower.Contains("map") || lower.Contains("compass") ||
            lower.Contains("telescope") || lower.Contains("lamp") || lower.Contains("lantern"))
            return "quest";
        return "artifact";
    }

    // Words that often trail an item mention but are not part of the item's name.
    private static readonly string[] TrailingFillers =
    {
        "which", "that", "with", "and", "but", "from", "into", "onto", "towards", "against",
        "enables", "allows", "gives", "contains", "will", "you", "your", "if", "so", "then",
        "of", "in", "on", "for", "when", "while", "the", "it", "its", "a", "an"
    };

    private static readonly string[] LeadingFillers =
    {
        "its", "the", "a", "an", "his", "her", "your", "their", "this", "that", "these",
        "those", "some", "all", "any", "other", "more", "each", "no", "one", "two"
    };

    private static string TrimTrailingFiller(string phrase)
    {
        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int end = words.Length;
        while (end > 1)
        {
            string lower = words[end - 1].ToLowerInvariant();
            // Keep the trailing word if it is itself an item keyword (e.g. "potion", "staff").
            if (KeywordRegex.IsMatch(words[end - 1]))
                break;
            bool filler = TrailingFillers.Contains(lower) || !char.IsUpper(words[end - 1][0]);
            if (!filler) break;
            end--;
        }
        // Never reduce to a single standalone keyword like just "Booty" or "Potion".
        return string.Join(" ", words.Take(Math.Max(end, 1)));
    }

    private static string TrimLeadingFiller(string phrase)
    {
        var words = phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        int start = 0;
        while (start < words.Count - 1)
        {
            if (!LeadingFillers.Contains(words[start].ToLowerInvariant()))
                break;
            start++;
        }
        return string.Join(" ", words.Skip(start));
    }

    private static bool IsArtifactName(string phrase)
    {
        // "X of Y": the word before "of" must be an item noun.
        int ofIndex = phrase.IndexOf(" of ", StringComparison.OrdinalIgnoreCase);
        if (ofIndex < 0) return false;
        string head = phrase[..ofIndex].Trim();
        return KeywordRegex.IsMatch(head);
    }
}

public record ExtractedItem(string Name, string Type, string Rarity, int SectionNumber);
public record ExtractedSpell(string Name, string Type);
