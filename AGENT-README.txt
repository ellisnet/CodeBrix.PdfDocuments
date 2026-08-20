================================================================================
AGENT-README: CodeBrix.PdfDocuments / CodeBrix.PdfDocCreate /
CodeBrix.PdfRasterizer / CodeBrix.PdfDocCreate.Html2Pdf /
CodeBrix.PdfDocCreate.Markdown2Pdf
A Comprehensive Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
This repository produces FIVE companion .NET libraries for working with PDF
documents:

  1. CodeBrix.PdfDocuments - Low-level PDF library for creating, reading, merging,
     and manipulating PDF documents using direct graphics drawing (XGraphics).

  2. CodeBrix.PdfDocCreate - High-level document object model for building
     richly formatted PDF documents with styled text, tables, charts, and images.
     This is built on top of CodeBrix.PdfDocuments.

  3. CodeBrix.PdfRasterizer - PDF page rasterizer that renders PDF pages to images
     (PNG, JPEG, BMP, GIF, TIFF) using the PDFium native rendering engine, with
     support for thumbnails, page information, and cross-platform operation.
     This depends on CodeBrix.PdfDocuments and CodeBrix.Imaging.

  4. CodeBrix.PdfDocCreate.Html2Pdf - Renders author-created HTML pages with CSS
     styling into PDF documents through the PdfDocCreate document object model.
     Applies a documented subset of CSS with real selector matching, cascade,
     specificity and inheritance. All text renders with the CodeBrix.Platform.Fonts
     package fonts (Roboto / Merriweather / Roboto Mono), so output is identical
     on every operating system. Designed for HTML/CSS written FOR PDF generation,
     not for repurposing arbitrary web pages. See PART 4.

  5. CodeBrix.PdfDocCreate.Markdown2Pdf - Renders ANY Markdown (.md) file into a
     nice-looking, pre-formatted, printable PDF with zero configuration, using a
     vendored C# port of the markdown-it CommonMark parser and the Html2Pdf
     rendering pipeline. Also exposes the generated HTML/CSS so a consumer can
     restyle the output before rendering it with Html2Pdf. See PART 5.

################################################################################
## IMPORTANT - LINUX ONLY: SVG RENDERING NEEDS A SkiaSharp NATIVE-ASSETS PACKAGE
################################################################################

If your application runs on LINUX and renders SVG content through
CodeBrix.PdfDocCreate.Html2Pdf or CodeBrix.PdfDocCreate.Markdown2Pdf, the
APPLICATION must reference ONE of these two NuGet packages itself:

    SkiaSharp.NativeAssets.Linux
    SkiaSharp.NativeAssets.Linux.NoDependencies

  dotnet add package SkiaSharp.NativeAssets.Linux
    -- OR --
  dotnet add package SkiaSharp.NativeAssets.Linux.NoDependencies

EITHER package satisfies Html2Pdf equally - neither is recommended over the
other. Reference exactly ONE, whichever suits the application. If the
application already references one of them for its own reasons, KEEP THAT ONE;
nothing needs to change, and it must not be swapped for the other.

The two differ only in how the native obtains font services - the plain variant
links against the system libfontconfig, the ".NoDependencies" variant is
self-contained - and that difference does not affect Html2Pdf, which never
consults system fonts. Choose on the application's own deployment grounds.

WINDOWS and macOS need NOTHING extra - SkiaSharp's own package brings those
natives transitively.

WHY this is not just a package dependency: Html2Pdf DELIBERATELY does not
declare a dependency on either package. Two mutually exclusive Linux variants
exist, and only the consuming application can choose between them; declaring one
here would force that choice on every consumer and break applications that
already reference the other. This is a deliberate design decision - DO NOT
"fix" it by adding a PackageReference to the Html2Pdf library.

WHAT HAPPENS IF IT IS MISSING: nothing crashes. SVG images are skipped and the
rest of the document renders normally. The skip is reported as a collected
rendering warning with the code "image.svg.nativemissing", whose message names
both packages. Check HtmlRenderResult.Warnings (or MarkdownRenderResult.Warnings)
when SVG content is silently absent from your output.

NOTE FOR THIS REPOSITORY'S OWN TESTS: the test projects DO carry a
PackageReference to SkiaSharp.NativeAssets.Linux.NoDependencies, so the suite
passes on Linux. That reference belongs ONLY in tests/, never in src/.

################################################################################

CodeBrix.PdfDocuments and CodeBrix.PdfDocCreate are forks of the popular
PdfSharpCore (v1.3.67) and MigraDocCore (v1.3.67) libraries.
CodeBrix.PdfRasterizer contains rendering logic derived from Docnet.Core and
bundles pre-built PDFium native binaries. CodeBrix.PdfDocCreate.Markdown2Pdf
contains a C# port of the markdown-it JavaScript parser (MIT) with its
footnote, task-list, and front-matter plugins.
All five libraries are licensed under the MIT License.

IMPORTANT: If you are familiar with PdfSharp/PdfSharpCore, the API surface of
CodeBrix.PdfDocuments is very similar. If you are familiar with MigraDoc/
MigraDocCore, the API surface of CodeBrix.PdfDocCreate is very similar.
However, ALL namespaces use "CodeBrix.PdfDocuments", "CodeBrix.PdfDocCreate",
and "CodeBrix.PdfRasterizer" instead of "PdfSharp"/"MigraDoc". Do NOT mix
the libraries.

PACKAGE-TO-NAMESPACE MAP (read this before writing any using directive - the
package name and the namespace are NOT the same, and this is the single most
common mistake):

  NuGet package                                        Namespace root
  ---------------------------------------------------  ----------------------------------
  CodeBrix.PdfDocuments.MitLicenseForever              CodeBrix.PdfDocuments.*
  CodeBrix.PdfDocCreate.MitLicenseForever              CodeBrix.PdfDocCreate.*
  CodeBrix.PdfRasterizer.MitLicenseForever             CodeBrix.PdfRasterizer
  CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever     CodeBrix.PdfDocCreate.Html2Pdf.*
  CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever CodeBrix.PdfDocCreate.Markdown2Pdf.*

The ".MitLicenseForever" suffix belongs to the PACKAGE ID only. It never
appears in a namespace, a using directive or a type name.

Source Repository: https://github.com/ellisnet/CodeBrix.PdfDocuments
License: MIT License

================================================================================

INSTALLATION
------------
There are THREE NuGet packages. Install one or more depending on your needs:

--- Package 1: CodeBrix.PdfDocuments (low-level) ---

NuGet Package: CodeBrix.PdfDocuments.MitLicenseForever
Dependencies:
  - CodeBrix.Compression.MitLicenseForever
  - CodeBrix.Imaging.ApacheLicenseForever

    dotnet add package CodeBrix.PdfDocuments.MitLicenseForever

--- Package 2: CodeBrix.PdfDocCreate (high-level, includes PdfDocuments) ---

NuGet Package: CodeBrix.PdfDocCreate.MitLicenseForever
Dependencies:
  - CodeBrix.PdfDocuments.MitLicenseForever

    dotnet add package CodeBrix.PdfDocCreate.MitLicenseForever

NOTE: Installing CodeBrix.PdfDocCreate automatically pulls in
CodeBrix.PdfDocuments, CodeBrix.Imaging, and CodeBrix.Compression.

--- Package 3: CodeBrix.PdfRasterizer (PDF-to-image rendering) ---

NuGet Package: CodeBrix.PdfRasterizer.MitLicenseForever
Dependencies:
  - CodeBrix.PdfDocuments.MitLicenseForever
  - CodeBrix.Imaging.ApacheLicenseForever
Bundled Native Libraries: PDFium binaries for multiple platform RIDs
  (see SUPPORTED PLATFORMS below for the full list)

    dotnet add package CodeBrix.PdfRasterizer.MitLicenseForever

NOTE: Installing CodeBrix.PdfRasterizer automatically pulls in
CodeBrix.PdfDocuments, CodeBrix.Imaging, and the PDFium native binaries
for your platform. There is NO separate PDFium installation required.
The pre-built native library (pdfium.dll / pdfium.dylib / pdfium.so) is
bundled inside the NuGet package and copied to the output directory at
build time.

--- Package 4: CodeBrix.PdfDocCreate.Html2Pdf (HTML+CSS to PDF) ---

NuGet Package: CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever
Dependencies:
  - CodeBrix.PdfDocCreate.MitLicenseForever
  - CodeBrix.MarkupParse.MitLicenseForever
  - CodeBrix.StyleSheetParse.MitLicenseForever
  - CodeBrix.Platform.Fonts.Roboto.OflLicenseForever
  - CodeBrix.Platform.Fonts.Merriweather.OflLicenseForever
  - CodeBrix.Platform.Fonts.RobotoMono.OflLicenseForever
  - CodeBrix.SkiaSvg.MitLicenseForever (SVG rasterization; brings SkiaSharp and
    HarfBuzzSharp, plus the Windows and macOS native assets they need - so SVG
    rendering works on those two platforms with no extra packages)

  NOT declared, but REQUIRED ON LINUX for SVG content - the consuming
  application must reference one of these itself (see the IMPORTANT notice at
  the top of this file for why):
  - SkiaSharp.NativeAssets.Linux, or
  - SkiaSharp.NativeAssets.Linux.NoDependencies (self-contained; no
    libfontconfig required at runtime)

    dotnet add package CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever

NOTE: The font packages ship .ttf files inside their nupkgs; the Html2Pdf
package's build targets copy them into the consuming application's output
under CodeBrix.Platform.Fonts.<Name>/Fonts/. No font installation or
registration is required - the renderer discovers them automatically.

--- Package 5: CodeBrix.PdfDocCreate.Markdown2Pdf (Markdown to PDF) ---

NuGet Package: CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever
Dependencies:
  - CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever (which pulls the rest)

  Markdown2Pdf renders through Html2Pdf, so it inherits the Linux SVG
  requirement exactly: an application on Linux whose Markdown embeds SVG images
  must reference SkiaSharp.NativeAssets.Linux or
  SkiaSharp.NativeAssets.Linux.NoDependencies itself. See the IMPORTANT notice
  at the top of this file.

    dotnet add package CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever

Requirements: .NET 10.0 or higher

SUPPORTED PLATFORMS (CodeBrix.PdfRasterizer only):
  The NuGet package includes pre-built PDFium native binaries for:
    - Windows x64      (win-x64)
    - Windows x86      (win-x86)
    - Windows ARM64    (win-arm64)
    - macOS x64        (osx-x64)
    - macOS ARM64      (osx-arm64, Apple Silicon)
    - Linux x64        (linux-x64)
    - Linux ARM64      (linux-arm64)
    - Linux ARM        (linux-arm)
    - Linux RISC-V 64  (linux-riscv64)
    - Android ARM64    (android-arm64)

  UNSUPPORTED PLATFORMS: CodeBrix.PdfRasterizer does NOT work on iOS,
  WebAssembly (Blazor WASM), or any other platform not listed
  above. There are no PDFium native binaries for those targets. Attempting
  to construct a PageRasterizer on an unsupported platform will fail at
  runtime when the native library cannot be loaded.

  CodeBrix.PdfDocuments and CodeBrix.PdfDocCreate are pure managed .NET
  and have no platform restrictions beyond .NET 10.0+.

Or in a .csproj file (NuGet will resolve the latest version):

    <!-- For low-level PDF drawing only -->
    <PackageReference Include="CodeBrix.PdfDocuments.MitLicenseForever" />

    <!-- For high-level document model (includes PdfDocuments) -->
    <PackageReference Include="CodeBrix.PdfDocCreate.MitLicenseForever" />

    <!-- For PDF-to-image rasterization -->
    <PackageReference Include="CodeBrix.PdfRasterizer.MitLicenseForever" />

WHEN TO USE WHICH:
  - Use CodeBrix.PdfDocuments alone when you need fine-grained control over
    page layout, graphics drawing, or when working with existing PDFs.
  - Use CodeBrix.PdfDocCreate when you want to build documents with a
    structured model (paragraphs, tables, styles, headers/footers).
  - Use CodeBrix.PdfRasterizer when you need to convert PDF pages to images
    (PNG, JPEG, etc.), generate thumbnails, or query page dimensions.
  - Use them together as needed; they are independent but complementary.

================================================================================

KEY NAMESPACES
--------------

CodeBrix.PdfDocuments (low-level):

    using CodeBrix.PdfDocuments.Pdf;         // PdfDocument, PdfPage
    using CodeBrix.PdfDocuments.Pdf.IO;      // PdfReader for opening existing PDFs
    using CodeBrix.PdfDocuments.Drawing;     // XGraphics, XFont, XBrush, XImage, XPoint
    using CodeBrix.PdfDocuments.Drawing.Layout;  // XTextFormatter for text layout

CodeBrix.PdfDocCreate (high-level document model):

    using CodeBrix.PdfDocCreate.DocumentObjectModel;         // Document, Section, Paragraph, Style
    using CodeBrix.PdfDocCreate.DocumentObjectModel.Tables;  // Table, Row, Cell, Column
    using CodeBrix.PdfDocCreate.Rendering;                   // PdfDocumentRenderer

CodeBrix.PdfDocCreate.Html2Pdf (HTML+CSS to PDF):

    using CodeBrix.PdfDocCreate.Html2Pdf;        // HtmlPdfRenderer, HtmlRenderOptions,
                                                 // HtmlRenderResult, RenderWarnings
    using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;  // Html2PdfFonts (font discovery)

CodeBrix.PdfDocCreate.Markdown2Pdf (Markdown to PDF):

    using CodeBrix.PdfDocCreate.Markdown2Pdf;    // MarkdownPdfRenderer,
                                                 // MarkdownRenderOptions,
                                                 // MarkdownRenderResult,
                                                 // MarkdownHtmlResult
    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;  // MarkdownParser (the
                                                 // markdown-it port, for advanced use)

CodeBrix.PdfRasterizer (PDF-to-image rendering):

    using CodeBrix.PdfRasterizer;          // PageRasterizer, PdfPageDimensions,
                                           // ThumbnailMaxDimensions, PdfRenderFlags
    using CodeBrix.Imaging;                // Image (return type of rasterization methods)
    using CodeBrix.Imaging.Formats.Png;    // PngFormat.Instance  (default)
    using CodeBrix.Imaging.Formats.Jpeg;   // JpegFormat.Instance
    using CodeBrix.Imaging.Formats.Bmp;    // BmpFormat.Instance
    using CodeBrix.Imaging.Formats.Gif;    // GifFormat.Instance
    using CodeBrix.Imaging.Formats.Tiff;   // TiffFormat.Instance

================================================================================

PART 1: CodeBrix.PdfDocuments (LOW-LEVEL API)
===============================================

--- CREATING A NEW PDF ---

    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;

    var document = new PdfDocument();
    var page = document.AddPage();
    var renderer = XGraphics.FromPdfPage(page);

    renderer.DrawString(
        "Hello, PDF!",
        new XFont("Arial", 24),
        XBrushes.Black,
        new XPoint(50, 50));

    document.Save("output.pdf");

--- DOCUMENT INFORMATION (METADATA) ---

    var document = new PdfDocument();

    document.Info.Title = "My Document";
    document.Info.Subject = "Document Subject";
    document.Info.Author = "Author Name";
    document.Info.Keywords = "pdf, codebrix, .net";
    document.Info.Creator = "My Application";

Unicode metadata is supported:

    document.Info.Title = "English, Ελληνικά, 漢語";

--- PAGE SETUP ---

    var page = document.AddPage();

    // Page size (default is A4)
    page.Size = PageSize.Letter;       // 8.5" x 11"
    page.Size = PageSize.A4;           // 210mm x 297mm
    page.Size = PageSize.Legal;        // 8.5" x 14"

    // Orientation
    page.Orientation = PageOrientation.Landscape;
    page.Orientation = PageOrientation.Portrait;

    // Custom size (in points, 1 inch = 72 points)
    page.Width = XUnit.FromInch(8.5);
    page.Height = XUnit.FromInch(11);

--- DRAWING TEXT ---

    var renderer = XGraphics.FromPdfPage(page);

    // Simple text drawing
    renderer.DrawString(
        "Hello World",
        new XFont("Arial", 16),
        XBrushes.Black,
        new XPoint(50, 50));

    // With different fonts and styles
    renderer.DrawString("Bold Text", new XFont("Arial", 12, XFontStyle.Bold),
        XBrushes.Black, new XPoint(50, 80));

    renderer.DrawString("Italic Text", new XFont("Arial", 12, XFontStyle.Italic),
        XBrushes.DarkBlue, new XPoint(50, 100));

Available XFontStyle values:
    XFontStyle.Regular
    XFontStyle.Bold
    XFontStyle.Italic
    XFontStyle.BoldItalic

--- FONTS AND FONT RESOLUTION ---

NO IFontResolver REGISTRATION IS REQUIRED. On Windows, macOS and Linux the
installed system fonts are discovered automatically. Just construct an XFont
with a family name and draw. (This differs from upstream PdfSharpCore, where
forgetting to set GlobalFontSettings.FontResolver is the classic first failure.)

    var font = new XFont("Arial", 12);          // no setup needed

THE CATCH: AN UNAVAILABLE FAMILY NEVER THROWS - IT IS SILENTLY SUBSTITUTED.

A misspelled or simply not-installed family name produces a document that
renders in some other face with no exception, no warning and no log entry.
Verify what you actually got:

    var font = new XFont("Consolas", 12);
    if (!string.Equals(font.FontFamily.Name, "Consolas", StringComparison.OrdinalIgnoreCase))
    {
        // A substitute was chosen - the layout may not be what you expect.
    }

This bites hardest cross-platform, and worst of all for MONOSPACE text. Typical
results on a stock Linux desktop with no IFontResolver registered:

    Arial, Verdana, Georgia, Times New Roman, Courier New  -> resolve correctly
    DejaVu Sans, DejaVu Sans Mono, Liberation Serif        -> resolve correctly
    Segoe UI, Calibri                                      -> substituted
    Consolas, Cascadia Mono, Lucida Console                -> substituted
    (any unknown name)                                     -> substituted

The three families in that middle group are monospace, and their substitute is
typically a PROPORTIONAL font. Code blocks, aligned columns and ASCII tables
therefore look correct on Windows and visibly wrong on Linux, silently.

RECOMMENDATIONS:
  - For cross-platform monospace, prefer "Courier New" or "DejaVu Sans Mono".
    Avoid Consolas, Cascadia Mono and Lucida Console unless you know the target
    machine has them.
  - For containers, CI and any minimal image, do not rely on system fonts at
    all. A slim base image may have almost none installed. Register an
    IFontResolver that serves fonts you embed with your application, and set it
    once at startup, before any font is used:

        GlobalFontSettings.FontResolver = new MyEmbeddedFontResolver();

  - When output must be reproducible across machines, assert on
    XFont.FontFamily.Name in a test rather than trusting the name you asked for.

--- DRAWING IMAGES ---

    using CodeBrix.PdfDocuments.Drawing;

    // From file
    renderer.DrawImage(XImage.FromFile("photo.png"), new XPoint(50, 150));

    // With size
    renderer.DrawImage(XImage.FromFile("photo.jpg"), 50, 150, 200, 150);
    // x, y, width, height

    // From CodeBrix.Imaging (with processing)
    using CodeBrix.Imaging;
    using CodeBrix.Imaging.Processing;

    using var image = Image.Load("photo.jpg");
    image.Mutate(x => x.Grayscale());
    var xImage = XImage.FromImageSource(image);
    renderer.DrawImage(xImage, new XPoint(50, 150));

Supported image formats for embedding (every format CodeBrix.Imaging decodes):
    PNG, JPEG, BMP, WebP, GIF, TIFF, TGA, PBM/PGM/PPM
Formats that can carry transparency (PNG, WebP, GIF, BMP, TIFF, TGA) embed
losslessly with their alpha channel preserved; JPEG and PBM embed as JPEG.
Known decoder caveats (from CodeBrix.Imaging): no animated WebP, no
arithmetic-coded JPEG; animated GIF embeds its first frame.
SVG is NOT supported at this level - SVG placement is an Html2Pdf/Markdown2Pdf
feature (see PART 4).

--- DRAWING GRAPHICS ---

    // Lines
    renderer.DrawLine(XPens.Black, new XPoint(0, 0), new XPoint(100, 100));

    // Rectangles
    renderer.DrawRectangle(XPens.Black, XBrushes.LightBlue, 50, 50, 200, 100);

    // Ellipses
    renderer.DrawEllipse(XPens.Red, 50, 50, 100, 80);

--- TEXT LAYOUT (XTextFormatter) ---

For multi-line text with automatic line wrapping:

    using CodeBrix.PdfDocuments.Drawing.Layout;

    var tf = new XTextFormatter(renderer);
    var rect = new XRect(50, 50, 400, 200);  // x, y, width, height

    tf.DrawString(
        "This is a long text that will automatically wrap within the specified rectangle...",
        new XFont("Arial", 12),
        XBrushes.Black,
        rect,
        XStringFormats.TopLeft);

--- PREDEFINED BRUSHES AND PENS ---

    XBrushes.Black, XBrushes.White, XBrushes.Red, XBrushes.Blue,
    XBrushes.Green, XBrushes.DarkBlue, XBrushes.LightBlue,
    XBrushes.LightGray, XBrushes.Gray, ... (standard color names)

    XPens.Black, XPens.Red, XPens.Blue, ... (standard color names)

--- READING EXISTING PDFs ---

    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfDocuments.Pdf.IO;

    // Open for modification
    using var fs = File.OpenRead("existing.pdf");
    var document = PdfReader.Open(fs, PdfDocumentOpenMode.Modify);

    // Open for import (to merge pages into another document)
    var importDoc = PdfReader.Open(fs, PdfDocumentOpenMode.Import);

PdfDocumentOpenMode values:
    PdfDocumentOpenMode.Modify  - Open for editing
    PdfDocumentOpenMode.Import  - Open to extract pages for merging
    PdfDocumentOpenMode.ReadOnly - Open for reading only

--- MERGING PDFs ---

    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfDocuments.Pdf.IO;

    var outputDocument = new PdfDocument();

    foreach (var pdfPath in new[] { "doc1.pdf", "doc2.pdf", "doc3.pdf" })
    {
        using var fs = File.OpenRead(pdfPath);
        var inputDocument = PdfReader.Open(fs, PdfDocumentOpenMode.Import);

        for (var i = 0; i < inputDocument.PageCount; i++)
        {
            outputDocument.AddPage(inputDocument.Pages[i]);
        }
    }

    outputDocument.Save("merged.pdf");

--- IMAGE DATA CONSOLIDATION ---

When merging PDFs that contain duplicate images, consolidate to reduce file size:

    // After merging, consolidate duplicate image data
    // This can reduce file size by 75%+ when the same images appear repeatedly
    outputDocument.ConsolidateImageData();
    outputDocument.Save("optimized.pdf");

--- PDF SECURITY AND ENCRYPTION ---

Creating password-protected PDFs:

    var document = new PdfDocument();
    // ... add pages and content ...

    // Set security options
    document.SecuritySettings.UserPassword = "userPass";
    document.SecuritySettings.OwnerPassword = "ownerPass";

    document.Save("protected.pdf");

Opening encrypted PDFs:

    using var fs = File.OpenRead("protected.pdf");
    var document = PdfReader.Open(fs, "password", PdfDocumentOpenMode.Modify);

Supported encryption:
    - 40-bit encryption
    - 128-bit encryption (V2/R3)
    - AES encryption (V4/R4)
    - 256-bit AES encryption (V5/R5, V5/R6)

--- DOCUMENT OUTLINES (BOOKMARKS) ---

    var document = new PdfDocument();
    // ... add pages ...

    var outline = document.Outlines.Add("Chapter 1", document.Pages[0]);
    var subOutline = outline.Outlines.Add("Section 1.1", document.Pages[1]);

--- SAVING ---

Save to file:

    document.Save("output.pdf");

Save to stream:

    using var stream = new MemoryStream();
    document.Save(stream);
    byte[] pdfBytes = stream.ToArray();

================================================================================

PART 2: CodeBrix.PdfDocCreate (HIGH-LEVEL DOCUMENT MODEL)
============================================================

CodeBrix.PdfDocCreate provides a structured document object model similar to
how you'd think about a Word document: documents contain sections, sections
contain paragraphs, tables, and images.

--- CREATING A DOCUMENT ---

    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.Rendering;

    var doc = new Document();

--- DOCUMENT INFO ---

    doc.Info.Title = "Sales Report";
    doc.Info.Subject = "Quarterly Sales Data";
    doc.Info.Author = "CodeBrix";

--- DEFINING STYLES ---

    // Create a custom style based on "Normal"
    var titleStyle = doc.AddStyle("Title", "Normal");
    titleStyle.Font.Size = 24;
    titleStyle.Font.Bold = true;
    titleStyle.ParagraphFormat.SpaceAfter = 6;
    titleStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;

    // Modify the default "Normal" style
    var bodyStyle = doc.Styles["Normal"];
    bodyStyle.Font.Size = 10;
    bodyStyle.Font.Name = "Arial";
    bodyStyle.ParagraphFormat.SpaceAfter = 4;

    // Modify the BUILT-IN heading styles - do NOT call AddStyle for these.
    // Heading1..Heading9 already exist (see BUILT-IN STYLE NAMES below).
    var h1 = doc.Styles["Heading1"];
    h1.Font.Size = 18;
    h1.Font.Bold = true;
    h1.ParagraphFormat.SpaceBefore = 12;
    h1.ParagraphFormat.SpaceAfter = 6;

    var h2 = doc.Styles["Heading2"];
    h2.Font.Size = 14;
    h2.Font.Bold = true;
    h2.ParagraphFormat.SpaceBefore = 8;
    h2.ParagraphFormat.SpaceAfter = 4;

--- BUILT-IN STYLE NAMES ---

Every Document starts with these styles already defined. Their names are also
available as constants on the StyleNames class:

    StyleNames.Normal                 "Normal"
    StyleNames.Heading1 .. Heading9   "Heading1" ... "Heading9"
    StyleNames.DefaultParagraphFont   "DefaultParagraphFont"
    StyleNames.Footnote               "Footnote"
    StyleNames.Header                 "Header"
    StyleNames.Footer                 "Footer"
    StyleNames.Hyperlink              "Hyperlink"

Use doc.AddStyle(name, baseStyleName) ONLY for names that are not in this list.
To change a built-in style, fetch it with doc.Styles[name] and set properties
on the object you get back.

The heading styles are pre-wired in two ways that AddStyle would destroy:

  - Each carries a ParagraphFormat.OutlineLevel (Heading1 -> Level1, and so on).
    OutlineLevel is what drives PDF outline/bookmark generation.
  - They form an inheritance chain. Heading2's base style is "Heading1" - NOT
    "Normal" - Heading3's is "Heading2", and so on. Setting Font.Name on
    Heading1 therefore flows down to every deeper heading.

ParagraphAlignment values:
    ParagraphAlignment.Left
    ParagraphAlignment.Center
    ParagraphAlignment.Right
    ParagraphAlignment.Justify

--- ADDING SECTIONS ---

    var section = doc.AddSection();

    // Page setup for the section
    section.PageSetup.PageFormat = PageFormat.Letter;
    section.PageSetup.Orientation = Orientation.Portrait;
    section.PageSetup.TopMargin = Unit.FromCentimeter(2.5);
    section.PageSetup.BottomMargin = Unit.FromCentimeter(2.5);
    section.PageSetup.LeftMargin = Unit.FromCentimeter(2.5);
    section.PageSetup.RightMargin = Unit.FromCentimeter(2.5);

--- ADDING PARAGRAPHS ---

    var para = section.AddParagraph("This is body text.");
    para.Style = "Normal";

    var title = section.AddParagraph("Document Title");
    title.Style = "Title";

    var heading = section.AddParagraph("Chapter 1");
    heading.Style = "Heading1";

    // Inline formatting
    var para = section.AddParagraph();
    para.AddText("Normal text ");
    var bold = para.AddFormattedText("bold text", TextFormat.Bold);
    para.AddText(" more normal text.");

    // Hard line break within a paragraph (does NOT start a new paragraph)
    para.AddLineBreak();
    para.AddText("Second line of the same paragraph.");

--- BULLET AND NUMBERED LISTS ---

Lists are built with ParagraphFormat.ListInfo. Do NOT fake them with a
borderless table or with literal "* " prefixes.

    var item1 = section.AddParagraph("First item");
    item1.Format.ListInfo.ListType = ListType.BulletList1;
    item1.Format.ListInfo.ContinuePreviousList = false;   // false starts a new list
    item1.Format.LeftIndent = Unit.FromCentimeter(0.75);

    var item2 = section.AddParagraph("Second item");
    item2.Format.ListInfo.ListType = ListType.BulletList1;
    item2.Format.ListInfo.ContinuePreviousList = true;    // true continues the list

ListType values:
    ListType.BulletList1, BulletList2, BulletList3    (three nesting levels)
    ListType.NumberList1, NumberList2, NumberList3

ListInfo also exposes NumberPosition (Unit), which sets where the bullet or
number is drawn.

For correct hanging indents at any nesting depth, combine a negative
FirstLineIndent with a tab stop at the LeftIndent position:

    item.Format.LeftIndent = Unit.FromCentimeter(0.75);
    item.Format.FirstLineIndent = Unit.FromCentimeter(-0.75);
    item.Format.AddTabStop(Unit.FromCentimeter(0.75), TabAlignment.Left);

--- HYPERLINKS AND BOOKMARKS ---

    // Web link
    var para = section.AddParagraph();
    para.AddText("See ");
    var link = para.AddHyperlink("https://example.com", HyperlinkType.Web);
    link.AddText("the documentation");

HyperlinkType values:
    HyperlinkType.Web       (also spelled HyperlinkType.Url)
    HyperlinkType.Local     (also spelled HyperlinkType.Bookmark) - same document
    HyperlinkType.File      - another file

    // Internal navigation: mark a target, then link to it by name
    section.AddParagraph("Chapter 1").AddBookmark("ch1");

    var toc = section.AddParagraph();
    toc.AddHyperlink("ch1", HyperlinkType.Bookmark).AddText("Jump to Chapter 1");

Note the difference from document outlines: AddBookmark creates a named jump
target INSIDE the document model, whereas PdfDocument.Outlines (see PART 1)
builds the reader's navigation pane.

--- TAB STOPS ---

Useful for aligned columns without the weight of a table - for example a table
of contents with dot leaders.

    var line = section.AddParagraph();
    line.Format.AddTabStop(Unit.FromCentimeter(8), TabAlignment.Right, TabLeader.Dots);
    line.AddText("Chapter 1");
    line.AddTab();
    line.AddText("14");

TabAlignment values:  Left, Center, Right, Decimal
TabLeader values:     Spaces, Dots, Dashes, Lines, Heavy, MiddleDot

ParagraphFormat.TabStops is the underlying collection; AddTabStop adds to it,
and ClearAll() removes any tab stops inherited from the style.

--- PARAGRAPH SHADING AND BORDERS ---

Shading is not limited to table cells - a paragraph can have its own
background and borders, which makes it a good building block for callouts.

    var callout = section.AddParagraph("Note: this is important.");
    callout.Format.Shading.Color = Colors.LightYellow;
    callout.Format.Borders.Width = 0.5;
    callout.Format.Borders.Color = Colors.Orange;

Shading members: Visible (bool), Color (Color), IsCleared (bool).

You do NOT need to set Visible = true. If Visible was never assigned, the
renderer treats the shading as visible whenever Color has been set, so setting
Color alone is enough. Reading Shading.Visible on such a paragraph still
returns false - that is the unassigned flag, not the effective state. Assign
Visible = false explicitly if you want to suppress a shading inherited from a
style.

A paragraph border is also the cleanest way to draw a horizontal rule - put a
top border on a paragraph with a tiny font:

    var rule = section.AddParagraph();
    rule.Format.Font.Size = 1;
    rule.Format.Borders.Top.Width = 0.75;
    rule.Format.Borders.Top.Color = Colors.Gray;

--- ADDING TABLES ---

    using CodeBrix.PdfDocCreate.DocumentObjectModel.Tables;

    var table = section.AddTable();
    table.Borders.Visible = true;

    // Add columns
    table.AddColumn(Unit.FromCentimeter(3));
    table.AddColumn(Unit.FromCentimeter(5));
    table.AddColumn(Unit.FromCentimeter(3));

    // Add header row
    var headerRow = table.AddRow();
    headerRow.Cells[0].AddParagraph("Name");
    headerRow.Cells[1].AddParagraph("Description");
    headerRow.Cells[2].AddParagraph("Price");
    headerRow.Shading.Color = Colors.LightGray;

    // Add data rows
    var row = table.AddRow();
    row.Cells[0].AddParagraph("Widget");
    row.Cells[1].AddParagraph("A useful widget");
    row.Cells[2].AddParagraph("$9.99");

Table cell merging:

    // Merge cells horizontally
    row.Cells[0].MergeRight = 1;  // Merge with the cell to the right

    // Merge cells vertically
    row.Cells[1].MergeDown = 1;   // Merge with the cell below

Cell shading:

    cell.Shading.Color = Colors.LightBlue;

Row height:

    row.HeightRule = RowHeightRule.Exactly;
    row.Height = 14;

Vertical alignment:

    row.VerticalAlignment = VerticalAlignment.Center;
    row.VerticalAlignment = VerticalAlignment.Top;
    row.VerticalAlignment = VerticalAlignment.Bottom;

Border styling:

    cell.Borders.Left.Width = 8;
    cell.Borders.Right.Width = 2;
    cell.Borders.Bottom.Width = 15;
    cell.Borders.Visible = true;

--- ADDING IMAGES ---

    var image = section.AddImage("photo.png");
    image.Width = Unit.FromCentimeter(8);
    image.Height = Unit.FromCentimeter(6);

--- HEADERS AND FOOTERS ---

    // Add header
    var header = section.Headers.Primary;
    var headerPara = header.AddParagraph("Document Header");
    headerPara.Format.Alignment = ParagraphAlignment.Center;

    // Add footer with page numbers
    var footer = section.Footers.Primary;
    var footerPara = footer.AddParagraph();
    footerPara.AddText("Page ");
    footerPara.AddPageField();
    footerPara.AddText(" of ");
    footerPara.AddNumPagesField();
    footerPara.Format.Alignment = ParagraphAlignment.Center;

--- PREDEFINED COLORS ---

    Colors.Black, Colors.White, Colors.Red, Colors.Blue, Colors.Green,
    Colors.LightBlue, Colors.LightGray, Colors.Gray, Colors.DarkBlue,
    Colors.Yellow, Colors.Orange, ... (standard color names)

--- CONSTRUCTING CUSTOM COLORS ---

    // RGB - all of these are fully opaque
    var c1 = Color.FromRgb(255, 253, 231);
    var c2 = new Color(255, 253, 231);           // identical to FromRgb

    // RGB with an explicit alpha (0 = transparent, 255 = opaque)
    var c3 = Color.FromArgb(128, 255, 253, 231);
    var c4 = new Color(128, 255, 253, 231);      // identical to FromArgb
    var c5 = Color.FromRgbColor(128, Colors.SteelBlue);   // recolor the alpha only

    // Packed ARGB integer, in the form 0xAARRGGBB
    var c6 = new Color(0xFFFFFDE7);

    // CMYK - all values are percentages, 0 to 100
    var c7 = Color.FromCmyk(0, 1, 9, 0);
    var c8 = Color.FromCmyk(80, 0, 1, 9, 0);              // leading value is alpha
    var c9 = Color.FromCmykColor(80, Colors.SteelBlue);

    // From a string
    var c10 = Color.Parse("SteelBlue");   // any name from the Colors class
    var c11 = Color.Parse("#fffde7");     // CSS hex - always OPAQUE
    var c12 = Color.Parse("#ffd");        // CSS shorthand, expands to #ffffdd
    var c13 = Color.Parse("0xFFFFFDE7");  // packed 0xAARRGGBB

Reading components back:

    color.A, color.R, color.G, color.B      // uint, 0-255
    color.C, color.M, color.Y, color.K      // double, 0-100 (CMYK)
    color.Alpha                             // double, 0-100 (CMYK alpha)
    Color.Empty                             // the "no color" value

WARNING - the two hex spellings do NOT mean the same thing. The prefix decides:

    Color.Parse("#c0c0c0")     -> A=255. Light grey, as in CSS.
    Color.Parse("0xc0c0c0")    -> A=0.   FULLY TRANSPARENT, not light grey.

"0x" introduces a packed 0xAARRGGBB integer, so six digits leave the alpha byte
at zero and the color silently disappears. Write the alpha explicitly
("0xFFC0C0C0"), or use the CSS form, or use Color.FromRgb. See pitfall #18.

An eight-digit "#" value is REJECTED rather than guessed at, because CSS writes
#rrggbbaa with the alpha last while "0x" puts it first - the two orders cannot
be told apart. Use the "0x" form when you need to specify alpha in a string.

--- RENDERING TO PDF ---

    var pdfRenderer = new PdfDocumentRenderer
    {
        Document = doc
    };
    pdfRenderer.RenderDocument();
    pdfRenderer.PdfDocument.Save("output.pdf");

Save to stream:

    using var stream = new MemoryStream();
    pdfRenderer.PdfDocument.Save(stream);
    byte[] pdfBytes = stream.ToArray();

Access the underlying PdfDocument for additional manipulation:

    var pdfDoc = pdfRenderer.PdfDocument;
    // Add security, outlines, etc.

================================================================================

PART 3: CodeBrix.PdfRasterizer (PDF-TO-IMAGE RENDERING)
=========================================================

CodeBrix.PdfRasterizer renders PDF pages to raster images using the PDFium
native rendering engine. It is a standalone library that depends on
CodeBrix.PdfDocuments (for PdfDocument input support) and CodeBrix.Imaging
(for image output).

IMPORTANT: The PDFium native binaries are bundled inside the NuGet package.
You do NOT need to install PDFium separately. The correct native library
for your platform is automatically copied to the build output directory.
PDFium binaries are included for Windows (x64, x86, ARM64), macOS (x64,
ARM64), Linux (x64, ARM64, ARM, RISC-V 64), and Android (ARM64). The
library will fail at runtime on unsupported platforms such as iOS or
WebAssembly where no PDFium binary is bundled.

The main class is PageRasterizer (sealed, IDisposable). Create an instance,
configure properties, call rasterize methods, then dispose.

TIP - VISUAL REGRESSION TESTING: pairing PdfRasterizer with PdfDocuments or
PdfDocCreate gives you an automated way to prove that a document still looks
right. Generate the PDF, rasterize it to images, and compare those images
against approved baselines byte-for-byte or with a perceptual diff. Layout
regressions that no unit test would catch - a shifted table, a font that
silently substituted, a paragraph that now spills onto a second page - show up
immediately. Use a fixed Dpi so the baselines stay stable.

IMPORTANT: PDFium is NOT thread-safe. All calls to the native library are
serialized internally via SemaphoreSlim. You CAN share a single
PageRasterizer instance across threads, but calls will be serialized.

--- CONSTRUCTOR ---

    using CodeBrix.PdfRasterizer;

    // Basic construction
    using var rasterizer = new PageRasterizer();

    // With optional logger and password
    using var rasterizer = new PageRasterizer(
        logger: msg => Console.WriteLine(msg),
        password: "secretPassword");

--- PROPERTIES ---

    rasterizer.Dpi = 300;
        // Default rendering DPI (default: 300). Values < 1 reset to 300.

    rasterizer.Password = "secret";
        // Default password for encrypted PDFs. null = no password.
        // Empty/whitespace strings are treated as null.

    rasterizer.OutputDirectory = @"C:\Output";
        // Default output directory for file-writing methods.
        // Required by RasterizeToImageFiles/RasterizeToThumbnailFiles
        // if not passed as a method parameter.

    rasterizer.RasterizedImageFormat = JpegFormat.Instance;
        // Default image format (default: PngFormat.Instance).
        // Cannot be set to UnknownImageFormat (throws ArgumentException).
        // Cannot be null (throws ArgumentNullException).

    rasterizer.FileNameGenerator = pageNum => $"Page_{pageNum}";
        // Function that generates file name stems (without extension).
        // Default: pageNumber => $"Rasterized_Page_{pageNumber}"
        // Setting to null resets to default.

    rasterizer.AllowOverwriteFiles = true;
        // Whether to overwrite existing files (default: false).
        // When false, IOException is thrown if a target file exists.

    rasterizer.BackgroundColor = 0xFFFFFFFF;
        // ARGB background color (default: opaque white 0xFFFFFFFF).
        // Drawn behind all page content.

    rasterizer.ThumbnailMaxDimensions = new ThumbnailMaxDimensions(150, 200);
        // Default max dimensions for thumbnail methods.
        // Setting to null reverts to built-in default (200x260 pixels).

--- RASTERIZE ALL PAGES TO IMAGE FILES ---

    // From file path
    await rasterizer.RasterizeToImageFiles("document.pdf");

    // From byte array
    byte[] pdfBytes = File.ReadAllBytes("document.pdf");
    await rasterizer.RasterizeToImageFiles(pdfBytes, outputDirectory: @"C:\Output");

    // From stream
    using var stream = File.OpenRead("document.pdf");
    await rasterizer.RasterizeToImageFiles(stream, outputDirectory: @"C:\Output");

    // From PdfDocument object
    var pdfDocument = new PdfDocument();
    // ... build document ...
    await rasterizer.RasterizeToImageFiles(pdfDocument, outputDirectory: @"C:\Output");

    // With all options
    await rasterizer.RasterizeToImageFiles(
        "document.pdf",
        outputDirectory: @"C:\Output",
        dpi: 150,
        password: "secret",
        desiredImageFormat: JpegFormat.Instance,
        pageNumbers: new[] { 1, 3, 5 },  // specific pages (1-based)
        cancellationToken: ct);

--- RASTERIZE SINGLE PAGE TO IMAGE FILE ---

    await rasterizer.RasterizeToImageFile(
        "document.pdf",
        pageNumber: 1,
        outputDirectory: @"C:\Output");

--- RASTERIZE ALL PAGES TO IN-MEMORY IMAGES ---

    // Returns IList<Image> - caller must dispose each image
    IList<Image> images = await rasterizer.RasterizeToImages("document.pdf");

    foreach (var image in images)
    {
        // Process image...
        image.Dispose();
    }

    // With specific page numbers
    IList<Image> pages = await rasterizer.RasterizeToImages(
        pdfBytes,
        pageNumbers: new[] { 1, 2 },
        desiredImageFormat: JpegFormat.Instance);

--- RASTERIZE SINGLE PAGE TO IN-MEMORY IMAGE ---

    // Returns Image - caller must dispose
    using var image = await rasterizer.RasterizeToImage(
        "document.pdf",
        pageNumber: 1);

    // Save to file
    await image.SaveAsync("page1.png");

    // Access dimensions
    Console.WriteLine($"Size: {image.Width} x {image.Height}");

--- GENERATE THUMBNAILS ---

Thumbnails are rasterized images that have been resized to fit within a
bounding box while preserving the page's aspect ratio. Only downscaled;
images already fitting within the box are left as-is.

    // Built-in default: 200x260 pixels
    // Custom: new ThumbnailMaxDimensions(maxWidth, maxHeight)

    // To in-memory images
    IList<Image> thumbs = await rasterizer.RasterizeToThumbnails("document.pdf");

    // With custom dimensions
    var maxDims = new ThumbnailMaxDimensions(150, 200);
    IList<Image> thumbs = await rasterizer.RasterizeToThumbnails(
        "document.pdf",
        maxDimensions: maxDims);

    // Single page thumbnail
    using var thumb = await rasterizer.RasterizeToThumbnail(
        "document.pdf",
        pageNumber: 1);

    // To thumbnail files
    await rasterizer.RasterizeToThumbnailFiles(
        "document.pdf",
        maxDimensions: maxDims,
        outputDirectory: @"C:\Thumbnails");

    // Single page thumbnail file
    await rasterizer.RasterizeToThumbnailFile(
        "document.pdf",
        pageNumber: 1,
        outputDirectory: @"C:\Thumbnails");

--- PAGE INFORMATION ---

    // Get page count
    int count = await rasterizer.GetPageCount("document.pdf");

    // From byte array
    int count = await rasterizer.GetPageCount(pdfBytes);

    // From stream
    int count = await rasterizer.GetPageCount(stream);

    // From PdfDocument
    int count = await rasterizer.GetPageCount(pdfDocument);

    // Get page dimensions (returns PdfPageDimensions record)
    PdfPageDimensions dims = await rasterizer.GetPageDimensions(
        "document.pdf",
        pageNumber: 1);  // 1-based

    double widthPt   = dims.WidthInPoints;       // points (1/72 inch)
    double heightPt  = dims.HeightInPoints;
    double widthIn   = dims.WidthInInches;        // computed
    double heightIn  = dims.HeightInInches;
    int    widthPx   = dims.GetWidthInPixels(300);  // at 300 DPI
    int    heightPx  = dims.GetHeightInPixels(300);

--- INPUT SOURCES ---

All rasterization methods have overloads accepting:

  1. string pdfPath         - File path to a PDF
  2. byte[] pdfBytes        - PDF content as byte array
  3. Stream pdfStream       - Readable stream containing PDF data
  4. PdfDocument pdfDocument - A CodeBrix.PdfDocuments.Pdf.PdfDocument object

The PdfDocument overload serializes the document to a byte array internally,
so it works with in-memory documents that haven't been saved to disk.

--- IMAGE FORMATS ---

Supported output formats (from CodeBrix.Imaging):

    PngFormat.Instance      // Default. Lossless, supports transparency.
    JpegFormat.Instance     // Lossy, smaller files, no transparency.
    BmpFormat.Instance      // Uncompressed bitmap.
    GifFormat.Instance      // 256 colors, supports transparency.
    TiffFormat.Instance     // Lossless, supports multi-page.

Set per-call via desiredImageFormat parameter, or as default via the
RasterizedImageFormat property.

--- RENDER FLAGS ---

    PdfRenderFlags.None                    // No special flags
    PdfRenderFlags.RenderAnnotations       // Render form fields/annotations
    PdfRenderFlags.OptimizeTextForLcd      // LCD-optimized text
    PdfRenderFlags.NoNativeText            // Don't use native text rendering
    PdfRenderFlags.Grayscale               // Render in grayscale
    PdfRenderFlags.RenderForPrinting       // Print-quality rendering
    PdfRenderFlags.DisableTextAntialiasing  // No text anti-aliasing
    // ... and more (see PdfRenderFlags enum)

Default: RenderAnnotations | RenderForPrinting

--- DISPOSING ---

PageRasterizer implements IDisposable. Always dispose when done:

    using var rasterizer = new PageRasterizer();
    // ... use rasterizer ...
    // Disposed automatically at end of scope

After disposal, all rasterization methods throw ObjectDisposedException.

NOTE: Disposing PageRasterizer does NOT unload PDFium. The native library
is initialized once per application lifetime and remains loaded.

--- CANCELLATION ---

All async methods accept CancellationToken:

    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    var images = await rasterizer.RasterizeToImages(
        "large-document.pdf",
        cancellationToken: cts.Token);

--- ENCRYPTED PDFS ---

    // Via constructor
    using var rasterizer = new PageRasterizer(password: "secret");

    // Via property
    rasterizer.Password = "secret";

    // Via method parameter (overrides property)
    var images = await rasterizer.RasterizeToImages(
        "encrypted.pdf",
        password: "secret");

--- PARAMETER RESOLUTION ORDER ---

For parameters like dpi, password, outputDirectory, desiredImageFormat,
and maxDimensions, the resolution order is:

  1. Method parameter (if provided and non-null)
  2. Property value (if set)
  3. Built-in default (Dpi=300, format=PNG, thumbnail=200x260)

This allows configuring once via properties, then overriding per-call.

================================================================================

PART 4: CodeBrix.PdfDocCreate.Html2Pdf (HTML+CSS TO PDF)
==========================================================

Renders author-created HTML with CSS styling into PDF. HTML parses with
CodeBrix.MarkupParse; the CSS dialect below is applied with real selector
matching (CodeBrix.MarkupParse's selector engine), cascade, specificity and
inheritance (stylesheets parse with CodeBrix.StyleSheetParse); the result is
composed onto the PdfDocCreate document object model, whose renderer performs
all layout (line breaking, pagination, tables).

DESIGN SCOPE: this is for HTML/CSS an author writes FOR PDF generation. It is
NOT a browser: floats, positioning, flexbox/grid, JavaScript, media queries,
CSS variables and calc() are out of scope. Unsupported CSS never fails a
render - it is ignored and reported in the result's Warnings collection.

--- BASIC USE ---

    using CodeBrix.PdfDocCreate.Html2Pdf;

    var renderer = new HtmlPdfRenderer();

    // From a file (relative stylesheet/image references resolve to its folder)
    var result = renderer.RenderFile("report.html", "report.pdf");

    // From a string, to a file or to bytes
    result = renderer.RenderHtml(html, "out.pdf", baseDirectory: "assets");
    result = renderer.RenderHtmlToBytes(html);

    Console.WriteLine($"{result.PageCount} pages, {result.Warnings.Count} warnings");
    foreach (var warning in result.Warnings.Messages) { Console.WriteLine(warning); }

    // Structured warnings for test baselines: a stable machine-readable Code
    // (e.g. "font.uncovered.removed", "image.format.unsupported"), a Category
    // enum, an occurrence count, and - for glyph-coverage warnings - the code
    // point involved. Finer-grained than Messages: distinct dropped code
    // points are separate items even when their display message collapses.
    foreach (var item in result.Warnings.Items)
    {
        Console.WriteLine($"{item.Category} {item.Code} x{item.Occurrences} U+{item.CodePoint:X4}");
    }

--- OPTIONS (HtmlRenderOptions, all with sensible defaults) ---

    renderer.Options.SetPageSize("a4");        // letter (default), legal, a3..b5
    renderer.Options.Landscape = true;
    renderer.Options.MarginTopPoints = 54;     // margins in points (default 72)
    renderer.Options.HeaderText = "{title}";   // centered; {page}, {pages}, {title}
    renderer.Options.FooterText = "Page {page} of {pages}";
    renderer.Options.AllowRemoteImages = true; // http(s) images; off by default
    renderer.Options.GenerateOutline = false;  // h1-h6 -> PDF bookmark pane (on)
    renderer.Options.DocumentTitle = "Override Title";  // else the <title> element
    renderer.Options.SvgRasterScale = 3.0;     // SVG raster sharpness (default 2.0)
    renderer.Options.KeepUncoveredCharacters = true;  // tofu instead of removal (off)

@page rules in the document's CSS override the configured size and margins:

    @page { size: a4 landscape; margin: 2cm; }

--- SUPPORTED HTML ELEMENTS ---

Block: h1-h6, p, div, section, article, main, header, footer, aside, nav,
blockquote, pre, hr, ul, ol, li (nested to any depth), dl/dt/dd, figure,
figcaption, table/thead/tbody/tfoot/tr/th/td (colspan AND rowspan; automatic
content-measured column widths), img, svg, details/summary, address.
Inline: a (real link annotations: web, mailto, and #anchor bookmarks), span,
strong/b, em/i, u/ins, s/del/strike, code/kbd/samp, sub, sup, small, br, img,
svg.
Ignored (with a warning where meaningful): script, style/link (consumed for
CSS), iframe, canvas, audio, video, form controls.

--- IMAGES AND SVG ---

img sources may be local files (relative paths resolve against the document's
base directory and work with EITHER separator style on every OS), data: URIs,
or - only when AllowRemoteImages is enabled - http(s) URLs. Every format
CodeBrix.Imaging decodes embeds: PNG, JPEG, BMP, WebP, GIF, TIFF, TGA,
PBM/PGM/PPM. Alpha-capable formats keep their transparency losslessly.

SVG is fully supported and renders through an offscreen CPU rasterizer
(CodeBrix.SkiaSvg) to a transparent PNG - identical output on Windows, macOS
and Linux, no GPU or window system involved:
  - <img src="figure.svg">, data:image/svg+xml URIs (base64 or percent-encoded),
    and inline <svg>...</svg> elements (block or inside a paragraph) all render.
  - The SVG's own width/height/viewBox decides its natural size (1 CSS px =
    0.75 pt); CSS width/height on the element (including physical units like
    mm) override it.
  - Options.SvgRasterScale (default 2.0, about 192 DPI at natural size) sets
    the raster sharpness; it never changes the placed size.
  - SVG <text> renders with the registered document fonts only (see FONTS);
    system fonts are never consulted. font-family values are candidate LISTS
    ("Some Face,serif") tried in order; generic families map to the package
    defaults (including SVG-style spellings "sans" and "mono"), and a family
    no registered font provides falls back to the default sans face - the
    same behavior as HTML text.
  - SVG text has per-glyph font fallback, driven by the same fallback chain
    HTML text uses (AddFallbackFamily / includeInFallback; Noto Music joins
    automatically): before rasterization, characters the styled face lacks
    are wrapped in tspans naming the covering fallback family. A character NO
    registered font covers renders as its missing-glyph shape - and WARNS,
    one structured item per distinct code point (code "font.svg-text.notdef",
    with the code point and an occurrence count), so coverage gaps are
    baselined instead of invisible.
  - A broken or unrenderable SVG degrades to a collected warning, never an
    exception.

--- SUPPORTED CSS DIALECT ---

Sources: inline style="" attributes, <style> blocks, and <link rel="stylesheet">
to LOCAL .css files. Remote stylesheets are skipped with a warning.

Properties: font-family, font-size (absolute, %, em, rem, keywords),
font-weight (normal/bold/bolder/lighter/100-900 - numeric weights select real
static font faces), font-style, color, background-color (and the color of the
background shorthand), line-height, text-align, text-decoration (underline,
line-through, none), text-indent, text-transform, white-space, margin/padding
(+ per-side longhands), border (+ per-side and width/style/color longhands;
solid/dashed/dotted), width/height (img, table, td), page-break-before/after
(and break-before/after), list-style-type, display (none), vertical-align.
Units: pt, px (0.75pt), em, rem, %, in, cm, mm, pc. Colors: named, #hex,
rgb()/rgba(). Selectors: the full CodeBrix.MarkupParse engine (type, .class,
#id, attribute, combinators, :nth-child and friends). !important is honored.
Cascade: built-in default sheet < author rules (specificity, then source
order) < inline style; author !important above all non-important layers.

--- FONTS (IMPORTANT) ---

All text renders with the CodeBrix.Platform.Fonts package fonts - NEVER with
operating-system fonts - so output is byte-comparable across machines:

    sans-serif (and unknown families)  ->  Roboto
    serif                              ->  Merriweather
    monospace                          ->  Roboto Mono

The font packages ship .ttf files inside their nupkgs, and the Html2Pdf
package's buildTransitive targets copy them into the consuming application's
build output under CodeBrix.Platform.Fonts.<Name>/Fonts/. The renderer
discovers them there automatically (Html2PdfFonts). A missing font layout
produces an InvalidOperationException naming the fix.

CONSUMER FONT REGISTRATION (all methods on the static Html2PdfFonts class;
all are idempotent, and all may also be called AFTER renders have happened -
additions take effect on the next render):

    // Package-shaped directories (CodeBrix.Platform.Fonts.<Name>/Fonts/ with
    // .ttf.manifest files) living somewhere unusual:
    Html2PdfFonts.AddFontDirectory(path);

    // Loose .ttf/.otf files - NO manifest needed; family name, weight and
    // style are read from the font's own name and OS/2 tables:
    Html2PdfFonts.AddFontFile("MyFont-Regular.ttf");
    Html2PdfFonts.AddFontFiles(paths);
    Html2PdfFonts.AddFontFilesFromDirectory(dir);

    // Per-glyph fallback: consulted (in registration order) for characters
    // the styled font lacks. Pass includeInFallback: true on the Add* calls,
    // or name an already-registered family:
    Html2PdfFonts.AddFallbackFamily("Family Name");

A registered font is usable everywhere at once: CSS font-family values, SVG
<text> font-family values, and per-glyph fallback (when opted in). The first
registration of a family name wins; later registrations of the same family
are silently ignored. File path arguments accept either separator style on
every operating system.

GLYPH COVERAGE is decided per character against the actual cmap table of the
font each run resolved to - not against assumed Unicode ranges - so adding a
font package or file extends what renders with no code change:
  - A character the styled font covers renders with it.
  - A character it lacks renders with the first fallback family that covers
    it (the run is split around it; only that character switches font).
  - A character nothing covers is REMOVED with a collected warning by
    default; set Options.KeepUncoveredCharacters = true to keep such
    characters and render the font's visible missing-glyph shape instead, so
    a coverage gap leaves a trace on the page.
Supplementary-plane characters (above U+FFFF, e.g. musical notation) are
handled as single code points end to end and embed correctly when a
registered font provides them through a cmap format 12 table.
Music notation families (CodeBrix.Platform.Fonts.NotoMusic*) are wired into
the fallback chain automatically when discovered, and are never a body-text
default.

================================================================================

PART 5: CodeBrix.PdfDocCreate.Markdown2Pdf (MARKDOWN TO PDF)
==============================================================

Point it at ANY .md file and get a nice-looking, pre-formatted, printable PDF
with zero configuration. Markdown parses through a vendored C# port of
markdown-it (CommonMark plus GFM tables and strikethrough, plus footnote,
task-list and YAML front-matter plugins, plus syntax highlighting for common
fence languages), converts to HTML with a polished built-in stylesheet, and
renders through Html2Pdf.

--- WORKFLOW (a): zero-config Markdown to PDF ---

    using CodeBrix.PdfDocCreate.Markdown2Pdf;

    var renderer = new MarkdownPdfRenderer();

    // Writes my_notes.pdf next to the source file
    var result = renderer.RenderFile("my_notes.md");

    // Or control the output location / render from a string / get bytes
    result = renderer.RenderFile("my_notes.md", "out/notes.pdf");
    result = renderer.RenderMarkdown(markdownText, "out.pdf", baseDirectory);
    result = renderer.RenderMarkdownToBytes(markdownText);

Defaults chosen for you: US Letter pages, book-ish margins, Merriweather body
text, Roboto headings, Roboto Mono code with syntax-highlight colors, footer
"page / pages" numbers, and a PDF outline built from the headings. The title
is inferred from YAML front matter (title:), else the first heading, else the
file name; front-matter author: fills the PDF author metadata.

The only options (MarkdownRenderOptions): PageSize ("letter" default),
AllowRemoteImages (false), FooterText ("{page} / {pages}"; null disables),
SvgRasterScale (2.0), KeepUncoveredCharacters (false) - the last two are
forwarded to Html2Pdf and mean the same things there.

--- WORKFLOW (b): restyle the generated HTML/CSS yourself ---

Consumers who want a different look intercept the HTML/CSS instead of asking
Markdown2Pdf for styling knobs:

    var generated = renderer.GenerateHtmlFromFile("my_notes.md");
    // generated.BodyHtml - the rendered markup
    // generated.Css      - the default stylesheet (Html2Pdf dialect)
    // generated.Title    - the inferred title

    var myCss = generated.Css.Replace("#1c1c1c", "#000033");   // or replace wholesale
    var html = generated.ToHtmlDocument(myCss);

    var htmlRenderer = new HtmlPdfRenderer();
    htmlRenderer.RenderHtml(html, "restyled.pdf", generated.BaseDirectory);

--- MARKDOWN FEATURES ---

CommonMark (verified against the full specification example corpus), GFM
tables (with column alignment) and strikethrough, footnotes ([^label] and
inline ^[note]), GitHub task lists (- [ ] / - [x], rendered as checkbox
glyphs), YAML front matter (consumed, never rendered; title/author mined),
reference links and images, autolinks, embedded HTML (rendered through
Html2Pdf's documented element subset, inline <svg> included), and fenced code
with automatic syntax highlighting for csharp, c/cpp, bash/shell/powershell,
json, xml/html/xaml, javascript/typescript, python, sql and yaml.

Images in Markdown support the same formats as Html2Pdf (PNG, JPEG, BMP,
WebP, GIF, TIFF, TGA, PBM/PGM/PPM, and SVG), referenced as relative/absolute
paths (either separator style works on every OS) or data: URIs - the data:
URI allow-list admits exactly those image types and rejects everything else.

Robustness contract: any .md input produces a document - unsupported
constructs degrade and are reported in Warnings, never thrown.

--- THE markdown-it PORT (advanced) ---

CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.MarkdownParser is a faithful C#
port of the markdown-it JavaScript parser and can be used directly for
Markdown-to-HTML work, including custom plugins:

    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

    var md = new MarkdownParser();                    // markdown-it "default" preset
    var html = md.Render("# hello **world**");
    var strict = new MarkdownParser(MarkdownPreset.CommonMark);

    md.Use(parser => parser.Inline.Ruler.After("emphasis", "my_rule", MyRule));

================================================================================

COMPLETE EXAMPLES
=================

Example 1: Simple PDF with Text and Image
--------------------------------------------
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;

    var document = new PdfDocument();
    document.Info.Title = "My Document";

    var page = document.AddPage();
    var renderer = XGraphics.FromPdfPage(page);

    renderer.DrawString("PDF with Image",
        new XFont("Arial", 16),
        XBrushes.Black,
        new XPoint(12, 24));

    renderer.DrawImage(XImage.FromFile("photo.png"), new XPoint(12, 50));

    document.Save("ImageDocument.pdf");

Example 2: Merge Multiple PDFs
---------------------------------
    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfDocuments.Pdf.IO;

    var output = new PdfDocument();

    foreach (var path in Directory.GetFiles("pdfs/", "*.pdf"))
    {
        using var fs = File.OpenRead(path);
        var input = PdfReader.Open(fs, PdfDocumentOpenMode.Import);
        for (var i = 0; i < input.PageCount; i++)
            output.AddPage(input.Pages[i]);
    }

    output.Save("merged.pdf");

Example 3: Styled Report with Tables (PdfDocCreate)
------------------------------------------------------
    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.DocumentObjectModel.Tables;
    using CodeBrix.PdfDocCreate.Rendering;

    var doc = new Document();
    doc.Info.Title = "Sales Report";

    // Styles
    var titleStyle = doc.AddStyle("Title", "Normal");
    titleStyle.Font.Size = 24;
    titleStyle.Font.Bold = true;
    titleStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;
    titleStyle.ParagraphFormat.SpaceAfter = 12;

    doc.Styles["Normal"].Font.Size = 10;

    // Section with margins
    var section = doc.AddSection();
    section.PageSetup.TopMargin = Unit.FromCentimeter(2.5);
    section.PageSetup.BottomMargin = Unit.FromCentimeter(2.5);
    section.PageSetup.LeftMargin = Unit.FromCentimeter(2.5);
    section.PageSetup.RightMargin = Unit.FromCentimeter(2.5);

    // Title
    section.AddParagraph("Quarterly Sales Report").Style = "Title";

    // Table
    var table = section.AddTable();
    table.Borders.Visible = true;
    table.AddColumn(Unit.FromCentimeter(4));
    table.AddColumn(Unit.FromCentimeter(3));
    table.AddColumn(Unit.FromCentimeter(3));

    var header = table.AddRow();
    header.Shading.Color = Colors.LightGray;
    header.Cells[0].AddParagraph("Product");
    header.Cells[1].AddParagraph("Q1 Sales");
    header.Cells[2].AddParagraph("Q2 Sales");

    var row1 = table.AddRow();
    row1.Cells[0].AddParagraph("Widgets");
    row1.Cells[1].AddParagraph("$45,000");
    row1.Cells[2].AddParagraph("$52,000");

    // Footer with page numbers
    var footer = section.Footers.Primary;
    var fp = footer.AddParagraph();
    fp.AddText("Page ");
    fp.AddPageField();
    fp.Format.Alignment = ParagraphAlignment.Center;

    // Render
    var renderer = new PdfDocumentRenderer { Document = doc };
    renderer.RenderDocument();
    renderer.PdfDocument.Save("SalesReport.pdf");

Example 4: PDF with Processed Image
--------------------------------------
    using CodeBrix.Imaging;
    using CodeBrix.Imaging.Processing;
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;

    using var image = Image.Load("photo.jpg");
    image.Mutate(x => x.Grayscale().Resize(400, 300));

    var document = new PdfDocument();
    var page = document.AddPage();
    var gfx = XGraphics.FromPdfPage(page);

    gfx.DrawString("Processed Image:", new XFont("Arial", 14),
        XBrushes.Black, new XPoint(50, 30));

    var xImage = XImage.FromImageSource(image);
    gfx.DrawImage(xImage, 50, 50, 400, 300);

    document.Save("processed-image.pdf");

Example 5: In-Memory PDF for Web API
---------------------------------------
    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.Rendering;

    public byte[] GenerateInvoicePdf(InvoiceDto invoice)
    {
        var doc = new Document();
        var section = doc.AddSection();

        section.AddParagraph($"Invoice #{invoice.Number}");
        section.AddParagraph($"Date: {invoice.Date:yyyy-MM-dd}");
        section.AddParagraph($"Total: {invoice.Total:C}");

        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream);
        return stream.ToArray();
    }

Example 6: Rasterize PDF Pages to Image Files
------------------------------------------------
    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    rasterizer.OutputDirectory = @"C:\Output\Images";
    rasterizer.Dpi = 150;

    await rasterizer.RasterizeToImageFiles("report.pdf");
    // Creates Rasterized_Page_1.png, Rasterized_Page_2.png, etc.

Example 7: Rasterize a Single Page to JPEG
--------------------------------------------
    using CodeBrix.Imaging;
    using CodeBrix.Imaging.Formats.Jpeg;
    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    using var image = await rasterizer.RasterizeToImage(
        "report.pdf",
        pageNumber: 1,
        desiredImageFormat: JpegFormat.Instance);

    await image.SaveAsync("page1.jpg");
    Console.WriteLine($"Image: {image.Width}x{image.Height}");

Example 8: Generate Thumbnails for All Pages
----------------------------------------------
    using CodeBrix.Imaging;
    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    var maxDims = new ThumbnailMaxDimensions(150, 200);

    IList<Image> thumbnails = await rasterizer.RasterizeToThumbnails(
        "report.pdf",
        maxDimensions: maxDims);

    for (var i = 0; i < thumbnails.Count; i++)
    {
        await thumbnails[i].SaveAsync($"thumb_{i + 1}.png");
        Console.WriteLine($"Thumb {i + 1}: {thumbnails[i].Width}x{thumbnails[i].Height}");
        thumbnails[i].Dispose();
    }

Example 9: Rasterize In-Memory PdfDocument
--------------------------------------------
    using CodeBrix.Imaging;
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfRasterizer;

    // Create a PDF in memory
    var document = new PdfDocument();
    var page = document.AddPage();
    var gfx = XGraphics.FromPdfPage(page);
    gfx.DrawString("Hello!", new XFont("Arial", 24),
        XBrushes.Black, new XPoint(50, 50));

    // Rasterize directly from the PdfDocument
    using var rasterizer = new PageRasterizer();
    using var image = await rasterizer.RasterizeToImage(document, pageNumber: 1);
    await image.SaveAsync("rendered.png");

Example 10: Get Page Dimensions
---------------------------------
    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    int pageCount = await rasterizer.GetPageCount("report.pdf");

    for (var i = 1; i <= pageCount; i++)
    {
        var dims = await rasterizer.GetPageDimensions("report.pdf", pageNumber: i);
        Console.WriteLine($"Page {i}: {dims.WidthInInches:F1}\" x {dims.HeightInInches:F1}\"");
        Console.WriteLine($"  At 300 DPI: {dims.GetWidthInPixels(300)} x {dims.GetHeightInPixels(300)} px");
    }

Example 11: Rasterize PDF from Web API (In-Memory)
-----------------------------------------------------
    using CodeBrix.Imaging;
    using CodeBrix.Imaging.Formats.Jpeg;
    using CodeBrix.PdfRasterizer;

    public async Task<byte[]> GetPageThumbnail(byte[] pdfBytes, int pageNumber)
    {
        using var rasterizer = new PageRasterizer();
        using var thumbnail = await rasterizer.RasterizeToThumbnail(
            pdfBytes,
            pageNumber: pageNumber,
            maxDimensions: new ThumbnailMaxDimensions(200, 260),
            desiredImageFormat: JpegFormat.Instance);

        using var ms = new MemoryStream();
        await thumbnail.SaveAsync(ms, JpegFormat.Instance);
        return ms.ToArray();
    }

================================================================================

COMMON USING STATEMENT COMBINATIONS
=====================================

For basic PDF creation (low-level):

    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfDocuments.Drawing;

For reading/merging existing PDFs:

    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfDocuments.Pdf.IO;

For text layout:

    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Drawing.Layout;

For document model (high-level):

    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.Rendering;

For tables in document model:

    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.DocumentObjectModel.Tables;
    using CodeBrix.PdfDocCreate.Rendering;

For embedding processed images:

    using CodeBrix.Imaging;
    using CodeBrix.Imaging.Processing;
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;

For PDF rasterization (basic):

    using CodeBrix.PdfRasterizer;

For PDF rasterization with image format control:

    using CodeBrix.Imaging;
    using CodeBrix.Imaging.Formats.Jpeg;   // or .Png, .Bmp, .Gif, .Tiff
    using CodeBrix.PdfRasterizer;

For rasterizing PdfDocument objects:

    using CodeBrix.Imaging;
    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfRasterizer;

================================================================================

WHAT THESE LIBRARIES DO NOT DO
===============================

Do NOT attempt to use these libraries for the following:

  - Extracting text content from PDFs (no OCR, no text extraction API).
    NOTE: this is about READING PDFs. Text you WRITE with these libraries is
    embedded as real, searchable, selectable text - see pitfall #20.
  - Filling PDF forms programmatically (form fields are rendered visually
    by PdfRasterizer but cannot be edited via an API)
  - Digital signatures
  - PDF/A compliance validation
  - Editing existing PDF text content (can add new content to existing pages)
  - Converting arbitrary live WEBSITES to PDF (CodeBrix.PdfDocCreate.Html2Pdf
    renders author-created HTML/CSS - see PART 4 - but it is not a browser)
  - Reading/writing Word (.docx) or Excel (.xlsx) files
  - Creating PDF portfolios

CodeBrix.PdfDocuments IS for: creating new PDF documents, drawing
text/images/graphics on PDF pages, merging multiple PDFs, adding
security/encryption, creating structured documents with styles/tables/headers
using the document model, and adding bookmarks/outlines.

CodeBrix.PdfRasterizer IS for: converting PDF pages to images (PNG, JPEG,
BMP, GIF, TIFF), generating thumbnails, and querying page dimensions.

================================================================================

MINIMUM VIABLE PROJECT TEMPLATE
=================================

For low-level PDF creation:

    dotnet new console -n MyPdfApp --framework net10.0
    cd MyPdfApp
    dotnet add package CodeBrix.PdfDocuments.MitLicenseForever

Program.cs:

    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;

    var doc = new PdfDocument();
    var page = doc.AddPage();
    var gfx = XGraphics.FromPdfPage(page);
    gfx.DrawString("Hello, PDF!", new XFont("Arial", 24),
        XBrushes.Black, new XPoint(50, 50));
    doc.Save("output.pdf");
    Console.WriteLine("Created output.pdf!");

For high-level document model:

    dotnet new console -n MyPdfApp --framework net10.0
    cd MyPdfApp
    dotnet add package CodeBrix.PdfDocCreate.MitLicenseForever

Program.cs:

    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.Rendering;

    var doc = new Document();
    var section = doc.AddSection();
    section.AddParagraph("Hello, PDF!");

    var renderer = new PdfDocumentRenderer { Document = doc };
    renderer.RenderDocument();
    renderer.PdfDocument.Save("output.pdf");
    Console.WriteLine("Created output.pdf!");

For PDF rasterization:

    dotnet new console -n MyPdfApp --framework net10.0
    cd MyPdfApp
    dotnet add package CodeBrix.PdfRasterizer.MitLicenseForever

Program.cs:

    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    rasterizer.OutputDirectory = @"C:\Output";
    await rasterizer.RasterizeToImageFiles("document.pdf");
    Console.WriteLine("Rasterized all pages!");

Build and run:

    dotnet build
    dotnet run

================================================================================

PERFORMANCE TIPS FOR CODING AGENTS
====================================

1. USE PdfDocCreate FOR STRUCTURED DOCUMENTS: When creating documents with
   headings, paragraphs, and tables, use the high-level document model.
   It handles pagination, text flow, and layout automatically.

2. USE PdfDocuments FOR CUSTOM GRAPHICS: When you need precise control over
   positioning (e.g., labels at exact coordinates), use XGraphics directly.

3. CONSOLIDATE IMAGE DATA WHEN MERGING: After merging PDFs with images,
   call ConsolidateImageData() to deduplicate images and reduce file size
   by 75%+ when the same images appear repeatedly.

4. USE STREAMS FOR WEB SCENARIOS: Save to MemoryStream and return byte
   arrays instead of writing temp files to disk.

5. REUSE FONTS: Create XFont objects once and reuse them. Don't create
   new XFont instances for every DrawString call with the same font.

6. USE XTextFormatter FOR LONG TEXT: For multi-line text that needs
   automatic line wrapping, use XTextFormatter instead of manually
   calculating line breaks with DrawString.

7. PROCESS IMAGES WITH CodeBrix.Imaging FIRST: Resize images before
   embedding them in PDFs to reduce file size.

8. DEFINE STYLES EARLY: When using PdfDocCreate, define all styles before
   adding content. Styles are inherited, so changes to "Normal" affect
   all styles based on it.

9. USE LOWER DPI FOR THUMBNAILS: When generating thumbnails, the image
   will be downscaled anyway. Consider using a lower DPI (e.g., 150)
   to speed up rendering before the resize step.

10. CONFIGURE PROPERTIES ONCE: Set Dpi, OutputDirectory, Password,
    RasterizedImageFormat, and ThumbnailMaxDimensions on the PageRasterizer
    instance once, then call methods without repeating parameters.

11. USE SINGLE-PAGE METHODS WHEN APPROPRIATE: If you only need one page,
    use RasterizeToImage/RasterizeToThumbnail instead of the multi-page
    variants. This avoids loading and rendering unnecessary pages.

12. DISPOSE IMAGES PROMPTLY: Images returned by rasterization methods
    consume memory. Dispose them as soon as you're done processing.

================================================================================

COMMON PITFALLS TO AVOID
=========================

1. DO NOT confuse the NuGet package names with their namespaces.
   - Package: CodeBrix.PdfDocuments.MitLicenseForever
     Namespace: CodeBrix.PdfDocuments.*
   - Package: CodeBrix.PdfDocCreate.MitLicenseForever
     Namespace: CodeBrix.PdfDocCreate.*
   - Package: CodeBrix.PdfRasterizer.MitLicenseForever
     Namespace: CodeBrix.PdfRasterizer

2. DO NOT use PdfSharp or MigraDoc namespaces. Even though this is a fork,
   all namespaces are CodeBrix.PdfDocuments.* and CodeBrix.PdfDocCreate.*.

3. DO NOT forget that coordinates in XGraphics are in points (1 inch = 72
   points). Use XUnit.FromInch(), XUnit.FromCentimeter(), etc. for conversion.

4. DO NOT open a PDF for Import and then try to modify it. Use
   PdfDocumentOpenMode.Modify for editing, PdfDocumentOpenMode.Import for
   extracting pages to merge.

5. DO NOT target .NET versions below 10.0.

6. DO NOT forget to call RenderDocument() on PdfDocumentRenderer before
   saving. The document model must be rendered to generate the actual PDF.

7. DO NOT forget that PdfDocCreate builds on PdfDocuments. You can access
   the underlying PdfDocument via pdfRenderer.PdfDocument for additional
   low-level manipulation after rendering.

8. DO NOT assume system fonts are available in Docker/CI environments, and
   DO NOT assume a font you asked for is the font you got. No IFontResolver
   registration is needed, but an unavailable family is silently substituted
   rather than raising an error - monospace families are the common casualty
   on Linux. See FONTS AND FONT RESOLUTION in PART 1 for the details and for
   the cross-platform-safe family names.

9. DO NOT forget to dispose PageRasterizer instances. Use 'using var' or
   'using (...) { }' patterns. After disposal, all methods throw
   ObjectDisposedException.

10. DO NOT forget to dispose Image objects returned by RasterizeToImages,
    RasterizeToImage, RasterizeToThumbnails, and RasterizeToThumbnail.
    The caller owns the returned images.

11. DO NOT set RasterizedImageFormat to null or UnknownImageFormat.
    Null throws ArgumentNullException; UnknownImageFormat throws
    ArgumentException.

12. DO NOT forget that page numbers are 1-based in PageRasterizer.
    Passing 0 or a number exceeding the page count throws ArgumentException.

13. DO NOT expect thread parallelism with PageRasterizer. PDFium is NOT
    thread-safe; all native calls are serialized via SemaphoreSlim.
    Multiple concurrent calls will work but execute sequentially.

14. DO NOT confuse the NuGet package name (CodeBrix.PdfRasterizer.
    MitLicenseForever) with the namespace (CodeBrix.PdfRasterizer).

15. DO NOT use CodeBrix.PdfRasterizer on iOS or WebAssembly.
    PDFium native binaries are bundled for Windows, macOS, Linux, and
    Android only. The library will fail at runtime on any platform
    without a bundled PDFium binary.

16. DO NOT try to install PDFium separately for CodeBrix.PdfRasterizer.
    The native binaries are bundled inside the NuGet package and are
    automatically copied to the output directory at build time.

17. DO NOT use LineSpacingRule.AtLeast on a PdfDocCreate paragraph whose
    text is much smaller than its LineSpacing value. This is a subtle layout
    gotcha. A common case: a tiny image-credit or caption line meant to sit
    snug beneath an image.
      With AtLeast, the renderer reserves a full line box of at least the
    specified LineSpacing height, then places the small text at the BOTTOM
    of that box. The reserved-but-empty space appears as a large gap ABOVE
    the text (roughly LineSpacing minus the glyph height), and it also pushes
    the NEXT paragraph (e.g. the caption below) further down. The result
    looks like mysterious leading that no SpaceBefore / SpaceAfter value
    seems to account for.
      FIX: use LineSpacingRule.Exactly with a LineSpacing close to the font
    size (e.g. fontSize * 1.1). The line box then fits the glyphs, so the
    paragraph hugs whatever is directly above it. Reserve AtLeast for body
    text where you genuinely WANT a guaranteed minimum leading regardless of
    the content on the line.

18. DO NOT write a six-digit "0x" color string and expect it to be opaque.
    Color.Parse("0xc0c0c0") is FULLY TRANSPARENT, not light grey.
      "0x" introduces a packed 0xAARRGGBB integer, so the alpha byte comes
    FIRST. Supply only six digits and alpha stays at zero. Nothing throws:
    shading, borders and text simply do not appear, which reads as a layout
    bug rather than a color bug and can cost a long time to find.
      FIX: write the alpha explicitly - "0xFFC0C0C0" - or use the CSS form
    "#c0c0c0", or use Color.FromRgb(192, 192, 192). All three are opaque.
    Note that new Color(192, 192, 192) is opaque too; only the six-digit "0x"
    STRING form is affected. See CONSTRUCTING CUSTOM COLORS in PART 2.

19. DO NOT call doc.AddStyle(...) with a built-in style name. Heading1 through
    Heading9, Normal, DefaultParagraphFont, Footnote, Header, Footer and
    Hyperlink all already exist on a new Document.
      AddStyle does not throw on a duplicate name - it REPLACES the existing
    style with a fresh one, and hands you back an object that is NOT the one
    stored in the document. Every property you then set is written to an
    orphan and has no effect, so the document silently renders unstyled.
    Worse, the replacement discards the built-in style's ParagraphFormat.
    OutlineLevel (which drives PDF outline/bookmark generation) and re-bases
    it, breaking the Heading1 -> Heading2 -> Heading3 inheritance chain.
      FIX: fetch the style instead - var h1 = doc.Styles["Heading1"]; - and
    set properties on that. Reserve AddStyle for names of your own.
    See BUILT-IN STYLE NAMES in PART 2.

20. DO NOT assume generated PDFs are not text-searchable. These libraries have
    no text EXTRACTION api (see WHAT THESE LIBRARIES DO NOT DO), but that is a
    statement about reading PDFs, not about writing them. Text drawn with
    DrawString or through the PdfDocCreate document model is embedded as real
    text with correct word spacing, so the output is searchable, selectable and
    accessible, and can be diffed as text in a regression suite.

================================================================================

DEEPER LEARNING: TEST FILE CROSS-REFERENCES
=============================================

The CodeBrix.PdfDocuments.Tests project contains working examples:

    https://github.com/ellisnet/CodeBrix.PdfDocuments
    Path: tests/CodeBrix.PdfDocuments.Tests/

Feature-to-test-file mapping:

  Creating simple PDFs (text, metadata, images, CodeBrix.Imaging integration):
    -> tests/CodeBrix.PdfDocuments.Tests/CreateSimplePDF.cs

  Merging PDFs and image data consolidation:
    -> tests/CodeBrix.PdfDocuments.Tests/Merge.cs

  Creating documents with PdfDocCreate (styles, paragraphs, images,
  web content integration):
    -> tests/CodeBrix.PdfDocuments.Tests/DocumentObjectModel/CreatePdfTests.cs

  Tables (borders, cell merging, vertical alignment, shading):
    -> tests/CodeBrix.PdfDocuments.Tests/Rendering/TestTable.cs

  Rendering tests (paragraphs, text formatting, alignment, tabs, borders):
    -> tests/CodeBrix.PdfDocuments.Tests/Rendering/RenderingTests.cs
    -> tests/CodeBrix.PdfDocuments.Tests/Rendering/TestLayout.cs
    -> tests/CodeBrix.PdfDocuments.Tests/Rendering/TestParagraphRenderer.cs
    -> tests/CodeBrix.PdfDocuments.Tests/Rendering/TestParagraphIterator.cs

  Text layout (XTextFormatter):
    -> tests/CodeBrix.PdfDocuments.Tests/Drawing/Layout/XTextFormatterTest.cs

  PDF security and encryption (passwords, AES, multiple encryption levels):
    -> tests/CodeBrix.PdfDocuments.Tests/Security/PdfSecurity.cs

  PDF reading/writing and I/O:
    -> tests/CodeBrix.PdfDocuments.Tests/IO/PdfReader.cs
    -> tests/CodeBrix.PdfDocuments.Tests/IO/LargePDFReadWrite.cs
    -> tests/CodeBrix.PdfDocuments.Tests/IO/IoBaseTest.cs

  Document outlines (bookmarks):
    -> tests/CodeBrix.PdfDocuments.Tests/Outlines/OutlineTests.cs

  PdfInteger and low-level PDF objects:
    -> tests/CodeBrix.PdfDocuments.Tests/PdfInteger.cs
    -> tests/CodeBrix.PdfDocuments.Tests/PdfReader.cs

  PDF rasterization (PageRasterizer - all methods, properties, thumbnails,
  page info, cancellation, multiple image formats, error handling):
    -> tests/CodeBrix.PdfDocuments.Tests/PdfRasterizer/PdfRasterizerTests.cs

HOW TO USE: Fetch the raw file content from GitHub using a URL like:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/{path}
For example:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/tests/CodeBrix.PdfDocuments.Tests/Rendering/TestTable.cs

================================================================================

QUICK REFERENCE CARD
=====================

--- Install ---
Low-level:    dotnet add package CodeBrix.PdfDocuments.MitLicenseForever
High-level:   dotnet add package CodeBrix.PdfDocCreate.MitLicenseForever
Rasterizer:   dotnet add package CodeBrix.PdfRasterizer.MitLicenseForever
HTML to PDF:  dotnet add package CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever
MD to PDF:    dotnet add package CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever

--- Html2Pdf (HTML+CSS to PDF) ---
Create:         var r = new HtmlPdfRenderer()
Page size:      r.Options.SetPageSize("a4"); r.Options.Landscape = true
Furniture:      r.Options.FooterText = "Page {page} of {pages}"
Render file:    var res = r.RenderFile("in.html", "out.pdf")
Render string:  r.RenderHtml(html, "out.pdf", baseDir) / r.RenderHtmlToBytes(html)
Inspect:        res.PageCount, res.Warnings.Messages, res.Title
Fonts:          automatic (package fonts); Html2PdfFonts.AddFontDirectory(dir),
                .AddFontFile(path) / .AddFontFiles / .AddFontFilesFromDirectory
                (loose .ttf/.otf, no manifest), .AddFallbackFamily(name)
SVG:            <img src="x.svg">, data:image/svg+xml, inline <svg> all render;
                r.Options.SvgRasterScale sets sharpness
Tofu opt-in:    r.Options.KeepUncoveredCharacters = true
CSS page rule:  @page { size: a4 landscape; margin: 2cm; }

--- Markdown2Pdf (Markdown to PDF) ---
Create:         var r = new MarkdownPdfRenderer()
Zero-config:    var res = r.RenderFile("doc.md")     // doc.pdf next to it
To bytes:       r.RenderMarkdownToBytes(markdownText)
Options:        r.Options.PageSize / .AllowRemoteImages / .FooterText
Restyle (b):    var g = r.GenerateHtmlFromFile("doc.md")
                new HtmlPdfRenderer().RenderHtml(g.ToHtmlDocument(myCss),
                    "out.pdf", g.BaseDirectory)
Parser only:    new MarkdownParser().Render("# md")  // markdown-it port

--- PdfDocuments (low-level) ---
Create doc:     new PdfDocument()
Add page:       document.AddPage()
Get graphics:   XGraphics.FromPdfPage(page)
Draw text:      gfx.DrawString(text, font, brush, point)
Draw image:     gfx.DrawImage(XImage.FromFile(path), point)
Draw line:      gfx.DrawLine(pen, point1, point2)
Draw rect:      gfx.DrawRectangle(pen, brush, x, y, w, h)
Text layout:    new XTextFormatter(gfx).DrawString(...)
Font:           new XFont("Arial", 12, XFontStyle.Bold)
Open PDF:       PdfReader.Open(stream, PdfDocumentOpenMode.Import)
Merge:          outputDoc.AddPage(inputDoc.Pages[i])
Consolidate:    document.ConsolidateImageData()
Save:           document.Save("file.pdf")
Metadata:       document.Info.Title = "..."
Security:       document.SecuritySettings.UserPassword = "..."

--- PdfDocCreate (high-level) ---
Create doc:     new Document()
Add style:      doc.AddStyle("MyName", "Normal")   // custom names ONLY
Built-in style: doc.Styles["Heading1"]             // NEVER AddStyle these
Add section:    doc.AddSection()
Add paragraph:  section.AddParagraph("text")
Line break:     para.AddLineBreak()
Bullet list:    para.Format.ListInfo.ListType = ListType.BulletList1
                para.Format.ListInfo.ContinuePreviousList = true
Hyperlink:      para.AddHyperlink("https://x.com", HyperlinkType.Web).AddText("x")
Bookmark:       para.AddBookmark("ch1")
Link to it:     para.AddHyperlink("ch1", HyperlinkType.Bookmark).AddText("go")
Tab stop:       para.Format.AddTabStop(Unit.FromCentimeter(8),
                    TabAlignment.Right, TabLeader.Dots); para.AddTab()
Para shading:   para.Format.Shading.Color = Colors.LightYellow
Custom color:   Color.FromRgb(255, 253, 231)  /  Color.Parse("#fffde7")
                (NOT Color.Parse("0xfffde7") - that is transparent)
Add table:      section.AddTable()
Add column:     table.AddColumn(width)
Add row:        table.AddRow()
Set cell:       row.Cells[0].AddParagraph("text")
Cell merge:     cell.MergeRight = 1; cell.MergeDown = 1
Shading:        cell.Shading.Color = Colors.LightBlue
Header:         section.Headers.Primary.AddParagraph("text")
Footer:         section.Footers.Primary.AddParagraph()
Page number:    para.AddPageField()
Page count:     para.AddNumPagesField()
Render:         new PdfDocumentRenderer { Document = doc }
                renderer.RenderDocument()
Save:           renderer.PdfDocument.Save("file.pdf")

--- PdfRasterizer (PDF-to-image) ---
Create:         using var r = new PageRasterizer()
Set DPI:        r.Dpi = 300
Set output:     r.OutputDirectory = @"C:\Output"
Set format:     r.RasterizedImageFormat = JpegFormat.Instance
Set password:   r.Password = "secret"
To files:       await r.RasterizeToImageFiles("doc.pdf")
To images:      IList<Image> imgs = await r.RasterizeToImages("doc.pdf")
Single image:   using var img = await r.RasterizeToImage("doc.pdf", 1)
Thumbnails:     IList<Image> t = await r.RasterizeToThumbnails("doc.pdf")
Single thumb:   using var t = await r.RasterizeToThumbnail("doc.pdf", 1)
Thumb dims:     new ThumbnailMaxDimensions(200, 260)
Page count:     int n = await r.GetPageCount("doc.pdf")
Page dims:      PdfPageDimensions d = await r.GetPageDimensions("doc.pdf", 1)
Dims props:     d.WidthInPoints, d.HeightInInches, d.GetWidthInPixels(300)
Dispose:        r.Dispose()  // or use 'using' pattern

Input types: string path, byte[], Stream, PdfDocument
Formats:     PngFormat, JpegFormat, BmpFormat, GifFormat, TiffFormat

Target: .NET 10.0+

================================================================================
