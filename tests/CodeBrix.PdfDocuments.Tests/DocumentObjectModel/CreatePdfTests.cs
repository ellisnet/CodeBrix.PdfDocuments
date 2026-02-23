using AngleSharp.Html.Parser;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocCreate.Rendering;
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfDocuments.Tests.Helpers;
using CodeBrix.PdfDocuments.Utils;
using SilverAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.DocumentObjectModel;

public class CreatePdfTests
{
    private readonly ITestOutputHelper _output;

    private readonly string _rootPath = PathHelper.GetInstance().RootDir;
    private const string OutputDirName = "Out";

    private const string WikipediaUrl = "https://en.wikipedia.org/wiki/Cuneiform";
    private const string WikipediaArticleTitle = "Cuneiform (writing system)";
    private const string WikipediaArticleSubject = "Wikipedia article on Cuneiform writing";
    private const string WikipediaArticleAuthor = "Wikipedia contributors";

    public CreatePdfTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    [Fact]
    public async Task CreateTestPdfFromOnlineArticle()
    {
        const string outName = "cuneiform.pdf";

        ValidateTargetAvailable(outName);

        // Fetch the Wikipedia article HTML
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "CodeBrix.PdfDocuments.Tests/1.0 (https://github.com/ellisnet/CodeBrix.PdfDocuments-private; test suite)");
        var html = await httpClient.GetStringAsync(WikipediaUrl, CancellationToken.None);

        // Parse the article content into structured elements
        var articleElements = ParseWikipediaArticle(html);
        Assert.NotEmpty(articleElements);

        // Build the CodeBrix.PdfDocCreate document
        var doc = new Document
        {
            Info =
            {
                Title = WikipediaArticleTitle,
                Subject = WikipediaArticleSubject,
                Author = WikipediaArticleAuthor
            }
        };

        // Define custom styles
        var titleStyle = doc.AddStyle("Title", "Normal");
        titleStyle.Font.Size = 24;
        titleStyle.Font.Bold = true;
        titleStyle.ParagraphFormat.SpaceAfter = 6;
        titleStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;

        var heading1Style = doc.Styles["Heading1"];
        heading1Style.Font.Size = 18;
        heading1Style.Font.Bold = true;
        heading1Style.ParagraphFormat.SpaceBefore = 12;
        heading1Style.ParagraphFormat.SpaceAfter = 6;
        heading1Style.ParagraphFormat.KeepWithNext = true;

        var heading2Style = doc.Styles["Heading2"];
        heading2Style.Font.Size = 14;
        heading2Style.Font.Bold = true;
        heading2Style.ParagraphFormat.SpaceBefore = 10;
        heading2Style.ParagraphFormat.SpaceAfter = 4;
        heading2Style.ParagraphFormat.KeepWithNext = true;

        var bodyStyle = doc.Styles["Normal"];
        bodyStyle.Font.Size = 10;
        bodyStyle.ParagraphFormat.SpaceAfter = 4;
        bodyStyle.ParagraphFormat.Alignment = ParagraphAlignment.Justify;

        var sourceStyle = doc.AddStyle("Source", "Normal");
        sourceStyle.Font.Size = 8;
        sourceStyle.Font.Italic = true;
        sourceStyle.ParagraphFormat.SpaceBefore = 12;
        sourceStyle.ParagraphFormat.Alignment = ParagraphAlignment.Left;

        var captionStyle = doc.AddStyle("Caption", "Normal");
        captionStyle.Font.Size = 8;
        captionStyle.Font.Italic = true;
        captionStyle.ParagraphFormat.SpaceBefore = 2;
        captionStyle.ParagraphFormat.SpaceAfter = 12;
        captionStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;

        // Create the section with page setup
        var section = doc.AddSection();
        section.PageSetup.PageFormat = PageFormat.Letter;
        section.PageSetup.TopMargin = "2.5cm";
        section.PageSetup.BottomMargin = "2.5cm";
        section.PageSetup.LeftMargin = "2.5cm";
        section.PageSetup.RightMargin = "2.5cm";
        section.PageSetup.HeaderDistance = "1.25cm";
        section.PageSetup.FooterDistance = "1.25cm";

        // Add page header
        var headerParagraph = section.Headers.Primary.AddParagraph($"{WikipediaArticleTitle} — Wikipedia");
        headerParagraph.Format.Font.Size = 8;
        headerParagraph.Format.Font.Italic = true;
        headerParagraph.Format.Alignment = ParagraphAlignment.Right;
        headerParagraph.Format.Borders.Bottom.Width = 0.5;
        headerParagraph.Format.Borders.Bottom.Color = Colors.Gray;

        // Add page footer with page number
        var footerParagraph = section.Footers.Primary.AddParagraph();
        footerParagraph.Format.Font.Size = 8;
        footerParagraph.Format.Alignment = ParagraphAlignment.Center;
        footerParagraph.AddText("Page ");
        footerParagraph.AddPageField();
        footerParagraph.AddText(" of ");
        footerParagraph.AddNumPagesField();

        // Add document title
        section.AddParagraph(WikipediaArticleTitle, "Title");

        // Add source attribution
        section.AddParagraph($"Source: {WikipediaUrl}", "Source");

        // Add a blank paragraph as spacing after the source line
        var spacer = section.AddParagraph();
        spacer.Format.SpaceAfter = 8;

        // Add all parsed article elements
        foreach (var element in articleElements)
        {
            switch (element.Type)
            {
                case ArticleElementType.Heading1:
                    section.AddParagraph(element.Text, "Heading1");
                    break;
                case ArticleElementType.Heading2:
                    section.AddParagraph(element.Text, "Heading2");
                    break;
                case ArticleElementType.Paragraph:
                    section.AddParagraph(element.Text);
                    break;
            }
        }

        // Parse article images and append them after the article text
        var articleImages = ParseWikipediaImages(html, _output);
        if (articleImages.Count > 0)
        {
            // Start images on a new page
            section.AddPageBreak();
            section.AddParagraph("Article Images", "Heading1");

            // Letter page: 612pt wide minus 2 × 2.5cm margins ≈ 612 - 141.7 ≈ 470pt usable
            var usableWidth = Unit.FromPoint(470);

            // Ensure the ImageSource implementation is registered before creating image sources
            ImageSource.ImageSourceImpl ??= new ImagingImageSource<Rgba32>();

            foreach (var articleImage in articleImages)
            {
                byte[] imageBytes;
                try
                {
                    imageBytes = await httpClient.GetByteArrayAsync(articleImage.Url, CancellationToken.None);
                }
                catch (HttpRequestException ex)
                {
                    _output.WriteLine($"  [DOWNLOAD FAILED] {articleImage.FileName}: {ex.Message}");
                    continue;
                }

                ImageSource.IImageSource imageSource;
                try
                {
                    imageSource = ImageSource.FromStream(
                        articleImage.Url,
                        () => new MemoryStream(imageBytes));
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"  [DECODE FAILED] {articleImage.FileName}: {ex.GetType().Name} - {ex.Message}");
                    continue;
                }

                _output.WriteLine($"  [ADDED TO PDF] {articleImage.FileName} — decoded {imageSource.Width}x{imageSource.Height}, transparent={imageSource.Transparent}");

                var img = section.AddImage(imageSource);
                img.LockAspectRatio = true;

                // Scale to 75% of usable page width, similar to DrawImageWithTextCaption
                var targetWidth = usableWidth.Point * 0.75;
                img.Width = Unit.FromPoint(targetWidth);

                // Cap the height so a tall image doesn't overflow onto a blank page.
                // Usable height ≈ Letter 792pt - 2 × 2.5cm margins - heading/footer space ≈ 600pt
                var scaledHeight = (targetWidth / imageSource.Width) * imageSource.Height;
                if (scaledHeight > 600)
                {
                    img.Height = Unit.FromPoint(600);
                }

                var caption = string.IsNullOrWhiteSpace(articleImage.Caption)
                    ? $"(no caption - {articleImage.FileName})"
                    : articleImage.Caption;
                section.AddParagraph(caption, "Caption");
            }
        }

        // Render the MigraDoc document to a PDF
        var renderer = new PdfDocumentRenderer(unicode: true) { Document = doc };
        renderer.RenderDocument();

        // Save via the PdfDocumentRenderer's PdfDocument
        var pdfDocument = renderer.PdfDocument;
        SaveDocument(pdfDocument, outName);
        ValidateFileIsPdf(outName);

        // Verify the PDF has multiple pages (the article is long enough)
        var outPath = GetOutFilePath(outName);
        using var verifyStream = File.OpenRead(outPath);
        var reopened = PdfDocuments.Pdf.IO.PdfReader.Open(verifyStream);
        Assert.True(reopened.PageCount > 1,
            $"Expected multi-page PDF but got {reopened.PageCount} page(s)");
    }

    private static List<ArticleElement> ParseWikipediaArticle(string html)
    {
        var elements = new List<ArticleElement>();

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        // Find the main article content container
        var parserOutput = document.QuerySelector(".mw-parser-output");
        if (parserOutput is null)
            return elements;

        // Section names where we stop collecting content
        var stopSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "References", "See also", "External links", "Notes", "Further reading", "Bibliography"
        };

        // Iterate over direct child elements looking for h2, h3, and p tags
        foreach (var child in parserOutput.Children)
        {
            var tag = child.TagName.ToUpperInvariant();

            if (tag is "H2" or "H3")
            {
                // Remove [edit] link spans before extracting text
                var editSection = child.QuerySelector(".mw-editsection");
                editSection?.Remove();

                var headingText = child.TextContent.Trim();

                // Stop if we've reached a non-prose section
                if (stopSections.Contains(headingText))
                    break;

                if (headingText.Length < 2)
                    continue;

                var type = tag == "H2" ? ArticleElementType.Heading1 : ArticleElementType.Heading2;
                elements.Add(new ArticleElement { Type = type, Text = headingText });
            }
            else if (tag == "P")
            {
                // Remove reference superscripts (e.g. [1], [2]) before extracting text
                foreach (var sup in child.QuerySelectorAll("sup.reference").ToList())
                    sup.Remove();

                var text = child.TextContent.Trim();

                if (string.IsNullOrWhiteSpace(text))
                    continue;

                elements.Add(new ArticleElement { Type = ArticleElementType.Paragraph, Text = text });
            }
        }

        return elements;
    }

    private void SaveDocument(PdfDocument document, string name)
    {
        var outFilePath = GetOutFilePath(name);
        var dir = Path.GetDirectoryName(outFilePath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        document.Save(outFilePath);
    }

    private void ValidateFileIsPdf(string v)
    {
        var path = GetOutFilePath(v);
        Assert.True(File.Exists(path));
        var fi = new FileInfo(path);
        Assert.True(fi.Length > 1);

        using var stream = File.OpenRead(path);
        ReadStreamAndVerifyPdfHeaderSignature(stream);
    }

    private void ValidateTargetAvailable(string file)
    {
        var path = GetOutFilePath(file);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        Assert.False(File.Exists(path));
    }

    private string GetOutFilePath(string name)
    {
        return Path.Combine(_rootPath, OutputDirName, name);
    }

    private static void ReadStreamAndVerifyPdfHeaderSignature(Stream stream)
    {
        var readBuffer = new byte[5];
        var pdfSignature = Encoding.ASCII.GetBytes("%PDF-"); // PDF must start with %PDF-

        stream.ReadExactly(readBuffer, 0, readBuffer.Length);
        readBuffer.Should().Equal(pdfSignature);
    }

    private static List<ArticleImage> ParseWikipediaImages(string html, ITestOutputHelper output)
    {
        var images = new List<ArticleImage>();

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var parserOutput = document.QuerySelector(".mw-parser-output");
        if (parserOutput is null)
            return images;

        // Supported raster formats for CodeBrix.Imaging (BMP, JPEG, PNG, WebP, GIF)
        var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".bmp", ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        // Wikipedia wraps article images in <figure> elements (or legacy <div class="thumb">)
        var figures = parserOutput.QuerySelectorAll("figure, div.thumb");

        foreach (var figure in figures)
        {
            // Skip nested figures (e.g. a gallery <figure> inside a parent <figure>)
            if (figure.ParentElement?.Closest("figure, div.thumb") is not null
                && figure.ParentElement.Closest("figure, div.thumb") != parserOutput)
                continue;

            // Find the <img> element
            var imgElement = figure.QuerySelector("img");
            if (imgElement is null)
                continue;

            // Get the image URL from src (preferred) or data-src
            var src = imgElement.GetAttribute("src") ?? imgElement.GetAttribute("data-src");
            if (string.IsNullOrWhiteSpace(src))
                continue;

            // Skip tiny icons and inline decorative images
            var widthAttr = imgElement.GetAttribute("width");
            if (int.TryParse(widthAttr, out var imgWidth) && imgWidth < 100)
                continue;

            // Ensure the URL is absolute
            if (src.StartsWith("//"))
                src = "https:" + src;
            else if (src.StartsWith("/"))
                src = "https://en.wikipedia.org" + src;

            // Filter out unsupported image formats (SVG, WebP, GIF, TIFF, etc.)
            // Wikipedia thumbnail URLs look like: /thumb/a/ab/File.svg/220px-File.svg.png
            // so check both the raw path and any extension before query params
            var urlPath = src.Split('?')[0];
            var ext = Path.GetExtension(urlPath);
            if (!supportedExtensions.Contains(ext))
                continue;

            // Get the caption from <figcaption> or <div class="thumbcaption">
            var captionElement = figure.QuerySelector("figcaption")
                ?? figure.QuerySelector(".thumbcaption");

            // Remove the "magnify" clip inside the caption if present
            captionElement?.QuerySelector(".magnify")?.Remove();

            var caption = captionElement?.TextContent.Trim() ?? "";

            // Extract the file name from the URL for readable output
            var fileName = Path.GetFileName(urlPath);

            // Predict how the PDF renderer will embed this image based on format
            var transparencyCapable = ext.ToLowerInvariant() is ".png" or ".webp" or ".gif";
            var renderMode = transparencyCapable ? "PdfBitmap" : "Jpeg";

            output.WriteLine($"Image [{images.Count + 1}]: {fileName}");
            output.WriteLine($"  Format: {ext.TrimStart('.').ToUpperInvariant()} | Render mode: {renderMode}");
            output.WriteLine($"  URL: {src}");
            output.WriteLine($"  Caption: {(string.IsNullOrWhiteSpace(caption) ? "(none)" : caption)}");

            images.Add(new ArticleImage { Url = src, Caption = caption, FileName = fileName });
        }

        output.WriteLine($"Total images to include in PDF: {images.Count}");
        return images;
    }

    private enum ArticleElementType
    {
        Heading1,
        Heading2,
        Paragraph
    }

    private class ArticleElement
    {
        public ArticleElementType Type { get; init; }
        public string Text { get; init; }
    }

    private class ArticleImage
    {
        public string Url { get; init; }
        public string Caption { get; init; }
        public string FileName { get; init; }
    }
}
