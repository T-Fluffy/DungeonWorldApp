namespace DungeonWorld.DataCleaner.Model;

public sealed class CleanedBook
{
    public int SchemaVersion { get; set; } = 1;
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public CleanedMeta Meta { get; set; } = new();
    public List<CleanedRule> Rules { get; set; } = new();
    public CleanedGraph Graph { get; set; } = new();
    public List<CleanedSection> Sections { get; set; } = new();
}

public sealed class CleanedMeta
{
    public string Title { get; set; } = "";
    public string? Author { get; set; }
    public string? SourceFile { get; set; }
    public int SectionCount { get; set; }
    public int PresentSectionCount { get; set; }
    public int MissingSectionCount { get; set; }
    public int CombatSectionCount { get; set; }
    public int EnemyCount { get; set; }
    public string? MapPath { get; set; }
    public string? AdventureSheetPath { get; set; }
    public string? Introduction { get; set; }
}

public sealed class CleanedRule
{
    public string Stat { get; set; } = "";
    public string Formula { get; set; } = "";
    public string? Description { get; set; }
}

public sealed class CleanedGraph
{
    public int EntrySection { get; set; } = 1;
    public Dictionary<int, List<int>> Outgoing { get; set; } = new();
    public Dictionary<int, List<int>> Incoming { get; set; } = new();
    public List<int> DeadEnds { get; set; } = new();
    public List<int> Terminal { get; set; } = new();
    public List<int> Unreachable { get; set; } = new();
    public List<OrphanLink> OrphanLinks { get; set; } = new();
    public int? MaxDepthFromEntry { get; set; }
}

public sealed class OrphanLink
{
    public int From { get; set; }
    public int Target { get; set; }
}

public sealed class CleanedSection
{
    public int Number { get; set; }
    public string? ImagePath { get; set; }
    public string Raw { get; set; } = "";
    public string Clean { get; set; } = "";
    public List<CleanedChoice> Choices { get; set; } = new();
    public List<int> References { get; set; } = new();
    public CleanedFeatures Features { get; set; } = new();
}

public sealed class CleanedChoice
{
    public string Kind { get; set; } = "choice";
    public string? Label { get; set; }
    public int Target { get; set; }
    public string? Text { get; set; }
    public bool IsDiceRoll { get; set; }
}

public sealed class CleanedFeatures
{
    public bool MissingText { get; set; }
    public bool HasCombat { get; set; }
    public bool LargeScaleCombat { get; set; }
    public List<CleanedEnemy> Enemies { get; set; } = new();
    public string? CombatNote { get; set; }
    public bool IsEnd { get; set; }
    public bool DeathEnd { get; set; }
    public bool VictoryEnd { get; set; }
    public bool HasLuckTest { get; set; }
    public int? LogDays { get; set; }
    public List<string> StatChanges { get; set; } = new();
    public List<string> Booty { get; set; } = new();
    public List<string> DiceInstructions { get; set; } = new();
    public List<string> ItemMentions { get; set; } = new();
}

public sealed class CleanedEnemy
{
    public string Name { get; set; } = "";
    public int Skill { get; set; }
    public int Stamina { get; set; }
    public bool Crew { get; set; }
    public bool HasStats { get; set; } = true;
}
