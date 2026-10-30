using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Cleaning;

// Reads every processed book under Storage/Books/ProcessedBooks and writes the
// structured CleanedData/<Title>.json used by the game API. Run after ingesting a new
// book, or to repair existing output:
//   dotnet run --project backend/DungeonWorld.DataCleaner
//   dotnet run --project backend/DungeonWorld.DataCleaner -- --only "FF02 Citadel of Chaos.json,FF03 Forest of Doom.json"

var dirs = LocateDirs();
if (dirs == null)
{
    Console.Error.WriteLine("Storage/Books directory not found. Run from the repository root or a subdirectory.");
    return 1;
}

var (booksDir, processedDir) = dirs.Value;
var cleanedDir = Path.Combine(booksDir, "CleanedData");
Directory.CreateDirectory(cleanedDir);

var onlyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
for (int i = 0; i < args.Length; i++)
{
    if (args[i] != "--only" || i + 1 >= args.Length)
    {
        Console.Error.WriteLine(args[i] == "--only"
            ? "--only requires a comma-separated list of processed book filenames."
            : $"Unknown option: {args[i]}");
        return 1;
    }

    var names = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (names.Length == 0)
    {
        Console.Error.WriteLine("--only requires at least one processed book filename.");
        return 1;
    }
    foreach (var name in names)
        onlyFiles.Add(name);
}

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var files = Directory.GetFiles(processedDir, "*.json")
    .Where(f => !Path.GetFileName(f).EndsWith(".cleaned.json", StringComparison.OrdinalIgnoreCase))
    .Where(f => !Path.GetFileName(f).EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
    .Where(f => onlyFiles.Count == 0 || onlyFiles.Contains(Path.GetFileName(f)))
    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
    .ToList();

if (files.Count == 0)
{
    Console.Error.WriteLine(onlyFiles.Count == 0
        ? "No book JSON files found to clean."
        : $"No matching processed books found for --only: {string.Join(", ", onlyFiles)}");
    return 1;
}

foreach (var file in files)
{
    try
    {
        var book = JsonSerializer.Deserialize<Book>(await File.ReadAllTextAsync(file), jsonOptions);
        if (book == null)
        {
            Console.Error.WriteLine($"  Could not parse {Path.GetFileName(file)}");
            continue;
        }

        var cleaned = BookCleaner.Clean(book, Path.GetFileName(file));
        var outPath = BookCleaner.WriteCleanedBook(cleaned, cleanedDir);

        Console.WriteLine($"{Path.GetFileName(file)} -> {Path.GetRelativePath(processedDir, outPath)}");
        Console.WriteLine($"  sections {cleaned.Meta.PresentSectionCount}/{cleaned.Meta.SectionCount} (missing {cleaned.Meta.MissingSectionCount})");
        Console.WriteLine($"  combat {cleaned.Meta.CombatSectionCount} sections, {cleaned.Meta.EnemyCount} enemies");
        Console.WriteLine($"  deadEnds {cleaned.Graph.DeadEnds.Count}, terminal {cleaned.Graph.Terminal.Count}, unreachable {cleaned.Graph.Unreachable.Count}, orphanLinks {cleaned.Graph.OrphanLinks.Count}");
        Console.WriteLine($"  rules: {string.Join(", ", cleaned.Rules.Select(r => $"{r.Stat}={r.Formula}"))}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ERROR processing {Path.GetFileName(file)}: {ex.Message}");
        Console.Error.WriteLine(ex.StackTrace);
    }
}

return 0;

// Locates Storage/Books, walking up from the current directory.
static (string Books, string Processed)? LocateDirs()
{
    var root = Directory.GetCurrentDirectory();
    var probes = new[] { Path.Combine(root, "backend", "Storage", "Books"), Path.Combine(root, "Storage", "Books") };
    foreach (var probe in probes)
    {
        if (Directory.Exists(Path.Combine(probe, "ProcessedBooks"))) return (probe, Path.Combine(probe, "ProcessedBooks"));
    }
    return null;
}
