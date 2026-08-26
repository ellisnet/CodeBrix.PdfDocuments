using System.Runtime.CompilerServices;

// The test project reads the CFF subsetter's parse results directly, so that a subset
// can be checked structure by structure rather than only through a PDF reader.
[assembly: InternalsVisibleTo("CodeBrix.PdfDocuments.Tests")]
