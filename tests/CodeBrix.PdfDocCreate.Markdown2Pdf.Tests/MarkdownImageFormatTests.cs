using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeBrix.Imaging;
using CodeBrix.Imaging.Formats.Pbm;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.Tests;

public class MarkdownImageFormatTests
{
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

    [Theory]
    [MemberData(nameof(SupportedFormats))]
    public void relative_image_file_renders_without_warnings_in_every_supported_format(string extension, string mimeType)
    {
        //Arrange
        _ = mimeType;
        var directory = Path.Combine(Path.GetTempPath(), "md2pdf-imgfmt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var fileName = "sample." + extension;
            File.WriteAllBytes(Path.Combine(directory, fileName), EncodeRedSquare(extension));
            var markdownPath = Path.Combine(directory, "doc.md");
            File.WriteAllText(markdownPath, $"# Image test\n\n![sample image]({fileName})\n");
            var renderer = new MarkdownPdfRenderer();

            //Act
            var result = renderer.RenderFile(markdownPath);

            //Assert
            result.Warnings.Messages.Any(m => m.Contains(fileName)).Should().BeFalse();
            File.Exists(Path.Combine(directory, "doc.pdf")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [MemberData(nameof(SupportedFormats))]
    public void data_uri_image_renders_without_warnings_in_every_supported_format(string extension, string mimeType)
    {
        //Arrange
        var dataUri = $"data:{mimeType};base64," + Convert.ToBase64String(EncodeRedSquare(extension));
        var renderer = new MarkdownPdfRenderer();
        var markdown = $"![sample image]({dataUri})\n";

        //Act
        var result = renderer.RenderMarkdownToBytes(markdown);

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Messages.Any(m => m.Contains("[image]")).Should().BeFalse();
    }

    [Theory]
    [InlineData("data:image/bmp;base64,QU1Q")]
    [InlineData("data:image/tiff;base64,VElGRg==")]
    [InlineData("data:image/x-tga;base64,VEdB")]
    [InlineData("data:image/x-targa;base64,VEdB")]
    [InlineData("data:image/x-portable-pixmap;base64,UDY=")]
    [InlineData("data:image/x-portable-graymap;base64,UDU=")]
    [InlineData("data:image/x-portable-bitmap;base64,UDQ=")]
    [InlineData("data:image/x-portable-anymap;base64,UDc=")]
    [InlineData("data:image/x-windows-bmp;base64,QU1Q")]
    [InlineData("data:image/svg+xml;base64,PHN2Zy8+")]
    [InlineData("data:image/svg+xml,%3Csvg%2F%3E")]
    public void parser_admits_data_uris_for_the_full_supported_format_set(string dataUri)
    {
        //Arrange
        var parser = new MarkdownParser();

        //Act
        var html = parser.Render($"![x]({dataUri})");

        //Assert
        html.Should().Contain("src=\"data:image/");
        html.Should().NotContain("src=\"\"");
    }

    [Fact]
    public void referenced_svg_file_renders_without_warnings()
    {
        //Arrange
        const string redSquareSvg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'>" +
            "<rect width='32' height='32' fill='#ff0000'/></svg>";
        var directory = Path.Combine(Path.GetTempPath(), "md2pdf-svg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "note.svg"), redSquareSvg);
            var markdownPath = Path.Combine(directory, "doc.md");
            File.WriteAllText(markdownPath, "# SVG\n\n![note](note.svg)\n");
            var renderer = new MarkdownPdfRenderer();

            //Act
            var result = renderer.RenderFile(markdownPath);

            //Assert
            result.Warnings.Messages.Any(m => m.Contains("note.svg")).Should().BeFalse();
            File.Exists(Path.Combine(directory, "doc.pdf")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void svg_data_uri_renders_without_warnings()
    {
        //Arrange
        const string redSquareSvg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'>" +
            "<rect width='32' height='32' fill='#ff0000'/></svg>";
        var dataUri = "data:image/svg+xml;base64," +
                      Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(redSquareSvg));
        var renderer = new MarkdownPdfRenderer();

        //Act
        var result = renderer.RenderMarkdownToBytes($"![note]({dataUri})\n");

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Messages.Any(m => m.Contains("[image]")).Should().BeFalse();
    }

    [Fact]
    public void inline_svg_html_block_renders_without_warnings()
    {
        //Arrange - raw HTML blocks pass through markdown-it and hit Html2Pdf's svg path.
        const string redSquareSvg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'>" +
            "<rect width='32' height='32' fill='#ff0000'/></svg>";
        var renderer = new MarkdownPdfRenderer();

        //Act
        var result = renderer.RenderMarkdownToBytes($"Before\n\n{redSquareSvg}\n\nAfter\n");

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Messages.Any(m => m.Contains("[image]")).Should().BeFalse();
        result.Warnings.Messages.Any(m => m.Contains("svg")).Should().BeFalse();
    }

    [Fact]
    public void backslash_relative_image_path_resolves_on_every_platform()
    {
        //Arrange
        var directory = Path.Combine(Path.GetTempPath(), "md2pdf-paths-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "sub"));
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "sub", "dot.png"), EncodeRedSquare("png"));
            var markdownPath = Path.Combine(directory, "doc.md");
            File.WriteAllText(markdownPath, "![dot](sub\\dot.png)\n");
            var renderer = new MarkdownPdfRenderer();

            //Act
            var result = renderer.RenderFile(markdownPath);

            //Assert
            result.Warnings.Messages.Any(m => m.Contains("dot.png")).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("vbscript:msgbox")]
    [InlineData("file:///etc/passwd")]
    [InlineData("data:text/html;base64,PGh0bWw+")]
    [InlineData("data:application/octet-stream;base64,AAAA")]
    [InlineData("data:image/unknown-format;base64,AAAA")]
    public void parser_still_rejects_unsafe_or_unknown_data_uris(string href)
    {
        //Arrange
        var parser = new MarkdownParser();

        //Act
        var html = parser.Render($"![x]({href})");

        //Assert - a rejected destination stops the construct from parsing as an
        // image at all; it renders as literal text.
        html.Should().NotContain("<img");
    }
}
