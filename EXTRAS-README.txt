================================================================================
EXTRAS-README: CodeBrix.PdfDocuments
Samples, tools and other content in this repository that is not part of a NuGet package
================================================================================

This repository has no samples/ folder, no tools/ folder and no demo
applications. The non-package content is: the inherited upstream documentation
under docs/, the three test projects under tests/, and one optional,
machine-local test-data folder. Each is described below.


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
                                                    NOTICE.txt), a CID-keyed
                                                    variant of it made by
                                                    make-mathjax-cid.py, and
                                                    three small images, all
                                                    embedded as resources; the
                                                    MathJax face is also linked
                                                    into the Html2Pdf tests as
                                                    a file
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


TestResults/
============
Transient output from test runs, git-ignored. Nothing in it is authored content
and it can be deleted at any time.
