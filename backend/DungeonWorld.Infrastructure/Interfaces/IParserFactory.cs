using DungeonWorld.Core.Interfaces;

/// <summary>
/// Factory for selecting the appropriate IBookParser based on PDF layout analysis.
/// Implements the Factory Pattern for parser selection.
/// </summary>
public interface IParserFactory
{
    IBookParser CreateParser(string filePath, string bookTitle);
}