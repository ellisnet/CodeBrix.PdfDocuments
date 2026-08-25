================================================================================
AGENT-README: CodeBrix.PdfDocCreate.Markdown2Pdf
A Guide for AI Coding Agents — CONSUMING the
CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever NuGet package
================================================================================

OVERVIEW
========
CodeBrix.PdfDocCreate.Markdown2Pdf turns ANY Markdown (.md) file into a
nice-looking, pre-formatted, printable PDF with zero configuration. Point it at
a file, get a PDF. Target: .NET 10 or later.

The pipeline has three stages, and all three are reachable:

  Markdown  --(vendored markdown-it port)-->  HTML
            --(built-in print stylesheet)-->  HTML document
            --(CodeBrix.PdfDocCreate.Html2Pdf)-->  PDF

Stage 1 is a faithful C# port of the markdown-it JavaScript parser, exposed as
a public API so you can parse Markdown to HTML on its own, or extend it with
custom rules and plugins. Stage 2 hands you the generated body HTML and the
default stylesheet so you can restyle the document before rendering. Stage 3 is
the Html2Pdf renderer, whose behaviour, CSS dialect and warnings this package
inherits unchanged.

Defaults chosen for you: US Letter pages, book-ish margins, Merriweather body
text, Roboto headings, Roboto Mono code with syntax-highlight colors, footer
"page / pages" numbers, and a PDF outline built from the headings. The title is
inferred from YAML front matter (title:), else the first heading, else the file
name; front-matter author: fills the PDF author metadata. All text renders with
the CodeBrix.Platform.Fonts package fonts and never with operating-system
fonts, so output is identical on every operating system.

ROBUSTNESS CONTRACT: any .md input produces a document. Unsupported constructs
degrade and are reported in the result's Warnings collection - they are never
thrown.

PROVENANCE: the parser is a C# port of markdown-it 14.1.0 with ports of its
markdown-it-footnote 4.0.0, markdown-it-task-lists and markdown-it-front-matter
plugins. The API shape mirrors the JavaScript original, but EVERY namespace is
CodeBrix.PdfDocCreate.Markdown2Pdf.*. Do not write API from memory of the
JavaScript library - the C# names are PascalCase and several members differ.

PACKAGE NAME vs NAMESPACE (the single most common mistake):

  NuGet package                                         Namespace root
  ----------------------------------------------------  --------------------------------
  CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever  CodeBrix.PdfDocCreate.Markdown2Pdf.*

The ".MitLicenseForever" suffix belongs to the PACKAGE ID only. It never
appears in a namespace, a using directive or a type name.

See also (sibling packages in the same repository):
  - src/CodeBrix.PdfDocCreate.Html2Pdf/AGENT-README.txt - the renderer this
    package renders THROUGH: the supported HTML elements, the CSS dialect, font
    registration, SVG handling, the warning codes and the Linux SVG rule
  - src/CodeBrix.PdfDocCreate/AGENT-README.txt - the document object model
    underneath, for building PDFs programmatically instead of from Markdown
  - AGENT-README.txt (repository root) - CodeBrix.PdfDocuments, the low-level
    PDF library at the bottom of the stack
  - src/CodeBrix.PdfRasterizer/AGENT-README.txt - rasterize the produced PDFs
    back to images (how this package's own tests check page geometry)

Source repository: https://github.com/ellisnet/CodeBrix.PdfDocuments

================================================================================

INSTALLATION
============
NuGet Package: CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever

    dotnet add package CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever

Or in a .csproj file (NuGet resolves the latest version):

    <PackageReference Include="CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever" />

NuGet dependencies (all pulled in automatically):
  - CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever, which itself brings
    CodeBrix.PdfDocCreate.MitLicenseForever (and through it
    CodeBrix.PdfDocuments, CodeBrix.Imaging and CodeBrix.Compression),
    CodeBrix.MarkupParse.MitLicenseForever,
    CodeBrix.StyleSheetParse.MitLicenseForever,
    CodeBrix.SkiaSvg.MitLicenseForever, and the font packages
    CodeBrix.Platform.Fonts.Roboto / .Merriweather / .RobotoMono /
    .NotoMusic (all OFL-licensed)

There is nothing else to install: no fonts, no native libraries on Windows or
macOS, no browser engine, no Node runtime. The markdown-it port is source code
inside this package, not a JavaScript dependency.

License: MIT (the font packages are OFL-licensed).

Requirements: .NET 10 or later.

################################################################################
## IMPORTANT - LINUX ONLY: SVG RENDERING NEEDS A SkiaSharp NATIVE-ASSETS PACKAGE
################################################################################

Markdown2Pdf renders through Html2Pdf, so it inherits that package's Linux SVG
requirement exactly. If your application runs on LINUX and your Markdown
embeds SVG content - an <img src="diagram.svg">, a data:image/svg+xml URI or an
inline <svg> block - the APPLICATION must reference ONE of these two NuGet
packages itself:

    SkiaSharp.NativeAssets.Linux
    SkiaSharp.NativeAssets.Linux.NoDependencies

  dotnet add package SkiaSharp.NativeAssets.Linux
    -- OR --
  dotnet add package SkiaSharp.NativeAssets.Linux.NoDependencies

EITHER package satisfies the renderer equally - neither is recommended over the
other. Reference exactly ONE, whichever suits the application. If the
application already references one of them for its own reasons, KEEP THAT ONE;
nothing needs to change, and it must not be swapped for the other.

WINDOWS and macOS need NOTHING extra - SkiaSharp's own package brings those
natives transitively.

WHY this is not just a package dependency: two mutually exclusive Linux
variants exist, and only the consuming application can choose between them;
declaring one here would force that choice on every consumer and break
applications that already reference the other. This is a deliberate design
decision - do NOT "fix" it by adding a dependency on either package.

WHAT HAPPENS IF IT IS MISSING: nothing crashes. SVG images are skipped and the
rest of the document renders normally. The skip is reported as a collected
rendering warning with the code "image.svg.nativemissing", whose message names
both packages. Check MarkdownRenderResult.Warnings when SVG content is silently
absent from your output.

################################################################################

================================================================================

KEY NAMESPACES / USINGS
=======================

    using CodeBrix.PdfDocCreate.Markdown2Pdf;
        // MarkdownPdfRenderer, MarkdownRenderOptions, MarkdownRenderResult,
        // MarkdownHtmlResult

    using CodeBrix.PdfDocCreate.Html2Pdf;
        // RenderWarnings, RenderWarning, RenderWarningCategory - the types of
        // MarkdownRenderResult.Warnings; also HtmlPdfRenderer and
        // HtmlRenderOptions for the restyling workflow

    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
        // MarkdownParser, MarkdownPreset, MarkdownItOptions, Token, MdEnv,
        // Renderer, Ruler<TRule>, ParserBlock, ParserInline, ParserCore,
        // and the rule delegates BlockRuleFn, InlineRuleFn, InlineRule2Fn,
        // CoreRule, RendererRuleFn

    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;   // StateBlock
    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesInline;  // StateInline
    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesCore;    // StateCore
    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;       // MdUtils, MdUrl
    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Helpers;      // LinkHelpers
    using CodeBrix.PdfDocCreate.Markdown2Pdf.Plugins;
        // FootnotePlugin, TaskListPlugin, FrontMatterPlugin

Note that the warnings type on the result belongs to the Html2Pdf namespace,
not to this package's - that using directive is easy to forget. See the
PACKAGE NAME vs NAMESPACE table in OVERVIEW for the package-to-namespace map.

================================================================================

CORE API REFERENCE
==================

--- MarkdownPdfRenderer (sealed class; the whole top-level surface) ---

    public MarkdownRenderOptions Options { get; }
        // modify BEFORE calling a render method

    public MarkdownRenderResult RenderFile(string markdownFilePath,
        string outputPdfPath = null)
        // Reads the file, renders it, and writes the PDF. When outputPdfPath
        // is null the PDF is written next to the source file with the same
        // name and a .pdf extension. Relative images and links resolve against
        // the Markdown file's own directory. A null or blank markdownFilePath
        // throws ArgumentException.

    public MarkdownRenderResult RenderMarkdown(string markdown,
        string outputPdfPath, string baseDirectory = null)
        // Renders a Markdown STRING to a PDF file. baseDirectory anchors
        // relative image references; null means the current directory. A blank
        // outputPdfPath throws ArgumentException; a null markdown throws
        // ArgumentNullException.

    public MarkdownRenderResult RenderMarkdownToBytes(string markdown,
        string baseDirectory = null)
        // Renders a Markdown string and returns the PDF in
        // MarkdownRenderResult.PdfBytes. No file is written.

    public MarkdownHtmlResult GenerateHtmlFromFile(string markdownFilePath)
        // Converts a Markdown FILE to ready-to-render HTML/CSS without
        // producing a PDF. BaseDirectory is the file's directory and the
        // fallback title is the file name without its extension.

    public MarkdownHtmlResult GenerateHtml(string markdown,
        string baseDirectory = null)
        // The same for a Markdown STRING. There is no file-name fallback, so
        // the title comes from front matter, else the first heading, else "".

    public static string BuildHtmlDocument(string bodyHtml, string css,
        string title)
        // Assembles <!DOCTYPE html> ... <head><title>..</title><style>css
        // </style></head><body>bodyHtml</body></html>. Public so that
        // consumers restyling the generated output can reassemble the document
        // themselves; the title is HTML-escaped.

All methods are synchronous. Each render call builds a fresh parser and a fresh
HtmlPdfRenderer, so one MarkdownPdfRenderer instance can be reused for many
documents sequentially and nothing is cached between calls. Options are read at
the start of each call, so changing them between calls affects the next call.

--- MarkdownRenderResult (sealed class; every member) ---

    public string OutputFilePath { get; }   // FULL path of the written PDF;
                                            // null when the render produced bytes
    public byte[] PdfBytes { get; }         // the PDF content; null when the
                                            // render wrote to a file
    public int PageCount { get; }           // pages in the rendered document
    public string Title { get; }            // the inferred title, also written
                                            // to the PDF metadata
    public RenderWarnings Warnings { get; } // non-fatal issues; empty on a
                                            // clean render

Exactly one of OutputFilePath / PdfBytes is non-null: RenderFile and
RenderMarkdown fill OutputFilePath; RenderMarkdownToBytes fills PdfBytes. This
is where the bytes come out - RenderMarkdownToBytes does not return byte[]
directly.

    var result = renderer.RenderMarkdownToBytes(markdownText);
    byte[] pdf = result.PdfBytes;           // <- here
    Console.WriteLine($"{result.PageCount} pages, {result.Warnings.Count} warnings");

--- MarkdownHtmlResult (sealed class; every member) ---

    public string BodyHtml { get; }      // the rendered markup (no <html> wrapper)
    public string Css { get; }           // the default stylesheet, in the
                                         // Html2Pdf CSS dialect
    public string Title { get; }         // the inferred title
    public string BaseDirectory { get; } // where relative images resolve from;
                                         // pass this on to Html2Pdf

    public string ToHtmlDocument()
        // BuildHtmlDocument(BodyHtml, Css, Title) - the exact document the
        // zero-config path would have rendered

    public string ToHtmlDocument(string replacementCss)
        // BuildHtmlDocument(BodyHtml, replacementCss, Title) - your stylesheet
        // instead of the built-in one

--- MarkdownRenderOptions (sealed class; all five properties, with defaults) ---

    renderer.Options.PageSize = "a4";
        // string; default "letter". Recognized names (case-insensitive):
        // letter, legal, ledger, a3, a4, a5, b4, b5. Any other value throws
        // ArgumentException from the underlying renderer.
        //   letter 612 x 792    legal 612 x 1008   ledger 792 x 1224
        //   a3 842 x 1191       a4 595 x 842       a5 420 x 595
        //   b4 709 x 1001       b5 499 x 709       (points)

    renderer.Options.AllowRemoteImages = true;
        // bool; default false. When false, http(s) image references are
        // skipped and reported as a warning rather than fetched.

    renderer.Options.FooterText = "Page {page} of {pages}";
        // string; default "{page} / {pages}". Set to null to remove the
        // footer. Tokens {page}, {pages} and {title} expand at render time.

    renderer.Options.SvgRasterScale = 3.0;
        // double; default 2.0. Forwarded to Html2Pdf; higher is sharper and
        // larger.

    renderer.Options.KeepUncoveredCharacters = true;
        // bool; default false. Forwarded to Html2Pdf: keep characters no
        // registered font covers (they render as .notdef boxes) instead of
        // dropping them and warning.

Those are ALL the options. Everything else about the look - page margins,
fonts, colors, heading sizes - is deliberately not a knob: intercept the
HTML/CSS instead (see WORKFLOW (b) below). The fixed choices this package makes
on your behalf are US Letter or your PageSize, 66 pt top margin, 72 pt bottom
margin, a heading-derived PDF outline, and the document title/author taken from
front matter.

--- WORKFLOW (a): zero-config Markdown to PDF ---

    using CodeBrix.PdfDocCreate.Markdown2Pdf;

    var renderer = new MarkdownPdfRenderer();

    // Writes my_notes.pdf next to the source file
    var result = renderer.RenderFile("my_notes.md");

    // Or control the output location / render from a string / get bytes
    result = renderer.RenderFile("my_notes.md", "out/notes.pdf");
    result = renderer.RenderMarkdown(markdownText, "out.pdf", baseDirectory);
    result = renderer.RenderMarkdownToBytes(markdownText);

--- WORKFLOW (b): restyle the generated HTML/CSS yourself ---

Consumers who want a different look intercept the HTML/CSS instead of asking
Markdown2Pdf for styling knobs:

    using CodeBrix.PdfDocCreate.Html2Pdf;
    using CodeBrix.PdfDocCreate.Markdown2Pdf;

    var generated = renderer.GenerateHtmlFromFile("my_notes.md");
    // generated.BodyHtml      - the rendered markup
    // generated.Css           - the default stylesheet (Html2Pdf dialect)
    // generated.Title         - the inferred title
    // generated.BaseDirectory - anchor for relative images

    var myCss = generated.Css.Replace("#1c1c1c", "#000033");  // or replace wholesale
    var html = generated.ToHtmlDocument(myCss);

    var htmlRenderer = new HtmlPdfRenderer();
    htmlRenderer.Options.SetPageSize("a4");
    htmlRenderer.Options.MarginTopPoints = 90;
    htmlRenderer.RenderHtml(html, "restyled.pdf", generated.BaseDirectory);

Because you drive HtmlPdfRenderer yourself in this workflow, every Html2Pdf
option becomes available - custom page sizes, all four margins, header text,
outline on/off, document metadata. See the Html2Pdf AGENT-README.

BuildHtmlDocument is public for the case where you want to assemble the
document from parts of your own:

    var html = MarkdownPdfRenderer.BuildHtmlDocument(
        generated.BodyHtml + footerHtml, myCss, "My Title");

--- MARKDOWN FEATURES ---

CommonMark (verified against the full CommonMark 0.31.2 specification example
corpus, all 652 examples), plus:

  - GFM tables, with column alignment
  - strikethrough (~~text~~)
  - footnotes: reference style [^label] with a definition block, and inline
    ^[note text]; rendered as a numbered <sup class="footnote-ref"> link and a
    <section class="footnotes"> list with back-references
  - GitHub task lists: - [ ] and - [x], rendered as a styleable
    <span class="task-list-item-checkbox task-unchecked"> or "task-checked"
    carrying a geometric-shape glyph (there are no form controls in the PDF
    dialect)
  - YAML front matter: a leading --- fenced block is consumed, never rendered,
    and its title: / author: keys feed the PDF metadata
  - reference links and images, autolinks, embedded HTML (rendered through
    Html2Pdf's documented element subset, inline <svg> included)
  - fenced code with automatic syntax highlighting

Fence languages that get highlighting (the aliases are exact):

    csharp, c#, cs                    -> C#
    bash, sh, shell, zsh, powershell, ps1, console
    json, jsonc
    xml, csproj, html, xaml, svg
    typescript, ts, javascript, js, jsx, tsx
    python, py
    sql
    yaml, yml
    c, cpp, c++, h, hpp

Any other language (or none) is escaped plainly, with no error. Highlighted
tokens become <span class="hl-keyword|hl-string|hl-comment|hl-number|hl-type|
hl-attribute">, which the default stylesheet colors and which your own
stylesheet can recolor.

Images support the same formats as Html2Pdf - PNG, JPEG, BMP, WebP, GIF, TIFF,
TGA, PBM/PGM/PPM and SVG - referenced as relative or absolute paths (either
separator style works on every OS) or as data: URIs. The data: URI allow-list
admits exactly those image media types and rejects everything else.

--- THE markdown-it PORT (ADVANCED) ---

CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.MarkdownParser is a faithful C#
port of the markdown-it JavaScript parser. Use it directly whenever you want
Markdown-to-HTML without a PDF, a different set of enabled rules, or custom
plugins.

    public sealed class MarkdownParser
        public MarkdownParser(MarkdownPreset preset = MarkdownPreset.Default,
            Action<MarkdownItOptions> configureOptions = null)

        public ParserInline Inline { get; }    // inline tokenizer + its rulers
        public ParserBlock Block { get; }      // block tokenizer + its ruler
        public ParserCore Core { get; }        // top-level chain + its ruler
        public Renderer Renderer { get; }      // token -> HTML
        public MarkdownItOptions Options { get; }

        public Func<string, bool> ValidateLink { get; set; }
        public Func<string, string> NormalizeLink { get; set; }
        public Func<string, string> NormalizeLinkText { get; set; }

        public MarkdownParser Configure(MarkdownPreset preset)
        public MarkdownParser Enable(IEnumerable<string> list,
            bool ignoreInvalid = false)
        public MarkdownParser Disable(IEnumerable<string> list,
            bool ignoreInvalid = false)
        public MarkdownParser Use(Action<MarkdownParser> plugin)

        public string Render(string src, MdEnv env = null)
        public string RenderInline(string src, MdEnv env = null)
        public List<Token> Parse(string src, MdEnv env)
        public List<Token> ParseInline(string src, MdEnv env)

    public enum MarkdownPreset { Default, CommonMark, Zero }

        Default     every rule registered below is enabled; Html = false.
                    This is the preset the PDF pipeline uses (with Html
                    switched on and a Highlight function installed).
        CommonMark  strict CommonMark: Html = true, XhtmlOut = true,
                    MaxNesting = 20, and only the CommonMark rules enabled -
                    no table, no strikethrough, no linkify, no typographer.
        Zero        nothing but "paragraph" and "text"; build up with Enable.

    var md = new MarkdownParser();                       // Default preset
    var html = md.Render("# hello **world**");
    var strict = new MarkdownParser(MarkdownPreset.CommonMark);

    var configured = new MarkdownParser(MarkdownPreset.Default, o =>
    {
        o.Html = true;
        o.Linkify = true;
        o.Typographer = true;
    });

Enable and Disable take rule names and throw InvalidOperationException for an
unknown name unless ignoreInvalid is true:

    md.Disable(new[] { "linkify", "smartquotes" });
    md.Enable(new[] { "table" });

--- MarkdownItOptions (sealed class; every property, with defaults) ---

    public bool Html { get; set; }              // default false - raw HTML in
                                                // the source is escaped
    public bool XhtmlOut { get; set; }          // default false - emit <br /> etc.
    public bool Breaks { get; set; }            // default false - "\n" becomes <br>
    public string LangPrefix { get; set; }      // default "language-" - class
                                                // prefix on fenced code blocks
    public bool Linkify { get; set; }           // default false - autolink bare URLs
    public bool Typographer { get; set; }       // default false - smart quotes,
                                                // (c), (tm), dashes, ellipses
    public string Quotes { get; set; }          // default the four curly quotes,
                                                // as one 4-character string
    public Func<string, string, string, string> Highlight { get; set; }
        // default null. (content, langName, langAttrs) => escaped HTML, or
        // null to fall back to plain escaping.
    public int MaxNesting { get; set; }         // default 100 (20 in the
                                                // CommonMark and Zero presets)

--- Token (sealed class; the parse output) ---

    public Token(string type, string tag, int nesting)

    public string Type { get; set; }        // "paragraph_open", "inline", ...
    public string Tag { get; set; }         // the HTML tag, "" for text tokens
    public int Nesting { get; set; }        // 1 opening, 0 self-contained,
                                            // -1 closing
    public int Level { get; set; }
    public List<string[]> Attrs { get; set; }   // each entry is [name, value]
    public int[] Map { get; set; }              // [startLine, endLine], block
                                                // tokens only
    public List<Token> Children { get; set; }   // inline tokens of an "inline"
    public string Content { get; set; }
    public string Markup { get; set; }          // the source marker: "**", "~~~",
                                                // "#", the fence characters
    public string Info { get; set; }            // fence info string
    public Dictionary<string, object> Meta { get; set; }
    public bool Block { get; set; }
    public bool Hidden { get; set; }

    public int AttrIndex(string name)
    public void AttrPush(string[] attrData)
    public void AttrSet(string name, string value)
    public string AttrGet(string name)
    public void AttrJoin(string name, string value)

Walking the token stream is often easier than a regex over the HTML:

    var md = new MarkdownParser();
    foreach (var token in md.Parse(markdown, new MdEnv()))
    {
        if (token.Type == "heading_open" && token.Tag == "h1")
        {
            // the heading text is in the NEXT token, an "inline"
        }
    }

--- The rule chains, their rulers and their delegates ---

    public delegate bool BlockRuleFn(StateBlock state, int startLine,
        int endLine, bool silent);
    public delegate bool InlineRuleFn(StateInline state, bool silent);
    public delegate void InlineRule2Fn(StateInline state);
    public delegate void CoreRule(StateCore state);
    public delegate string RendererRuleFn(List<Token> tokens, int idx,
        MarkdownItOptions options, MdEnv env, Renderer self);

A rule returns true when it consumed input. The silent parameter means
"validation only - do not push tokens".

    public sealed class ParserBlock
        public Ruler<BlockRuleFn> Ruler { get; }
        public void Tokenize(StateBlock state, int startLine, int endLine)
        public void Parse(string src, MarkdownParser md, MdEnv env,
            List<Token> outTokens)

    public sealed class ParserInline
        public Ruler<InlineRuleFn> Ruler { get; }    // tokenizing rules
        public Ruler<InlineRule2Fn> Ruler2 { get; }  // pair post-processing only
        public void Tokenize(StateInline state)
        public void SkipToken(StateInline state)
        public void Parse(string str, MarkdownParser md, MdEnv env,
            List<Token> outTokens)

    public sealed class ParserCore
        public Ruler<CoreRule> Ruler { get; }
        public void Process(StateCore state)

    public sealed class Ruler<TRule> where TRule : Delegate
        public void At(string name, TRule fn, IEnumerable<string> alt = null)
        public void Before(string beforeName, string ruleName, TRule fn,
            IEnumerable<string> alt = null)
        public void After(string afterName, string ruleName, TRule fn,
            IEnumerable<string> alt = null)
        public void Push(string ruleName, TRule fn, IEnumerable<string> alt = null)
        public List<string> Enable(IEnumerable<string> list,
            bool ignoreInvalid = false)
        public void EnableOnly(IEnumerable<string> list, bool ignoreInvalid = false)
        public List<string> Disable(IEnumerable<string> list,
            bool ignoreInvalid = false)
        public TRule[] GetRules(string chainName)

The built-in rule names, in registration order - these are the anchors you pass
to Before / After / At:

    Core.Ruler      normalize, block, inline, linkify, replacements,
                    smartquotes, text_join
    Block.Ruler     table, code, fence, blockquote, hr, list, reference,
                    html_block, heading, lheading, paragraph
    Inline.Ruler    text, linkify, newline, escape, backticks, strikethrough,
                    emphasis, link, image, autolink, html_inline, entity
    Inline.Ruler2   balance_pairs, strikethrough, emphasis, fragments_join

--- Renderer (sealed class) ---

    public Dictionary<string, RendererRuleFn> Rules { get; }
        // keyed by token Type. Populated by default with code_inline,
        // code_block, fence, image, hardbreak, softbreak, text, html_block and
        // html_inline. Any token type WITHOUT a rule is rendered generically
        // from its Tag, Attrs and Nesting, so a plugin only needs a rule when
        // the default markup is wrong.

    public string Render(List<Token> tokens, MarkdownItOptions options, MdEnv env)
    public string RenderInline(List<Token> tokens, MarkdownItOptions options,
        MdEnv env)
    public string RenderInlineAsText(List<Token> tokens,
        MarkdownItOptions options, MdEnv env)
    public string RenderToken(List<Token> tokens, int idx,
        MarkdownItOptions options)
    public string RenderAttrs(Token token)

Overriding an existing rule is how you change generated markup - for example,
to add a class to every table:

    var baseRule = md.Renderer.Rules.TryGetValue("table_open", out var existing)
        ? existing
        : null;
    md.Renderer.Rules["table_open"] = (tokens, idx, options, env, self) =>
    {
        tokens[idx].AttrJoin("class", "md-table");
        return baseRule != null
            ? baseRule(tokens, idx, options, env, self)
            : self.RenderToken(tokens, idx, options);
    };

--- The parser states ---

    public sealed class StateCore
        public StateCore(string src, MarkdownParser md, MdEnv env)
        public string Src { get; set; }
        public MarkdownParser Md { get; }
        public MdEnv Env { get; }
        public List<Token> Tokens { get; }
        public bool InlineMode { get; set; }

    public sealed class StateBlock
        public StateBlock(string src, MarkdownParser md, MdEnv env,
            List<Token> tokens)
        public string Src { get; set; }
        public MarkdownParser Md { get; }   public MdEnv Env { get; }
        public List<Token> Tokens { get; }
        public List<int> BMarks { get; }    // line begin offsets
        public List<int> EMarks { get; }    // line end offsets
        public List<int> TShift { get; }    // offset of the first non-space
        public List<int> SCount { get; }    // indent in spaces
        public List<int> BsCount { get; }
        public int BlkIndent { get; set; }
        public int Line { get; set; }       public int LineMax { get; set; }
        public bool Tight { get; set; }
        public int DdIndent { get; set; }   public int ListIndent { get; set; }
        public string ParentType { get; set; }   // "root", "paragraph", ...
        public int Level { get; set; }
        public Token Push(string type, string tag, int nesting)
        public bool IsEmpty(int line)
        public int SkipEmptyLines(int from)
        public int SkipSpaces(int pos)
        public int SkipSpacesBack(int pos, int min)
        public int SkipChars(int pos, int code)
        public int SkipCharsBack(int pos, int code, int min)
        public string GetLines(int begin, int end, int indent, bool keepLastLF)

    public sealed class StateInline
        public StateInline(string src, MarkdownParser md, MdEnv env,
            List<Token> outTokens)
        public string Src { get; set; }
        public MarkdownParser Md { get; }   public MdEnv Env { get; }
        public List<Token> Tokens { get; }
        public List<TokenMeta> TokensMeta { get; }
        public int Pos { get; set; }        public int PosMax { get; set; }
        public int Level { get; set; }
        public string Pending { get; set; } public int PendingLevel { get; set; }
        public Dictionary<int, int> Cache { get; }
        public List<Delimiter> Delimiters { get; set; }
        public Dictionary<int, int> Backticks { get; }
        public bool BackticksScanned { get; set; }
        public int LinkLevel { get; set; }
        public Token Push(string type, string tag, int nesting)
        public Token PushPending()
        public ScanDelimsResult ScanDelims(int start, bool canSplitWord)

--- MdEnv and its companions ---

    public sealed class MdEnv : Dictionary<string, object>
        public MdEnv()
        public Dictionary<string, LinkReference> References { get; }
        // The per-render scratch pad. Link reference definitions collect here,
        // and plugins use it to carry state between rules (the footnote plugin
        // stores its collected notes here). Pass the SAME MdEnv to Parse and
        // Render when you split the two steps.

    public sealed class LinkReference
        public string Title { get; set; }   public string Href { get; set; }

    public sealed class Delimiter
        public int Marker { get; set; }     public int Length { get; set; }
        public int Token { get; set; }      public int End { get; set; }
        public bool Open { get; set; }      public bool Close { get; set; }

    public sealed class TokenMeta
        public List<Delimiter> Delimiters { get; set; }

    public readonly struct ScanDelimsResult
        public ScanDelimsResult(bool canOpen, bool canClose, int length)
        public bool CanOpen { get; }  public bool CanClose { get; }
        public int Length { get; }

--- Link safety (ValidateLink / NormalizeLink / NormalizeLinkText) ---

The default validator rejects vbscript:, javascript:, file: and data: URLs,
EXCEPT data: URLs whose media type is one of the supported image types
(image/gif, png, jpeg, webp, bmp, x-windows-bmp, tiff, x-tga, x-targa,
x-portable-pixmap, x-portable-graymap, x-portable-bitmap, x-portable-anymap,
svg+xml). A rejected link renders as plain text - nothing is thrown.

The default NormalizeLink percent-encodes and punycodes the host of http:,
https: and mailto: URLs; NormalizeLinkText reverses that for display. Replace
any of the three to tighten or loosen the policy:

    md.ValidateLink = url => url.StartsWith("https://", StringComparison.Ordinal);

--- Helper classes ---

    public static class MdUtils                 // ...MarkdownIt.Common
        public static string EscapeHtml(string str)
        public static string EscapeRE(string str)
        public static string UnescapeMd(string str)
        public static string UnescapeAll(string str)
        public static string NormalizeReference(string str)
        public static string FromCodePoint(int c)
        public static bool IsValidEntityCode(int c)
        public static int CharCode(string str, int pos)
        public static bool IsSpace(int code)
        public static bool IsWhiteSpace(int code)
        public static bool IsPunctChar(char ch)
        public static bool IsMdAsciiPunct(int ch)
        public static List<Token> ArrayReplaceAt(List<Token> src, int pos,
            List<Token> newElements)

    public static class MdUrl                   // ...MarkdownIt.Common
        public const string EncodeDefaultChars = ";/?:@&=+$,-_.!~*'()#";
        public const string DecodeDefaultChars = ";/?:@&=+$,#";
        public static MdUrlParts Parse(string url, bool slashesDenoteHost)
        public static string Format(MdUrlParts url)
        public static string Encode(string str,
            string exclude = EncodeDefaultChars, bool keepEscaped = true)
        public static string Decode(string str,
            string exclude = DecodeDefaultChars)

    public sealed class MdUrlParts
        public string Protocol / Auth / Port / Hostname / Hash / Search
                    / Pathname { get; set; }
        public bool Slashes { get; set; }

    public static class LinkHelpers              // ...MarkdownIt.Helpers
        public static int ParseLinkLabel(StateInline state, int start,
            bool disableNested = false)
        public static LinkDestinationResult ParseLinkDestination(string str,
            int start, int max)
        public static LinkTitleResult ParseLinkTitle(string str, int start,
            int max, LinkTitleResult prevState = null)

    public sealed class LinkDestinationResult
        public bool Ok { get; set; }  public int Pos { get; set; }
        public string Str { get; set; }

    public sealed class LinkTitleResult
        public bool Ok { get; set; }  public bool CanContinue { get; set; }
        public int Pos { get; set; }  public string Str { get; set; }
        public int Marker { get; set; }

Always escape text you inject into generated HTML with MdUtils.EscapeHtml.

--- The bundled plugins, applied to a bare parser ---

    public static class FootnotePlugin
        public static void Apply(MarkdownParser md)

    public static class TaskListPlugin
        public static void Apply(MarkdownParser md)

    public static class FrontMatterPlugin
        public static void Apply(MarkdownParser md, Action<string> callback)

A parser you create yourself has NONE of them; the PDF pipeline installs all
three internally. Add them explicitly when you use MarkdownParser directly:

    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
    using CodeBrix.PdfDocCreate.Markdown2Pdf.Plugins;

    string frontMatter = null;

    var md = new MarkdownParser(MarkdownPreset.Default, o => o.Html = true);
    md.Use(FootnotePlugin.Apply);
    md.Use(TaskListPlugin.Apply);
    FrontMatterPlugin.Apply(md, text => frontMatter = text);

    var html = md.Render(markdown);
    // frontMatter now holds the raw YAML block, or null when there was none

FrontMatterPlugin takes a callback and therefore does not fit the
Action<MarkdownParser> shape that Use expects - call Apply directly, as above.

Markup the plugins produce, for your stylesheet:

    footnotes     <sup class="footnote-ref">, <hr class="footnotes-sep">,
                  <section class="footnotes">, <ol class="footnotes-list">,
                  <li class="footnote-item">, <a class="footnote-backref">
    task lists    <li class="task-list-item">, <ul class="contains-task-list">,
                  <span class="task-list-item-checkbox task-checked"> and
                  ... task-unchecked

--- WRITING A CUSTOM PLUGIN ---

A plugin is just an Action<MarkdownParser> that registers rules. This one adds
==highlight== syntax and renders it as a styleable span:

    using System;
    using System.Collections.Generic;
    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesInline;

    public static class MarkPlugin
    {
        public static void Apply(MarkdownParser md)
        {
            md.Inline.Ruler.Before("emphasis", "mark", Rule);

            md.Renderer.Rules["mark_open"] =
                (tokens, idx, options, env, self) => "<span class=\"mark\">";
            md.Renderer.Rules["mark_close"] =
                (tokens, idx, options, env, self) => "</span>";
        }

        private static bool Rule(StateInline state, bool silent)
        {
            var start = state.Pos;
            if (start + 4 > state.PosMax) { return false; }
            if (state.Src[start] != '=' || state.Src[start + 1] != '=')
            {
                return false;
            }

            var end = state.Src.IndexOf("==", start + 2, StringComparison.Ordinal);
            if (end < 0 || end + 2 > state.PosMax) { return false; }

            if (!silent)
            {
                var open = state.Push("mark_open", "span", 1);
                open.Markup = "==";

                var text = state.Push("text", "", 0);
                text.Content = state.Src.Substring(start + 2, end - start - 2);

                var close = state.Push("mark_close", "span", -1);
                close.Markup = "==";
            }

            state.Pos = end + 2;
            return true;
        }
    }

    // Use it
    var md = new MarkdownParser();
    md.Use(MarkPlugin.Apply);
    var html = md.Render("This is ==important== text.");

Rules of thumb that keep a plugin correct:

  - Return false without touching state when the rule does not match; return
    true only after advancing state.Pos (inline) or state.Line (block).
  - Honour the silent flag: push no tokens when it is true.
  - Only add a Renderer rule when the generic Tag/Attrs/Nesting rendering is
    wrong; a token type with no rule still renders.
  - Register with Before / After against a known rule name (see the list
    above); At replaces an existing rule outright.
  - A core rule (md.Core.Ruler) runs over the whole token stream after
    parsing, which is the easiest place to rewrite or annotate tokens.

Note that a custom plugin affects the parser YOU created. The PDF pipeline
builds its own parser internally, so a plugin cannot be injected into
RenderFile / RenderMarkdown. To render custom syntax to PDF, parse with your
own plugged-in parser and hand the HTML to Html2Pdf yourself:

    var body = md.Render(markdown);
    var generated = new MarkdownPdfRenderer().GenerateHtml("");   // for the CSS
    var html = MarkdownPdfRenderer.BuildHtmlDocument(body, generated.Css, "Title");
    new HtmlPdfRenderer().RenderHtml(html, "out.pdf", baseDirectory);

================================================================================

COMPLETE EXAMPLES
=================

Example 1: A Markdown file to a PDF, with warnings checked
----------------------------------------------------------
    using CodeBrix.PdfDocCreate.Html2Pdf;
    using CodeBrix.PdfDocCreate.Markdown2Pdf;

    var renderer = new MarkdownPdfRenderer();
    renderer.Options.PageSize = "a4";
    renderer.Options.FooterText = "{title} - page {page} of {pages}";

    var result = renderer.RenderFile("docs/guide.md", "out/guide.pdf");

    Console.WriteLine($"Wrote {result.OutputFilePath}");
    Console.WriteLine($"{result.PageCount} pages, title \"{result.Title}\"");

    foreach (RenderWarning w in result.Warnings.Items)
    {
        Console.WriteLine($"[{w.Category}] {w.Code} x{w.Occurrences}: {w.Message}");
    }

Example 2: Markdown from a request body, PDF back as bytes
-----------------------------------------------------------
    using CodeBrix.PdfDocCreate.Markdown2Pdf;

    public byte[] MarkdownToPdf(string markdown)
    {
        var renderer = new MarkdownPdfRenderer();
        renderer.Options.AllowRemoteImages = false;   // default; be explicit
        renderer.Options.FooterText = null;           // no page furniture

        var result = renderer.RenderMarkdownToBytes(markdown);
        return result.PdfBytes;
    }

Example 3: Restyle the generated document before rendering
-----------------------------------------------------------
    using CodeBrix.PdfDocCreate.Html2Pdf;
    using CodeBrix.PdfDocCreate.Markdown2Pdf;

    var mdRenderer = new MarkdownPdfRenderer();
    var generated = mdRenderer.GenerateHtmlFromFile("docs/report.md");

    var css = generated.Css
        + "\nh1 { color: #7a1020; border-bottom: 1pt solid #7a1020; }"
        + "\nbody { font-size: 12pt; }";

    var html = generated.ToHtmlDocument(css);

    var htmlRenderer = new HtmlPdfRenderer();
    htmlRenderer.Options.SetPageSize("a4");
    htmlRenderer.Options.MarginTopPoints = 90;
    htmlRenderer.Options.MarginBottomPoints = 90;
    htmlRenderer.Options.HeaderText = "{title}";
    htmlRenderer.Options.DocumentAuthor = "Documentation Team";

    var result = htmlRenderer.RenderHtml(html, "out/report.pdf",
        generated.BaseDirectory);

Example 4: Markdown to HTML only, with a chosen feature set
------------------------------------------------------------
    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
    using CodeBrix.PdfDocCreate.Markdown2Pdf.Plugins;

    // Strict CommonMark, then add GFM tables and strikethrough back
    var md = new MarkdownParser(MarkdownPreset.CommonMark);
    md.Enable(new[] { "table", "strikethrough" });
    md.Use(TaskListPlugin.Apply);

    string html = md.Render(markdownText);
    string inlineOnly = md.RenderInline("just **one** line, no <p> wrapper");

Example 5: Mine a document's structure from the token stream
-------------------------------------------------------------
    using System.Collections.Generic;
    using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

    static List<(int Level, string Text)> ExtractHeadings(string markdown)
    {
        var md = new MarkdownParser();
        var env = new MdEnv();
        var tokens = md.Parse(markdown, env);
        var headings = new List<(int, string)>();

        for (var i = 0; i < tokens.Count; i++)
        {
            if (tokens[i].Type != "heading_open") { continue; }

            var level = int.Parse(tokens[i].Tag.Substring(1));   // "h2" -> 2
            var text = md.Renderer.RenderInlineAsText(
                tokens[i + 1].Children, md.Options, env);
            headings.Add((level, text));
        }

        return headings;
    }

Example 6: Verify the produced PDF by rasterizing it
-----------------------------------------------------
    using CodeBrix.PdfDocCreate.Markdown2Pdf;
    using CodeBrix.PdfRasterizer;

    var result = new MarkdownPdfRenderer().RenderMarkdownToBytes("# Title\n\nBody.");

    using var rasterizer = new PageRasterizer();
    var dims = await rasterizer.GetPageDimensions(result.PdfBytes, pageNumber: 1);

    // US Letter by default: 8.5in x 11in
    Console.WriteLine($"{dims.WidthInInches:F2} x {dims.HeightInInches:F2} inches");

================================================================================

MINIMUM VIABLE PROJECT
======================

    dotnet new console -n MdToPdf --framework net10.0
    cd MdToPdf
    dotnet add package CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever
    # On Linux, ONLY if your Markdown embeds SVG images:
    # dotnet add package SkiaSharp.NativeAssets.Linux.NoDependencies

MdToPdf.csproj:

    <Project Sdk="Microsoft.NET.Sdk">
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>disable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
      </PropertyGroup>
      <ItemGroup>
        <PackageReference
          Include="CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever" />
      </ItemGroup>
    </Project>

Program.cs:

    using CodeBrix.PdfDocCreate.Markdown2Pdf;

    var path = args.Length > 0 ? args[0] : "sample.md";

    if (!File.Exists(path))
    {
        File.WriteAllText(path, """
            ---
            title: Sample Document
            author: A. Coder
            ---

            # Sample Document

            Body text with **bold**, *italic* and `code`.

            | Feature | Supported |
            |---------|:---------:|
            | Tables  |    yes    |
            | Footnotes | yes[^1] |

            - [x] task lists
            - [ ] still to do

            ~~~csharp
            var renderer = new MarkdownPdfRenderer();
            ~~~

            [^1]: Footnotes render at the bottom of the page.
            """);
    }

    var renderer = new MarkdownPdfRenderer();
    var result = renderer.RenderFile(path);

    Console.WriteLine($"Wrote {result.OutputFilePath} ({result.PageCount} pages)");
    foreach (var message in result.Warnings.Messages)
    {
        Console.WriteLine($"  warning: {message}");
    }

Build and run:

    dotnet build
    dotnet run

================================================================================

PERFORMANCE TIPS
================

1. THE FIRST RENDER IN A PROCESS IS THE SLOW ONE. It discovers and parses the
   package font files. Warm that up off the critical path with
   Html2PdfFonts.EnsureRegistered() (from
   CodeBrix.PdfDocCreate.Html2Pdf.Fonts) at start-up, and check
   Html2PdfFonts.HasDefaultFamilies to confirm the fonts were found.

2. REUSE ONE MarkdownPdfRenderer. It is cheap to construct but reusing it keeps
   your options in one place. Do NOT share one instance across threads: each
   call mutates nothing on the instance, but the underlying font registry and
   options are process-wide state that is simplest to touch from one place.

3. GENERATE HTML ONCE WHEN RENDERING SEVERAL VARIANTS. If you need the same
   document at two page sizes, call GenerateHtmlFromFile once and render the
   resulting HTML twice with HtmlPdfRenderer rather than parsing the Markdown
   twice.

4. REUSE A MarkdownParser FOR MANY SMALL DOCUMENTS. Constructing one builds
   four rule chains and a renderer rule table. When you are converting
   thousands of comment bodies to HTML, build the parser once.

5. PARSE ONCE WHEN YOU NEED BOTH TOKENS AND HTML. Call Parse(src, env) and then
   Renderer.Render(tokens, Options, env) with the SAME MdEnv instead of calling
   Render(src) after Parse(src) - that parses twice.

6. TURN OFF WHAT YOU DO NOT USE. md.Disable(new[] { "linkify", "smartquotes",
   "replacements" }) measurably shortens the core chain on large inputs; the
   Zero preset plus Enable is the extreme version.

7. PREFER LOCAL IMAGES. AllowRemoteImages = true makes rendering wait on
   network I/O for every remote image; it is false by default for that reason
   as well as for safety.

8. LOWER SvgRasterScale FOR DRAFTS. It defaults to 2.0; SVG rasterization is
   the most expensive step in an SVG-heavy document, and 1.0 is much faster
   when you only need to check the layout.

================================================================================

COMMON PITFALLS TO AVOID
========================

1. DO NOT expect RenderMarkdownToBytes to return byte[]. It returns a
   MarkdownRenderResult; the bytes are in result.PdfBytes, and
   result.OutputFilePath is null. The file-writing methods are the mirror
   image: OutputFilePath is set and PdfBytes is null.

2. DO NOT confuse the NuGet package name with the namespace.
   Package: CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever
   Namespace: CodeBrix.PdfDocCreate.Markdown2Pdf.*

3. DO NOT forget that the Warnings type lives in the Html2Pdf namespace.
   MarkdownRenderResult.Warnings is a
   CodeBrix.PdfDocCreate.Html2Pdf.RenderWarnings; declaring the variable
   explicitly needs "using CodeBrix.PdfDocCreate.Html2Pdf;".

4. DO NOT ignore Warnings. Nothing throws for a missing image, an unsupported
   CSS property, a skipped SVG or a character no font covers - it is collected.
   A document that "renders but looks wrong" almost always has the explanation
   in result.Warnings.Items (each with Category, Code, Message, Occurrences).

5. DO NOT look for styling options that are not there. MarkdownRenderOptions
   has exactly five properties (PageSize, AllowRemoteImages, FooterText,
   SvgRasterScale, KeepUncoveredCharacters). Margins, fonts and colors are
   changed by intercepting the HTML/CSS with GenerateHtml*, not by a knob.

6. DO NOT expect a plugin to affect RenderFile / RenderMarkdown. Those build
   their own parser internally. Parse with your own parser and hand the HTML
   to Html2Pdf (see WRITING A CUSTOM PLUGIN).

7. DO NOT expect a bare MarkdownParser to have footnotes, task lists or front
   matter. new MarkdownParser() is plain markdown-it; apply FootnotePlugin,
   TaskListPlugin and FrontMatterPlugin yourself.

8. DO NOT pass FrontMatterPlugin.Apply to Use(). Its signature is
   Apply(MarkdownParser md, Action<string> callback), which is not
   Action<MarkdownParser>. Call FrontMatterPlugin.Apply(md, callback) directly.

9. DO NOT assume raw HTML in the Markdown is rendered. MarkdownItOptions.Html
   is FALSE by default on a parser you construct, so embedded HTML is escaped.
   The PDF pipeline switches it on internally; set o.Html = true yourself when
   using MarkdownParser directly.

10. DO NOT assume the CommonMark preset behaves like the default one. It turns
    OFF tables, strikethrough, linkify, typographer, replacements and
    smartquotes, and lowers MaxNesting to 20. Re-enable what you need with
    Enable(...).

11. DO NOT expect javascript:, vbscript:, file: or arbitrary data: links to
    render. The default ValidateLink rejects them and the link degrades to
    plain text - silently. Only data:image/* URIs of the supported image types
    are allowed through.

12. DO NOT rely on remote images. AllowRemoteImages is false by default, so an
    <img src="https://..."> is skipped with a warning. Setting it to true makes
    rendering perform network I/O.

13. DO NOT forget the base directory when rendering from a string. RenderFile
    resolves relative images against the .md file's own folder;
    RenderMarkdown and RenderMarkdownToBytes resolve against the baseDirectory
    argument, which defaults to the CURRENT directory when you omit it.

14. DO NOT expect emoji or other exotic glyphs to appear. Only the package
    fonts (plus fonts you register with Html2PdfFonts) are consulted - system
    fonts are never used. Uncovered characters are dropped and warned about
    unless KeepUncoveredCharacters is true, which makes them .notdef boxes
    instead.

15. DO NOT ship SVG-bearing Markdown to Linux without one of the SkiaSharp
    native-assets packages in the APPLICATION. The SVGs silently vanish and
    the only trace is the "image.svg.nativemissing" warning.

16. DO NOT write a rule that returns true without advancing the state. An
    inline rule must move state.Pos, a block rule must move state.Line;
    returning true without doing so hangs the tokenizer.

17. DO NOT mutate a Token's Attrs list directly when AttrSet / AttrJoin /
    AttrPush will do - Attrs is a List<string[]> that starts null, and the
    helpers create it for you.

18. DO NOT emit unescaped text from a custom renderer rule. Run user content
    through MdUtils.EscapeHtml first.

================================================================================

WHAT THIS PACKAGE DOES NOT DO
=============================

  - It does NOT give you a styling API. Five options, then the HTML/CSS
    hand-off. That is deliberate.
  - It does NOT let you plug custom Markdown syntax into the one-call PDF path.
    Parse with your own MarkdownParser and render through Html2Pdf instead.
  - It does NOT expose the built-in stylesheet as a public constant. Read it
    from MarkdownHtmlResult.Css.
  - It does NOT support Markdown dialects beyond CommonMark plus the GFM
    features and plugins listed above: no definition lists, no abbreviations,
    no MathJax/LaTeX, no Mermaid, no admonition/callout blocks, no wiki links,
    no Liquid or Handlebars templating.
  - It does NOT execute anything: no scripts, no shortcodes, no include
    directives, no front-matter templating. Front matter is consumed for
    title/author and otherwise discarded.
  - It does NOT convert PDF back to Markdown, or Markdown to Word/HTML files
    on disk - GenerateHtml* returns strings for you to write.
  - It does NOT render live web pages, remote stylesheets or JavaScript; it
    inherits Html2Pdf's element and CSS subset exactly (see that package's
    AGENT-README for the boundaries).
  - It does NOT use operating-system fonts, ever.
  - It does NOT throw for bad input. Any .md string produces a document;
    problems become warnings.

================================================================================

WORKING EXAMPLES ON GITHUB
==========================

The package's own test project is the reference for every feature area. Base
URL:
    https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests/

  The whole CommonMark specification example corpus (all 652 examples) replayed
  through MarkdownParser(MarkdownPreset.CommonMark) and compared to the
  reference HTML - the definitive statement of what the parser does:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests/CommonMarkSpecTests.cs

  Parser behaviour: GFM tables and strikethrough matching upstream, the
  footnote / task-list / front-matter plugins applied to a bare parser, fenced
  code with and without a known language, reference links and images, nested
  multi-block lists, the javascript: link validator, and block source maps on
  tokens:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests/MarkdownParserTests.cs

  End-to-end rendering: zero-config bytes, RenderFile's default output path,
  title inference order (front matter, then heading, then file name),
  GenerateHtml returning restylable HTML/CSS, hostile and odd Markdown still
  rendering, relative images resolving against the source directory, the
  PageSize option changing the page geometry (verified by rasterizing), and a
  long document paginating with footer page numbers:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests/MarkdownPdfRendererTests.cs

  Images: every supported raster format as a relative file and as a data: URI,
  referenced SVG files, SVG data: URIs, inline <svg> blocks, backslash-relative
  paths resolving on every platform, and the data: URI allow-list rejecting
  unsafe or unknown types:
    -> https://github.com/ellisnet/CodeBrix.PdfDocuments/tree/main/tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests/MarkdownImageFormatTests.cs

HOW TO USE: Fetch the raw file content from GitHub using a URL like:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/{path}
For example:
    https://raw.githubusercontent.com/ellisnet/CodeBrix.PdfDocuments/main/tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests/MarkdownPdfRendererTests.cs

================================================================================

QUICK REFERENCE CARD
====================

--- Install ---
    dotnet add package CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever
    Linux + SVG: also ONE of SkiaSharp.NativeAssets.Linux /
                 SkiaSharp.NativeAssets.Linux.NoDependencies (app's choice)
Namespaces:     CodeBrix.PdfDocCreate.Markdown2Pdf,
                CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt (+ .RulesBlock,
                .RulesInline, .RulesCore, .Common, .Helpers),
                CodeBrix.PdfDocCreate.Markdown2Pdf.Plugins,
                CodeBrix.PdfDocCreate.Html2Pdf (the Warnings types)

--- MarkdownPdfRenderer ---
Create:         var r = new MarkdownPdfRenderer()
File to PDF:    var res = r.RenderFile("doc.md")          // doc.pdf beside it
File, choose:   r.RenderFile("doc.md", "out/doc.pdf")
String to file: r.RenderMarkdown(md, "out.pdf", baseDir)
String to bytes:r.RenderMarkdownToBytes(md, baseDir)
HTML from file: var g = r.GenerateHtmlFromFile("doc.md")
HTML from text: var g = r.GenerateHtml(md, baseDir)
Reassemble:     MarkdownPdfRenderer.BuildHtmlDocument(body, css, title)

--- Options (all five) ---
Page size:      r.Options.PageSize = "a4"    // letter(def) legal ledger
                                             // a3 a4 a5 b4 b5
Remote images:  r.Options.AllowRemoteImages = true      // default false
Footer:         r.Options.FooterText = "Page {page} of {pages}"
                                             // default "{page} / {pages}";
                                             // null removes it
SVG sharpness:  r.Options.SvgRasterScale = 3.0          // default 2.0
Tofu opt-in:    r.Options.KeepUncoveredCharacters = true // default false

--- Results ---
MarkdownRenderResult:  .OutputFilePath | .PdfBytes, .PageCount, .Title,
                       .Warnings
MarkdownHtmlResult:    .BodyHtml, .Css, .Title, .BaseDirectory,
                       .ToHtmlDocument(), .ToHtmlDocument(css)
Warnings:              res.Warnings.Count / .Messages / .Items
Warning item:          .Category (Css|Image|Font|Html), .Code, .Message,
                       .Occurrences, .CodePoint

--- Restyle workflow ---
    var g = r.GenerateHtmlFromFile("doc.md");
    var html = g.ToHtmlDocument(g.Css + "\nh1 { color: #7a1020; }");
    new HtmlPdfRenderer().RenderHtml(html, "out.pdf", g.BaseDirectory);

--- markdown-it port ---
Parser:         var md = new MarkdownParser()             // Default preset
Presets:        MarkdownPreset.Default | CommonMark | Zero
Configure:      new MarkdownParser(MarkdownPreset.Default, o => o.Html = true)
Render:         md.Render(src[, env]) / md.RenderInline(src[, env])
Parse:          md.Parse(src, env) / md.ParseInline(src, env)  -> List<Token>
Rules on/off:   md.Enable(names) / md.Disable(names) / md.Configure(preset)
Plugin:         md.Use(MyPlugin.Apply)
Chains:         md.Core / md.Block / md.Inline / md.Renderer
Rulers:         .Ruler.Before(anchor, name, fn) / .After / .At / .Push
                .Enable / .EnableOnly / .Disable / .GetRules
Link policy:    md.ValidateLink / .NormalizeLink / .NormalizeLinkText
Options:        Html, XhtmlOut, Breaks, LangPrefix, Linkify, Typographer,
                Quotes, Highlight, MaxNesting

--- Rule delegates ---
Block:          bool BlockRuleFn(StateBlock s, int startLine, int endLine,
                    bool silent)
Inline:         bool InlineRuleFn(StateInline s, bool silent)
Inline post:    void InlineRule2Fn(StateInline s)
Core:           void CoreRule(StateCore s)
Renderer:       string RendererRuleFn(List<Token> tokens, int idx,
                    MarkdownItOptions options, MdEnv env, Renderer self)

--- Built-in rule names (anchors for Before/After) ---
Core:    normalize, block, inline, linkify, replacements, smartquotes,
         text_join
Block:   table, code, fence, blockquote, hr, list, reference, html_block,
         heading, lheading, paragraph
Inline:  text, linkify, newline, escape, backticks, strikethrough, emphasis,
         link, image, autolink, html_inline, entity
Inline2: balance_pairs, strikethrough, emphasis, fragments_join

--- Token ---
Members:        Type, Tag, Nesting, Level, Attrs, Map, Children, Content,
                Markup, Info, Meta, Block, Hidden
Attributes:     AttrSet(name, value) / AttrGet / AttrJoin / AttrPush / AttrIndex
Push a token:   state.Push(type, tag, nesting)

--- Plugins ---
Footnotes:      md.Use(FootnotePlugin.Apply)
Task lists:     md.Use(TaskListPlugin.Apply)
Front matter:   FrontMatterPlugin.Apply(md, text => frontMatter = text)

--- Helpers ---
Escape:         MdUtils.EscapeHtml(str) / UnescapeAll / NormalizeReference
URLs:           MdUrl.Parse / Format / Encode / Decode
Link parsing:   LinkHelpers.ParseLinkLabel / ParseLinkDestination /
                ParseLinkTitle

--- Highlighted fence languages ---
csharp c# cs | bash sh shell zsh powershell ps1 console | json jsonc |
xml csproj html xaml svg | typescript ts javascript js jsx tsx | python py |
sql | yaml yml | c cpp c++ h hpp        (anything else escapes plainly)

Target: .NET 10 or later

================================================================================
