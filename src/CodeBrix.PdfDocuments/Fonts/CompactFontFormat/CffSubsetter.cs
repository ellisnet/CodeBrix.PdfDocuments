using System;
using System.Collections.Generic;
using System.IO;

namespace CodeBrix.PdfDocuments.Fonts.CompactFontFormat;

/// <summary>
/// Reads the structure of a CFF (Compact Font Format, Adobe TN #5176) program and writes
/// a SPARSE subset of it: the charstrings of glyphs a document does not use are replaced
/// by a one-byte <c>endchar</c>, and everything else - glyph numbering, the charset, the
/// encoding, the FDSelect table, the Private DICTs and every subroutine - is kept as it
/// was.
/// </summary>
/// <remarks>
/// <para>
/// Keeping glyph indices is what makes this safe to wire into a CID font that has
/// already been written with those indices: the <c>cmap</c> and <c>hmtx</c> tables of
/// the OpenType wrapper, the <c>/W</c> widths array, the ToUnicode map and the
/// Identity-H encoding all keep meaning exactly what they meant. A renumbering subset
/// would be smaller still (subroutines could be pruned and the charset shrunk) and is a
/// possible later mode; the sparse subset already removes the outline data, which is
/// almost all of a CFF program's bytes.
/// </para>
/// <para>
/// The program is rebuilt piece by piece in the order CFF readers expect:
/// header, Name INDEX, Top DICT INDEX, String INDEX, Global Subr INDEX, charset,
/// Encoding, FDSelect, CharStrings INDEX, FDArray INDEX, then each Private DICT with its
/// local Subrs INDEX. Only the DICT operands that carry absolute offsets are re-encoded
/// (always as five-byte integers, so a DICT's size is known before the offsets are);
/// every other operand and every table is copied verbatim. A Private DICT and its local
/// subroutines are copied together as one span, because the Subrs operand inside a
/// Private DICT is an offset RELATIVE to the DICT itself.
/// </para>
/// <para>
/// <see cref="CreateSparseSubset"/> returns null - and the caller embeds the whole
/// program as before - for a CFF2 program, for a Private DICT whose local subroutines
/// do not follow it, or for a program that is not structurally a CFF at all.
/// </para>
/// </remarks>
internal sealed class CffSubsetter
{
    // Top DICT operators that carry absolute offsets into the program.
    const int OpCharset = 15;
    const int OpEncoding = 16;
    const int OpCharStrings = 17;
    const int OpPrivate = 18;
    const int OpSubrs = 19;             // Private DICT: offset relative to the Private DICT
    const int OpEscape = 12;
    const int OpRos = (12 << 8) | 30;   // CID-keyed fonts only
    const int OpFdArray = (12 << 8) | 36;
    const int OpFdSelect = (12 << 8) | 37;

    /// <summary>The Type 2 charstring operator that ends a glyph; on its own, an empty glyph.</summary>
    const byte EndChar = 14;

    readonly byte[] _data;

    CffSubsetter(byte[] data)
    {
        _data = data;
    }

    /// <summary>
    /// Writes a sparse subset of a CFF program.
    /// </summary>
    /// <param name="cff">The complete CFF program (the bytes of an OpenType <c>CFF </c> table).</param>
    /// <param name="usedGlyphs">The glyph indices whose charstrings are kept. Glyph 0 is always kept.</param>
    /// <returns>The subset program, or null when the program is one this class does not handle.</returns>
    /// <exception cref="ArgumentNullException">Either argument is null.</exception>
    /// <exception cref="InvalidDataException">The program is structurally not a CFF.</exception>
    public static byte[] CreateSparseSubset(byte[] cff, IEnumerable<int> usedGlyphs)
    {
        if (cff == null)
            throw new ArgumentNullException(nameof(cff));
        if (usedGlyphs == null)
            throw new ArgumentNullException(nameof(usedGlyphs));

        CffFont font = Parse(cff);
        if (font == null)
            return null;

        CffSubsetter subsetter = new CffSubsetter(cff);
        HashSet<int> keep = subsetter.KeptGlyphs(font, usedGlyphs);
        return subsetter.Write(font, keep);
    }

    /// <summary>
    /// Reads the structure of a CFF program.
    /// </summary>
    /// <param name="cff">The complete CFF program.</param>
    /// <returns>The structure, or null when the program is a CFF2 program.</returns>
    /// <exception cref="InvalidDataException">The program is structurally not a CFF.</exception>
    public static CffFont Parse(byte[] cff)
    {
        if (cff == null)
            throw new ArgumentNullException(nameof(cff));
        if (cff.Length < 4)
            throw new InvalidDataException("The CFF program is shorter than its header.");
        if (cff[0] != 1)
            return cff[0] == 2 ? null : throw new InvalidDataException("The CFF program's major version is neither 1 nor 2.");

        CffSubsetter reader = new CffSubsetter(cff);
        CffFont font = new CffFont { HeaderSize = cff[2] };

        int pos = font.HeaderSize;
        font.NameIndex = reader.ReadIndex(pos);
        font.TopDictIndex = reader.ReadIndex(font.NameIndex.End);
        if (font.TopDictIndex.Count < 1)
            throw new InvalidDataException("The CFF program has no Top DICT.");
        font.StringIndex = reader.ReadIndex(font.TopDictIndex.End);
        font.GlobalSubrIndex = reader.ReadIndex(font.StringIndex.End);

        font.TopDict = reader.ReadDict(font.TopDictIndex.ItemStart(0), font.TopDictIndex.ItemEnd(0));
        font.IsCidKeyed = font.TopDict.Find(OpRos) != null;

        DictEntry charStrings = font.TopDict.Find(OpCharStrings)
            ?? throw new InvalidDataException("The CFF program's Top DICT names no CharStrings.");
        font.CharStringsOffset = (int)charStrings.Operands[0];
        font.CharStringsIndex = reader.ReadIndex(font.CharStringsOffset);
        font.GlyphCount = font.CharStringsIndex.Count;

        DictEntry charset = font.TopDict.Find(OpCharset);
        font.CharsetOffset = charset != null ? (int)charset.Operands[0] : 0;
        DictEntry encoding = font.TopDict.Find(OpEncoding);
        font.EncodingOffset = encoding != null ? (int)encoding.Operands[0] : 0;

        if (font.IsCidKeyed)
        {
            DictEntry fdArray = font.TopDict.Find(OpFdArray)
                ?? throw new InvalidDataException("The CID-keyed CFF program names no FDArray.");
            DictEntry fdSelect = font.TopDict.Find(OpFdSelect)
                ?? throw new InvalidDataException("The CID-keyed CFF program names no FDSelect.");
            font.FdArrayOffset = (int)fdArray.Operands[0];
            font.FdSelectOffset = (int)fdSelect.Operands[0];
            font.FdArrayIndex = reader.ReadIndex(font.FdArrayOffset);
            for (int idx = 0; idx < font.FdArrayIndex.Count; idx++)
            {
                CffDict fdDict = reader.ReadDict(font.FdArrayIndex.ItemStart(idx), font.FdArrayIndex.ItemEnd(idx));
                DictEntry priv = fdDict.Find(OpPrivate);
                PrivateDict privateDict = priv != null
                    ? reader.ReadPrivate((int)priv.Operands[0], (int)priv.Operands[1])
                    : null;
                font.FontDicts.Add(new FontDict { Dict = fdDict, Private = privateDict });
            }
        }
        else
        {
            DictEntry priv = font.TopDict.Find(OpPrivate);
            if (priv != null)
                font.Private = reader.ReadPrivate((int)priv.Operands[0], (int)priv.Operands[1]);
        }

        return font;
    }

    // ----- writing -----

    /// <summary>
    /// The byte blocks a subset program is laid out from. Every one of them is finished -
    /// laying out only chooses where each goes and re-encodes the offsets that name them.
    /// </summary>
    sealed class SubsetPieces
    {
        /// <summary>The header, copied verbatim.</summary>
        public byte[] Header;
        /// <summary>The Name INDEX, copied verbatim.</summary>
        public byte[] NameIndex;
        /// <summary>The String INDEX.</summary>
        public byte[] StringIndex;
        /// <summary>The Global Subr INDEX.</summary>
        public byte[] GlobalSubrIndex;
        /// <summary>The charset, or null when the Top DICT names a predefined one.</summary>
        public byte[] Charset;
        /// <summary>The Encoding, or null when the Top DICT names a predefined one.</summary>
        public byte[] Encoding;
        /// <summary>The FDSelect (CID-keyed fonts), or null.</summary>
        public byte[] FdSelect;
        /// <summary>The CharStrings, one item per glyph of the subset.</summary>
        public List<byte[]> CharStrings;
        /// <summary>Each Private DICT with its local Subrs INDEX, one span each; entries may be null.</summary>
        public List<byte[]> PrivateSpans;
    }

    byte[] Write(CffFont font, HashSet<int> keep)
    {
        SubsetPieces pieces = new SubsetPieces
        {
            Header = Slice(0, font.HeaderSize),
            NameIndex = Slice(font.NameIndex.Start, font.NameIndex.End),
            StringIndex = Slice(font.StringIndex.Start, font.StringIndex.End),
            GlobalSubrIndex = Slice(font.GlobalSubrIndex.Start, font.GlobalSubrIndex.End),
            Charset = font.CharsetOffset > 2 ? Slice(font.CharsetOffset, font.CharsetOffset + CharsetLength(font.CharsetOffset, font.GlyphCount)) : null,
            Encoding = font.EncodingOffset > 1 ? Slice(font.EncodingOffset, font.EncodingOffset + EncodingLength(font.EncodingOffset)) : null,
            FdSelect = font.IsCidKeyed ? Slice(font.FdSelectOffset, font.FdSelectOffset + FdSelectLength(font.FdSelectOffset, font.GlyphCount)) : null,
        };

        // The rebuilt CharStrings INDEX: kept glyphs verbatim, the rest a bare endchar.
        pieces.CharStrings = new List<byte[]>(font.GlyphCount);
        for (int glyph = 0; glyph < font.GlyphCount; glyph++)
        {
            pieces.CharStrings.Add(keep.Contains(glyph)
                ? Slice(font.CharStringsIndex.ItemStart(glyph), font.CharStringsIndex.ItemEnd(glyph))
                : new[] { EndChar });
        }

        // Private DICTs travel with their local subroutines as one span each.
        pieces.PrivateSpans = new List<byte[]>();
        if (font.IsCidKeyed)
        {
            foreach (FontDict fd in font.FontDicts)
            {
                if (fd.Private == null)
                {
                    pieces.PrivateSpans.Add(null);
                    continue;
                }
                byte[] span = PrivateSpan(fd.Private);
                if (span == null)
                    return null;
                pieces.PrivateSpans.Add(span);
            }
        }
        else if (font.Private != null)
        {
            byte[] span = PrivateSpan(font.Private);
            if (span == null)
                return null;
            pieces.PrivateSpans.Add(span);
        }

        return Layout(font, pieces);
    }

    /// <summary>
    /// Places the finished pieces one after another and writes the DICTs that name them.
    /// </summary>
    /// <param name="font">The parsed program the subset is built from.</param>
    /// <param name="pieces">The finished byte blocks.</param>
    /// <returns>The subset program.</returns>
    /// <remarks>
    /// The pieces are emitted in the order CFF readers expect: header, Name INDEX, Top DICT
    /// INDEX, String INDEX, Global Subr INDEX, charset, Encoding, FDSelect, CharStrings
    /// INDEX, FDArray INDEX, then each Private DICT with its local Subrs INDEX. Sizes are
    /// settled before offsets, which the five-byte integer form makes possible: a DICT's
    /// size does not depend on the values its offset operands will carry.
    /// </remarks>
    byte[] Layout(CffFont font, SubsetPieces pieces)
    {
        byte[] charStringsIndex = BuildIndex(pieces.CharStrings);

        int topDictSize = BuildTopDict(font, 0, 0, 0, 0, 0, 0).Length;
        int topDictIndexSize = IndexSize(new[] { topDictSize });

        int fdArrayIndexSize = 0;
        if (font.IsCidKeyed)
        {
            int[] fdSizes = new int[font.FontDicts.Count];
            for (int idx = 0; idx < fdSizes.Length; idx++)
                fdSizes[idx] = BuildFontDict(font.FontDicts[idx], 0, 0).Length;
            fdArrayIndexSize = IndexSize(fdSizes);
        }

        // Now the layout.
        int pos = pieces.Header.Length + pieces.NameIndex.Length + topDictIndexSize
            + pieces.StringIndex.Length + pieces.GlobalSubrIndex.Length;
        int charsetOffset = font.CharsetOffset;
        if (pieces.Charset != null) { charsetOffset = pos; pos += pieces.Charset.Length; }
        int encodingOffset = font.EncodingOffset;
        if (pieces.Encoding != null) { encodingOffset = pos; pos += pieces.Encoding.Length; }
        int fdSelectOffset = 0;
        if (pieces.FdSelect != null) { fdSelectOffset = pos; pos += pieces.FdSelect.Length; }
        int charStringsOffset = pos;
        pos += charStringsIndex.Length;
        int fdArrayOffset = 0;
        if (font.IsCidKeyed) { fdArrayOffset = pos; pos += fdArrayIndexSize; }
        int[] privateOffsets = new int[pieces.PrivateSpans.Count];
        for (int idx = 0; idx < pieces.PrivateSpans.Count; idx++)
        {
            if (pieces.PrivateSpans[idx] == null)
                continue;
            privateOffsets[idx] = pos;
            pos += pieces.PrivateSpans[idx].Length;
        }

        // The DICTs with their real offsets.
        int topPrivateSize = 0, topPrivateOffset = 0;
        if (!font.IsCidKeyed && font.Private != null)
        {
            topPrivateSize = font.Private.Size;
            topPrivateOffset = privateOffsets[0];
        }
        byte[] topDict = BuildTopDict(font, charsetOffset, encodingOffset, charStringsOffset, topPrivateSize, topPrivateOffset, fdArrayOffset, fdSelectOffset);
        if (topDict.Length != topDictSize)
            throw new InvalidOperationException("The Top DICT changed size between the two passes.");
        byte[] topDictIndex = BuildIndex(new List<byte[]> { topDict });

        byte[] fdArrayIndex = null;
        if (font.IsCidKeyed)
        {
            List<byte[]> fdDicts = new List<byte[]>(font.FontDicts.Count);
            for (int idx = 0; idx < font.FontDicts.Count; idx++)
            {
                FontDict fd = font.FontDicts[idx];
                fdDicts.Add(BuildFontDict(fd, fd.Private != null ? fd.Private.Size : 0, fd.Private != null ? privateOffsets[idx] : 0));
            }
            fdArrayIndex = BuildIndex(fdDicts);
            if (fdArrayIndex.Length != fdArrayIndexSize)
                throw new InvalidOperationException("The FDArray INDEX changed size between the two passes.");
        }

        // Emit.
        MemoryStream output = new MemoryStream(pos);
        output.Write(pieces.Header, 0, pieces.Header.Length);
        output.Write(pieces.NameIndex, 0, pieces.NameIndex.Length);
        output.Write(topDictIndex, 0, topDictIndex.Length);
        output.Write(pieces.StringIndex, 0, pieces.StringIndex.Length);
        output.Write(pieces.GlobalSubrIndex, 0, pieces.GlobalSubrIndex.Length);
        if (pieces.Charset != null) output.Write(pieces.Charset, 0, pieces.Charset.Length);
        if (pieces.Encoding != null) output.Write(pieces.Encoding, 0, pieces.Encoding.Length);
        if (pieces.FdSelect != null) output.Write(pieces.FdSelect, 0, pieces.FdSelect.Length);
        output.Write(charStringsIndex, 0, charStringsIndex.Length);
        if (fdArrayIndex != null) output.Write(fdArrayIndex, 0, fdArrayIndex.Length);
        foreach (byte[] span in pieces.PrivateSpans)
        {
            if (span != null)
                output.Write(span, 0, span.Length);
        }
        if (output.Length != pos)
            throw new InvalidOperationException("The subset program's layout and its bytes disagree.");
        return output.ToArray();
    }

    /// <summary>
    /// Writes a COMPACT subset of a CFF program: the sparse subset, plus the subroutines
    /// no kept glyph calls and the strings no kept glyph names removed.
    /// </summary>
    /// <param name="cff">The complete CFF program (the bytes of an OpenType <c>CFF </c> table).</param>
    /// <param name="usedGlyphs">The glyph indices whose charstrings are kept. Glyph 0 is always kept.</param>
    /// <returns>The subset program, or null when the program is one this class does not handle.</returns>
    /// <exception cref="ArgumentNullException">Either argument is null.</exception>
    /// <exception cref="InvalidDataException">The program is structurally not a CFF.</exception>
    /// <remarks>
    /// <para>
    /// GLYPH NUMBERING IS PRESERVED, exactly as <see cref="CreateSparseSubset"/> preserves
    /// it, and for the same reason: the PDF that embeds this program has already been
    /// written with the original glyph indices, and for a <c>/CIDFontType0</c> whose
    /// program is not CID-keyed, PDF 32000-1:2008 section 9.7.4.2 says the CIDs "shall be
    /// used directly as glyph indices". Renumbering such a font would move every glyph.
    /// </para>
    /// <para>
    /// What it removes instead are the two things that do not depend on numbering. The
    /// SUBROUTINES a subset keeps are only those the kept charstrings reach, transitively;
    /// dropping the rest changes the bias every <c>callsubr</c> and <c>callgsubr</c>
    /// operand is written against, so the kept charstrings and subroutines are rewritten
    /// to match. The STRINGS a subset keeps are only those still named - by the charset
    /// entry of a kept glyph, by a DICT, or by an Encoding supplement; the rest become
    /// empty strings, which keeps every SID valid and the charset untouched, and is
    /// smaller than rewriting the charset would be.
    /// </para>
    /// <para>
    /// The kept set also grows by the <c>seac</c> closure: the four-operand form of
    /// <c>endchar</c> draws an accented glyph as two others, named by StandardEncoding
    /// code, and a document that uses the composite uses those two without naming them.
    /// </para>
    /// </remarks>
    public static byte[] CreateCompactSubset(byte[] cff, IEnumerable<int> usedGlyphs)
    {
        if (cff == null)
            throw new ArgumentNullException(nameof(cff));
        if (usedGlyphs == null)
            throw new ArgumentNullException(nameof(usedGlyphs));

        CffFont font = Parse(cff);
        if (font == null)
            return null;

        CffSubsetter subsetter = new CffSubsetter(cff);
        return subsetter.WriteCompact(font, subsetter.KeptGlyphs(font, usedGlyphs));
    }

    /// <summary>
    /// The glyphs a subset keeps: glyph 0, the ones the document asked for, and the ones a
    /// <c>seac</c> composite among them draws with.
    /// </summary>
    /// <param name="font">The parsed program.</param>
    /// <param name="usedGlyphs">The glyph indices the document asked for.</param>
    /// <returns>The glyphs to keep.</returns>
    /// <remarks>
    /// BOTH subsets need this. The four-operand form of <c>endchar</c> draws an accented
    /// glyph out of two others named by StandardEncoding code, and nothing else in the
    /// document names those two - so a subset that kept only what was asked for would keep
    /// the composite's own charstring and blank the two glyphs it is made of, and the
    /// accented glyph would render as nothing at all.
    /// </remarks>
    HashSet<int> KeptGlyphs(CffFont font, IEnumerable<int> usedGlyphs)
    {
        HashSet<int> keep = new HashSet<int> { 0 };
        foreach (int glyph in usedGlyphs)
        {
            if (glyph >= 0 && glyph < font.GlyphCount)
                keep.Add(glyph);
        }
        // A scan that fails leaves the asked-for set alone, which is what this subsetter
        // did before it looked for composites at all.
        ScanClosure(font, keep, ReadCharsetIds(font));
        return keep;
    }

    byte[] WriteCompact(CffFont font, HashSet<int> keep)
    {
        int[] charsetIds = ReadCharsetIds(font);

        // Re-walked over the final kept set, so the subroutine closure covers the glyphs
        // the seac expansion added as well.
        CffCharStringScanner scanner = ScanClosure(font, keep, charsetIds);
        if (scanner.Failed)
            return null;

        SubsetPieces pieces = new SubsetPieces
        {
            Header = Slice(0, font.HeaderSize),
            NameIndex = Slice(font.NameIndex.Start, font.NameIndex.End),
            Charset = font.CharsetOffset > 2 ? Slice(font.CharsetOffset, font.CharsetOffset + CharsetLength(font.CharsetOffset, font.GlyphCount)) : null,
            Encoding = font.EncodingOffset > 1 ? Slice(font.EncodingOffset, font.EncodingOffset + EncodingLength(font.EncodingOffset)) : null,
            FdSelect = font.IsCidKeyed ? Slice(font.FdSelectOffset, font.FdSelectOffset + FdSelectLength(font.FdSelectOffset, font.GlyphCount)) : null,
            GlobalSubrIndex = EmptyUnusedSubrs(font.GlobalSubrIndex, scanner.UsedGlobalSubrs),
        };

        // The CharStrings, exactly as the sparse subset builds them: a kept glyph verbatim,
        // everything else a bare endchar.
        pieces.CharStrings = new List<byte[]>(font.GlyphCount);
        for (int glyph = 0; glyph < font.GlyphCount; glyph++)
        {
            pieces.CharStrings.Add(keep.Contains(glyph)
                ? Slice(font.CharStringsIndex.ItemStart(glyph), font.CharStringsIndex.ItemEnd(glyph))
                : new[] { EndChar });
        }

        // Each Private DICT verbatim, with the slots of its unreached local subroutines emptied.
        pieces.PrivateSpans = new List<byte[]>();
        foreach (PrivateDict privateDict in PrivateDicts(font))
        {
            if (privateDict == null)
            {
                pieces.PrivateSpans.Add(null);
                continue;
            }
            byte[] span = CompactPrivateSpan(privateDict, scanner.UsedLocalSubrs(privateDict.Subrs));
            if (span == null)
                return null;
            pieces.PrivateSpans.Add(span);
        }

        pieces.StringIndex = CompactStringIndex(font, keep, charsetIds);
        return Layout(font, pieces);
    }

    /// <summary>
    /// An INDEX with the same number of items, in which every item nothing reaches is now
    /// zero-length.
    /// </summary>
    /// <param name="index">The INDEX to compact.</param>
    /// <param name="used">The items to carry over verbatim.</param>
    /// <returns>The compacted INDEX.</returns>
    /// <remarks>
    /// Keeping the COUNT is the whole point: a subroutine operand is written as its index
    /// less a bias taken from that count, so removing items would move the bias and every
    /// operand in every charstring and surviving subroutine would have to be re-encoded -
    /// which cannot be done reliably, because a subroutine's own parse depends on the stack
    /// its caller left (see <see cref="CffCharStringScanner"/>). Emptying the slots instead
    /// leaves every operand meaning exactly what it meant. It costs one offset per dropped
    /// subroutine, which is one or two bytes against the dozens the subroutine held.
    /// </remarks>
    byte[] EmptyUnusedSubrs(CffIndex index, HashSet<int> used)
    {
        if (index.Count == 0)
            return Slice(index.Start, index.End);
        List<byte[]> items = new List<byte[]>(index.Count);
        for (int idx = 0; idx < index.Count; idx++)
        {
            items.Add(used.Contains(idx)
                ? Slice(index.ItemStart(idx), index.ItemEnd(idx))
                : Array.Empty<byte>());
        }
        return BuildIndex(items);
    }

    /// <summary>
    /// Walks the kept glyphs, then the glyphs their <c>seac</c> composites draw with, until
    /// nothing new is reached.
    /// </summary>
    CffCharStringScanner ScanClosure(CffFont font, HashSet<int> keep, int[] charsetIds)
    {
        CffCharStringScanner scanner = new CffCharStringScanner(_data, font.GlobalSubrIndex);
        HashSet<int> walked = new HashSet<int>();
        // A seac component may itself be a composite, so this settles rather than assuming
        // one round; the bound is there because a font could name itself.
        for (int round = 0; round < 8; round++)
        {
            List<int> pending = new List<int>();
            foreach (int glyph in keep)
            {
                if (!walked.Contains(glyph))
                    pending.Add(glyph);
            }
            if (pending.Count == 0)
                break;
            foreach (int glyph in pending)
            {
                walked.Add(glyph);
                scanner.Collect(font.CharStringsIndex.ItemStart(glyph), font.CharStringsIndex.ItemEnd(glyph),
                    LocalSubrsFor(font, glyph));
            }
            // seac names its components by StandardEncoding code; the charset turns the
            // SID that names into the glyph that draws.
            if (charsetIds == null || font.IsCidKeyed)
                continue;
            foreach (int code in scanner.SeacCodes)
            {
                if (code < 0 || code > 255)
                    continue;
                int sid = CffCharStringScanner.StandardEncodingSids[code];
                if (sid == 0)
                    continue;
                for (int glyph = 0; glyph < charsetIds.Length; glyph++)
                {
                    if (charsetIds[glyph] == sid)
                    {
                        keep.Add(glyph);
                        break;
                    }
                }
            }
        }
        return scanner;
    }

    /// <summary>The Private DICTs of the program, in FDArray order for a CID-keyed font.</summary>
    static IEnumerable<PrivateDict> PrivateDicts(CffFont font)
    {
        if (font.IsCidKeyed)
        {
            foreach (FontDict fd in font.FontDicts)
                yield return fd.Private;
        }
        else if (font.Private != null)
        {
            yield return font.Private;
        }
    }

    /// <summary>The Local Subr INDEX that applies to a glyph, or null when there is none.</summary>
    CffIndex LocalSubrsFor(CffFont font, int glyph)
    {
        if (!font.IsCidKeyed)
            return font.Private != null ? font.Private.Subrs : null;
        int fd = FdForGlyph(font, glyph);
        if (fd < 0 || fd >= font.FontDicts.Count)
            return null;
        PrivateDict privateDict = font.FontDicts[fd].Private;
        return privateDict != null ? privateDict.Subrs : null;
    }

    /// <summary>Reads the Font DICT index a glyph uses from the FDSelect table.</summary>
    int FdForGlyph(CffFont font, int glyph)
    {
        if (_fdSelect == null)
            _fdSelect = ReadFdSelect(font);
        return glyph >= 0 && glyph < _fdSelect.Length ? _fdSelect[glyph] : 0;
    }
    int[] _fdSelect;

    int[] ReadFdSelect(CffFont font)
    {
        int[] map = new int[font.GlyphCount];
        int offset = font.FdSelectOffset;
        int format = _data[offset];
        if (format == 0)
        {
            for (int glyph = 0; glyph < font.GlyphCount; glyph++)
                map[glyph] = _data[offset + 1 + glyph];
        }
        else if (format == 3)
        {
            int ranges = ReadUInt16(offset + 1);
            int pos = offset + 3;
            for (int idx = 0; idx < ranges; idx++)
            {
                int first = ReadUInt16(pos);
                int fd = _data[pos + 2];
                // A range entry is three bytes, and the sentinel that closes the last one
                // sits exactly where the next range's first glyph would: one read serves both.
                int next = ReadUInt16(pos + 3);
                for (int glyph = first; glyph < next && glyph < map.Length; glyph++)
                    map[glyph] = fd;
                pos += 3;
            }
        }
        else
        {
            throw new InvalidDataException("A CFF FDSelect has an unknown format.");
        }
        return map;
    }

    /// <summary>
    /// The charset as an array of identifiers, one per glyph - SIDs for a font that is not
    /// CID-keyed, CIDs for one that is - or null when the Top DICT names a predefined
    /// charset this class does not carry a table for.
    /// </summary>
    int[] ReadCharsetIds(CffFont font)
    {
        int[] ids = new int[font.GlyphCount];
        int offset = font.CharsetOffset;
        if (offset == 0)
        {
            // ISOAdobe: glyph n is SID n, which is what an identity charset looks like.
            for (int glyph = 0; glyph < ids.Length; glyph++)
                ids[glyph] = glyph;
            return ids;
        }
        if (offset == 1 || offset == 2)
            return null;    // Expert and ExpertSubset; no table here, so nothing is compacted.

        int format = _data[offset];
        int pos = offset + 1;
        int covered = 1;    // glyph 0 is .notdef, never listed
        if (format == 0)
        {
            while (covered < font.GlyphCount)
            {
                ids[covered++] = ReadUInt16(pos);
                pos += 2;
            }
            return ids;
        }
        if (format == 1 || format == 2)
        {
            while (covered < font.GlyphCount)
            {
                int first = ReadUInt16(pos);
                int left = format == 1 ? _data[pos + 2] : ReadUInt16(pos + 2);
                pos += format == 1 ? 3 : 4;
                for (int idx = 0; idx <= left && covered < font.GlyphCount; idx++)
                    ids[covered++] = first + idx;
            }
            return ids;
        }
        throw new InvalidDataException("A CFF charset has an unknown format.");
    }

    /// <summary>
    /// The Private DICT verbatim, followed by its local Subrs INDEX with the slots of the
    /// subroutines no kept glyph reaches emptied.
    /// </summary>
    /// <param name="privateDict">The Private DICT.</param>
    /// <param name="usedLocalSubrs">The local subroutines the kept glyphs reach.</param>
    /// <returns>The span, or null when the subroutines do not follow the DICT.</returns>
    /// <remarks>
    /// The Subrs operand inside a Private DICT is an offset RELATIVE to the DICT, and the
    /// DICT is copied byte for byte, so the compacted INDEX has to begin at the same
    /// distance the original one did - which is what the padding is for. A DICT whose
    /// subroutines do not follow it is refused, exactly as the sparse subset refuses it.
    /// </remarks>
    byte[] CompactPrivateSpan(PrivateDict privateDict, HashSet<int> usedLocalSubrs)
    {
        if (privateDict.Subrs == null)
            return Slice(privateDict.Offset, privateDict.Offset + privateDict.Size);

        int relative = privateDict.SubrsRelativeOffset.Value;
        if (relative < privateDict.Size)
            return null;

        byte[] subrsIndex = EmptyUnusedSubrs(privateDict.Subrs, usedLocalSubrs);
        MemoryStream span = new MemoryStream(relative + subrsIndex.Length);
        span.Write(_data, privateDict.Offset, privateDict.Size);
        for (int pad = privateDict.Size; pad < relative; pad++)
            span.WriteByte(0);
        span.Write(subrsIndex, 0, subrsIndex.Length);
        return span.ToArray();
    }

    /// <summary>
    /// The String INDEX with every string no longer named replaced by an empty one, which
    /// leaves every SID meaning what it meant and the charset untouched.
    /// </summary>
    byte[] CompactStringIndex(CffFont font, HashSet<int> keep, int[] charsetIds)
    {
        int count = font.StringIndex.Count;
        if (count == 0)
            return Slice(font.StringIndex.Start, font.StringIndex.End);

        // An Encoding SUPPLEMENT names glyphs by SID as well. Rather than carry a parser
        // for a structure no font on hand uses - and so no test could exercise - a program
        // that has one keeps every string.
        if (HasEncodingSupplement(font))
            return Slice(font.StringIndex.Start, font.StringIndex.End);

        HashSet<int> live = new HashSet<int>();
        CollectDictSids(font.TopDict, live);
        foreach (FontDict fd in font.FontDicts)
            CollectDictSids(fd.Dict, live);
        if (charsetIds != null && !font.IsCidKeyed)
        {
            foreach (int glyph in keep)
            {
                if (glyph >= 0 && glyph < charsetIds.Length)
                    live.Add(charsetIds[glyph]);
            }
        }
        else if (charsetIds == null)
        {
            // Nothing could be read, so nothing is claimed: keep every string.
            return Slice(font.StringIndex.Start, font.StringIndex.End);
        }

        List<byte[]> strings = new List<byte[]>(count);
        for (int idx = 0; idx < count; idx++)
        {
            strings.Add(live.Contains(StandardStringCount + idx)
                ? Slice(font.StringIndex.ItemStart(idx), font.StringIndex.ItemEnd(idx))
                : Array.Empty<byte>());
        }
        return BuildIndex(strings);
    }

    /// <summary>Adds the SID operands a DICT carries to the live set.</summary>
    static void CollectDictSids(CffDict dict, HashSet<int> live)
    {
        if (dict == null)
            return;
        foreach (DictEntry entry in dict.Entries)
        {
            int operandCount = SidOperandCount(entry.Operator);
            for (int idx = 0; idx < operandCount && idx < entry.Operands.Length; idx++)
            {
                double operand = entry.Operands[idx];
                if (!double.IsNaN(operand))
                    live.Add((int)operand);
            }
        }
    }

    /// <summary>How many of a DICT operator's leading operands are SIDs.</summary>
    static int SidOperandCount(int op)
    {
        switch (op)
        {
            case 0:                     // version
            case 1:                     // Notice
            case 2:                     // FullName
            case 3:                     // FamilyName
            case 4:                     // Weight
            case (OpEscape << 8) | 0:   // Copyright
            case (OpEscape << 8) | 21:  // PostScript
            case (OpEscape << 8) | 22:  // BaseFontName
            case (OpEscape << 8) | 38:  // FontName (Font DICT)
                return 1;
            case OpRos:                 // ROS: registry and ordering, then a plain integer
                return 2;
            default:
                return 0;
        }
    }

    /// <summary>Whether the Encoding carries a supplement, which names glyphs by SID.</summary>
    /// <param name="font">The parsed program.</param>
    /// <returns>True when a supplement is present.</returns>
    bool HasEncodingSupplement(CffFont font)
    {
        return font.EncodingOffset > 1 && (_data[font.EncodingOffset] & 0x80) != 0;
    }

    /// <summary>The number of standard strings; SIDs below this do not index the String INDEX.</summary>
    const int StandardStringCount = 391;

    /// <summary>
    /// The Private DICT and its local Subrs INDEX as one verbatim span, or null when the
    /// subroutines do not follow the DICT (the Subrs operand is relative to the DICT, so
    /// the span must keep that distance).
    /// </summary>
    byte[] PrivateSpan(PrivateDict privateDict)
    {
        int end = privateDict.Offset + privateDict.Size;
        if (privateDict.SubrsRelativeOffset != null)
        {
            int subrsStart = privateDict.Offset + privateDict.SubrsRelativeOffset.Value;
            if (subrsStart < end)
                return null;
            CffIndex subrs = ReadIndex(subrsStart);
            end = Math.Max(end, subrs.End);
        }
        return Slice(privateDict.Offset, end);
    }

    byte[] BuildTopDict(CffFont font, int charsetOffset, int encodingOffset, int charStringsOffset, int privateSize, int privateOffset, int fdArrayOffset, int fdSelectOffset = 0)
    {
        MemoryStream dict = new MemoryStream();
        foreach (DictEntry entry in font.TopDict.Entries)
        {
            switch (entry.Operator)
            {
                case OpCharset:
                    WriteInt5(dict, charsetOffset);
                    break;
                case OpEncoding:
                    WriteInt5(dict, encodingOffset);
                    break;
                case OpCharStrings:
                    WriteInt5(dict, charStringsOffset);
                    break;
                case OpPrivate:
                    WriteInt5(dict, privateSize);
                    WriteInt5(dict, privateOffset);
                    break;
                case OpFdArray:
                    WriteInt5(dict, fdArrayOffset);
                    break;
                case OpFdSelect:
                    WriteInt5(dict, fdSelectOffset);
                    break;
                default:
                    dict.Write(_data, entry.OperandsStart, entry.OperandsEnd - entry.OperandsStart);
                    break;
            }
            WriteOperator(dict, entry.Operator);
        }
        return dict.ToArray();
    }

    byte[] BuildFontDict(FontDict fd, int privateSize, int privateOffset)
    {
        MemoryStream dict = new MemoryStream();
        foreach (DictEntry entry in fd.Dict.Entries)
        {
            if (entry.Operator == OpPrivate)
            {
                WriteInt5(dict, privateSize);
                WriteInt5(dict, privateOffset);
            }
            else
            {
                dict.Write(_data, entry.OperandsStart, entry.OperandsEnd - entry.OperandsStart);
            }
            WriteOperator(dict, entry.Operator);
        }
        return dict.ToArray();
    }

    static void WriteOperator(Stream stream, int op)
    {
        if (op >= 0x100)
        {
            stream.WriteByte(OpEscape);
            stream.WriteByte((byte)(op & 0xff));
        }
        else
        {
            stream.WriteByte((byte)op);
        }
    }

    /// <summary>The five-byte integer operand form: 29 followed by a big-endian int32.</summary>
    static void WriteInt5(Stream stream, int value)
    {
        stream.WriteByte(29);
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    /// <summary>Builds an INDEX structure from its items.</summary>
    internal static byte[] BuildIndex(List<byte[]> items)
    {
        MemoryStream index = new MemoryStream();
        int count = items.Count;
        index.WriteByte((byte)(count >> 8));
        index.WriteByte((byte)count);
        if (count == 0)
            return index.ToArray();

        long dataLength = 0;
        foreach (byte[] item in items)
            dataLength += item.Length;
        int offSize = OffSizeFor(dataLength + 1);
        index.WriteByte((byte)offSize);

        long offset = 1;
        WriteOffset(index, offset, offSize);
        foreach (byte[] item in items)
        {
            offset += item.Length;
            WriteOffset(index, offset, offSize);
        }
        foreach (byte[] item in items)
            index.Write(item, 0, item.Length);
        return index.ToArray();
    }

    /// <summary>The size an INDEX of items with these lengths will have.</summary>
    static int IndexSize(int[] itemLengths)
    {
        int count = itemLengths.Length;
        if (count == 0)
            return 2;
        long dataLength = 0;
        foreach (int length in itemLengths)
            dataLength += length;
        int offSize = OffSizeFor(dataLength + 1);
        return (int)(2 + 1 + (count + 1) * offSize + dataLength);
    }

    static int OffSizeFor(long lastOffset)
    {
        if (lastOffset <= 0xff) return 1;
        if (lastOffset <= 0xffff) return 2;
        if (lastOffset <= 0xffffff) return 3;
        return 4;
    }

    static void WriteOffset(Stream stream, long value, int offSize)
    {
        for (int shift = (offSize - 1) * 8; shift >= 0; shift -= 8)
            stream.WriteByte((byte)(value >> shift));
    }

    // ----- reading -----

    /// <summary>Reads an INDEX structure at the specified position.</summary>
    internal CffIndex ReadIndex(int pos)
    {
        CffIndex index = new CffIndex { Start = pos };
        int count = ReadUInt16(pos);
        index.Count = count;
        if (count == 0)
        {
            index.End = pos + 2;
            index.Offsets = new int[] { 1 };
            index.DataBase = pos + 2 - 1;
            return index;
        }
        int offSize = _data[pos + 2];
        if (offSize < 1 || offSize > 4)
            throw new InvalidDataException("A CFF INDEX has an offset size outside 1 to 4.");
        index.Offsets = new int[count + 1];
        int p = pos + 3;
        for (int idx = 0; idx <= count; idx++)
        {
            int value = 0;
            for (int b = 0; b < offSize; b++)
                value = (value << 8) | _data[p++];
            index.Offsets[idx] = value;
        }
        // Offsets are 1-based from the byte before the data.
        index.DataBase = p - 1;
        index.End = index.DataBase + index.Offsets[count];
        if (index.End > _data.Length)
            throw new InvalidDataException("A CFF INDEX runs past the end of the program.");
        return index;
    }

    /// <summary>Reads a DICT structure occupying [start, end).</summary>
    internal CffDict ReadDict(int start, int end)
    {
        CffDict dict = new CffDict();
        List<double> operands = new List<double>();
        int operandsStart = start;
        int pos = start;
        while (pos < end)
        {
            int b0 = _data[pos];
            if (b0 <= 21)
            {
                int op = b0;
                int opStart = pos;
                pos++;
                if (b0 == OpEscape)
                {
                    op = (OpEscape << 8) | _data[pos];
                    pos++;
                }
                dict.Entries.Add(new DictEntry
                {
                    Operator = op,
                    Operands = operands.ToArray(),
                    OperandsStart = operandsStart,
                    OperandsEnd = opStart,
                });
                operands.Clear();
                operandsStart = pos;
            }
            else if (b0 == 28)
            {
                operands.Add((short)((_data[pos + 1] << 8) | _data[pos + 2]));
                pos += 3;
            }
            else if (b0 == 29)
            {
                operands.Add((_data[pos + 1] << 24) | (_data[pos + 2] << 16) | (_data[pos + 3] << 8) | _data[pos + 4]);
                pos += 5;
            }
            else if (b0 == 30)
            {
                // A real number: nibbles until one is 0xf. Its value is never an offset,
                // so it is kept only as bytes; the parsed value is not needed.
                pos++;
                while (pos < end)
                {
                    int b = _data[pos++];
                    if ((b >> 4) == 0xf || (b & 0xf) == 0xf)
                        break;
                }
                operands.Add(double.NaN);
            }
            else if (b0 >= 32 && b0 <= 246)
            {
                operands.Add(b0 - 139);
                pos++;
            }
            else if (b0 >= 247 && b0 <= 250)
            {
                operands.Add((b0 - 247) * 256 + _data[pos + 1] + 108);
                pos += 2;
            }
            else if (b0 >= 251 && b0 <= 254)
            {
                operands.Add(-(b0 - 251) * 256 - _data[pos + 1] - 108);
                pos += 2;
            }
            else
            {
                throw new InvalidDataException("A CFF DICT contains a reserved byte value.");
            }
        }
        return dict;
    }

    PrivateDict ReadPrivate(int size, int offset)
    {
        if (offset < 0 || size < 0 || offset + size > _data.Length)
            throw new InvalidDataException("A CFF Private DICT lies outside the program.");
        PrivateDict privateDict = new PrivateDict { Offset = offset, Size = size };
        CffDict dict = ReadDict(offset, offset + size);
        DictEntry subrs = dict.Find(OpSubrs);
        if (subrs != null)
        {
            privateDict.SubrsRelativeOffset = (int)subrs.Operands[0];
            privateDict.Subrs = ReadIndex(offset + privateDict.SubrsRelativeOffset.Value);
            privateDict.LocalSubrCount = privateDict.Subrs.Count;
        }
        return privateDict;
    }

    int CharsetLength(int offset, int glyphCount)
    {
        int format = _data[offset];
        int pos = offset + 1;
        int covered = 1; // .notdef is implicit
        switch (format)
        {
            case 0:
                return 1 + 2 * (glyphCount - 1);
            case 1:
                while (covered < glyphCount)
                {
                    covered += _data[pos + 2] + 1;
                    pos += 3;
                }
                return pos - offset;
            case 2:
                while (covered < glyphCount)
                {
                    covered += ReadUInt16(pos + 2) + 1;
                    pos += 4;
                }
                return pos - offset;
            default:
                throw new InvalidDataException("A CFF charset has an unknown format.");
        }
    }

    int EncodingLength(int offset)
    {
        int format = _data[offset];
        int length;
        switch (format & 0x7f)
        {
            case 0:
                length = 2 + _data[offset + 1];
                break;
            case 1:
                length = 2 + 2 * _data[offset + 1];
                break;
            default:
                throw new InvalidDataException("A CFF Encoding has an unknown format.");
        }
        if ((format & 0x80) != 0)
            length += 1 + 3 * _data[offset + length];
        return length;
    }

    int FdSelectLength(int offset, int glyphCount)
    {
        int format = _data[offset];
        switch (format)
        {
            case 0:
                return 1 + glyphCount;
            case 3:
                return 1 + 2 + 3 * ReadUInt16(offset + 1) + 2;
            default:
                throw new InvalidDataException("A CFF FDSelect has an unknown format.");
        }
    }

    int ReadUInt16(int pos)
    {
        return (_data[pos] << 8) | _data[pos + 1];
    }

    byte[] Slice(int start, int end)
    {
        if (start < 0 || end > _data.Length || end < start)
            throw new InvalidDataException("A CFF structure lies outside the program.");
        byte[] bytes = new byte[end - start];
        Buffer.BlockCopy(_data, start, bytes, 0, bytes.Length);
        return bytes;
    }
}

/// <summary>The structure of a CFF program as <see cref="CffSubsetter.Parse"/> read it.</summary>
internal sealed class CffFont
{
    /// <summary>The header size (byte 2 of the program; 4 in every CFF 1 program seen).</summary>
    public int HeaderSize;
    /// <summary>The Name INDEX.</summary>
    public CffIndex NameIndex;
    /// <summary>The Top DICT INDEX (one item).</summary>
    public CffIndex TopDictIndex;
    /// <summary>The String INDEX.</summary>
    public CffIndex StringIndex;
    /// <summary>The Global Subr INDEX.</summary>
    public CffIndex GlobalSubrIndex;
    /// <summary>The CharStrings INDEX.</summary>
    public CffIndex CharStringsIndex;
    /// <summary>The parsed Top DICT.</summary>
    public CffDict TopDict;
    /// <summary>True when the Top DICT carries a ROS operator.</summary>
    public bool IsCidKeyed;
    /// <summary>The number of glyphs (charstrings).</summary>
    public int GlyphCount;
    /// <summary>The charset offset; 0, 1 and 2 name the predefined charsets.</summary>
    public int CharsetOffset;
    /// <summary>The Encoding offset; 0 and 1 name the predefined encodings.</summary>
    public int EncodingOffset;
    /// <summary>The CharStrings INDEX offset.</summary>
    public int CharStringsOffset;
    /// <summary>The FDArray offset (CID-keyed fonts).</summary>
    public int FdArrayOffset;
    /// <summary>The FDSelect offset (CID-keyed fonts).</summary>
    public int FdSelectOffset;
    /// <summary>The FDArray INDEX (CID-keyed fonts).</summary>
    public CffIndex FdArrayIndex;
    /// <summary>The Private DICT (fonts that are not CID-keyed), or null.</summary>
    public PrivateDict Private;
    /// <summary>The Font DICTs (CID-keyed fonts), each with its Private DICT.</summary>
    public List<FontDict> FontDicts = new List<FontDict>();

    /// <summary>The number of global subroutines.</summary>
    public int GlobalSubrCount
    {
        get { return GlobalSubrIndex.Count; }
    }

    /// <summary>The number of local subroutines of the (non-CID) Private DICT.</summary>
    public int LocalSubrCount
    {
        get { return Private != null ? Private.LocalSubrCount : 0; }
    }
}

/// <summary>An INDEX structure: where it is and where its items are.</summary>
internal sealed class CffIndex
{
    /// <summary>The position of the INDEX's count field.</summary>
    public int Start;
    /// <summary>The position just past the INDEX.</summary>
    public int End;
    /// <summary>The number of items.</summary>
    public int Count;
    /// <summary>The 1-based item offsets, Count + 1 of them.</summary>
    public int[] Offsets;
    /// <summary>The position offsets are relative to (one byte before the data).</summary>
    public int DataBase;

    /// <summary>The position of an item's first byte.</summary>
    public int ItemStart(int idx)
    {
        return DataBase + Offsets[idx];
    }

    /// <summary>The position just past an item.</summary>
    public int ItemEnd(int idx)
    {
        return DataBase + Offsets[idx + 1];
    }
}

/// <summary>A parsed DICT: its entries in order.</summary>
internal sealed class CffDict
{
    /// <summary>The entries, in the order they appear.</summary>
    public List<DictEntry> Entries = new List<DictEntry>();

    /// <summary>Finds the entry with the specified operator, or null.</summary>
    public DictEntry Find(int op)
    {
        foreach (DictEntry entry in Entries)
        {
            if (entry.Operator == op)
                return entry;
        }
        return null;
    }
}

/// <summary>One DICT entry: its operands and operator, and where the operand bytes are.</summary>
internal sealed class DictEntry
{
    /// <summary>The operator; two-byte operators are (12 &lt;&lt; 8) | second byte.</summary>
    public int Operator;
    /// <summary>The operand values (NaN for a real number, whose value is never needed).</summary>
    public double[] Operands;
    /// <summary>The position of the first operand byte.</summary>
    public int OperandsStart;
    /// <summary>The position of the operator byte (just past the operands).</summary>
    public int OperandsEnd;
}

/// <summary>A Private DICT and what its local subroutines are.</summary>
internal sealed class PrivateDict
{
    /// <summary>The DICT's absolute offset.</summary>
    public int Offset;
    /// <summary>The DICT's size in bytes.</summary>
    public int Size;
    /// <summary>The Subrs operand - relative to <see cref="Offset"/> - or null when there are none.</summary>
    public int? SubrsRelativeOffset;
    /// <summary>The number of local subroutines.</summary>
    public int LocalSubrCount;
    /// <summary>The local Subrs INDEX, or null when there are none.</summary>
    public CffIndex Subrs;
}

/// <summary>A Font DICT of a CID-keyed font with its Private DICT.</summary>
internal sealed class FontDict
{
    /// <summary>The parsed Font DICT.</summary>
    public CffDict Dict;
    /// <summary>Its Private DICT, or null.</summary>
    public PrivateDict Private;
}
