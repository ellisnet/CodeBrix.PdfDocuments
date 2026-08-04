using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

public class HtmlPdfRendererTests
{
    // A 1x1 red pixel PNG.
    private const string TinyPngDataUri =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    [Fact]
    public void renders_simple_html_to_pdf_bytes()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        const string html = "<html><head><title>Smoke Test</title></head>" +
                            "<body><h1>Hello</h1><p>First paragraph with <strong>bold</strong> and <em>italic</em>.</p></body></html>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.PdfBytes.Should().NotBeNull();
        Encoding.ASCII.GetString(result.PdfBytes, 0, 4).Should().Be("%PDF");
        result.PageCount.Should().Be(1);
        result.Title.Should().Be("Smoke Test");
    }

    [Fact]
    public async Task renders_full_feature_document_and_rasterizes()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        renderer.Options.FooterText = "Page {page} of {pages}";
        var html = @"<html><head><title>Feature Document</title>
<style>
  h1 { color: #223a5e; }
  .callout { background-color: #fff8e1; border: 1pt solid #e0c97f; padding: 6pt; }
  td.num { text-align: right; }
</style></head>
<body>
  <h1>Feature Document</h1>
  <p>Intro paragraph with a <a href='https://example.com'>web link</a>, some <code>inline code</code>,
     H<sub>2</sub>O and x<sup>2</sup>.</p>
  <div class='callout'><p>A boxed callout with <strong>bold</strong> content.</p></div>
  <h2 id='lists'>Lists</h2>
  <ul>
    <li>First bullet</li>
    <li>Second bullet with nested list
      <ol><li>alpha</li><li>beta</li></ol>
    </li>
  </ul>
  <h2>Table</h2>
  <table>
    <thead><tr><th>Name</th><th>Description</th><th class='num'>Price</th></tr></thead>
    <tbody>
      <tr><td>Widget</td><td>A useful widget for many tasks</td><td class='num'>$9.99</td></tr>
      <tr><td colspan='2'>Spanning cell</td><td class='num'>$0.00</td></tr>
      <tr><td rowspan='2'>Tall cell</td><td>Row A</td><td class='num'>$1.00</td></tr>
      <tr><td>Row B</td><td class='num'>$2.00</td></tr>
    </tbody>
  </table>
  <h2>Code</h2>
  <pre>line one
    indented line two
line three</pre>
  <blockquote><p>A quotation that spans a couple of lines to prove the box works.</p></blockquote>
  <hr>
  <p><img src='" + TinyPngDataUri + @"' width='24' height='24' alt='dot'> After the image.</p>
</body></html>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.PageCount.Should().BeGreaterThanOrEqualTo(1);

        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var image = await rasterizer.RasterizeToImage(
            result.PdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        image.Width.Should().Be(816);   // 612pt at 96 DPI
        image.Height.Should().Be(1056); // 792pt at 96 DPI
    }

    [Fact]
    public async Task page_rule_overrides_page_size_and_margins()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        const string html = "<html><head><style>@page { size: a5; margin: 36pt; }</style></head>" +
                            "<body><p>A5 content</p></body></html>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        using var rasterizer = new PageRasterizer();
        var dims = await rasterizer.GetPageDimensions(
            result.PdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        dims.WidthInPoints.Should().BeApproximately(420, 1.0);
        dims.HeightInPoints.Should().BeApproximately(595, 1.0);
    }

    [Fact]
    public void unsupported_css_collects_warnings_but_render_succeeds()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        const string html = "<html><head><style>p { float: left; transform: rotate(3deg); }</style></head>" +
                            "<body><p>Content survives.</p></body></html>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Messages.Any(m => m.Contains("float")).Should().BeTrue();
        result.Warnings.Messages.Any(m => m.Contains("transform")).Should().BeTrue();
    }

    [Fact]
    public void missing_image_is_a_warning_not_an_error()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        const string html = "<body><p>Before</p><img src='no-such-file.png' alt='gone'><p>After</p></body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Messages.Any(m => m.Contains("no-such-file.png")).Should().BeTrue();
    }

    [Fact]
    public void emoji_degrades_to_a_warning_instead_of_tofu()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        const string html = "<body><p>Launch \U0001F680 complete</p></body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Messages.Any(m => m.Contains("Basic Multilingual Plane")).Should().BeTrue();
    }

    [Fact]
    public void long_document_paginates()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        var body = new StringBuilder("<body>");
        for (var i = 1; i <= 120; i++)
        {
            body.Append($"<p>Paragraph number {i} with enough words to take up a reasonable amount of horizontal space.</p>");
        }
        body.Append("</body>");

        //Act
        var result = renderer.RenderHtmlToBytes(body.ToString());

        //Assert
        result.PageCount.Should().BeGreaterThan(1);
    }

    [Fact]
    public void page_break_before_starts_a_new_page()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        const string html = "<html><head><style>.break { page-break-before: always; }</style></head>" +
                            "<body><p>Page one</p><p class='break'>Page two</p></body></html>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.PageCount.Should().Be(2);
    }

    [Fact]
    public void renders_file_with_linked_stylesheet_and_relative_image()
    {
        //Arrange
        var directory = Path.Combine(Path.GetTempPath(), "html2pdf-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "style.css"), "h1 { color: #7a1f1f; }");
            var pngBytes = Convert.FromBase64String(TinyPngDataUri.Substring(TinyPngDataUri.IndexOf(',') + 1));
            File.WriteAllBytes(Path.Combine(directory, "dot.png"), pngBytes);
            var htmlPath = Path.Combine(directory, "doc.html");
            File.WriteAllText(htmlPath,
                "<html><head><title>Linked</title><link rel='stylesheet' href='style.css'></head>" +
                "<body><h1>Heading</h1><p><img src='dot.png' width='16'></p></body></html>");
            var outputPath = Path.Combine(directory, "doc.pdf");
            var renderer = new HtmlPdfRenderer();

            //Act
            var result = renderer.RenderFile(htmlPath, outputPath);

            //Assert
            File.Exists(outputPath).Should().BeTrue();
            result.OutputFilePath.Should().Be(Path.GetFullPath(outputPath));
            result.Warnings.Count.Should().Be(0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
