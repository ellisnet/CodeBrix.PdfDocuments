using System;
using System.Collections.Concurrent;
using System.IO;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

/// <summary>
/// Answers "does this font file contain a glyph for this code point?" by reading the
/// font's own character-to-glyph table (cmap), so coverage decisions are made against
/// the actual font in use rather than assumed Unicode ranges. Handles the BMP-only
/// format 4 table and the full-Unicode format 12 table (plus the rarer formats 6 and
/// 0), for both .ttf and .otf files.
/// </summary>
internal sealed class FontGlyphCoverage
{
    private static readonly ConcurrentDictionary<string, FontGlyphCoverage> Cache =
        new ConcurrentDictionary<string, FontGlyphCoverage>(StringComparer.Ordinal);

    private static readonly FontGlyphCoverage Empty = new FontGlyphCoverage(0, Array.Empty<byte>());

    private readonly int _format;
    private readonly byte[] _subtable;

    private FontGlyphCoverage(int format, byte[] subtable)
    {
        _format = format;
        _subtable = subtable;
    }

    /// <summary>
    /// Loads (and caches) the coverage for a font file. A file that cannot be parsed
    /// yields an empty coverage that reports every code point as uncovered.
    /// </summary>
    public static FontGlyphCoverage Load(string filePath)
        => Cache.GetOrAdd(filePath, static path =>
        {
            try
            {
                return Parse(File.ReadAllBytes(path));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return Empty;
            }
        });

    /// <summary>True when the font maps the code point to a real (non-missing) glyph.</summary>
    public bool Covers(int codePoint)
    {
        if (codePoint < 0 || codePoint > 0x10FFFF) { return false; }

        var span = new ReadOnlySpan<byte>(_subtable);
        return _format switch
        {
            4 => CoversFormat4(span, codePoint),
            6 => CoversFormat6(span, codePoint),
            12 => CoversFormat12(span, codePoint),
            0 => CoversFormat0(span, codePoint),
            _ => false,
        };
    }

    private static FontGlyphCoverage Parse(byte[] fileData)
    {
        if (!SfntReader.TryFindTable(fileData, "cmap", out var cmapOffset, out var cmapLength))
        {
            return Empty;
        }

        var span = new ReadOnlySpan<byte>(fileData);
        var numTables = SfntReader.ReadUInt16(span, cmapOffset + 2);

        var bestScore = -1;
        var bestOffset = -1;
        for (var i = 0; i < numTables; i++)
        {
            var recordOffset = cmapOffset + 4 + i * 8;
            if (recordOffset + 8 > fileData.Length) { break; }

            var platformId = SfntReader.ReadUInt16(span, recordOffset);
            var encodingId = SfntReader.ReadUInt16(span, recordOffset + 2);
            var subtableOffset = (int)SfntReader.ReadUInt32(span, recordOffset + 4);
            var score = (platformId, encodingId) switch
            {
                (3, 10) => 100, // Windows, full Unicode (format 12)
                (0, 6) => 95,   // Unicode, full repertoire
                (0, 4) => 94,
                (3, 1) => 80,   // Windows, BMP (format 4)
                (0, 3) => 75,
                (0, 2) => 60,
                (0, 1) => 59,
                (0, 0) => 58,
                _ => 0,
            };

            if (score > bestScore && cmapOffset + subtableOffset + 4 <= fileData.Length)
            {
                bestScore = score;
                bestOffset = cmapOffset + subtableOffset;
            }
        }

        if (bestOffset < 0) { return Empty; }

        var format = (int)SfntReader.ReadUInt16(span, bestOffset);
        int subtableLength = format switch
        {
            0 or 4 or 6 => SfntReader.ReadUInt16(span, bestOffset + 2),
            12 => bestOffset + 12 <= fileData.Length ? (int)SfntReader.ReadUInt32(span, bestOffset + 4) : 0,
            _ => 0,
        };

        if (subtableLength <= 0 || bestOffset + subtableLength > fileData.Length)
        {
            // Tolerate a slightly over-declared length by clamping to the file end.
            subtableLength = fileData.Length - bestOffset;
        }

        if (format is not (0 or 4 or 6 or 12) || subtableLength < 8) { return Empty; }

        var subtable = new byte[subtableLength];
        Array.Copy(fileData, bestOffset, subtable, 0, subtableLength);
        return new FontGlyphCoverage(format, subtable);
    }

    private static bool CoversFormat4(ReadOnlySpan<byte> table, int codePoint)
    {
        if (codePoint > 0xFFFF || table.Length < 14) { return false; }

        var segCount = SfntReader.ReadUInt16(table, 6) / 2;
        var endCodesOffset = 14;
        var startCodesOffset = endCodesOffset + segCount * 2 + 2; // +2 reservedPad
        var idDeltaOffset = startCodesOffset + segCount * 2;
        var idRangeOffsetOffset = idDeltaOffset + segCount * 2;
        if (idRangeOffsetOffset + segCount * 2 > table.Length) { return false; }

        // Binary search the first segment whose endCode >= codePoint.
        var low = 0;
        var high = segCount - 1;
        while (low < high)
        {
            var mid = (low + high) / 2;
            if (SfntReader.ReadUInt16(table, endCodesOffset + mid * 2) < codePoint) { low = mid + 1; }
            else { high = mid; }
        }

        var startCode = SfntReader.ReadUInt16(table, startCodesOffset + low * 2);
        var endCode = SfntReader.ReadUInt16(table, endCodesOffset + low * 2);
        if (codePoint < startCode || codePoint > endCode) { return false; }

        var idRangeOffset = SfntReader.ReadUInt16(table, idRangeOffsetOffset + low * 2);
        if (idRangeOffset == 0)
        {
            var idDelta = SfntReader.ReadInt16(table, idDeltaOffset + low * 2);
            return ((codePoint + idDelta) & 0xFFFF) != 0;
        }

        var glyphAddress = idRangeOffsetOffset + low * 2 + idRangeOffset + (codePoint - startCode) * 2;
        if (glyphAddress + 2 > table.Length) { return false; }
        var glyph = SfntReader.ReadUInt16(table, glyphAddress);
        if (glyph == 0) { return false; }

        var delta = SfntReader.ReadInt16(table, idDeltaOffset + low * 2);
        return ((glyph + delta) & 0xFFFF) != 0;
    }

    private static bool CoversFormat6(ReadOnlySpan<byte> table, int codePoint)
    {
        if (codePoint > 0xFFFF || table.Length < 10) { return false; }

        int firstCode = SfntReader.ReadUInt16(table, 6);
        int entryCount = SfntReader.ReadUInt16(table, 8);
        var index = codePoint - firstCode;
        if (index < 0 || index >= entryCount || 10 + index * 2 + 2 > table.Length) { return false; }
        return SfntReader.ReadUInt16(table, 10 + index * 2) != 0;
    }

    private static bool CoversFormat12(ReadOnlySpan<byte> table, int codePoint)
    {
        if (table.Length < 16) { return false; }

        var numGroups = (int)SfntReader.ReadUInt32(table, 12);
        var low = 0;
        var high = numGroups - 1;
        while (low <= high)
        {
            var mid = (low + high) / 2;
            var groupOffset = 16 + mid * 12;
            if (groupOffset + 12 > table.Length) { return false; }

            var start = SfntReader.ReadUInt32(table, groupOffset);
            var end = SfntReader.ReadUInt32(table, groupOffset + 4);
            if (codePoint < start) { high = mid - 1; }
            else if (codePoint > end) { low = mid + 1; }
            else
            {
                var startGlyph = SfntReader.ReadUInt32(table, groupOffset + 8);
                return startGlyph + (uint)(codePoint - start) != 0;
            }
        }

        return false;
    }

    private static bool CoversFormat0(ReadOnlySpan<byte> table, int codePoint)
        => codePoint <= 0xFF && 6 + codePoint < table.Length && table[6 + codePoint] != 0;
}
