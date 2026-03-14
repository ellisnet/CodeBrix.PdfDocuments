================================================================================
AGENT-README: CodeBrix.PdfDocuments / CodeBrix.PdfDocCreate / CodeBrix.PdfRasterizer
A Comprehensive Guide for AI Coding Agents
================================================================================

OVERVIEW
--------
CodeBrix.PdfDocuments, CodeBrix.PdfDocCreate, and CodeBrix.PdfRasterizer are
THREE companion .NET libraries for working with PDF documents:

  1. CodeBrix.PdfDocuments - Low-level PDF library for creating, reading, merging,
     and manipulating PDF documents using direct graphics drawing (XGraphics).

  2. CodeBrix.PdfDocCreate - High-level document object model for building
     richly formatted PDF documents with styled text, tables, charts, and images.
     This is built on top of CodeBrix.PdfDocuments.

  3. CodeBrix.PdfRasterizer - PDF page rasterizer that renders PDF pages to images
     (PNG, JPEG, BMP, GIF, TIFF) using the PDFium native rendering engine, with
     support for thumbnails, page information, and cross-platform operation.
     This depends on CodeBrix.PdfDocuments and CodeBrix.Imaging.

CodeBrix.PdfDocuments and CodeBrix.PdfDocCreate are forks of the popular
PdfSharpCore (v1.3.67) and MigraDocCore (v1.3.67) libraries.
CodeBrix.PdfRasterizer contains rendering logic derived from Docnet.Core and
bundles pre-built PDFium native binaries.
All three libraries are licensed under the MIT License.

IMPORTANT: If you are familiar with PdfSharp/PdfSharpCore, the API surface of
CodeBrix.PdfDocuments is very similar. If you are familiar with MigraDoc/
MigraDocCore, the API surface of CodeBrix.PdfDocCreate is very similar.
However, ALL namespaces use "CodeBrix.PdfDocuments", "CodeBrix.PdfDocCreate",
and "CodeBrix.PdfRasterizer" instead of "PdfSharp"/"MigraDoc". Do NOT mix
the libraries.

Source Repository: https://github.com/ellisnet/CodeBrix.PdfDocuments
License: MIT License

================================================================================

INSTALLATION
------------
There are THREE NuGet packages. Install one or more depending on your needs:

--- Package 1: CodeBrix.PdfDocuments (low-level) ---

NuGet Package: CodeBrix.PdfDocuments.MitLicenseForever
Latest Version: 1.0.49 (as of Feb 2026)
Package Size: ~271 KB
Dependencies:
  - CodeBrix.Compression.MitLicenseForever (>= 1.0.48)
  - CodeBrix.Imaging.ApacheLicenseForever (>= 1.0.48)

    dotnet add package CodeBrix.PdfDocuments.MitLicenseForever

--- Package 2: CodeBrix.PdfDocCreate (high-level, includes PdfDocuments) ---

NuGet Package: CodeBrix.PdfDocCreate.MitLicenseForever
Latest Version: 1.0.49 (as of Feb 2026)
Package Size: ~156 KB
Dependencies:
  - CodeBrix.PdfDocuments.MitLicenseForever (>= 1.0.49)

    dotnet add package CodeBrix.PdfDocCreate.MitLicenseForever

NOTE: Installing CodeBrix.PdfDocCreate automatically pulls in
CodeBrix.PdfDocuments, CodeBrix.Imaging, and CodeBrix.Compression.

--- Package 3: CodeBrix.PdfRasterizer (PDF-to-image rendering) ---

NuGet Package: CodeBrix.PdfRasterizer.MitLicenseForever
Dependencies:
  - CodeBrix.PdfDocuments.MitLicenseForever
  - CodeBrix.Imaging.ApacheLicenseForever (>= 1.0.73)
Bundled Native Libraries: PDFium binaries for 7 platform RIDs
  (win-x64, win-x86, osx-x64, osx-arm64, linux-x64, linux-arm64, linux-arm)

    dotnet add package CodeBrix.PdfRasterizer.MitLicenseForever

NOTE: Installing CodeBrix.PdfRasterizer automatically pulls in
CodeBrix.PdfDocuments, CodeBrix.Imaging, and the PDFium native binaries
for your platform. There is NO separate PDFium installation required.
The pre-built native library (pdfium.dll / pdfium.dylib / pdfium.so) is
bundled inside the NuGet package and copied to the output directory at
build time.

Requirements: .NET 10.0 or higher

SUPPORTED PLATFORMS (CodeBrix.PdfRasterizer only):
  The NuGet package includes pre-built PDFium native binaries for:
    - Windows x64      (win-x64)
    - Windows x86      (win-x86)
    - Windows ARM64    (win-arm64 — uses win-x64 via emulation)
    - macOS x64        (osx-x64)
    - macOS ARM64      (osx-arm64, Apple Silicon)
    - Linux x64        (linux-x64)
    - Linux ARM64      (linux-arm64)
    - Linux ARM        (linux-arm)

  UNSUPPORTED PLATFORMS: CodeBrix.PdfRasterizer does NOT work on iOS,
  Android, WebAssembly (Blazor WASM), or any other platform not listed
  above. There are no PDFium native binaries for those targets. Attempting
  to construct a PageRasterizer on an unsupported platform will fail at
  runtime when the native library cannot be loaded.

  CodeBrix.PdfDocuments and CodeBrix.PdfDocCreate are pure managed .NET
  and have no platform restrictions beyond .NET 10.0+.

Or in a .csproj file:

    <!-- For low-level PDF drawing only -->
    <PackageReference Include="CodeBrix.PdfDocuments.MitLicenseForever" Version="1.0.49" />

    <!-- For high-level document model (includes PdfDocuments) -->
    <PackageReference Include="CodeBrix.PdfDocCreate.MitLicenseForever" Version="1.0.49" />

    <!-- For PDF-to-image rasterization -->
    <PackageReference Include="CodeBrix.PdfRasterizer.MitLicenseForever" Version="1.0.49" />

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

Supported image formats for embedding:
    PNG, JPEG, BMP, WebP, GIF

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

    // Create heading styles
    var h1 = doc.AddStyle("Heading1", "Normal");
    h1.Font.Size = 18;
    h1.Font.Bold = true;
    h1.ParagraphFormat.SpaceBefore = 12;
    h1.ParagraphFormat.SpaceAfter = 6;

    var h2 = doc.AddStyle("Heading2", "Normal");
    h2.Font.Size = 14;
    h2.Font.Bold = true;
    h2.ParagraphFormat.SpaceBefore = 8;
    h2.ParagraphFormat.SpaceAfter = 4;

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
PDFium binaries are included for Windows (x64, x86), macOS (x64, ARM64),
and Linux (x64, ARM64, ARM). No other platforms are supported — the library
will fail at runtime on iOS, Android, WebAssembly, or any unlisted target.

The main class is PageRasterizer (sealed, IDisposable). Create an instance,
configure properties, call rasterize methods, then dispose.

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

  - Extracting text content from PDFs (no OCR, no text extraction API)
  - Filling PDF forms programmatically (form fields are rendered visually
    by PdfRasterizer but cannot be edited via an API)
  - Digital signatures
  - PDF/A compliance validation
  - Editing existing PDF text content (can add new content to existing pages)
  - Converting HTML to PDF directly
  - Reading/writing Word (.docx) files (use FreePPlus for Excel)
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

8. DO NOT assume system fonts are available in Docker/CI environments.
   Consider embedding fonts if needed.

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

15. DO NOT use CodeBrix.PdfRasterizer on iOS, Android, or WebAssembly.
    PDFium native binaries are only bundled for desktop/server platforms
    (Windows, macOS, Linux). The library will fail at runtime on any
    platform without a bundled PDFium binary.

16. DO NOT try to install PDFium separately for CodeBrix.PdfRasterizer.
    The native binaries are bundled inside the NuGet package and are
    automatically copied to the output directory at build time.

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
Add style:      doc.AddStyle("Name", "Normal")
Add section:    doc.AddSection()
Add paragraph:  section.AddParagraph("text")
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
