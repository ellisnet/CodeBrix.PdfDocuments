using CodeBrix.Imaging.PixelFormats;
using CodeBrix.MarkupParse.Dom;
using CodeBrix.MarkupParse.Html.Parser;
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
using PdfCreateDocument = CodeBrix.PdfDocCreate.DocumentObjectModel.Document;

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

    private const double UsableWidthPt = 470; // Letter 612pt - 2 × 2.5cm margins
    private const int ImageDownloadDelayMs = 300;

    private static readonly HashSet<string> SupportedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".bmp", ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private static readonly HashSet<string> StopSections =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "References", "See also", "External links", "Notes",
            "Further reading", "Bibliography", "Sources"
        };

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
            "CodeBrix.PdfDocuments.Tests/1.0 (https://github.com/ellisnet/CodeBrix.PdfDocuments; test suite)");
        var html = await httpClient.GetStringAsync(WikipediaUrl, CancellationToken.None);

        // Parse the article content into structured elements (text, images, lists, block quotes)
        var parser = new HtmlParser();
        var htmlDoc = parser.ParseDocument(html);
        var articleElements = ParseArticleElements(htmlDoc, s => _output.WriteLine(s));
        Assert.NotEmpty(articleElements);

        // Ensure the ImageSource implementation is registered before downloading
        ImageSource.ImageSourceImpl ??= new ImagingImageSource<Rgba32>();

        // Download images with rate limiting to avoid 429 from Wikimedia
        await DownloadImagesAsync(articleElements, httpClient, s => _output.WriteLine(s));

        // Build the CodeBrix.PdfDocCreate document
        var doc = new PdfCreateDocument
        {
            Info =
            {
                Title = WikipediaArticleTitle,
                Subject = WikipediaArticleSubject,
                Author = WikipediaArticleAuthor
            }
        };

        DefineStyles(doc);

        var section = doc.AddSection();
        SetupPage(section);
        AddHeaderAndFooter(section, WikipediaArticleTitle);

        // Title page content
        section.AddParagraph(WikipediaArticleTitle, "Title");
        section.AddParagraph($"Source: {WikipediaUrl}", "Source");

        var datePara = section.AddParagraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        datePara.Style = "Source";
        datePara.Format.SpaceBefore = 2;

        var spacer = section.AddParagraph();
        spacer.Format.SpaceAfter = 8;

        // Render all elements in document order
        var isFirstHeading1 = true;
        foreach (var element in articleElements)
        {
            switch (element.Type)
            {
                case ElementType.Heading1:
                    if (!isFirstHeading1)
                    {
                        // Horizontal rule before each major section (except the first)
                        var rule = section.AddParagraph();
                        rule.Format.Borders.Bottom.Width = 0.75;
                        rule.Format.Borders.Bottom.Color = Colors.LightGray;
                        rule.Format.SpaceAfter = 4;
                    }
                    section.AddParagraph(element.Text, "Heading1");
                    isFirstHeading1 = false;
                    break;

                case ElementType.Heading2:
                    section.AddParagraph(element.Text, "Heading2");
                    break;

                case ElementType.Paragraph:
                    AddFormattedParagraph(section, element.Runs);
                    break;

                case ElementType.BulletList:
                    AddListItems(section, element.ListItems, bullet: true);
                    break;

                case ElementType.NumberedList:
                    AddListItems(section, element.ListItems, bullet: false);
                    break;

                case ElementType.BlockQuote:
                    AddBlockQuote(section, element.Runs);
                    break;

                case ElementType.Image:
                    if (element.ImageData?.Source is not null)
                        AddInlineImage(section, element.ImageData);
                    break;
            }
        }

        // Render the MigraDoc document to a PDF
        var renderer = new PdfDocumentRenderer(unicode: true) { Document = doc };
        renderer.RenderDocument();

        // Add PDF bookmarks from headings
        AddBookmarks(renderer.PdfDocument, articleElements);

        // Save via the PdfDocumentRenderer's PdfDocument
        var pdfDocument = renderer.PdfDocument;
        SaveDocument(pdfDocument, outName);
        ValidateFileIsPdf(outName);

        // Verify the PDF has multiple pages (the article is long enough)
        var outPath = GetOutFilePath(outName);
        using var verifyStream = File.OpenRead(outPath);
        var reopened = Pdf.IO.PdfReader.Open(verifyStream);
        Assert.True(reopened.PageCount > 1,
            $"Expected multi-page PDF but got {reopened.PageCount} page(s)");
    }

    // ── Style definitions ────────────────────────────────────────────────

    private static void DefineStyles(PdfCreateDocument doc)
    {
        var titleStyle = doc.AddStyle("Title", "Normal");
        titleStyle.Font.Size = 24;
        titleStyle.Font.Bold = true;
        titleStyle.ParagraphFormat.SpaceAfter = 6;
        titleStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;

        var heading1Style = doc.Styles["Heading1"];
        heading1Style.Font.Size = 18;
        heading1Style.Font.Bold = true;
        heading1Style.Font.Color = new Color(0, 51, 102);
        heading1Style.ParagraphFormat.SpaceBefore = 16;
        heading1Style.ParagraphFormat.SpaceAfter = 6;
        heading1Style.ParagraphFormat.KeepWithNext = true;

        var heading2Style = doc.Styles["Heading2"];
        heading2Style.Font.Size = 14;
        heading2Style.Font.Bold = true;
        heading2Style.Font.Color = new Color(0, 68, 136);
        heading2Style.ParagraphFormat.SpaceBefore = 12;
        heading2Style.ParagraphFormat.SpaceAfter = 4;
        heading2Style.ParagraphFormat.KeepWithNext = true;

        var bodyStyle = doc.Styles["Normal"];
        bodyStyle.Font.Size = 10;
        bodyStyle.ParagraphFormat.SpaceAfter = 4;
        bodyStyle.ParagraphFormat.Alignment = ParagraphAlignment.Justify;

        var sourceStyle = doc.AddStyle("Source", "Normal");
        sourceStyle.Font.Size = 8;
        sourceStyle.Font.Italic = true;
        sourceStyle.Font.Color = Colors.DarkGray;
        sourceStyle.ParagraphFormat.SpaceBefore = 12;
        sourceStyle.ParagraphFormat.Alignment = ParagraphAlignment.Left;

        var captionStyle = doc.AddStyle("Caption", "Normal");
        captionStyle.Font.Size = 8;
        captionStyle.Font.Italic = true;
        captionStyle.Font.Color = Colors.DarkGray;
        captionStyle.ParagraphFormat.SpaceBefore = 2;
        captionStyle.ParagraphFormat.SpaceAfter = 12;
        captionStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;

        var bulletStyle = doc.AddStyle("BulletItem", "Normal");
        bulletStyle.ParagraphFormat.LeftIndent = Unit.FromCentimeter(1);
        bulletStyle.ParagraphFormat.FirstLineIndent = Unit.FromCentimeter(-0.5);
        bulletStyle.ParagraphFormat.SpaceAfter = 2;

        var blockQuoteStyle = doc.AddStyle("BlockQuote", "Normal");
        blockQuoteStyle.Font.Italic = true;
        blockQuoteStyle.Font.Color = new Color(80, 80, 80);
        blockQuoteStyle.ParagraphFormat.LeftIndent = Unit.FromCentimeter(1.5);
        blockQuoteStyle.ParagraphFormat.RightIndent = Unit.FromCentimeter(1);
        blockQuoteStyle.ParagraphFormat.SpaceBefore = 6;
        blockQuoteStyle.ParagraphFormat.SpaceAfter = 6;
        blockQuoteStyle.ParagraphFormat.Borders.Left.Width = 2;
        blockQuoteStyle.ParagraphFormat.Borders.Left.Color = Colors.LightGray;
        blockQuoteStyle.ParagraphFormat.Borders.DistanceFromLeft = Unit.FromCentimeter(0.3);
    }

    private static void SetupPage(Section section)
    {
        section.PageSetup.PageFormat = PageFormat.Letter;
        section.PageSetup.TopMargin = "2.5cm";
        section.PageSetup.BottomMargin = "2.5cm";
        section.PageSetup.LeftMargin = "2.5cm";
        section.PageSetup.RightMargin = "2.5cm";
        section.PageSetup.HeaderDistance = "1.25cm";
        section.PageSetup.FooterDistance = "1.25cm";
    }

    private static void AddHeaderAndFooter(Section section, string articleTitle)
    {
        var headerParagraph = section.Headers.Primary.AddParagraph($"{articleTitle} — Wikipedia");
        headerParagraph.Format.Font.Size = 8;
        headerParagraph.Format.Font.Italic = true;
        headerParagraph.Format.Font.Color = Colors.Gray;
        headerParagraph.Format.Alignment = ParagraphAlignment.Right;
        headerParagraph.Format.Borders.Bottom.Width = 0.5;
        headerParagraph.Format.Borders.Bottom.Color = Colors.LightGray;

        var footerParagraph = section.Footers.Primary.AddParagraph();
        footerParagraph.Format.Font.Size = 8;
        footerParagraph.Format.Font.Color = Colors.Gray;
        footerParagraph.Format.Alignment = ParagraphAlignment.Center;
        footerParagraph.AddText("Page ");
        footerParagraph.AddPageField();
        footerParagraph.AddText(" of ");
        footerParagraph.AddNumPagesField();
    }

    // ── Document building helpers ────────────────────────────────────────

    private static void AddFormattedParagraph(Section section, List<TextRun> runs)
    {
        if (runs is null || runs.Count == 0) return;

        var para = section.AddParagraph();
        AppendRuns(para, runs);
    }

    private static void AppendRuns(Paragraph para, List<TextRun> runs)
    {
        foreach (var run in runs)
        {
            if (string.IsNullOrEmpty(run.Text)) continue;

            if (!run.Bold && !run.Italic)
            {
                para.AddText(run.Text);
            }
            else
            {
                var format = run.Bold && run.Italic ? TextFormat.Bold | TextFormat.Italic
                    : run.Bold ? TextFormat.Bold
                    : TextFormat.Italic;
                para.AddFormattedText(run.Text, format);
            }
        }
    }

    private static void AddListItems(
        Section section, List<ListItem> items, bool bullet)
    {
        if (items is null || items.Count == 0) return;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var para = section.AddParagraph();
            para.Style = "BulletItem";

            var prefix = bullet ? "\u2022  " : $"{i + 1}.  ";
            para.AddText(prefix);
            AppendRuns(para, item.Runs);
        }
    }

    private static void AddBlockQuote(Section section, List<TextRun> runs)
    {
        if (runs is null || runs.Count == 0) return;

        var para = section.AddParagraph();
        para.Style = "BlockQuote";
        AppendRuns(para, runs);
    }

    private static void AddInlineImage(Section section, ImageData imageData)
    {
        var source = imageData.Source!;

        // Size the image proportionally
        var naturalWidth = (double)source.Width;
        var naturalHeight = (double)source.Height;

        // Scale: use natural size up to max 60% of usable width, minimum 30%
        var maxWidth = UsableWidthPt * 0.60;
        var minWidth = UsableWidthPt * 0.30;
        var targetWidth = Math.Clamp(naturalWidth, minWidth, maxWidth);

        var scaledHeight = (targetWidth / naturalWidth) * naturalHeight;
        // Cap height to avoid overflowing a page
        if (scaledHeight > 500)
        {
            scaledHeight = 500;
            targetWidth = (scaledHeight / naturalHeight) * naturalWidth;
        }

        var img = section.AddImage(source);
        img.LockAspectRatio = true;
        img.Width = Unit.FromPoint(targetWidth);
        img.Height = Unit.FromPoint(scaledHeight);

        var caption = string.IsNullOrWhiteSpace(imageData.Caption)
            ? $"(no caption — {imageData.FileName})"
            : imageData.Caption;
        section.AddParagraph(caption, "Caption");
    }

    // ── PDF bookmarks ────────────────────────────────────────────────────

    private static void AddBookmarks(
        PdfDocument pdfDocument, List<ArticleElement> elements)
    {
        PdfOutline currentH1 = null;
        foreach (var el in elements)
        {
            if (el.Type == ElementType.Heading1)
            {
                currentH1 = pdfDocument.Outlines.Add(el.Text, pdfDocument.Pages[0]);
            }
            else if (el.Type == ElementType.Heading2 && currentH1 is not null)
            {
                currentH1.Outlines.Add(el.Text, pdfDocument.Pages[0]);
            }
        }
    }

    // ── HTML parsing ─────────────────────────────────────────────────────

    private static List<ArticleElement> ParseArticleElements(
        IDocument document, Action<string> log)
    {
        var elements = new List<ArticleElement>();

        // Some articles expose more than one .mw-parser-output element - e.g. a small wrapper
        // emitted by a transcluded template (coordinates, short description, hatnotes) plus the
        // real article body. QuerySelector would return whichever comes first in document order,
        // which can be the near-empty wrapper. Pick the container that actually holds the prose:
        // the one with the most paragraph descendants.
        var parserOutput = document.QuerySelectorAll(".mw-parser-output")
            .OrderByDescending(e => e.QuerySelectorAll("p").Count())
            .ThenByDescending(e => e.Children.Count())
            .FirstOrDefault();
        if (parserOutput is null)
            return elements;

        // Wikipedia (Parsoid) HTML wraps each article section in a <section> element and each
        // heading in a <div class="mw-heading">…<h2>…</h2></div>, so the prose is no longer a
        // direct child of .mw-parser-output. Flatten those wrappers so the block-level handling
        // below still sees paragraphs, lists, figures, and bare headings in document order.
        foreach (var child in FlattenContentBlocks(parserOutput))
        {
            var tag = child.TagName.ToUpperInvariant();

            // Stop at non-prose sections
            if (tag is "H2" or "H3")
            {
                child.QuerySelector(".mw-editsection")?.Remove();
                var headingText = child.TextContent.Trim();

                if (StopSections.Contains(headingText))
                    break;
                if (headingText.Length < 2)
                    continue;

                var type = tag == "H2" ? ElementType.Heading1 : ElementType.Heading2;
                elements.Add(new ArticleElement { Type = type, Text = headingText });
            }
            else if (tag == "P")
            {
                // Remove reference superscripts before extracting
                foreach (var sup in child.QuerySelectorAll("sup.reference").ToList())
                    sup.Remove();

                var runs = ExtractTextRuns(child);
                if (runs.Count == 0 || runs.All(r => string.IsNullOrWhiteSpace(r.Text)))
                    continue;

                elements.Add(new ArticleElement { Type = ElementType.Paragraph, Runs = runs });
            }
            else if (tag is "UL" or "OL")
            {
                var listItems = ParseListItems(child);
                if (listItems.Count == 0)
                    continue;

                var type = tag == "UL" ? ElementType.BulletList : ElementType.NumberedList;
                elements.Add(new ArticleElement { Type = type, ListItems = listItems });
            }
            else if (tag == "BLOCKQUOTE")
            {
                foreach (var sup in child.QuerySelectorAll("sup.reference").ToList())
                    sup.Remove();

                var runs = ExtractTextRuns(child);
                if (runs.Count > 0 && runs.Any(r => !string.IsNullOrWhiteSpace(r.Text)))
                    elements.Add(new ArticleElement { Type = ElementType.BlockQuote, Runs = runs });
            }
            else if (tag is "FIGURE" || (tag == "DIV" && child.ClassList.Contains("thumb")))
            {
                var imageInfo = ParseSingleImage(child, log);
                if (imageInfo is not null)
                    elements.Add(new ArticleElement { Type = ElementType.Image, ImageData = imageInfo });
            }
        }

        return elements;
    }

    // Yields the block-level content elements of a .mw-parser-output container in document order,
    // unwrapping the two structural wrappers modern Wikipedia (Parsoid) HTML introduces:
    //   * <section> — each article section; its children are flattened (sections can nest).
    //   * <div class="mw-heading"> — the wrapper around each <h2>/<h3>/…; the inner heading is
    //     surfaced so the existing H2/H3 handling keeps working.
    // Every other element is yielded as-is. Enumeration is lazy, so a StopSections break in the
    // consumer stops the walk without visiting trailing sections (References, See also, etc.).
    private static IEnumerable<IElement> FlattenContentBlocks(IElement container)
    {
        foreach (var child in container.Children)
        {
            var tag = child.TagName.ToUpperInvariant();
            if (tag == "SECTION")
            {
                foreach (var block in FlattenContentBlocks(child))
                    yield return block;
            }
            else if (tag == "DIV" && child.ClassList.Contains("mw-heading"))
            {
                var heading = child.QuerySelector("h1, h2, h3, h4, h5, h6");
                if (heading is not null)
                    yield return heading;
            }
            else
            {
                yield return child;
            }
        }
    }

    private static List<TextRun> ExtractTextRuns(
        INode node, bool bold = false, bool italic = false)
    {
        var runs = new List<TextRun>();

        foreach (var child in node.ChildNodes)
        {
            if (child is IText textNode)
            {
                var text = textNode.Data;
                if (!string.IsNullOrEmpty(text))
                    runs.Add(new TextRun(text, bold, italic));
            }
            else if (child is IElement element)
            {
                var elTag = element.TagName.ToUpperInvariant();

                // Skip hidden elements, edit links, reference markers
                if (elTag == "SUP" && element.ClassList.Contains("reference"))
                    continue;
                if (element.ClassList.Contains("mw-editsection"))
                    continue;
                if (elTag is "STYLE" or "SCRIPT")
                    continue;

                var newBold = bold || elTag is "B" or "STRONG";
                var newItalic = italic || elTag is "I" or "EM";

                runs.AddRange(ExtractTextRuns(element, newBold, newItalic));
            }
        }

        return runs;
    }

    private static List<ListItem> ParseListItems(IElement listElement)
    {
        var items = new List<ListItem>();

        foreach (var li in listElement.Children.Where(c => c.TagName.ToUpperInvariant() == "LI"))
        {
            // Remove reference superscripts
            foreach (var sup in li.QuerySelectorAll("sup.reference").ToList())
                sup.Remove();

            var runs = ExtractTextRuns(li);
            if (runs.Count > 0 && runs.Any(r => !string.IsNullOrWhiteSpace(r.Text)))
                items.Add(new ListItem { Runs = runs });
        }

        return items;
    }

    private static ImageData ParseSingleImage(IElement figure, Action<string> log)
    {
        // Skip nested figures
        if (figure.ParentElement?.Closest("figure, div.thumb") is { } parent
            && parent.QuerySelector(".mw-parser-output") is null)
            return null;

        var imgElement = figure.QuerySelector("img");
        if (imgElement is null)
            return null;

        var src = imgElement.GetAttribute("src") ?? imgElement.GetAttribute("data-src");
        if (string.IsNullOrWhiteSpace(src))
            return null;

        // Skip tiny icons
        var widthAttr = imgElement.GetAttribute("width");
        if (int.TryParse(widthAttr, out var imgWidth) && imgWidth < 100)
            return null;

        if (src.StartsWith("//"))
            src = "https:" + src;
        else if (src.StartsWith("/"))
            src = "https://en.wikipedia.org" + src;

        var urlPath = src.Split('?')[0];
        var ext = Path.GetExtension(urlPath);
        if (!SupportedImageExtensions.Contains(ext))
            return null;

        var captionElement = figure.QuerySelector("figcaption")
            ?? figure.QuerySelector(".thumbcaption");
        captionElement?.QuerySelector(".magnify")?.Remove();
        var caption = captionElement?.TextContent.Trim() ?? "";

        var fileName = Path.GetFileName(urlPath);

        log($"  Image: {fileName} — {(string.IsNullOrWhiteSpace(caption) ? "(no caption)" : caption)}");

        return new ImageData { Url = src, Caption = caption, FileName = fileName };
    }

    // ── Image downloading ────────────────────────────────────────────────

    private static async Task DownloadImagesAsync(
        List<ArticleElement> elements, HttpClient httpClient, Action<string> log)
    {
        var imageElements = elements.Where(e => e.Type == ElementType.Image && e.ImageData is not null).ToList();
        if (imageElements.Count == 0) return;

        log($"Downloading {imageElements.Count} images (with {ImageDownloadDelayMs}ms delay between requests)...");

        foreach (var element in imageElements)
        {
            var img = element.ImageData!;

            // Rate-limit to avoid 429 from Wikimedia
            await Task.Delay(ImageDownloadDelayMs);

            byte[] imageBytes;
            try
            {
                imageBytes = await httpClient.GetByteArrayAsync(img.Url);
            }
            catch (HttpRequestException ex)
            {
                log($"  [DOWNLOAD FAILED] {img.FileName}: {ex.Message}");
                continue;
            }

            try
            {
                img.Source = ImageSource.FromStream(
                    img.Url,
                    () => new MemoryStream(imageBytes));
                log($"  [OK] {img.FileName} — {img.Source.Width}x{img.Source.Height}");
            }
            catch (Exception ex)
            {
                log($"  [DECODE FAILED] {img.FileName}: {ex.GetType().Name} - {ex.Message}");
            }
        }
    }

    // ── Utility methods ──────────────────────────────────────────────────

    private void SaveDocument(PdfDocument document, string name)
    {
        var outFilePath = GetOutFilePath(name);
        var dir = Path.GetDirectoryName(outFilePath);
        if (dir != null && (!Directory.Exists(dir)))
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

    // ── Data types ─────────────────────────────────────────────────────

    private enum ElementType
    {
        Heading1,
        Heading2,
        Paragraph,
        BulletList,
        NumberedList,
        Image,
        BlockQuote
    }

    private record TextRun(string Text, bool Bold, bool Italic);

    private class ListItem
    {
        public List<TextRun> Runs { get; init; } = [];
    }

    private class ImageData
    {
        public string Url { get; init; } = "";
        public string Caption { get; init; } = "";
        public string FileName { get; init; } = "";
        public ImageSource.IImageSource Source { get; set; }
    }

    private class ArticleElement
    {
        public ElementType Type { get; init; }
        public string Text { get; init; } = "";
        public List<TextRun> Runs { get; init; }
        public List<ListItem> ListItems { get; init; }
        public ImageData ImageData { get; init; }
    }
}
