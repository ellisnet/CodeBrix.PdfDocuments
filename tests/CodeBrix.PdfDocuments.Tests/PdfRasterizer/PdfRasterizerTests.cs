using CodeBrix.Imaging.Formats;
using CodeBrix.Imaging.Formats.Bmp;
using CodeBrix.Imaging.Formats.Gif;
using CodeBrix.Imaging.Formats.Jpeg;
using CodeBrix.Imaging.Formats.Png;
using CodeBrix.Imaging.Formats.Tiff;
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Fonts;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#if SAVE_TEMP_FILES
using CodeBrix.PdfDocuments.Tests.Helpers;
#endif

// ReSharper disable RedundantArgumentDefaultValue

namespace CodeBrix.PdfDocuments.Tests.PdfRasterizer;

public class PdfRasterizerTests : IDisposable
{
#if SAVE_TEMP_FILES

#if TESTING_ON_LINUX
    public const string TempFolder = @"/home/jeremy/Temp";
#elif TESTING_ON_MACOS
    public const string TempFolder = @"/Users/jeremy/Temp";
#elif TESTING_ON_LINUX_ORANGEPI
    public const string TempFolder = "/home/orangepi/Temp";
#else
    //TESTING_ON_WINDOWS
    public const string TempFolder = @"C:\Temp"; 
#endif

#endif

    private const string RobotoFamilyName = "Roboto";
    private const string RobotoRegularFaceName = "Roboto-Regular";
    private const string RobotoRegularResourceName = "CodeBrix.PdfDocuments.Tests.SampleFiles.Roboto-Regular.ttf";

    static PdfRasterizerTests()
    {
        var robotoFontResolver = new EmbeddedFontResolver(
            fontFamilyName: RobotoFamilyName,
            fontFaceResources:
            [
                new EmbeddedResourceFontFace(FaceName: RobotoRegularFaceName, EmbeddedResourceName: RobotoRegularResourceName),
            ],
            fontEmbeddedResourceAssembly: typeof(PdfRasterizerTests).Assembly);

        MetaFontResolver.Instance.RegisterFontResolver(RobotoRegularFaceName, robotoFontResolver);
    }

    private static readonly string[][] KnockKnockJokes =
    [
        ["Knock knock!", "Who's there?", "Lettuce.", "Lettuce who?", "Lettuce in, it's cold out here!"],
        ["Knock knock!", "Who's there?", "Boo.", "Boo who?", "Don't cry, it's just a joke!"],
        ["Knock knock!", "Who's there?", "Nobel.", "Nobel who?", "Nobel, that's why I knocked!"],
        ["Knock knock!", "Who's there?", "Tank.", "Tank who?", "You're welcome!"],
        ["Knock knock!", "Who's there?", "Atch.", "Atch who?", "Bless you!"],
        ["Knock knock!", "Who's there?", "Cow says.", "Cow says who?", "No silly, cow says moooo!"],
        ["Knock knock!", "Who's there?", "Wooden shoe.", "Wooden shoe who?", "Wooden shoe like to hear another joke?"],
        ["Knock knock!", "Who's there?", "Harry.", "Harry who?", "Harry up and answer the door!"],
        ["Knock knock!", "Who's there?", "Ice cream.", "Ice cream who?", "Ice cream every time I see a spider!"],
        ["Knock knock!", "Who's there?", "Olive.", "Olive who?", "Olive you and I don't care who knows it!"],
    ];

    private readonly PageRasterizer _rasterizer = new();

    public void Dispose() => _rasterizer.Dispose();

    #region | Test helpers |

    private static PdfDocument CreateSamplePdf(int pageCount = 2, int jokeIndex = 0)
    {
        var doc = new PdfDocument();
        var titleFont = new XFont(RobotoFamilyName, 20);
        var jokeFont = new XFont(RobotoFamilyName, 14);

        for (var i = 0; i < pageCount; i++)
        {
            var page = doc.AddPage();
            page.Size = PageSize.Letter; // 612 x 792 pts
            using var gfx = XGraphics.FromPdfPage(page);

            // Draw the knock-knock joke text
            var joke = KnockKnockJokes[(jokeIndex + i) % KnockKnockJokes.Length];
            var y = 60.0;
            for (var line = 0; line < joke.Length; line++)
            {
                var font = (line == 0) ? titleFont : jokeFont;
                gfx.DrawString(joke[line], font, XBrushes.Black, new XPoint(60, y));
                y += (line == 0) ? 40 : 28;
            }

            // The iconic colored rectangles
            var brush = (i % 2 == 0) ? XBrushes.Blue : XBrushes.Red;
            gfx.DrawRectangle(brush, 50, 300, 200, 100);
            gfx.DrawRectangle(XBrushes.Green, 100, 450, 150, 150);
        }
        return doc;
    }

    private static byte[] CreateSamplePdfBytes(int pageCount = 2, int jokeIndex = 0)
    {
        using var doc = CreateSamplePdf(pageCount, jokeIndex);
        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    private static MemoryStream CreateSamplePdfStream(int pageCount = 2, int jokeIndex = 0)
    {
        var bytes = CreateSamplePdfBytes(pageCount, jokeIndex);
        return new MemoryStream(bytes);
    }

    private static string CreateTempOutputDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"PdfRasterizerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CleanupDirectory(string dir)
    {
        if (Directory.Exists(dir))
        {
            try { Directory.Delete(dir, true); } catch { /* best effort */ }
        }
    }

    private static IImageFormat ResolveImageFormat(string name) => name switch
    {
        "png" => PngFormat.Instance,
        "jpeg" => JpegFormat.Instance,
        "bmp" => BmpFormat.Instance,
        "gif" => GifFormat.Instance,
        "tiff" => TiffFormat.Instance,
        _ => throw new ArgumentException($"Unknown format: {name}")
    };

    #endregion

    #region | Constructor and property tests |

    [Fact]
    public void Constructor_SetsDefaultProperties()
    {
        using var rasterizer = new PageRasterizer();
        rasterizer.Dpi.Should().Be(300);
        rasterizer.Password.Should().Be(null);
        rasterizer.OutputDirectory.Should().Be(null);
        rasterizer.RasterizedImageFormat.Should().Be(PngFormat.Instance);
        rasterizer.AllowOverwriteFiles.Should().Be(false);
        rasterizer.BackgroundColor.Should().Be(0xFFFFFFFF);
        rasterizer.ThumbnailMaxDimensions.Should().Be(null);
    }

    [Fact]
    public void Dpi_SetAndGet()
    {
        _rasterizer.Dpi = 150;
        _rasterizer.Dpi.Should().Be(150);
    }

    [Fact]
    public void Dpi_ResetsBelowOneToDefault()
    {
        _rasterizer.Dpi = 0;
        _rasterizer.Dpi.Should().Be(300);

        _rasterizer.Dpi = -1;
        _rasterizer.Dpi.Should().Be(300);
    }

    [Fact]
    public void Password_SetAndGet()
    {
        _rasterizer.Password = "secret";
        _rasterizer.Password.Should().Be("secret");
    }

    [Fact]
    public void Password_ClearsOnEmptyOrNull()
    {
        _rasterizer.Password = "secret";
        _rasterizer.Password = "";
        _rasterizer.Password.Should().Be(null);

        _rasterizer.Password = "secret";
        _rasterizer.Password = null;
        _rasterizer.Password.Should().Be(null);
    }

    [Fact]
    public void OutputDirectory_SetAndGet()
    {
        _rasterizer.OutputDirectory = @"C:\Output";
        _rasterizer.OutputDirectory.Should().Be(@"C:\Output");
    }

    [Fact]
    public void OutputDirectory_ClearsOnWhitespace()
    {
        _rasterizer.OutputDirectory = @"C:\Output";
        _rasterizer.OutputDirectory = "   ";
        _rasterizer.OutputDirectory.Should().Be(null);
    }

    [Fact]
    public void FileNameGenerator_SetAndGet()
    {
        Func<int, string> generator = pageNumber => $"Page_{pageNumber}";
        _rasterizer.FileNameGenerator = generator;
        _rasterizer.FileNameGenerator.Should().Be(generator);
    }

    [Fact]
    public void FileNameGenerator_ResetsToDefaultOnNull()
    {
        _rasterizer.FileNameGenerator = pageNumber => $"Custom_{pageNumber}";
        _rasterizer.FileNameGenerator = null;
        _rasterizer.FileNameGenerator!(1).Should().Be("Rasterized_Page_1");
    }

    [Fact]
    public void RasterizedImageFormat_SetAndGet()
    {
        _rasterizer.RasterizedImageFormat = JpegFormat.Instance;
        _rasterizer.RasterizedImageFormat.Should().Be(JpegFormat.Instance);
    }

    [Fact]
    public void RasterizedImageFormat_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _rasterizer.RasterizedImageFormat = null);
    }

    [Fact]
    public void AllowOverwriteFiles_SetAndGet()
    {
        _rasterizer.AllowOverwriteFiles = true;
        _rasterizer.AllowOverwriteFiles.Should().Be(true);
    }

    [Fact]
    public void BackgroundColor_SetAndGet()
    {
        _rasterizer.BackgroundColor = 0xFF0000FF;
        _rasterizer.BackgroundColor.Should().Be(0xFF0000FF);
    }

    [Fact]
    public void ThumbnailMaxDimensions_SetAndGet()
    {
        var dims = new ThumbnailMaxDimensions(100, 130);
        _rasterizer.ThumbnailMaxDimensions = dims;
        _rasterizer.ThumbnailMaxDimensions.Should().Be(dims);
    }

    [Fact]
    public void ThumbnailMaxDimensions_ResetsOnNull()
    {
        _rasterizer.ThumbnailMaxDimensions = new ThumbnailMaxDimensions(100, 130);
        _rasterizer.ThumbnailMaxDimensions = null;
        _rasterizer.ThumbnailMaxDimensions.Should().Be(null);
    }

    #endregion

    #region | Disposal tests |

    [Fact]
    public async Task Dispose_ThrowsOnSubsequentRasterize()
    {
        var rasterizer = new PageRasterizer();
        rasterizer.Dispose();

        var pdfBytes = CreateSamplePdfBytes(1);
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => rasterizer.RasterizeToImages(pdfBytes, cancellationToken: CancellationToken.None));
    }

    #endregion

    #region | GetPageCount tests |

    [Fact]
    public async Task GetPageCount_FromPdfDocument_ReturnsCorrectCount()
    {
        using var doc = CreateSamplePdf(3);
        var count = await _rasterizer.GetPageCount(doc, cancellationToken: CancellationToken.None);
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetPageCount_FromBytes_ReturnsCorrectCount()
    {
        var pdfBytes = CreateSamplePdfBytes(4);
        var count = await _rasterizer.GetPageCount(pdfBytes, cancellationToken: CancellationToken.None);
        count.Should().Be(4);
    }

    [Fact]
    public async Task GetPageCount_FromStream_ReturnsCorrectCount()
    {
        using var stream = CreateSamplePdfStream(5);
        var count = await _rasterizer.GetPageCount(stream, cancellationToken: CancellationToken.None);
        count.Should().Be(5);
    }

    #endregion

    #region | GetPageDimensions tests |

    [Fact]
    public async Task GetPageDimensions_ReturnsCorrectDimensions()
    {
        using var doc = CreateSamplePdf(1);
        var dims = await _rasterizer.GetPageDimensions(doc, 1, cancellationToken: CancellationToken.None);

        // Letter size: 612 x 792 points (8.5 x 11 inches)
        dims.WidthInPoints.Should().Be(612.0);
        dims.HeightInPoints.Should().Be(792.0);
    }

    [Fact]
    public async Task GetPageDimensions_ComputedProperties_AreCorrect()
    {
        using var doc = CreateSamplePdf(1);
        var dims = await _rasterizer.GetPageDimensions(doc, 1, cancellationToken: CancellationToken.None);

        // 612 / 72 = 8.5, 792 / 72 = 11.0
        dims.WidthInInches.Should().Be(8.5);
        dims.HeightInInches.Should().Be(11.0);

        // At 300 DPI: (612 * 300 / 72) = 2550, (792 * 300 / 72) = 3300
        dims.GetWidthInPixels(300).Should().Be(2550);
        dims.GetHeightInPixels(300).Should().Be(3300);
    }

    [Fact]
    public async Task GetPageDimensions_InvalidPageNumber_ThrowsArgumentException()
    {
        var pdfBytes = CreateSamplePdfBytes(1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _rasterizer.GetPageDimensions(pdfBytes, 0, cancellationToken: CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _rasterizer.GetPageDimensions(pdfBytes, 2, cancellationToken: CancellationToken.None));
    }

    #endregion

    #region | RasterizeToImages tests |

    [Fact]
    public async Task RasterizeToImages_FromPdfDocument_ReturnsAllPages()
    {
        using var doc = CreateSamplePdf(3);
        var images = await _rasterizer.RasterizeToImages(doc, cancellationToken: CancellationToken.None);

        try
        {
            images.Count.Should().Be(3);
            foreach (var image in images)
            {
                Assert.True(image.Width > 0);
                Assert.True(image.Height > 0);
            }

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                _ = await PdfHelper.WriteImageCollection(
                    images, TempFolder, $"{DateTime.Now.Ticks}_RasterizeToImages_FromPdfDocument");
            }
#endif
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    [Theory]
    [InlineData("png")]
    [InlineData("jpeg")]
    [InlineData("bmp")]
    [InlineData("gif")]
    public async Task RasterizeToImages_InDifferentFormats_ProducesValidImages(string formatName)
    {
        var format = ResolveImageFormat(formatName);
        var pdfBytes = CreateSamplePdfBytes(1, jokeIndex: 1);
        var images = await _rasterizer.RasterizeToImages(pdfBytes, desiredImageFormat: format, 
            cancellationToken: CancellationToken.None);

        try
        {
            images.Count.Should().Be(1);
            Assert.True(images[0].Width > 0);
            Assert.True(images[0].Height > 0);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                _ = await PdfHelper.WriteImageCollection(
                    images, TempFolder, $"{DateTime.Now.Ticks}_RasterizeToImages_As_{formatName}");
            }
#endif
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    [Fact]
    public async Task RasterizeToImages_WithPageNumbers_ReturnsSubset()
    {
        var pdfBytes = CreateSamplePdfBytes(4, jokeIndex: 2);
        var images = await _rasterizer.RasterizeToImages(pdfBytes, pageNumbers: [1, 3],
            cancellationToken: CancellationToken.None);

        try
        {
            images.Count.Should().Be(2);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                _ = await PdfHelper.WriteImageCollection(
                    images, TempFolder, $"{DateTime.Now.Ticks}_RasterizeToImages_PageSubset");
            }
#endif
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    [Fact]
    public async Task RasterizeToImages_FromStream_ReturnsAllPages()
    {
        using var stream = CreateSamplePdfStream(2, jokeIndex: 3);
        var images = await _rasterizer.RasterizeToImages(stream,
            cancellationToken: CancellationToken.None);

        try
        {
            images.Count.Should().Be(2);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                _ = await PdfHelper.WriteImageCollection(
                    images, TempFolder, $"{DateTime.Now.Ticks}_RasterizeToImages_FromStream");
            }
#endif
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    [Fact]
    public async Task RasterizeToImages_InvalidPageNumber_ThrowsArgumentException()
    {
        var pdfBytes = CreateSamplePdfBytes(2);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _rasterizer.RasterizeToImages(pdfBytes, pageNumbers: [0], 
                cancellationToken: CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _rasterizer.RasterizeToImages(pdfBytes, pageNumbers: [3], 
                cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task RasterizeToImages_WithCustomDpi_ProducesCorrectDimensions()
    {
        using var doc = CreateSamplePdf(1, jokeIndex: 4);
        var images = await _rasterizer.RasterizeToImages(doc, dpi: 72,
            cancellationToken: CancellationToken.None);

        try
        {
            // At 72 DPI, a Letter page (612 x 792 pts) should be ~612 x 792 pixels
            images.Count.Should().Be(1);
            images[0].Width.Should().Be(612);
            images[0].Height.Should().Be(792);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                _ = await PdfHelper.WriteImageCollection(
                    images, TempFolder, $"{DateTime.Now.Ticks}_RasterizeToImages_72dpi");
            }
#endif
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    #endregion

    #region | RasterizeToImage tests |

    [Theory]
    [InlineData("png")]
    [InlineData("jpeg")]
    [InlineData("bmp")]
    [InlineData("tiff")]
    public async Task RasterizeToImage_SinglePage_InDifferentFormats(string formatName)
    {
        var format = ResolveImageFormat(formatName);
        var pdfBytes = CreateSamplePdfBytes(2, jokeIndex: 5);
        using var image = await _rasterizer.RasterizeToImage(pdfBytes, 1, desiredImageFormat: format,
            cancellationToken: CancellationToken.None);

        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);

#if SAVE_TEMP_FILES
        if (Directory.Exists(TempFolder))
        {
            _ = await PdfHelper.WriteImage(
                image, TempFolder, $"{DateTime.Now.Ticks}_RasterizeToImage_As_{formatName}");
        }
#endif
    }

    [Fact]
    public async Task RasterizeToImage_FromPdfDocument_ReturnsImage()
    {
        using var doc = CreateSamplePdf(2, jokeIndex: 6);
        using var image = await _rasterizer.RasterizeToImage(doc, 2,
            cancellationToken: CancellationToken.None);

        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);

#if SAVE_TEMP_FILES
        if (Directory.Exists(TempFolder))
        {
            _ = await PdfHelper.WriteImage(
                image, TempFolder, $"{DateTime.Now.Ticks}_RasterizeToImage_FromPdfDocument_Page2");
        }
#endif
    }

    [Fact]
    public async Task RasterizeToImage_FromStream_ReturnsImage()
    {
        using var stream = CreateSamplePdfStream(1, jokeIndex: 7);
        using var image = await _rasterizer.RasterizeToImage(stream, 1,
            cancellationToken: CancellationToken.None);

        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);

#if SAVE_TEMP_FILES
        if (Directory.Exists(TempFolder))
        {
            _ = await PdfHelper.WriteImage(
                image, TempFolder, $"{DateTime.Now.Ticks}_RasterizeToImage_FromStream");
        }
#endif
    }

    [Fact]
    public async Task RasterizeToImage_InvalidPageZero_ThrowsArgumentException()
    {
        var pdfBytes = CreateSamplePdfBytes(1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _rasterizer.RasterizeToImage(pdfBytes, 0, 
                cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task RasterizeToImage_PageExceedsCount_ThrowsArgumentException()
    {
        var pdfBytes = CreateSamplePdfBytes(1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _rasterizer.RasterizeToImage(pdfBytes, 2, 
                cancellationToken: CancellationToken.None));
    }

    #endregion

    #region | RasterizeToImageFiles tests |

    [Theory]
    [InlineData("png")]
    [InlineData("jpeg")]
    [InlineData("bmp")]
    [InlineData("tiff")]
    public async Task RasterizeToImageFiles_InDifferentFormats_CreatesFiles(string formatName)
    {
        var format = ResolveImageFormat(formatName);
        var outputDir = CreateTempOutputDirectory();

        try
        {
            var pdfBytes = CreateSamplePdfBytes(2, jokeIndex: 8);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToImageFiles(pdfBytes, outputDir, desiredImageFormat: format,
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(2);

            foreach (var file in files)
            {
                Assert.True(new FileInfo(file).Length > 0);
            }

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                foreach (var file in files)
                {
                    File.Copy(file,
                        Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ImageFiles_As_{formatName}_{Path.GetFileName(file)}"),
                        overwrite: true);
                }
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToImageFiles_FromPdfDocument_CreatesFiles()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            using var doc = CreateSamplePdf(2, jokeIndex: 9);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToImageFiles(doc, outputDir,
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(2);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                foreach (var file in files)
                {
                    File.Copy(file,
                        Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ImageFiles_FromPdfDoc_{Path.GetFileName(file)}"),
                        overwrite: true);
                }
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToImageFiles_UsesOutputDirectoryProperty()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            var pdfBytes = CreateSamplePdfBytes(1, jokeIndex: 1);
            _rasterizer.OutputDirectory = outputDir;
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToImageFiles(pdfBytes, 
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                foreach (var file in files)
                {
                    File.Copy(file,
                        Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ImageFiles_OutputDirProp_{Path.GetFileName(file)}"),
                        overwrite: true);
                }
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToImageFiles_NoOutputDirectory_ThrowsArgumentException()
    {
        var pdfBytes = CreateSamplePdfBytes(1);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _rasterizer.RasterizeToImageFiles(pdfBytes, 
                cancellationToken: CancellationToken.None));
    }

    [Fact]
    public async Task RasterizeToImageFiles_UsesCustomFileNameGenerator()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            var pdfBytes = CreateSamplePdfBytes(1, jokeIndex: 2);
            _rasterizer.FileNameGenerator = pageNumber => $"MyPage_{pageNumber}";
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToImageFiles(pdfBytes, outputDir, 
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);
            // Asserted exactly, not with Assert.Contains: a substring check passed happily against
            // the old "MyPage_1..png", which is how the doubled dot survived.
            Path.GetFileName(files[0]).Should().Be("MyPage_1.png");

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                foreach (var file in files)
                {
                    File.Copy(file,
                        Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ImageFiles_CustomName_{Path.GetFileName(file)}"),
                        overwrite: true);
                }
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToImageFiles_UsesDefaultNameWithASingleDotBeforeTheExtension()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            var pdfBytes = CreateSamplePdfBytes(1, jokeIndex: 2);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToImageFiles(pdfBytes, outputDir,
                cancellationToken: TestContext.Current.CancellationToken);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);
            Path.GetFileName(files[0]).Should().Be("Rasterized_Page_1.png");
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToImageFiles_WithNonDefaultFormat_UsesThatFormatsExtension()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            var pdfBytes = CreateSamplePdfBytes(1, jokeIndex: 2);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToImageFiles(pdfBytes, outputDir,
                desiredImageFormat: JpegFormat.Instance,
                cancellationToken: TestContext.Current.CancellationToken);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);
            Path.GetFileName(files[0]).Should().Be($"Rasterized_Page_1{JpegFormat.Instance.DefaultFileExtension}");
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToThumbnailFiles_UsesASingleDotBeforeTheExtension()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            var pdfBytes = CreateSamplePdfBytes(1, jokeIndex: 2);
            _rasterizer.AllowOverwriteFiles = true;
            _rasterizer.FileNameGenerator = pageNumber => $"Thumb_{pageNumber}";
            await _rasterizer.RasterizeToThumbnailFiles(pdfBytes, outputDirectory: outputDir,
                cancellationToken: TestContext.Current.CancellationToken);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);
            Path.GetFileName(files[0]).Should().Be("Thumb_1.png");
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    #endregion

    #region | RasterizeToImageFile tests |

    [Theory]
    [InlineData("png")]
    [InlineData("jpeg")]
    [InlineData("bmp")]
    [InlineData("tiff")]
    public async Task RasterizeToImageFile_InDifferentFormats_CreatesSingleFile(string formatName)
    {
        var format = ResolveImageFormat(formatName);
        var outputDir = CreateTempOutputDirectory();

        try
        {
            var pdfBytes = CreateSamplePdfBytes(3, jokeIndex: 3);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToImageFile(pdfBytes, 2, outputDir, desiredImageFormat: format,
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);
            Assert.True(new FileInfo(files[0]).Length > 0);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                File.Copy(files[0],
                    Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ImageFile_As_{formatName}_{Path.GetFileName(files[0])}"),
                    overwrite: true);
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToImageFile_FromPdfDocument_CreatesSingleFile()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            using var doc = CreateSamplePdf(2, jokeIndex: 4);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToImageFile(doc, 1, outputDir,
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                File.Copy(files[0],
                    Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ImageFile_FromPdfDoc_{Path.GetFileName(files[0])}"),
                    overwrite: true);
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToImageFile_FromStream_CreatesSingleFile()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            using var stream = CreateSamplePdfStream(2, jokeIndex: 5);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToImageFile(stream, 1, outputDir,
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                File.Copy(files[0],
                    Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ImageFile_FromStream_{Path.GetFileName(files[0])}"),
                    overwrite: true);
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    #endregion

    #region | RasterizeToThumbnails tests |

    [Fact]
    public async Task RasterizeToThumbnails_FitsWithinDefaultMaxDimensions()
    {
        var pdfBytes = CreateSamplePdfBytes(2, jokeIndex: 6);
        var images = await _rasterizer.RasterizeToThumbnails(pdfBytes,
            cancellationToken: CancellationToken.None);

        try
        {
            images.Count.Should().Be(2);
            foreach (var image in images)
            {
                // Default max dimensions: 200 x 260
                Assert.True(image.Width <= 200);
                Assert.True(image.Height <= 260);
                Assert.True(image.Width > 0);
                Assert.True(image.Height > 0);
            }

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                _ = await PdfHelper.WriteImageCollection(
                    images, TempFolder, $"{DateTime.Now.Ticks}_Thumbnails_DefaultDims");
            }
#endif
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    [Fact]
    public async Task RasterizeToThumbnails_FitsWithinCustomMaxDimensions()
    {
        var customDims = new ThumbnailMaxDimensions(100, 130);
        var pdfBytes = CreateSamplePdfBytes(2, jokeIndex: 7);
        var images = await _rasterizer.RasterizeToThumbnails(pdfBytes, maxDimensions: customDims, 
            cancellationToken: CancellationToken.None);

        try
        {
            images.Count.Should().Be(2);
            foreach (var image in images)
            {
                Assert.True(image.Width <= 100);
                Assert.True(image.Height <= 130);
            }

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                _ = await PdfHelper.WriteImageCollection(
                    images, TempFolder, $"{DateTime.Now.Ticks}_Thumbnails_CustomDims");
            }
#endif
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    [Fact]
    public async Task RasterizeToThumbnails_UsesPropertyMaxDimensions()
    {
        var pdfBytes = CreateSamplePdfBytes(1, jokeIndex: 8);
        _rasterizer.ThumbnailMaxDimensions = new ThumbnailMaxDimensions(80, 100);
        var images = await _rasterizer.RasterizeToThumbnails(pdfBytes, 
            cancellationToken: CancellationToken.None);

        try
        {
            images.Count.Should().Be(1);
            Assert.True(images[0].Width <= 80);
            Assert.True(images[0].Height <= 100);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                _ = await PdfHelper.WriteImageCollection(
                    images, TempFolder, $"{DateTime.Now.Ticks}_Thumbnails_PropertyDims");
            }
#endif
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    [Fact]
    public async Task RasterizeToThumbnails_FromPdfDocument_ReturnsAllPages()
    {
        using var doc = CreateSamplePdf(3, jokeIndex: 9);
        var images = await _rasterizer.RasterizeToThumbnails(doc,
            cancellationToken: CancellationToken.None);

        try
        {
            images.Count.Should().Be(3);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                _ = await PdfHelper.WriteImageCollection(
                    images, TempFolder, $"{DateTime.Now.Ticks}_Thumbnails_FromPdfDocument");
            }
#endif
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    [Fact]
    public async Task RasterizeToThumbnails_WithPageNumbers_ReturnsSubset()
    {
        var pdfBytes = CreateSamplePdfBytes(4, jokeIndex: 0);
        var images = await _rasterizer.RasterizeToThumbnails(pdfBytes, pageNumbers: [2, 4],
            cancellationToken: CancellationToken.None);

        try
        {
            images.Count.Should().Be(2);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                _ = await PdfHelper.WriteImageCollection(
                    images, TempFolder, $"{DateTime.Now.Ticks}_Thumbnails_PageSubset");
            }
#endif
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    #endregion

    #region | RasterizeToThumbnail tests |

    [Fact]
    public async Task RasterizeToThumbnail_SinglePage_FitsWithinMaxDimensions()
    {
        var pdfBytes = CreateSamplePdfBytes(2, jokeIndex: 1);
        using var image = await _rasterizer.RasterizeToThumbnail(pdfBytes, 1,
            cancellationToken: CancellationToken.None);

        Assert.True(image.Width <= 200);
        Assert.True(image.Height <= 260);
        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);

#if SAVE_TEMP_FILES
        if (Directory.Exists(TempFolder))
        {
            _ = await PdfHelper.WriteImage(
                image, TempFolder, $"{DateTime.Now.Ticks}_Thumbnail_SinglePage");
        }
#endif
    }

    [Fact]
    public async Task RasterizeToThumbnail_FromPdfDocument_ReturnsImage()
    {
        using var doc = CreateSamplePdf(2, jokeIndex: 2);
        using var image = await _rasterizer.RasterizeToThumbnail(doc, 1,
            cancellationToken: CancellationToken.None);

        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);
        Assert.True(image.Width <= 200);
        Assert.True(image.Height <= 260);

#if SAVE_TEMP_FILES
        if (Directory.Exists(TempFolder))
        {
            _ = await PdfHelper.WriteImage(
                image, TempFolder, $"{DateTime.Now.Ticks}_Thumbnail_FromPdfDocument");
        }
#endif
    }

    [Fact]
    public async Task RasterizeToThumbnail_FromStream_ReturnsImage()
    {
        using var stream = CreateSamplePdfStream(1, jokeIndex: 3);
        using var image = await _rasterizer.RasterizeToThumbnail(stream, 1,
            cancellationToken: CancellationToken.None);

        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);

#if SAVE_TEMP_FILES
        if (Directory.Exists(TempFolder))
        {
            _ = await PdfHelper.WriteImage(
                image, TempFolder, $"{DateTime.Now.Ticks}_Thumbnail_FromStream");
        }
#endif
    }

    #endregion

    #region | RasterizeToThumbnailFiles tests |

    [Fact]
    public async Task RasterizeToThumbnailFiles_CreatesFiles()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            var pdfBytes = CreateSamplePdfBytes(2, jokeIndex: 4);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToThumbnailFiles(pdfBytes, outputDirectory: outputDir,
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(2);

            foreach (var file in files)
            {
                Assert.True(new FileInfo(file).Length > 0);
            }

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                foreach (var file in files)
                {
                    File.Copy(file,
                        Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ThumbnailFiles_{Path.GetFileName(file)}"),
                        overwrite: true);
                }
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToThumbnailFiles_FromPdfDocument_CreatesFiles()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            using var doc = CreateSamplePdf(2, jokeIndex: 5);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToThumbnailFiles(doc, outputDirectory: outputDir,
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(2);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                foreach (var file in files)
                {
                    File.Copy(file,
                        Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ThumbnailFiles_FromPdfDoc_{Path.GetFileName(file)}"),
                        overwrite: true);
                }
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    #endregion

    #region | RasterizeToThumbnailFile tests |

    [Fact]
    public async Task RasterizeToThumbnailFile_CreatesSingleFile()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            var pdfBytes = CreateSamplePdfBytes(2, jokeIndex: 6);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToThumbnailFile(pdfBytes, 1, outputDirectory: outputDir,
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);
            Assert.True(new FileInfo(files[0]).Length > 0);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                File.Copy(files[0],
                    Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ThumbnailFile_Single_{Path.GetFileName(files[0])}"),
                    overwrite: true);
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToThumbnailFile_FromPdfDocument_CreatesSingleFile()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            using var doc = CreateSamplePdf(2, jokeIndex: 7);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToThumbnailFile(doc, 1, outputDirectory: outputDir,
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                File.Copy(files[0],
                    Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ThumbnailFile_FromPdfDoc_{Path.GetFileName(files[0])}"),
                    overwrite: true);
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    [Fact]
    public async Task RasterizeToThumbnailFile_FromStream_CreatesSingleFile()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            using var stream = CreateSamplePdfStream(2, jokeIndex: 8);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToThumbnailFile(stream, 1, outputDirectory: outputDir,
                cancellationToken: CancellationToken.None);

            var files = Directory.GetFiles(outputDir);
            files.Length.Should().Be(1);

#if SAVE_TEMP_FILES
            if (Directory.Exists(TempFolder))
            {
                File.Copy(files[0],
                    Path.Combine(TempFolder, $"{DateTime.Now.Ticks}_ThumbnailFile_FromStream_{Path.GetFileName(files[0])}"),
                    overwrite: true);
            }
#endif
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    #endregion

    #region | CancellationToken tests |

    [Fact]
    public async Task RasterizeToImages_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var pdfBytes = CreateSamplePdfBytes(1);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _rasterizer.RasterizeToImages(pdfBytes, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task RasterizeToImage_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var pdfBytes = CreateSamplePdfBytes(1);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _rasterizer.RasterizeToImage(pdfBytes, 1, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task GetPageCount_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var pdfBytes = CreateSamplePdfBytes(1);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _rasterizer.GetPageCount(pdfBytes, cancellationToken: cts.Token));
    }

    #endregion

    #region | BackgroundColor tests |

    [Fact]
    public async Task RasterizeToImage_WithCustomBackgroundColor_ProducesImage()
    {
        var pdfBytes = CreateSamplePdfBytes(1, jokeIndex: 9);
        _rasterizer.BackgroundColor = 0xFFFF0000; // Opaque red background
        using var image = await _rasterizer.RasterizeToImage(pdfBytes, 1, dpi: 72, 
            cancellationToken: CancellationToken.None);

        Assert.True(image.Width > 0);
        Assert.True(image.Height > 0);

#if SAVE_TEMP_FILES
        if (Directory.Exists(TempFolder))
        {
            _ = await PdfHelper.WriteImage(
                image, TempFolder, $"{DateTime.Now.Ticks}_RasterizeToImage_RedBackground");
        }
#endif
    }

    #endregion

    #region | AllowOverwriteFiles tests |

    [Fact]
    public async Task RasterizeToImageFiles_OverwriteDisabled_ThrowsIOException()
    {
        var outputDir = CreateTempOutputDirectory();

        try
        {
            var pdfBytes = CreateSamplePdfBytes(1);
            _rasterizer.AllowOverwriteFiles = true;
            await _rasterizer.RasterizeToImageFiles(pdfBytes, outputDir, 
                cancellationToken: CancellationToken.None);

            // Second call with overwrite disabled should throw
            _rasterizer.AllowOverwriteFiles = false;
            await Assert.ThrowsAsync<IOException>(
                () => _rasterizer.RasterizeToImageFiles(pdfBytes, outputDir, 
                    cancellationToken: CancellationToken.None));
        }
        finally
        {
            CleanupDirectory(outputDir);
        }
    }

    #endregion
}
