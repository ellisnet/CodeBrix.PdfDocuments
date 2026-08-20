using System;
using System.IO;
using System.Text;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

/// <summary>
/// Face metadata read from a font file's own name and OS/2 tables, so loose .ttf/.otf
/// files can register without a manifest.
/// </summary>
/// <param name="FamilyName">Typographic family name (name ID 16, falling back to ID 1).</param>
/// <param name="LegacyFamilyName">Legacy family name (name ID 1) - the name the PDF
/// layer's font metrics re-resolution asks for, so it must resolve too.</param>
/// <param name="Weight">usWeightClass clamped to 100-900.</param>
/// <param name="IsItalic">fsSelection italic bit.</param>
/// <param name="Stretch">"Normal", "Condensed" or "Expanded" from usWidthClass.</param>
internal sealed record FontFileInfo(string FamilyName, string LegacyFamilyName, int Weight, bool IsItalic, string Stretch)
{
    /// <summary>
    /// Reads the face metadata from a font file. Returns null when the file is not a
    /// parseable sfnt font or carries no usable family name.
    /// </summary>
    public static FontFileInfo Read(string filePath)
    {
        byte[] fileData;
        try
        {
            fileData = File.ReadAllBytes(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        var familyName = ReadFamilyName(fileData, preferTypographic: true);
        if (string.IsNullOrWhiteSpace(familyName)) { return null; }

        var legacyFamilyName = ReadFamilyName(fileData, preferTypographic: false) ?? familyName;

        var weight = 400;
        var italic = false;
        var stretch = "Normal";
        if (SfntReader.TryFindTable(fileData, "OS/2", out var os2Offset, out var os2Length) && os2Length >= 64)
        {
            var span = new ReadOnlySpan<byte>(fileData);
            weight = Math.Clamp((int)SfntReader.ReadUInt16(span, os2Offset + 4), 100, 900);
            var widthClass = SfntReader.ReadUInt16(span, os2Offset + 6);
            stretch = widthClass switch
            {
                < 5 => "Condensed",
                5 => "Normal",
                _ => "Expanded",
            };
            var fsSelection = SfntReader.ReadUInt16(span, os2Offset + 62);
            italic = (fsSelection & 0x0001) != 0;
        }

        return new FontFileInfo(familyName.Trim(), legacyFamilyName.Trim(), weight, italic, stretch);
    }

    private static string ReadFamilyName(byte[] fileData, bool preferTypographic)
    {
        if (!SfntReader.TryFindTable(fileData, "name", out var nameOffset, out var nameLength)
            || nameLength < 6)
        {
            return null;
        }

        var span = new ReadOnlySpan<byte>(fileData);
        var count = SfntReader.ReadUInt16(span, nameOffset + 2);
        var stringStorage = nameOffset + SfntReader.ReadUInt16(span, nameOffset + 4);

        string bestName = null;
        var bestScore = -1;
        for (var i = 0; i < count; i++)
        {
            var recordOffset = nameOffset + 6 + i * 12;
            if (recordOffset + 12 > fileData.Length) { break; }

            var platformId = SfntReader.ReadUInt16(span, recordOffset);
            var nameId = SfntReader.ReadUInt16(span, recordOffset + 6);
            if (nameId != 16 && nameId != 1) { continue; }

            var length = SfntReader.ReadUInt16(span, recordOffset + 8);
            var offset = stringStorage + SfntReader.ReadUInt16(span, recordOffset + 10);
            if (length == 0 || offset + length > fileData.Length) { continue; }

            string value;
            if (platformId is 3 or 0)
            {
                value = Encoding.BigEndianUnicode.GetString(fileData, offset, length);
            }
            else if (platformId == 1)
            {
                value = Encoding.ASCII.GetString(fileData, offset, length);
            }
            else
            {
                continue;
            }

            // Windows Unicode entries beat Macintosh ones; which of ID 16 (typographic
            // family) and ID 1 (legacy family) wins depends on the caller.
            var idScore = nameId == 16 == preferTypographic ? 10 : 0;
            var score = idScore + (platformId == 3 ? 2 : platformId == 0 ? 1 : 0);
            if (score > bestScore && !string.IsNullOrWhiteSpace(value))
            {
                bestScore = score;
                bestName = value;
            }
        }

        return bestName;
    }
}
