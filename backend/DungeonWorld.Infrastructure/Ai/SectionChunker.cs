namespace DungeonWorld.Infrastructure.Ai;

/// <summary>
/// Pure helpers for batching pages into LLM-sized chunks and merging the
/// per-chunk section lists back into a single section set.
/// </summary>
public static class SectionChunker
{
    /// <summary>Splits logical pages into consecutive groups of at most <paramref name="pageSize"/>.</summary>
    public static List<List<T>> Chunk<T>(List<T> pages, int pageSize)
    {
        if (pageSize <= 0) pageSize = 8;

        var chunks = new List<List<T>>();
        for (int i = 0; i < pages.Count; i += pageSize)
        {
            chunks.Add(pages.GetRange(i, Math.Min(pageSize, pages.Count - i)));
        }
        return chunks;
    }

    /// <summary>
    /// Merges section lists produced by per-chunk LLM calls, in document order.
    /// When the same section number appears as the last item of one chunk and the
    /// first of the next, its content is a split section and gets concatenated.
    /// Duplicate sections appearing elsewhere are dropped (first occurrence wins).
    /// </summary>
    public static List<LlmSection> MergeChunks(IEnumerable<IReadOnlyList<LlmSection>> chunks)
    {
        var byNumber = new Dictionary<int, LlmSection>();
        int? previousChunkLast = null;

        foreach (var chunk in chunks)
        {
            int? chunkLast = null;

            foreach (var sec in chunk)
            {
                if (sec.Number <= 0) continue;
                chunkLast = sec.Number;

                if (byNumber.TryGetValue(sec.Number, out var existing))
                {
                    // A section split across a chunk boundary.
                    if (previousChunkLast == sec.Number)
                    {
                        existing.Content = string.Concat(
                            existing.Content.TrimEnd(), "\n\n", sec.Content.Trim());
                    }
                }
                else
                {
                    byNumber[sec.Number] = new LlmSection
                    {
                        Number = sec.Number,
                        Content = sec.Content,
                    };
                }
            }

            previousChunkLast = chunkLast;
        }

        return byNumber.Values.OrderBy(s => s.Number).ToList();
    }
}
