using DungeonWorld.Cleaning.Model;

namespace DungeonWorld.Cleaning.Cleaner;

public static class GraphAnalyzer
{
    public static void Build(CleanedBook book)
    {
        var sections = book.Sections;
        var present = sections.Select(s => s.Number).ToHashSet();
        var range = Enumerable.Range(1, book.Meta.SectionCount).ToHashSet();

        var outgoing = sections.ToDictionary(s => s.Number, s => s.References.Distinct().OrderBy(x => x).ToList());
        var incoming = range.ToDictionary(n => n, _ => new List<int>());
        foreach (var (from, targets) in outgoing)
        {
            foreach (var to in targets)
            {
                if (!incoming.ContainsKey(to)) incoming[to] = new List<int>();
                incoming[to].Add(from);
            }
        }

        foreach (var key in incoming.Keys)
        {
            incoming[key] = incoming[key].Distinct().OrderBy(x => x).ToList();
        }

        book.Graph.Outgoing = outgoing;
        book.Graph.Incoming = incoming;
        book.Graph.DeadEnds = sections
            .Where(s => s.References.Count == 0 && !s.Features.MissingText)
            .Select(s => s.Number)
            .OrderBy(x => x)
            .ToList();
        book.Graph.Terminal = sections
            .Where(s => s.References.Count == 0 && (s.Features.DeathEnd || s.Features.VictoryEnd) && !s.Features.MissingText)
            .Select(s => s.Number)
            .OrderBy(x => x)
            .ToList();
        book.Graph.OrphanLinks = sections
            .SelectMany(s => s.References.Where(r => !range.Contains(r)).Select(r => new OrphanLink { From = s.Number, Target = r }))
            .OrderBy(o => o.From)
            .ThenBy(o => o.Target)
            .ToList();

        var entry = book.Graph.EntrySection;
        var reachable = new HashSet<int>();
        var queue = new Queue<int>();
        if (present.Contains(entry))
        {
            queue.Enqueue(entry);
            reachable.Add(entry);
        }
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var to in outgoing.TryGetValue(cur, out var tos) ? tos : [])
            {
                if (!range.Contains(to)) continue;
                if (reachable.Add(to)) queue.Enqueue(to);
            }
        }
        book.Graph.Unreachable = present.Where(n => !reachable.Contains(n)).OrderBy(x => x).ToList();

        book.Graph.MaxDepthFromEntry = MaxDepth(outgoing, entry, range);
    }

    // Maximum shortest-path distance (in edges) from the entry to any reachable section.
    // Bounded, terminates on cyclic graphs (books routinely contain self/mutual references).
    private static int MaxDepth(Dictionary<int, List<int>> outgoing, int entry, HashSet<int> range)
    {
        var dist = new Dictionary<int, int>();
        var queue = new Queue<int>();
        queue.Enqueue(entry);
        dist[entry] = 0;
        int maxDepth = 0;
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            foreach (var to in outgoing.TryGetValue(node, out var tos) ? tos : [])
            {
                if (!range.Contains(to)) continue;
                if (dist.ContainsKey(to)) continue;
                dist[to] = dist[node] + 1;
                maxDepth = Math.Max(maxDepth, dist[to]);
                queue.Enqueue(to);
            }
        }
        return maxDepth;
    }
}
