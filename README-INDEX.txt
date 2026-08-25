================================================================================
README-INDEX: CodeBrix.PdfDocuments
Map of the README files in this repository
================================================================================

If you are an AI coding agent: find the NuGet package you are consuming below and
read its AGENT-README file in full. Read MAINTAINER-README.txt only if you are
changing this repository itself.

This repository produces five packages. They are complementary rather than
alternatives: PdfDocCreate builds on PdfDocuments, Html2Pdf builds on
PdfDocCreate, Markdown2Pdf builds on Html2Pdf, and PdfRasterizer turns finished
PDF pages back into images.

AGENT-README FILES (consumer documentation, one per NuGet package)
------------------------------------------------------------------
  AGENT-README.txt
      CodeBrix.PdfDocuments.MitLicenseForever - low-level PDF library: create,
      read, merge and encrypt PDFs and draw text, images, shapes and charts at
      exact coordinates with XGraphics.
  src/CodeBrix.PdfDocCreate/AGENT-README.txt
      CodeBrix.PdfDocCreate.MitLicenseForever - high-level document object
      model: sections, paragraphs, styles, tables, headers and footers, laid
      out and paginated for you, then rendered to PDF.
  src/CodeBrix.PdfRasterizer/AGENT-README.txt
      CodeBrix.PdfRasterizer.MitLicenseForever - renders PDF pages to PNG,
      JPEG, BMP, GIF or TIFF images and thumbnails, and reports page counts and
      dimensions, using the bundled PDFium native engine.
  src/CodeBrix.PdfDocCreate.Html2Pdf/AGENT-README.txt
      CodeBrix.PdfDocCreate.Html2Pdf.MitLicenseForever - renders
      author-written HTML with a documented subset of CSS into PDF through the
      PdfDocCreate model, with packaged fonts so output matches on every
      operating system.
  src/CodeBrix.PdfDocCreate.Markdown2Pdf/AGENT-README.txt
      CodeBrix.PdfDocCreate.Markdown2Pdf.MitLicenseForever - renders any
      Markdown file into a formatted, printable PDF with zero configuration,
      and can hand back the generated HTML and CSS for restyling instead.

MAINTAINER AND EXTRAS
---------------------
  MAINTAINER-README.txt
      Building, testing, packaging, versioning and provenance notes for
      maintainers.
  EXTRAS-README.txt
      Samples, tools and other non-package content in this repository.

GENERAL
-------
  README.md
      Human-facing overview shown on GitHub and nuget.org.
  README-INDEX.txt
      This file.
