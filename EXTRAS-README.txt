================================================================================
EXTRAS-README: CodeBrix.PdfDocuments
Samples, tools and other content in this repository that is not part of a NuGet package
================================================================================

This repository has no samples/ folder, no tools/ folder and no demo
applications. The non-package content is: the inherited upstream documentation
under docs/, the three test projects under tests/, and three optional,
machine-local test-data sources - a dropped-in folder and two folders named by
environment variables. Each is described below.


docs/ - INHERITED UPSTREAM DOCUMENTATION
========================================
PATH
    docs/                          35 markdown files
        index.md
        PdfSharpCore/index.md, faq.md, samples/ (24 files)
        MigraDocCore/index.md, faq.md, samples/ (6 files)

WHAT IT IS
    The documentation site content that came with the upstream PdfSharpCore and
    MigraDocCore projects, carried over unchanged when this repository was
    forked. The sample pages walk through Hello World, page sizes, graphics,
    text layout, bookmarks, annotations, watermarks, XForms, CMYK colors,
    document splitting and concatenation, protecting and unprotecting documents,
    image export, and the MigraDocCore invoice and image samples.

HOW TO USE IT
    Read the files directly; nothing builds or publishes them. They are plain
    markdown with fenced C# snippets and relative links between pages.

WHAT IT DEMONSTRATES - AND THE TRAP
    It documents the UPSTREAM libraries, not the packages this repository
    produces. The prose says "PdfSharpCore" and "MigraDocCore", the snippets say
    `using PdfSharp.Drawing;` and `using PdfSharp.Pdf;`, and none of those
    namespaces exist in the shipped packages - they were renamed to
    CodeBrix.PdfDocuments.* and CodeBrix.PdfDocCreate.* at fork time. The
    content is still useful as background on the original API and on PDF
    concepts, but every namespace, package name and installation instruction in
    it is stale. The AGENT-README file for the package you are using is the
    correct source for names and usage; README-INDEX.txt maps them.

    Nothing in docs/ is packed into any NuGet package.


tests/ - THE TEST PROJECTS
==========================
PATH
    tests/CodeBrix.PdfDocuments.Tests/
    tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/
    tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests/

WHAT THEY ARE
    The repository's automated test suite - not part of any package, and the
    only other non-package code here. CodeBrix.PdfDocuments.Tests covers the
    PdfDocuments, PdfDocCreate and PdfRasterizer libraries together; the other
    two cover their own library.

    They double as the worked examples the AGENT-README files link to, which is
    why the inherited upstream test file names (CreateSimplePDF.cs, Merge.cs,
    Rendering/TestTable.cs and the rest) are kept as they are.

HOW TO RUN THEM
    dotnet test CodeBrix.PdfDocuments.slnx

    Build, runner and per-project prep are covered in MAINTAINER-README.txt.

SUPPORTING DATA THEY CARRY
    tests/CodeBrix.PdfDocuments.Tests/Assets/       sample PDFs, including
                                                    several password-protected
                                                    ones from different
                                                    producers, plus test images
    tests/CodeBrix.PdfDocuments.Tests/SampleFiles/  a Roboto face, a MathJax
                                                    AMS face (CFF outlines, for
                                                    the CFF subsetting tests;
                                                    Apache-2.0, see its
                                                    NOTICE.txt), two derived
                                                    variants of that face - a
                                                    CID-keyed one made by
                                                    make-mathjax-cid.py and a
                                                    custom-Encoding, seac-using
                                                    one made by
                                                    make-mathjax-encoded.py -
                                                    and three small images, all
                                                    embedded as resources; the
                                                    MathJax face is also linked
                                                    into the Html2Pdf tests as
                                                    a file. The two .py scripts
                                                    are the generators, kept
                                                    beside their output so a
                                                    fixture can be rebuilt if
                                                    the source face changes;
                                                    nothing in the build runs
                                                    them
    tests/CodeBrix.PdfDocCreate.Markdown2Pdf.Tests/Fixtures/
                                                    the CommonMark
                                                    specification test corpus
                                                    (JSON), test-only, never
                                                    packed

    All of that belongs to the test projects themselves; it is committed and is
    not optional.


tests/optional-testing-files/ - OPTIONAL, MACHINE-LOCAL TEST DATA
=================================================================
PATH
    tests/optional-testing-files/lilyport-svg-dialect/
        README.txt                   what the dialect is and what a renderer
                                     must support
        inventory.tsv                the frozen vocabulary: kind, name, count
        SvgDialectInventory.cs       reference copy of the scanner
        SvgDialectInventoryTests.cs  reference copy of the gate

WHAT IT IS
    A frozen inventory of the SVG dialect that a music-engraving engine emits
    for documentation snippets: which elements, attributes and font families
    appear across its output, measured over a large corpus. It is a
    specification a PDF renderer can be checked against - "these eleven
    elements and thirty-four attributes, and nothing else".

    IT IS NOT COMMITTED AND IS NOT EXPECTED TO BE PRESENT. The whole folder is
    git-ignored (.gitignore matches **/optional-testing-files/), because it is
    derived from GPL-3 / GFDL-licensed sources and this repository ships MIT
    packages. It is a drop that a maintainer places on a machine by hand. The
    two .cs files in it are reference copies from their own repository; they are
    not compiled here - no project in this repository globs that folder.

WHAT USES IT
    tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/LilyPortSvgDialectTests.cs. It
    locates the inventory relative to the test assembly's base directory, at
    <repo>/tests/optional-testing-files/lilyport-svg-dialect/inventory.tsv, and
    every test in the class opens with

        Assert.SkipWhen(inventoryPath == null,
            "The LilyPort SVG dialect inventory is not present on this machine.");

    so the suite is green with the folder and green without it - the tests are
    simply reported as skipped. Nothing else in the repository reads it.

WHAT IT DEMONSTRATES
    That Html2Pdf covers the engraving dialect: the test file declares the set
    of SVG elements and attributes Html2Pdf supports, and fails if the inventory
    lists a member outside that set - which is the alarm that the engraving
    dialect has grown a new requirement. The remaining tests render synthetic
    SVG through Html2Pdf, rasterize the resulting PDF with PdfRasterizer and
    assert on the content bounds.

HOW TO USE IT
    Copy the lilyport-svg-dialect folder into tests/optional-testing-files/ and
    re-run tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests. Remove it and the same
    tests skip again.

RULE THAT COMES WITH IT
    No content from that folder may be copied into a committed file. The SVG
    markup in LilyPortSvgDialectTests.cs is entirely synthetic, authored against
    the written specification precisely so that no corpus content enters this
    repository. Keep it that way when you extend those tests.

ONE STALE STATEMENT INSIDE THE DROP
    The drop's own README.txt says Html2Pdf places raster images only and skips
    SVG. That was true when the inventory was measured, and it has since been
    overtaken twice: Html2Pdf first rasterized SVG, and now places it as PDF
    vector content by default (rasterizing only on request, or for the part of
    a picture PDF cannot express). Read that sentence as history. The rest of
    the document - the dialect vocabulary and the notes on millimetre sizing,
    currentColor inheritance, the "sans" generic family and invisible link
    rectangles - still describes what a renderer has to handle.


HTML2PDF_LILYPORT_SVG_CORPUS - OPTIONAL, MACHINE-LOCAL SVG CORPUS
=================================================================
PATH
    Anywhere on the machine. The folder is named by the environment variable
    HTML2PDF_LILYPORT_SVG_CORPUS; nothing in this repository points at a
    location.

WHAT IT IS
    A folder of REAL engraving-engine SVG output - whole pages as the engraver
    emitted them, not synthetic markup. It is the volume counterpart to the
    frozen dialect inventory above: the inventory says which elements and
    attributes may appear, and this says what actually happens when several
    hundred real pictures go through the renderer.

    IT IS NOT COMMITTED AND IS NOT EXPECTED TO BE PRESENT, for the same reason
    the inventory is not: the corpus is GFDL/GPL-3 material and this repository
    ships MIT packages. The test reads it where it lies and copies nothing.

WHAT USES IT
    tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests/LilyPortCorpusGateTests.cs. It
    places every .svg in the folder through the vector route into one document
    and asserts the whole run is clean: no picture failed, nothing fell back to
    a raster, no image XObject was embedded, and the only warnings are the
    per-glyph coverage notes the corpus is known to carry.

HOW THE GATE SKIPS
    The test reads the variable and then

        Assert.SkipWhen(directory == null,
            "HTML2PDF_LILYPORT_SVG_CORPUS is not set to an existing folder.");
        Assert.SkipWhen(files.Count == 0,
            "The corpus folder holds no SVG files.");

    so it is green unset, green pointing at an empty folder, and meaningful only
    when it is pointed at real material - exactly like the dialect gate above.

HOW TO USE IT
    Set HTML2PDF_LILYPORT_SVG_CORPUS to the folder and re-run
    tests/CodeBrix.PdfDocCreate.Html2Pdf.Tests. Unset it and the test skips
    again.

RULE THAT COMES WITH IT
    The same rule the dialect drop carries: no content from that corpus may be
    copied into a committed file.


CODEBRIX_CFF_FONT_SWEEP - OPTIONAL, MACHINE-LOCAL FONT DIRECTORY
================================================================
PATH
    Anywhere on the machine; named by the environment variable
    CODEBRIX_CFF_FONT_SWEEP. On a Debian machine with the URW base-35 fonts
    installed, /usr/share/fonts/opentype/urw-base35 is the material.

WHAT IT IS
    A directory of real fonts with PostScript (CFF) outlines. This repository
    ships three small CFF fixtures that cover the structures a CFF program can
    have one at a time; a directory of production faces covers VOLUME instead,
    which is a different kind of coverage and needs fonts nobody wants in a
    git repository.

WHAT USES IT
    tests/CodeBrix.PdfDocuments.Tests/Fonts/CffSubsetFontSweepTests.cs subsets
    every CFF face in the directory, several glyph sets each, and asserts that
    a subset MOVED NOTHING - every kept charstring, every surviving subroutine
    and the charset byte-identical at their original index, and every subroutine
    INDEX still holding the same item count.

HOW THE GATE SKIPS

        Assert.SkipUnless(Directory.Exists(directory),
            "CODEBRIX_CFF_FONT_SWEEP is not set to an existing folder.");

    Unset, or pointing at a folder that is not there, the test skips.

HOW TO USE IT
    Set CODEBRIX_CFF_FONT_SWEEP to a folder of .otf files and re-run
    tests/CodeBrix.PdfDocuments.Tests.


TestResults/
============
Transient output from test runs, git-ignored. Nothing in it is authored content
and it can be deleted at any time.
