#region PDFsharp - A .NET library for processing PDF
//
// Authors:
//   Stefan Lange
//
// Copyright (c) 2005-2016 empira Software GmbH, Cologne Area (Germany)
//
// http://www.PdfSharpCore.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Diagnostics;
using System.Collections.Generic;
using CodeBrix.PdfDocuments.Fonts.OpenType;
using CodeBrix.PdfDocuments.Pdf.Internal;

namespace CodeBrix.PdfDocuments.Fonts; //Was previously: namespace PdfSharpCore.Fonts;

/// <summary>
/// Helper class that determines the characters used in a particular font.
/// </summary>
internal class CMapInfo
{
    public CMapInfo(OpenTypeDescriptor descriptor)
    {
        Debug.Assert(descriptor != null);
        _descriptor = descriptor;
    }
    internal OpenTypeDescriptor _descriptor;

    /// <summary>
    /// Adds the characters of the specified string to the hashtable. A surrogate pair
    /// is added as its single supplementary-plane code point, so its glyph resolves
    /// through the font's format 12 cmap subtable.
    /// </summary>
    public void AddChars(string text)
    {
        if (text != null)
        {
            bool symbol = _descriptor.FontFace.cmap.symbol;
            int length = text.Length;
            for (int idx = 0; idx < length; idx++)
            {
                char ch = text[idx];
                int codePoint = ch;
                if (char.IsHighSurrogate(ch) && idx + 1 < length && char.IsLowSurrogate(text[idx + 1]))
                {
                    codePoint = char.ConvertToUtf32(ch, text[idx + 1]);
                    idx++;
                }

                if (!CharacterToGlyphIndex.ContainsKey(codePoint))
                {
                    int mapped = codePoint;
                    if (symbol && codePoint <= 0xFFFF)
                    {
                        // Remap ch for symbol fonts.
                        mapped = (char)(codePoint | (_descriptor.FontFace.os2.usFirstCharIndex & 0xFF00));  // @@@ refactor
                    }
                    int glyphIndex = _descriptor.CharCodeToGlyphIndex(mapped);
                    CharacterToGlyphIndex.Add(codePoint, glyphIndex);
                    GlyphIndices[glyphIndex] = null;
                    if (codePoint <= 0xFFFF)
                    {
                        MinChar = (char)Math.Min(MinChar, (char)codePoint);
                        MaxChar = (char)Math.Max(MaxChar, (char)codePoint);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds the glyphIndices to the hashtable.
    /// </summary>
    public void AddGlyphIndices(string glyphIndices)
    {
        if (glyphIndices != null)
        {
            int length = glyphIndices.Length;
            for (int idx = 0; idx < length; idx++)
            {
                int glyphIndex = glyphIndices[idx];
                GlyphIndices[glyphIndex] = null;
            }
        }
    }

    /// <summary>
    /// Adds a ANSI characters.
    /// </summary>
    internal void AddAnsiChars()
    {
        byte[] ansi = new byte[256 - 32];
        for (int idx = 0; idx < 256 - 32; idx++)
            ansi[idx] = (byte)(idx + 32);
        string text = PdfEncoders.WinAnsiEncoding.GetString(ansi, 0, ansi.Length);
        AddChars(text);
    }

    internal bool Contains(char ch)
    {
        return CharacterToGlyphIndex.ContainsKey(ch);
    }

    public int[] Chars
    {
        get
        {
            int[] chars = new int[CharacterToGlyphIndex.Count];
            CharacterToGlyphIndex.Keys.CopyTo(chars, 0);
            Array.Sort(chars);
            return chars;
        }
    }

    public int[] GetGlyphIndices()
    {
        int[] indices = new int[GlyphIndices.Count];
        GlyphIndices.Keys.CopyTo(indices, 0);
        Array.Sort(indices);
        return indices;
    }

    public char MinChar = char.MaxValue;
    public char MaxChar = char.MinValue;

    /// <summary>
    /// Maps used Unicode code points (supplementary-plane values included) to their
    /// glyph indices.
    /// </summary>
    public Dictionary<int, int> CharacterToGlyphIndex = new Dictionary<int, int>();
    public Dictionary<int, object> GlyphIndices = new Dictionary<int, object>();
}