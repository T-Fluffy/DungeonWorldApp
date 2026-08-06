namespace DungeonWorld.Core.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string PdfUploadPath { get; set; } = "Uploads/PDFs";
    public string ImageOutputPath { get; set; } = "wwwroot/assets/book-images";
}