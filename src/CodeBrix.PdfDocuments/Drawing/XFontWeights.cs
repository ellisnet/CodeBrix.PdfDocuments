#region PDFsharp - A .NET library for processing PDF
//
// Authors:
//   Stefan Lange
//
// Copyright (c) 2005-2016 empira Software GmbH, Cologne Area (Germany)
//
// http://www.PdfSharp.com
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

namespace CodeBrix.PdfDocuments.Drawing; //Was previously: namespace PdfSharpCore.Drawing;

// PROVENANCE: this file arrived from the upstream fork carrying two things - the internal
// FontWeightValues enum below, and a large XFontWeights class of predefined weight values.
// Upstream marked the whole file "Not used in PDFsharp 1.x": the weight machinery was written
// for a 2.0 API that never shipped.
//
// The XFontWeights class, along with the companion XFontWeight and XFontStretch types, sat
// behind "#if true_ // PDFSHARP20" and "#if PDFSHARP20" guards. Because PDFSHARP20 is never
// defined in this code base, none of it ever compiled. Those guarded blocks were removed on
// 2026-08-01, which deleted XFontWeight.cs and XFontStretch.cs entirely and trimmed this file.
//
// FontWeightValues is retained but commented out. Unlike the rest, it was NOT behind a guard -
// it compiled into the assembly as an unused internal type. It is kept here because it is the
// canonical OpenType usWeightClass table (100..950), which is the numbering any future weight
// support would need. Uncomment it if that work is ever picked up.
//
// enum FontWeightValues
// {
//     Thin = 100,
//     ExtraLight = 200,
//     Light = 300,
//     Normal = 400,
//     Medium = 500,
//     SemiBold = 600,
//     Bold = 700,
//     ExtraBold = 800,
//     Black = 900,
//     ExtraBlack = 950,
// }
