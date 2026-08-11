namespace DungeonWorld.Core.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string PdfUploadPath { get; set; } = "Books";
    public string ImageOutputPath { get; set; } = "wwwroot/assets/book-images";
    public string AvatarPath { get; set; } = "Storage/Avatars";
}