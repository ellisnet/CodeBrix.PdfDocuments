================================================================================
MAINTAINER-README: CodeBrix.PdfDocuments
Notes for people and agents MAINTAINING this repository — not for package consumers
================================================================================

If you are CONSUMING one of the NuGet packages this repository produces, this is
the wrong file. Read the AGENT-README.txt for the package you reference instead;
README-INDEX.txt maps them. Everything below is about building, testing,
packaging and maintaining the repository itself.


PURPOSE AND SCOPE
=================
The repository produces FIVE NuGet packages from five projects under src/. All
five are MIT licensed (PackageLicenseExpression = MIT) and target net10.0 only.

  src/CodeBrix.PdfDocuments
      -> CodeBrix.PdfDocuments.MitLicenseForever
  src/CodeBrix.PdfDocCreate
      -> CodeBrix.PdfDocCreate.MitLicenseForever
  src/CodeBrix.PdfRasterizer
      -> CodeBrix.PdfRasterizer.MitLicenseForever
  src/CodeBrix.PdfDocCreate.Html2Pdf
      -> CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever
  src/CodeBrix.PdfDocCreate.Markdown2Pdf
      -> CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever

Consumer documentation, one file per package:

  CodeBrix.PdfDocuments.MitLicenseForever
      -> AGENT-README.txt                                        (repo root)
  CodeBrix.PdfDocCreate.MitLicenseForever
      -> src/CodeBrix.PdfDocCreate/AGENT-README.txt
  CodeBrix.PdfRasterizer.MitLicenseForever
      -> src/CodeBrix.PdfRasterizer/AGENT-README.txt
  CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever
      -> src/CodeBrix.PdfDocCreate.Html2Pdf/AGENT-README.txt
  CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever
      -> src/CodeBrix.PdfDocCreate.Markdown2Pdf/AGENT-README.txt

The root AGENT-README.txt is the CodeBrix.PdfDocuments package's file, NOT a
repository overview. That is deliberate: the PdfDocuments project packs the root
file (see PACKAGING AND PUBLISHING).

Project reference graph inside the repository:

    CodeBrix.PdfDocuments
        ^-- CodeBrix.PdfDocCreate
        |       ^-- CodeBrix.PdfDocCreate.Html2Pdf
        |               ^-- CodeBrix.PdfDocCreate.Markdown2Pdf
        ^-- CodeBrix.PdfRasterizer

Each ProjectReference becomes a package dependency at pack time, resolved from
the referenced project's PackageId and version. External package references
(CodeBrix.Compression, CodeBrix.Imaging, CodeBrix.MarkupParse,
CodeBrix.StyleSheetParse, CodeBrix.SkiaSvg and the CodeBrix.Platform.Fonts.*
packages) are pinned in the individual csproj files.


REPOSITORY LAYOUT
=================
    CodeBrix.PdfDocuments.slnx   solution: five src projects, a /Tests/ folder
                                 holding the three test projects, and a
                                 /Solution Items/ folder
    global.json                  pins the test runner to Microsoft.Testing.Platform
    LICENSE                      MIT, whole repository
    THIRD-PARTY-NOTICES.txt      upstream notices; see PROVENANCE below
    icon-codebrix-128.png        package icon, packed by all five projects
    README.md                    human-facing readme, packed by all five projects
    AGENT-README.txt             consumer doc for CodeBrix.PdfDocuments
    MAINTAINER-README.txt        this file
    EXTRAS-README.txt            non-package content (docs/, optional test data)
    README-INDEX.txt             map of the readme files
    .gitignore                   includes **/optional-testing-files/ and TestResults

    src/CodeBrix.PdfDocuments/           Charting, Drawing, enums, Exceptions,
                                         Fonts, !internal, Internal, Pdf,
                                         Resources, SilverlightInternals, Utils
    src/CodeBrix.PdfDocCreate/           CompileFixes, enums, Fields, Internals,
                                         IO, Rendering, Resources, Shapes,
                                         Tables, Visitors
    src/CodeBrix.PdfRasterizer/          Pdfium (P/Invoke), runtimes (PDFium
                                         natives, about 54 MB on disk)
    src/CodeBrix.PdfDocCreate.Html2Pdf/  Composition, Css, Fonts, Svg,
                                         buildTransitive (packed MSBuild targets)
    src/CodeBrix.PdfDocCreate.Markdown2Pdf/
                                         MarkdownIt (the vendored parser port),
                                         Plugins, Highlighting

    tests/CodeBrix.PdfDocuments.Tests/           covers PdfDocuments,
                                                 PdfDocCreate and PdfRasterizer
    tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/
    tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests/
    tests/optional-testing-files/                git-ignored, machine-local
                                                 optional test data - see
                                                 EXTRAS-README.txt

    docs/                        inherited upstream documentation - see
                                 EXTRAS-README.txt
    TestResults/                 transient, git-ignored

There is no Directory.Build.props, no Directory.Build.targets and no
.editorconfig. Every csproj carries its own property blocks, and the versioning
block is duplicated verbatim in all five packable projects - a change to the
scheme has to be made five times.


BUILDING
========
Requires the .NET 10 SDK. Every project in the repository - libraries and test
projects alike - targets net10.0 and nothing else.

    dotnet restore CodeBrix.PdfDocuments.slnx
    dotnet build CodeBrix.PdfDocuments.slnx

Things to know before you build:

  - GeneratePackageOnBuild is true on ALL FIVE packable projects, so an ordinary
    build produces five .nupkg files into the projects' bin/<Configuration>/
    folders, each stamped with a new version (see PACKAGING AND PUBLISHING).
    There is no separate pack step and no pack driver script.
  - src/CodeBrix.PdfRasterizer sets AllowUnsafeBlocks - it P/Invokes PDFium.
  - GenerateDocumentationFile is set on Html2Pdf and Markdown2Pdf only, so
    CS1591 (missing XML doc comment) is enforced in those two projects and not
    in the three older ones.
  - src/CodeBrix.PdfDocuments embeds Resources/Messages.restext and
    Messages.de.restext as EmbeddedResource; they are excluded from None first.
  - The PDFium natives are copied to the output directory of anything that
    references PdfRasterizer (CopyToOutputDirectory=PreserveNewest), which makes
    build output for the test projects large.


TESTING
=======
Three test projects, all xunit.v3 with xunit.runner.visualstudio,
Microsoft.NET.Test.Sdk and SilverAssertions. global.json selects the
Microsoft.Testing.Platform runner for the whole repository.

    dotnet test CodeBrix.PdfDocuments.slnx
    dotnet test tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests

No environment variables gate any test; there is no opt-in suite. Special prep
and non-obvious wiring:

  - LINUX SVG NATIVES. tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests and
    tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests each carry a
    PackageReference to SkiaSharp.NativeAssets.Linux.NoDependencies so that SVG
    rasterization works when the suite runs on Linux. THAT REFERENCE BELONGS
    ONLY IN tests/, NEVER IN src/ - see the note under PACKAGING AND PUBLISHING
    about why the Html2Pdf library declares neither Linux variant. The
    reference is harmless on Windows and macOS.
  - BUILD TARGETS IMPORT. The same two test projects <Import> the file
    src/CodeBrix.PdfDocCreate.Html2Pdf/buildTransitive/net10.0/
    CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever.targets directly. A
    package consumer receives that font-copy behaviour through the packed
    buildTransitive folder, but a ProjectReference does not flow buildTransitive
    assets, so the import is what makes the test run exercise the identical
    mechanism. If you change the targets file, both test projects pick the
    change up automatically; if you MOVE it, fix both imports.
  - TEST ASSETS. CodeBrix.PdfDocuments.Tests embeds SampleFiles/ (a Roboto
    face and three small test images) as EmbeddedResource and copies Assets/
    (sample and password-protected PDFs, images) to the output directory.
    CodeBrix.PdfDocCreate.Markdown2Pdf.Tests copies Fixtures/**/*.json - the
    CommonMark specification corpus - to the output directory.
  - SKIPS ARE NORMAL. tests/CodeBrix.PdfDocuments.Tests/IO/LargePDFReadWrite.cs
    carries a permanent [Fact(Skip = "Too slow for Unit test runner")]. Several
    font-coverage and SVG tests call Assert.SkipWhen when a probe assumption
    does not hold on the machine. LilyPortSvgDialectTests skips its whole class
    unless the optional inventory folder is present (EXTRAS-README.txt).
  - CANCELLATION. Tests pass TestContext.Current.CancellationToken to async
    calls, which is what satisfies xUnit1051.
  - INTERNALS ACCESS. Three InternalsVisibleTo.cs files grant it:
      src/CodeBrix.PdfDocCreate/Rendering/InternalsVisibleTo.cs
          -> CodeBrix.PdfDocuments.Tests
      src/CodeBrix.PdfDocCreate.Html2Pdf/InternalsVisibleTo.cs
          -> CodeBrix.PdfDocCreate.Html2Pdf.Tests,
             CodeBrix.PdfDocCreate.Markdown2Pdf,
             CodeBrix.PdfDocCreate.Markdown2Pdf.Tests
      src/CodeBrix.PdfDocCreate.Markdown2Pdf/InternalsVisibleTo.cs
          -> CodeBrix.PdfDocCreate.Markdown2Pdf.Tests
    Note the first one sits in a sub-folder rather than at the project root, and
    that CodeBrix.PdfDocuments has no InternalsVisibleTo.cs of its own - its
    tests live in CodeBrix.PdfDocuments.Tests and use the public surface.


PACKAGING AND PUBLISHING
========================
There are no .nuspec files and no packaging script. Packing is entirely
csproj-driven, and happens on every build.

VERSIONING. All five packable projects use the family date-stamp scheme, with
the explanatory comment block copied verbatim into each csproj:

    1.<x>.<y>.<z>
      x  whole years since _VersionBaseYear (2026)
      y  day of year, UTC, 1-based
      z  minute of day, UTC, 0..1439

Version, AssemblyVersion and FileVersion all take that value. Consequences a
maintainer has to keep in mind: every build produces a NEW version and a fresh
.nupkg; two builds within the SAME UTC minute produce the SAME version, so do
not publish twice inside one minute; and this is not SemVer - major is pinned to
1 and minor encodes the year, so neither signals API compatibility.

WHAT SHIPS IN EVERY NUPKG. All five projects pack, at the package root:

    icon-codebrix-128.png        (repo root)
    README.md                    (repo root)
    THIRD-PARTY-NOTICES.txt      (repo root)
    AGENT-README.txt             (see below - the source differs per project)

WHICH AGENT-README EACH PROJECT PACKS. This is the one packaging detail that is
easy to get wrong:

    CodeBrix.PdfDocuments        packs ..\..\AGENT-README.txt   (the ROOT file)
    CodeBrix.PdfDocCreate        packs its own AGENT-README.txt
    CodeBrix.PdfRasterizer       packs its own AGENT-README.txt
    CodeBrix.PdfDocCreate.Html2Pdf      packs its own AGENT-README.txt
    CodeBrix.PdfDocCreate.Markdown2Pdf  packs its own AGENT-README.txt

MAINTAINER-README.txt, EXTRAS-README.txt and README-INDEX.txt are repository-only
and are NOT packed into any nupkg. Keep it that way: they document the repo, not
the packages.

ADDITIONAL PACKAGE CONTENT.

  - CodeBrix.PdfDocCreate.Html2Pdf packs buildTransitive\**\* into
    buildTransitive/ in the nupkg. That is the MSBuild targets file that copies
    the CodeBrix.Platform.Fonts.* .ttf files into a consuming application's
    output under CodeBrix.Platform.Fonts.<Name>/Fonts/, which is where the
    Html2Pdf font resolver looks. Consumers can opt out with
    CodeBrixHtml2PdfDisableFontCopy=true.
  - CodeBrix.PdfRasterizer packs a PDFium native plus the BSD LICENSE file for
    each supported RID: win-x64, win-x86, win-arm64, osx-x64, osx-arm64,
    linux-x64, linux-arm, linux-arm64, linux-riscv64, android-arm64. WATCH THE
    LINUX-X64 ENTRY: its source folder on disk is runtimes\linux\native (no
    "-x64"), packed to runtimes\linux-x64\native. Every other RID's folder name
    matches its package path.

THE SkiaSharp LINUX NATIVE-ASSETS RULE. The Html2Pdf csproj contains a
COMMENTED-OUT PackageReference to SkiaSharp.NativeAssets.Linux.NoDependencies,
with a comment explaining why. Do not uncomment it, and do not "fix" the missing
dependency. Two mutually exclusive Linux variants exist
(SkiaSharp.NativeAssets.Linux and SkiaSharp.NativeAssets.Linux.NoDependencies);
only the consuming application can choose between them, and declaring one here
would break every consumer that already references the other. A consumer on
Linux that renders SVG references one itself; when neither is present nothing
crashes - SVG images are skipped and the skip is reported as a collected
rendering warning with the code "image.svg.nativemissing". This repository's own
test projects reference the NoDependencies variant so the suite passes on Linux,
which is why that reference exists in tests/ and must never be moved to src/.

PUBLISHING. Push the five .nupkg files produced by a Release build. Because the
version is a clock stamp, the five packages published from one build session
carry very close but not necessarily identical versions; build the solution once
and publish that set rather than rebuilding per project.


PROVENANCE AND VENDORED SOURCES
===============================
THIRD-PARTY-NOTICES.txt at the repo root is the authoritative record and carries
the full license texts. Summary of where the code came from:

  CodeBrix.PdfDocuments
      A fork of PdfSharpCore 1.3.67 (github.com/ststeiger/PdfSharpCore), with
      PdfSharpCore.Charting merged in. PdfSharpCore is itself a port of
      PdfSharp.Xamarin, which is a port of PdfSharp by empira Software GmbH.
      MIT throughout.

  CodeBrix.PdfDocCreate
      A fork of MigraDocCore.DocumentObjectModel with MigraDocCore.Rendering
      merged in, from the same 1.3.67 drop; MigraDocCore descends from empira's
      MigraDoc. MIT.

  CodeBrix.PdfRasterizer
      PDFium P/Invoke bindings and the rendering approach derive from Docnet.Core
      (github.com/GowenGit/docnet, MIT), simplified from CppSharp-generated
      wrappers to hand-written direct P/Invoke. The bundled PDFium natives are
      BSD-licensed builds of Google/Foxit's PDFium; the win-arm64, android-arm64
      and linux-riscv64 binaries come from bblanchon/pdfium-binaries and, for
      linux-riscv64, from the pypdfium2 5.6.0 manylinux wheel.

  CodeBrix.PdfDocCreate.Html2Pdf
      Written for this repository. It composes onto PdfDocCreate and delegates
      parsing to the CodeBrix.MarkupParse and CodeBrix.StyleSheetParse packages
      and SVG rasterization to CodeBrix.SkiaSvg.

  CodeBrix.PdfDocCreate.Markdown2Pdf
      Contains a C# port of markdown-it 14.1.0 (MIT) in the
      CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt namespace, plus ports of
      mdurl 2.0.0 (MIT), markdown-it-footnote 4.0.0 (MIT), markdown-it-task-lists
      (ISC, adapted to emit styleable spans instead of input elements) and
      markdown-it-front-matter (MIT). Named-entity decoding uses
      CodeBrix.MarkupParse in place of the upstream "entities" dependency, and
      hostname punycoding uses System.Globalization.IdnMapping in place of
      punycode.js. linkify-it is NOT ported.

  Test-only third-party content
      CodeBrix.PdfDocCreate.Markdown2Pdf.Tests embeds the CommonMark
      specification test corpus 0.31.2 (CC BY-SA 4.0) as
      Fixtures/commonmark-spec-0.31.2.json. It is used only by tests and is
      never packed.

  Compile shim
      DynamicallyAccessedMemberTypes.cs (BSD 3-Clause, copied from the .NET
      Runtime source tree) exists in src/CodeBrix.PdfDocuments/!internal/ and
      src/CodeBrix.PdfDocCreate/CompileFixes/. It is conditionally compiled for
      older target frameworks only and is not part of the net10.0 build.

  Removed upstream dependencies
      All references to SixLabors.ImageSharp and SixLabors.Fonts were removed at
      fork time; image and font handling comes from the CodeBrix.Imaging and
      CodeBrix.Compression packages instead. Do not reintroduce SixLabors
      packages.

NAMESPACE MAPPING applied to the forked code (never partly revert it):

    PdfSharp / PdfSharpCore    ->  CodeBrix.PdfDocuments
    MigraDoc / MigraDocCore    ->  CodeBrix.PdfDocCreate

The upstream API shape is largely preserved, which is exactly why the namespaces
must not be mixed: a consumer who pastes upstream sample code will compile
against nothing.


CODING CONVENTIONS
==================
These are the repository-specific rules; family-wide CodeBrix conventions apply
on top of them.

  - MIXED FILE ENCODINGS - READ THIS BEFORE ANY GREP-BASED SURVEY. Of the .cs
    files under src/ and tests/, about 65 of roughly 773 are not UTF-8 or ASCII:
    around 30 are ISO-8859-1, around 30 more are 8-bit text whose encoding
    `file` cannot determine, and 5 are UTF-16LE with a byte-order mark. They are
    all inherited fork files and are confined to src/CodeBrix.PdfDocuments (about
    53) and src/CodeBrix.PdfDocCreate (about 12); the three newer projects and
    all test code are UTF-8 or ASCII. The cause is German comments carried over
    from the empira sources.
    CONSEQUENCE: a plain text search SILENTLY UNDER-REPORTS, and how it fails
    depends on the tool. Both GNU grep and ugrep classify the 8-bit files as
    binary and print no matching line for them unless you pass -a (the "binary
    file matches" notice goes to stderr, so a piped survey sees nothing at all).
    The UTF-16LE files split further: ugrep decodes the byte-order mark and
    matches them, GNU grep cannot match them at any setting because their bytes
    are NUL-interleaved. A survey that has to be complete therefore needs -a
    PLUS an encoding-aware pass (read each file trying utf-8, then utf-16, then
    latin-1) rather than trust in either tool. Enumerate the offenders with:

        find src tests -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' \
            -exec file --mime-encoding {} + | grep -v 'utf-8\|us-ascii'

    Do NOT bulk-convert these files to UTF-8. Every line of every converted file
    shows as changed, which buries the real diff and destroys blame.
  - File-scoped namespaces everywhere, including the forked code, which was
    converted at fork time. A handful of block-bodied stragglers remain; convert
    one only when you are already editing that file for another reason.
  - RENAMES CARRY A TRAILING MARKER. About 660 files record the upstream
    spelling in a trailing comment, for example:

        namespace CodeBrix.PdfDocuments.Drawing;
            //Was previously: namespace PdfSharpCore.Drawing;

    (the marker is a trailing comment on the same line in the source; it is
    wrapped here only to fit this file's width)

    Keep those comments, and add one whenever you rename another upstream
    namespace or type. They are how a maintainer maps a file back to upstream.
  - UPSTREAM LICENSE HEADERS STAY. About 617 forked files open with a
    "#region PDFsharp - ..." or "#region MigraDoc - ..." block carrying the
    empira copyright and MIT text. Never delete one; the notices file depends on
    them being present in the shipped source.
  - Nullable reference types are OFF - no <Nullable> property appears in any
    csproj. Do not add `?` annotations to reference types in this repository.
  - Test file naming is split by ancestry. New tests use the family convention
    <Class>Tests.cs (HtmlPdfRendererTests.cs, PdfRasterizerTests.cs,
    MarkdownParserTests.cs). The inherited upstream test files keep their
    original names (CreateSimplePDF.cs, Merge.cs, TestTable.cs, PdfInteger.cs,
    Security/PdfSecurity.cs and so on). DO NOT RENAME THEM: the AGENT-README
    files link them by path as working examples on GitHub.
  - The lower-case folder names in the forked projects (enums, runtimes) and the
    odd ones (!internal, CompileFixes, SilverlightInternals) are inherited.
    Leave them alone; new code goes in properly named sub-folders.


NOTES
=====
  - .slnx SOLUTION ITEMS. The /Solution Items/ folder in
    CodeBrix.PdfDocuments.slnx currently lists .gitignore, AGENT-README.txt,
    icon-codebrix-128.png, LICENSE, README.md and THIRD-PARTY-NOTICES.txt. The
    newer root documents (MAINTAINER-README.txt, EXTRAS-README.txt,
    README-INDEX.txt) are not listed there.
  - Root-level document filenames use dashes, not underscores, and the
    AGENT-README.txt name is referenced from the eight AI-agent pointer stubs
    (AGENTS.md, CLAUDE.md, .clinerules, .cursorrules,
    .cursor/rules/agent-readme.mdc, .windsurfrules,
    .github/copilot-instructions.md, .junie/guidelines.md). Renaming any readme
    means updating those stubs and the packing entries in the csproj files.
  - README.md and the five AGENT-README files are packed into the nupkgs, so an
    error in them ships to consumers. THIRD-PARTY-NOTICES.txt ships too, and is
    a license obligation - keep it in step with any new vendored code.
  - tests/optional-testing-files/ is git-ignored by the pattern
    **/optional-testing-files/ in .gitignore. TestResults/ and the usual
    bin/obj output are ignored as well.
  - The PDFium natives make a full clone about 54 MB heavier than the source
    alone; that is expected, not an accident of history.
