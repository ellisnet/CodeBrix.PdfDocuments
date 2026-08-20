# CodeBrix.PdfDocuments

Create and process PDF documents using .NET, without the need of interop.

CodeBrix.PdfDocuments is a .NET library for creating, reading, merging, and manipulating PDF documents.
CodeBrix.PdfDocCreate is a companion library that provides a document object model for building
richly formatted PDF documents with styled text, tables, charts, and images.
CodeBrix.PdfRasterizer is a companion library that renders PDF pages to images (PNG, JPEG, BMP, GIF,
TIFF) using the PDFium native rendering engine, with support for thumbnails, page information, and
cross-platform operation.

CodeBrix.PdfDocuments has dependencies on the CodeBrix.Imaging package for image and font handling,
and the CodeBrix.Compression package for data compression. CodeBrix.PdfRasterizer has dependencies
on CodeBrix.PdfDocuments and the CodeBrix.Imaging package, and bundles pre-built PDFium native
binaries for Windows, macOS, Linux, and Android.

CodeBrix.PdfDocCreate.Html2Pdf is a companion library that renders author-created HTML pages with
CSS styling into PDF documents, and CodeBrix.PdfDocCreate.Markdown2Pdf is a companion library that
renders any Markdown (.md) file into a nice-looking, pre-formatted, printable PDF with zero
configuration.

CodeBrix.PdfDocuments, CodeBrix.PdfDocCreate, CodeBrix.PdfRasterizer, CodeBrix.PdfDocCreate.Html2Pdf,
and CodeBrix.PdfDocCreate.Markdown2Pdf are provided as .NET 10 libraries and associated
`CodeBrix.PdfDocuments.MitLicenseForever`, `CodeBrix.PdfDocCreate.MitLicenseForever`,
`CodeBrix.PdfRasterizer.MitLicenseForever`, `CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever`, and
`CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever` NuGet packages.

CodeBrix.PdfDocuments supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025;
and will be actively supported by Microsoft until Nov 14, 2028. Please update your C#/.NET code and projects
to the latest LTS version of Microsoft .NET.

CodeBrix.PdfDocuments is a fork of the code of the popular PdfSharpCore library version 1.3.67 and the
MigraDocCore libraries version 1.3.67 - see below for licensing details.

## ⚠️ Important for Linux: SVG rendering needs a SkiaSharp native-assets package

**If your application runs on Linux and renders SVG content** through
`CodeBrix.PdfDocCreate.Html2Pdf` or `CodeBrix.PdfDocCreate.Markdown2Pdf`, your application must
reference **one** of these two NuGet packages itself:

```
dotnet add package SkiaSharp.NativeAssets.Linux
```

**or**

```
dotnet add package SkiaSharp.NativeAssets.Linux.NoDependencies
```

**Either package satisfies Html2Pdf equally - neither is recommended over the other.** Reference
exactly one, whichever suits your application. **If your application already references one of them
for its own reasons, keep that one** - nothing needs to change, and you should not swap it for the
other.

The two differ only in how the native library obtains font services: `SkiaSharp.NativeAssets.Linux`
links against the system `libfontconfig`, while `SkiaSharp.NativeAssets.Linux.NoDependencies` is
self-contained. That difference does not affect Html2Pdf, which never consults system fonts, so the
choice is yours to make on your own deployment grounds.

**Windows and macOS require nothing extra** - SkiaSharp supplies those native binaries through its
own package. This requirement applies to Linux only, and only when SVG content is actually rendered.

**Why isn't this just a package dependency?** Two mutually exclusive Linux native-assets variants
exist, and only the consuming application can decide which one it wants. If Html2Pdf declared either
one, it would force that choice on every consumer and conflict with applications that already
reference the other. So the choice is deliberately left to you.

**What happens if it is missing?** Nothing crashes. SVG images are skipped and the rest of the
document renders normally. The skip is reported through the render result's collected warnings, with
the code `image.svg.nativemissing` and a message naming both packages - so if SVG content is
unexpectedly absent from your PDF, inspect `result.Warnings`.

## CodeBrix.PdfDocuments supports:

* Creating PDF documents from scratch
* Reading and modifying existing PDF documents
* Merging multiple PDF documents
* Drawing text, images, and graphics on PDF pages
* Unicode text and metadata
* Document information (title, author, subject, etc.)
* Page formatting and layout
* Text formatting with custom fonts and styles
* Image embedding (PNG, JPEG, etc.)
* PDF security and encryption
* Document outlines and bookmarks
* Image data consolidation for optimized file sizes

## CodeBrix.PdfDocCreate additionally supports:

* Document object model for structured PDF creation
* Styled paragraphs with headings and body text
* Tables with formatted cells
* Charts
* Page headers and footers
* Paragraph alignment and spacing

## CodeBrix.PdfRasterizer supports:

* Rasterizing PDF pages to in-memory images or image files
* Rasterizing a single page or all pages at once
* Output in multiple image formats: PNG, JPEG, BMP, GIF, TIFF
* Configurable rendering resolution (DPI)
* Thumbnail generation with configurable maximum dimensions
* Page information: page count and page dimensions (points, inches, pixels)
* Encrypted/password-protected PDF support
* Selective page rasterization (specific page numbers)
* Configurable background color
* Custom file name generation for output files
* Form field rendering (fillable PDF forms)
* CancellationToken support for async operations
* Accepts PDF input from file paths, byte arrays, streams, or PdfDocument objects
* Cross-platform: Windows (x64, x86, ARM64), macOS (x64, ARM64), Linux (x64, ARM64, ARM, RISC-V 64), Android (ARM64)

## CodeBrix.PdfDocCreate.Html2Pdf supports:

CodeBrix.PdfDocCreate.Html2Pdf (`CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever`) renders
author-created HTML pages with CSS styling into PDF documents. It is designed for HTML/CSS written
for PDF generation - it is not a web browser.

* HTML parsing via CodeBrix.MarkupParse, CSS parsing via CodeBrix.StyleSheetParse
* A documented CSS dialect applied with real selector matching, cascade, specificity, and inheritance
* Inline `style` attributes, `<style>` blocks, and linked local stylesheets
* `@page` rules for page size, orientation, and margins
* Headings (with PDF outline/bookmarks), paragraphs, nested lists, definition lists, blockquotes,
  code blocks, tables (colspan and rowspan, content-measured column widths), images, and links
  (including in-document `#anchor` links)
* Every image format CodeBrix.Imaging decodes (PNG, JPEG, BMP, WebP, GIF, TIFF, TGA, PBM/PGM/PPM),
  with transparency preserved, as local files, data: URIs, or (opt-in) http(s) URLs
* SVG images - referenced files, `data:image/svg+xml` URIs, and inline `<svg>` elements - rendered
  through an offscreen CPU rasterizer, identically on Windows, macOS, and Linux (on Linux this
  requires a SkiaSharp native-assets package reference in your application - see the important
  note near the top of this document)
* Headers/footers with page-number tokens
* All text rendered with the CodeBrix.Platform.Fonts packages (Roboto, Merriweather, Roboto Mono),
  automatically copied into the application output - identical PDF output on every operating system
* Consumer font registration (loose `.ttf`/`.otf` files, no manifest needed) usable from CSS and
  SVG text, with per-glyph fallback decided against each font's actual character coverage
* Unsupported CSS and missing resources degrade to collectable warnings, never exceptions

## CodeBrix.PdfDocCreate.Markdown2Pdf supports:

CodeBrix.PdfDocCreate.Markdown2Pdf (`CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever`) renders
any Markdown (.md) file into a nice-looking, pre-formatted, printable PDF with zero configuration.

* A faithful C# port of the markdown-it CommonMark parser (verified against the full CommonMark
  specification test corpus)
* GFM tables and strikethrough, footnotes, GitHub task lists, and YAML front matter
* Automatic syntax highlighting for common fenced-code languages
* A polished built-in print style: Merriweather body text, Roboto headings, Roboto Mono code,
  footer page numbers, and a PDF outline built from the headings
* Title and author inference from front matter, the first heading, or the file name
* Embedded HTML rendered through the Html2Pdf element subset
* A restyling workflow: `GenerateHtml` returns the ready-to-render HTML/CSS so consumers can replace
  the stylesheet and render the result with Html2Pdf themselves

## Sample Code

### Create a Simple PDF

```csharp
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Pdf;

// Create a new PDF document
var document = new PdfDocument();

// Add a page and create a graphics renderer
var page = document.AddPage();
var renderer = XGraphics.FromPdfPage(page);

// Draw text on the page
renderer.DrawString(
    "Hello, PDF!",
    new XFont("Arial", 24),
    XBrushes.Black,
    new XPoint(50, 50));

// Save the document
document.Save("HelloPdf.pdf");
```

### Create a PDF with Images

```csharp
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Pdf;

var document = new PdfDocument();
var page = document.AddPage();
var renderer = XGraphics.FromPdfPage(page);

// Draw text
renderer.DrawString("PDF with Image", new XFont("Arial", 16), XBrushes.Black, new XPoint(12, 24));

// Draw an image from file
renderer.DrawImage(XImage.FromFile("photo.png"), new XPoint(12, 50));

document.Save("ImageDocument.pdf");
```

### Read and Merge PDF Documents

```csharp
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfDocuments.Pdf.IO;

// Open existing PDF files and merge them
var outputDocument = new PdfDocument();

foreach (var pdfPath in new[] { "document1.pdf", "document2.pdf" })
{
    using var fs = File.OpenRead(pdfPath);
    var inputDocument = PdfReader.Open(fs, PdfDocumentOpenMode.Import);

    for (var i = 0; i < inputDocument.PageCount; i++)
    {
        outputDocument.AddPage(inputDocument.Pages[i]);
    }
}

outputDocument.Save("merged.pdf");
```

### Create a Styled Document with CodeBrix.PdfDocCreate

```csharp
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocCreate.Rendering;

// Create a document with styled content
var doc = new Document
{
    Info =
    {
        Title = "Sales Report",
        Subject = "Quarterly Sales Data",
        Author = "CodeBrix"
    }
};

// Define styles
var titleStyle = doc.AddStyle("Title", "Normal");
titleStyle.Font.Size = 24;
titleStyle.Font.Bold = true;
titleStyle.ParagraphFormat.SpaceAfter = 6;
titleStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;

var bodyStyle = doc.Styles["Normal"];
bodyStyle.Font.Size = 10;
bodyStyle.ParagraphFormat.SpaceAfter = 4;

// Add content
var section = doc.AddSection();
var titleParagraph = section.AddParagraph("Quarterly Sales Report");
titleParagraph.Style = "Title";

var bodyParagraph = section.AddParagraph("This report summarizes quarterly sales data.");
bodyParagraph.Style = "Normal";

// Render to PDF
var pdfRenderer = new PdfDocumentRenderer { Document = doc };
pdfRenderer.RenderDocument();
pdfRenderer.PdfDocument.Save("SalesReport.pdf");
```

### Rasterize PDF Pages to Image Files

```csharp
using CodeBrix.PdfRasterizer;

// Create a rasterizer instance
using var rasterizer = new PageRasterizer();

// Configure output settings
rasterizer.OutputDirectory = @"C:\Output\Images";
rasterizer.Dpi = 300;

// Rasterize all pages of a PDF to PNG files
await rasterizer.RasterizeToImageFiles("report.pdf");
```

### Rasterize a Single PDF Page to an In-Memory Image

```csharp
using CodeBrix.Imaging;
using CodeBrix.Imaging.Formats.Jpeg;
using CodeBrix.PdfRasterizer;

using var rasterizer = new PageRasterizer();

// Rasterize page 1 as a JPEG image
using var image = await rasterizer.RasterizeToImage(
    "report.pdf",
    pageNumber: 1,
    desiredImageFormat: JpegFormat.Instance);

// Save the image to a file
await image.SaveAsync("page1.jpg");

// Or access image properties
Console.WriteLine($"Image size: {image.Width} x {image.Height}");
```

### Generate Thumbnails

```csharp
using CodeBrix.Imaging;
using CodeBrix.PdfRasterizer;

using var rasterizer = new PageRasterizer();

// Generate thumbnails with custom max dimensions (150x200 pixels)
var maxDimensions = new ThumbnailMaxDimensions(150, 200);
IList<Image> thumbnails = await rasterizer.RasterizeToThumbnails(
    "report.pdf",
    maxDimensions: maxDimensions);

foreach (var thumbnail in thumbnails)
{
    Console.WriteLine($"Thumbnail size: {thumbnail.Width} x {thumbnail.Height}");
    thumbnail.Dispose();
}
```

### Get PDF Page Information

```csharp
using CodeBrix.PdfRasterizer;

using var rasterizer = new PageRasterizer();

// Get the number of pages
int pageCount = await rasterizer.GetPageCount("report.pdf");
Console.WriteLine($"Page count: {pageCount}");

// Get dimensions of a specific page
PdfPageDimensions dims = await rasterizer.GetPageDimensions("report.pdf", pageNumber: 1);
Console.WriteLine($"Page size: {dims.WidthInInches:F1}\" x {dims.HeightInInches:F1}\"");
Console.WriteLine($"At 300 DPI: {dims.GetWidthInPixels(300)} x {dims.GetHeightInPixels(300)} pixels");
```

### Rasterize Pages from a PdfDocument Object

```csharp
using CodeBrix.Imaging;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfRasterizer;

// Create a PDF document programmatically
var document = new PdfDocument();
var page = document.AddPage();
// ... draw content on the page ...

// Rasterize the in-memory document directly
using var rasterizer = new PageRasterizer();
IList<Image> images = await rasterizer.RasterizeToImages(document);

foreach (var image in images)
{
    // Process the image...
    image.Dispose();
}
```

### Render HTML with CSS to PDF

```csharp
using CodeBrix.PdfDocCreate.Html2Pdf;

var renderer = new HtmlPdfRenderer();
renderer.Options.FooterText = "Page {page} of {pages}";

var result = renderer.RenderFile("report.html", "report.pdf");

Console.WriteLine($"{result.PageCount} page(s), {result.Warnings.Count} warning(s)");
```

### Render Markdown to PDF (zero configuration)

```csharp
using CodeBrix.PdfDocCreate.Markdown2Pdf;

var renderer = new MarkdownPdfRenderer();

// Writes my_notes.pdf next to the source file
var result = renderer.RenderFile("my_notes.md");

Console.WriteLine($"'{result.Title}': {result.PageCount} page(s)");
```

### Restyle the generated Markdown HTML before rendering

```csharp
using CodeBrix.PdfDocCreate.Html2Pdf;
using CodeBrix.PdfDocCreate.Markdown2Pdf;

var markdownRenderer = new MarkdownPdfRenderer();
var generated = markdownRenderer.GenerateHtmlFromFile("my_notes.md");

var myCss = generated.Css + "\nh1 { color: #7a1f1f; }";
var html = generated.ToHtmlDocument(myCss);

var htmlRenderer = new HtmlPdfRenderer();
htmlRenderer.RenderHtml(html, "restyled.pdf", generated.BaseDirectory);
```

Note that significant additional sample code is available in the `CodeBrix.PdfDocuments.Tests`,
`CodeBrix.PdfDocCreate.Html2Pdf.Tests`, and `CodeBrix.PdfDocCreate.Markdown2Pdf.Tests` projects.

## License

The project is licensed under the MIT License. see: https://en.wikipedia.org/wiki/MIT_License

All code from PdfSharpCore version 1.3.67 and MigraDocCore version 1.3.67 was licensed under the
MIT License. This project (CodeBrix.PdfDocuments/CodeBrix.PdfDocCreate) complies with all
provisions of the open source license of PdfSharpCore and MigraDocCore (code) - and will make
all modified, adapted and derived code within the CodeBrix.PdfDocuments/CodeBrix.PdfDocCreate 
libraries freely available as open source, under the same license as the PdfSharpCore and 
MigraDocCore code license.

CodeBrix.PdfRasterizer contains P/Invoke bindings and rendering logic derived from
[Docnet.Core](https://github.com/GowenGit/docnet) (MIT License, copyright 2018 Modestas
Petravicius). Pre-built PDFium native binaries are bundled under a BSD 3-Clause license
(copyright 2014 The PDFium Authors). See THIRD-PARTY-NOTICES.txt in the root of the 
repository for full license details.
