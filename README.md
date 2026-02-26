# CodeBrix.PdfDocuments

Create and process PDF documents using .NET, without the need of interop.

CodeBrix.PdfDocuments is a .NET library for creating, reading, merging, and manipulating PDF documents.
CodeBrix.PdfDocCreate is a companion library that provides a document object model for building
richly formatted PDF documents with styled text, tables, charts, and images.

CodeBrix.PdfDocuments has dependencies on the CodeBrix.Imaging package for image and font handling,
and the CodeBrix.Compression package for data compression.

CodeBrix.PdfDocuments and CodeBrix.PdfDocCreate are provided as .NET 10 libraries and associated
`CodeBrix.PdfDocuments.MitLicenseForever` and `CodeBrix.PdfDocCreate.MitLicenseForever` NuGet packages.

CodeBrix.PdfDocuments supports applications and assemblies that target Microsoft .NET version 10.0 and later.
Microsoft .NET version 10.0 is a Long-Term Supported (LTS) version of .NET, and was released on Nov 11, 2025;
and will be actively supported by Microsoft until Nov 14, 2028. Please update your C#/.NET code and projects
to the latest LTS version of Microsoft .NET.

CodeBrix.PdfDocuments is a fork of the code of the popular PdfSharpCore library version 1.3.67 and the
MigraDocCore libraries version 1.3.67 - see below for licensing details.

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

Note that significant additional sample code is available in the `CodeBrix.PdfDocuments.Tests` project.

## License

The project is licensed under the MIT License. see: https://en.wikipedia.org/wiki/MIT_License

All code from PdfSharpCore version 1.3.67 and MigraDocCore version 1.3.67 was licensed under the
MIT License. This project (CodeBrix.PdfDocuments/CodeBrix.PdfDocCreate) complies with all
provisions of the open source license of PdfSharpCore and MigraDocCore (code) - and will make
all modified, adapted and derived code within the CodeBrix.PdfDocuments/CodeBrix.PdfDocCreate 
libraries freely available as open source, under the same license as the PdfSharpCore and 
MigraDocCore code license.
