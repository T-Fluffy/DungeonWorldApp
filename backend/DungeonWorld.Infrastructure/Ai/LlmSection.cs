using System.Text.Json;

namespace DungeonWorld.Infrastructure.Ai;

public sealed class LlmSection
{
    public int Number { get; set; }
    public string Content { get; set; } = "";
}

public sealed class LlmSectionResponse
{
    public List<LlmSection> Sections { get; set; } = new();
}

public static class LlmSectionParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static List<LlmSection> Parse(string json)
    {
        var trimmed = json.Trim();

        // Strict object form: {"sections": [...]}
        try
        {
            var obj = JsonSerializer.Deserialize<LlmSectionResponse>(trimmed, Options);
            if (obj?.Sections != null && obj.Sections.Count > 0)
                return Sanitize(obj.Sections);
        }
        catch (JsonException) { }

        // Bare array form: [...]
        try
        {
            var arr = JsonSerializer.Deserialize<List<LlmSection>>(trimmed, Options);
            if (arr is { Count: > 0 })
                return Sanitize(arr);
        }
        catch (JsonException) { }

        // Models sometimes wrap JSON in prose or markdown fences.
        var start = trimmed.IndexOf('[');
        var end = trimmed.LastIndexOf(']');
        if (start >= 0 && end > start)
        {
            try
            {
                var arr = JsonSerializer.Deserialize<List<LlmSection>>(
                    trimmed.Substring(start, end - start + 1), Options);
                if (arr is { Count: > 0 })
                    return Sanitize(arr);
            }
            catch (JsonException) { }
        }

        throw new InvalidOperationException(
            $"Could not parse LLM section JSON. Snippet: {Snippet(trimmed)}");
    }

    private static List<LlmSection> Sanitize(List<LlmSection> sections)
    {
        return sections
            .Where(s => s.Number is > 0 and <= 400)
            .Select(s => new LlmSection { Number = s.Number, Content = s.Content.Trim() })
            .ToList();
    }

    private static string Snippet(string s) =>
        s.Length > 200 ? s[..200] + "..." : s;
}
