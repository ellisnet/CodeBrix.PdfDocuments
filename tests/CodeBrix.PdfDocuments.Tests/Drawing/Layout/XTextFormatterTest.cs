using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Drawing.Layout;
using CodeBrix.PdfDocuments.Drawing.Layout.enums;
using CodeBrix.PdfDocuments.Fonts;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfDocuments.Tests.Helpers;
using SilverAssertions;
using System.IO;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Drawing.Layout; //Was previously: namespace PdfSharpCore.Test.Drawing.Layout;

public class XTextFormatterTest
{
    //We want to use a custom font for the tests in this class; so that the PDFs will be rendered the same way on all platforms
    private const string RobotoFamilyName = "Roboto";
    private const string RobotoRegularFaceName = "Roboto-Regular";
    private const string RobotoRegularResourceName = "CodeBrix.PdfDocuments.Tests.SampleFiles.Roboto-Regular.ttf";

    static XTextFormatterTest()
    {
        var robotoFontResolver = new EmbeddedFontResolver(
            fontFamilyName: RobotoFamilyName,
            fontFaceResources:
            [
                new EmbeddedResourceFontFace(FaceName: RobotoRegularFaceName, EmbeddedResourceName: RobotoRegularResourceName),
                //If we needed to use different embedded resources for Bold or Italic, we would do (step 1):
                //new EmbeddedResourceFontFace(FaceName: RobotoBoldFaceName, EmbeddedResourceName: RobotoBoldResourceName),
                //new EmbeddedResourceFontFace(FaceName: RobotoItalicFaceName, EmbeddedResourceName: RobotoItalicResourceName),

            ],
            fontEmbeddedResourceAssembly: typeof(XTextFormatterTest).Assembly);

        MetaFontResolver.Instance.RegisterFontResolver(RobotoRegularFaceName, robotoFontResolver);
        //And we need to register our resolver (again) for those different face names (step 2):
        //MetaFontResolver.Instance.RegisterFontResolver(RobotoBoldFaceName, robotoFontResolver);
        //MetaFontResolver.Instance.RegisterFontResolver(RobotoItalicFaceName, robotoFontResolver);
    }

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

    private static readonly string _outDir = "TestResults/XTextFormatterTest";
    private static readonly string _expectedImagesPath = Path.Combine("Drawing", "Layout");

    private PdfDocument _document;
    private XGraphics _renderer;
    private XTextFormatter _textFormatter;

    private static DiffOutput DiffPage(PdfDocument document, string filePrefix, int pageNum)
    {
        var rasterized = PdfHelper.Rasterize(document);
        var rasterizedFiles = PdfHelper.WriteImageCollection(rasterized.ImageCollection, _outDir, filePrefix);

#if SAVE_TEMP_FILES
        if (Directory.Exists(TempFolder))
        {
            _ = PdfHelper.WriteImageCollection(
                rasterized.ImageCollection, TempFolder, $"{System.DateTime.Now.Ticks}_{filePrefix}");
        }
#endif

        var expectedImagePath = PathHelper.GetInstance().GetAssetPath(_expectedImagesPath, $"{filePrefix}_{pageNum}.png");
        return PdfHelper.Diff(rasterizedFiles[pageNum-1], expectedImagePath, _outDir, filePrefix);
    }

    // Run before each test
    public XTextFormatterTest()
    {
        _document = new PdfDocument();
        var page = _document.AddPage();
        page.Size = PageSize.A6; // 295 x 417 pts
        _renderer = XGraphics.FromPdfPage(page);
        _textFormatter = new XTextFormatter(_renderer);
    }
        
    [Fact]
    public void DrawSingleLineString()
    {
        var layout = new XRect(12, 12, 200, 50);
        _textFormatter.DrawString("This is a simple single line test", new XFont(RobotoFamilyName, 12), XBrushes.Black, layout);

        var diffResult = DiffPage(_document, "DrawSingleLineString", 1);
            
        diffResult.DiffValue.Should().Be(0);
    }
        
    [Fact]
    public void DrawMultilineStringWithTruncate()
    {
        var layout = new XRect(12, 12, 200, 32);
        _renderer.DrawRectangle(XBrushes.LightGray, layout);
        _textFormatter.DrawString("This is text\nspanning 3 lines\nbut only space for 2", new XFont(RobotoFamilyName, 12), XBrushes.Black, layout);

        var diffResult = DiffPage(_document, "DrawMultilineStringWithTruncate", 1);
            
        diffResult.DiffValue.Should().Be(0);
    }
        
    [Fact]
    public void DrawMultiLineStringWithOverflow()
    {
        var layout = new XRect(12, 12, 200, 32);
        _renderer.DrawRectangle(XBrushes.LightGray, layout);
        _textFormatter.AllowVerticalOverflow = true;
        _textFormatter.DrawString("This is text\nspanning 3 lines\nand overflow shows all three", new XFont(RobotoFamilyName, 12), XBrushes.Black, layout);

        var diffResult = DiffPage(_document, "DrawMultiLineStringWithOverflow", 1);
            
        diffResult.DiffValue.Should().Be(0);
    }
        
    [Fact]
    public void DrawMultiLineStringsWithAlignment()
    {
        var layout1 = new XRect(12, 12, 200, 80);
        _renderer.DrawRectangle(XBrushes.LightGray, layout1);
        _textFormatter.DrawString("This is text\naligned to the top-left", new XFont(RobotoFamilyName, 12), XBrushes.Black, layout1);

        var layout2 = new XRect(12, 100, 200, 80);
        _renderer.DrawRectangle(XBrushes.LightGray, layout2);
        _textFormatter.SetAlignment(new TextFormatAlignment { Horizontal = XParagraphAlignment.Center, Vertical = XVerticalAlignment.Middle});
        _textFormatter.DrawString("This is text\naligned to the middle-center", new XFont(RobotoFamilyName, 12), XBrushes.Black, layout2);

        var layout3 = new XRect(12, 200, 200, 80);
        _renderer.DrawRectangle(XBrushes.LightGray, layout3);
        _textFormatter.SetAlignment(new TextFormatAlignment { Horizontal = XParagraphAlignment.Right, Vertical = XVerticalAlignment.Bottom});
        _textFormatter.DrawString("This is text\naligned to the bottom-right", new XFont(RobotoFamilyName, 12), XBrushes.Black, layout3);

        var diffResult = DiffPage(_document, "DrawMultiLineStringsWithAlignment", 1);
            
        diffResult.DiffValue.Should().Be(0);
    }
        
    [Fact]
    public void DrawMultiLineStringsWithLineHeight()
    {
        var font = new XFont(RobotoFamilyName, 12);

        var layout1 = new XRect(10, 10, 200, 80);
        _renderer.DrawRectangle(XBrushes.LightGray, layout1);
        _textFormatter.DrawString("This is text\naligned to the top-left\nand a custom line height", font, XBrushes.Black, layout1, 16);

        var layout2 = new XRect(10, 110, 200, 80);
        _renderer.DrawRectangle(XBrushes.LightGray, layout2);
        _textFormatter.SetAlignment(new TextFormatAlignment { Horizontal = XParagraphAlignment.Center, Vertical = XVerticalAlignment.Middle});
        _textFormatter.DrawString("This is text\naligned to the middle-center\nand a custom line height", font, XBrushes.Black, layout2, 16);

        var layout3 = new XRect(10, 210, 200, 80);
        _renderer.DrawRectangle(XBrushes.LightGray, layout3);
        _textFormatter.SetAlignment(new TextFormatAlignment { Horizontal = XParagraphAlignment.Right, Vertical = XVerticalAlignment.Bottom});
        _textFormatter.DrawString("This is text\naligned to the bottom-right\nand a custom line height", font, XBrushes.Black, layout3, 16);

        var layout4 = new XRect(10, 310, 200, 80);
        _renderer.DrawRectangle(XBrushes.LightGray, layout4);
        _textFormatter.SetAlignment(new TextFormatAlignment { Horizontal = XParagraphAlignment.Center, Vertical = XVerticalAlignment.Middle});
        _textFormatter.DrawString("This is text\nwith a very small\nline height", font, XBrushes.Black, layout4, 6);

        var diffResult = DiffPage(_document, "DrawMultiLineStringsWithLineHeight", 1);
            
        diffResult.DiffValue.Should().Be(0);
    }

    public enum CaptionPlacement
    {
        Above, //place the caption centered above the image
        Below, //place the caption centered below the image
    }

    [Theory]
#if (!TESTING_ON_LINUX) && (!TESTING_ON_MACOS)
    //These BMP and JPG test case files generate just fine on Linux and macOS, but
    //  they are imperceptibly different from how they are generated on Windows; so
    //  the test fails because they are not exact (like the PNG test case files are).
    [InlineData("test-image-01.bmp", CaptionPlacement.Above)]
    [InlineData("test-image-01.bmp", CaptionPlacement.Below)]
    [InlineData("test-image-01.jpg", CaptionPlacement.Below)]
    [InlineData("test-image-01.jpg", CaptionPlacement.Above)]
#endif
    [InlineData("test-image-01.png", CaptionPlacement.Above)]
    [InlineData("test-image-01.png", CaptionPlacement.Below)]
    public void DrawImageWithTextCaption(string sampleImageFilename, CaptionPlacement captionPlacement)
    {
        var font = new XFont(RobotoFamilyName, 8);
        var captionText = "Plate 1A: A beautiful scenery vista";

        var layout = new XRect(10, 10, 220, 150);
        _renderer.DrawRectangle(XBrushes.LightGray, layout);

        var filePrefix = $"{nameof(DrawImageWithTextCaption)}_from_{sampleImageFilename}_{nameof(CaptionPlacement)}_{captionPlacement}";

        // Load the embedded resource image
        var assembly = typeof(XTextFormatterTest).Assembly;
        var resourceName = $"CodeBrix.PdfDocuments.Tests.SampleFiles.{sampleImageFilename}";
        using var resourceStream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(resourceStream);

        // Copy to a byte array so XImage.FromStream can access it via its Func<Stream> factory
        using var ms = new MemoryStream();
        resourceStream.CopyTo(ms);
        var imageBytes = ms.ToArray();
        using var image = XImage.FromStream(() => new MemoryStream(imageBytes));

        // Measure caption text height
        var captionSize = _renderer.MeasureString(captionText, font);
        var captionHeight = captionSize.Height;

        // Scale image to 75% of layout width, preserving aspect ratio
        var targetWidth = layout.Width * 0.75;
        var scale = targetWidth / image.PointWidth;
        var drawWidth = image.PointWidth * scale;
        var drawHeight = image.PointHeight * scale;

        // If image + caption exceeds layout height, scale down further to fit
        if (drawHeight + captionHeight > layout.Height)
        {
            var availableImageHeight = layout.Height - captionHeight;
            scale = availableImageHeight / image.PointHeight;
            drawWidth = image.PointWidth * scale;
            drawHeight = image.PointHeight * scale;
        }

        // Left-align the image in the layout
        var imageX = layout.X;

        // Position image and caption based on placement
        double imageY, captionY;
        if (captionPlacement == CaptionPlacement.Above)
        {
            captionY = layout.Y;
            imageY = layout.Y + captionHeight;
        }
        else
        {
            imageY = layout.Y;
            captionY = layout.Y + drawHeight;
        }

        // Draw the image
        _renderer.DrawImage(image, imageX, imageY, drawWidth, drawHeight);

        // Draw the caption centered horizontally with respect to the image
        var captionRect = new XRect(layout.X, captionY, drawWidth, captionHeight);
        _textFormatter.SetAlignment(new TextFormatAlignment { Horizontal = XParagraphAlignment.Center, Vertical = XVerticalAlignment.Top });
        _textFormatter.DrawString(captionText, font, XBrushes.Black, captionRect);

        // Compare with reference image
        var diffResult = DiffPage(_document, filePrefix, 1);
        diffResult.DiffValue.Should().Be(0);
    }
}
