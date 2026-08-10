namespace DungeonWorld.Infrastructure.Interfaces;

public interface ILayoutAnalyzer
{
    bool IsDoublePageLayout(string filePath);
    bool IsSinglePageLayout(string filePath) => !IsDoublePageLayout(filePath);
}
