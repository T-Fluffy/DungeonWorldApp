namespace DungeonWorld.Core.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "DungeonWorld";
    public string Audience { get; set; } = "DungeonWorld.Client";
    public string Key { get; set; } = string.Empty;
    public int ExpiryDays { get; set; } = 7;
}
