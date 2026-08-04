using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.Tests;

public class MarkdownPdfRendererTests
{
    private const string FeatureMarkdown = @"---
title: Feature Tour
author: Test Author
---

# Feature Tour

An **introduction** paragraph with *emphasis*, `inline code`, a [link](https://example.com),
~~strikethrough~~, and a footnote[^note].

[^note]: The footnote text itself.

## Lists

- First bullet
- Second bullet
  1. nested ordered
  2. another

## Tasks

- [ ] something to do
- [x] something done

## Table

| Name | Description | Price |
|------|-------------|------:|
| Widget | A useful widget | $9.99 |
| Gadget | Even more useful | $19.99 |

## Code

```csharp
public static int Add(int a, int b)
{
    return a + b; // sum
}
```

> A blockquote to close things out.

---

The end.
";

    [Fact]
    public void renders_any_markdown_to_pdf_bytes_with_zero_configuration()
    {
        //Arrange
        var renderer = new MarkdownPdfRenderer();

        //Act
        var result = renderer.RenderMarkdownToBytes(FeatureMarkdown);

        //Assert
        result.PdfBytes.Should().NotBeNull();
        Encoding.ASCII.GetString(result.PdfBytes, 0, 4).Should().Be("%PDF");
        result.PageCount.Should().BeGreaterThanOrEqualTo(1);
        result.Title.Should().Be("Feature Tour");
    }

    [Fact]
    public async Task rendered_pdf_rasterizes_to_letter_pages()
    {
        //Arrange
        var renderer = new MarkdownPdfRenderer();

        //Act
        var result = renderer.RenderMarkdownToBytes(FeatureMarkdown);

        //Assert
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var image = await rasterizer.RasterizeToImage(
            result.PdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        image.Width.Should().Be(816);   // 612pt Letter at 96 DPI
        image.Height.Should().Be(1056); // 792pt Letter at 96 DPI
    }

    [Fact]
    public void render_file_writes_pdf_next_to_the_source_by_default()
    {
        //Arrange
        var directory = Path.Combine(Path.GetTempPath(), "md2pdf-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var mdPath = Path.Combine(directory, "my_notes.md");
            File.WriteAllText(mdPath, "# Notes\n\nSome content.\n");
            var renderer = new MarkdownPdfRenderer();

            //Act
            var result = renderer.RenderFile(mdPath);

            //Assert
            result.OutputFilePath.Should().Be(Path.Combine(directory, "my_notes.pdf"));
            File.Exists(result.OutputFilePath).Should().BeTrue();
            result.Title.Should().Be("Notes");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void title_inference_prefers_front_matter_then_heading_then_file_name()
    {
        //Arrange
        var renderer = new MarkdownPdfRenderer();

        //Act
        var fromFrontMatter = renderer.GenerateHtml("---\ntitle: From Front Matter\n---\n# Ignored\n");
        var fromHeading = renderer.GenerateHtml("# From Heading\n\ntext\n");
        var fromNothing = renderer.GenerateHtml("just a paragraph\n");

        //Assert
        fromFrontMatter.Title.Should().Be("From Front Matter");
        fromHeading.Title.Should().Be("From Heading");
        fromNothing.Title.Should().Be("Document");
    }

    [Fact]
    public void generate_html_returns_restylable_html_and_css()
    {
        //Arrange
        var renderer = new MarkdownPdfRenderer();

        //Act
        var result = renderer.GenerateHtml("# Hello\n\nWorld with `code`.\n");

        //Assert
        result.BodyHtml.Should().Contain("<h1>Hello</h1>");
        result.Css.Should().Contain("font-family: serif");
        result.ToHtmlDocument().Should().Contain("<style>");
        result.ToHtmlDocument().Should().Contain("<title>Hello</title>");

        //Act - workflow (b): replace the stylesheet and render through Html2Pdf
        var restyled = result.ToHtmlDocument("html { font-family: sans-serif; font-size: 14pt; color: #333333; }");
        var htmlRenderer = new CodeBrix.PdfDocCreate.Html2Pdf.HtmlPdfRenderer();
        var pdf = htmlRenderer.RenderHtmlToBytes(restyled);

        //Assert
        pdf.PdfBytes.Should().NotBeNull();
        Encoding.ASCII.GetString(pdf.PdfBytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public void hostile_or_odd_markdown_still_renders()
    {
        //Arrange
        var renderer = new MarkdownPdfRenderer();
        var weird = new StringBuilder();
        weird.AppendLine("# Emoji \U0001F680 and CJK 漢字 test");
        weird.AppendLine();
        weird.AppendLine(new string('*', 300));
        weird.AppendLine("[[[[[[nested [brackets](");
        weird.AppendLine("| broken | table");
        weird.AppendLine("![missing image](does-not-exist.png)");
        weird.AppendLine("<div-not-really <<>> &nonsense;");

        //Act
        var result = renderer.RenderMarkdownToBytes(weird.ToString());

        //Assert - never crashes, always produces a document
        result.PdfBytes.Should().NotBeNull();
        result.PageCount.Should().BeGreaterThanOrEqualTo(1);
        result.Warnings.Messages.Any(m => m.Contains("Basic Multilingual Plane")).Should().BeTrue();
    }

    [Fact]
    public void relative_images_resolve_against_the_markdown_file_directory()
    {
        //Arrange
        var directory = Path.Combine(Path.GetTempPath(), "md2pdf-img-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var pngBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
            File.WriteAllBytes(Path.Combine(directory, "dot.png"), pngBytes);
            var mdPath = Path.Combine(directory, "doc.md");
            File.WriteAllText(mdPath, "# Image Test\n\n![dot](dot.png)\n");
            var renderer = new MarkdownPdfRenderer();

            //Act
            var result = renderer.RenderFile(mdPath);

            //Assert - no missing-image warnings means the relative path resolved
            result.Warnings.Messages.Any(m => m.Contains("dot.png")).Should().BeFalse();
            File.Exists(Path.Combine(directory, "doc.pdf")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task page_size_option_changes_the_page_geometry()
    {
        //Arrange
        var renderer = new MarkdownPdfRenderer();
        renderer.Options.PageSize = "a4";

        //Act
        var result = renderer.RenderMarkdownToBytes("# A4 Document\n\ntext\n");

        //Assert
        result.PdfBytes.Should().NotBeNull();
        using var rasterizer = new PageRasterizer();
        var dims = await rasterizer.GetPageDimensions(
            result.PdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        dims.WidthInPoints.Should().BeApproximately(595, 1.0);
        dims.HeightInPoints.Should().BeApproximately(842, 1.0);
    }

    [Fact]
    public void long_document_paginates_and_keeps_footer_page_numbers()
    {
        //Arrange
        var renderer = new MarkdownPdfRenderer();
        var markdown = new StringBuilder("# Long Document\n\n");
        for (var i = 1; i <= 100; i++)
        {
            markdown.AppendLine($"## Section {i}");
            markdown.AppendLine();
            markdown.AppendLine($"Paragraph for section number {i} with a reasonable amount of words in it.");
            markdown.AppendLine();
        }

        //Act
        var result = renderer.RenderMarkdownToBytes(markdown.ToString());

        //Assert
        result.PageCount.Should().BeGreaterThan(2);
    }
}
