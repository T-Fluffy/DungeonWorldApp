// File: DungeonWorld.Tests/MediaArtTests.cs
using System.Drawing;
using System.Drawing.Imaging;
using DungeonWorld.Infrastructure.Parsing;
using FluentAssertions;

namespace DungeonWorld.Tests;

public class ArtRegionDetectorTests
{
    private static Bitmap WhiteImage(int w, int h)
    {
        var bmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bmp))
            g.Clear(Color.White);
        return bmp;
    }

    private static void Fill(Bitmap bmp, Rectangle rect, bool dark = true)
    {
        using var g = Graphics.FromImage(bmp);
        g.FillRectangle(dark ? Brushes.Black : Brushes.White, rect);
    }

    // Simulates a text-only page: a handful of sparse thin dark lines.
    private static Bitmap TextOnlyPage(int w = 600, int h = 800)
    {
        var bmp = WhiteImage(w, h);
        using var g = Graphics.FromImage(bmp);
        using var pen = new Pen(Color.Black, 2f);
        for (int y = 80; y < 700; y += 24)
            g.DrawLine(pen, 60, y, 540, y);
        return bmp;
    }

    [Fact]
    public void TextOnlyPage_YieldsNoRegions()
    {
        using var page = TextOnlyPage();
        var detector = new ArtRegionDetector();

        detector.Detect(page).Should().BeEmpty();
    }

    [Fact]
    public void PageWithDenseArtBlock_ReturnsOneRegion()
    {
        using var page = WhiteImage(600, 800);
        Fill(page, new Rectangle(80, 100, 440, 500));

        var regions = new ArtRegionDetector().Detect(page);

        regions.Should().ContainSingle();
        // Region should cover most of the art block.
        regions[0].Width.Should().BeGreaterThan(350);
        regions[0].Height.Should().BeGreaterThan(400);
    }

    [Fact]
    public void PageWithTwoDenseArtBlocks_ReturnsTwoRegions()
    {
        using var page = WhiteImage(600, 800);
        Fill(page, new Rectangle(60, 100, 480, 250));
        Fill(page, new Rectangle(60, 500, 480, 250));

        var regions = new ArtRegionDetector().Detect(page);

        regions.Should().HaveCount(2);
        regions.Select(r => r.Y).OrderBy(y => y).Should().BeInAscendingOrder();
    }

    [Fact]
    public void BoldHeadingLine_IsNotArt()
    {
        // A single heavy rule across the top of an otherwise text page.
        using var page = TextOnlyPage();
        Fill(page, new Rectangle(50, 30, 500, 18));

        new ArtRegionDetector().Detect(page).Should().BeEmpty();
    }

    [Fact]
    public void SmallDecorativeBlock_TooShort_IsNotArt()
    {
        using var page = TextOnlyPage();
        Fill(page, new Rectangle(80, 100, 400, 20));

        new ArtRegionDetector().Detect(page).Should().BeEmpty();
    }
}

public class MediaArtParserTests
{
    // The fixture PDF is copied to the test output directory (see .csproj).
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Storage", "Books", "Seas of Blood.pdf");

    [Fact]
    public void Extract_WritesOutputForDigitalBook()
    {
        var parser = new MediaArtParser();
        var outputDir = Path.Combine(Path.GetTempPath(), $"dw-mediaart-{Guid.NewGuid():N}");

        try
        {
            var results = parser.Extract(FixturePath, outputDir, "ff16_seas_of_blood");

            results.Should().NotBeEmpty();
            results.Sum(r => r.FileCount).Should().BeGreaterThan(0);

            var files = Directory.GetFiles(Path.Combine(outputDir, "ff16_seas_of_blood"), "*.png");
            files.Should().NotBeEmpty();
            foreach (var file in files)
            {
                using var img = Image.FromFile(file);
                img.Width.Should().BeGreaterThan(0);
                img.Height.Should().BeGreaterThan(0);
            }
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Extract_WritesRealPngFiles_NotJpegBytes()
    {
        var parser = new MediaArtParser();
        var outputDir = Path.Combine(Path.GetTempPath(), $"dw-mediaart-{Guid.NewGuid():N}");

        try
        {
            parser.Extract(FixturePath, outputDir, "ff16_seas_of_blood");

            var file = Directory.GetFiles(Path.Combine(outputDir, "ff16_seas_of_blood"), "*.png").First();
            var bytes = File.ReadAllBytes(file);
            // PNG magic bytes, not the JPEG (FF D8 FF) the old extraction wrote.
            bytes.Take(4).Should().Equal(0x89, 0x50, 0x4E, 0x47);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }
}
