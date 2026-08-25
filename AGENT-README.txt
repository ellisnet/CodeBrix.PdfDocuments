================================================================================
AGENT-README: CodeBrix.PdfDocuments
A Guide for AI Coding Agents — CONSUMING the CodeBrix.PdfDocuments.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.PdfDocuments is a pure-managed .NET library (.NET 10 or later) for
creating, reading, merging and manipulating PDF documents with direct graphics
drawing. You add pages to a PdfDocument, obtain an XGraphics for a page, and
draw text, images, shapes, paths, other PDF pages, charts and bar codes at
exact coordinates. It also opens existing PDFs (including encrypted ones),
copies pages between documents, adds bookmarks, links, annotations, fills
form fields, applies passwords and permissions, and exposes the raw PDF
object model for anything the typed API does not cover.

There is no automatic pagination or text flow at this level: you decide where
every page break goes. For structured documents (paragraphs, tables, headers
that flow across pages) use the companion CodeBrix.PdfDocCreate package, which
is built on top of this one (see OTHER PACKAGES IN THIS REPOSITORY below).

PROVENANCE: CodeBrix.PdfDocuments is a port of PdfSharpCore 1.3.67. If you know
PdfSharp / PdfSharpCore, the API is very similar - but EVERY namespace is
CodeBrix.PdfDocuments.* (for example CodeBrix.PdfDocuments.Pdf and
CodeBrix.PdfDocuments.Drawing). Do NOT write "using PdfSharp..." or
"using PdfSharpCore..."; those namespaces do not exist in this package. Do not
write API from memory of the upstream library either - several members differ
(font resolution, XTextFormatter, image sources); this file documents the real
surface.

PACKAGE NAME vs NAMESPACE (the single most common mistake):

  NuGet package                            Namespace root
  ---------------------------------------  ------------------------------
  CodeBrix.PdfDocuments.MitLicenseForever  CodeBrix.PdfDocuments.*

The ".MitLicenseForever" suffix belongs to the PACKAGE ID only. It never
appears in a namespace, a using directive or a type name.

Source repository: https://github.com/ellisnet/CodeBrix.PdfDocuments
License: MIT

OTHER PACKAGES IN THIS REPOSITORY
---------------------------------
This repository also produces four companion packages. Each has its own
AGENT-README; this file covers ONLY CodeBrix.PdfDocuments.

  CodeBrix.PdfDocCreate.MitLicenseForever (MIT)
      High-level document object model (paragraphs, tables, styles, headers,
      footers, charts) rendered to PDF through CodeBrix.PdfDocuments; the
      underlying PdfDocument stays reachable for low-level post-processing.
      See src/CodeBrix.PdfDocCreate/AGENT-README.txt

  CodeBrix.PdfRasterizer.MitLicenseForever (MIT)
      Renders PDF pages to PNG/JPEG/BMP/GIF/TIFF images and thumbnails with a
      bundled PDFium engine; accepts a path, bytes, a Stream or a PdfDocument.
      See src/CodeBrix.PdfRasterizer/AGENT-README.txt

  CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever (MIT)
      Renders author-created HTML + CSS to PDF through PdfDocCreate, with
      package fonts and SVG support.
      See src/CodeBrix.PdfDocCreate.Html2Pdf/AGENT-README.txt

  CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever (MIT)
      Renders any Markdown file to a printable PDF with zero configuration
      (vendored markdown-it port + Html2Pdf).
      See src/CodeBrix.PdfDocCreate.Markdown2Pdf/AGENT-README.txt

WHEN TO USE THIS PACKAGE ALONE: fine-grained control over page layout and
graphics, stamping/watermarking existing PDFs, merging and splitting, security,
bookmarks, links, annotations, form filling, and raw PDF object access.

================================================================================

INSTALLATION
============
NuGet package id: CodeBrix.PdfDocuments.MitLicenseForever

    dotnet add package CodeBrix.PdfDocuments.MitLicenseForever

Or in a .csproj (NuGet resolves the latest version):

    <PackageReference Include="CodeBrix.PdfDocuments.MitLicenseForever" />

NuGet dependencies (pulled in automatically):
  - CodeBrix.Compression.MitLicenseForever   (Flate / deflate streams)
  - CodeBrix.Imaging.ApacheLicenseForever    (image decoding, font parsing)

License: MIT

Requirements:
  - .NET 10 or later.
  - Pure managed code; no native libraries are bundled or required.
  - System-font discovery (used when you do not register your own font
    resolver) is implemented for Windows, macOS and Linux only. Linux
    discovery asks fontconfig for installed .ttf files (with a fallback scan
    of the font directories named in /etc/fonts/fonts.conf); Windows scans
    %SystemRoot%\Fonts and %LOCALAPPDATA%\Microsoft\Windows\Fonts; macOS
    scans /Library/Fonts. Only .ttf files are considered. On any other
    platform, or in a container with no fonts, register your own
    IFontResolver before creating the first XFont (see FONTS AND FONT
    RESOLUTION).

================================================================================

KEY NAMESPACES / USINGS
=======================

    using CodeBrix.PdfDocuments;                 // PageSize, PageOrientation enums
    using CodeBrix.PdfDocuments.Pdf;             // PdfDocument, PdfPage, PdfPages,
                                                 // PdfDocumentOptions, PdfOutline,
                                                 // PdfRectangle, PdfCustomValues,
                                                 // PdfDictionary, PdfArray, PdfName,
                                                 // PdfString, PdfInteger, PdfReal ...
    using CodeBrix.PdfDocuments.Pdf.IO;          // PdfReader, PdfDocumentOpenMode,
                                                 // PdfPasswordProvider
    using CodeBrix.PdfDocuments.Pdf.IO.enums;    // PdfReadAccuracy
    using CodeBrix.PdfDocuments.Pdf.Security;    // PdfSecuritySettings,
                                                 // PdfDocumentSecurityLevel
    using CodeBrix.PdfDocuments.Pdf.Annotations; // PdfTextAnnotation,
                                                 // PdfLinkAnnotation,
                                                 // PdfRubberStampAnnotation
    using CodeBrix.PdfDocuments.Pdf.AcroForms;   // PdfAcroForm, PdfTextField,
                                                 // PdfCheckBoxField ...
    using CodeBrix.PdfDocuments.Pdf.Actions;     // PdfGoToAction
    using CodeBrix.PdfDocuments.Pdf.Advanced;    // PdfInternals, PdfCatalog,
                                                 // PdfReference, PdfImage,
                                                 // PdfFormXObject, PdfContents
    using CodeBrix.PdfDocuments.Pdf.Content;     // ContentReader
    using CodeBrix.PdfDocuments.Pdf.Content.Objects; // CSequence, COperator, CObject
    using CodeBrix.PdfDocuments.Pdf.Filters;     // Filtering, FlateDecode
    using CodeBrix.PdfDocuments.Drawing;         // XGraphics, XFont, XBrush(es),
                                                 // XPen(s), XColor(s), XImage,
                                                 // XPdfForm, XForm, XGraphicsPath,
                                                 // XPoint, XSize, XRect, XUnit,
                                                 // XMatrix, XStringFormat(s)
    using CodeBrix.PdfDocuments.Drawing.Layout;  // XTextFormatter,
                                                 // TextFormatAlignment,
                                                 // XParagraphAlignment
    using CodeBrix.PdfDocuments.Drawing.Layout.enums; // XVerticalAlignment
    using CodeBrix.PdfDocuments.Drawing.BarCodes;// Code3of9Standard,
                                                 // Code2of5Interleaved,
                                                 // CodeDataMatrix, CodeOmr
    using CodeBrix.PdfDocuments.Charting;        // Chart, ChartFrame, Series,
                                                 // XSeries, Axis, Legend
    using CodeBrix.PdfDocuments.Fonts;           // IFontResolver, FontResolverInfo,
                                                 // GlobalFontSettings,
                                                 // MetaFontResolver,
                                                 // EmbeddedFontResolver
    using CodeBrix.PdfDocuments.Utils;           // ImagingImageSource<TPixel>
                                                 // (bridge from CodeBrix.Imaging)

Most files need only the first three or four of these. PageSize and
PageOrientation live in the ROOT namespace CodeBrix.PdfDocuments, not in .Pdf.

================================================================================

CORE API REFERENCE
==================

THE THREE OBJECTS YOU ALWAYS USE
--------------------------------

    public sealed class PdfDocument : PdfObject, IDisposable
        public PdfDocument()
        public PdfDocument(string filename)         // saved to this path on Close()
        public PdfDocument(Stream outputStream)     // saved to this stream on Close()
        public PdfPage AddPage()
        public PdfPage AddPage(PdfPage page, AnnotationCopyingType annotationCopying = AnnotationCopyingType.ShallowCopy)
        public PdfPage InsertPage(int index)
        public PdfPage InsertPage(int index, PdfPage page, AnnotationCopyingType annotationCopying = AnnotationCopyingType.ShallowCopy)
        public PdfPages Pages                       // indexer, Count, enumerable
        public int PageCount
        public PdfDocumentInformation Info          // Title, Author, Subject, Keywords,
                                                    // Creator, Producer, CreationDate,
                                                    // ModificationDate
        public PdfDocumentOptions Options
        public PdfDocumentSettings Settings         // .TrimMargins
        public PdfSecuritySettings SecuritySettings
        public PdfOutlineCollection Outlines
        public PdfAcroForm AcroForm                 // null when there is no form
        public PdfViewerPreferences ViewerPreferences
        public PdfPageLayout PageLayout
        public PdfPageMode PageMode
        public PdfCustomValues CustomValues
        public string Language
        public int Version                          // e.g. 14 = PDF 1.4
        public bool IsImported, IsReadOnly
        public long FileSize; public string FullPath; public Guid Guid
        public object Tag
        public new PdfInternals Internals
        public void ConsolidateImages()
        public void MakeAcroFormsReadOnly()
        public void Save(string path)
        public void Save(Stream stream)             // leaves the stream OPEN, Position reset to 0
        public void Save(Stream stream, bool closeStream)
        public bool CanSave(ref string message)
        public void Close()
        public void Dispose()

    public sealed class PdfPage : PdfDictionary
        public PageSize Size                        // enum in CodeBrix.PdfDocuments
        public PageOrientation Orientation          // Portrait | Landscape
        public XUnit Width, Height
        public int Rotate                           // must be a multiple of 90
        public TrimMargins TrimMargins
        public PdfRectangle MediaBox, CropBox, BleedBox, ArtBox, TrimBox
        public PdfContents Contents
        public PdfResources Resources
        public bool HasAnnotations
        public PdfAnnotations Annotations
        public PdfLinkAnnotation AddDocumentLink(PdfRectangle rect, int destinationPage)
        public PdfLinkAnnotation AddWebLink(PdfRectangle rect, string url)
        public PdfLinkAnnotation AddFileLink(PdfRectangle rect, string fileName)
        public PdfCustomValues CustomValues
        public object Tag
        public void Close()

    public sealed class XGraphics : IDisposable
        public static XGraphics FromPdfPage(PdfPage page)
        public static XGraphics FromPdfPage(PdfPage page, XGraphicsUnit unit)
        public static XGraphics FromPdfPage(PdfPage page, XPageDirection pageDirection)
        public static XGraphics FromPdfPage(PdfPage page, XGraphicsPdfPageOptions options)
        public static XGraphics FromPdfPage(PdfPage page, XGraphicsPdfPageOptions options, XGraphicsUnit unit, XPageDirection pageDirection)
        public static XGraphics FromForm(XForm form)
        public static XGraphics FromPdfForm(XPdfForm form)
        public static XGraphics FromImage(XImage image)
        public static XGraphics CreateMeasureContext(XSize size, XGraphicsUnit pageUnit, XPageDirection pageDirection)
        public XSize PageSize; public XGraphicsUnit PageUnit; public XPageDirection PageDirection
        public XMatrix Transform                    // current world transform (read-only)
        public XSmoothingMode SmoothingMode
        public PdfPage PdfPage
        public SpaceTransformer Transformer         // .WorldToDefaultPage(XRect) for links/annotations
        public void Dispose()

Create one XGraphics per page, draw, and dispose it (a 'using' declaration is
the idiomatic form). Disposing finishes the content stream.

CREATING A NEW PDF
------------------

    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;

    var document = new PdfDocument();
    var page = document.AddPage();
    using (var gfx = XGraphics.FromPdfPage(page))
    {
        gfx.DrawString("Hello, PDF!", new XFont("Arial", 24), XBrushes.Black,
            new XPoint(50, 50));
    }
    document.Save("output.pdf");

DOCUMENT INFORMATION (METADATA)
-------------------------------

    document.Info.Title = "My Document";
    document.Info.Subject = "Document Subject";
    document.Info.Author = "Author Name";
    document.Info.Keywords = "pdf, codebrix, .net";
    document.Info.Creator = "My Application";
    document.Info.CreationDate = DateTime.UtcNow;

Unicode metadata is supported:

    document.Info.Title = "English, Ελληνικά, 漢語";

PAGE SETUP
----------

    using CodeBrix.PdfDocuments;          // PageSize, PageOrientation

    var page = document.AddPage();

    // Standard sizes (default is A4)
    page.Size = PageSize.Letter;          // 8.5" x 11"
    page.Size = PageSize.A4;              // 210mm x 297mm
    page.Size = PageSize.Legal;           // 8.5" x 14"

    // Orientation
    page.Orientation = PageOrientation.Landscape;

    // Custom size (XUnit; 1 inch = 72 points)
    page.Width = XUnit.FromInch(8.5);
    page.Height = XUnit.FromMillimeter(200);

    // Display rotation (viewer rotates the page; multiples of 90 only)
    page.Rotate = 90;

    // Trim margins: content boxes shrink by these amounts when set
    page.TrimMargins.All = XUnit.FromMillimeter(3);   // or Left/Right/Top/Bottom
    document.Settings.TrimMargins.All = XUnit.FromMillimeter(3); // default for new pages

PageSize values: Undefined, A0-A6, RA0-RA5, B0-B5, C0-C5, Quarto, Foolscap,
Executive, GovernmentLetter, Letter, Legal, Ledger, Tabloid, Post, Crown,
LargePost, Demy, Medium, Royal, Elephant, DoubleDemy, QuadDemy, STMT, Folio,
Statement, Size10x14.

COORDINATES AND UNITS
---------------------
XGraphics coordinates are in points by default (72 per inch), origin top-left,
y growing downwards. Change the unit or direction when you obtain the
graphics object:

    using var gfx = XGraphics.FromPdfPage(page, XGraphicsUnit.Millimeter);
    using var gfx = XGraphics.FromPdfPage(page, XPageDirection.Upwards); // PDF-style y axis

XGraphicsUnit: Point, Inch, Millimeter, Centimeter, Presentation (1/96").
XPageDirection: Downwards (default), Upwards.

Value types (all in CodeBrix.PdfDocuments.Drawing):

    public struct XUnit           // XUnit.FromPoint/FromInch/FromMillimeter/
                                  // FromCentimeter/FromPresentation(double);
                                  // .Point/.Inch/.Millimeter/.Centimeter;
                                  // implicit from double/int (points) and to double
    public struct XPoint          // new XPoint(double x, double y); .X .Y
    public struct XSize           // new XSize(double width, double height); .Width .Height
    public struct XRect           // new XRect(x, y, width, height); new XRect(XPoint, XSize);
                                  // XRect.FromLTRB(l, t, r, b); .X .Y .Width .Height
                                  // .Left .Top .Right .Bottom .Center .TopLeft ... .Size
    public struct XVector         // direction/offset arithmetic with XPoint
    public struct XMatrix         // new XMatrix(m11, m12, m21, m22, offsetX, offsetY);
                                  // XMatrix.Identity; .Translate/.Scale/.Rotate/
                                  // .RotateAt/.Shear/.Skew(...); .Multiply(XMatrix, XMatrixOrder);
                                  // .Invert(); .Determinant; .HasInverse; .IsIdentity

    var widthPt = page.Width.Point;               // XUnit -> double
    double w = page.Width;                        // implicit conversion also works

DRAWING TEXT
------------

    public void DrawString(string s, XFont font, XBrush brush, XPoint point)
    public void DrawString(string s, XFont font, XBrush brush, double x, double y)
    public void DrawString(string s, XFont font, XBrush brush, XPoint point, XStringFormat format)
    public void DrawString(string s, XFont font, XBrush brush, XRect layoutRectangle)
    public void DrawString(string text, XFont font, XBrush brush, XRect layoutRectangle, XStringFormat format)
    public XSize MeasureString(string text, XFont font)
    public XSize MeasureString(string text, XFont font, XStringFormat stringFormat)

The point/x,y overloads position the text BASELINE at the given point (left
aligned). The rectangle overloads position within the rectangle according to
the XStringFormat:

    public class XStringFormat
        public XStringAlignment Alignment        // Near | Center | Far (horizontal)
        public XLineAlignment LineAlignment      // Near | Center | Far | BaseLine

    public static class XStringFormats             // ready-made instances
        Default, BaseLineLeft, TopLeft, CenterLeft, BottomLeft, BaseLineCenter,
        TopCenter, Center, BottomCenter, BaseLineRight, TopRight, CenterRight,
        BottomRight

    var font = new XFont("Arial", 16);
    gfx.DrawString("Hello World", font, XBrushes.Black, new XPoint(50, 50));

    // Centred title in a box
    var box = new XRect(0, 30, page.Width, 40);
    gfx.DrawString("Centred title", new XFont("Arial", 20, XFontStyle.Bold),
        XBrushes.DarkBlue, box, XStringFormats.Center);

    // Right-aligned page number at a custom format
    var right = new XStringFormat { Alignment = XStringAlignment.Far,
                                    LineAlignment = XLineAlignment.BaseLine };
    gfx.DrawString("Page 1", font, XBrushes.Gray, new XPoint(page.Width - 40, page.Height - 30), right);

    // Measuring for manual layout
    XSize size = gfx.MeasureString("Hello World", font);
    double x = (page.Width - size.Width) / 2;

Fonts:

    public sealed class XFont
        public XFont(string familyName, double emSize)
        public XFont(string familyName, double emSize, XFontStyle style)
        public XFont(string familyName, double emSize, XFontStyle style, XPdfFontOptions pdfOptions)
        public XFontFamily FontFamily      // .Name = the family actually resolved
        public string Name; public double Size; public XFontStyle Style
        public bool Bold, Italic, Underline, Strikeout
        public double GetHeight(); public int Height
        public XFontMetrics Metrics
        public XPdfFontOptions PdfOptions

    XFontStyle: Regular, Bold, Italic, BoldItalic, Underline, Strikeout (flags)

    gfx.DrawString("Bold", new XFont("Arial", 12, XFontStyle.Bold), XBrushes.Black, 50, 80);
    gfx.DrawString("Bold italic underlined", new XFont("Arial", 12,
        XFontStyle.BoldItalic | XFontStyle.Underline), XBrushes.Black, 50, 100);

FONTS AND FONT RESOLUTION
-------------------------
NO IFontResolver REGISTRATION IS REQUIRED on Windows, macOS and Linux: the
installed system fonts are discovered automatically the first time a font is
needed. Just construct an XFont with a family name and draw. (This differs
from upstream PdfSharpCore, where forgetting to set the resolver is the classic
first failure.)

    var font = new XFont("Arial", 12);          // no setup needed

HOW RESOLUTION WORKS (so you can predict it):

    public static class GlobalFontSettings
        public static IFontResolver FontResolver           // default: MetaFontResolver.Instance
        public static PdfFontEncoding DefaultFontEncoding  // default: Unicode
        public const string DefaultFontName = "PlatformDefault";

    public interface IFontResolver
        FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic);
        byte[] GetFont(string faceName);
        string DefaultFontName { get; }

    public class FontResolverInfo
        public FontResolverInfo(string faceName)
        public FontResolverInfo(string faceName, bool mustSimulateBold, bool mustSimulateItalic)
        public FontResolverInfo(string faceName, XStyleSimulations styleSimulations)
        public string FaceName; public bool MustSimulateBold, MustSimulateItalic

    public class MetaFontResolver : IFontResolver          // the default resolver
        public static MetaFontResolver Instance
        public void RegisterFontResolver(string faceName, IFontResolver resolver)

    public class EmbeddedFontResolver : IFontResolver      // serves .ttf embedded resources
        public EmbeddedFontResolver(string fontFamilyName,
            IList<EmbeddedResourceFontFace> fontFaceResources,
            Assembly fontEmbeddedResourceAssembly)
    public record EmbeddedResourceFontFace(string FaceName, string EmbeddedResourceName);

    public class FontResolver : IFontResolver              // CodeBrix.PdfDocuments.Utils
        public string DefaultFontName => "Arial";          // the system-font resolver
        public bool NullIfFontNotFound { get; set; }       // default false

    public static class PlatformFontResolver                // CodeBrix.PdfDocuments.Fonts
        public static FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)

PlatformFontResolver.ResolveTypeface is the platform lookup itself, exposed so
a custom IFontResolver can delegate to it for families it does not serve.
(XPrivateFontCollection is public but carries no public members; there is
nothing to call on it - register fonts through an IFontResolver instead.)

MetaFontResolver first looks for a registered resolver whose DefaultFontName
equals the requested family (case-insensitive); otherwise it falls back to
the system-font resolver (CodeBrix.PdfDocuments.Utils.FontResolver). GetFont
requests are routed by the FACE name that was registered.

THE CATCH: AN UNAVAILABLE FAMILY NEVER THROWS - IT IS SILENTLY SUBSTITUTED.

The system-font resolver returns the FIRST installed font it knows when the
family is not found (NullIfFontNotFound is false by default). A misspelled or
not-installed family name therefore produces a document that renders in some
other face with no exception, no warning and no log entry. Verify what you
actually got:

    var font = new XFont("Consolas", 12);
    if (!string.Equals(font.FontFamily.Name, "Consolas", StringComparison.OrdinalIgnoreCase))
    {
        // A substitute was chosen - the layout may not be what you expect.
    }

This bites hardest cross-platform, and worst of all for MONOSPACE text. Typical
results on a stock Linux desktop with no resolver registered:

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
    all. A slim base image may have almost none installed. Ship .ttf files as
    embedded resources and register an EmbeddedFontResolver for each family
    BEFORE the first XFont is created. This is exactly what the test suite does
    to get identical output on every OS:

        using CodeBrix.PdfDocuments.Fonts;

        var roboto = new EmbeddedFontResolver(
            fontFamilyName: "Roboto",
            fontFaceResources:
            [
                new EmbeddedResourceFontFace(FaceName: "Roboto-Regular",
                    EmbeddedResourceName: "MyApp.Fonts.Roboto-Regular.ttf"),
                new EmbeddedResourceFontFace(FaceName: "Roboto-Bold",
                    EmbeddedResourceName: "MyApp.Fonts.Roboto-Bold.ttf"),
            ],
            fontEmbeddedResourceAssembly: typeof(Program).Assembly);

        // Register the resolver once per FACE name it serves:
        MetaFontResolver.Instance.RegisterFontResolver("Roboto-Regular", roboto);
        MetaFontResolver.Instance.RegisterFontResolver("Roboto-Bold", roboto);

        var font = new XFont("Roboto", 12);                 // family name
        var bold = new XFont("Roboto", 12, XFontStyle.Bold);

    EmbeddedFontResolver maps (family, bold, italic) to a face by convention:
    it looks for face names ending in "-Bold", "-Italic", "-BoldItalic" and
    falls back to the regular face with style simulation when a face is
    missing. Faces are read from the assembly's manifest resources lazily.
  - To take over resolution completely, assign your own IFontResolver:

        GlobalFontSettings.FontResolver = new MyFontResolver();

    Do this at startup. The setter throws InvalidOperationException once any
    font has been used, and assigning null throws ArgumentNullException.
  - When output must be reproducible across machines, assert on
    XFont.FontFamily.Name in a test rather than trusting the name you asked for.

FONT EMBEDDING AND ENCODING: every font used is embedded in the PDF as a
subset (TrueType/OpenType via the resolved .ttf data). There is no option to
not embed. What you can choose is the encoding:

    public class XPdfFontOptions
        public XPdfFontOptions(PdfFontEncoding encoding)
        public PdfFontEncoding FontEncoding
        public static XPdfFontOptions WinAnsiDefault, UnicodeDefault

    PdfFontEncoding: WinAnsi (single-byte, Western only), Unicode (default; any script)

    var font = new XFont("Arial", 12, XFontStyle.Regular, XPdfFontOptions.UnicodeDefault);

TEXT LAYOUT (XTextFormatter)
----------------------------
For multi-line text with automatic line wrapping inside a rectangle:

    using CodeBrix.PdfDocuments.Drawing.Layout;
    using CodeBrix.PdfDocuments.Drawing.Layout.enums;   // XVerticalAlignment

    public class XTextFormatter
        public XTextFormatter(XGraphics gfx)
        public XParagraphAlignment Alignment { get; set; }           // Left (default) | Center | Right | Justify
        public XVerticalAlignment VerticalAlignment { get; set; }    // Top (default) | Middle | Bottom
        public bool AllowVerticalOverflow { get; set; }              // default false: text past the bottom is cut
        public void SetAlignment(TextFormatAlignment alignments)
        public void DrawString(string text, XFont font, XBrush brush, XRect layoutRectangle, XUnit? lineHeight = null)
        public void DrawString(string text, XFont font, XBrush brush, XRect layoutRectangle, TextFormatAlignment alignments, XUnit? lineHeight = null)
        public XRect GetLayout(string text, XFont font, XBrush brush, XRect layoutRectangle, XUnit? lineHeight = null)

    public class TextFormatAlignment
        public XParagraphAlignment Horizontal { get; set; }
        public XVerticalAlignment Vertical { get; set; }

    var tf = new XTextFormatter(gfx);
    var rect = new XRect(50, 50, 400, 200);           // x, y, width, height
    tf.DrawString(longText, new XFont("Arial", 12), XBrushes.Black, rect);

    // Alignment and a custom line height (points)
    tf.SetAlignment(new TextFormatAlignment
        { Horizontal = XParagraphAlignment.Justify, Vertical = XVerticalAlignment.Top });
    tf.DrawString(longText, new XFont("Arial", 12), XBrushes.Black, rect, 16);

    // How tall would it be?  (returns the rectangle actually used)
    XRect used = tf.GetLayout(longText, font, XBrushes.Black, rect);

XTextFormatter does NOT accept an XStringFormat: the fifth parameter is a line
height (XUnit?) or a TextFormatAlignment. Explicit "\n" characters start new
lines. There is no automatic page breaking - measure with GetLayout and start
a new page yourself.

COLORS, BRUSHES AND PENS
------------------------

    public struct XColor
        public static XColor FromArgb(int red, int green, int blue)
        public static XColor FromArgb(int alpha, int red, int green, int blue)
        public static XColor FromArgb(int argb)                  // 0xAARRGGBB packed
        public static XColor FromArgb(int alpha, XColor color)   // same colour, new alpha
        public static XColor FromCmyk(double cyan, double magenta, double yellow, double black)
        public static XColor FromGrayScale(double grayScale)
        public static XColor FromKnownColor(XKnownColor color)
        public static XColor FromName(string name)
        public double A; public byte R, G, B; public double C, M, Y, K, GS
        public XColorSpace ColorSpace                            // Rgb | Cmyk | GrayScale
        public bool IsEmpty; public static XColor Empty

    public static class XColors      // one static XColor per known colour name
                                     // (XColors.Red, XColors.SteelBlue, XColors.Transparent ...)

    public sealed class XSolidBrush : XBrush
        public XSolidBrush(XColor color)
        public XColor Color
    public static class XBrushes     // 141 ready-made solid brushes (XBrushes.Black ...)

    public sealed class XPen
        public XPen(XColor color)
        public XPen(XColor color, double width)
        public XPen(XBrush brush, double width)
        public XColor Color; public XBrush Brush; public double Width
        public XDashStyle DashStyle          // Solid | Dash | Dot | DashDot | DashDotDot | Custom
        public double[] DashPattern; public double DashOffset
        public XLineCap LineCap              // Flat | Round | Square
        public XLineJoin LineJoin            // Miter | Round | Bevel
        public double MiterLimit
    public static class XPens        // 141 ready-made 1pt pens (XPens.Black ...)

    public sealed class XLinearGradientBrush : XBaseGradientBrush
        public XLinearGradientBrush(XPoint point1, XPoint point2, XColor color1, XColor color2)
        public XLinearGradientBrush(XRect rect, XColor color1, XColor color2, XLinearGradientMode linearGradientMode)
    public sealed class XRadialGradientBrush : XBaseGradientBrush
        public XRadialGradientBrush(XPoint center, double r1, double r2, XColor color1, XColor color2)
        public XRadialGradientBrush(XPoint center1, XPoint center2, double r1, double r2, XColor color1, XColor color2)

    XLinearGradientMode: Horizontal, Vertical, ForwardDiagonal, BackwardDiagonal

    var brand = XColor.FromArgb(0x1F, 0x5F, 0x9F);
    var halfRed = XColor.FromArgb(128, XColors.Red);              // 50% alpha
    var brush = new XSolidBrush(brand);
    var pen = new XPen(XColors.Navy, 2) { DashStyle = XDashStyle.Dash, LineCap = XLineCap.Round };
    var custom = new XPen(XColors.Black, 1) { DashStyle = XDashStyle.Custom,
                                              DashPattern = new double[] { 4, 2, 1, 2 } };
    var fade = new XLinearGradientBrush(new XRect(50, 50, 300, 40),
        XColors.White, XColors.SteelBlue, XLinearGradientMode.Horizontal);
    gfx.DrawRectangle(fade, 50, 50, 300, 40);

DRAWING SHAPES
--------------
Every shape has pen-only (outline), brush-only (fill) and pen+brush overloads,
each with XRect/XPoint and plain-double variants:

    public void DrawLine(XPen pen, XPoint pt1, XPoint pt2)
    public void DrawLine(XPen pen, double x1, double y1, double x2, double y2)
    public void DrawLines(XPen pen, XPoint[] points)
    public void DrawRectangle(XPen pen, XBrush brush, double x, double y, double width, double height)
    public void DrawRectangle(XPen pen, XRect rect)  /  DrawRectangle(XBrush brush, XRect rect)
    public void DrawRectangles(XPen pen, XBrush brush, XRect[] rectangles)
    public void DrawRoundedRectangle(XPen pen, XBrush brush, XRect rect, XSize ellipseSize)
    public void DrawRoundedRectangle(XPen pen, XBrush brush, double x, double y, double width, double height, double ellipseWidth, double ellipseHeight)
    public void DrawEllipse(XPen pen, XBrush brush, XRect rect)
    public void DrawEllipse(XPen pen, double x, double y, double width, double height)
    public void DrawPolygon(XPen pen, XPoint[] points)
    public void DrawPolygon(XPen pen, XBrush brush, XPoint[] points, XFillMode fillmode)
    public void DrawArc(XPen pen, XRect rect, double startAngle, double sweepAngle)
    public void DrawPie(XPen pen, XBrush brush, XRect rect, double startAngle, double sweepAngle)
    public void DrawBezier(XPen pen, XPoint pt1, XPoint pt2, XPoint pt3, XPoint pt4)
    public void DrawBeziers(XPen pen, XPoint[] points)          // 1 + 3n points
    public void DrawCurve(XPen pen, XPoint[] points)              // cardinal spline
    public void DrawCurve(XPen pen, XPoint[] points, double tension)
    public void DrawClosedCurve(XPen pen, XBrush brush, XPoint[] points, XFillMode fillmode, double tension)

    XFillMode: Alternate (even-odd), Winding. Angles are degrees, clockwise
    from the x axis (page direction Downwards).

    gfx.DrawLine(XPens.Black, 50, 50, 250, 50);
    gfx.DrawRectangle(XPens.Black, XBrushes.LightBlue, 50, 70, 200, 100);
    gfx.DrawRoundedRectangle(XPens.DarkGray, XBrushes.WhiteSmoke,
        new XRect(50, 190, 200, 60), new XSize(12, 12));
    gfx.DrawEllipse(XPens.Red, 300, 70, 100, 80);
    gfx.DrawPolygon(XPens.Black, XBrushes.Gold,
        new[] { new XPoint(320, 200), new XPoint(380, 200), new XPoint(350, 250) },
        XFillMode.Winding);
    gfx.DrawPie(XPens.Black, XBrushes.Orange, new XRect(420, 70, 100, 100), 0, 120);
    gfx.DrawArc(XPens.Blue, new XRect(420, 190, 100, 100), 180, 90);
    gfx.DrawBezier(XPens.Green, new XPoint(50, 300), new XPoint(120, 250),
        new XPoint(180, 350), new XPoint(250, 300));

PATHS (XGraphicsPath)
---------------------
Build a figure once, then stroke, fill or clip with it:

    public sealed class XGraphicsPath
        public void AddMove(double x1, double y1)
        public void AddLine(double x1, double y1, double x2, double y2)  /  AddLine(XPoint, XPoint)
        public void AddLines(XPoint[] points)
        public void AddBezier(XPoint pt1, XPoint pt2, XPoint pt3, XPoint pt4)
        public void AddBeziers(XPoint[] points)
        public void AddCurve(XPoint[] points)  /  AddCurve(XPoint[] points, double tension)
        public void AddClosedCurve(XPoint[] points)
        public void AddArc(XRect rect, double startAngle, double sweepAngle)
        public void AddArc(XPoint point1, XPoint point2, XSize size, double rotationAngle, bool isLargeArg, XSweepDirection sweepDirection)
        public void AddRectangle(XRect rect)  /  AddRectangle(double x, double y, double width, double height)
        public void AddRoundedRectangle(double x, double y, double width, double height, double ellipseWidth, double ellipseHeight)
        public void AddEllipse(XRect rect)
        public void AddPolygon(XPoint[] points)
        public void AddPie(XRect rect, double startAngle, double sweepAngle)
        public void AddString(string s, XFontFamily family, XFontStyle style, double emSize, XPoint origin, XStringFormat format)
        public void AddString(string s, XFontFamily family, XFontStyle style, double emSize, XRect layoutRect, XStringFormat format)
        public void AddPath(XGraphicsPath path, bool connect)
        public void StartFigure(); public void CloseFigure()
        public XFillMode FillMode
        public XGraphicsPath Clone()

    XGraphics:
        public void DrawPath(XPen pen, XBrush brush, XGraphicsPath path)
        public void DrawPath(XPen pen, XGraphicsPath path)  /  DrawPath(XBrush brush, XGraphicsPath path)
        public void IntersectClip(XGraphicsPath path)

    var path = new XGraphicsPath();
    path.AddArc(new XRect(50, 50, 100, 100), 180, 180);   // top half-circle
    path.AddLine(150, 100, 150, 200);
    path.AddLine(150, 200, 50, 200);
    path.CloseFigure();
    gfx.DrawPath(new XPen(XColors.Black, 1.5), XBrushes.LightYellow, path);

    // Text as outline geometry
    var textPath = new XGraphicsPath();
    textPath.AddString("OUTLINE", new XFontFamily("Arial"), XFontStyle.Bold, 48,
        new XPoint(50, 300), XStringFormats.Default);
    gfx.DrawPath(XPens.DarkRed, textPath);

TRANSFORMS, STATE AND CLIPPING
------------------------------

    public XGraphicsState Save()
    public void Restore(XGraphicsState state)
    public void Restore()                                   // last saved state
    public XGraphicsContainer BeginContainer()
    public XGraphicsContainer BeginContainer(XRect dstrect, XRect srcrect, XGraphicsUnit unit)
    public void EndContainer(XGraphicsContainer container)
    public void TranslateTransform(double dx, double dy)
    public void ScaleTransform(double scaleX, double scaleY)  /  ScaleTransform(double scaleXY)
    public void ScaleAtTransform(double scaleX, double scaleY, XPoint center)
    public void RotateTransform(double angle)                 // degrees, about the origin
    public void RotateAtTransform(double angle, XPoint point)
    public void ShearTransform(double shearX, double shearY)
    public void SkewAtTransform(double shearX, double shearY, XPoint center)
    public void MultiplyTransform(XMatrix matrix)
    public void MultiplyTransform(XMatrix matrix, XMatrixOrder order)   // Prepend | Append
    public void IntersectClip(XRect rect)
    public void IntersectClip(XGraphicsPath path)
    public int GraphicsStateLevel

Each *Transform call has an overload taking an XMatrixOrder. Transforms
accumulate; use Save/Restore (or BeginContainer/EndContainer) to scope them.
The clip region can only shrink (IntersectClip) - restore a saved state to
get the previous clip back.

    // Diagonal rotated text (a "DRAFT" stamp) centred on the page
    var state = gfx.Save();
    gfx.RotateAtTransform(-45, new XPoint(page.Width / 2, page.Height / 2));
    gfx.DrawString("DRAFT", new XFont("Arial", 96, XFontStyle.Bold),
        new XSolidBrush(XColor.FromArgb(60, 255, 0, 0)),
        new XRect(0, 0, page.Width, page.Height), XStringFormats.Center);
    gfx.Restore(state);

    // Clip everything to a rounded panel, draw, then unclip
    gfx.Save();
    var panel = new XGraphicsPath();
    panel.AddRoundedRectangle(50, 400, 300, 150, 20, 20);
    gfx.IntersectClip(panel);
    gfx.DrawImage(photo, 50, 400, 300, 150);
    gfx.Restore();

    // Scaled sub-drawing with a container
    var c = gfx.BeginContainer();
    gfx.TranslateTransform(300, 600);
    gfx.ScaleTransform(0.5);
    DrawLogo(gfx);                                    // your own routine in "logo units"
    gfx.EndContainer(c);

DRAWING IMAGES
--------------

    public class XImage : IDisposable
        public static XImage FromFile(string path)
        public static XImage FromStream(Func<Stream> stream)     // factory: called when needed
        public static XImage FromImageSource(IImageSource imageSouce)
        public static bool ExistsFile(string path)
        public int PixelWidth, PixelHeight
        public double PointWidth, PointHeight              // size at the image's own DPI
        public XSize Size
        public double HorizontalResolution, VerticalResolution
        public bool Interpolate
        public void Dispose()

    XGraphics:
        public void DrawImage(XImage image, XPoint point)   /  DrawImage(XImage image, double x, double y)
        public void DrawImage(XImage image, XRect rect)     /  DrawImage(XImage image, double x, double y, double width, double height)
        public void DrawImage(XImage image, XRect destRect, XRect srcRect, XGraphicsUnit srcUnit)

    // From file, natural size
    gfx.DrawImage(XImage.FromFile("photo.png"), new XPoint(50, 150));

    // Scaled into a box (x, y, width, height in page units)
    gfx.DrawImage(XImage.FromFile("photo.jpg"), 50, 150, 200, 150);

    // From bytes already in memory
    byte[] bytes = File.ReadAllBytes("photo.png");
    using var img = XImage.FromStream(() => new MemoryStream(bytes));
    gfx.DrawImage(img, 50, 320, img.PointWidth / 2, img.PointHeight / 2);

    // Crop: draw the top-left quarter of the image into a box
    gfx.DrawImage(img, new XRect(300, 320, 100, 100),
        new XRect(0, 0, img.PixelWidth / 2.0, img.PixelHeight / 2.0), XGraphicsUnit.Point);

FROM A CodeBrix.Imaging IMAGE (process first, then embed). XImage.FromImageSource
takes an IImageSource, NOT a CodeBrix.Imaging Image - bridge it through
ImagingImageSource<TPixel> in CodeBrix.PdfDocuments.Utils:

    using CodeBrix.Imaging;
    using CodeBrix.Imaging.PixelFormats;
    using CodeBrix.Imaging.Processing;
    using CodeBrix.PdfDocuments.Utils;

    public class ImagingImageSource<TPixel> : ImageSource where TPixel : unmanaged, IPixel<TPixel>
        public static IImageSource FromImagingImage(Image<TPixel> image, IImageFormat imgFormat, int? quality = 75)

    using var image = Image.Load<Rgb24>("photo.jpg", out var format);
    image.Mutate(ctx => ctx.Grayscale().Resize(400, 300));
    var source = ImagingImageSource<Rgb24>.FromImagingImage(image, format);
    using var xImage = XImage.FromImageSource(source);
    gfx.DrawImage(xImage, 50, 50, 400, 300);

The abstract ImageSource class (CodeBrix.PdfDocuments.Drawing) also offers
static factories that return an IImageSource without CodeBrix.Imaging types
in your code:

    ImageSource.FromFile(string path, int? quality = 75)
    ImageSource.FromBinary(string name, Func<byte[]> imageSource, int? quality = 75)
    ImageSource.FromStream(string name, Func<Stream> imageStream, int? quality = 75)

"quality" is the JPEG quality used when the image is embedded as JPEG.

Supported image formats for embedding (every format CodeBrix.Imaging decodes):
    PNG, JPEG, BMP, WebP, GIF, TIFF, TGA, PBM/PGM/PPM
Formats that can carry transparency (PNG, WebP, GIF, BMP, TIFF, TGA) embed
losslessly with their alpha channel preserved; JPEG and PBM embed as JPEG.
Known decoder caveats (from CodeBrix.Imaging): no animated WebP, no
arithmetic-coded JPEG; animated GIF embeds its first frame.
SVG is NOT supported at this level - SVG placement is a feature of the
CodeBrix.PdfDocCreate.Html2Pdf / Markdown2Pdf packages.

DRAWING PDF PAGES ONTO PAGES (XPdfForm): WATERMARK, OVERLAY, STAMP, N-UP
-----------------------------------------------------------------------
An XPdfForm is a page of an EXISTING PDF wrapped as a drawable image. Draw it
onto any page of the document you are building: scaled, rotated, clipped,
several per page. This is how you watermark, overlay letterhead, stamp, impose
N-up, or "print" one PDF into another.

    public class XPdfForm : XForm             // XForm : XImage, so DrawImage accepts it
        public static XPdfForm FromFile(string path)
        public static XPdfForm FromFile(string path, PdfReadAccuracy accuracy)
        public static XPdfForm FromStream(Stream stream)
        public static XPdfForm FromStream(Stream stream, string password)
        public int PageNumber                 // 1-based; set it to switch page (default 1)
        public int PageIndex                  // 0-based alias
        public int PageCount
        public PdfPage Page                   // the source page
        public double PointWidth, PointHeight // source page size in points
        public XSize Size
        public XImage PlaceHolder             // drawn instead when the source is unavailable
        public static string ExtractPageNumber(string path, out int pageNumber)   // "file.pdf#3"

    // Letterhead: draw page 1 of a template under new content
    var letterhead = XPdfForm.FromFile("letterhead.pdf");
    var page = document.AddPage();
    page.Width = letterhead.PointWidth;
    page.Height = letterhead.PointHeight;
    using var gfx = XGraphics.FromPdfPage(page);
    gfx.DrawImage(letterhead, new XRect(0, 0, page.Width, page.Height));
    gfx.DrawString("Dear customer, ...", body, XBrushes.Black, 72, 200);

    // 2-up imposition: two source pages side by side on one landscape page
    var src = XPdfForm.FromFile("report.pdf");
    for (int i = 0; i < src.PageCount; i += 2)
    {
        var sheet = document.AddPage();
        sheet.Orientation = PageOrientation.Landscape;
        using var g = XGraphics.FromPdfPage(sheet);
        double w = sheet.Width / 2, h = sheet.Height;
        src.PageNumber = i + 1;
        g.DrawImage(src, new XRect(0, 0, w, h));
        if (i + 1 < src.PageCount)
        {
            src.PageNumber = i + 2;
            g.DrawImage(src, new XRect(w, 0, w, h));
        }
    }

To stamp a watermark ONTO the pages of an existing document instead, open it
with PdfDocumentOpenMode.Modify and draw on each page with
XGraphicsPdfPageOptions.Append - see EDITING EXISTING PAGES below and
COMPLETE EXAMPLE 4.

REUSABLE TEMPLATES (XForm): draw something once, place it many times. The
content is stored once in the PDF (a form XObject), so repeated logos or
frames do not bloat the file:

    public class XForm : XImage
        public XForm(PdfDocument document, XSize size)
        public XForm(PdfDocument document, XUnit width, XUnit height)
        public XForm(PdfDocument document, XRect viewBox)
        public void DrawingFinished()          // called automatically on first DrawImage

    var stamp = new XForm(document, XUnit.FromMillimeter(60), XUnit.FromMillimeter(20));
    using (var fg = XGraphics.FromForm(stamp))
    {
        fg.DrawRoundedRectangle(XPens.DarkRed, new XRect(1, 1, stamp.PointWidth - 2,
            stamp.PointHeight - 2), new XSize(6, 6));
        fg.DrawString("CONFIDENTIAL", new XFont("Arial", 14, XFontStyle.Bold),
            XBrushes.DarkRed, new XRect(0, 0, stamp.PointWidth, stamp.PointHeight),
            XStringFormats.Center);
    }
    foreach (PdfPage p in document.Pages)
    {
        using var g = XGraphics.FromPdfPage(p, XGraphicsPdfPageOptions.Append);
        g.DrawImage(stamp, p.Width - stamp.PointWidth - 20, 20);
    }

READING EXISTING PDFs
---------------------

    public static class PdfReader
        public static int TestPdfFile(string path)         // PDF version as int (e.g. 14), 0 = not a PDF
        public static int TestPdfFile(Stream stream)       // stream position is preserved
        public static int TestPdfFile(byte[] data)
        public static PdfDocument Open(string path)        // = Modify, Strict
        public static PdfDocument Open(string path, PdfDocumentOpenMode openmode)
        public static PdfDocument Open(string path, string password, PdfDocumentOpenMode openmode)
        public static PdfDocument Open(string path, PdfDocumentOpenMode openmode, PdfPasswordProvider provider)
        public static PdfDocument Open(string path, string password, PdfDocumentOpenMode openmode, PdfPasswordProvider provider, PdfReadAccuracy accuracy)
        public static PdfDocument Open(Stream stream)      // = Modify, Strict
        public static PdfDocument Open(Stream stream, PdfDocumentOpenMode openmode)
        public static PdfDocument Open(Stream stream, string password, PdfDocumentOpenMode openmode)
        public static PdfDocument Open(Stream stream, PdfDocumentOpenMode openmode, PdfPasswordProvider passwordProvider)
        public static PdfDocument Open(Stream stream, string password, PdfDocumentOpenMode openmode, PdfPasswordProvider passwordProvider, PdfReadAccuracy accuracy)
        (every Open also has an overload with a trailing PdfReadAccuracy)

    public delegate void PdfPasswordProvider(PdfPasswordProviderArgs args);
    public class PdfPasswordProviderArgs { public string Password; public bool Abort; }

    PdfDocumentOpenMode:
        Modify          - open for editing; can be saved
        Import          - open to copy pages from; CANNOT be saved or drawn on
        ReadOnly        - open for reading only
        InformationOnly - only the trailer/info is parsed
    PdfReadAccuracy: Strict (default), Moderate (tolerates broken references)

    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfDocuments.Pdf.IO;
    using CodeBrix.PdfDocuments.Pdf.IO.enums;

    // Open for modification
    var document = PdfReader.Open("existing.pdf", PdfDocumentOpenMode.Modify);

    // From a stream, for import (merging)
    using var fs = File.OpenRead("existing.pdf");
    var importDoc = PdfReader.Open(fs, PdfDocumentOpenMode.Import);

    // Damaged file: relax cross-reference checking
    var doc = PdfReader.Open("damaged.pdf", PdfDocumentOpenMode.Modify, PdfReadAccuracy.Moderate);

    // Prompt for a password only when the file needs one
    var doc = PdfReader.Open("maybe-encrypted.pdf", PdfDocumentOpenMode.Modify,
        args => { args.Password = AskUser(); });

    // Is it a PDF at all?
    if (PdfReader.TestPdfFile("unknown.bin") == 0) { /* not a PDF */ }

Opening a file that is not a valid PDF throws PdfReaderException
(CodeBrix.PdfDocuments.Pdf.IO). Text EXTRACTION is not provided (see WHAT THIS
PACKAGE DOES NOT DO); the content stream can be read as operators (see
CONTENT STREAMS).

EDITING EXISTING PAGES
----------------------
Open with PdfDocumentOpenMode.Modify, then obtain an XGraphics for a page with
an XGraphicsPdfPageOptions value that says where your drawing goes relative to
the page's existing content:

    XGraphicsPdfPageOptions:
        Append   - draw OVER the existing content (default for FromPdfPage)
        Prepend  - draw UNDER the existing content (background / watermark)
        Replace  - discard the existing content

    var document = PdfReader.Open("in.pdf", PdfDocumentOpenMode.Modify);
    foreach (PdfPage page in document.Pages)
    {
        using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Prepend);
        gfx.DrawRectangle(XBrushes.LightYellow, 0, 0, page.Width, 40);   // header band under content
    }
    document.Save("out.pdf");

You can add to a page, not edit what is already there: existing text cannot be
changed or re-flowed. Drawing at page-unit coordinates ignores page.Rotate;
account for rotated pages yourself when placing content.

MERGING, SPLITTING AND REORDERING PAGES
---------------------------------------

    public sealed class PdfPages : PdfDictionary, IEnumerable<PdfPage>
        public int Count
        public PdfPage this[int index]                     // 0-based
        public PdfPage Add()  /  Add(PdfPage page, AnnotationCopyingType annotationCopying = ShallowCopy)
        public PdfPage Insert(int index)  /  Insert(int index, PdfPage page, AnnotationCopyingType annotationCopying = ShallowCopy)
        public void InsertRange(int index, PdfDocument document, AnnotationCopyingType annotationCopying = ShallowCopy)
        public void InsertRange(int index, PdfDocument document, int startIndex, int pageCount, AnnotationCopyingType annotationCopying = ShallowCopy)
        public void Remove(PdfPage page)
        public void RemoveAt(int index)
        public void MovePage(int oldIndex, int newIndex)

    AnnotationCopyingType: DoNotCopy, ShallowCopy (default), DeepCopy

Pages copied from another document must come from a document opened in Import
mode. The page is copied (with its resources) into the target.

    // Merge
    var output = new PdfDocument();
    foreach (var path in new[] { "doc1.pdf", "doc2.pdf", "doc3.pdf" })
    {
        using var fs = File.OpenRead(path);
        var input = PdfReader.Open(fs, PdfDocumentOpenMode.Import);
        for (var i = 0; i < input.PageCount; i++)
            output.AddPage(input.Pages[i]);
    }
    output.Save("merged.pdf");

    // Copy a page range in one call (pages 3-5 of source, inserted at the front)
    output.Pages.InsertRange(0, input, startIndex: 2, pageCount: 3);

    // Split: one file per page
    var source = PdfReader.Open("big.pdf", PdfDocumentOpenMode.Import);
    for (int i = 0; i < source.PageCount; i++)
    {
        var single = new PdfDocument();
        single.AddPage(source.Pages[i]);
        single.Save($"page-{i + 1}.pdf");
    }

    // Reorder / delete in place
    var doc = PdfReader.Open("in.pdf", PdfDocumentOpenMode.Modify);
    doc.Pages.MovePage(0, doc.PageCount - 1);   // first page to the end
    doc.Pages.RemoveAt(1);
    doc.Save("out.pdf");

IMAGE CONSOLIDATION
-------------------
When merged pages carry the same image repeatedly (logos, letterheads), the
image data is duplicated once per page. Consolidate before saving:

    public void ConsolidateImages()        // on PdfDocument

    output.ConsolidateImages();            // dedupes identical image XObjects by content hash
    output.Save("optimized.pdf");

This can reduce file size by 75% or more when the same images appear on many
pages.

DOCUMENT OPTIONS (COMPRESSION, COLOR MODE)
------------------------------------------

    public sealed class PdfDocumentOptions              // document.Options
        public PdfColorMode ColorMode                   // Undefined | Rgb (default) | Cmyk
        public bool CompressContentStreams              // default true
        public bool NoCompression                       // default false; true = no Flate at all
        public PdfFlateEncodeMode FlateEncodeMode       // Default | BestSpeed | BestCompression
        public bool EnableCcittCompressionForBilevelImages   // default false
        public PdfUseFlateDecoderForJpegImages UseFlateDecoderForJpegImages // Automatic | Never (default) | Always

    document.Options.ColorMode = PdfColorMode.Cmyk;              // XColor.FromCmyk colours written as CMYK
    document.Options.FlateEncodeMode = PdfFlateEncodeMode.BestCompression;
    document.Options.NoCompression = true;                        // human-readable output for debugging

SECURITY AND ENCRYPTION
-----------------------

    public sealed class PdfSecuritySettings                // document.SecuritySettings
        public PdfDocumentSecurityLevel DocumentSecurityLevel  // None | Encrypted40Bit | Encrypted128Bit
        public string UserPassword  { set; }               // password to OPEN
        public string OwnerPassword { set; }               // password to change permissions
        public bool HasOwnerPermissions { get; }
        public bool PermitPrint, PermitFullQualityPrint, PermitModifyDocument,
                    PermitExtractContent, PermitAccessibilityExtractContent,
                    PermitAnnotations, PermitFormsFill, PermitAssembleDocument

Setting either password automatically raises DocumentSecurityLevel from None to
Encrypted128Bit. Saving with a security level but no password throws. Permission
flags are enforced by viewers for users who open with the USER password; the
owner password bypasses them.

    var document = new PdfDocument();
    // ... add pages and content ...
    document.SecuritySettings.UserPassword = "userPass";     // required to open
    document.SecuritySettings.OwnerPassword = "ownerPass";   // required to change permissions
    document.SecuritySettings.PermitPrint = true;
    document.SecuritySettings.PermitFullQualityPrint = false;
    document.SecuritySettings.PermitModifyDocument = false;
    document.SecuritySettings.PermitExtractContent = false;
    document.SecuritySettings.PermitAnnotations = false;
    document.SecuritySettings.PermitFormsFill = true;
    document.Save("protected.pdf");

    // Weaker RC4-40 for legacy readers
    document.SecuritySettings.DocumentSecurityLevel = PdfDocumentSecurityLevel.Encrypted40Bit;

Opening encrypted PDFs:

    var document = PdfReader.Open("protected.pdf", "password", PdfDocumentOpenMode.Modify);

Reading supports 40-bit and 128-bit RC4, AES (V4/R4) and 256-bit AES (V5/R5 and
V5/R6) encrypted files. Writing produces RC4 40-bit or 128-bit (the two
PdfDocumentSecurityLevel values). Opening a user-password-protected file
without the password throws.

DOCUMENT OUTLINES (BOOKMARKS)
-----------------------------

    public class PdfOutlineCollection : PdfObject, IList<PdfOutline>   // document.Outlines, outline.Outlines
        public PdfOutline Add(string title, PdfPage destinationPage)
        public PdfOutline Add(string title, PdfPage destinationPage, bool opened)
        public PdfOutline Add(string title, PdfPage destinationPage, bool opened, PdfOutlineStyle style)
        public PdfOutline Add(string title, PdfPage destinationPage, bool opened, PdfOutlineStyle style, XColor textColor)
        public int Count; public PdfOutline this[int index]; Remove / RemoveAt / Insert / Clear

    public sealed class PdfOutline : PdfDictionary
        public string Title; public PdfPage DestinationPage; public bool Opened
        public PdfOutlineStyle Style                 // Regular | Italic | Bold | BoldItalic
        public XColor TextColor
        public PdfPageDestinationType PageDestinationType  // Xyz | Fit | FitH | FitV | FitR | FitB | FitBH | FitBV
        public double Left, Top, Right, Bottom, Zoom  // used by Xyz / FitR destinations
        public PdfOutlineCollection Outlines         // children
        public PdfOutline Parent; public bool HasChildren

    var chapter = document.Outlines.Add("Chapter 1", document.Pages[0], true,
        PdfOutlineStyle.Bold, XColors.DarkBlue);
    chapter.Outlines.Add("Section 1.1", document.Pages[1]);
    var sec = chapter.Outlines.Add("Section 1.2", document.Pages[2]);
    sec.PageDestinationType = PdfPageDestinationType.FitH;
    sec.Top = 700;                                    // PDF units, y from the bottom

    document.PageMode = PdfPageMode.UseOutlines;      // open the bookmarks panel

LINKS AND ANNOTATIONS
---------------------
Annotation rectangles are in PDF DEFAULT PAGE SPACE (points, y from the
bottom), not XGraphics space. Convert an XRect with the graphics object's
Transformer:

    XRect world = new XRect(50, 50, 200, 20);                     // XGraphics coordinates
    PdfRectangle rect = new PdfRectangle(gfx.Transformer.WorldToDefaultPage(world));

    public sealed class PdfRectangle : PdfItem
        public PdfRectangle(XRect rect)  /  PdfRectangle(XPoint pt1, XPoint pt2)  /  PdfRectangle(XPoint pt, XSize size)
        public double X1, Y1, X2, Y2, Width, Height; public XRect ToXRect()

Links (page helpers create and attach the annotation for you):

    page.AddWebLink(rect, "https://github.com/ellisnet/CodeBrix.PdfDocuments");
    page.AddDocumentLink(rect, destinationPage: 3);      // 1-based page number in this document
    page.AddFileLink(rect, "attachment.pdf");

    public sealed class PdfLinkAnnotation : PdfAnnotation    // same three as statics:
        public static PdfLinkAnnotation CreateWebLink(PdfRectangle rect, string url)
        public static PdfLinkAnnotation CreateDocumentLink(PdfRectangle rect, int destinationPage)
        public static PdfLinkAnnotation CreateFileLink(PdfRectangle rect, string fileName)

Sticky notes and stamps:

    public abstract class PdfAnnotation : PdfDictionary
        public PdfRectangle Rectangle; public string Title, Subject, Contents
        public XColor Color; public double Opacity
        public PdfAnnotationFlags Flags       // Invisible, Hidden, Print, NoZoom, NoRotate,
                                              // NoView, ReadOnly, Locked, ToggleNoView
    public sealed class PdfTextAnnotation : PdfAnnotation
        public PdfTextAnnotation()
        public bool Open
        public PdfTextAnnotationIcon Icon     // NoIcon, Comment, Help, Insert, Key,
                                              // NewParagraph, Note, Paragraph
    public sealed class PdfRubberStampAnnotation : PdfAnnotation
        public PdfRubberStampAnnotationIcon Icon   // Approved, AsIs, Confidential, Departmental,
                                              // Draft, Experimental, Expired, Final, ForComment,
                                              // ForPublicRelease, NotApproved,
                                              // NotForPublicRelease, Sold, TopSecret, NoIcon
    public sealed class PdfAnnotations : PdfArray            // page.Annotations
        public void Add(PdfAnnotation annotation); Remove; Clear; Count; this[int]

    var note = new PdfTextAnnotation
    {
        Title = "Reviewer",
        Subject = "Question",
        Contents = "Is this figure up to date?",
        Icon = PdfTextAnnotationIcon.Note,
        Open = false,
        Color = XColors.Yellow,
    };
    note.Rectangle = new PdfRectangle(gfx.Transformer.WorldToDefaultPage(new XRect(500, 40, 20, 20)));
    page.Annotations.Add(note);

    var stamp = new PdfRubberStampAnnotation { Icon = PdfRubberStampAnnotationIcon.Draft, Opacity = 0.6 };
    stamp.Rectangle = new PdfRectangle(gfx.Transformer.WorldToDefaultPage(new XRect(400, 700, 150, 50)));
    page.Annotations.Add(stamp);

VIEWER PREFERENCES, PAGE LAYOUT AND PAGE MODE
---------------------------------------------

    public sealed class PdfViewerPreferences : PdfDictionary      // document.ViewerPreferences
        public bool HideToolbar, HideMenubar, HideWindowUI, FitWindow, CenterWindow, DisplayDocTitle
        public PdfReadingDirection? Direction                      // LeftToRight | RightToLeft

    PdfPageLayout: SinglePage, OneColumn, TwoColumnLeft, TwoColumnRight, TwoPageLeft, TwoPageRight
    PdfPageMode:   UseNone, UseOutlines, UseThumbs, FullScreen, UseOC, UseAttachments

    document.PageLayout = PdfPageLayout.TwoPageRight;
    document.PageMode = PdfPageMode.UseOutlines;
    document.ViewerPreferences.DisplayDocTitle = true;
    document.ViewerPreferences.FitWindow = true;
    document.Language = "en-US";

CUSTOM VALUES (PRIVATE APPLICATION DATA)
----------------------------------------
Store your own bytes in the document or in a page. They travel with the file
and come back when you reopen it.

    public class PdfCustomValues : PdfDictionary            // document.CustomValues / page.CustomValues
        public PdfCustomValue this[string key] { get; set; } // null get = absent; null set = remove
        public bool Contains(string key)
        public PdfCustomValueCompressionMode CompressionMode  // Default | Uncompressed | Compressed
        public static void ClearAllCustomValues(PdfDocument document)
    public class PdfCustomValue : PdfDictionary
        public PdfCustomValue(byte[] bytes)
        public byte[] Value
        public PdfCustomValueCompressionMode CompressionMode

    document.CustomValues["/MyAppState"] = new PdfCustomValue(Encoding.UTF8.GetBytes(json));
    ...
    var stored = document.CustomValues["/MyAppState"];
    if (stored != null) json = Encoding.UTF8.GetString(stored.Value);

Keys are PDF names and start with "/". The values live under the dictionary
key named by document.Internals.CustomValueKey (default
"/CodeBrix.PdfDocuments.CustomValue"); assign that property before reading
files written with a different key.

ACROFORMS (FILLING FORM FIELDS)
-------------------------------
Existing fillable forms CAN be filled and flattened to read-only through the
API. (Creating new form fields from scratch is not supported - see WHAT THIS
PACKAGE DOES NOT DO.)

    public sealed class PdfAcroForm : PdfDictionary            // document.AcroForm (null if no form)
        public PdfAcroField.PdfAcroFieldCollection Fields
    public sealed class PdfAcroFieldCollection : PdfArray
        public string[] Names; public string[] DescendantNames
        public PdfAcroField this[string name]; public PdfAcroField this[int index]
    public abstract class PdfAcroField : PdfDictionary
        public string Name; public PdfAcroFieldFlags Flags; public bool ReadOnly
        public virtual PdfItem Value
        public bool HasKids; public PdfAcroFieldCollection Fields   // child fields
        public PdfAcroField this[string name]
        public string[] GetDescendantNames(); public string[] GetAppearanceNames()
    public sealed class PdfTextField : PdfAcroField
        public string Text                    // setter also regenerates the appearance
        public XFont Font; public XColor ForeColor, BackColor
        public int MaxLength; public bool MultiLine, Password
    public sealed class PdfCheckBoxField : PdfButtonField
        public bool Checked; public string CheckedName, UncheckedName
    public sealed class PdfRadioButtonField : PdfButtonField
        public int SelectedIndex
    public sealed class PdfComboBoxField : PdfChoiceField
        public int SelectedIndex; public override PdfItem Value
    public sealed class PdfListBoxField : PdfChoiceField
        public int SelectedIndex
    public sealed class PdfPushButtonField, PdfSignatureField, PdfGenericField

    PdfDocument:
        public void MakeAcroFormsReadOnly()   // flatten: sets every field read-only

    var document = PdfReader.Open("form.pdf", PdfDocumentOpenMode.Modify);
    var fields = document.AcroForm.Fields;
    foreach (var name in fields.Names) Console.WriteLine(name);

    if (fields["FullName"] is PdfTextField fullName) fullName.Text = "Ada Lovelace";
    if (fields["Agree"] is PdfCheckBoxField agree) agree.Checked = true;
    if (fields["Country"] is PdfComboBoxField country) country.SelectedIndex = 2;

    document.MakeAcroFormsReadOnly();          // optional: lock the filled values
    document.Save("form-filled.pdf");

ACTIONS
-------
CodeBrix.PdfDocuments.Pdf.Actions holds the action dictionaries used by
outlines and links: PdfAction (abstract), PdfGoToAction and the
PdfNamedActionNames enum. You normally do not construct these yourself - use
Outlines.Add and page.AddDocumentLink/AddWebLink, which build them for you.

THE LOW-LEVEL OBJECT MODEL (Pdf and Pdf.Advanced)
-------------------------------------------------
Every typed class above is a thin wrapper over PDF objects. When the typed
API has no property for something, read or write the dictionary directly.
Keys are PDF names with a leading slash ("/Title").

    public class PdfDictionary : PdfObject
        public DictionaryElements Elements
        public PdfStream Stream                      // null unless the object has a stream
        public PdfStream CreateStream(byte[] value)
    public sealed class DictionaryElements
        public bool ContainsKey(string key); public ICollection<string> Keys; public PdfName[] KeyNames
        public bool GetBoolean(string key);  public void SetBoolean(string key, bool value)
        public int GetInteger(string key);   public void SetInteger(string key, int value)
        public double GetReal(string key);   public void SetReal(string key, double value)
        public string GetString(string key); public void SetString(string key, string value)
        public string GetName(string key);   public void SetName(string key, string value)
        public PdfRectangle GetRectangle(string key); public void SetRectangle(string key, PdfRectangle rect)
        public XMatrix GetMatrix(string key); public void SetMatrix(string key, XMatrix matrix)
        public DateTime GetDateTime(string key, DateTime defaultValue); public void SetDateTime(string key, DateTime value)
        public PdfDictionary GetDictionary(string key); public PdfArray GetArray(string key)
        public PdfObject GetObject(string key);       public PdfReference GetReference(string key)
        public PdfItem GetValue(string key);          public void SetValue(string key, PdfItem value)
        public void SetObject(string key, PdfObject obj); public void SetReference(string key, PdfObject obj)
        public PdfItem this[string key] { get; set; }
        public bool Remove(string key); public void Add(string key, PdfItem value); public int Count
    public sealed class PdfStream
        public byte[] Value                          // raw (filtered) bytes
        public byte[] UnfilteredValue                // decoded bytes (Flate etc. removed)
        public int Length; public bool TryUnfilter(); public void Zip()

    public class PdfArray : PdfObject, IEnumerable<PdfItem>
        public ArrayElements Elements                // Count, this[int], Add, Insert, RemoveAt,
                                                     // GetInteger/GetReal/GetString/GetName/
                                                     // GetDictionary/GetArray/GetObject/GetReference(int index)
    Primitive items (CodeBrix.PdfDocuments.Pdf):
        PdfName(string value) .Value      PdfString(string value) .Value
        PdfInteger(int value) .Value      PdfReal(double value) .Value
        PdfBoolean(bool value) .Value     PdfLong, PdfUInteger, PdfNull, PdfLiteral, PdfDate
        PdfRectangle
    Indirect objects:
        public abstract class PdfObject : PdfItem   { PdfDocument Owner; bool IsIndirect; PdfReference Reference; PdfObjectInternals Internals }
        public sealed class PdfReference : PdfItem  { PdfObjectID ObjectID; int ObjectNumber, GenerationNumber; PdfObject Value }

    public class PdfInternals                            // document.Internals
        public PdfCatalog Catalog
        public PdfObject GetObject(PdfObjectID objectID)
        public PdfObject[] GetAllObjects()
        public T CreateIndirectObject<T>() where T : PdfObject
        public void AddObject(PdfObject obj); public void RemoveObject(PdfObject obj)
        public PdfObject[] GetClosure(PdfObject obj)
        public void WriteObject(Stream stream, PdfItem item)   // debugging aid
        public string FirstDocumentID, SecondDocumentID
        public string CustomValueKey

    Other Pdf.Advanced types you may meet while walking a document: PdfCatalog,
    PdfResources (AddFont/AddImage/AddForm/AddExtGState/AddPattern/AddShading),
    PdfContents / PdfContent (page content streams), PdfImage (.Image),
    PdfFormXObject (.Resources), PdfFont / PdfFontDescriptor, PdfExtGState,
    PdfShading / PdfShadingPattern / PdfTilingPattern, PdfSoftMask,
    PdfTransparencyGroupAttributes, PdfEmbeddedFile / PdfFileSpecification,
    PdfObjectStream, PdfObjectID.

    // Walk the images referenced by a page
    var xobjects = page.Resources.Elements.GetDictionary("/XObject");
    if (xobjects != null)
        foreach (var key in xobjects.Elements.Keys)
        {
            var xo = xobjects.Elements.GetDictionary(key);
            if (xo?.Elements.GetName("/Subtype") == "/Image")
                Console.WriteLine($"{key}: {xo.Elements.GetInteger("/Width")} x {xo.Elements.GetInteger("/Height")}");
        }

    // Add a non-standard entry to the Info dictionary
    document.Info.Elements.SetString("/Department", "Finance");

CONTENT STREAMS (Pdf.Content)
-----------------------------
Parse a page's content stream into operators, inspect or transform it, and
write it back:

    public static class ContentReader
        public static CSequence ReadContent(PdfPage page)
        public static CSequence ReadContent(byte[] content)
        public static CSequence ReadContent(MemoryStream content)
    public class CSequence : CObject, IList<CObject>
        public byte[] ToContent()                    // serialize back to bytes
    public class COperator : CObject   { string Name; CSequence Operands; OpCode OpCode }
    public sealed class OpCode        { string Name; OpCodeName OpCodeName; int Operands; string Description }
    Operand objects: CInteger (.Value), CReal (.Value), CString (.Value, .CStringType),
                     CName (.Name), CArray, CComment (.Text)
    public sealed class PdfContents : PdfArray         // page.Contents
        public PdfContent AppendContent(); PrependContent(); CreateSingleContent()
        public PdfContent ReplaceContent(CSequence cseq)

    var content = ContentReader.ReadContent(page);
    foreach (CObject item in content)
        if (item is COperator op && op.OpCode.OpCodeName == OpCodeName.Tj)
            Console.WriteLine("text-show operator with " + op.Operands.Count + " operand(s)");
    page.Contents.ReplaceContent(content);        // after modifying the sequence

The parser understands the full operator set (OpCodeName enum, OpCodes table).
Note that Tj/TJ operands are font-encoded bytes, not readable strings - this is
why the package offers no text extraction.

FILTERS (Pdf.Filters)
---------------------

    public static class Filtering
        public static Filter GetFilter(string filterName)    // "/FlateDecode", "FlateDecode", "Fl" ...
        public static byte[] Encode(byte[] data, string filterName)
        public static byte[] Decode(byte[] data, string filterName)
        public static byte[] Decode(byte[] data, string filterName, FilterParms parms)
        public static string DecodeToString(byte[] data, string filterName)
        public static FlateDecode FlateDecode; public static LzwDecode LzwDecode
        public static AsciiHexDecode ASCIIHexDecode; public static Ascii85Decode ASCII85Decode
    public abstract class Filter { byte[] Encode(byte[] data); byte[] Decode(byte[] data, FilterParms parms) }
    public class FlateDecode : Filter { public byte[] Encode(byte[] data, PdfFlateEncodeMode mode) }

Implemented: FlateDecode, LZWDecode, ASCIIHexDecode, ASCII85Decode (plus the
PNG predictors used by Flate streams). DCTDecode (JPEG), CCITTFaxDecode,
JBIG2Decode, JPXDecode and RunLengthDecode are recognised by name but not
decoded - PdfStream.UnfilteredValue returns the still-encoded bytes for them.

    byte[] raw = someObject.Stream.UnfilteredValue;                // usual route
    byte[] decoded = Filtering.Decode(someObject.Stream.Value, "/FlateDecode");

CHARTING
--------
CodeBrix.PdfDocuments.Charting draws simple charts directly with XGraphics -
no dependency on the PdfDocCreate package.

    public class Chart : DocumentObject
        public Chart(ChartType type)
        public ChartType Type            // Line, Column2D, ColumnStacked2D, Area2D, Bar2D,
                                         // BarStacked2D, Pie2D, PieExploded2D
        public SeriesCollection SeriesCollection   // .AddSeries()
        public XValues XValues                     // .AddXSeries()
        public Axis XAxis, YAxis, ZAxis
        public Legend Legend                       // .Docking (Top|Bottom|Left|Right), .Font, .LineFormat
        public PlotArea PlotArea                   // .LineFormat, .FillFormat, padding
        public Font Font
        public DataLabel DataLabel; public bool HasDataLabel
        public BlankType DisplayBlanksAs           // NotPlotted | Interpolated | Zero
    public class Series : ChartObject
        public string Name
        public Point Add(double value); public void Add(params double[] values); public void AddBlank()
        public LineFormat LineFormat; public FillFormat FillFormat
        public MarkerStyle MarkerStyle; public XUnit MarkerSize
        public XColor MarkerForegroundColor, MarkerBackgroundColor
        public ChartType ChartType               // per-series override (mixed charts)
        public DataLabel DataLabel; public bool HasDataLabel
    public class XSeries : ChartObject
        public XValue Add(string value); public void Add(params string[] values); public void AddBlank()
    public class Axis : ChartObject
        public AxisTitle Title                   // .Caption, .Font, .Orientation, .Alignment
        public double MinimumScale, MaximumScale, MajorTick, MinorTick
        public TickMarkType MajorTickMark, MinorTickMark   // None | Inside | Outside | Cross
        public TickLabels TickLabels             // .Format, .Font
        public LineFormat LineFormat
        public Gridlines MajorGridlines, MinorGridlines; public bool HasMajorGridlines, HasMinorGridlines
    public class DataLabel : DocumentObject      { string Format; Font Font; DataLabelPosition Position; DataLabelType Type }
    public class LineFormat : DocumentObject     { bool Visible; XUnit Width; XColor Color; XDashStyle DashStyle }
    public class FillFormat : DocumentObject     { bool Visible; XColor Color }
    public sealed class Font : DocumentObject    { Font(string name, XUnit size); Name; Size; Bold; Italic; Color ... }
    public class ChartFrame
        public ChartFrame(XRect rect)
        public XPoint Location; public XSize Size
        public void Add(Chart chart)
        public void Draw(XGraphics gfx)

    using CodeBrix.PdfDocuments.Charting;

    var chart = new Chart(ChartType.Column2D);
    var sales = chart.SeriesCollection.AddSeries();
    sales.Name = "Sales";
    sales.Add(12, 18, 9, 21);
    sales.FillFormat.Color = XColors.SteelBlue;
    chart.XValues.AddXSeries().Add("Q1", "Q2", "Q3", "Q4");
    chart.XAxis.Title.Caption = "Quarter";
    chart.YAxis.MajorTickMark = TickMarkType.Outside;
    chart.YAxis.HasMajorGridlines = true;
    chart.Legend.Docking = DockingType.Bottom;
    chart.DataLabel.Type = DataLabelType.Value;
    chart.HasDataLabel = true;

    var frame = new ChartFrame(new XRect(50, 100, 450, 260));
    frame.Add(chart);
    frame.Draw(gfx);

Charting has its own Font, Point, LineFormat and DocumentObject types; when a
file also uses the PdfDocCreate document model, alias the namespace
(using Charting = CodeBrix.PdfDocuments.Charting;) to avoid ambiguity.

BAR CODES
---------

    public abstract class BarCode : CodeBase
        public static BarCode FromType(CodeType type, string text, XSize size, CodeDirection direction)
        public static BarCode FromType(CodeType type, string text, XSize size)
        public string Text; public XSize Size
        public CodeDirection Direction   // LeftToRight | BottomToTop | RightToLeft | TopToBottom
        public AnchorType Anchor         // TopLeft (default) ... BottomRight: which corner 'position' means
        public TextLocation TextLocation // None | Above | Below | AboveEmbedded | BelowEmbedded
        public virtual double WideNarrowRatio; public bool TurboBit
    public class Code3of9Standard : ThickThinBarCode        // Code 39
        public Code3of9Standard(string code)  /  (string code, XSize size)  /  (string code, XSize size, CodeDirection direction)
    public class Code2of5Interleaved : ThickThinBarCode     // Interleaved 2 of 5 (even digit count)
        public Code2of5Interleaved(string code)  /  (string code, XSize size)  /  (string code, XSize size, CodeDirection direction)
    public class CodeOmr : BarCode                         // optical mark recognition marks
    public class CodeDataMatrix : MatrixCode
        public CodeDataMatrix(string code, int length)                  // square, length x length modules
        public CodeDataMatrix(string code, int length, XSize size)
        public CodeDataMatrix(string code, int rows, int columns, XSize size)
        public CodeDataMatrix(string code, DataMatrixEncoding dmEncoding, int rows, int columns, XSize size)
        public int QuietZone; public void SetEncoding(DataMatrixEncoding dmEncoding)
    CodeType: Code2of5Interleaved, Code3of9Standard, Omr, DataMatrix
    DataMatrixEncoding: Ascii, C40, Text, X12, EDIFACT, Base256

    XGraphics:
        public void DrawBarCode(BarCodes.BarCode barcode, XPoint position)
        public void DrawBarCode(BarCodes.BarCode barcode, XBrush brush, XPoint position)
        public void DrawBarCode(BarCodes.BarCode barcode, XBrush brush, XFont font, XPoint position)  // font for the human-readable text
        public void DrawMatrixCode(BarCodes.MatrixCode matrixcode, XPoint position)
        public void DrawMatrixCode(BarCodes.MatrixCode matrixcode, XBrush brush, XPoint position)

    using CodeBrix.PdfDocuments.Drawing.BarCodes;

    var code39 = new Code3of9Standard("CB-2026-0042", new XSize(180, 40));
    code39.TextLocation = TextLocation.BelowEmbedded;
    gfx.DrawBarCode(code39, XBrushes.Black, new XFont("Arial", 8), new XPoint(50, 600));

    var i2of5 = new Code2of5Interleaved("12345678", new XSize(150, 40));
    gfx.DrawBarCode(i2of5, XBrushes.Black, new XPoint(300, 600));

    var dm = new CodeDataMatrix("https://example.com/item/42", 26, new XSize(80, 80));
    gfx.DrawMatrixCode(dm, XBrushes.Black, new XPoint(50, 660));

SAVING
------

    document.Save("output.pdf");                    // file (overwrites)

    using var ms = new MemoryStream();
    document.Save(ms);                              // stream stays OPEN; Position reset to 0
    byte[] pdfBytes = ms.ToArray();

    document.Save(stream, closeStream: true);       // dispose the stream after writing

    // Construct-with-target form: written when Close() is called
    var doc = new PdfDocument("output.pdf");
    ...
    doc.Close();

A document opened in Import mode cannot be saved (InvalidOperationException).
A document with a security level but no password cannot be saved
(PdfDocumentsException). Save can be called more than once. Dispose the
document when done; disposal does not save.

================================================================================

COMPLETE EXAMPLES
=================

Example 1: Simple PDF with Text and Image
-----------------------------------------
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;

    var document = new PdfDocument();
    document.Info.Title = "My Document";

    var page = document.AddPage();
    using (var gfx = XGraphics.FromPdfPage(page))
    {
        gfx.DrawString("PDF with Image", new XFont("Arial", 16), XBrushes.Black,
            new XPoint(12, 24));
        gfx.DrawImage(XImage.FromFile("photo.png"), new XPoint(12, 50));
    }

    document.Save("ImageDocument.pdf");

Example 2: Merge Multiple PDFs and Consolidate Images
-----------------------------------------------------
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

    output.ConsolidateImages();       // dedupe repeated logos/letterheads
    output.Save("merged.pdf");

Example 3: PDF with an Image Processed by CodeBrix.Imaging
----------------------------------------------------------
    using CodeBrix.Imaging;
    using CodeBrix.Imaging.PixelFormats;
    using CodeBrix.Imaging.Processing;
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfDocuments.Utils;

    using var image = Image.Load<Rgb24>("photo.jpg", out var format);
    image.Mutate(x => x.Grayscale().Resize(400, 300));

    var document = new PdfDocument();
    var page = document.AddPage();
    using (var gfx = XGraphics.FromPdfPage(page))
    {
        gfx.DrawString("Processed Image:", new XFont("Arial", 14), XBrushes.Black,
            new XPoint(50, 30));

        var source = ImagingImageSource<Rgb24>.FromImagingImage(image, format);
        using var xImage = XImage.FromImageSource(source);
        gfx.DrawImage(xImage, 50, 50, 400, 300);
    }

    document.Save("processed-image.pdf");

Example 4: Watermark Every Page of an Existing PDF
--------------------------------------------------
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfDocuments.Pdf.IO;

    var document = PdfReader.Open("contract.pdf", PdfDocumentOpenMode.Modify);
    var font = new XFont("Arial", 72, XFontStyle.Bold);
    var brush = new XSolidBrush(XColor.FromArgb(48, 200, 0, 0));   // translucent red

    foreach (PdfPage page in document.Pages)
    {
        using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
        var state = gfx.Save();
        gfx.RotateAtTransform(-40, new XPoint(page.Width / 2, page.Height / 2));
        gfx.DrawString("CONFIDENTIAL", font, brush,
            new XRect(0, 0, page.Width, page.Height), XStringFormats.Center);
        gfx.Restore(state);
    }

    document.Save("contract-watermarked.pdf");

To overlay a PDF page (a letterhead or a stamp designed in another tool)
instead of text, load it with XPdfForm.FromFile("stamp.pdf") and draw it with
gfx.DrawImage(form, rect) inside the same loop.

Example 5: In-Memory PDF for a Web API
--------------------------------------
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;

    public static byte[] BuildInvoicePdf(string customer)
    {
        var document = new PdfDocument();
        document.Info.Title = $"Invoice for {customer}";
        var page = document.AddPage();
        using (var gfx = XGraphics.FromPdfPage(page))
        {
            gfx.DrawString($"Invoice - {customer}", new XFont("Arial", 20, XFontStyle.Bold),
                XBrushes.Black, new XRect(40, 40, page.Width - 80, 30), XStringFormats.TopLeft);
        }

        using var ms = new MemoryStream();
        document.Save(ms);              // stream is left open, positioned at 0
        return ms.ToArray();
    }

    // ASP.NET Core: return File(BuildInvoicePdf(name), "application/pdf", "invoice.pdf");

Example 6: Password-Protected PDF with Restricted Permissions
-------------------------------------------------------------
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfDocuments.Pdf.IO;

    var document = new PdfDocument();
    var page = document.AddPage();
    using (var gfx = XGraphics.FromPdfPage(page))
        gfx.DrawString("For the addressee only", new XFont("Arial", 14), XBrushes.Black, 50, 50);

    var s = document.SecuritySettings;
    s.UserPassword = "open-me";            // needed to open
    s.OwnerPassword = "owner-only";        // needed to change permissions
    s.PermitPrint = true;
    s.PermitFullQualityPrint = true;
    s.PermitModifyDocument = false;
    s.PermitExtractContent = false;
    s.PermitAnnotations = false;
    s.PermitFormsFill = false;
    s.PermitAssembleDocument = false;
    document.Save("restricted.pdf");

    // Reopen with the user password
    var reopened = PdfReader.Open("restricted.pdf", "open-me", PdfDocumentOpenMode.ReadOnly);
    Console.WriteLine(reopened.PageCount);

Example 7: Fill an Existing Form and Flatten It
-----------------------------------------------
    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfDocuments.Pdf.AcroForms;
    using CodeBrix.PdfDocuments.Pdf.IO;

    var document = PdfReader.Open("application-form.pdf", PdfDocumentOpenMode.Modify);
    var fields = document.AcroForm?.Fields
        ?? throw new InvalidOperationException("The PDF has no form fields.");

    foreach (var name in fields.Names)
        Console.WriteLine($"{name}: {fields[name].GetType().Name}");

    if (fields["Applicant.Name"] is PdfTextField nameField) nameField.Text = "Ada Lovelace";
    if (fields["Applicant.Email"] is PdfTextField email) email.Text = "ada@example.com";
    if (fields["Consent"] is PdfCheckBoxField consent) consent.Checked = true;

    document.MakeAcroFormsReadOnly();
    document.Save("application-form-filled.pdf");

Example 8: Report Page with a Chart, a Bar Code, Links and a Bookmark
---------------------------------------------------------------------
    using CodeBrix.PdfDocuments.Charting;
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Drawing.BarCodes;
    using CodeBrix.PdfDocuments.Pdf;

    var document = new PdfDocument();
    var page = document.AddPage();
    using (var gfx = XGraphics.FromPdfPage(page))
    {
        var title = new XFont("Arial", 18, XFontStyle.Bold);
        gfx.DrawString("Quarterly Sales", title, XBrushes.Black,
            new XRect(40, 30, page.Width - 80, 30), XStringFormats.TopLeft);

        var chart = new Chart(ChartType.Column2D);
        var series = chart.SeriesCollection.AddSeries();
        series.Name = "Units";
        series.Add(120, 180, 95, 210);
        chart.XValues.AddXSeries().Add("Q1", "Q2", "Q3", "Q4");
        chart.YAxis.HasMajorGridlines = true;
        chart.Legend.Docking = DockingType.Bottom;
        var frame = new ChartFrame(new XRect(40, 80, page.Width - 80, 260));
        frame.Add(chart);
        frame.Draw(gfx);

        var code = new Code3of9Standard("RPT-2026-Q4", new XSize(170, 36))
            { TextLocation = TextLocation.BelowEmbedded };
        gfx.DrawBarCode(code, XBrushes.Black, new XFont("Arial", 8), new XPoint(40, 380));

        var linkArea = new XRect(40, 440, 260, 16);
        gfx.DrawString("Source data (opens in browser)", new XFont("Arial", 11),
            XBrushes.Blue, linkArea, XStringFormats.CenterLeft);
        page.AddWebLink(new PdfRectangle(gfx.Transformer.WorldToDefaultPage(linkArea)),
            "https://example.com/sales/q4");
    }

    document.Outlines.Add("Quarterly Sales", page, true, PdfOutlineStyle.Bold);
    document.PageMode = PdfPageMode.UseOutlines;
    document.Save("report.pdf");

Example 9: Render a PdfDocument to an Image (needs CodeBrix.PdfRasterizer)
--------------------------------------------------------------------------
A PdfDocument built with this package can be handed straight to the
CodeBrix.PdfRasterizer package without saving it first. Add
CodeBrix.PdfRasterizer.MitLicenseForever and see its AGENT-README for the
full API:

    using CodeBrix.Imaging;
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;
    using CodeBrix.PdfRasterizer;

    var document = new PdfDocument();
    var page = document.AddPage();
    using (var gfx = XGraphics.FromPdfPage(page))
        gfx.DrawString("Hello!", new XFont("Arial", 24), XBrushes.Black, new XPoint(50, 50));

    using var rasterizer = new PageRasterizer();
    using var image = await rasterizer.RasterizeToImage(document, pageNumber: 1);
    await image.SaveAsync("rendered.png");

================================================================================

MINIMUM VIABLE PROJECT
======================

    dotnet new console -n MyPdfApp --framework net10.0
    cd MyPdfApp
    dotnet add package CodeBrix.PdfDocuments.MitLicenseForever

MyPdfApp.csproj (as generated, plus the package):

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.PdfDocuments.MitLicenseForever" />
      </ItemGroup>
    </Project>

Program.cs:

    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Pdf;

    var doc = new PdfDocument();
    doc.Info.Title = "Hello";
    var page = doc.AddPage();
    using (var gfx = XGraphics.FromPdfPage(page))
    {
        gfx.DrawString("Hello, PDF!", new XFont("Arial", 24), XBrushes.Black,
            new XPoint(50, 50));
    }
    doc.Save("output.pdf");
    Console.WriteLine("Created output.pdf");

Build and run:

    dotnet build
    dotnet run

================================================================================

PERFORMANCE TIPS
================

1. USE PdfDocuments FOR CUSTOM GRAPHICS, PdfDocCreate FOR FLOWING DOCUMENTS.
   When you need precise positioning (labels, forms, overlays) draw with
   XGraphics. When you need headings, paragraphs and tables that paginate
   themselves, use the CodeBrix.PdfDocCreate package instead of hand-rolling
   a layout engine on top of DrawString.

2. CONSOLIDATE IMAGES WHEN MERGING: after merging PDFs that share images, call
   document.ConsolidateImages() before Save() to deduplicate identical image
   data - 75%+ smaller files are typical when a logo appears on every page.

3. USE STREAMS FOR WEB SCENARIOS: Save to a MemoryStream and return the bytes
   instead of writing temp files. Save(Stream) leaves the stream open at
   position 0, ready to read.

4. REUSE FONTS: create XFont objects once and reuse them. Do not construct a
   new XFont for every DrawString call with the same family/size/style.

5. USE XTextFormatter FOR LONG TEXT: for multi-line text that needs automatic
   wrapping, use XTextFormatter instead of measuring and breaking lines by
   hand with MeasureString.

6. PROCESS IMAGES WITH CodeBrix.Imaging FIRST: resize large photos to the size
   they will be drawn at before embedding them. The PDF stores the full
   pixel data whatever size you draw it.

7. DRAW REPEATED CONTENT THROUGH AN XForm: a logo, frame or stamp drawn on
   every page directly is stored once per page; drawn through an XForm it is
   stored once per document.

8. REUSE ONE XPdfForm PER SOURCE FILE: XPdfForm.FromFile parses the source
   PDF. Load it once, change PageNumber as you go, and draw it as many times
   as needed instead of reloading per page.

9. ONE XGraphics PER PAGE, DISPOSED PROMPTLY: obtain the graphics object,
   draw, dispose. Holding many open XGraphics objects keeps their renderers
   alive.

10. LEAVE COMPRESSION ON: the defaults (CompressContentStreams = true,
    NoCompression = false) produce the smallest files. Switch NoCompression on
    only to inspect output in a text editor.

11. PREFER Import MODE FOR SOURCES YOU ONLY COPY FROM: Import parses less than
    Modify and is what AddPage/InsertRange require anyway.

================================================================================

COMMON PITFALLS TO AVOID
========================

1. DO NOT confuse the NuGet package name with the namespace.
   - Package:   CodeBrix.PdfDocuments.MitLicenseForever
   - Namespace: CodeBrix.PdfDocuments.*
   ".MitLicenseForever" never appears in code.

2. DO NOT use PdfSharp or PdfSharpCore namespaces. This is a port; all
   namespaces are CodeBrix.PdfDocuments.*. And do not assume the upstream
   API from memory - XTextFormatter, image sources and font resolution
   differ here (see pitfalls 10-12).

3. DO NOT forget that XGraphics coordinates are in points by default (1 inch
   = 72 points), origin top-left, y downwards. Use XUnit.FromInch(),
   XUnit.FromMillimeter(), XUnit.FromCentimeter() for conversion, or obtain
   the graphics object with XGraphics.FromPdfPage(page, XGraphicsUnit.X).

4. DO NOT open a PDF for Import and then try to modify or save it. Use
   PdfDocumentOpenMode.Modify for editing; PdfDocumentOpenMode.Import for
   extracting pages to copy into another document. Saving an imported
   document throws InvalidOperationException.

5. DO NOT target .NET versions below 10.0.

6. DO NOT assume system fonts are available in Docker/CI environments, and
   DO NOT assume a font you asked for is the font you got. No IFontResolver
   registration is needed, but an unavailable family is silently substituted
   with the first installed font rather than raising an error - monospace
   families are the common casualty on Linux. Ship .ttf files as embedded
   resources and register an EmbeddedFontResolver. See FONTS AND FONT
   RESOLUTION.

7. DO NOT set GlobalFontSettings.FontResolver after any font has been used -
   the setter throws InvalidOperationException ("Must not change font
   resolver after it was once used"). Assign it, or register resolvers with
   MetaFontResolver.Instance, at application startup. Assigning null throws.

8. DO NOT register an EmbeddedFontResolver under the FAMILY name. Register it
   once per FACE name it serves ("Roboto-Regular", "Roboto-Bold", ...).
   MetaFontResolver routes ResolveTypeface by the resolver's DefaultFontName
   (the family) but routes GetFont by the registered face name; an
   unregistered face falls through to the system-font resolver and you get
   a substitute or "No Font File Found".

9. DO NOT assume generated PDFs are not text-searchable. This package has no
   text EXTRACTION API (see WHAT THIS PACKAGE DOES NOT DO), but that is a
   statement about reading PDFs, not about writing them. Text drawn with
   DrawString or XTextFormatter is embedded as real text with correct word
   spacing, so the output is searchable, selectable and accessible, and can
   be diffed as text in a regression suite.

10. DO NOT pass an XStringFormat to XTextFormatter.DrawString. Its fifth
    parameter is a line height (XUnit?) or a TextFormatAlignment; there is no
    XStringFormat overload and the call will not compile. Set
    Alignment/VerticalAlignment (or call SetAlignment) and then
    DrawString(text, font, brush, rect).

11. DO NOT pass a CodeBrix.Imaging Image to XImage.FromImageSource. It takes an
    IImageSource. Bridge with
    ImagingImageSource<TPixel>.FromImagingImage(image, format) (namespace
    CodeBrix.PdfDocuments.Utils), or use ImageSource.FromFile/FromBinary/
    FromStream. XImage.FromStream takes a Func<Stream>, not a Stream.

12. DO NOT look for a "PdfFontEmbedding" option. Fonts are always embedded as
    subsets; XPdfFontOptions controls only the encoding (WinAnsi or
    Unicode).

13. DO NOT forget that PageSize and PageOrientation live in the ROOT namespace
    (using CodeBrix.PdfDocuments;), not in CodeBrix.PdfDocuments.Pdf.

14. DO NOT use annotation or link rectangles in XGraphics coordinates. PDF
    annotations use default page space (points, y from the BOTTOM). Convert
    with new PdfRectangle(gfx.Transformer.WorldToDefaultPage(xrect)).

15. DO NOT set page.Rotate to anything but a multiple of 90 - it throws
    ArgumentException. And remember Rotate is a viewer instruction: your
    XGraphics drawing coordinates are not rotated with it.

16. DO NOT expect Save(Stream) to close the stream, and DO NOT expect
    Save(Stream, closeStream: true) to leave it usable. The one-argument form
    leaves the stream open and resets Position to 0; the two-argument form
    disposes it.

17. DO NOT set a DocumentSecurityLevel without a password. Saving throws
    "user or owner password required". Conversely, setting either password
    silently raises the level from None to Encrypted128Bit - that is the
    intended way to turn encryption on.

18. DO NOT draw on an XPdfForm or call DrawingFinished on it - it is an
    imported page and throws InvalidOperationException. Draw it onto your
    own page with DrawImage. Its PageNumber is 1-based.

19. DO NOT forget XGraphicsPdfPageOptions when adding to EXISTING pages.
    FromPdfPage(page) appends over existing content; use Prepend for a
    background/watermark under the content and Replace to discard it.

20. DO NOT rely on DCTDecode/CCITT/JBIG2/JPX streams being decoded by
    PdfStream.UnfilteredValue - only Flate, LZW, ASCIIHex and ASCII85 are
    decoded; the others come back still encoded.

21. DO NOT mix up the two DocumentObject families when you also use
    CodeBrix.PdfDocCreate: CodeBrix.PdfDocuments.Charting has its own Chart,
    Font, Point, LineFormat and DocumentObject types. Alias one namespace.

================================================================================

WHAT THIS PACKAGE DOES NOT DO
=============================

  - Extracting text content from PDFs (no text extraction, no OCR). The
    content stream can be parsed into operators (CONTENT STREAMS), but text
    operands are font-encoded bytes, not strings. NOTE: this is about READING
    PDFs; text you WRITE is real, searchable text - see pitfall 9.
  - Creating NEW form fields. Existing AcroForm fields can be read, filled
    (PdfTextField.Text, PdfCheckBoxField.Checked, ...) and flattened with
    MakeAcroFormsReadOnly(), but there is no API to add a field to a page.
  - Digital signatures (signing or verifying).
  - PDF/A or PDF/X compliance generation or validation.
  - Editing existing page content (you can add content over or under it,
    replace the whole content stream, or rebuild it from parsed operators,
    but not change a word in place).
  - Automatic pagination, text flow or tables - that is the
    CodeBrix.PdfDocCreate package.
  - Rendering PDF pages to images - that is the CodeBrix.PdfRasterizer
    package (which accepts a PdfDocument directly).
  - HTML/CSS or Markdown to PDF - CodeBrix.PdfDocCreate.Html2Pdf and
    CodeBrix.PdfDocCreate.Markdown2Pdf.
  - SVG placement (only raster images embed here).
  - Writing AES-encrypted files (RC4 40/128-bit only; AES files can be READ).
  - Reading or writing Word (.docx) or Excel (.xlsx) files.
  - PDF portfolios, JavaScript actions, multimedia annotations.

CodeBrix.PdfDocuments IS for: creating new PDFs; drawing text, images, shapes,
paths, charts and bar codes at exact coordinates; watermarking, overlaying
and imposing existing PDF pages; merging, splitting and reordering pages;
passwords and permissions; bookmarks, links and annotations; filling forms;
and raw PDF object access.

================================================================================

WORKING EXAMPLES ON GITHUB
==========================

The CodeBrix.PdfDocuments.Tests project contains compiling, passing examples
of this package's API. (The same test project also holds tests for the
PdfDocCreate and PdfRasterizer packages; those are cross-referenced from their
own AGENT-README files.)

    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests

Feature-to-test-file mapping:

  Creating simple PDFs (text, Unicode metadata, images from file, images via
  CodeBrix.Imaging + ImagingImageSource):
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/CreateSimplePDF.cs

  Merging PDFs and ConsolidateImages():
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Merge.cs

  XTextFormatter (wrapping, alignment, custom line height, image captions),
  including EmbeddedFontResolver + MetaFontResolver registration of an
  embedded Roboto face for OS-independent output:
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Drawing/Layout/XTextFormatterTest.cs

  ImagingImageSource / ImageSource for every supported image format and
  transparency handling:
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Utils/ImagingImageSourceTests.cs

  Security: creating 40-bit and 128-bit protected files, opening AES-encrypted
  files, password failures, reading files encrypted by several tools:
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Security/PdfSecurity.cs

  Reading PDFs from streams, invalid-file exceptions, TestPdfFile, content
  stream round-trip (ContentReader / CSequence.ToContent):
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/IO/PdfReader.cs
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/IO/IoBaseTest.cs
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/PdfReader.cs
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/CA2022CoverageTests.cs
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/IO/LargePDFReadWrite.cs

  Document outlines (bookmarks) with style and colour:
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Outlines/OutlineTests.cs

  Low-level PDF objects (PdfInteger, PdfInternals.CustomValueKey, content
  object names):
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/PdfInteger.cs
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Pdfs/PdfInternalsTests.cs
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Pdfs/Content/Objects/CNameTests.cs
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Pdfs/PdfDateTests.cs

HOW TO USE: fetch the raw file content from GitHub with a URL like
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/{path}
for example
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/tests/CodeBrix.PdfDocuments.Tests/Merge.cs

================================================================================

QUICK REFERENCE CARD
====================

--- Install ---
    dotnet add package CodeBrix.PdfDocuments.MitLicenseForever
Namespaces:     CodeBrix.PdfDocuments.Pdf / .Pdf.IO / .Drawing (+ root for PageSize)

--- Document ---
Create doc:     var document = new PdfDocument()
Add page:       var page = document.AddPage()
Page size:      page.Size = PageSize.Letter; page.Orientation = PageOrientation.Landscape
Custom size:    page.Width = XUnit.FromInch(8.5); page.Height = XUnit.FromMillimeter(200)
Rotate:         page.Rotate = 90                         // multiples of 90
Metadata:       document.Info.Title = "..."; .Author; .Subject; .Keywords; .Creator
Options:        document.Options.ColorMode / .CompressContentStreams / .NoCompression
Viewer:         document.PageLayout / .PageMode / .ViewerPreferences.FitWindow
Save file:      document.Save("file.pdf")
Save stream:    document.Save(ms)  // stream left open, Position = 0
Save+close:     document.Save(stream, closeStream: true)

--- Graphics ---
Get graphics:   using var gfx = XGraphics.FromPdfPage(page)
Units:          XGraphics.FromPdfPage(page, XGraphicsUnit.Millimeter)
On existing:    XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append | Prepend | Replace)
Font:           new XFont("Arial", 12, XFontStyle.Bold)
Draw text:      gfx.DrawString(text, font, brush, new XPoint(x, y))
Text in box:    gfx.DrawString(text, font, brush, rect, XStringFormats.Center)
Measure:        XSize s = gfx.MeasureString(text, font)
Wrap text:      new XTextFormatter(gfx).DrawString(text, font, brush, rect)
Align wrap:     tf.SetAlignment(new TextFormatAlignment { Horizontal = XParagraphAlignment.Justify })
Image file:     gfx.DrawImage(XImage.FromFile(path), x, y, w, h)
Image bytes:    XImage.FromStream(() => new MemoryStream(bytes))
Image (Imaging):XImage.FromImageSource(ImagingImageSource<Rgb24>.FromImagingImage(img, format))
PDF page:       var f = XPdfForm.FromFile("in.pdf"); f.PageNumber = 2; gfx.DrawImage(f, rect)
Template:       var form = new XForm(document, size); XGraphics.FromForm(form); gfx.DrawImage(form, x, y)
Line:           gfx.DrawLine(XPens.Black, x1, y1, x2, y2)
Rect:           gfx.DrawRectangle(pen, brush, x, y, w, h)
Rounded:        gfx.DrawRoundedRectangle(pen, brush, rect, new XSize(rx, ry))
Ellipse:        gfx.DrawEllipse(pen, brush, rect)
Polygon:        gfx.DrawPolygon(pen, brush, points, XFillMode.Winding)
Arc/Pie:        gfx.DrawArc(pen, rect, start, sweep) / gfx.DrawPie(pen, brush, rect, start, sweep)
Bezier/Curve:   gfx.DrawBezier(pen, p1, p2, p3, p4) / gfx.DrawCurve(pen, points)
Path:           var p = new XGraphicsPath(); p.AddArc(...); p.CloseFigure(); gfx.DrawPath(pen, brush, p)
Colour:         XColor.FromArgb(r, g, b) / FromArgb(a, r, g, b) / FromCmyk(c, m, y, k) / XColors.Red
Brush/Pen:      new XSolidBrush(color); new XPen(color, width) { DashStyle = XDashStyle.Dash }
Gradient:       new XLinearGradientBrush(rect, c1, c2, XLinearGradientMode.Horizontal)
Transform:      gfx.TranslateTransform(dx, dy); .RotateAtTransform(deg, center); .ScaleTransform(s)
State:          var st = gfx.Save(); ... gfx.Restore(st)
Clip:           gfx.IntersectClip(rect | path)
Container:      var c = gfx.BeginContainer(); ... gfx.EndContainer(c)

--- Existing PDFs ---
Open:           PdfReader.Open(path | stream, PdfDocumentOpenMode.Modify | Import | ReadOnly)
With password:  PdfReader.Open(path, "pw", PdfDocumentOpenMode.Modify)
Tolerant:       PdfReader.Open(path, PdfDocumentOpenMode.Modify, PdfReadAccuracy.Moderate)
Is PDF?:        PdfReader.TestPdfFile(path | stream | bytes) != 0
Merge:          output.AddPage(input.Pages[i])            // input opened as Import
Range:          output.Pages.InsertRange(index, input, startIndex, pageCount)
Reorder/Remove: document.Pages.MovePage(old, new); document.Pages.RemoveAt(i)
Consolidate:    document.ConsolidateImages()

--- Security ---
Passwords:      document.SecuritySettings.UserPassword / .OwnerPassword = "..."
Level:          document.SecuritySettings.DocumentSecurityLevel = PdfDocumentSecurityLevel.Encrypted128Bit
Permissions:    document.SecuritySettings.PermitPrint / .PermitModifyDocument / .PermitExtractContent /
                .PermitAnnotations / .PermitFormsFill / .PermitAssembleDocument / .PermitFullQualityPrint

--- Navigation and annotations ---
Bookmark:       document.Outlines.Add("Title", page, true, PdfOutlineStyle.Bold, XColors.Blue)
Child:          outline.Outlines.Add("Sub", page)
Rect convert:   new PdfRectangle(gfx.Transformer.WorldToDefaultPage(xrect))
Web link:       page.AddWebLink(pdfRect, url)
Page link:      page.AddDocumentLink(pdfRect, pageNumber)   // 1-based
Note:           page.Annotations.Add(new PdfTextAnnotation { Contents = "...", Icon = PdfTextAnnotationIcon.Note })
Stamp:          page.Annotations.Add(new PdfRubberStampAnnotation { Icon = PdfRubberStampAnnotationIcon.Draft })

--- Forms ---
Fields:         document.AcroForm.Fields.Names; document.AcroForm.Fields["name"]
Fill:           (fields["x"] as PdfTextField).Text = "..."; (fields["y"] as PdfCheckBoxField).Checked = true
Flatten:        document.MakeAcroFormsReadOnly()

--- Fonts ---
Embedded:       new EmbeddedFontResolver(family, [new EmbeddedResourceFontFace(face, resource)], asm)
Register:       MetaFontResolver.Instance.RegisterFontResolver(faceName, resolver)   // per FACE
Replace all:    GlobalFontSettings.FontResolver = myResolver   // before first font use
Check result:   font.FontFamily.Name
Encoding:       new XFont(name, size, style, XPdfFontOptions.WinAnsiDefault | .UnicodeDefault)

--- Charts and bar codes ---
Chart:          var chart = new Chart(ChartType.Column2D); chart.SeriesCollection.AddSeries().Add(1, 2, 3)
X labels:       chart.XValues.AddXSeries().Add("A", "B", "C")
Draw chart:     var fr = new ChartFrame(rect); fr.Add(chart); fr.Draw(gfx)
Code 39:        gfx.DrawBarCode(new Code3of9Standard("TEXT", size), XBrushes.Black, font, point)
I 2of5:         new Code2of5Interleaved("12345678", size)
DataMatrix:     gfx.DrawMatrixCode(new CodeDataMatrix("text", 26, size), XBrushes.Black, point)

--- Low level ---
Dictionary:     obj.Elements.GetString("/Key"); .SetString; .GetDictionary; .GetArray; .ContainsKey
Stream bytes:   obj.Stream.UnfilteredValue
Content ops:    ContentReader.ReadContent(page) -> CSequence; page.Contents.ReplaceContent(seq)
All objects:    document.Internals.GetAllObjects()
Custom data:    document.CustomValues["/Key"] = new PdfCustomValue(bytes)

Target: .NET 10 or later

================================================================================
