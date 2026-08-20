using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeBrix.Imaging;
using CodeBrix.Imaging.Formats.Pbm;
using CodeBrix.Imaging.Formats.Tga;
using CodeBrix.Imaging.Formats.Webp;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

public class ImageFormatSupportTests
{
    // Every format CodeBrix.Imaging can decode must embed through Html2Pdf.
    public static IEnumerable<object[]> SupportedFormats =>
        new[]
        {
            new object[] { "bmp", "image/bmp" },
            new object[] { "gif", "image/gif" },
            new object[] { "jpg", "image/jpeg" },
            new object[] { "ppm", "image/x-portable-pixmap" },
            new object[] { "png", "image/png" },
            new object[] { "tga", "image/x-tga" },
            new object[] { "tif", "image/tiff" },
            new object[] { "webp", "image/webp" },
        };

    private static byte[] EncodeRedSquare(string extension)
    {
        using var image = new Image<Rgba32>(16, 16);
        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 16; x++)
            {
                image[x, y] = new Rgba32(255, 0, 0);
            }
        }

        using var ms = new MemoryStream();
        switch (extension)
        {
            case "bmp": image.SaveAsBmp(ms); break;
            case "gif": image.SaveAsGif(ms); break;
            case "jpg": image.SaveAsJpeg(ms); break;
            case "ppm": image.SaveAsPbm(ms, new PbmEncoder { ColorType = PbmColorType.Rgb }); break;
            case "png": image.SaveAsPng(ms); break;
            case "tga": image.SaveAsTga(ms); break;
            case "tif": image.SaveAsTiff(ms); break;
            case "webp": image.SaveAsWebp(ms); break;
            default: throw new ArgumentException($"Unknown extension '{extension}'.", nameof(extension));
        }

        return ms.ToArray();
    }

    private static async Task<bool> RasterHasContentPixel(byte[] pdfBytes)
    {
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var raster = await rasterizer.RasterizeToImage(
            pdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        using var rgba = raster.CloneAs<Rgba32>();
        for (var y = 0; y < rgba.Height; y++)
        {
            for (var x = 0; x < rgba.Width; x++)
            {
                var pixel = rgba[x, y];
                if (pixel.R < 230 || pixel.G < 230 || pixel.B < 230)
                {
                    return true;
                }
            }
        }

        return false;
    }

    [Theory]
    [MemberData(nameof(SupportedFormats))]
    public async Task renders_local_image_file_in_every_supported_format(string extension, string mimeType)
    {
        //Arrange
        _ = mimeType;
        var directory = Path.Combine(Path.GetTempPath(), "html2pdf-imgfmt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "sample." + extension), EncodeRedSquare(extension));
            var renderer = new HtmlPdfRenderer();
            var html = $"<body><img src='sample.{extension}'></body>";

            //Act
            var result = renderer.RenderHtmlToBytes(html, directory);

            //Assert
            result.PdfBytes.Should().NotBeNull();
            result.Warnings.Count.Should().Be(0);
            (await RasterHasContentPixel(result.PdfBytes)).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(SupportedFormats))]
    public async Task renders_data_uri_image_in_every_supported_format(string extension, string mimeType)
    {
        //Arrange
        var dataUri = $"data:{mimeType};base64," + Convert.ToBase64String(EncodeRedSquare(extension));
        var renderer = new HtmlPdfRenderer();
        var html = $"<body><img src='{dataUri}'></body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Count.Should().Be(0);
        (await RasterHasContentPixel(result.PdfBytes)).Should().BeTrue();
    }

    [Theory]
    [InlineData("png")]
    [InlineData("tga")]
    [InlineData("webp")]
    public async Task alpha_capable_formats_keep_transparency_through_the_pdf(string extension)
    {
        //Arrange - left half transparent, right half red; if the alpha channel were
        // dropped (the old JPEG re-encode path) the transparent half would render black.
        // TIFF is excluded only because this Imaging fork cannot ENCODE tiff alpha;
        // decoding an alpha tiff still takes the lossless path.
        using var image = new Image<Rgba32>(16, 16);
        for (var y = 0; y < 16; y++)
        {
            for (var x = 8; x < 16; x++)
            {
                image[x, y] = new Rgba32(255, 0, 0);
            }
        }

        using var ms = new MemoryStream();
        switch (extension)
        {
            case "png": image.SaveAsPng(ms); break;
            case "tga": image.SaveAsTga(ms, new TgaEncoder { BitsPerPixel = TgaBitsPerPixel.Pixel32 }); break;
            case "webp": image.SaveAsWebp(ms, new WebpEncoder { FileFormat = WebpFileFormatType.Lossless }); break;
        }

        var dataUri = "data:application/octet-stream;base64," + Convert.ToBase64String(ms.ToArray());
        var renderer = new HtmlPdfRenderer();
        var html = $"<body><img src='{dataUri}'></body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.Warnings.Count.Should().Be(0);
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var raster = await rasterizer.RasterizeToImage(
            result.PdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        using var rgba = raster.CloneAs<Rgba32>();
        var sawRed = false;
        var sawBlack = false;
        for (var y = 0; y < rgba.Height; y++)
        {
            for (var x = 0; x < rgba.Width; x++)
            {
                var pixel = rgba[x, y];
                if (pixel.R > 180 && pixel.G < 100 && pixel.B < 100) { sawRed = true; }
                if (pixel.R < 60 && pixel.G < 60 && pixel.B < 60) { sawBlack = true; }
            }
        }

        sawRed.Should().BeTrue();
        sawBlack.Should().BeFalse();
    }

    [Fact]
    public void image_only_warnings_stay_empty_for_mixed_format_document()
    {
        //Arrange - all eight formats side by side in one document.
        var parts = SupportedFormats
            .Select(row => (Extension: (string)row[0], MimeType: (string)row[1]))
            .Select(f => $"<p><img src='data:{f.MimeType};base64,{Convert.ToBase64String(EncodeRedSquare(f.Extension))}' width='12' height='12'></p>");
        var renderer = new HtmlPdfRenderer();
        var html = "<body>" + string.Concat(parts) + "</body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Count.Should().Be(0);
        result.PageCount.Should().Be(1);
    }
}
