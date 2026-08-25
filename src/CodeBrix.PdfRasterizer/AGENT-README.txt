================================================================================
AGENT-README: CodeBrix.PdfRasterizer
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.PdfRasterizer.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.PdfRasterizer renders PDF pages to raster images (PNG, JPEG, BMP, GIF,
TIFF) using the PDFium native rendering engine. It also generates thumbnails
(aspect-preserving downscales into a bounding box) and reports page counts and
page dimensions. Target: .NET 10 or later.

It is a standalone library that depends on CodeBrix.PdfDocuments (so a
PdfDocument object can be rasterized directly, without saving it first) and on
CodeBrix.Imaging (the returned Image type and the output formats).

The rendering logic is derived from Docnet.Core; the package bundles pre-built
PDFium native binaries (BSD-licensed) for every supported platform, so there is
NOTHING to install beyond the NuGet package. The namespace root is
CodeBrix.PdfRasterizer - the ".MitLicenseForever" suffix belongs to the
PACKAGE ID only and never appears in a namespace, using directive or type name.

The whole public surface is four types: PageRasterizer (sealed, IDisposable),
the records PdfPageDimensions and ThumbnailMaxDimensions, and the [Flags] enum
PdfRenderFlags. Create a PageRasterizer, configure its properties, call the
rasterize methods, then dispose it.

TIP - VISUAL REGRESSION TESTING: pairing PdfRasterizer with PdfDocuments or
PdfDocCreate gives you an automated way to prove that a document still looks
right. Generate the PDF, rasterize it to images, and compare those images
against approved baselines byte-for-byte or with a perceptual diff. Layout
regressions that no unit test would catch - a shifted table, a font that
silently substituted, a paragraph that now spills onto a second page - show up
immediately. Use a fixed Dpi so the baselines stay stable.

See also (sibling packages in the same repository):
  - AGENT-README.txt (repository root) - CodeBrix.PdfDocuments, the low-level
    PDF library whose PdfDocument this package accepts as input
  - src/CodeBrix.PdfDocCreate/AGENT-README.txt - the high-level document model
  - src/CodeBrix.PdfDocCreate.Html2Pdf/AGENT-README.txt - HTML+CSS to PDF
  - src/CodeBrix.PdfDocCreate.Markdown2Pdf/AGENT-README.txt - Markdown to PDF

Source Repository: https://github.com/ellisnet/CodeBrix.PdfDocuments

================================================================================

INSTALLATION
============
NuGet Package: CodeBrix.PdfRasterizer.MitLicenseForever

    dotnet add package CodeBrix.PdfRasterizer.MitLicenseForever

Or in a .csproj file (NuGet resolves the latest version):

    <PackageReference Include="CodeBrix.PdfRasterizer.MitLicenseForever" />

NuGet dependencies (pulled in automatically):
  - CodeBrix.PdfDocuments.MitLicenseForever
  - CodeBrix.Imaging.ApacheLicenseForever
  (CodeBrix.PdfDocuments in turn brings CodeBrix.Compression.MitLicenseForever.)

License: MIT (the bundled PDFium binaries are BSD-licensed; their LICENSE file
ships beside each native binary under runtimes/<rid>/native/ in the package).

Requirements: .NET 10 or later.

BUNDLED NATIVE LIBRARIES: the PDFium native binary for your platform is inside
the NuGet package and is copied to the build output at build time. You do NOT
install PDFium separately, and there is no separate PDFium package to
reference.

SUPPORTED PLATFORMS - PDFium binaries are bundled for exactly these RIDs:
    - win-x64          Windows x64          (pdfium.dll)
    - win-x86          Windows x86          (pdfium.dll)
    - win-arm64        Windows ARM64        (pdfium.dll)
    - osx-x64          macOS x64            (pdfium.dylib)
    - osx-arm64        macOS Apple Silicon  (pdfium.dylib)
    - linux-x64        Linux x64            (pdfium.so)
    - linux-arm64      Linux ARM64          (pdfium.so)
    - linux-arm        Linux ARM (32-bit)   (pdfium.so)
    - linux-riscv64    Linux RISC-V 64      (pdfium.so)
    - android-arm64    Android ARM64        (pdfium.so)

UNSUPPORTED PLATFORMS: iOS, WebAssembly (Blazor WASM), and any other platform
not listed above. There are no PDFium binaries for those targets, and
constructing a PageRasterizer fails at runtime when the native library cannot
be loaded.

HOW THE NATIVE LIBRARY IS FOUND: the first PageRasterizer constructed in a
process installs a resolver that probes
    <directory of the CodeBrix.PdfRasterizer assembly>/runtimes/<rid>/native/
for the platform's PDFium file (win-arm64 additionally falls back to the
win-x64 binary; linux-x64 additionally falls back to runtimes/linux/native/).
If that probe fails, the .NET runtime's ordinary native-library probing
applies. Keep the runtimes/ folder that the build produces next to the
application's assemblies. PDFium is initialized once per process and stays
loaded for the lifetime of the application.

================================================================================

KEY NAMESPACES / USINGS
=======================

    using CodeBrix.PdfRasterizer;          // PageRasterizer, PdfPageDimensions,
                                           // ThumbnailMaxDimensions, PdfRenderFlags
    using CodeBrix.Imaging;                // Image (return type of rasterization
                                           // methods) and its Save/SaveAs* methods
    using CodeBrix.Imaging.Formats.Png;    // PngFormat.Instance  (default)
    using CodeBrix.Imaging.Formats.Jpeg;   // JpegFormat.Instance
    using CodeBrix.Imaging.Formats.Bmp;    // BmpFormat.Instance
    using CodeBrix.Imaging.Formats.Gif;    // GifFormat.Instance
    using CodeBrix.Imaging.Formats.Tiff;   // TiffFormat.Instance
    using CodeBrix.PdfDocuments.Pdf;       // PdfDocument (optional input type)

PACKAGE-TO-NAMESPACE MAP:

  NuGet package                                Namespace root
  -------------------------------------------  ----------------------
  CodeBrix.PdfRasterizer.MitLicenseForever     CodeBrix.PdfRasterizer

================================================================================

CORE API REFERENCE
==================

--- CONSTRUCTOR ---

    public PageRasterizer(Action<string> logger = null, string password = null)

    using CodeBrix.PdfRasterizer;

    // Basic construction
    using var rasterizer = new PageRasterizer();

    // With optional logger and password
    using var rasterizer = new PageRasterizer(
        logger: msg => Console.WriteLine(msg),
        password: "secretPassword");

The constructor is where PDFium is loaded and initialized (once per process).

--- PROPERTIES ---

    rasterizer.Dpi = 300;                          // int
        // Default rendering DPI (default: 300). Values < 1 reset to 300.

    rasterizer.Password = "secret";                // string
        // Default password for encrypted PDFs. null = no password.
        // Empty strings are stored as null.

    rasterizer.OutputDirectory = @"C:\Output";     // string
        // Default output directory for file-writing methods. Required by
        // RasterizeToImageFiles / RasterizeToImageFile /
        // RasterizeToThumbnailFiles / RasterizeToThumbnailFile when the
        // method's outputDirectory argument is not passed; if neither is
        // set, ArgumentException. Whitespace is stored as null. The
        // directory is created if it does not exist.

    rasterizer.RasterizedImageFormat = JpegFormat.Instance;   // IImageFormat
        // Default image format (default: PngFormat.Instance). The format's
        // DefaultFileExtension decides the file extension.
        // Cannot be set to UnknownImageFormat (throws ArgumentException).
        // Cannot be null (throws ArgumentNullException).

    rasterizer.FileNameGenerator = pageNum => $"Page_{pageNum}";  // Func<int,string>
        // Generates file name stems (without extension) from the 1-based
        // page number. Default: pageNumber => $"Rasterized_Page_{pageNumber}"
        // Setting to null resets to the default.

    rasterizer.AllowOverwriteFiles = true;         // bool
        // Whether to overwrite existing files (default: false).
        // When false, IOException is thrown if a target file exists.

    rasterizer.BackgroundColor = 0xFFFFFFFF;       // uint, ARGB
        // Packed ARGB value (alpha in the HIGH byte: 0xAARRGGBB). Default is
        // opaque white 0xFFFFFFFF. It is the base layer filled behind all
        // page content. The bitmap only carries an alpha channel when PDFium
        // reports that the page itself uses transparency; on such pages a
        // value with alpha 0x00 (e.g. 0x00000000) leaves the background
        // transparent - which only alpha-capable output formats (PNG, GIF,
        // TIFF) can keep. JPEG and BMP have no alpha channel, so keep
        // BackgroundColor opaque (alpha 0xFF) when rendering to them.

    rasterizer.ThumbnailMaxDimensions = new ThumbnailMaxDimensions(150, 200);
        // Default bounding box for the thumbnail methods. Setting to null
        // reverts to the built-in default (200 x 260 pixels). Values below
        // 1 in either member throw ArgumentException.

--- THE TWO RECORDS ---

    public record ThumbnailMaxDimensions(int MaxHorizontalPixels, int MaxVerticalPixels);
        // MaxHorizontalPixels = maximum width in pixels  (must be >= 1)
        // MaxVerticalPixels   = maximum height in pixels (must be >= 1)
        var box = new ThumbnailMaxDimensions(MaxHorizontalPixels: 150, MaxVerticalPixels: 200);
        int w = box.MaxHorizontalPixels;
        int h = box.MaxVerticalPixels;

    public record PdfPageDimensions(double WidthInPoints, double HeightInPoints)
        // Computed members:
        //   double WidthInInches            => WidthInPoints / 72.0
        //   double HeightInInches           => HeightInPoints / 72.0
        //   int    GetWidthInPixels(int dpi)  => (int)(WidthInPoints * dpi / 72.0)
        //   int    GetHeightInPixels(int dpi) => (int)(HeightInPoints * dpi / 72.0)

--- RASTERIZE ALL PAGES TO IMAGE FILES ---

Full signature (string-path overload; the byte[] and Stream overloads take the
same parameters; the PdfDocument overload has NO password parameter):

    public async Task RasterizeToImageFiles(
        string pdfPath,
        string outputDirectory = null,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations
                                   | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)

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

    // With all options, including render flags
    await rasterizer.RasterizeToImageFiles(
        "document.pdf",
        outputDirectory: @"C:\Output",
        dpi: 150,
        password: "secret",
        desiredImageFormat: JpegFormat.Instance,
        pageNumbers: new[] { 1, 3, 5 },  // specific pages (1-based)
        renderFlags: PdfRenderFlags.RenderAnnotations | PdfRenderFlags.Grayscale,
        cancellationToken: ct);

Each page is written as <outputDirectory>/<FileNameGenerator(pageNumber)><ext>
where <ext> is the format's DefaultFileExtension (".png" by default).

--- RASTERIZE SINGLE PAGE TO IMAGE FILE ---

    public async Task RasterizeToImageFile(
        string pdfPath,              // or byte[] pdfBytes / Stream pdfStream / PdfDocument pdfDocument
        int pageNumber,              // 1-based
        string outputDirectory = null,
        int? dpi = null,
        string password = null,      // absent on the PdfDocument overload
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations
                                   | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)

    await rasterizer.RasterizeToImageFile(
        "document.pdf",
        pageNumber: 1,
        outputDirectory: @"C:\Output");

--- RASTERIZE ALL PAGES TO IN-MEMORY IMAGES ---

    public async Task<IList<Image>> RasterizeToImages(
        string pdfPath,              // or byte[] / Stream / PdfDocument
        int? dpi = null,
        string password = null,      // absent on the PdfDocument overload
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations
                                   | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)

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

    public async Task<Image> RasterizeToImage(
        string pdfPath,              // or byte[] / Stream / PdfDocument
        int pageNumber,              // 1-based
        int? dpi = null,
        string password = null,      // absent on the PdfDocument overload
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations
                                   | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)

    // Returns Image - caller must dispose
    using var image = await rasterizer.RasterizeToImage(
        "document.pdf",
        pageNumber: 1);

    // Save to file (CodeBrix.Imaging infers the format from the extension)
    image.Save("page1.png");

    // Access dimensions
    Console.WriteLine($"Size: {image.Width} x {image.Height}");

--- GENERATE THUMBNAILS ---

Thumbnails are rasterized images that have been resized to fit within a
bounding box while preserving the page's aspect ratio. Only downscaled;
images already fitting within the box are left as-is.

    // Built-in default: 200 x 260 pixels
    // Custom: new ThumbnailMaxDimensions(MaxHorizontalPixels, MaxVerticalPixels)

    public async Task<IList<Image>> RasterizeToThumbnails(
        string pdfPath,              // or byte[] / Stream / PdfDocument
        ThumbnailMaxDimensions maxDimensions = null,
        int? dpi = null,
        string password = null,      // absent on the PdfDocument overload
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations
                                   | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)

    public async Task<Image> RasterizeToThumbnail(
        string pdfPath,              // or byte[] / Stream / PdfDocument
        int pageNumber,
        ThumbnailMaxDimensions maxDimensions = null,
        int? dpi = null,
        string password = null,      // absent on the PdfDocument overload
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations
                                   | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)

    public async Task RasterizeToThumbnailFiles(
        string pdfPath,              // or byte[] / Stream / PdfDocument
        ThumbnailMaxDimensions maxDimensions = null,
        string outputDirectory = null,
        int? dpi = null,
        string password = null,      // absent on the PdfDocument overload
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations
                                   | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)

    public async Task RasterizeToThumbnailFile(
        string pdfPath,              // or byte[] / Stream / PdfDocument
        int pageNumber,
        ThumbnailMaxDimensions maxDimensions = null,
        string outputDirectory = null,
        int? dpi = null,
        string password = null,      // absent on the PdfDocument overload
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations
                                   | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)

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

    public async Task<int> GetPageCount(string pdfPath, string password = null,
        CancellationToken cancellationToken = default)
    public async Task<int> GetPageCount(byte[] pdfBytes, string password = null,
        CancellationToken cancellationToken = default)
    public async Task<int> GetPageCount(Stream pdfStream, string password = null,
        CancellationToken cancellationToken = default)
    public async Task<int> GetPageCount(PdfDocument pdfDocument,
        CancellationToken cancellationToken = default)

    public async Task<PdfPageDimensions> GetPageDimensions(string pdfPath, int pageNumber,
        string password = null, CancellationToken cancellationToken = default)
    public async Task<PdfPageDimensions> GetPageDimensions(byte[] pdfBytes, int pageNumber,
        string password = null, CancellationToken cancellationToken = default)
    public async Task<PdfPageDimensions> GetPageDimensions(Stream pdfStream, int pageNumber,
        string password = null, CancellationToken cancellationToken = default)
    public async Task<PdfPageDimensions> GetPageDimensions(PdfDocument pdfDocument, int pageNumber,
        CancellationToken cancellationToken = default)

    // Get page count
    int count = await rasterizer.GetPageCount("document.pdf");
    int count = await rasterizer.GetPageCount(pdfBytes);
    int count = await rasterizer.GetPageCount(stream);
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

Every rasterization and page-information method has overloads accepting:

  1. string pdfPath          - File path to a PDF
  2. byte[] pdfBytes         - PDF content as byte array
  3. Stream pdfStream        - Readable stream containing PDF data
  4. PdfDocument pdfDocument - A CodeBrix.PdfDocuments.Pdf.PdfDocument object

The PdfDocument overloads serialize the document to a byte array internally,
so they work with in-memory documents that have not been saved to disk. The
PdfDocument overloads have no password parameter (the object is already open);
every other overload does.

--- IMAGE FORMATS ---

Supported output formats (from CodeBrix.Imaging):

    PngFormat.Instance      // Default. Lossless, supports transparency.
    JpegFormat.Instance     // Lossy, smaller files, no transparency.
    BmpFormat.Instance      // Uncompressed bitmap, no transparency.
    GifFormat.Instance      // 256 colors, supports transparency.
    TiffFormat.Instance     // Lossless, supports multi-page.

Set per-call via the desiredImageFormat parameter, or as the default via the
RasterizedImageFormat property. Passing UnknownImageFormat per-call throws
ArgumentException.

--- RENDER FLAGS (PdfRenderFlags, [Flags] enum, complete list) ---

    PdfRenderFlags.None                     = 0x00    // No special flags
    PdfRenderFlags.RenderAnnotations        = 0x01    // Render form fields/annotations
    PdfRenderFlags.OptimizeTextForLcd       = 0x02    // LCD-optimized text
    PdfRenderFlags.NoNativeText             = 0x04    // Don't use native text rendering
    PdfRenderFlags.Grayscale                = 0x08    // Render in grayscale
    PdfRenderFlags.LimitImageCacheSize      = 0x200   // Limit PDFium's image cache
    PdfRenderFlags.ForceHalftone            = 0x400   // Force halftone for image stretching
    PdfRenderFlags.RenderForPrinting        = 0x800   // Print-quality rendering
    PdfRenderFlags.DisableTextAntialiasing  = 0x1000  // No text anti-aliasing
    PdfRenderFlags.DisableImageAntialiasing = 0x2000  // No image anti-aliasing
    PdfRenderFlags.DisablePathAntialiasing  = 0x4000  // No path anti-aliasing

These are the eleven members; there are no others. Every rasterization method
takes a renderFlags parameter whose default is
RenderAnnotations | RenderForPrinting. Combine flags with |. Passing a value
WITHOUT RenderAnnotations omits form fields and annotations from the output:

    // Grayscale, no annotations, print quality
    using var img = await rasterizer.RasterizeToImage(
        "document.pdf",
        pageNumber: 1,
        renderFlags: PdfRenderFlags.Grayscale | PdfRenderFlags.RenderForPrinting);

    // Keep the defaults and add one flag
    await rasterizer.RasterizeToImageFiles(
        "document.pdf",
        renderFlags: PdfRenderFlags.RenderAnnotations
                   | PdfRenderFlags.RenderForPrinting
                   | PdfRenderFlags.DisableTextAntialiasing);

--- DISPOSING ---

PageRasterizer implements IDisposable. Always dispose when done:

    using var rasterizer = new PageRasterizer();
    // ... use rasterizer ...
    // Disposed automatically at end of scope

After disposal, all rasterization and page-information methods throw
ObjectDisposedException.

NOTE: Disposing PageRasterizer does NOT unload PDFium. The native library is
initialized once per application lifetime and remains loaded; constructing
another PageRasterizer afterwards is cheap.

--- CANCELLATION ---

All async methods accept a CancellationToken:

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

For dpi, password, outputDirectory, desiredImageFormat and maxDimensions the
resolution order is:

  1. Method parameter (if provided and non-null; for dpi, if > 0)
  2. Property value (if set)
  3. Built-in default (Dpi=300, format=PNG, thumbnail=200 x 260)

This allows configuring once via properties, then overriding per-call.

--- THREAD SAFETY ---

PDFium is NOT thread-safe. Every call into the native library is serialized
through a single process-wide SemaphoreSlim. You CAN share one PageRasterizer
across threads, and you can create several instances, but native work always
executes one call at a time. Each call waits up to 10 seconds to acquire the
lock and throws TimeoutException if it cannot; a long-running call on another
thread can therefore make a concurrent call fail rather than queue forever.
Do not expect parallel speed-up from multiple rasterizers.

================================================================================

COMPLETE EXAMPLES
=================

Example 1: Rasterize PDF Pages to Image Files
----------------------------------------------
    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    rasterizer.OutputDirectory = @"C:\Output\Images";
    rasterizer.Dpi = 150;

    await rasterizer.RasterizeToImageFiles("report.pdf");
    // Creates Rasterized_Page_1.png, Rasterized_Page_2.png, etc.

Example 2: Rasterize a Single Page to JPEG
-------------------------------------------
    using CodeBrix.Imaging;
    using CodeBrix.Imaging.Formats.Jpeg;
    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    using var image = await rasterizer.RasterizeToImage(
        "report.pdf",
        pageNumber: 1,
        desiredImageFormat: JpegFormat.Instance);

    image.Save("page1.jpg");
    Console.WriteLine($"Image: {image.Width}x{image.Height}");

Example 3: Generate Thumbnails for All Pages
---------------------------------------------
    using CodeBrix.Imaging;
    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    var maxDims = new ThumbnailMaxDimensions(150, 200);

    IList<Image> thumbnails = await rasterizer.RasterizeToThumbnails(
        "report.pdf",
        maxDimensions: maxDims);

    for (var i = 0; i < thumbnails.Count; i++)
    {
        thumbnails[i].Save($"thumb_{i + 1}.png");
        Console.WriteLine($"Thumb {i + 1}: {thumbnails[i].Width}x{thumbnails[i].Height}");
        thumbnails[i].Dispose();
    }

Example 4: Rasterize In-Memory PdfDocument
-------------------------------------------
    using CodeBrix.Imaging;
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfRasterizer;

    // Create a PDF in memory (CodeBrix.PdfDocuments)
    var document = new PdfDocument();
    var page = document.AddPage();
    var gfx = XGraphics.FromPdfPage(page);
    gfx.DrawString("Hello!", new XFont("Arial", 24),
        XBrushes.Black, new XPoint(50, 50));

    // Rasterize directly from the PdfDocument - no Save() needed
    using var rasterizer = new PageRasterizer();
    using var image = await rasterizer.RasterizeToImage(document, pageNumber: 1);
    image.Save("rendered.png");

Example 5: Get Page Dimensions
-------------------------------
    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    int pageCount = await rasterizer.GetPageCount("report.pdf");

    for (var i = 1; i <= pageCount; i++)
    {
        var dims = await rasterizer.GetPageDimensions("report.pdf", pageNumber: i);
        Console.WriteLine($"Page {i}: {dims.WidthInInches:F1}\" x {dims.HeightInInches:F1}\"");
        Console.WriteLine($"  At 300 DPI: {dims.GetWidthInPixels(300)} x {dims.GetHeightInPixels(300)} px");
    }

Example 6: Rasterize PDF from Web API (In-Memory)
--------------------------------------------------
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
        thumbnail.SaveAsJpeg(ms);
        return ms.ToArray();
    }

Example 7: Render Flags and Background Color
---------------------------------------------
    using CodeBrix.Imaging.Formats.Png;
    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    rasterizer.OutputDirectory = @"C:\Output\Gray";
    rasterizer.RasterizedImageFormat = PngFormat.Instance;
    rasterizer.BackgroundColor = 0xFFF5F5F5;   // opaque light grey (0xAARRGGBB)
    rasterizer.AllowOverwriteFiles = true;

    // Grayscale, without form fields/annotations, without text anti-aliasing
    await rasterizer.RasterizeToImageFiles(
        "form.pdf",
        renderFlags: PdfRenderFlags.Grayscale
                   | PdfRenderFlags.RenderForPrinting
                   | PdfRenderFlags.DisableTextAntialiasing);

================================================================================

MINIMUM VIABLE PROJECT
======================

    dotnet new console -n MyPdfApp --framework net10.0
    cd MyPdfApp
    dotnet add package CodeBrix.PdfRasterizer.MitLicenseForever

MyPdfApp.csproj (as generated; only the package reference is added):

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.PdfRasterizer.MitLicenseForever" />
      </ItemGroup>
    </Project>

Program.cs:

    using CodeBrix.PdfRasterizer;

    using var rasterizer = new PageRasterizer();
    rasterizer.OutputDirectory = Path.Combine(AppContext.BaseDirectory, "Output");
    await rasterizer.RasterizeToImageFiles("document.pdf");
    Console.WriteLine("Rasterized all pages!");

Build and run:

    dotnet build
    dotnet run

Nothing else is required: the PDFium binary for the current platform is
copied to the build output by the package.

================================================================================

PERFORMANCE TIPS
================

1. USE LOWER DPI FOR THUMBNAILS: When generating thumbnails, the image will be
   downscaled anyway. Consider using a lower DPI (e.g., 150) to speed up
   rendering before the resize step - pass dpi: 150 to the thumbnail method
   or set the Dpi property.

2. CONFIGURE PROPERTIES ONCE: Set Dpi, OutputDirectory, Password,
   RasterizedImageFormat and ThumbnailMaxDimensions on the PageRasterizer
   instance once, then call methods without repeating parameters.

3. USE SINGLE-PAGE METHODS WHEN APPROPRIATE: If you only need one page, use
   RasterizeToImage / RasterizeToThumbnail instead of the multi-page variants.
   This avoids loading and rendering unnecessary pages. For a subset of pages,
   pass pageNumbers to the multi-page methods.

4. DISPOSE IMAGES PROMPTLY: Images returned by rasterization methods consume
   memory (width x height x 4 bytes each, uncompressed). Dispose them as soon
   as you are done processing.

5. NATIVE CALLS ARE SERIALIZED: Running several rasterizers on several threads
   does not render pages in parallel - every native call takes the same
   process-wide lock (and times out after 10 seconds of waiting). Batch work
   sequentially instead of fanning out.

6. PDF BYTES ARE READ ONCE PER CALL: The string, Stream and PdfDocument
   overloads each turn the input into a byte[] before rendering. When
   rendering several pages of the same document with single-page methods,
   read the file into a byte[] once and pass that.

================================================================================

COMMON PITFALLS TO AVOID
========================

1. DO NOT confuse the NuGet package name (CodeBrix.PdfRasterizer.
   MitLicenseForever) with the namespace (CodeBrix.PdfRasterizer). The
   ".MitLicenseForever" suffix never appears in code.

2. DO NOT forget to dispose PageRasterizer instances. Use 'using var' or
   'using (...) { }' patterns. After disposal, all methods throw
   ObjectDisposedException.

3. DO NOT forget to dispose Image objects returned by RasterizeToImages,
   RasterizeToImage, RasterizeToThumbnails and RasterizeToThumbnail. The
   caller owns the returned images.

4. DO NOT set RasterizedImageFormat to null or UnknownImageFormat. Null throws
   ArgumentNullException; UnknownImageFormat throws ArgumentException (also
   when passed per-call as desiredImageFormat).

5. DO NOT forget that page numbers are 1-based. Passing 0 or a number
   exceeding the page count throws ArgumentException.

6. DO NOT expect thread parallelism. PDFium is NOT thread-safe; all native
   calls are serialized through one lock. Concurrent calls work but execute
   sequentially, and a call that waits more than 10 seconds for the lock
   throws TimeoutException.

7. DO NOT use CodeBrix.PdfRasterizer on iOS or WebAssembly. PDFium binaries
   are bundled for Windows, macOS, Linux and Android (ARM64) only. The library
   fails at runtime on any platform without a bundled PDFium binary.

8. DO NOT try to install PDFium separately. The native binaries are bundled
   inside the NuGet package and copied to the output directory at build time.
   Do not strip the runtimes/ folder from the build output either - that is
   where the resolver looks first.

9. DO NOT call a file-writing method without an output directory. If neither
   the outputDirectory argument nor the OutputDirectory property is set,
   ArgumentException is thrown.

10. DO NOT rely on overwriting. AllowOverwriteFiles defaults to false, so a
    second run into the same directory throws IOException on the first
    existing file. Set AllowOverwriteFiles = true or use a fresh
    FileNameGenerator / directory.

11. DO NOT expect a transparent background in JPEG or BMP output. Only
    alpha-capable formats (PNG, GIF, TIFF) can keep a BackgroundColor whose
    alpha byte is 0x00, and only on pages PDFium reports as using
    transparency. Keep BackgroundColor opaque (0xFF alpha) for JPEG and BMP.

12. DO NOT pass renderFlags without RenderAnnotations if you still want form
    fields and annotations drawn. The parameter REPLACES the default
    (RenderAnnotations | RenderForPrinting); it does not add to it.

13. DO NOT pass a password to the PdfDocument overloads - they have no
    password parameter. The document is already open; a password only
    applies to the string / byte[] / Stream inputs.

================================================================================

WHAT THIS PACKAGE DOES NOT DO
=============================

  - It does NOT extract text from PDFs (no OCR, no text extraction API).
  - It does NOT fill or edit PDF forms. Form fields are RENDERED visually
    (when RenderAnnotations is set) but cannot be edited through any API here.
  - It does NOT create, merge, edit, encrypt or sign PDFs. For creating and
    manipulating PDFs use CodeBrix.PdfDocuments (repository-root
    AGENT-README.txt) or CodeBrix.PdfDocCreate; this package only reads and
    renders.
  - It does NOT render on iOS or WebAssembly (no PDFium binaries for them).
  - It does NOT render pages in parallel (see THREAD SAFETY).
  - It does NOT unload PDFium; the native library stays loaded for the life
    of the process.

CodeBrix.PdfRasterizer IS for: converting PDF pages to images (PNG, JPEG,
BMP, GIF, TIFF), generating thumbnails, and querying page counts and page
dimensions.

================================================================================

WORKING EXAMPLES ON GITHUB
==========================

The rasterizer's tests live inside the CodeBrix.PdfDocuments test project:

  PDF rasterization (PageRasterizer - every property with its reset/validation
  rules, every method family from file/bytes/stream/PdfDocument, every output
  format, page subsets, custom DPI and background color, thumbnails within
  default/custom/property bounds, page count and dimensions, cancellation,
  overwrite protection, disposal, invalid page numbers):
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/PdfRasterizer/PdfRasterizerTests.cs

  The Html2Pdf test project also uses PageRasterizer to verify rendered PDFs
  pixel-by-pixel - a worked example of the visual-regression pattern:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/HtmlPdfRendererTests.cs

HOW TO USE: Fetch the raw file content from GitHub using a URL like:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/{path}
For example:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/tests/CodeBrix.PdfDocuments.Tests/PdfRasterizer/PdfRasterizerTests.cs

================================================================================

QUICK REFERENCE CARD
====================

--- Install ---
    dotnet add package CodeBrix.PdfRasterizer.MitLicenseForever
Namespace:      CodeBrix.PdfRasterizer  (+ CodeBrix.Imaging for Image/formats)

--- PageRasterizer ---
Create:         using var r = new PageRasterizer()
                using var r = new PageRasterizer(logger: Console.WriteLine, password: "pw")
Set DPI:        r.Dpi = 300
Set output:     r.OutputDirectory = @"C:\Output"
Set format:     r.RasterizedImageFormat = JpegFormat.Instance
Set password:   r.Password = "secret"
File names:     r.FileNameGenerator = n => $"page-{n}"     // stem, no extension
Overwrite:      r.AllowOverwriteFiles = true               // default false
Background:     r.BackgroundColor = 0xFFFFFFFF             // uint ARGB
Thumb default:  r.ThumbnailMaxDimensions = new ThumbnailMaxDimensions(200, 260)
To files:       await r.RasterizeToImageFiles("doc.pdf")
Single file:    await r.RasterizeToImageFile("doc.pdf", 1, outputDirectory: dir)
To images:      IList<Image> imgs = await r.RasterizeToImages("doc.pdf")
Single image:   using var img = await r.RasterizeToImage("doc.pdf", 1)
Thumbnails:     IList<Image> t = await r.RasterizeToThumbnails("doc.pdf")
Single thumb:   using var t = await r.RasterizeToThumbnail("doc.pdf", 1)
Thumb files:    await r.RasterizeToThumbnailFiles("doc.pdf") / RasterizeToThumbnailFile(..., 1)
Thumb dims:     new ThumbnailMaxDimensions(MaxHorizontalPixels, MaxVerticalPixels)
Page subset:    pageNumbers: new[] { 1, 3 }                // 1-based
Flags:          renderFlags: PdfRenderFlags.RenderAnnotations | PdfRenderFlags.Grayscale
                (default RenderAnnotations | RenderForPrinting; 11 members total)
Cancel:         cancellationToken: ct
Page count:     int n = await r.GetPageCount("doc.pdf")
Page dims:      PdfPageDimensions d = await r.GetPageDimensions("doc.pdf", 1)
Dims props:     d.WidthInPoints, d.HeightInPoints, d.WidthInInches, d.HeightInInches,
                d.GetWidthInPixels(300), d.GetHeightInPixels(300)
Save image:     img.Save("page.png") / img.SaveAsJpeg(stream)   (CodeBrix.Imaging)
Dispose:        r.Dispose()  // or use 'using' pattern; images are yours to dispose

Input types:    string path, byte[], Stream, PdfDocument (no password param)
Formats:        PngFormat, JpegFormat, BmpFormat, GifFormat, TiffFormat (.Instance)
Resolution:     method arg > property > default (Dpi 300, PNG, 200 x 260)
Threading:      native calls serialized; 10 s lock wait then TimeoutException
Platforms:      win-x64/x86/arm64, osx-x64/arm64, linux-x64/arm64/arm/riscv64,
                android-arm64; NOT iOS / WebAssembly

Target: .NET 10 or later

================================================================================
