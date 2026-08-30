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
/// <para>
/// <see cref="Compact"/> goes further, and on a text face it is the one worth having: it
/// also empties the SUBROUTINES no kept glyph calls and the STRINGS no kept glyph is named
/// by, which on the URW faces are together far larger than the charstrings a sparse subset
/// already removed. Measured on C059-Roman with nine glyphs kept, the CFF program goes from
/// 70,299 bytes whole to 24,728 sparse to 4,685 compact.
/// </para>
/// <para>
/// ⚠ NO MODE RENUMBERS GLYPHS, and that is deliberate rather than unfinished. The PDF that
/// embeds the program has already been written with the original glyph indices, and PDF
/// 32000-1:2008 section 9.7.4.2 says that for a <c>/CIDFontType0</c> whose program is not
/// CID-keyed - which every URW text face is - "the CIDs shall be used directly as glyph
/// indices". Renumbering such a face would move every glyph in the document. What
/// renumbering would additionally save was measured at about a tenth of the subset.
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

    /// <summary>
    /// A compact subset: everything <see cref="Sparse"/> does, and in addition the
    /// subroutines no kept glyph calls and the strings no kept glyph is named by are
    /// emptied. Glyph indices, the charset, the Encoding, the FDSelect table and every
    /// subroutine INDEX's item COUNT are all still exactly what they were, so every
    /// charstring and every kept subroutine is carried over byte for byte. A program this
    /// cannot be done safely for falls back to <see cref="Sparse"/>, and one the subsetter
    /// does not handle at all falls back to <see cref="None"/>.
    /// </summary>
    /// <remarks>
    /// Two things about a compact subset are worth knowing before choosing it. A glyph the
    /// document does not use loses its NAME as well as its outline - the string becomes
    /// empty - which nothing in PDF rendering consults but a font-inspection tool would
    /// notice. And the glyphs a <c>seac</c> composite draws with are kept even though
    /// nothing names them, because the accented glyph would otherwise come out blank.
    /// </remarks>
    Compact = 2,
}
