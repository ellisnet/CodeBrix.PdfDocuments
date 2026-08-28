================================================================================
AGENT-README: CodeBrix.PdfDocCreate.Html2Pdf
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.PdfDocCreate.Html2Pdf renders author-created HTML pages with CSS
styling into PDF documents. HTML parses with CodeBrix.MarkupParse; the CSS
dialect documented below is applied with real selector matching
(CodeBrix.MarkupParse's selector engine), cascade, specificity and inheritance
(stylesheets parse with CodeBrix.StyleSheetParse); the result is composed onto
the CodeBrix.PdfDocCreate document object model, whose renderer performs all
layout (line breaking, pagination, tables). Target: .NET 10 or later.

All text renders with the CodeBrix.Platform.Fonts package fonts (Roboto,
Merriweather, RobotoMono - plus any fonts you register) and NEVER with
operating-system fonts, so output is byte-comparable on every operating
system. Images embed in every format CodeBrix.Imaging decodes; SVG (files,
data: URIs and inline <svg>) is placed as PDF VECTOR CONTENT by default - or
rasterized on request - identically on Windows, macOS and Linux, with NO native
dependency of any kind.

DESIGN SCOPE: this is for HTML/CSS an author writes FOR PDF generation. It is
NOT a browser: floats, positioning, flexbox/grid, JavaScript, media queries,
CSS variables and calc() are out of scope. Unsupported CSS never fails a
render - it is ignored and reported in the result's Warnings collection.

The public surface is eight types in two namespaces: HtmlPdfRenderer,
HtmlRenderOptions, HtmlRenderResult, SvgPlacementMode, RenderWarnings,
RenderWarning and RenderWarningCategory (CodeBrix.PdfDocCreate.Html2Pdf) and
the static Html2PdfFonts (CodeBrix.PdfDocCreate.Html2Pdf.Fonts). The
".MitLicenseForever" suffix belongs to the PACKAGE ID only and never appears in
a namespace, using directive or type name.

See also (sibling packages in the same repository):
  - src/CodeBrix.PdfDocCreate.Markdown2Pdf/AGENT-README.txt - Markdown to PDF,
    which renders THROUGH this package (and inherits its SVG behavior)
  - src/CodeBrix.PdfDocCreate/AGENT-README.txt - the document model this
    package composes onto
  - AGENT-README.txt (repository root) - CodeBrix.PdfDocuments, the low-level
    PDF library underneath
  - src/CodeBrix.PdfRasterizer/AGENT-README.txt - render the PDFs this package
    produces back to images (visual regression testing)

Source Repository: https://github.com/ellisnet/CodeBrix.PdfDocuments

================================================================================

INSTALLATION
============
NuGet Package: CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever

    dotnet add package CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever

Or in a .csproj file (NuGet resolves the latest version):

    <PackageReference Include="CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever" />

NuGet dependencies (all pulled in automatically):
  - CodeBrix.PdfDocCreate.MitLicenseForever (which brings
    CodeBrix.PdfDocuments.MitLicenseForever, CodeBrix.Imaging.ApacheLicenseForever
    and CodeBrix.Compression.MitLicenseForever)
  - CodeBrix.MarkupParse.MitLicenseForever          (HTML parsing + selectors)
  - CodeBrix.StyleSheetParse.MitLicenseForever      (CSS parsing)
  - CodeBrix.Platform.Fonts.Roboto.OflLicenseForever        (sans-serif)
  - CodeBrix.Platform.Fonts.Merriweather.OflLicenseForever  (serif)
  - CodeBrix.Platform.Fonts.RobotoMono.OflLicenseForever    (monospace)
  - CodeBrix.Platform.Fonts.NotoMusic.OflLicenseForever     (music-notation
    glyphs; never a body-text default)

  Each of those font packages ships COMPANION families alongside its primary
  face - Noto Sans / Noto Serif (polytonic Greek), Noto Sans/Serif Armenian,
  Noto Sans/Serif Georgian, Iosevka and Noto Sans Mono, plus Noto Music. Every
  companion joins the per-glyph fallback chain automatically at discovery; none
  of them is ever a body-text default. Nothing has to be registered or
  configured to get them.
  - CodeBrix.Imaging.Drawing.NoSkia.ApacheLicenseForever (the fully managed SVG
    engine; brings CodeBrix.SvgParse.MsplLicenseForever - all managed code, no
    native library on any platform)

License: MIT (the font packages are OFL-licensed).

Requirements: .NET 10 or later.

FONT FILES: the font packages ship .ttf files inside their nupkgs, and this
package's buildTransitive targets copy them into the consuming application's
build output under CodeBrix.Platform.Fonts.<Name>/Fonts/ (the same layout a
CodeBrix.Platform application uses). No font installation or registration is
required - the renderer discovers them there automatically. If an application
already delivers those fonts through its own asset pipeline and the duplicate
copy is unwanted, opt out with this MSBuild property:

    <CodeBrixHtml2PdfDisableFontCopy>true</CodeBrixHtml2PdfDisableFontCopy>

(then make sure the fonts are still reachable - see FONTS below.)

################################################################################
## NO NATIVE DEPENDENCIES - NOTHING TO INSTALL, ON ANY OPERATING SYSTEM
################################################################################

Every part of this package is managed code: HTML, CSS, images, fonts AND SVG.
There is no native library, no GPU, no window system and no system font
anywhere in the chain, so Windows, macOS and Linux need NOTHING beyond the
NuGet package itself - no apt/brew/msi step, no runtime identifier, no
native-assets package.

UPGRADING FROM AN OLDER VERSION: releases before the vector SVG route
rasterized SVG through a Skia-based engine, and asked Linux applications to
reference SkiaSharp.NativeAssets.Linux or
SkiaSharp.NativeAssets.Linux.NoDependencies themselves. That requirement is
GONE. If an application referenced one of those packages ONLY for Html2Pdf, it
can drop the reference; if it uses one for its own reasons, keeping it changes
nothing here. The warning code "image.svg.nativemissing" is retired and can no
longer be raised - remove any code that pattern-matches it.

################################################################################

================================================================================

KEY NAMESPACES / USINGS
=======================

    using CodeBrix.PdfDocCreate.Html2Pdf;        // HtmlPdfRenderer, HtmlRenderOptions,
                                                 // HtmlRenderResult, SvgPlacementMode,
                                                 // RenderWarnings, RenderWarning,
                                                 // RenderWarningCategory
    using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;  // Html2PdfFonts (font discovery and
                                                 // consumer font registration)

PACKAGE-TO-NAMESPACE MAP:

  NuGet package                                      Namespace root
  -------------------------------------------------  --------------------------------
  CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever   CodeBrix.PdfDocCreate.Html2Pdf.*

================================================================================

CORE API REFERENCE
==================

--- HtmlPdfRenderer (sealed class) ---

    public HtmlRenderOptions Options { get; }     // modify BEFORE calling a render method

    public HtmlRenderResult RenderFile(string htmlFilePath, string outputPdfPath)
        // Reads the HTML file; relative stylesheet/image references resolve
        // against the HTML file's directory. Writes the PDF to outputPdfPath
        // (parent directory created if missing). Blank htmlFilePath throws
        // ArgumentException.

    public HtmlRenderResult RenderHtml(string html, string outputPdfPath, string baseDirectory = null)
        // Renders an HTML string to a PDF file. baseDirectory anchors relative
        // resource references; null = the current directory. Blank
        // outputPdfPath throws ArgumentException; null html throws
        // ArgumentNullException.

    public HtmlRenderResult RenderHtmlToBytes(string html, string baseDirectory = null)
        // Renders an HTML string and returns the PDF in HtmlRenderResult.PdfBytes.
        // No file is written.

All three methods are synchronous. Options are copied at the start of each
render, so a change to Options between calls affects the next render only, and
one HtmlPdfRenderer instance can be reused for many documents.

--- HtmlRenderResult (sealed class; every member) ---

    public string OutputFilePath { get; }   // FULL path of the written PDF;
                                            // null when the render produced bytes
    public byte[] PdfBytes { get; }         // the PDF content; null when the
                                            // render wrote to a file
    public int PageCount { get; }           // pages in the rendered document
    public string Title { get; }            // title written to the PDF metadata
                                            // (Options.DocumentTitle, else the
                                            // <title> element, else "")
    public RenderWarnings Warnings { get; } // non-fatal issues; empty on a clean render

Exactly one of OutputFilePath / PdfBytes is non-null: RenderFile and RenderHtml
fill OutputFilePath, RenderHtmlToBytes fills PdfBytes.

--- HtmlRenderOptions (sealed class; every property, with defaults) ---

    renderer.Options.PageWidthPoints  = 595;   // double; default 612 (US Letter)
    renderer.Options.PageHeightPoints = 842;   // double; default 792 (US Letter)
    renderer.Options.SetPageSize("a4");        // sets both from a named size
    renderer.Options.Landscape = true;         // bool; default false - swaps width
                                               // and height when height > width
    renderer.Options.MarginTopPoints    = 54;  // double; default 72 (1 inch)
    renderer.Options.MarginRightPoints  = 54;  // double; default 72
    renderer.Options.MarginBottomPoints = 54;  // double; default 72
    renderer.Options.MarginLeftPoints   = 54;  // double; default 72
    renderer.Options.HeaderText = "{title}";   // string; default null (no header)
    renderer.Options.FooterText = "Page {page} of {pages}";  // string; default null
    renderer.Options.AllowRemoteImages = true; // bool; default false
    renderer.Options.GenerateOutline = false;  // bool; default true (h1-h6 -> bookmarks)
    renderer.Options.SvgPlacement = SvgPlacementMode.Raster;
                                               // SvgPlacementMode; default Vector
    renderer.Options.SvgRasterScale = 3.0;     // double; default 2.0
    renderer.Options.KeepUncoveredCharacters = true;  // bool; default false
    renderer.Options.DocumentTitle = "Override Title"; // string; default null
    renderer.Options.DocumentAuthor = "Jane Doe";      // string; default null
    renderer.Options.CffSubsetMode = PdfCffSubsetMode.Sparse;
                                               // PdfCffSubsetMode (CodeBrix.PdfDocuments.Pdf);
                                               // default None - see CFF FONTS below

    public void SetPageSize(string name)
        // Recognized names (case-insensitive): letter, legal, ledger, a3, a4,
        // a5, b4, b5. Any other name throws ArgumentException.
        //   letter 612 x 792    legal 612 x 1008   ledger 792 x 1224
        //   a3 842 x 1191       a4 595 x 842       a5 420 x 595
        //   b4 709 x 1001       b5 499 x 709       (points)

Page furniture: HeaderText and FooterText render centered on every page in
the body font at 80% of the body size (minimum 6 pt), grey. The tokens
{page}, {pages} and {title} expand to the page number, total page count and
document title.

Metadata: the PDF title is Options.DocumentTitle, else the <title> element.
The PDF author is Options.DocumentAuthor, else a <meta name="author"
content="..."> element if present.

SvgPlacement decides how SVG content reaches the page:

    public enum SvgPlacementMode { Vector = 0, Raster = 1 }

  Vector (the default) writes the picture's drawing commands into the page as
    PDF operators - paths, fills, strokes, dashes, clips, transforms, text as
    REAL PDF text in the embedded face (glyph outlines only where a run
    cannot be text), gradients as PDF shading patterns and group opacity as a
    PDF transparency group - so it stays sharp at any zoom and adds NO
    image XObject to the file. Only a part PDF cannot express falls back to a
    raster; see IMAGES AND SVG below.
  Raster rasterizes the whole picture to a transparent PNG in managed code and
    embeds it as a bitmap - the placement every release before the vector
    route used.

CFF FONTS. The packaged fonts all have TrueType outlines and are embedded as
subsets. A face you add through Html2PdfFonts.AddFontFile whose glyphs live in
a CFF table (an OpenType .otf with PostScript outlines) is embedded WHOLE by
default - every version has done that - so a single such face costs its
entire program per PDF (about 60 KB for a typical text face, compressed).
Options.CffSubsetMode = PdfCffSubsetMode.Sparse opts in to a subset: the
charstrings of unused glyphs are dropped (glyph numbering, cmap, metrics and
subroutines are kept), and the program is declared as PDF 32000-1 section 9.9
asks for an OpenType CFF font - /FontFile3 /Subtype /OpenType on a
/CIDFontType0 - which raises the file to PDF 1.6 when it was lower. The value
is handed to PdfDocumentOptions.CffSubsetMode on the document being written;
nothing else changes, and None leaves the output byte for byte as it was.

SvgRasterScale is relative to the SVG's natural CSS-pixel size (2.0 is about
192 DPI at natural size); raise it for sharper print output at the cost of a
larger PDF. It is clamped to 0.25 - 8.0 at render time, and additionally
capped so that no raster side exceeds 10,000 pixels. It never changes the
PLACED size of the image. In Raster mode it sets the whole picture's density;
in Vector mode it applies ONLY to a part that had to fall back to a raster, and
has no effect at all on a picture that stays entirely vector.

@page rules in the document's CSS override the configured size and margins:

    @page { size: a4 landscape; margin: 2cm; }

--- RenderWarnings / RenderWarning / RenderWarningCategory ---

    public enum RenderWarningCategory { Css, Image, Font, Html }

    public sealed class RenderWarning
    {
        public RenderWarningCategory Category { get; }
        public string Code { get; }        // stable machine-readable code (below)
        public string Message { get; }     // display text, "[category] ..." form
        public int Occurrences { get; }    // how many times this exact warning fired
        public int? CodePoint { get; }     // the Unicode code point, for glyph-
                                           // coverage warnings; else null
    }

    public sealed class RenderWarnings
    {
        public IReadOnlyList<string> Messages { get; }        // distinct display
                                                              // messages, first-
                                                              // occurrence order
        public int Count { get; }                             // Messages.Count
        public IReadOnlyList<RenderWarning> Items { get; }    // structured view
    }

Messages collapses duplicates by display text. Items is finer-grained: one
entry per distinct (Code, CodePoint, Message) with an occurrence count, so
distinct dropped code points are separate items even when their display
message collapses. Codes are part of the library's compatibility surface;
display prose is not - assert on Code in tests, never on Message.

STABLE WARNING CODES (complete vocabulary; every warning the library raises
carries one of these):

  Category Css
    css.stylesheet.unparseable   a stylesheet could not be parsed; ignored
    css.stylesheet.remote        <link> to an http(s) stylesheet; skipped
    css.stylesheet.missing       linked local stylesheet file not found; skipped
    css.selector.unsupported     a selector component the engine cannot match
    css.property.unsupported     a CSS property outside the dialect; ignored
    css.value.invalid            a value that does not parse (incl. font-size)
    css.inline-style.unparseable a style="" attribute that could not be parsed
    css.background.partial       'background' shorthand: only its color applied
    css.page-rule.unsupported    an @page property other than size/margin
    css.page-margin.invalid      an @page margin value that does not parse

  Category Image
    image.src.missing            <img> without a usable src; skipped
    image.remote.disabled        http(s) image while AllowRemoteImages is false
    image.load.failed            the image bytes could not be read/downloaded
    image.format.unsupported     bytes are not a format CodeBrix.Imaging decodes
    image.svg.empty              <svg> element without usable content; skipped
    image.svg.failed             the SVG could not be rendered; skipped
    image.svg.rasterized         Vector mode: a part PDF cannot express as
                                 vectors was rasterized on its own (the message
                                 names the reason); the rest stays vector
    image.svg.filter-unsupported an exotic filter primitive, or feTurbulence,
                                 was dropped by the SVG engine
    image.svg.text-unsupported   SVG text on a path, or a glyph-id text run;
                                 not drawn
    image.svg.fonts-missing      tripwire: the SVG engine had no font
                                 registered. Should not occur - report it
    image.svg.degraded           catch-all for any other SVG-engine warning

  RETIRED (can no longer be raised; listed so pattern-matching code is cleaned
  up rather than left dead):
    image.svg.nativemissing      required a SkiaSharp native on Linux; there is
                                 no native in the chain any more

  Category Font
    font.family.unresolved       font-family matched no registered font; the
                                 default sans-serif family was used
    font.uncovered.removed       characters no registered font covers were
                                 removed (default behavior); CodePoint set
    font.uncovered.kept          same, but kept as missing-glyph shapes because
                                 KeepUncoveredCharacters = true; CodePoint set
    font.svg-text.notdef         SVG <text> characters no registered font
                                 covers; rendered as missing-glyph shapes;
                                 CodePoint set

  Category Html
    html.element.ignored         an unsupported element (script, iframe, ...)
    html.table.nested            a table nested inside another table/box; skipped

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
base directory and work with EITHER separator style on every OS; percent-
encoded names resolve too), data: URIs, or - only when AllowRemoteImages is
enabled - http(s) URLs. Every format CodeBrix.Imaging decodes embeds: PNG,
JPEG, BMP, WebP, GIF, TIFF, TGA, PBM/PGM/PPM. Alpha-capable formats keep their
transparency losslessly.

SVG is fully supported and goes through a fully managed SVG engine
(CodeBrix.Imaging.Drawing.NoSkia) - identical output on Windows, macOS and
Linux, no native library, GPU or window system involved:
  - <img src="figure.svg">, data:image/svg+xml URIs (base64 or percent-encoded),
    and inline <svg>...</svg> elements (block or inside a paragraph) all render.
  - PLACEMENT: Options.SvgPlacement decides. Vector (the default) writes the
    picture into the page as PDF operators and adds NO image XObject to the
    file; Raster embeds the whole picture as a transparent PNG at
    Options.SvgRasterScale.
  - NATURAL SIZE: when the SVG root declares its width/height in ABSOLUTE
    units (mm, cm, in, pt, pc, px) those values are used EXACTLY, converted to
    points - 80mm places at 226.77 pt, not at a rounded pixel count.
    Otherwise the CSS-pixel drawing bounds are used at 1 CSS px = 0.75 pt. CSS
    width/height on the element (including physical units like mm) override
    the natural size either way.
  - WHAT VECTOR MODE EMITS: save/restore, matrix concatenation, paths (fills
    with nonzero or even-odd winding, strokes with caps, joins, miter limit
    and dash patterns), rectangle and path clips (intersecting), nested
    pictures (<use> and groups), embedded images written from their ORIGINAL
    PNG/JPEG bytes, and text as REAL PDF TEXT. Each run is drawn as one string
    at its anchor-resolved baseline origin, in the same face the SVG engine
    measured it with, embedded as a SUBSET with a ToUnicode map - so the text
    is selectable, searchable and extractable as words (pdftotext returns it),
    and no glyph geometry is written for it. The page lays the run's glyphs
    out from that same font file's advance widths; only kerning can differ
    from the engine's own measurement, and it stays inside the run. Text the
    SVG itself positions per character (an x or y LIST on <text>/<tspan>) is
    placed glyph by glyph, where the document put each one.
  - WHERE VECTOR TEXT STAYS GLYPH OUTLINES: a run PDF cannot express as text
    is drawn as outline paths instead, with no font embedded for it. That is:
    a STROKED run (PDF text cannot be stroked with the SVG's pen), including
    stroke-and-fill; a run whose fill is a GRADIENT; and a run in a family no
    registered face provides - the engine drew missing-glyph boxes for it, and
    outlines keep the coverage gap visible. Such text is correct on the page
    but is not selectable. Text on a path is not drawn at all (below).
  - GRADIENTS ARE VECTOR: linear, radial and focal (two-point conical)
    gradients become PDF shading patterns - axial (type 2) or radial (type 3)
    - with any number of stops (three or more are stitched into one function).
    Fills and strokes both take them (a stroke gets a shading pen),
    gradientUnits="objectBoundingBox" is honoured on non-square shapes so the
    gradient keeps its direction, and spreadMethod="pad" becomes PDF /Extend.
    A fill-opacity is folded into the stop colours by the SVG engine, and one
    alpha shared by every stop becomes a transparency group around the draw,
    so a translucent gradient still composites correctly.
  - GROUP OPACITY IS VECTOR: <g opacity="..."> becomes a PDF transparency
    group (a form XObject carrying /Group /S /Transparency) drawn under an
    ExtGState with ca/CA, so overlapping children composite ONCE - what an SVG
    viewer shows. No bitmap is embedded and no warning is raised.
  - BLEND MODES: the emitter maps the W3C separable and non-separable modes
    (multiply, screen, overlay, darken, lighten, color-dodge, color-burn,
    hard-light, soft-light, difference, exclusion, hue, saturation, color,
    luminosity) to PDF /BM on a transparency group, and a single draw carrying
    a blend mode goes into a group of one.
    ⚠ NO SVG REACHES THAT PATH TODAY: the SVG engine (CodeBrix.SvgParse) does
    not parse the CSS mix-blend-mode property, so a picture never arrives
    carrying a blend mode. The mapping is exercised by the
    CodeBrix.PdfDocuments tests instead. Treat blend modes as machinery that
    is in place, not as a feature the HTML/SVG input can reach.
  - WHERE VECTOR MODE FALLS BACK: only for what PDF cannot express, and only
    for THAT part - the single command, or the layer or clip scope it governs.
    The complete trigger list is: image filters (a blur, and so on), colour
    filters, Porter-Duff compositing other than source-over, the "plus" and
    "modulate" blends, difference clips, cropped images, image opacity,
    repeating or reflecting gradients (spreadMethod="repeat" or "reflect"),
    gradients whose stops DIFFER in alpha (per-stop stop-opacity - a PDF
    shading carries no alpha, so those need a soft mask, which is future
    work), and pattern fills (<pattern>). Such a part is rasterized on its own
    at Options.SvgRasterScale, embedded as a transparent PNG, and reported
    with the code "image.svg.rasterized" (the message names the reason).
    The same code also covers the safety net: should the vector writer throw on a
    picture (it never should - the case is a defect to report), the picture is
    placed as the engine's own raster instead of the renderer's grey "Image could
    not be read." box, and the message names the exception type and text.
    Everything else on the page stays vector - group opacity and ordinary
    gradients included.
  - SVG <text> renders with the registered document fonts only (see FONTS);
    system fonts are never consulted. font-family values are candidate LISTS
    ("Some Face,serif") tried in order; generic families map to the package
    defaults (including SVG-style spellings "sans" and "mono"), and a family
    no registered font provides falls back to the default sans face - the
    same behavior as HTML text. Internally the engine's per-document font
    registry is filled with exactly the faces the picture's text asks for, and
    each of those faces is recorded under its FONT FILE's own name-table
    family names (typographic and legacy) with its weight and slant - which is
    how a compiled run is mapped back to the exact face whose file the engine
    measured with (nearest weight, matching slant) and that face, not a
    lookalike, is what gets embedded. If you inspect the PDF, note that a bold
    face is named "Roboto,Bold" in /BaseFont, not "Roboto-Bold".
  - SVG text has per-glyph font fallback, driven by the same fallback chain
    HTML text uses (the font packages' companion families are wired in
    automatically; AddFallbackFamily / includeInFallback append to them):
    before the picture is compiled, characters the styled face
    lacks are wrapped in tspans naming the covering fallback family. A
    character NO registered font covers renders as its missing-glyph shape -
    and WARNS, one structured item per distinct code point (code
    "font.svg-text.notdef", with the code point and an occurrence count), so
    coverage gaps are baselined instead of invisible.
  - Text on a path and glyph-id text runs are not drawn; they warn with
    "image.svg.text-unsupported". Exotic filter primitives and feTurbulence
    are dropped with "image.svg.filter-unsupported".
  - A broken or unrenderable SVG degrades to a collected warning, never an
    exception.
  - THREAD SAFETY: the SVG engine is per-document, so concurrent renders do
    not serialize on it. There is nothing for a consumer to do.

--- SUPPORTED CSS DIALECT ---

Sources: inline style="" attributes, <style> blocks, and <link rel="stylesheet">
to LOCAL .css files. Remote stylesheets are skipped with a warning. Sources
contribute in document order.

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

    sans-serif (and unknown families)  ->  Roboto        (Html2PdfFonts.DefaultSansFamily)
    serif                              ->  Merriweather  (Html2PdfFonts.DefaultSerifFamily)
    monospace                          ->  RobotoMono    (Html2PdfFonts.DefaultMonoFamily)

The renderer discovers the copied font packages automatically (see
INSTALLATION). A missing font layout produces an InvalidOperationException
naming the fix (call Html2PdfFonts.AddFontDirectory before rendering).

Html2PdfFonts (static class, namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts) -
every member. All registration methods are idempotent and thread-safe, and all
may also be called AFTER renders have happened - additions take effect on the
next render:

    public const string DefaultSansFamily  = "Roboto";
    public const string DefaultSerifFamily = "Merriweather";
    public const string DefaultMonoFamily  = "RobotoMono";

    public static void EnsureRegistered()
        // Discovers the package font families and registers them with the
        // PDF font pipeline. The renderer calls it automatically; calling it
        // eagerly at startup is harmless and moves the one-time discovery
        // cost out of the first render.

    public static bool HasDefaultFamilies { get; }
        // True when Roboto, Merriweather and RobotoMono were all found.
        // Triggers EnsureRegistered. Use it as a startup self-check.

    public static void AddFontDirectory(string directory)
        // Extra directory to probe for package-shaped font folders
        // (CodeBrix.Platform.Fonts.<Name>/Fonts/ with .ttf.manifest files)
        // living somewhere unusual. Blank -> ArgumentException.

    public static void AddFontFile(string filePath, bool includeInFallback = false)
    public static void AddFontFiles(IEnumerable<string> filePaths, bool includeInFallback = false)
    public static void AddFontFilesFromDirectory(string directory, bool includeInFallback = false)
        // Loose .ttf/.otf files - NO manifest needed; family name, weight and
        // style are read from the font's own name and OS/2 tables. Faces that
        // share a family name group into one family. A missing file throws
        // FileNotFoundException; a missing directory DirectoryNotFoundException;
        // an unreadable font InvalidOperationException. includeInFallback: true
        // also appends the family to the per-glyph fallback chain.

    public static void AddFallbackFamily(string familyName)
        // Appends an already-registered family to the per-glyph fallback
        // chain, consulted (in registration order) for characters the styled
        // font lacks. Fallback families never substitute whole runs - only
        // individual characters.

    // Package-shaped directories living somewhere unusual:
    Html2PdfFonts.AddFontDirectory(path);

    // Loose .ttf/.otf files:
    Html2PdfFonts.AddFontFile("MyFont-Regular.ttf");
    Html2PdfFonts.AddFontFiles(paths);
    Html2PdfFonts.AddFontFilesFromDirectory(dir);

    // Per-glyph fallback: pass includeInFallback: true on the Add* calls, or
    // name an already-registered family. The companions of the built-in font
    // packages are already in the chain; these APPEND after them.
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
  - A character nothing covers is REMOVED with a collected warning
    ("font.uncovered.removed") by default; set
    Options.KeepUncoveredCharacters = true to keep such characters and render
    the font's visible missing-glyph shape instead ("font.uncovered.kept"), so
    a coverage gap leaves a trace on the page.
Supplementary-plane characters (above U+FFFF, e.g. musical notation) are
handled as single code points end to end and embed correctly when a
registered font provides them through a cmap format 12 table.
COMPANION FAMILIES AND THE FALLBACK CHAIN. A CodeBrix.Platform.Fonts.* package
ships one primary family plus the companions that cover the scripts the primary
lacks. Every companion is wired into the per-glyph fallback chain automatically
when discovered; the three body-text defaults (Roboto, Merriweather, RobotoMono)
never are, so a sans document cannot silently pick up serif or monospace prose.

The chain is ordered by the package a companion came from - the sans package
first, then serif, then a special-purpose package such as Noto Music, and the
monospace package last. That order matters because several packages cover the
same scripts: Noto Sans, Noto Serif, Noto Sans Mono and Iosevka ALL carry the
Greek Extended block, and body text is sans by default, so ancient Greek must
resolve to the sans companion rather than the serif one. The monospace package
ranks last because a monospaced glyph is the most jarring substitution inside
proportional text, and because Iosevka's very wide repertoire would otherwise
shadow purpose-built families such as Noto Music.

What this buys you, with no code at all: polytonic (ancient) Greek, Armenian,
Georgian and music notation all render, in a face that matches the surrounding
text. Roboto itself carries just ONE code point of the Greek Extended block, so
before the companions were auto-wired, polytonic Greek was admitted against
Roboto by the legacy allow-list and rendered as tofu boxes with NO warning -
a silent wrong answer. Rendering polytonic Greek requires
CodeBrix.Platform.Fonts.Roboto.OflLicenseForever 1.0.240.51 or later, the first
version carrying the Noto Sans companion.

A consumer can still extend the chain with AddFallbackFamily(...) or
AddFontFile(..., includeInFallback: true); those append AFTER the automatic
companions.

================================================================================

COMPLETE EXAMPLES
=================

Example 1: Render an HTML File to a PDF File
---------------------------------------------
    using CodeBrix.PdfDocCreate.Html2Pdf;

    var renderer = new HtmlPdfRenderer();
    renderer.Options.SetPageSize("a4");
    renderer.Options.FooterText = "Page {page} of {pages}";

    // Relative stylesheet/image references resolve against report.html's folder
    HtmlRenderResult result = renderer.RenderFile("report.html", "out/report.pdf");

    Console.WriteLine($"Wrote {result.OutputFilePath}: {result.PageCount} pages");
    foreach (var message in result.Warnings.Messages)
    {
        Console.WriteLine(message);          // "[css] ...", "[image] ...", ...
    }

Example 2: Render an HTML String with a Base Directory
-------------------------------------------------------
    using CodeBrix.PdfDocCreate.Html2Pdf;

    var html = """
        <!doctype html>
        <html>
        <head>
          <title>Quarterly Summary</title>
          <meta name="author" content="Finance Team">
          <link rel="stylesheet" href="styles/report.css">
        </head>
        <body>
          <h1>Quarterly Summary</h1>
          <p class="lead">Figures are in <strong>thousands</strong>.</p>
          <img src="charts/revenue.svg" style="width: 120mm">
          <table>
            <thead><tr><th>Region</th><th>Revenue</th></tr></thead>
            <tbody>
              <tr><td>North</td><td style="text-align: right">1,204</td></tr>
              <tr><td>South</td><td style="text-align: right">987</td></tr>
            </tbody>
          </table>
        </body>
        </html>
        """;

    var renderer = new HtmlPdfRenderer();
    renderer.Options.Landscape = true;
    renderer.Options.MarginLeftPoints = 54;
    renderer.Options.MarginRightPoints = 54;
    renderer.Options.HeaderText = "{title}";

    // styles/report.css and charts/revenue.svg resolve against "assets"
    var result = renderer.RenderHtml(html, "summary.pdf", baseDirectory: "assets");
    Console.WriteLine($"{result.Title}: {result.PageCount} page(s), {result.Warnings.Count} warning(s)");

Example 3: Render to Bytes and Save (or Return from a Web API)
---------------------------------------------------------------
    using CodeBrix.PdfDocCreate.Html2Pdf;

    var renderer = new HtmlPdfRenderer();
    renderer.Options.DocumentTitle = "Invoice 1042";
    renderer.Options.DocumentAuthor = "Billing";

    HtmlRenderResult result = renderer.RenderHtmlToBytes(
        "<h1>Invoice 1042</h1><p>Total due: <b>$120.00</b></p>");

    byte[] pdf = result.PdfBytes;           // OutputFilePath is null here
    File.WriteAllBytes("invoice-1042.pdf", pdf);

    // In an ASP.NET Core endpoint:
    // return Results.File(result.PdfBytes, "application/pdf", "invoice-1042.pdf");

Example 4: Register Your Own Fonts and a Fallback
--------------------------------------------------
    using CodeBrix.PdfDocCreate.Html2Pdf;
    using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

    // Once at startup. Loose .ttf/.otf files need no manifest.
    Html2PdfFonts.AddFontFilesFromDirectory(Path.Combine(AppContext.BaseDirectory, "fonts"));
    Html2PdfFonts.AddFontFile("fonts/NotoSansSymbols2-Regular.ttf", includeInFallback: true);
    Html2PdfFonts.EnsureRegistered();

    if (!Html2PdfFonts.HasDefaultFamilies)
    {
        throw new InvalidOperationException("Package fonts were not copied next to the app.");
    }

    var renderer = new HtmlPdfRenderer();
    var result = renderer.RenderHtmlToBytes(
        "<p style=\"font-family: 'My Corporate Face', serif\">Body text.</p>");

Example 5: Assert on Structured Warnings (test-baseline style)
---------------------------------------------------------------
    using System.Linq;
    using CodeBrix.PdfDocCreate.Html2Pdf;

    var renderer = new HtmlPdfRenderer();
    var result = renderer.RenderHtmlToBytes(
        "<p style=\"float: left\">Hello</p><img src=\"missing.png\">");

    foreach (RenderWarning item in result.Warnings.Items)
    {
        Console.WriteLine($"{item.Category} {item.Code} x{item.Occurrences}" +
            (item.CodePoint is int cp ? $" U+{cp:X4}" : ""));
    }

    bool floatIgnored = result.Warnings.Items.Any(w => w.Code == "css.property.unsupported");
    bool imageMissing = result.Warnings.Items.Any(w => w.Code == "image.load.failed");
    bool fontsClean   = !result.Warnings.Items.Any(w => w.Category == RenderWarningCategory.Font);

================================================================================

MINIMUM VIABLE PROJECT
======================

    dotnet new console -n MyHtmlPdfApp --framework net10.0
    cd MyHtmlPdfApp
    dotnet add package CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever
    # That is the whole install - nothing extra on any operating system.

MyHtmlPdfApp.csproj:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference Include="CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever" />
      </ItemGroup>
      <ItemGroup>
        <None Update="index.html" CopyToOutputDirectory="PreserveNewest" />
      </ItemGroup>
    </Project>

index.html:

    <!doctype html>
    <html>
    <head>
      <title>Hello, PDF</title>
      <style>
        body { font-family: serif; font-size: 11pt; }
        h1   { font-family: sans-serif; color: #1a4d80; }
        code { font-family: monospace; }
      </style>
    </head>
    <body>
      <h1>Hello, PDF</h1>
      <p>Rendered with <code>CodeBrix.PdfDocCreate.Html2Pdf</code>.</p>
    </body>
    </html>

Program.cs:

    using CodeBrix.PdfDocCreate.Html2Pdf;

    var renderer = new HtmlPdfRenderer();
    renderer.Options.FooterText = "Page {page} of {pages}";

    var input = Path.Combine(AppContext.BaseDirectory, "index.html");
    var result = renderer.RenderFile(input, "hello.pdf");

    Console.WriteLine($"Created {result.OutputFilePath} ({result.PageCount} page(s))");
    foreach (var warning in result.Warnings.Messages) Console.WriteLine(warning);

Build and run:

    dotnet build
    dotnet run

The build copies the font packages' .ttf files into the output under
CodeBrix.Platform.Fonts.<Name>/Fonts/; the renderer finds them there.

================================================================================

PERFORMANCE TIPS
================

1. REUSE THE RENDERER: HtmlPdfRenderer holds no per-document state - Options
   are copied at the start of each render - so one instance can render many
   documents. Font discovery happens once per process, not per render.

2. WARM UP AT STARTUP: Call Html2PdfFonts.EnsureRegistered() (or read
   Html2PdfFonts.HasDefaultFamilies) during application startup so the
   one-time font directory scan does not land inside the first request.

3. LEAVE SvgPlacement ON Vector: vector placement adds no bitmap to the file,
   so a page of engravings or diagrams costs a fraction of the equivalent
   raster and stays sharp at any zoom. Raster mode is for the rare document
   that genuinely needs a flattened picture.

   KEEP SvgRasterScale AT ITS DEFAULT UNLESS PRINTING: it only matters in
   Raster mode, or for a part of a vector picture that fell back to a raster.
   The raster grows with the square of the scale; 2.0 (about 192 DPI at
   natural size) is the default, values are clamped to 0.25 - 8.0 and each
   raster side is capped at 10,000 pixels. Raise it only for print-quality
   output.

4. RENDER TO BYTES FOR WEB SCENARIOS: RenderHtmlToBytes produces the PDF in
   memory (HtmlRenderResult.PdfBytes) with no temp file.

5. DOWNLOADS ARE SYNCHRONOUS: with AllowRemoteImages = true every http(s)
   image is fetched inline during the (synchronous) render. Prefer local
   files or data: URIs for documents rendered on a request path.

6. SIZE IMAGES BEFORE EMBEDDING: a raster image's ORIGINAL bytes are embedded
   as-is; the CSS width/height only decides the placed size, not the stored
   pixels. Pre-size large photos with CodeBrix.Imaging to keep the PDF small.

================================================================================

COMMON PITFALLS TO AVOID
========================

1. DO NOT add a Skia or native-assets package "for SVG". Nothing in this
   package needs one on any operating system, and no version of it ever will
   again - see the notice above. ⚠ The same rule binds the library itself:
   Html2Pdf must never reacquire a Skia or native dependency.

2. DO NOT read PdfBytes after RenderFile/RenderHtml, or OutputFilePath after
   RenderHtmlToBytes - each is null in the other mode. RenderFile and
   RenderHtml write a file and set OutputFilePath; RenderHtmlToBytes sets
   PdfBytes only.

3. DO NOT confuse the NuGet package name
   (CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever) with the namespaces
   (CodeBrix.PdfDocCreate.Html2Pdf and CodeBrix.PdfDocCreate.Html2Pdf.Fonts).

4. DO NOT feed it arbitrary web pages. Floats, positioning, flexbox/grid,
   JavaScript, media queries, CSS variables and calc() are ignored (with
   "css.property.unsupported" / "html.element.ignored" warnings), not
   emulated. Write HTML/CSS for this renderer.

5. DO NOT expect system fonts. A font-family that matches no registered font
   silently becomes Roboto, with a "font.family.unresolved" warning. Register
   the font with Html2PdfFonts first (loose .ttf/.otf files need no manifest).

6. DO NOT ignore Warnings when text is missing. Characters no registered
   font covers (emoji, unusual scripts) are REMOVED by default with a
   "font.uncovered.removed" warning. Register a covering font (with
   includeInFallback: true) or set Options.KeepUncoveredCharacters = true to
   see the gaps as missing-glyph boxes instead.

7. DO NOT expect http(s) images to load by default. AllowRemoteImages is
   false; remote images are skipped with "image.remote.disabled". Remote
   STYLESHEETS are never loaded ("css.stylesheet.remote") - copy CSS locally.

8. DO NOT nest tables. A table inside another table (or box) is skipped with
   "html.table.nested".

9. DO NOT delete or relocate the CodeBrix.Platform.Fonts.<Name>/Fonts/
   folders the build places in the output. Without them the first render
   throws InvalidOperationException. If the fonts must live elsewhere, call
   Html2PdfFonts.AddFontDirectory(path) before rendering; if you set
   CodeBrixHtml2PdfDisableFontCopy=true, you own delivering them.

10. DO NOT expect a second registration of the same family name to replace
    the first. The first registration wins; later ones are silently ignored.

11. DO NOT pass an unknown name to SetPageSize - only letter, legal, ledger,
    a3, a4, a5, b4 and b5 are recognized; anything else throws
    ArgumentException. For other sizes set PageWidthPoints/PageHeightPoints
    directly.

12. DO NOT be surprised when Options are overridden by the document: an @page
    rule in the HTML's CSS (size, landscape, margins) takes precedence over
    the corresponding HtmlRenderOptions values.

13. DO NOT match on warning Message text in tests. Messages are display prose
    and may change; Code, Category, Occurrences and CodePoint are the stable
    surface.

14. DO NOT rely on Landscape = true flipping an already-wide page. The swap
    only happens when height > width.

15. ⚠ DO NOT expect PERCENTAGE rgba() colours in SVG to render. A colour
    written as rgba(r%, g%, b%, a%) is mis-parsed as opaque black by the
    CodeBrix.SvgParse the SVG engine currently brings in, so such fills and
    strokes come out black. This is a pre-existing parser limitation, not a
    vector-placement one - Raster mode is black too. The fix already exists in
    the SvgParse repository; once it is published, Html2Pdf will pin the newer
    SvgParse directly. Until then, rewrite those colours in the numeric or hex
    form (LilyPond's SVG backend is a known source of the percentage form).

16. DO NOT assume EVERY SVG text run is selectable. Vector mode writes SVG
    <text> as real PDF text in the embedded face, so it copies, searches and
    extracts - except where PDF has no way to say it: a STROKED (or stroked-
    and-filled) run, a run filled with a GRADIENT, and a run in a family no
    registered face provides all stay glyph outlines, correct on the page but
    not text. Fill SVG text with a solid colour, and register the fonts it
    names, to keep it selectable. Raster placement has no text at all - the
    whole picture becomes a bitmap - and text on a path is never drawn.

================================================================================

WHAT THIS PACKAGE DOES NOT DO
=============================

  - It is NOT a browser: no floats, absolute/relative positioning, flexbox,
    grid, JavaScript, media queries, CSS variables, calc(), or remote
    stylesheets. It does not convert live websites to PDF.
  - It does NOT use operating-system fonts, ever - only the package fonts and
    fonts you register (this is what makes output identical everywhere).
  - It does NOT depend on Skia, or on any other native library, on any
    operating system - and it never will again; see the notice above.
  - It does NOT make EVERY SVG text run selectable: vector placement emits
    real PDF text, but a stroked or gradient-filled run, and a run no
    registered face covers, stay glyph outlines. Raster placement, which turns
    the whole picture into a bitmap, has no text at all.
  - It does NOT render forms as fillable fields, play audio/video, or embed
    iframes/canvas - those elements are ignored with a warning.
  - It does NOT expose the composed CodeBrix.PdfDocCreate Document for
    further editing; the output is a finished PDF (file or bytes). For
    programmatic document construction use CodeBrix.PdfDocCreate directly.
  - It does NOT render Markdown - that is CodeBrix.PdfDocCreate.Markdown2Pdf,
    which builds on this package.
  - It does NOT throw for unsupported content; it collects warnings. Check
    HtmlRenderResult.Warnings to find out what did not apply.

================================================================================

WORKING EXAMPLES ON GITHUB
==========================

The package's own test project (tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests)
is the reference for every feature area. Base URL:
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/

  Rendering end to end (bytes and files, a full-feature document verified by
  rasterizing it, @page overriding size and margins, unsupported CSS as
  warnings, missing images as warnings, emoji degrading to a warning):
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/HtmlPdfRendererTests.cs

  CSS cascade, specificity, !important, inline styles, inheritance, em/rem
  resolution:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/StyleResolverTests.cs

  Package font discovery, generic-family mapping, numeric weights and
  bold/italic face selection:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/Html2PdfFontsTests.cs

  Glyph coverage from real cmap tables, loose font-file registration,
  uncovered-character removal vs KeepUncoveredCharacters, fallback families,
  the automatic companion wiring and its chain order (polytonic Greek,
  Armenian and Georgian resolving to the SANS companions, not the serif ones):
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/FontCoverageTests.cs

  Noto Music joining the fallback chain; BMP and supplementary-plane music
  glyphs drawn in the PDF:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/NotoMusicFallbackTests.cs

  SVG: referenced files, base64 and plain data: URIs, inline block and inline
  paragraph <svg>, CSS physical sizing:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/SvgSupportTests.cs

  Vector SVG placement: Vector as the default, no image XObject embedded,
  nested transforms and rotation, absolute dash intervals, zero-width strokes,
  declared physical units sizing the picture exactly, <use> through nested
  pictures, SVG text as real text in the embedded face the engine measured
  with (a font subset with a ToUnicode map), stroked text staying glyph
  outlines, an x list placing each glyph where the document put it, group
  opacity emitted as a transparency group rather than a raster, and the raster
  fallback for an image filter:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/SvgVectorPlacementTests.cs

  Vector SVG fidelity: two-stop and multi-stop gradients as shadings, a
  bounding-box gradient keeping its diagonal on a wide shape, radial and focal
  gradients, a gradient stroke, group opacity compositing overlapping children
  once, fill-opacity on a gradient becoming a group of one, and the raster
  fallback for a translucent-stop gradient and for a repeating gradient:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/SvgVectorFidelityTests.cs

  SVG dialect produced by music engravers (viewBox offsets, currentColor,
  invisible link rectangles, generic font families in SVG text):
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/LilyPortSvgDialectTests.cs

  The engraved-music corpus gate: every picture in a real engraving-engine SVG
  corpus placed as vectors with no fallback and no embedded bitmap (skips
  unless HTML2PDF_LILYPORT_SVG_CORPUS points at a folder of .svg files):
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/LilyPortCorpusGateTests.cs

  Every raster image format as local file and data: URI, alpha preserved
  through the PDF:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/ImageFormatSupportTests.cs

  Relative paths with forward or back slashes, percent-encoded names and
  absolute paths resolving on every OS:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/CrossPlatformPathTests.cs

HOW TO USE: Fetch the raw file content from GitHub using a URL like:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/{path}
For example:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/HtmlPdfRendererTests.cs

================================================================================

QUICK REFERENCE CARD
====================

--- Install ---
    dotnet add package CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever
    Nothing else on any OS - no native library anywhere in the chain
Namespaces:     CodeBrix.PdfDocCreate.Html2Pdf, CodeBrix.PdfDocCreate.Html2Pdf.Fonts

--- HtmlPdfRenderer ---
Create:         var r = new HtmlPdfRenderer()
Page size:      r.Options.SetPageSize("a4")   // letter legal ledger a3 a4 a5 b4 b5
Custom size:    r.Options.PageWidthPoints = 595; r.Options.PageHeightPoints = 842
Landscape:      r.Options.Landscape = true
Margins:        r.Options.MarginTopPoints / MarginRightPoints /
                MarginBottomPoints / MarginLeftPoints   (default 72 each)
Furniture:      r.Options.HeaderText = "{title}"; r.Options.FooterText = "Page {page} of {pages}"
Metadata:       r.Options.DocumentTitle / DocumentAuthor  (else <title> / <meta name="author">)
Remote images:  r.Options.AllowRemoteImages = true       // default false
Outline:        r.Options.GenerateOutline = false        // default true (h1-h6)
SVG placement:  r.Options.SvgPlacement = SvgPlacementMode.Raster  // default Vector
SVG sharpness:  r.Options.SvgRasterScale = 3.0           // default 2.0; 0.25-8.0
                                                         // raster parts only
Tofu opt-in:    r.Options.KeepUncoveredCharacters = true
CFF subsetting: r.Options.CffSubsetMode = PdfCffSubsetMode.Sparse  // default None (whole)
Render file:    var res = r.RenderFile("in.html", "out.pdf")
Render string:  r.RenderHtml(html, "out.pdf", baseDir)
Render bytes:   r.RenderHtmlToBytes(html, baseDir)
Result:         res.OutputFilePath (file mode) | res.PdfBytes (bytes mode);
                res.PageCount, res.Title, res.Warnings
Warnings:       res.Warnings.Count / .Messages / .Items
Item:           item.Category (Css|Image|Font|Html), item.Code, item.Message,
                item.Occurrences, item.CodePoint
CSS page rule:  @page { size: a4 landscape; margin: 2cm; }   // beats Options

--- Html2PdfFonts ---
Defaults:       DefaultSansFamily "Roboto", DefaultSerifFamily "Merriweather",
                DefaultMonoFamily "RobotoMono"
Warm-up:        Html2PdfFonts.EnsureRegistered(); Html2PdfFonts.HasDefaultFamilies
Package dir:    Html2PdfFonts.AddFontDirectory(dir)
Loose fonts:    Html2PdfFonts.AddFontFile(path[, includeInFallback: true])
                .AddFontFiles(paths[, ...]) / .AddFontFilesFromDirectory(dir[, ...])
Fallback:       Html2PdfFonts.AddFallbackFamily("Family Name")
                (package companions are auto-wired; chain order is
                 sans pkg -> serif pkg -> Noto Music -> mono pkg)
Opt out copy:   <CodeBrixHtml2PdfDisableFontCopy>true</CodeBrixHtml2PdfDisableFontCopy>

--- Warning codes ---
css.*     stylesheet.unparseable, stylesheet.remote, stylesheet.missing,
          selector.unsupported, property.unsupported, value.invalid,
          inline-style.unparseable, background.partial, page-rule.unsupported,
          page-margin.invalid
image.*   src.missing, remote.disabled, load.failed, format.unsupported,
          svg.empty, svg.failed, svg.rasterized, svg.filter-unsupported,
          svg.text-unsupported, svg.fonts-missing, svg.degraded
          (retired: svg.nativemissing - can no longer be raised)
font.*    family.unresolved, uncovered.removed, uncovered.kept, svg-text.notdef
html.*    element.ignored, table.nested

Target: .NET 10 or later

================================================================================
