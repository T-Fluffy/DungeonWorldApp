using DungeonWorld.Core.Entities;

namespace DungeonWorld.Core.Interfaces;

public interface IBookParser
{
    // Unique identifier for this parser (e.g., "SinglePage", "DoublePage", "SeasOfBlood")
    string ParserId { get; }
    
    // Determines if this parser can handle the given PDF/book
    bool CanHandle(string filePath, string bookTitle);
    
    // Main parsing method
    Task<Book> ParseAsync(string filePath);
}