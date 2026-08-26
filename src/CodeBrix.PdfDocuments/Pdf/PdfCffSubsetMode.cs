namespace CodeBrix.PdfDocuments.Pdf;

/// <summary>
/// How a font with PostScript (CFF) outlines - an OpenType font whose glyphs live in a
/// <c>CFF </c> table rather than in <c>glyf</c>/<c>loca</c> - is embedded when the document
/// is saved. Set through <see cref="PdfDocumentOptions.CffSubsetMode"/>.
/// </summary>
/// <remarks>
/// <para>
/// TrueType-outline fonts have always been embedded as a subset containing only the glyphs
/// a document uses. CFF-outline fonts have not: PdfSharp's subsetter rebuilds the
/// <c>glyf</c> and <c>loca</c> tables and knows nothing about the CFF program, so a
/// CFF face was embedded WHOLE, under a subset-style prefixed name, and declared as a
/// TrueType program (<c>/FontFile2</c>, <c>/CIDFontType2</c>). That is what
/// <see cref="None"/> still does, byte for byte.
/// </para>
/// <para>
/// <see cref="Sparse"/> is opt-in. It rewrites the CFF program so that every charstring
/// the document does not use becomes a one-byte <c>endchar</c>, leaves glyph numbering,
/// <c>cmap</c>, <c>hmtx</c>, the charset and the subroutines untouched, and declares the
/// program the way PDF 32000-1:2008 section 9.9 asks for an OpenType CFF font:
/// <c>/FontFile3</c> with <c>/Subtype /OpenType</c> on a <c>/CIDFontType0</c> descendant,
/// which raises the file's declared version to PDF 1.6 when it is lower.
/// </para>
/// </remarks>
public enum PdfCffSubsetMode
{
    /// <summary>
    /// The default, and the behaviour every version before this option existed had: a
    /// CFF-outline font is embedded whole and declared as <c>/FontFile2</c> on a
    /// <c>/CIDFontType2</c> descendant. Nothing about the output changes.
    /// </summary>
    None = 0,

    /// <summary>
    /// A sparse subset: the charstrings of unused glyphs are replaced by <c>endchar</c>,
    /// glyph indices are preserved, and the program is embedded as <c>/FontFile3</c>
    /// <c>/OpenType</c> on a <c>/CIDFontType0</c> descendant. A CFF program the subsetter
    /// does not handle (a CFF2 program, or one whose Private DICT and local subroutines
    /// are not laid out contiguously) is embedded exactly as <see cref="None"/> would.
    /// </summary>
    Sparse = 1,
}
