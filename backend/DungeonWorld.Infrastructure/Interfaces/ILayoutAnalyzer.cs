public interface ILayoutAnalyzer
{
    bool IsDoublePageLayout(string filePath);
    bool IsSinglePageLayout(string filePath) => !IsDoublePageLayout(filePath);
}