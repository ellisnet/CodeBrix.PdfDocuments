using System;
using System.IO;
using CodeBrix.Imaging;
using CodeBrix.Imaging.PixelFormats;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

/// <summary>
/// File references inside documents must resolve identically on Windows, macOS and
/// Linux regardless of which separator style the document author used. These tests
/// create their own fixture files, so they run on every operating system unchanged.
/// </summary>
public class CrossPlatformPathTests
{
    private static string CreateFixtureDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "html2pdf-paths-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteRedPng(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using var image = new Image<Rgba32>(8, 8);
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                image[x, y] = new Rgba32(255, 0, 0);
            }
        }

        using var fs = File.Create(path);
        image.SaveAsPng(fs);
    }

    private static HtmlRenderResult Render(string html, string baseDirectory)
        => new HtmlPdfRenderer().RenderHtmlToBytes(html, baseDirectory);

    [Fact]
    public void forward_slash_relative_image_resolves_in_subdirectory()
    {
        //Arrange
        var directory = CreateFixtureDirectory();
        try
        {
            WriteRedPng(Path.Combine(directory, "sub", "dot.png"));

            //Act
            var result = Render("<body><img src='sub/dot.png'></body>", directory);

            //Assert
            result.Warnings.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void backslash_relative_image_resolves_on_every_platform()
    {
        //Arrange
        var directory = CreateFixtureDirectory();
        try
        {
            WriteRedPng(Path.Combine(directory, "sub", "dot.png"));

            //Act
            var result = Render("<body><img src='sub\\dot.png'></body>", directory);

            //Assert
            result.Warnings.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void backslash_stylesheet_href_resolves_on_every_platform()
    {
        //Arrange
        var directory = CreateFixtureDirectory();
        try
        {
            var cssPath = Path.Combine(directory, "styles", "style.css");
            Directory.CreateDirectory(Path.GetDirectoryName(cssPath));
            File.WriteAllText(cssPath, "h1 { color: #7a1f1f; }");
            var html = "<html><head><link rel='stylesheet' href='styles\\style.css'></head>" +
                       "<body><h1>Heading</h1></body></html>";

            //Act
            var result = Render(html, directory);

            //Assert
            result.Warnings.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void percent_encoded_image_name_resolves()
    {
        //Arrange
        var directory = CreateFixtureDirectory();
        try
        {
            WriteRedPng(Path.Combine(directory, "my dot.png"));

            //Act
            var result = Render("<body><img src='my%20dot.png'></body>", directory);

            //Assert
            result.Warnings.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void percent_encoded_name_in_backslash_subdirectory_resolves()
    {
        //Arrange
        var directory = CreateFixtureDirectory();
        try
        {
            WriteRedPng(Path.Combine(directory, "sub", "my dot.png"));

            //Act
            var result = Render("<body><img src='sub\\my%20dot.png'></body>", directory);

            //Assert
            result.Warnings.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void absolute_native_path_image_resolves()
    {
        //Arrange
        var directory = CreateFixtureDirectory();
        try
        {
            var imagePath = Path.Combine(directory, "dot.png");
            WriteRedPng(imagePath);

            //Act - a rooted path in the running platform's own style.
            var result = Render($"<body><img src='{imagePath}'></body>", baseDirectory: null);

            //Assert
            result.Warnings.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void parent_directory_traversal_resolves_with_both_separator_styles()
    {
        //Arrange - the document lives in a subdirectory and reaches a sibling directory.
        var directory = CreateFixtureDirectory();
        try
        {
            var docDirectory = Path.Combine(directory, "docs");
            Directory.CreateDirectory(docDirectory);
            WriteRedPng(Path.Combine(directory, "shared", "dot.png"));

            //Act
            var forward = Render("<body><img src='../shared/dot.png'></body>", docDirectory);
            var backward = Render("<body><img src='..\\shared\\dot.png'></body>", docDirectory);

            //Assert
            forward.Warnings.Count.Should().Be(0);
            backward.Warnings.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void missing_file_still_warns_with_the_reference_as_written()
    {
        //Arrange
        var directory = CreateFixtureDirectory();
        try
        {
            //Act
            var result = Render("<body><img src='nowhere\\missing.png' alt='gone'></body>", directory);

            //Assert
            result.Warnings.Count.Should().Be(1);
            result.Warnings.Messages[0].Should().Contain("missing.png");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
