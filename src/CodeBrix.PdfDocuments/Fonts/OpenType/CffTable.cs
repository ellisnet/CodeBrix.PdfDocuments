using System;

namespace CodeBrix.PdfDocuments.Fonts.OpenType;

/// <summary>
/// The <c>CFF </c> table of an OpenType font with PostScript outlines, carried as raw
/// bytes so that a rewritten CFF program can be compiled into a new font image by
/// <see cref="OpenTypeFontface.Compile(uint)"/>.
/// </summary>
/// <remarks>
/// The table is never parsed here - <c>CompactFontFormat.CffSubsetter</c> owns the
/// program's structure - and a font read from disk does not create one: it is only ever
/// constructed for the subset image <see cref="OpenTypeFontface.CreateCffFontSubSet"/>
/// builds. Every other table of that image is an <see cref="IRefFontTable"/> reference
/// into the original face.
/// </remarks>
internal sealed class CffTable : OpenTypeFontTable
{
    /// <summary>The table tag, four characters including the trailing space.</summary>
    public const string Tag = TableTagNames.Cff;

    /// <summary>Initializes a table holding the specified CFF program.</summary>
    /// <param name="bytes">The complete CFF program.</param>
    public CffTable(byte[] bytes)
        : base(null, Tag)
    {
        _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        DirectoryEntry.Length = bytes.Length;
    }

    /// <summary>Gets the CFF program the table carries.</summary>
    public byte[] Bytes
    {
        get { return _bytes; }
    }
    readonly byte[] _bytes;

    /// <summary>Records the length and checksum of the padded table.</summary>
    public override void PrepareForCompilation()
    {
        base.PrepareForCompilation();
        DirectoryEntry.Length = _bytes.Length;
        DirectoryEntry.CheckSum = CalcChecksum(Padded());
    }

    /// <summary>Writes the table, padded to a multiple of four bytes.</summary>
    public override void Write(OpenTypeFontWriter writer)
    {
        writer.Write(Padded());
    }

    byte[] Padded()
    {
        int padded = DirectoryEntry.PaddedLength;
        if (padded == _bytes.Length)
            return _bytes;
        byte[] bytes = new byte[padded];
        Buffer.BlockCopy(_bytes, 0, bytes, 0, _bytes.Length);
        return bytes;
    }
}
