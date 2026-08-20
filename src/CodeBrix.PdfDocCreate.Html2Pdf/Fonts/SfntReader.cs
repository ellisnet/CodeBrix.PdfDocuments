using System;
using System.Buffers.Binary;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

/// <summary>
/// Minimal big-endian reader over the sfnt container shared by .ttf and .otf files
/// (and the first font of a .ttc collection): locates tables by tag. Used for glyph
/// coverage (cmap) and face metadata (name, OS/2) - never for rendering.
/// </summary>
internal static class SfntReader
{
    private const uint TtcTag = 0x74746366;        // 'ttcf'
    private const uint TrueTypeVersion = 0x00010000;
    private const uint OpenTypeCffTag = 0x4F54544F; // 'OTTO'
    private const uint AppleTrueTag = 0x74727565;   // 'true'

    public static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset)
        => BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));

    public static short ReadInt16(ReadOnlySpan<byte> data, int offset)
        => BinaryPrimitives.ReadInt16BigEndian(data.Slice(offset, 2));

    public static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
        => BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));

    /// <summary>
    /// Finds a top-level sfnt table. Returns false when the file is not an sfnt font
    /// or has no such table; never throws on malformed data shorter than expected.
    /// </summary>
    public static bool TryFindTable(byte[] fileData, string tag, out int tableOffset, out int tableLength)
    {
        tableOffset = 0;
        tableLength = 0;
        if (fileData == null || fileData.Length < 12) { return false; }

        var span = new ReadOnlySpan<byte>(fileData);
        var sfntStart = 0;
        var version = ReadUInt32(span, 0);
        if (version == TtcTag)
        {
            // Font collection: use the first font's offset table.
            if (fileData.Length < 16) { return false; }
            sfntStart = (int)ReadUInt32(span, 12);
            if (sfntStart < 0 || sfntStart + 12 > fileData.Length) { return false; }
            version = ReadUInt32(span, sfntStart);
        }

        if (version != TrueTypeVersion && version != OpenTypeCffTag && version != AppleTrueTag)
        {
            return false;
        }

        var numTables = ReadUInt16(span, sfntStart + 4);
        var recordOffset = sfntStart + 12;
        var tagValue = (uint)((tag[0] << 24) | (tag[1] << 16) | (tag[2] << 8) | tag[3]);
        for (var i = 0; i < numTables; i++, recordOffset += 16)
        {
            if (recordOffset + 16 > fileData.Length) { return false; }
            if (ReadUInt32(span, recordOffset) != tagValue) { continue; }

            var offset = (int)ReadUInt32(span, recordOffset + 8);
            var length = (int)ReadUInt32(span, recordOffset + 12);
            if (offset < 0 || length <= 0 || offset + length > fileData.Length) { return false; }

            tableOffset = offset;
            tableLength = length;
            return true;
        }

        return false;
    }
}
