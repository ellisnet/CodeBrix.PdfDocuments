================================================================================
AGENT-README: CodeBrix.PdfDocCreate
A Guide for AI Coding Agents — CONSUMING the CodeBrix.PdfDocCreate.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.PdfDocCreate is a pure-managed .NET library (.NET 10 or later) that
builds PDF documents from a STRUCTURED document object model instead of from
drawing commands. You create a Document, add Sections, and fill them with
Paragraphs, Tables, Images, TextFrames and Charts. A renderer then performs all
layout: line breaking, pagination, repeating headers and footers, table row
splitting across pages, footnote placement and PDF outline (bookmark)
generation. You never compute a page break yourself.

This is the layer to reach for when the content is a report, an invoice, a
letter, a manual or anything else whose length is not known in advance. When
you need to place marks at exact coordinates instead, use the underlying
CodeBrix.PdfDocuments package - and note that both can be combined, because the
rendered PdfDocument stays reachable through
PdfDocumentRenderer.PdfDocument for low-level post-processing (security,
extra annotations, merging).

PROVENANCE: CodeBrix.PdfDocCreate is a port of MigraDocCore (itself a
port of empira's MigraDoc), with the MigraDocCore.DocumentObjectModel and
MigraDocCore.Rendering assemblies merged into one. If you know MigraDoc the API
is very similar - but EVERY namespace is CodeBrix.PdfDocCreate.*. Do NOT write
"using MigraDoc..." or "using MigraDocCore..."; those namespaces do not exist
in this package. Do not write API from memory of the upstream library either;
several members and defaults differ (images take an IImageSource, not a path;
there is no RTF renderer). This file documents the real surface.

PACKAGE NAME vs NAMESPACE (the single most common mistake):

  NuGet package                            Namespace root
  ---------------------------------------  ------------------------------
  CodeBrix.PdfDocCreate.MitLicenseForever  CodeBrix.PdfDocCreate.*

The ".MitLicenseForever" suffix belongs to the PACKAGE ID only. It never
appears in a namespace, a using directive or a type name.

See also (sibling packages in the same repository):
  - AGENT-README.txt (repository root) - CodeBrix.PdfDocuments, the low-level
    PDF library this package renders through and depends on: XGraphics, XFont,
    fonts and font resolution, security, outlines, merging, annotations
  - src/CodeBrix.PdfDocCreate.Html2Pdf/AGENT-README.txt - render author-written
    HTML + CSS onto this document model
  - src/CodeBrix.PdfDocCreate.Markdown2Pdf/AGENT-README.txt - render Markdown
    to PDF with zero configuration
  - src/CodeBrix.PdfRasterizer/AGENT-README.txt - rasterize the PDFs produced
    here back to images (useful for visual regression tests)

Source repository: https://github.com/ellisnet/CodeBrix.PdfDocuments
License: MIT

================================================================================

INSTALLATION
============
NuGet Package: CodeBrix.PdfDocCreate.MitLicenseForever

    dotnet add package CodeBrix.PdfDocCreate.MitLicenseForever

Or in a .csproj file (NuGet resolves the latest version):

    <PackageReference Include="CodeBrix.PdfDocCreate.MitLicenseForever" />

NuGet dependencies (all pulled in automatically):
  - CodeBrix.PdfDocuments.MitLicenseForever (which itself brings
    CodeBrix.Imaging.ApacheLicenseForever and
    CodeBrix.Compression.MitLicenseForever)

Installing this package therefore gives you the low-level PDF API as well; you
do NOT need a separate PackageReference to CodeBrix.PdfDocuments.

License: MIT.

Requirements: .NET 10 or later. Pure managed code - no native libraries, no
platform restrictions, no font installation step. Runs on Windows, macOS,
Linux, Android and iOS, in containers and on web servers.

================================================================================

KEY NAMESPACES / USINGS
=======================

    using CodeBrix.PdfDocCreate.DocumentObjectModel;
        // Document, DocumentInfo, Section, Sections, Paragraph, ParagraphFormat,
        // Style, Styles, StyleNames, Font, Color, Colors, Unit, UnitType,
        // Borders, Border, Shading, ListInfo, TabStop, TabStops, PageSetup,
        // HeaderFooter, HeadersFooters, Footnote, Hyperlink, FormattedText,
        // Text, Character, PageBreak, and the enums (ParagraphAlignment,
        // ListType, TabAlignment, TabLeader, TextFormat, Underline,
        // Strikethrough, BorderStyle, LineSpacingRule, HyperlinkType,
        // PageFormat, Orientation, BreakType, OutlineLevel, SymbolName,
        // FootnoteLocation, FootnoteNumberingRule, FootnoteNumberStyle,
        // HeaderFooterIndex, StyleType, UnitType)

    using CodeBrix.PdfDocCreate.DocumentObjectModel.Tables;
        // Table, Rows, Row, Columns, Column, Cells, Cell, and the enums
        // (VerticalAlignment, RowHeightRule, RowAlignment, Edge, RoundedCorner)

    using CodeBrix.PdfDocCreate.DocumentObjectModel.Shapes;
        // Shape, TextFrame, Image, PictureFormat, LineFormat, FillFormat,
        // WrapFormat, LeftPosition, TopPosition, Barcode, and the enums
        // (RelativeHorizontal, RelativeVertical, WrapStyle, ShapePosition,
        // TextOrientation, LineStyle, DashStyle, BarcodeType)

    using CodeBrix.PdfDocCreate.DocumentObjectModel.Shapes.Charts;
        // Chart, Series, SeriesCollection, XSeries, XValues, Axis, AxisTitle,
        // TickLabels, Gridlines, Legend, PlotArea, TextArea, DataLabel, Point,
        // XValue, and the enums (ChartType, MarkerStyle, TickMarkType,
        // DataLabelType, DataLabelPosition, BlankType, HorizontalAlignment)

    using CodeBrix.PdfDocCreate.DocumentObjectModel.Fields;
        // BookmarkField, PageField, NumPagesField, PageRefField, SectionField,
        // SectionPagesField, DateField, InfoField, InfoFieldType

    using CodeBrix.PdfDocCreate.DocumentObjectModel.IO;
        // DdlReader, DdlWriter, DdlReaderError(s), DdlErrorLevel,
        // DdlParserException

    using CodeBrix.PdfDocCreate.Rendering;
        // PdfDocumentRenderer, DocumentRenderer, PageRenderOptions

Images additionally need types from the dependency package:

    using CodeBrix.PdfDocuments.Drawing;     // ImageSource
    using CodeBrix.PdfDocuments.Utils;       // ImagingImageSource<TPixel>
    using CodeBrix.Imaging.PixelFormats;     // Rgba32

================================================================================

CORE API REFERENCE
==================

THE FOUR OBJECTS YOU ALWAYS USE
-------------------------------

    Document                  the whole document: styles + sections + metadata
    Section                   a run of pages sharing one PageSetup and one set
                              of headers/footers
    Paragraph / Table / ...   the content inside a section
    PdfDocumentRenderer       lays the document out and produces the PDF

The minimum round trip is four statements:

    var doc = new Document();
    var section = doc.AddSection();
    section.AddParagraph("Hello, PDF!");

    var renderer = new PdfDocumentRenderer { Document = doc };
    renderer.RenderDocument();
    renderer.PdfDocument.Save("output.pdf");

Nothing is laid out until RenderDocument() runs. Property values you read
before that point are the values you assigned, not the effective values (see
INHERITANCE AND FLATTENING below).

--- Document (sealed class) ---

    public Document()

    public Section AddSection()                       // appends a new section
    public void Add(Section section)
    public void Add(Style style)
    public Style AddStyle(string name, string baseStyle)
    public new Document Clone()
    public void BindToRenderer(object renderer)
    public bool IsBoundToRenderer { get; }

    public DocumentInfo Info { get; }                 // PDF metadata
    public Styles Styles { get; }                     // style table
    public Sections Sections { get; }
    public Section LastSection { get; }
    public PageSetup DefaultPageSetup { get; }        // read-only prototype
    public Unit DefaultTabStop { get; set; }          // effective default 1.25cm
    public string ImagePath { get; set; }             // extra image search root
    public string DdlFile { get; set; }               // source DDL path, if any
    public bool UseCmykColor { get; set; }            // renders in CMYK
    public FootnoteLocation FootnoteLocation { get; set; }
    public FootnoteNumberingRule FootnoteNumberingRule { get; set; }
    public FootnoteNumberStyle FootnoteNumberStyle { get; set; }
    public int FootnoteStartingNumber { get; set; }
    public string Comment { get; set; }

    public class DocumentInfo
        public string Title { get; set; }
        public string Author { get; set; }
        public string Keywords { get; set; }
        public string Subject { get; set; }
        public string Comment { get; set; }

Info flows into the PDF's own metadata when the document is rendered:

    doc.Info.Title = "Sales Report";
    doc.Info.Subject = "Quarterly Sales Data";
    doc.Info.Author = "Accounts Department";

UseCmykColor = true switches the produced PDF to PdfColorMode.Cmyk and makes
every Color render through its C/M/Y/K components - set it before rendering,
and construct colors with Color.FromCmyk when you care about exact ink values.

STYLES
------
Styles are the way to make a document consistent. A style carries a Font and a
ParagraphFormat; paragraphs name a style and then override only what differs.

    public sealed class Style
        public string Name { get; }
        public string BaseStyle { get; set; }
        public Font Font { get; }
        public ParagraphFormat ParagraphFormat { get; }
        public StyleType Type { get; }          // Paragraph | Character
        public bool BuildIn { get; }
        public bool IsReadOnly { get; }
        public Style GetBaseStyle()
        public const string DefaultParagraphName = "Normal";
        public const string DefaultParagraphFontName = "DefaultParagraphFont";

    public class Styles : DocumentObjectCollection
        public Style this[string styleName] { get; }
        public int GetIndex(string styleName)
        public Style AddStyle(string name, string baseStyleName)
        public Style Normal { get; }

Creating and modifying styles:

    // A style of your own, based on "Normal"
    var titleStyle = doc.AddStyle("Title", StyleNames.Normal);
    titleStyle.Font.Size = 24;
    titleStyle.Font.Bold = true;
    titleStyle.ParagraphFormat.SpaceAfter = 6;
    titleStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;

    // Modify the default body style
    var bodyStyle = doc.Styles[StyleNames.Normal];
    bodyStyle.Font.Size = 10;
    bodyStyle.Font.Name = "Arial";
    bodyStyle.ParagraphFormat.SpaceAfter = 4;

    // Modify the BUILT-IN heading styles - fetch them, never AddStyle them
    var h1 = doc.Styles[StyleNames.Heading1];
    h1.Font.Size = 18;
    h1.Font.Bold = true;
    h1.ParagraphFormat.SpaceBefore = 12;
    h1.ParagraphFormat.SpaceAfter = 6;

BUILT-IN STYLE NAMES
--------------------
Every Document starts with these styles already defined. The names are also
constants on the StyleNames class:

    StyleNames.Normal                 "Normal"
    StyleNames.Heading1 .. Heading9   "Heading1" ... "Heading9"
    StyleNames.DefaultParagraphFont   "DefaultParagraphFont"
    StyleNames.Footnote               "Footnote"
    StyleNames.Header                 "Header"
    StyleNames.Footer                 "Footer"
    StyleNames.Hyperlink              "Hyperlink"
    StyleNames.InvalidStyleName       "InvalidStyleName"

Use doc.AddStyle(name, baseStyleName) ONLY for names that are not in this list.
To change a built-in style, fetch it with doc.Styles[name] and set properties
on the object you get back.

The heading styles are pre-wired in two ways that AddStyle would destroy:

  - Each carries a ParagraphFormat.OutlineLevel (Heading1 -> OutlineLevel.Level1,
    Heading2 -> Level2, and so on through Level9). OutlineLevel is what drives
    PDF outline/bookmark generation: any paragraph whose effective OutlineLevel
    is Level1 or deeper becomes an entry in the reader's navigation pane,
    automatically, with no extra call.
  - They form an inheritance chain. Heading2's base style is "Heading1" - NOT
    "Normal" - Heading3's is "Heading2", and so on. Setting Font.Name on
    Heading1 therefore flows down to every deeper heading.

Set OutlineLevel yourself on any paragraph or style to add outline entries for
non-heading content:

    para.Format.OutlineLevel = OutlineLevel.Level2;

OutlineLevel values: BodyText (no entry), Level1 .. Level9.

SECTIONS AND PAGE SETUP
-----------------------

    public class Section
        public Paragraph AddParagraph()
        public Paragraph AddParagraph(string paragraphText)
        public Paragraph AddParagraph(string paragraphText, string style)
        public Table AddTable()
        public Image AddImage(IImageSource imageSource)
        public TextFrame AddTextFrame()
        public Chart AddChart()
        public Chart AddChart(ChartType type)
        public void AddPageBreak()
        public void Add(Paragraph | Table | Image | TextFrame | Chart)
        public PageSetup PageSetup { get; set; }
        public HeadersFooters Headers { get; set; }
        public HeadersFooters Footers { get; set; }
        public DocumentElements Elements { get; set; }
        public Paragraph LastParagraph { get; }
        public Table LastTable { get; }
        public Section PreviousSection()
        public string Comment { get; set; }

    public class PageSetup
        public Unit PageWidth { get; set; }
        public Unit PageHeight { get; set; }
        public PageFormat PageFormat { get; set; }
        public Orientation Orientation { get; set; }     // Portrait | Landscape
        public Unit TopMargin { get; set; }
        public Unit BottomMargin { get; set; }
        public Unit LeftMargin { get; set; }
        public Unit RightMargin { get; set; }
        public Unit HeaderDistance { get; set; }
        public Unit FooterDistance { get; set; }
        public bool MirrorMargins { get; set; }
        public bool DifferentFirstPageHeaderFooter { get; set; }
        public bool OddAndEvenPagesHeaderFooter { get; set; }
        public BreakType SectionStart { get; set; }
        public int StartingNumber { get; set; }
        public bool HorizontalPageBreak { get; set; }
        public string Comment { get; set; }
        public PageSetup PreviousPageSetup()
        public static void GetPageSize(PageFormat pageFormat,
            out Unit pageWidth, out Unit pageHeight)

THE DEFAULTS, which apply to every section that does not override them:

    PageFormat    A4          PageWidth  21 cm      PageHeight  29.7 cm
    Orientation   Portrait    TopMargin  2.5 cm     BottomMargin 2 cm
                              LeftMargin 2.5 cm     RightMargin  2.5 cm
    HeaderDistance 1.25 cm    FooterDistance 1.25 cm
    SectionStart  BreakType.BreakNextPage
    MirrorMargins false   DifferentFirstPageHeaderFooter false
    OddAndEvenPagesHeaderFooter false   HorizontalPageBreak false

The default is A4, NOT US Letter. If you want Letter, say so.

Page size, three equivalent ways:

    // 1. Named format - resolved during rendering
    section.PageSetup.PageFormat = PageFormat.Letter;

    // 2. Explicit dimensions
    section.PageSetup.PageWidth  = Unit.FromInch(8.5);
    section.PageSetup.PageHeight = Unit.FromInch(11);

    // 3. Ask for the format's dimensions and assign them yourself
    PageSetup.GetPageSize(PageFormat.Letter, out var w, out var h);
    section.PageSetup.PageWidth = w;
    section.PageSetup.PageHeight = h;

PageFormat values: A0, A1, A2, A3, A4, A5, A6, B5, Letter, Legal, Ledger,
P11x17. Note that Ledger is 1224 x 792 points (wide) while P11x17 is
792 x 1224 points (tall) - they are the same sheet in opposite orientations.

PageFormat is only consulted for a dimension you did NOT assign. Assign both
PageWidth and PageHeight and PageFormat is ignored entirely; assign just one
and the other comes from the format. Orientation.Landscape swaps the effective
width and height at render time without changing the assigned values.

Margins:

    section.PageSetup.TopMargin    = Unit.FromCentimeter(2.5);
    section.PageSetup.BottomMargin = Unit.FromCentimeter(2.5);
    section.PageSetup.LeftMargin   = Unit.FromCentimeter(2.5);
    section.PageSetup.RightMargin  = Unit.FromCentimeter(2.5);

MirrorMargins = true swaps the left and right margins on even pages, which is
what you want for a document that will be printed double-sided and bound.

Page numbering restarts:

    section.PageSetup.StartingNumber = 1;   // this section's first page is "1"

SectionStart decides where a new section begins:

    BreakType.BreakNextPage   the next page (default)
    BreakType.BreakEvenPage   the next even-numbered page
    BreakType.BreakOddPage    the next odd-numbered page (chapter openings)

HEADERS AND FOOTERS
-------------------

    public class HeadersFooters
        public HeaderFooter Primary { get; set; }
        public HeaderFooter FirstPage { get; set; }
        public HeaderFooter EvenPage { get; set; }
        public bool HasHeaderFooter(HeaderFooterIndex index)
        public bool IsHeader { get; }
        public bool IsFooter { get; }

    public class HeaderFooter
        public Paragraph AddParagraph()
        public Paragraph AddParagraph(string paragraphText)
        public Table AddTable()
        public Image AddImage(IImageSource imageSource)
        public TextFrame AddTextFrame()
        public Chart AddChart() / AddChart(ChartType type)
        public string Style { get; set; }         // defaults to Header / Footer
        public ParagraphFormat Format { get; set; }
        public DocumentElements Elements { get; set; }
        public bool IsFirstPage / IsEvenPage / IsPrimary { get; }

    public enum HeaderFooterIndex { Primary = 0, FirstPage = 1, EvenPage = 2 }

Primary is used on every page unless a more specific one applies:

    var header = section.Headers.Primary;
    var headerPara = header.AddParagraph("Quarterly Report");
    headerPara.Format.Alignment = ParagraphAlignment.Center;

    var footer = section.Footers.Primary;
    var footerPara = footer.AddParagraph();
    footerPara.AddText("Page ");
    footerPara.AddPageField();
    footerPara.AddText(" of ");
    footerPara.AddNumPagesField();
    footerPara.Format.Alignment = ParagraphAlignment.Center;

FirstPage and EvenPage are INERT until their PageSetup switch is turned on:

    section.PageSetup.DifferentFirstPageHeaderFooter = true;
    section.Headers.FirstPage.AddParagraph("");        // title page: no header
    section.Footers.FirstPage.AddParagraph("Confidential draft");

    section.PageSetup.OddAndEvenPagesHeaderFooter = true;
    section.Headers.EvenPage.AddParagraph("Company Name");   // verso
    section.Headers.Primary.AddParagraph("Chapter Title");   // recto

A section that defines no header or footer of a given kind INHERITS the
previous section's - together with the previous section's whole PageSetup. Give
a later section its own empty HeaderFooter if you want to stop that:

    nextSection.Headers.Primary.AddParagraph("");

PARAGRAPHS AND INLINE CONTENT
-----------------------------

    public class Paragraph
        public Text AddText(string text)
        public Text AddChar(char ch)  /  AddChar(char ch, int count)
        public Character AddCharacter(SymbolName symbolType)
        public Character AddCharacter(SymbolName symbolType, int count)
        public Character AddCharacter(char ch)  /  AddCharacter(char ch, int count)
        public Character AddSpace(int count)
        public void AddTab()
        public void AddLineBreak()
        public FormattedText AddFormattedText()
        public FormattedText AddFormattedText(TextFormat textFormat)
        public FormattedText AddFormattedText(Font font)
        public FormattedText AddFormattedText(string text)
        public FormattedText AddFormattedText(string text, TextFormat textFormat)
        public FormattedText AddFormattedText(string text, Font font)
        public FormattedText AddFormattedText(string text, string style)
        public Hyperlink AddHyperlink(string name)
        public Hyperlink AddHyperlink(string name, HyperlinkType type)
        public BookmarkField AddBookmark(string name)
        public PageField AddPageField()
        public NumPagesField AddNumPagesField()
        public PageRefField AddPageRefField(string name)
        public SectionField AddSectionField()
        public SectionPagesField AddSectionPagesField()
        public DateField AddDateField()  /  AddDateField(string format)
        public InfoField AddInfoField(InfoFieldType iType)
        public Footnote AddFootnote()  /  AddFootnote(string text)
        public Image AddImage(IImageSource imageSource)
        public string Style { get; set; }
        public ParagraphFormat Format { get; set; }
        public ParagraphElements Elements { get; set; }

    var para = section.AddParagraph("This is body text.");
    para.Style = StyleNames.Normal;

    section.AddParagraph("Document Title", "Title");
    section.AddParagraph("Chapter 1", StyleNames.Heading1);

    // Inline formatting
    var mixed = section.AddParagraph();
    mixed.AddText("Normal text ");
    mixed.AddFormattedText("bold text", TextFormat.Bold);
    mixed.AddText(" and ");
    var ft = mixed.AddFormattedText("red italic");
    ft.Italic = true;
    ft.Color = Colors.Firebrick;
    mixed.AddText(".");

    // Hard line break WITHIN a paragraph (does not start a new paragraph)
    mixed.AddLineBreak();
    mixed.AddText("Second line of the same paragraph.");

TextFormat is a flags enum with both an "on" and an "off" value per attribute,
so it can clear a style's setting as well as apply one:

    TextFormat.Bold / NotBold
    TextFormat.Italic / NotItalic
    TextFormat.Underline / NoUnderline

    para.AddFormattedText("still upright", TextFormat.NotItalic);

FormattedText carries the same inline API as Paragraph plus direct character
formatting - FontName, Size, Bold, Italic, Underline, Color, Superscript,
Subscript, Font and Style - and can nest inside itself.

Special characters go in through AddCharacter(SymbolName) so that they survive
DDL round trips and font substitution:

    SymbolName.Blank, En, Em, EmQuarter (= Em4), Tab, LineBreak,
    Euro, Copyright, Trademark, RegisteredTrademark, Bullet, Not,
    EmDash, EnDash, NonBreakableBlank (= HardBlank)

    para.AddCharacter(SymbolName.NonBreakableBlank);
    para.AddCharacter(SymbolName.EmDash);
    para.AddCharacter(SymbolName.Bullet, 3);

The static Character class exposes the same set as ready-made objects
(Character.Bullet, Character.EmDash, Character.HardBlank, and so on).

FONTS AND CHARACTER FORMATTING
------------------------------

    public sealed class Font
        public Font()  /  Font(string name)  /  Font(string name, Unit size)
        public string Name { get; set; }
        public Unit Size { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public Underline Underline { get; set; }
        public Strikethrough Strikethrough { get; set; }
        public Color Color { get; set; }
        public bool Superscript { get; set; }
        public bool Subscript { get; set; }
        public void ApplyFont(Font font)

    Underline values:      None, Single, Words, Dotted, Dash, DotDash, DotDotDash
    Strikethrough values:  None, Single, Words, Dotted, Dash, DotDash, DotDotDash

Superscript and Subscript are mutually exclusive: setting one clears the other.

Font families resolve through the underlying CodeBrix.PdfDocuments font
machinery, which needs no registration but SILENTLY SUBSTITUTES a family it
cannot find. See FONTS AND FONT RESOLUTION in the repository-root AGENT-README
for the resolver contract, the cross-platform-safe family names and how to
detect a substitution.

PARAGRAPH FORMAT (LAYOUT OF A PARAGRAPH)
----------------------------------------

    public class ParagraphFormat
        public ParagraphAlignment Alignment { get; set; }
        public Unit LeftIndent { get; set; }
        public Unit RightIndent { get; set; }
        public Unit FirstLineIndent { get; set; }
        public Unit SpaceBefore { get; set; }
        public Unit SpaceAfter { get; set; }
        public Unit LineSpacing { get; set; }
        public LineSpacingRule LineSpacingRule { get; set; }
        public bool KeepTogether { get; set; }
        public bool KeepWithNext { get; set; }
        public bool PageBreakBefore { get; set; }
        public bool WidowControl { get; set; }
        public OutlineLevel OutlineLevel { get; set; }
        public Font Font { get; }
        public Borders Borders { get; }
        public Shading Shading { get; }
        public ListInfo ListInfo { get; }
        public TabStops TabStops { get; }
        public bool HasTabStops { get; }
        public TabStop AddTabStop(Unit position)
        public TabStop AddTabStop(Unit position, TabAlignment alignment)
        public TabStop AddTabStop(Unit position, TabLeader leader)
        public TabStop AddTabStop(Unit position, TabAlignment alignment,
            TabLeader leader)
        public void RemoveTabStop(Unit position)
        public void Add(TabStop tabStop)
        public void ClearAll()

    ParagraphAlignment values: Left, Center, Right, Justify
    LineSpacingRule values:    Single, OnePtFive, Double, AtLeast, Exactly,
                               Multiple

PAGE FLOW CONTROL
-----------------
These four members are how you stop the renderer from breaking a page in an
ugly place. They are the reason to use this package rather than drawing.

    para.Format.PageBreakBefore = true;   // start this paragraph on a new page
    para.Format.KeepTogether    = true;   // never split this paragraph
    para.Format.KeepWithNext    = true;   // keep with the paragraph that follows
    para.Format.WidowControl    = true;   // no single line stranded on a page

A heading style should almost always set KeepWithNext:

    doc.Styles[StyleNames.Heading2].ParagraphFormat.KeepWithNext = true;

An explicit page break between two blocks of content:

    section.AddPageBreak();     // adds a PageBreak element to the section

A new SECTION also starts a new page (SectionStart decides which one). Use
AddPageBreak inside a section when the page setup and headers stay the same;
start a new section when they change.

LISTS
-----
Lists are built with ParagraphFormat.ListInfo. Do NOT fake them with a
borderless table or with literal "* " prefixes.

    public class ListInfo
        public ListType ListType { get; set; }
        public Unit NumberPosition { get; set; }
        public bool ContinuePreviousList { get; set; }

    var item1 = section.AddParagraph("First item");
    item1.Format.ListInfo.ListType = ListType.BulletList1;
    item1.Format.ListInfo.ContinuePreviousList = false;   // false starts a list
    item1.Format.LeftIndent = Unit.FromCentimeter(0.75);

    var item2 = section.AddParagraph("Second item");
    item2.Format.ListInfo.ListType = ListType.BulletList1;
    item2.Format.ListInfo.ContinuePreviousList = true;    // true continues it

ListType values:
    BulletList1, BulletList2, BulletList3    (three nesting levels)
    NumberList1, NumberList2, NumberList3

NumberPosition sets where the bullet or number is drawn. For correct hanging
indents at any nesting depth, combine a negative FirstLineIndent with a tab
stop at the LeftIndent position:

    item.Format.LeftIndent = Unit.FromCentimeter(0.75);
    item.Format.FirstLineIndent = Unit.FromCentimeter(-0.75);
    item.Format.AddTabStop(Unit.FromCentimeter(0.75), TabAlignment.Left);

TAB STOPS
---------
Useful for aligned columns without the weight of a table - for example a table
of contents with dot leaders.

    var line = section.AddParagraph();
    line.Format.AddTabStop(Unit.FromCentimeter(8), TabAlignment.Right,
        TabLeader.Dots);
    line.AddText("Chapter 1");
    line.AddTab();
    line.AddText("14");

    TabAlignment values:  Left, Center, Right, Decimal
    TabLeader values:     Spaces, Dots, Dashes, Lines, Heavy, MiddleDot

ParagraphFormat.TabStops is the underlying collection; AddTabStop adds to it,
and ClearAll() removes any tab stops inherited from the style. Past the last
explicit tab stop the renderer falls back to repeating stops every 1.25 cm, or
every Document.DefaultTabStop when that has been assigned. A negative
FirstLineIndent additionally creates an automatic stop at LeftIndent, which is
what makes the hanging-indent recipe above work.

SHADING AND BORDERS
-------------------

    public sealed class Shading
        public bool Visible { get; set; }
        public Color Color { get; set; }
        public bool IsCleared { get; }
        public void Clear()

    public class Borders
        public Border Top / Left / Bottom / Right / DiagonalUp / DiagonalDown
        public bool Visible { get; set; }
        public BorderStyle Style { get; set; }
        public Unit Width { get; set; }
        public Color Color { get; set; }
        public Unit Distance { get; set; }
        public Unit DistanceFromTop / DistanceFromBottom
                  / DistanceFromLeft / DistanceFromRight { get; set; }
        public bool HasBorder(BorderType type)
        public void ClearAll()

    public class Border
        public bool Visible { get; set; }
        public BorderStyle Style { get; set; }
        public Unit Width { get; set; }
        public Color Color { get; set; }
        public string Name { get; }
        public void Clear()

    BorderStyle values: None, Single, Dot, DashSmallGap, DashLargeGap,
                        DashDot, DashDotDot
    BorderType values:  Top, Left, Bottom, Right, Horizontal, Vertical,
                        DiagonalDown, DiagonalUp

Shading is not limited to table cells - a paragraph can have its own background
and borders, which makes it a good building block for callouts:

    var callout = section.AddParagraph("Note: this is important.");
    callout.Format.Shading.Color = Colors.LightYellow;
    callout.Format.Borders.Width = 0.5;
    callout.Format.Borders.Color = Colors.Orange;

You do NOT need to set Visible = true. If Visible was never assigned, the
renderer treats the shading as visible whenever Color has been set, so setting
Color alone is enough. Reading Shading.Visible on such a paragraph still
returns false - that is the unassigned flag, not the effective state. Assign
Visible = false explicitly to suppress a shading inherited from a style.

A paragraph border is also the cleanest way to draw a horizontal rule - put a
top border on a paragraph with a tiny font:

    var rule = section.AddParagraph();
    rule.Format.Font.Size = 1;
    rule.Format.Borders.Top.Width = 0.75;
    rule.Format.Borders.Top.Color = Colors.Gray;

FIELDS (PAGE NUMBERS, DATES, METADATA, CROSS REFERENCES)
--------------------------------------------------------
Fields are placeholders the renderer fills in at layout time, when the page
count and page numbers are finally known. They live in ParagraphElements, so
every Add*Field method exists on Paragraph, FormattedText and Hyperlink.

    para.AddPageField()          -> this page's number       (PageField)
    para.AddNumPagesField()      -> total pages in document  (NumPagesField)
    para.AddSectionField()       -> this section's number    (SectionField)
    para.AddSectionPagesField()  -> pages in this section    (SectionPagesField)
    para.AddPageRefField("ch1")  -> page holding bookmark    (PageRefField)
    para.AddDateField()          -> the render date          (DateField)
    para.AddDateField("yyyy-MM-dd")
    para.AddInfoField(InfoFieldType.Title)                   (InfoField)
    para.AddBookmark("ch1")      -> a named jump target      (BookmarkField)

The Add*Field METHODS are on Paragraph, so the DocumentObjectModel using is
enough to call them. The field TYPES and the InfoFieldType enum live in
CodeBrix.PdfDocCreate.DocumentObjectModel.Fields - add that using as soon as
you name one of them (which AddInfoField forces you to do).

PageField, NumPagesField, SectionField, SectionPagesField and PageRefField all
derive from NumericFieldBase and share a Format property that selects the
numbering style:

    public abstract class NumericFieldBase : DocumentObject
        public string Format { get; set; }

    Recognized values: "ROMAN" (I, II, III), "roman" (i, ii, iii),
    "ALPHABETIC" (A, B, C), "alphabetic" (a, b, c). Any other value - including
    null - renders plain arabic numerals.

    var pf = footer.AddParagraph().AddPageField();
    pf.Format = "roman";          // front matter numbered i, ii, iii

DateField.Format is a standard .NET DateTime format string, applied to the date
of the render:

    para.AddDateField("D");                 // Monday, 24 August 2026
    para.AddDateField("yyyy-MM-dd HH:mm");

InfoFieldType values: Title, Author, Keywords, Subject - each pulls the
matching property from Document.Info, so metadata stays in one place:

    header.AddParagraph().AddInfoField(InfoFieldType.Title);

PageRefField is the "see page N" cross reference. Mark the target with a
bookmark, then reference it by the same name:

    section.AddParagraph("Chapter 1").AddBookmark("ch1");
    ...
    var xref = section.AddParagraph();
    xref.AddText("See Chapter 1 on page ");
    xref.AddPageRefField("ch1");

HYPERLINKS AND BOOKMARKS
------------------------

    public class Hyperlink
        public string Name { get; set; }
        public HyperlinkType Type { get; set; }
        public Font Font { get; }
        public ParagraphElements Elements { get; }
        // plus the whole inline API: AddText, AddFormattedText, AddImage,
        // AddCharacter, AddTab, the Add*Field family, AddFootnote

    HyperlinkType values:
        HyperlinkType.Web       (also spelled HyperlinkType.Url)
        HyperlinkType.Local     (also spelled HyperlinkType.Bookmark)
        HyperlinkType.File

    // Web link
    var para = section.AddParagraph();
    para.AddText("See ");
    para.AddHyperlink("https://example.com", HyperlinkType.Web)
        .AddText("the documentation");

    // Internal navigation: mark a target, then link to it by name
    section.AddParagraph("Chapter 1").AddBookmark("ch1");

    var toc = section.AddParagraph();
    toc.AddHyperlink("ch1", HyperlinkType.Bookmark).AddText("Jump to Chapter 1");

Note the difference from document outlines: AddBookmark creates a named jump
target INSIDE the document model, whereas the PDF outline pane is built from
ParagraphFormat.OutlineLevel (see BUILT-IN STYLE NAMES above).

FOOTNOTES
---------

    public class Footnote
        public Paragraph AddParagraph()  /  AddParagraph(string text)
        public Table AddTable()
        public Image AddImage(IImageSource imageSource)
        public string Reference { get; set; }   // a custom mark instead of a number
        public string Style { get; set; }       // defaults to StyleNames.Footnote
        public ParagraphFormat Format { get; set; }
        public DocumentElements Elements { get; }

    var body = section.AddParagraph("The figure was revised in 2026");
    var note = body.AddFootnote("Revised after the audit; see appendix B.");
    body.AddText(".");

Document-level footnote settings:

    doc.FootnoteLocation = FootnoteLocation.BottomOfPage;  // or BeneathText
    doc.FootnoteNumberingRule = FootnoteNumberingRule.RestartPage;
        // or RestartContinuous, RestartSection
    doc.FootnoteNumberStyle = FootnoteNumberStyle.Arabic;
        // or LowercaseLetter, UppercaseLetter, LowercaseRoman, UppercaseRoman
    doc.FootnoteStartingNumber = 1;

Set Footnote.Reference to use a literal mark (an asterisk, a dagger) instead of
the automatic number.

TABLES
------

    public class Table
        public Column AddColumn()  /  AddColumn(Unit width)
        public Row AddRow()
        public Columns Columns { get; }
        public Rows Rows { get; }
        public Cell this[int rwIdx, int clmIdx] { get; }
        public bool IsEmpty { get; }
        public string Style { get; set; }
        public ParagraphFormat Format { get; set; }
        public Borders Borders { get; }
        public Shading Shading { get; }
        public bool KeepTogether { get; set; }
        public Unit TopPadding / BottomPadding
                  / LeftPadding / RightPadding { get; set; }
        public void SetShading(int clm, int row, int clms, int rows, Color clr)
        public void SetEdge(int clm, int row, int clms, int rows,
            Edge edge, BorderStyle style, Unit width)
        public void SetEdge(int clm, int row, int clms, int rows,
            Edge edge, BorderStyle style, Unit width, Color clr)

    public class Rows
        public Row AddRow()
        public Row this[int index] { get; }
        public RowAlignment Alignment { get; set; }   // Left | Center | Right
        public Unit LeftIndent { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public Unit Height { get; set; }
        public RowHeightRule HeightRule { get; set; }

    public class Row
        public Cell this[int index] { get; }
        public Cells Cells { get; }
        public int Index { get; }
        public bool HeadingFormat { get; set; }
        public int KeepWith { get; set; }
        public Unit Height { get; set; }
        public RowHeightRule HeightRule { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public Unit TopPadding / BottomPadding { get; set; }
        public Borders Borders { get; }
        public Shading Shading { get; }
        public string Style { get; set; }
        public ParagraphFormat Format { get; set; }

    public class Column
        public Unit Width { get; set; }
        public int Index { get; }
        public Cell this[int index] { get; }
        public bool HeadingFormat { get; set; }
        public int KeepWith { get; set; }
        public Unit LeftPadding / RightPadding { get; set; }
        public Borders Borders { get; }
        public Shading Shading { get; }
        public string Style { get; set; }
        public ParagraphFormat Format { get; set; }

    public class Cell
        public Paragraph AddParagraph()  /  AddParagraph(string paragraphText)
        public Image AddImage(IImageSource imageSource)
        public TextFrame AddTextFrame()
        public Chart AddChart()  /  AddChart(ChartType type)
        public int MergeRight { get; set; }
        public int MergeDown { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public RoundedCorner RoundedCorner { get; set; }
        public Borders Borders { get; }
        public Shading Shading { get; }
        public Table Table / Row Row / Column Column { get; }
        public string Style { get; set; }
        public ParagraphFormat Format { get; set; }
        public DocumentElements Elements { get; }

    VerticalAlignment values: Top, Center, Bottom
    RowHeightRule values:     AtLeast, Auto, Exactly
    RowAlignment values:      Left, Center, Right
    RoundedCorner values:     None, TopLeft, TopRight, BottomLeft, BottomRight
    Edge values (flags):      Top, Left, Bottom, Right, Horizontal, Vertical,
                              DiagonalDown, DiagonalUp, Box (all four sides),
                              Interior, Cross

Building a table:

    var table = section.AddTable();
    table.Borders.Visible = true;

    table.AddColumn(Unit.FromCentimeter(3));
    table.AddColumn(Unit.FromCentimeter(5));
    table.AddColumn(Unit.FromCentimeter(3));

    var headerRow = table.AddRow();
    headerRow.HeadingFormat = true;              // repeat on every page
    headerRow.Shading.Color = Colors.LightGray;
    headerRow.Cells[0].AddParagraph("Name");
    headerRow.Cells[1].AddParagraph("Description");
    headerRow.Cells[2].AddParagraph("Price");

    var row = table.AddRow();
    row.Cells[0].AddParagraph("Widget");
    row.Cells[1].AddParagraph("A useful widget");
    row.Cells[2].AddParagraph("$9.99");

A column with no width - and no Columns.Width covering it - defaults to 2.5 cm.
Table left/right padding defaults to 1.2 mm.

TABLES ACROSS PAGE BREAKS - the four members that matter:

    headerRow.HeadingFormat = true;   // this row repeats at the top of every
                                      // page the table continues onto
    row.KeepWith = 2;                 // keep this row with the next 2 rows
    table.KeepTogether = true;        // never split the table at all
    table.Rows.LeftIndent = Unit.FromCentimeter(1);   // indent the whole table

HeadingFormat also exists on Column, and marks a leading column as a heading.

Cell merging, shading, height and alignment:

    row.Cells[0].MergeRight = 1;      // merge with the cell to the right
    row.Cells[1].MergeDown  = 1;      // merge with the cell below
    cell.Shading.Color = Colors.LightBlue;
    row.HeightRule = RowHeightRule.Exactly;
    row.Height = 14;
    row.VerticalAlignment = VerticalAlignment.Center;
    cell.Borders.Left.Width = 8;
    cell.Borders.Bottom.Width = 1.5;

Range operations save a lot of loops:

    // Shade a 3-column, 1-row range starting at column 0, row 0
    table.SetShading(0, 0, 3, 1, Colors.LightGray);

    // Box border around a 3x4 range
    table.SetEdge(0, 0, 3, 4, Edge.Box, BorderStyle.Single, 0.75, Colors.Black);

IMAGES
------
An image is added from an IImageSource, NOT from a path string. There is no
AddImage(string) overload anywhere in this package.

    public class Image : Shape
        public IImageSource Source { get; set; }
        public Unit Width / Height { get; set; }        // from Shape
        public double ScaleWidth / ScaleHeight { get; set; }
        public bool LockAspectRatio { get; set; }
        public double Resolution { get; set; }
        public PictureFormat PictureFormat { get; }     // CropLeft/Right/Top/Bottom
        public string GetFilePath(string workingDir)

The image source factory lives in the dependency package, and its
implementation must be installed once per process before the first call:

    using CodeBrix.Imaging.PixelFormats;
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Utils;

    ImageSource.ImageSourceImpl ??= new ImagingImageSource<Rgba32>();

    var image = section.AddImage(ImageSource.FromFile("photo.png"));
    image.LockAspectRatio = true;
    image.Width = Unit.FromCentimeter(8);

    public abstract class ImageSource                   // CodeBrix.PdfDocuments.Drawing
        public const int DefaultQuality = 75;
        public static ImageSource ImageSourceImpl { get; set; }
        public static IImageSource FromFile(string path, int? quality = DefaultQuality)
        public static IImageSource FromBinary(string name, Func<byte[]> imageSource,
            int? quality = DefaultQuality)
        public static IImageSource FromStream(string name, Func<Stream> imageStream,
            int? quality = DefaultQuality)

        public interface IImageSource : IDisposable
            int Width { get; }  int Height { get; }  string Name { get; }
            bool Transparent { get; }
            void SaveAsJpeg(MemoryStream ms);
            void SaveAsPdfBitmap(MemoryStream ms);

Formats that can carry alpha (PNG, WebP, GIF, BMP, TGA, TIFF) embed losslessly
with a soft mask; alpha-free input (JPEG, PBM) re-encodes as JPEG at the
quality you pass. Set Document.ImagePath to give relative image names an extra
search root.

VECTOR IMAGE SOURCES. An image source may draw itself as page content instead
of supplying pixels:

    public interface IVectorImageSource : IImageSource    // nested in ImageSource
        double WidthPoints { get; }      // natural size, in POINTS
        double HeightPoints { get; }
        void Draw(XGraphics graphics, XRect destination)

Add one with the same AddImage call. The image renderer lays it out at its
natural size in POINTS (Width, Height, ScaleWidth/ScaleHeight and
LockAspectRatio all behave as usual; Resolution is meaningless and ignored),
then calls Draw at render time with the graphics state saved and restored
around the call - so NOTHING is embedded as a bitmap and the implementation
may transform and clip freely. PictureFormat cropping does NOT apply to a
vector source, and its SaveAsJpeg / SaveAsPdfBitmap members are never called
(an implementation may throw NotSupportedException from them).

This package parses no vector format itself - it provides the seam, and the
caller supplies the drawing. SVG in particular is NOT a feature here: it
belongs to CodeBrix.PdfDocCreate.Html2Pdf, which implements
IVectorImageSource over its own SVG engine.

TEXT FRAMES AND SHAPE POSITIONING
---------------------------------
A TextFrame is a free-floating box that holds its own paragraphs, tables,
images and charts. Use it for pull quotes, sidebars, stamps and captions that
must sit at a fixed spot rather than in the text flow.

    public class Shape : DocumentObject          // base of TextFrame, Image, Chart
        public Unit Width { get; set; }
        public Unit Height { get; set; }
        public LeftPosition Left { get; set; }
        public TopPosition Top { get; set; }
        public RelativeHorizontal RelativeHorizontal { get; set; }
        public RelativeVertical RelativeVertical { get; set; }
        public WrapFormat WrapFormat { get; }
        public LineFormat LineFormat { get; }
        public FillFormat FillFormat { get; }

    public class TextFrame : Shape
        public Paragraph AddParagraph()  /  AddParagraph(string paragraphText)
        public Table AddTable()
        public Image AddImage(IImageSource imageSource)
        public Chart AddChart()  /  AddChart(ChartType type)
        public Unit MarginLeft / MarginRight / MarginTop / MarginBottom { get; set; }
        public TextOrientation Orientation { get; set; }
        public DocumentElements Elements { get; }

    public class WrapFormat
        public WrapStyle Style { get; set; }        // TopBottom | None | Through
        public Unit DistanceTop / DistanceBottom
                  / DistanceLeft / DistanceRight { get; set; }

    public class LineFormat
        public bool Visible { get; set; }
        public Unit Width { get; set; }
        public Color Color { get; set; }
        public DashStyle DashStyle { get; set; }    // Solid, Dash, DashDot,
                                                    // DashDotDot, SquareDot
        public LineStyle Style { get; set; }        // Single

    public class FillFormat
        public bool Visible { get; set; }
        public Color Color { get; set; }

    RelativeHorizontal values: Character, Column, Margin, Page
    RelativeVertical values:   Line, Margin, Page, Paragraph
    ShapePosition values:      Undefined, Left, Right, Center, Top, Bottom,
                               Inside, Outside
    TextOrientation values:    Horizontal, HorizontalRotatedFarEast, Upward,
                               Vertical, VerticalFarEast, Downward

Left and Top are LeftPosition / TopPosition structs with implicit conversions
from Unit, double, int, string and ShapePosition, so both spellings compile:

    frame.Left = ShapePosition.Right;             // aligned to the right edge
    frame.Top  = Unit.FromCentimeter(4);          // 4 cm down from the anchor

A worked example:

    var frame = section.AddTextFrame();
    frame.Width  = Unit.FromCentimeter(6);
    frame.Height = Unit.FromCentimeter(3.5);
    frame.RelativeHorizontal = RelativeHorizontal.Margin;
    frame.RelativeVertical = RelativeVertical.Paragraph;
    frame.Left = ShapePosition.Right;
    frame.Top = Unit.FromCentimeter(0.5);
    frame.MarginLeft = frame.MarginRight = Unit.FromMillimeter(3);
    frame.MarginTop = frame.MarginBottom = Unit.FromMillimeter(3);
    frame.FillFormat.Color = Colors.WhiteSmoke;
    frame.LineFormat.Width = 0.5;
    frame.LineFormat.Color = Colors.Silver;
    frame.WrapFormat.Style = WrapStyle.TopBottom;
    frame.WrapFormat.DistanceLeft = Unit.FromMillimeter(4);

    var quote = frame.AddParagraph("\"Layout you never had to compute.\"");
    quote.Format.Font.Italic = true;
    quote.Format.Alignment = ParagraphAlignment.Center;

A TextFrame whose Width or Height was never assigned defaults to 1 inch in that
dimension - big enough to see, far too small for real content, so set both.

WrapStyle.TopBottom keeps body text above and below the frame (the default
behaviour when the frame is anchored to a Line or a Paragraph). WrapStyle.None
and WrapStyle.Through both let the frame float free of the text flow; there is
no text-hugging-the-contour wrap in this package.

BARCODES ARE NOT RENDERED
-------------------------
The Barcode shape type exists (section.Elements.AddBarcode(), Barcode.Code,
Barcode.Type with Barcode25i / Barcode39 / Barcode128, LineHeight, LineRatio,
NarrowLineWidth, BearerBars, Text) but NO renderer handles it: a Barcode added
to a document produces nothing on the page. It is unfinished upstream code that
came across with the port. For real bar codes, draw them with the
CodeBrix.PdfDocuments bar-code API onto the rendered PdfDocument - see BAR
CODES in the repository-root AGENT-README.

CHARTS
------
Charts are shapes too, so they are positioned exactly like a TextFrame and can
live in a section, a cell, a header, a footnote or a text frame.

    public class Chart : Shape
        public Chart()  /  Chart(ChartType type)
        public ChartType Type { get; set; }
        public SeriesCollection SeriesCollection { get; }
        public XValues XValues { get; }
        public Axis XAxis / YAxis / ZAxis { get; }
        public PlotArea PlotArea { get; }
        public DataLabel DataLabel { get; }
        public bool HasDataLabel { get; set; }
        public BlankType DisplayBlanksAs { get; set; }  // NotPlotted|Interpolated|Zero
        public TextArea HeaderArea / FooterArea / TopArea / BottomArea
                     / LeftArea / RightArea { get; }
        public string Style { get; set; }
        public ParagraphFormat Format { get; }

    public class SeriesCollection
        public Series AddSeries()
        public Series this[int index] { get; }

    public class Series
        public string Name { get; set; }
        public Point Add(double value)
        public void Add(params double[] values)
        public void AddBlank()
        public int Count { get; }
        public ChartType ChartType { get; set; }     // per-series override
        public LineFormat LineFormat { get; }
        public FillFormat FillFormat { get; }
        public MarkerStyle MarkerStyle { get; set; }
        public Unit MarkerSize { get; set; }
        public Color MarkerForegroundColor / MarkerBackgroundColor { get; set; }
        public DataLabel DataLabel { get; }
        public bool HasDataLabel { get; set; }

    public class XValues
        public XSeries AddXSeries()
        public XSeries this[int index] { get; }

    public class XSeries
        public XValue Add(string value)
        public void Add(params string[] values)
        public void AddBlank()

    public class Axis
        public AxisTitle Title { get; }
        public double MinimumScale / MaximumScale { get; set; }
        public double MajorTick / MinorTick { get; set; }
        public TickMarkType MajorTickMark / MinorTickMark { get; set; }
        public TickLabels TickLabels { get; }
        public Gridlines MajorGridlines / MinorGridlines { get; }
        public bool HasMajorGridlines / HasMinorGridlines { get; set; }
        public LineFormat LineFormat { get; }

    public class AxisTitle
        public string Caption { get; set; }
        public Font Font { get; }
        public Unit Orientation { get; set; }        // rotation angle
        public HorizontalAlignment Alignment { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public string Style { get; set; }

    public class TickLabels
        public string Format { get; set; }           // .NET numeric format string
        public Font Font { get; }
        public string Style { get; set; }

    public class Gridlines
        public LineFormat LineFormat { get; }

    public class PlotArea
        public LineFormat LineFormat { get; }
        public FillFormat FillFormat { get; }
        public Unit LeftPadding / RightPadding
                  / TopPadding / BottomPadding { get; set; }

    public class DataLabel
        public DataLabelType Type { get; set; }      // None | Percent | Value
        public DataLabelPosition Position { get; set; }
                                     // Center | InsideBase | InsideEnd | OutsideEnd
        public string Format { get; set; }           // .NET numeric format string
        public Font Font { get; }
        public string Style { get; set; }

    public class TextArea                            // the six chart areas
        public Paragraph AddParagraph()  /  AddParagraph(string paragraphText)
        public Table AddTable()
        public Image AddImage(IImageSource imageSource)
        public Legend AddLegend()
        public Unit Width / Height { get; set; }
        public Unit LeftPadding / RightPadding
                  / TopPadding / BottomPadding { get; set; }
        public VerticalAlignment VerticalAlignment { get; set; }
        public LineFormat LineFormat { get; }
        public FillFormat FillFormat { get; }
        public ParagraphFormat Format { get; }
        public string Style { get; set; }

    public class Legend
        public LineFormat LineFormat { get; }
        public ParagraphFormat Format { get; }
        public string Style { get; set; }

    ChartType values:  Line, Column2D, ColumnStacked2D, Area2D, Bar2D,
                       BarStacked2D, Pie2D, PieExploded2D
    MarkerStyle:       None, Circle, Dash, Diamond, Dot, Plus, Square, Star,
                       Triangle, X
    TickMarkType:      None, Inside, Outside, Cross
    BlankType:         NotPlotted, Interpolated, Zero

A complete chart:

    var chart = section.AddChart(ChartType.Column2D);
    chart.Width  = Unit.FromCentimeter(16);   // REQUIRED - no default size
    chart.Height = Unit.FromCentimeter(9);

    var quarters = chart.XValues.AddXSeries();
    quarters.Add("Q1", "Q2", "Q3", "Q4");

    var widgets = chart.SeriesCollection.AddSeries();
    widgets.Name = "Widgets";
    widgets.Add(45000, 52000, 48500, 61000);
    widgets.FillFormat.Color = Colors.SteelBlue;

    var gadgets = chart.SeriesCollection.AddSeries();
    gadgets.Name = "Gadgets";
    gadgets.Add(31000, 29500, 40250, 44000);
    gadgets.FillFormat.Color = Colors.DarkOrange;

    chart.XAxis.MajorTickMark = TickMarkType.Outside;
    chart.XAxis.Title.Caption = "Quarter";

    chart.YAxis.MajorTickMark = TickMarkType.Outside;
    chart.YAxis.HasMajorGridlines = true;
    chart.YAxis.MajorGridlines.LineFormat.Color = Colors.Gainsboro;
    chart.YAxis.TickLabels.Format = "#,##0";
    chart.YAxis.Title.Caption = "Revenue";

    chart.HasDataLabel = true;
    chart.DataLabel.Type = DataLabelType.Value;
    chart.DataLabel.Position = DataLabelPosition.OutsideEnd;
    chart.DataLabel.Format = "#,##0";

    chart.PlotArea.LineFormat.Color = Colors.LightGray;
    chart.PlotArea.LineFormat.Width = 0.5;

    chart.HeaderArea.AddParagraph("Sales by quarter");
    chart.BottomArea.AddLegend();

Series.Add(params double[]) fills a whole series in one call;
Series.AddBlank() inserts a gap that DisplayBlanksAs then interprets. Set
Series.ChartType to mix, for example, a Line series on top of Column2D bars.
Pie charts read the FIRST series only.

COLORS
------

    public struct Color
        public Color(uint argb)                       // packed 0xAARRGGBB
        public Color(byte r, byte g, byte b)
        public Color(byte a, byte r, byte g, byte b)
        public Color(double cyan, double magenta, double yellow, double black)
        public Color(double alpha, double cyan, double magenta,
            double yellow, double black)
        public static Color FromRgb(byte r, byte g, byte b)
        public static Color FromArgb(byte a, byte r, byte g, byte b)
        public static Color FromRgbColor(byte a, Color color)
        public static Color FromCmyk(double cyan, double magenta,
            double yellow, double black)
        public static Color FromCmyk(double alpha, double cyan, double magenta,
            double yellow, double black)
        public static Color FromCmykColor(double alpha, Color color)
        public static Color Parse(string color)
        public static readonly Color Empty
        public uint A, R, G, B { get; }               // 0-255
        public double Alpha, C, M, Y, K { get; }      // 0-100 (CMYK)
        public uint Argb { get; }  public uint RGB { get; }
        public bool IsCmyk { get; }  public bool IsEmpty { get; }

The Colors class exposes 141 named constants (Colors.Black, Colors.White,
Colors.Red, Colors.SteelBlue, Colors.LightYellow, Colors.DarkOrange,
Colors.Gainsboro, Colors.WhiteSmoke, ... - the standard HTML/X11 names).

    // RGB - all of these are fully opaque
    var c1 = Color.FromRgb(255, 253, 231);
    var c2 = new Color(255, 253, 231);           // identical to FromRgb

    // RGB with an explicit alpha (0 = transparent, 255 = opaque)
    var c3 = Color.FromArgb(128, 255, 253, 231);
    var c4 = new Color(128, 255, 253, 231);      // identical to FromArgb
    var c5 = Color.FromRgbColor(128, Colors.SteelBlue);   // change alpha only

    // Packed ARGB integer, in the form 0xAARRGGBB
    var c6 = new Color(0xFFFFFDE7);

    // CMYK - all values are percentages, 0 to 100
    var c7 = Color.FromCmyk(0, 1, 9, 0);
    var c8 = Color.FromCmyk(80, 0, 1, 9, 0);             // leading value is alpha
    var c9 = Color.FromCmykColor(80, Colors.SteelBlue);

    // From a string
    var c10 = Color.Parse("SteelBlue");   // any name from the Colors class
    var c11 = Color.Parse("#fffde7");     // CSS hex - always OPAQUE
    var c12 = Color.Parse("#ffd");        // CSS shorthand, expands to #ffffdd
    var c13 = Color.Parse("0xFFFFFDE7");  // packed 0xAARRGGBB

WARNING - the two hex spellings do NOT mean the same thing. The prefix decides:

    Color.Parse("#c0c0c0")     -> A=255. Light grey, as in CSS.
    Color.Parse("0xc0c0c0")    -> A=0.   FULLY TRANSPARENT, not light grey.

"0x" introduces a packed 0xAARRGGBB integer, so six digits leave the alpha byte
at zero and the color silently disappears. Write the alpha explicitly
("0xFFC0C0C0"), or use the CSS form, or use Color.FromRgb. See the pitfalls.

An eight-digit "#" value is REJECTED rather than guessed at, because CSS writes
#rrggbbaa with the alpha last while "0x" puts it first - the two orders cannot
be told apart. Use the "0x" form when you need to specify alpha in a string.

UNIT AND UNITTYPE
-----------------
Every measurement in this package is a Unit. A bare number means POINTS
(1 inch = 72 points).

    public struct Unit : IFormattable, INullableValue
        public Unit(double point)
        public Unit(double value, UnitType type)
        public static Unit FromPoint(double value)
        public static Unit FromCentimeter(double value)
        public static Unit FromMillimeter(double value)
        public static Unit FromInch(double value)
        public static Unit FromPica(double value)
        public static Unit Parse(string value)
        public double Point / Centimeter / Millimeter / Inch / Pica { get; set; }
        public double Value { get; set; }        // the number in its own unit
        public UnitType Type { get; set; }
        public bool IsEmpty { get; }
        public void ConvertType(UnitType type)
        public static readonly Unit Empty
        public static readonly Unit Zero
        // implicit conversions: from string, int, float, double; to double, float

    public enum UnitType { Point = 0, Centimeter = 1, Inch = 2,
                           Millimeter = 3, Pica = 4 }

Because of the implicit string conversion, these all compile and mean what they
look like:

    section.PageSetup.LeftMargin = "2.5cm";
    frame.Width  = "3in";
    para.Format.SpaceAfter = "6pt";
    row.Height = 14;                 // no suffix = 14 POINTS
    para.Format.LeftIndent = "18mm";
    tab = para.Format.AddTabStop("4pc");   // picas

Recognized suffixes: cm, in, mm, pc, pt, and none (points). Anything else
throws ArgumentException. A comma is accepted as the decimal separator.

RENDERING TO PDF
----------------

    public class PdfDocumentRenderer
        public PdfDocumentRenderer()
        public PdfDocumentRenderer(bool unicode)
        public Document Document { set; }
        public DocumentRenderer DocumentRenderer { get; set; }
        public bool Unicode { get; }
        public string Language { get; set; }
        public string WorkingDirectory { get; set; }
        public PdfDocument PdfDocument { get; set; }
        public void RenderDocument()
        public void PrepareRenderPages()
        public void RenderPages(int startPage, int endPage)
        public int PageCount { get; }
        public void Save(string path)
        public void Save(Stream stream, bool closeStream)
        public void WriteDocumentInformation()

The standard call:

    var renderer = new PdfDocumentRenderer { Document = doc };
    renderer.RenderDocument();
    renderer.Save("output.pdf");

renderer.Save(path) and renderer.PdfDocument.Save(path) both work and produce
the same bytes; Save(path) additionally honours WorkingDirectory. To a stream:

    using var stream = new MemoryStream();
    renderer.Save(stream, closeStream: false);
    byte[] pdfBytes = stream.ToArray();

The underlying PDF document is available for low-level post-processing after
rendering - security, extra annotations, merging with other PDFs, image
consolidation:

    renderer.RenderDocument();
    var pdf = renderer.PdfDocument;
    pdf.SecuritySettings.OwnerPassword = "secret";
    pdf.Save("secured.pdf");

That same seam is how a PdfDocCreate document opts in to CFF font subsetting -
the option lives on the PDF document, not on the DOM:

    var renderer = new PdfDocumentRenderer { Document = doc };
    renderer.RenderDocument();
    renderer.PdfDocument.Options.CffSubsetMode = PdfCffSubsetMode.Compact;
    renderer.PdfDocument.Save("output.pdf");

It only affects fonts with PostScript (CFF) outlines, the default leaves output
byte for byte as it was, and the mode is read when the document is saved - so
set it after RenderDocument() and before Save(). PdfCffSubsetMode lives in
CodeBrix.PdfDocuments.Pdf; the CodeBrix.PdfDocuments AGENT-README documents the
modes and what each one costs.

new PdfDocumentRenderer(unicode: true) makes every text run use Unicode font
encoding instead of WinAnsi - required for any text outside Windows-1252.
Language sets the PDF's /Lang entry ("en-US"), which assistive technology reads.

Rendering only part of a document:

    renderer.PrepareRenderPages();               // lay out, create the PdfDocument
    int total = renderer.PageCount;              // valid from here on
    renderer.RenderPages(1, Math.Min(10, total));  // first ten pages only
    renderer.Save("preview.pdf");

PageCount and RenderPages both require the layout pass, so call
PrepareRenderPages() (or RenderDocument()) first; PageCount on a fresh renderer
throws NullReferenceException.

DOCUMENTRENDERER: LAYOUT WITHOUT A PDF, AND PROGRESS
----------------------------------------------------
DocumentRenderer is the layout engine underneath PdfDocumentRenderer. Use it
directly to draw laid-out pages onto any XGraphics - an on-screen preview
surface, an image, a page of a PDF you are assembling yourself - or to report
progress on a long document.

    public class DocumentRenderer
        public DocumentRenderer(Document document)
        public void PrepareDocument()                  // run the layout pass
        public FormattedDocument FormattedDocument { get; }
        public void RenderPage(XGraphics gfx, int page)
        public void RenderPage(XGraphics gfx, int page, PageRenderOptions options)
        public void RenderObject(XGraphics graphics, XUnit xPosition,
            XUnit yPosition, XUnit width, DocumentObject documentObject)
        public DocumentObject[] GetDocumentObjectsFromPage(int page)
        public RenderInfo[] GetRenderInfoFromPage(int page)
        public string WorkingDirectory { get; set; }
        public XPrivateFontCollection PrivateFonts { get; set; }
        public event PrepareDocumentProgressEventHandler PrepareDocumentProgress;
        public bool HasPrepareDocumentProgress { get; }

        public delegate void PrepareDocumentProgressEventHandler(
            object sender, PrepareDocumentProgressEventArgs e);

        public class PrepareDocumentProgressEventArgs : EventArgs
            public int Value;      // work completed
            public int Maximum;    // total work

    public class FormattedDocument
        public int PageCount { get; }
        public PageInfo GetPageInfo(int page)          // 1-based

    public class PageInfo
        public XUnit Width { get; }
        public XUnit Height { get; }
        public PageOrientation Orientation { get; }

    [Flags] public enum PageRenderOptions
        None = 0, RenderHeader = 1, RenderFooter = 2, RenderContent = 4,
        RenderPdfBackground = 8, RenderPdfContent = 16,
        All = RenderHeader | RenderFooter | RenderContent
              | RenderPdfBackground | RenderPdfContent,
        RemovePage = 32

Draw one page onto your own surface:

    using CodeBrix.PdfDocuments.Drawing;

    var dr = new DocumentRenderer(doc);
    dr.PrepareDocumentProgress += (s, e) =>
        Console.WriteLine($"Laying out {e.Value} of {e.Maximum}");
    dr.PrepareDocument();

    int pageCount = dr.FormattedDocument.PageCount;
    var info = dr.FormattedDocument.GetPageInfo(1);

    // ... obtain an XGraphics for a surface of info.Width x info.Height ...
    dr.RenderPage(gfx, 1);                          // everything
    dr.RenderPage(gfx, 1, PageRenderOptions.RenderContent);   // body only

Page numbers are 1-based everywhere in this API.

Hand a prepared DocumentRenderer to a PdfDocumentRenderer to avoid laying the
document out twice:

    var pdfRenderer = new PdfDocumentRenderer { Document = doc };
    pdfRenderer.DocumentRenderer = dr;
    pdfRenderer.RenderPages(1, pageCount);

THE DDL TEXT FORMAT (DdlReader / DdlWriter)
-------------------------------------------
The whole document object model serializes to and from a readable text format.
This is genuinely useful: templates as files, golden-file regression tests, and
"show me what the model actually contains" debugging.

    public class DdlWriter : IDisposable
        public DdlWriter(Stream stream)  /  (string filename)  /  (TextWriter writer)
        public int Indent { get; set; }
        public int InitialIndent { get; set; }
        public void WriteDocument(DocumentObject documentObject)
        public void WriteDocument(DocumentObjectCollection documentObjectContainer)
        public void Flush()
        public static string WriteToString(DocumentObject docObject)
        public static string WriteToString(DocumentObject docObject, int indent)
        public static string WriteToString(DocumentObject docObject, int indent,
            int initialIndent)
        public static void WriteToFile(DocumentObject docObject, string filename)
        public static void WriteToFile(DocumentObject docObject, string filename,
            int indent)
        public static void WriteToFile(DocumentObject docObject, string filename,
            int indent, int initialIndent)
        // the same four WriteToString / WriteToFile overload sets also accept a
        // DocumentObjectCollection

    public class DdlReader : IDisposable
        public DdlReader(Stream stream)  /  (string filename)  /  (TextReader reader)
        public DdlReader(Stream stream, DdlReaderErrors errors)
        public DdlReader(string filename, DdlReaderErrors errors)
        public DdlReader(TextReader reader, DdlReaderErrors errors)
        public Document ReadDocument()
        public DocumentObject ReadObject()
        public static Document DocumentFromFile(string documentFileName)
        public static Document DocumentFromString(string ddl)
        public static DocumentObject ObjectFromFile(string documentFileName)
        public static DocumentObject ObjectFromFile(string documentFileName,
            DdlReaderErrors errors)
        public static DocumentObject ObjectFromString(string ddl)
        public static DocumentObject ObjectFromString(string ddl,
            DdlReaderErrors errors)

    public class DdlReaderErrors : IEnumerable
        public int ErrorCount { get; }
        public DdlReaderError this[int index] { get; }
        public void AddError(DdlReaderError error)

    public class DdlReaderError
        public DdlErrorLevel ErrorLevel;
        public string ErrorMessage;
        public int ErrorNumber;          // DdlReaderError.NoErrorNumber == -1
        public string SourceFile;
        public int SourceLine;
        public int SourceColumn;

    public enum DdlErrorLevel { None, Info, Warning, Error, ... }

    public class DdlParserException : Exception
        public DdlReaderError Error { get; }

Round trip:

    string ddl = DdlWriter.WriteToString(doc);
    File.WriteAllText("template.mdddl", ddl);

    var errors = new DdlReaderErrors();
    using var reader = new DdlReader("template.mdddl", errors);
    var reloaded = reader.ReadDocument();
    if (errors.ErrorCount > 0)
    {
        for (var i = 0; i < errors.ErrorCount; i++)
        {
            var e = errors[i];
            Console.WriteLine(
                $"{e.ErrorLevel} {e.SourceLine}:{e.SourceColumn} {e.ErrorMessage}");
        }
    }

Pass a DdlReaderErrors instance to collect problems; without one, a malformed
document throws DdlParserException instead.

UTILITIES AND THE REMAINING PUBLIC TYPES
----------------------------------------
Three small helpers are worth knowing about:

    public sealed class TextMeasurement          // ...DocumentObjectModel
        public TextMeasurement(XGraphics graphics, Font font)
        public Font Font { get; set; }           // null throws ArgumentNullException
        public XSize MeasureString(string text)
        public XSize MeasureString(string text, UnitType unitType)

    // Measure a DOM Font without rendering - e.g. to size a column to its
    // widest value before adding rows.
    using var gfx = XGraphics.CreateMeasureContext(
        new XSize(2000, 2000), XGraphicsUnit.Point, XPageDirection.Downwards);
    var measurer = new TextMeasurement(gfx, doc.Styles["Normal"].Font);
    XSize size = measurer.MeasureString("Widest cell value", UnitType.Centimeter);

    public class DocumentRelations                // ...DocumentObjectModel
        public static DocumentObject GetParent(DocumentObject documentObject)
        public static DocumentObject GetParentOfType(DocumentObject documentObject,
            Type type)
        public static bool HasParentOfType(DocumentObject documentObject, Type type)
        // Walk up from any element to the Cell, Section or Document holding it.

    public sealed class DdlEncoder                // ...DocumentObjectModel
        public static string StringToText(string str)
        public static string StringToLiteral(string str)
        // Escape a string for hand-written DDL.

    public sealed class Chars                     // ...DocumentObjectModel
        // Character constants (Chars.CR, Chars.LF, Chars.HT,
        // Chars.NonBreakableSpace, ...) used by the DDL scanner.

    public static class ProductVersionInfo        // CodeBrix.PdfDocCreate
        // Assembly identity constants used for PDF Producer metadata.

The rest of the assembly's public types are plumbing you should not need and
should not build on. They are public because the port kept the upstream
accessibility, not because they are a supported surface:

  - the DOM metadata layer: DocumentObject and DocumentObjectCollection (the
    abstract bases of everything above), Meta, ValueDescriptor,
    ValueDescriptorCollection and the GV enum
  - the visitor layer: DocumentObjectVisitor, VisitorBase, PdfFlattenVisitor,
    RtfFlattenVisitor, MergedCellList, CellComparer
  - the layout layer inside Rendering: Area, LayoutInfo, RenderInfo and the
    per-element *RenderInfo types
  - the chart bridge in Rendering.ChartMapper: ChartMapper, AxisMapper,
    PlotAreaMapper, SeriesCollectionMapper, XValuesMapper, LineFormatMapper
  - the chart element collections behind the friendly API: ChartObject (the
    base class of Series, Axis, Legend, PlotArea, TextArea and friends),
    SeriesElements (Series.Elements) and XSeriesElements
  - ImageHelper, which resolves image names against Document.ImagePath

Use the documented API above instead; these types change with the renderer.

INHERITANCE AND FLATTENING (WHY A PROPERTY READS BACK "EMPTY")
--------------------------------------------------------------
Almost every property on this model starts UNASSIGNED rather than at a default.
Unassigned values are filled in during the layout pass, in this order:

  paragraph/cell/row  ->  its style  ->  the style's base style  ->  "Normal"
  section PageSetup   ->  previous section's PageSetup  ->  the built-in default
  section headers     ->  previous section's headers
  column width        ->  Columns.Width  ->  2.5 cm
  row height/rule/alignment -> Rows.Height / HeightRule / VerticalAlignment

Consequences worth remembering:

  - Reading para.Format.Font.Size before rendering returns the value you set,
    or an unassigned Unit - NOT the size the text will actually be drawn at.
  - Assigning any value to an inherited property pins it and stops inheritance
    for that one property only.
  - The layout pass MUTATES the document with the flattened values, so a
    Document should be rendered once. Build a fresh Document (or Clone() one)
    for a second render.

================================================================================

COMPLETE EXAMPLES
=================

Example 1: Styled report with a repeating table header
------------------------------------------------------
    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.DocumentObjectModel.Tables;
    using CodeBrix.PdfDocCreate.Rendering;

    var doc = new Document();
    doc.Info.Title = "Sales Report";
    doc.Info.Author = "Accounts Department";

    // Styles first - everything inherits from them
    doc.Styles["Normal"].Font.Name = "Arial";
    doc.Styles["Normal"].Font.Size = 10;
    doc.Styles["Normal"].ParagraphFormat.SpaceAfter = 4;

    var titleStyle = doc.AddStyle("Title", StyleNames.Normal);
    titleStyle.Font.Size = 24;
    titleStyle.Font.Bold = true;
    titleStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;
    titleStyle.ParagraphFormat.SpaceAfter = 12;

    var h1 = doc.Styles[StyleNames.Heading1];
    h1.Font.Size = 16;
    h1.Font.Bold = true;
    h1.ParagraphFormat.SpaceBefore = 12;
    h1.ParagraphFormat.KeepWithNext = true;

    // Section: US Letter, 1 inch margins
    var section = doc.AddSection();
    section.PageSetup.PageFormat = PageFormat.Letter;
    section.PageSetup.TopMargin = Unit.FromInch(1);
    section.PageSetup.BottomMargin = Unit.FromInch(1);
    section.PageSetup.LeftMargin = Unit.FromInch(1);
    section.PageSetup.RightMargin = Unit.FromInch(1);

    section.AddParagraph("Quarterly Sales Report", "Title");
    section.AddParagraph("Regional breakdown", StyleNames.Heading1);

    var salesRows = new[]
    {
        ("Widgets", 45000m, 52000m),
        ("Gadgets", 31000m, 29500m),
    };

    var table = section.AddTable();
    table.Borders.Visible = true;
    table.Borders.Width = 0.5;
    table.Borders.Color = Colors.Silver;
    table.AddColumn(Unit.FromCentimeter(6));
    table.AddColumn(Unit.FromCentimeter(4));
    table.AddColumn(Unit.FromCentimeter(4));

    var header = table.AddRow();
    header.HeadingFormat = true;              // repeats on every page
    header.Shading.Color = Colors.LightGray;
    header.Format.Font.Bold = true;
    header.Cells[0].AddParagraph("Product");
    header.Cells[1].AddParagraph("Q1 Sales");
    header.Cells[2].AddParagraph("Q2 Sales");

    foreach (var (product, q1, q2) in salesRows)
    {
        var row = table.AddRow();
        row.Cells[0].AddParagraph(product);
        row.Cells[1].AddParagraph(q1.ToString("C0"));
        row.Cells[1].Format.Alignment = ParagraphAlignment.Right;
        row.Cells[2].AddParagraph(q2.ToString("C0"));
        row.Cells[2].Format.Alignment = ParagraphAlignment.Right;
    }

    // Footer with "Page N of M"
    var footer = section.Footers.Primary;
    var fp = footer.AddParagraph();
    fp.AddText("Page ");
    fp.AddPageField();
    fp.AddText(" of ");
    fp.AddNumPagesField();
    fp.Format.Alignment = ParagraphAlignment.Center;

    var renderer = new PdfDocumentRenderer { Document = doc };
    renderer.RenderDocument();
    renderer.Save("SalesReport.pdf");

Example 2: In-memory PDF for a web API
--------------------------------------
    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.Rendering;

    public byte[] GenerateInvoicePdf(InvoiceDto invoice)
    {
        var doc = new Document();
        doc.Info.Title = $"Invoice {invoice.Number}";

        var section = doc.AddSection();
        section.AddParagraph($"Invoice #{invoice.Number}", StyleNames.Heading1);
        section.AddParagraph($"Date: {invoice.Date:yyyy-MM-dd}");
        section.AddParagraph($"Total: {invoice.Total:C}");

        var renderer = new PdfDocumentRenderer { Document = doc };
        renderer.RenderDocument();

        using var stream = new MemoryStream();
        renderer.Save(stream, closeStream: false);
        return stream.ToArray();
    }

Example 3: Front matter and body with different numbering and headers
---------------------------------------------------------------------
    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.DocumentObjectModel.Fields;
    using CodeBrix.PdfDocCreate.Rendering;

    var doc = new Document();
    doc.Info.Title = "User Manual";

    // --- Front matter: roman numerals, no header on the title page ---
    var front = doc.AddSection();
    front.PageSetup.PageFormat = PageFormat.A4;
    front.PageSetup.StartingNumber = 1;
    front.PageSetup.DifferentFirstPageHeaderFooter = true;

    front.Footers.FirstPage.AddParagraph("");          // title page: no number
    var frontFooter = front.Footers.Primary.AddParagraph();
    frontFooter.Format.Alignment = ParagraphAlignment.Center;
    var frontPage = frontFooter.AddPageField();
    frontPage.Format = "roman";                        // i, ii, iii

    front.AddParagraph("User Manual", StyleNames.Heading1);
    front.AddPageBreak();
    front.AddParagraph("Contents", StyleNames.Heading2);

    // --- Body: arabic numerals restarting at 1, mirrored margins ---
    var body = doc.AddSection();
    body.PageSetup.SectionStart = BreakType.BreakOddPage;
    body.PageSetup.StartingNumber = 1;
    body.PageSetup.MirrorMargins = true;
    body.PageSetup.OddAndEvenPagesHeaderFooter = true;

    var recto = body.Headers.Primary.AddParagraph();
    recto.Format.Alignment = ParagraphAlignment.Right;
    recto.AddInfoField(InfoFieldType.Title);

    var verso = body.Headers.EvenPage.AddParagraph("User Manual");
    verso.Format.Alignment = ParagraphAlignment.Left;

    var bodyFooter = body.Footers.Primary.AddParagraph();
    bodyFooter.Format.Alignment = ParagraphAlignment.Center;
    bodyFooter.AddPageField();

    var intro = body.AddParagraph("Introduction", StyleNames.Heading1);
    intro.Format.PageBreakBefore = true;
    intro.AddBookmark("intro");

    var text = body.AddParagraph("The system was audited in 2026");
    text.AddFootnote("Audit report AR-2026-14, section 3.");
    text.AddText(".");

    var pointer = body.AddParagraph();
    pointer.AddText("The introduction begins on page ");
    pointer.AddPageRefField("intro");
    pointer.AddText(".");

    var renderer = new PdfDocumentRenderer(unicode: true) { Document = doc };
    renderer.Language = "en-US";
    renderer.RenderDocument();
    renderer.Save("Manual.pdf");

Example 4: An image and a floating side note
--------------------------------------------
    using CodeBrix.Imaging.PixelFormats;
    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.DocumentObjectModel.Shapes;
    using CodeBrix.PdfDocCreate.Rendering;
    using CodeBrix.PdfDocuments.Drawing;
    using CodeBrix.PdfDocuments.Utils;

    ImageSource.ImageSourceImpl ??= new ImagingImageSource<Rgba32>();

    var doc = new Document();
    var section = doc.AddSection();

    var image = section.AddImage(ImageSource.FromFile("diagram.png"));
    image.LockAspectRatio = true;
    image.Width = Unit.FromCentimeter(10);

    var caption = section.AddParagraph("Figure 1: system overview");
    caption.Format.Font.Size = 8;
    caption.Format.Font.Italic = true;
    caption.Format.LineSpacingRule = LineSpacingRule.Exactly;
    caption.Format.LineSpacing = 9;            // fontSize * 1.1, hugs the image

    var note = section.AddTextFrame();
    note.Width = Unit.FromCentimeter(5);
    note.Height = Unit.FromCentimeter(3);
    note.RelativeHorizontal = RelativeHorizontal.Margin;
    note.RelativeVertical = RelativeVertical.Paragraph;
    note.Left = ShapePosition.Right;
    note.MarginLeft = note.MarginTop = Unit.FromMillimeter(3);
    note.FillFormat.Color = Colors.LightYellow;
    note.LineFormat.Width = 0.5;
    note.LineFormat.Color = Colors.Orange;
    note.WrapFormat.Style = WrapStyle.TopBottom;
    note.WrapFormat.DistanceLeft = Unit.FromMillimeter(4);
    note.AddParagraph("Diagrams are not to scale.");

    var renderer = new PdfDocumentRenderer { Document = doc };
    renderer.RenderDocument();
    renderer.Save("Figures.pdf");

Example 5: A chart in a section
-------------------------------
    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.DocumentObjectModel.Shapes.Charts;
    using CodeBrix.PdfDocCreate.Rendering;

    var doc = new Document();
    var section = doc.AddSection();
    section.PageSetup.Orientation = Orientation.Landscape;
    section.AddParagraph("Revenue by quarter", StyleNames.Heading1);

    var chart = section.AddChart(ChartType.Column2D);
    chart.Width = Unit.FromCentimeter(20);
    chart.Height = Unit.FromCentimeter(10);

    chart.XValues.AddXSeries().Add("Q1", "Q2", "Q3", "Q4");

    var widgets = chart.SeriesCollection.AddSeries();
    widgets.Name = "Widgets";
    widgets.Add(45000, 52000, 48500, 61000);
    widgets.FillFormat.Color = Colors.SteelBlue;

    var trend = chart.SeriesCollection.AddSeries();
    trend.Name = "Trend";
    trend.ChartType = ChartType.Line;               // mixed chart
    trend.Add(44000, 49000, 51000, 58000);
    trend.MarkerStyle = MarkerStyle.Circle;
    trend.MarkerSize = 4;
    trend.LineFormat.Color = Colors.DarkOrange;
    trend.LineFormat.Width = 1.25;

    chart.YAxis.HasMajorGridlines = true;
    chart.YAxis.TickLabels.Format = "#,##0";
    chart.XAxis.MajorTickMark = TickMarkType.Outside;
    chart.BottomArea.AddLegend();

    var renderer = new PdfDocumentRenderer { Document = doc };
    renderer.RenderDocument();
    renderer.Save("Revenue.pdf");

Example 6: Save the model as DDL and reload it as a template
------------------------------------------------------------
    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.DocumentObjectModel.IO;
    using CodeBrix.PdfDocCreate.Rendering;

    // Author the shell once and keep it as a text file
    var template = new Document();
    template.Styles[StyleNames.Normal].Font.Name = "Arial";
    var s = template.AddSection();
    s.PageSetup.PageFormat = PageFormat.Letter;
    s.Headers.Primary.AddParagraph("ACME Corporation");
    DdlWriter.WriteToFile(template, "letterhead.mdddl");

    // Later - reload, fill in and render
    var errors = new DdlReaderErrors();
    Document doc;
    using (var reader = new DdlReader("letterhead.mdddl", errors))
    {
        doc = reader.ReadDocument();
    }

    if (errors.ErrorCount > 0)
    {
        for (var i = 0; i < errors.ErrorCount; i++)
        {
            Console.WriteLine(errors[i].ToString());
        }
    }

    doc.LastSection.AddParagraph("Dear customer,");
    doc.LastSection.AddParagraph("Your order has shipped.");

    var renderer = new PdfDocumentRenderer { Document = doc };
    renderer.RenderDocument();
    renderer.Save("letter.pdf");

================================================================================

MINIMUM VIABLE PROJECT
======================

    dotnet new console -n MyPdfApp --framework net10.0
    cd MyPdfApp
    dotnet add package CodeBrix.PdfDocCreate.MitLicenseForever

MyPdfApp.csproj:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>disable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.PdfDocCreate.MitLicenseForever" />
      </ItemGroup>
    </Project>

Program.cs:

    using CodeBrix.PdfDocCreate.DocumentObjectModel;
    using CodeBrix.PdfDocCreate.Rendering;

    var doc = new Document();
    doc.Info.Title = "Hello";

    doc.Styles[StyleNames.Normal].Font.Name = "Arial";
    doc.Styles[StyleNames.Normal].Font.Size = 11;

    var section = doc.AddSection();
    section.PageSetup.PageFormat = PageFormat.Letter;

    section.AddParagraph("Hello, PDF!", StyleNames.Heading1);
    section.AddParagraph("Built from a document model, not from coordinates.");

    var footer = section.Footers.Primary.AddParagraph();
    footer.Format.Alignment = ParagraphAlignment.Center;
    footer.AddText("Page ");
    footer.AddPageField();

    var renderer = new PdfDocumentRenderer { Document = doc };
    renderer.RenderDocument();
    renderer.Save("output.pdf");

    Console.WriteLine($"Created output.pdf ({renderer.PageCount} page(s))");

Build and run:

    dotnet build
    dotnet run

================================================================================

PERFORMANCE TIPS
================

1. DEFINE STYLES BEFORE ADDING CONTENT. Styles are inherited, so changes to
   "Normal" affect every style based on it. Setting them up front also avoids
   re-specifying the same font on thousands of paragraphs.

2. STYLE, DO NOT REPEAT. Setting Format.Font.Size on 5,000 paragraphs stores
   5,000 values that all have to be flattened. One style reference each is
   cheaper to build, cheaper to lay out and smaller in DDL.

3. LAY OUT ONCE. RenderDocument() runs the layout pass and then renders every
   page. If you need the page count first, call PrepareRenderPages(), read
   PageCount, then RenderPages(...) - do not construct a second renderer.

4. REUSE A PREPARED DocumentRenderer. If you already called
   PrepareDocument() on a DocumentRenderer (for a preview, say), assign it to
   PdfDocumentRenderer.DocumentRenderer instead of letting the PDF renderer
   lay the document out a second time.

5. SIZE IMAGES BEFORE EMBEDDING. The image bytes go into the PDF at their
   native pixel dimensions regardless of the Width/Height you set on the
   Image shape. Resize with CodeBrix.Imaging first and pass the result through
   ImageSource.FromStream / FromBinary; scaling a 4000-pixel photo down to
   8 cm on the page does not shrink the file.

6. SHARE ONE IImageSource FOR A REPEATED IMAGE. A logo used in every section's
   header should be created once and assigned to each Image.Source, so the
   image data is embedded once.

7. BUILD A FRESH Document PER RENDER. The layout pass mutates the document
   with flattened values. For a server that renders the same template
   repeatedly, keep the template as DDL text (DdlWriter.WriteToString) and
   parse a new Document per request - parsing is far cheaper than the risk of
   re-rendering a mutated model.

8. STREAM, DO NOT SPOOL. In a web scenario call Save(stream, false) into a
   MemoryStream and return the bytes rather than writing a temp file.

9. TABLE STRUCTURE COSTS MORE THAN TABLE TEXT. Every Cell is a document
   object with its own format, borders and shading. A 50-column table is
   expensive; prefer tab stops for simple aligned columns.

================================================================================

COMMON PITFALLS TO AVOID
========================

1. DO NOT confuse the NuGet package name with the namespace.
   Package: CodeBrix.PdfDocCreate.MitLicenseForever
   Namespaces: CodeBrix.PdfDocCreate.DocumentObjectModel[.Tables|.Shapes|
   .Shapes.Charts|.Fields|.IO], CodeBrix.PdfDocCreate.Rendering.

2. DO NOT use MigraDoc or MigraDocCore namespaces. Even though this is a port,
   every namespace is CodeBrix.PdfDocCreate.*.

3. DO NOT forget to call RenderDocument() on PdfDocumentRenderer before
   saving. The document model must be rendered to generate the actual PDF;
   PdfDocument is null until it is.

4. DO NOT call section.AddImage("photo.png"). There is NO string overload.
   AddImage takes an IImageSource:
   ImageSource.ImageSourceImpl ??= new ImagingImageSource<Rgba32>();
   var img = section.AddImage(ImageSource.FromFile("photo.png"));
   Calling ImageSource.FromFile without setting ImageSourceImpl first throws
   NullReferenceException, because nothing else in this package installs it.

5. DO NOT call doc.AddStyle(...) with a built-in style name. Heading1 through
   Heading9, Normal, DefaultParagraphFont, Footnote, Header, Footer and
   Hyperlink all already exist on a new Document.
     AddStyle does not throw on a duplicate name - it REPLACES the existing
   style with a fresh one, and hands you back an object that is NOT the one
   stored in the document. Every property you then set is written to an orphan
   and has no effect, so the document silently renders unstyled. Worse, the
   replacement discards the built-in style's ParagraphFormat.OutlineLevel
   (which drives PDF outline/bookmark generation) and re-bases it, breaking the
   Heading1 -> Heading2 -> Heading3 inheritance chain.
     FIX: fetch the style instead - var h1 = doc.Styles["Heading1"]; - and set
   properties on that. Reserve AddStyle for names of your own.

6. DO NOT assume the default page size is US Letter. It is A4
   (21 x 29.7 cm) with 2.5 / 2 / 2.5 / 2.5 cm margins. Set PageFormat,
   or PageWidth and PageHeight, on every section that needs something else.

7. DO NOT set PageWidth and PageHeight AND expect PageFormat to still apply.
   PageFormat only supplies a dimension you did not assign yourself. Assign
   both and it is ignored; assign one and the other comes from the format.
   Also note Ledger (1224 x 792 pt) and P11x17 (792 x 1224 pt) are the same
   sheet in opposite orientations.

8. DO NOT expect Headers.FirstPage or Headers.EvenPage to appear on their own.
   They are ignored until the matching PageSetup flag is set:
   DifferentFirstPageHeaderFooter for FirstPage, OddAndEvenPagesHeaderFooter
   for EvenPage. Content added to them without the flag simply never renders.

9. DO NOT assume a new section starts with clean headers and page setup. A
   section that defines neither inherits BOTH from the previous section. Give
   the new section its own PageSetup values, and an empty
   Headers.Primary.AddParagraph(""), when you want a clean slate.

10. DO NOT create a Chart or a TextFrame without setting Width and Height. A
    Chart has no default size at all and renders as nothing; a TextFrame
    defaults to 1 inch by 1 inch, which is almost never what you wanted.

11. DO NOT use LineSpacingRule.AtLeast on a paragraph whose text is much
    smaller than its LineSpacing value. A common case: a tiny image-credit or
    caption line meant to sit snug beneath an image.
      With AtLeast, the renderer reserves a full line box of at least the
    specified LineSpacing height, then places the small text at the BOTTOM of
    that box. The reserved-but-empty space appears as a large gap ABOVE the
    text (roughly LineSpacing minus the glyph height), and it also pushes the
    NEXT paragraph further down. The result looks like mysterious leading that
    no SpaceBefore / SpaceAfter value seems to account for.
      FIX: use LineSpacingRule.Exactly with a LineSpacing close to the font
    size (e.g. fontSize * 1.1). Reserve AtLeast for body text where you
    genuinely WANT a guaranteed minimum leading regardless of the content.

12. DO NOT write a six-digit "0x" color string and expect it to be opaque.
    Color.Parse("0xc0c0c0") is FULLY TRANSPARENT, not light grey.
      "0x" introduces a packed 0xAARRGGBB integer, so the alpha byte comes
    FIRST. Supply only six digits and alpha stays at zero. Nothing throws:
    shading, borders and text simply do not appear, which reads as a layout bug
    rather than a color bug and can cost a long time to find.
      FIX: write the alpha explicitly - "0xFFC0C0C0" - or use the CSS form
    "#c0c0c0", or use Color.FromRgb(192, 192, 192). All three are opaque. Note
    that new Color(192, 192, 192) is opaque too; only the six-digit "0x" STRING
    form is affected.

13. DO NOT read a property back and believe it. Unassigned properties inherit
    during the layout pass, so before RenderDocument() a getter returns what
    you set (or an unassigned value), not the effective value. Shading.Visible
    is the classic case: it reads false on a paragraph whose shading will in
    fact be painted, because setting Color alone is enough.

14. DO NOT render the same Document twice. The layout pass flattens inherited
    values INTO the model, so the second render starts from a mutated
    document. Build a new Document (or Clone() before the first render).

15. DO NOT read PdfDocumentRenderer.PageCount before laying the document out.
    It dereferences the internal DocumentRenderer and throws
    NullReferenceException on a fresh renderer. Call PrepareRenderPages() or
    RenderDocument() first.

16. DO NOT assume a bare number is a physical size. Every Unit-typed property
    treats a bare number as POINTS: row.Height = 14 is 14 points, not 14 mm.
    Use Unit.FromCentimeter / FromMillimeter / FromInch, or the string forms
    "2.5cm", "18mm", "1in", "4pc".

17. DO NOT add a Barcode and expect to see it. No renderer handles the Barcode
    shape; it produces nothing on the page. Draw bar codes with the
    CodeBrix.PdfDocuments bar-code API instead.

18. DO NOT expect non-Windows-1252 text to survive the default encoding. Use
    new PdfDocumentRenderer(unicode: true) for anything outside Latin-1, and
    remember that a font family that is not installed is SILENTLY SUBSTITUTED
    rather than reported (see FONTS in the repository-root AGENT-README).

19. DO NOT fake bullets with "* " prefixes or borderless tables. Use
    ParagraphFormat.ListInfo - the renderer then handles numbering, list
    continuation across page breaks and hanging indents.

20. DO NOT assume the generated PDF is not text-searchable. There is no text
    EXTRACTION API here, but that is a statement about reading PDFs. Text added
    through the document model is embedded as real text with correct word
    spacing, so the output is searchable, selectable and accessible, and can be
    diffed as text in a regression suite.

================================================================================

WHAT THIS PACKAGE DOES NOT DO
=============================

  - Render to RTF. Despite the format's presence in the upstream lineage, no
    RTF renderer ships in this package; PdfDocumentRenderer (PDF) and
    DocumentRenderer (draw onto an XGraphics) are the only renderers.
  - Render bar codes. The Barcode shape type exists but nothing draws it.
  - Read an existing PDF, extract its text, or convert a PDF back into a
    Document. This package writes; use CodeBrix.PdfDocuments to read.
  - Contour text wrap around a shape. WrapStyle offers TopBottom, None and
    Through only.
  - Floats, columns-within-a-section, or multi-column text flow.
  - Automatic table of contents generation. You can build one - bookmarks,
    PageRefField and tab stops with dot leaders are all here - but nothing
    collects the headings for you.
  - Fill in an existing PDF's form fields, sign a document, or validate PDF/A.
    Form filling and security live in CodeBrix.PdfDocuments and can be applied
    to PdfDocumentRenderer.PdfDocument after rendering.
  - HTML or Markdown input. Use CodeBrix.PdfDocCreate.Html2Pdf or
    CodeBrix.PdfDocCreate.Markdown2Pdf, both of which compose onto this model.
  - Read or write Word (.docx) or Excel (.xlsx) files.
  - Draw at exact coordinates. That is CodeBrix.PdfDocuments' job; the two
    combine through PdfDocumentRenderer.PdfDocument.

================================================================================

WORKING EXAMPLES ON GITHUB
==========================

The repository's test project exercises this package end to end. Base URL:
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/

  A full document built from the object model - styles, headings, paragraphs,
  inline runs, images through ImageSource, PDF bookmarks from OutlineLevel:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/DocumentObjectModel/CreatePdfTests.cs

  Tables: borders, cell merging, vertical alignment, shading:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Rendering/TestTable.cs

  Rendering behaviour: paragraphs, text formatting, alignment, tabs, borders:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Rendering/RenderingTests.cs
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Rendering/TestLayout.cs
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Rendering/TestParagraphRenderer.cs
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Rendering/TestParagraphIterator.cs

  A vector image source: laid out at its natural size in points, honouring an
  explicit width with the aspect ratio locked, drawn once, and embedding no
  image XObject at all:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Rendering/VectorImageSourceTests.cs

  Document outlines (the bookmarks OutlineLevel produces), verified on the
  rendered PdfDocument:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Outlines/OutlineTests.cs

  Applying security to the PdfDocument a render produced:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocuments.Tests/Security/PdfSecurity.cs

  Two packages that build ON this document model, and are worth reading as
  large worked examples of composing onto it:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests/

HOW TO USE: Fetch the raw file content from GitHub using a URL like:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/{path}
For example:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/tests/CodeBrix.PdfDocuments.Tests/Rendering/TestTable.cs

================================================================================

QUICK REFERENCE CARD
====================

--- Install ---
    dotnet add package CodeBrix.PdfDocCreate.MitLicenseForever
Namespaces:     CodeBrix.PdfDocCreate.DocumentObjectModel (+ .Tables, .Shapes,
                .Shapes.Charts, .Fields, .IO), CodeBrix.PdfDocCreate.Rendering

--- Document ---
Create:         var doc = new Document()
Metadata:       doc.Info.Title / .Author / .Subject / .Keywords
Add style:      doc.AddStyle("MyName", StyleNames.Normal)   // custom names ONLY
Built-in style: doc.Styles[StyleNames.Heading1]             // NEVER AddStyle these
Built-ins:      Normal, Heading1..Heading9, DefaultParagraphFont, Footnote,
                Header, Footer, Hyperlink
CMYK output:    doc.UseCmykColor = true
Footnotes:      doc.FootnoteLocation / .FootnoteNumberingRule
                .FootnoteNumberStyle / .FootnoteStartingNumber
Default tabs:   doc.DefaultTabStop = "1cm"        // else 1.25 cm

--- Section and page setup ---
Add section:    var s = doc.AddSection()
Page size:      s.PageSetup.PageFormat = PageFormat.Letter   // default is A4
                s.PageSetup.PageWidth / .PageHeight          // or explicit
Orientation:    s.PageSetup.Orientation = Orientation.Landscape
Margins:        s.PageSetup.TopMargin / BottomMargin / LeftMargin / RightMargin
                (defaults 2.5 / 2 / 2.5 / 2.5 cm)
Mirror:         s.PageSetup.MirrorMargins = true
Numbering:      s.PageSetup.StartingNumber = 1
Section break:  s.PageSetup.SectionStart = BreakType.BreakOddPage
Header dist:    s.PageSetup.HeaderDistance / .FooterDistance   (default 1.25 cm)
First page:     s.PageSetup.DifferentFirstPageHeaderFooter = true
Odd/even:       s.PageSetup.OddAndEvenPagesHeaderFooter = true
Page break:     s.AddPageBreak()

--- Headers and footers ---
Primary:        s.Headers.Primary.AddParagraph("text")
                s.Footers.Primary.AddParagraph()
First / even:   s.Headers.FirstPage / s.Headers.EvenPage  (need the flags above)

--- Paragraphs ---
Add:            s.AddParagraph("text") / AddParagraph("text", "StyleName")
Inline:         para.AddText(...) / AddFormattedText(text, TextFormat.Bold)
                para.AddLineBreak() / AddTab() / AddSpace(n)
Symbols:        para.AddCharacter(SymbolName.EmDash)
Alignment:      para.Format.Alignment = ParagraphAlignment.Justify
Spacing:        para.Format.SpaceBefore / .SpaceAfter
Indents:        para.Format.LeftIndent / .RightIndent / .FirstLineIndent
Leading:        para.Format.LineSpacingRule = LineSpacingRule.Exactly
                para.Format.LineSpacing = 13
Page flow:      para.Format.PageBreakBefore / .KeepTogether / .KeepWithNext
                para.Format.WidowControl
Outline entry:  para.Format.OutlineLevel = OutlineLevel.Level1

--- Lists, tabs, decoration ---
Bullet list:    para.Format.ListInfo.ListType = ListType.BulletList1
                para.Format.ListInfo.ContinuePreviousList = true
Number list:    ListType.NumberList1 / NumberList2 / NumberList3
Tab stop:       para.Format.AddTabStop("8cm", TabAlignment.Right, TabLeader.Dots)
                para.AddTab()
Shading:        para.Format.Shading.Color = Colors.LightYellow
Borders:        para.Format.Borders.Width / .Color / .Top / .Bottom

--- Fields ---
Page number:    para.AddPageField()          Total pages: para.AddNumPagesField()
Section:        para.AddSectionField()       Section pages: AddSectionPagesField()
Cross ref:      para.AddBookmark("ch1"); other.AddPageRefField("ch1")
Date:           para.AddDateField("yyyy-MM-dd")
Metadata:       para.AddInfoField(InfoFieldType.Title)
Numeral style:  field.Format = "roman" | "ROMAN" | "alphabetic" | "ALPHABETIC"

--- Links and footnotes ---
Web link:       para.AddHyperlink("https://x.com", HyperlinkType.Web).AddText("x")
Internal link:  para.AddHyperlink("ch1", HyperlinkType.Bookmark).AddText("go")
Footnote:       para.AddFootnote("note text")

--- Tables ---
Add table:      var t = s.AddTable()
Columns:        t.AddColumn(Unit.FromCentimeter(4))   // default width 2.5 cm
Rows:           var r = t.AddRow()
Cell content:   r.Cells[0].AddParagraph("text")
Repeat header:  r.HeadingFormat = true
Keep rows:      r.KeepWith = 2;  t.KeepTogether = true
Indent table:   t.Rows.LeftIndent = "1cm"
Merge:          cell.MergeRight = 1; cell.MergeDown = 1
Shading:        cell.Shading.Color = Colors.LightBlue
Row height:     r.HeightRule = RowHeightRule.Exactly; r.Height = 14
Align:          r.VerticalAlignment = VerticalAlignment.Center
Ranges:         t.SetShading(clm, row, clms, rows, color)
                t.SetEdge(clm, row, clms, rows, Edge.Box, BorderStyle.Single, 0.75)

--- Images ---
Install once:   ImageSource.ImageSourceImpl ??= new ImagingImageSource<Rgba32>()
Add:            var img = s.AddImage(ImageSource.FromFile("photo.png"))
                ImageSource.FromStream(name, () => stream)
                ImageSource.FromBinary(name, () => bytes)
Size:           img.LockAspectRatio = true; img.Width = "8cm"
Crop:           img.PictureFormat.CropLeft / CropRight / CropTop / CropBottom

--- Text frames and charts ---
Text frame:     var f = s.AddTextFrame(); f.Width = "6cm"; f.Height = "3cm"
Anchor:         f.RelativeHorizontal = RelativeHorizontal.Margin
                f.RelativeVertical = RelativeVertical.Paragraph
                f.Left = ShapePosition.Right; f.Top = "0.5cm"
Look:           f.FillFormat.Color / f.LineFormat.Width / .Color / .DashStyle
Wrap:           f.WrapFormat.Style = WrapStyle.TopBottom; .DistanceLeft = "4mm"
Chart:          var c = s.AddChart(ChartType.Column2D)
                c.Width = "16cm"; c.Height = "9cm"      // REQUIRED
Categories:     c.XValues.AddXSeries().Add("Q1", "Q2", "Q3", "Q4")
Series:         var ser = c.SeriesCollection.AddSeries();
                ser.Name = "Widgets"; ser.Add(1.0, 2.0, 3.0)
Axes:           c.YAxis.HasMajorGridlines = true; c.YAxis.TickLabels.Format = "#,##0"
                c.XAxis.Title.Caption = "Quarter"
Labels:         c.HasDataLabel = true; c.DataLabel.Type = DataLabelType.Value
Legend:         c.BottomArea.AddLegend()
Types:          Line, Column2D, ColumnStacked2D, Area2D, Bar2D, BarStacked2D,
                Pie2D, PieExploded2D

--- Colors and units ---
Named:          Colors.SteelBlue  (141 constants)
RGB:            Color.FromRgb(255, 253, 231) / new Color(255, 253, 231)
Alpha:          Color.FromArgb(128, 255, 253, 231) / new Color(0xFFFFFDE7)
CMYK:           Color.FromCmyk(0, 1, 9, 0)
Parse:          Color.Parse("#fffde7")   // NOT "0xfffde7" - that is transparent
Units:          Unit.FromCentimeter/FromMillimeter/FromInch/FromPoint/FromPica
                or the strings "2.5cm", "18mm", "1in", "12pt", "4pc"
                a bare number is POINTS

--- Rendering ---
Render:         var r = new PdfDocumentRenderer { Document = doc }
Unicode text:   new PdfDocumentRenderer(unicode: true)
Language tag:   r.Language = "en-US"
Lay out + draw: r.RenderDocument()
Save:           r.Save("file.pdf") / r.Save(stream, closeStream: false)
Low level:      r.PdfDocument            // security, annotations, merging
Partial:        r.PrepareRenderPages(); r.RenderPages(1, 10)
Page count:     r.PageCount              // only after laying out
Preview:        var dr = new DocumentRenderer(doc); dr.PrepareDocument();
                dr.RenderPage(gfx, pageNumber)
Progress:       dr.PrepareDocumentProgress += (s, e) => ... e.Value / e.Maximum

--- DDL text format ---
Write:          DdlWriter.WriteToString(doc) / WriteToFile(doc, "t.mdddl")
Read:           DdlReader.DocumentFromFile("t.mdddl")
                DdlReader.DocumentFromString(ddl)
Collect errors: new DdlReader(path, new DdlReaderErrors()).ReadDocument()

Target: .NET 10 or later

================================================================================
