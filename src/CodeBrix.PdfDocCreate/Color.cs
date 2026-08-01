#region MigraDoc - Creating Documents on the Fly
//
// Authors:
//   Stefan Lange (mailto:Stefan.Lange@PdfSharpCore.com)
//   Klaus Potzesny (mailto:Klaus.Potzesny@PdfSharpCore.com)
//   David Stephensen (mailto:David.Stephensen@PdfSharpCore.com)
//
// Copyright (c) 2001-2009 empira Software GmbH, Cologne (Germany)
//
// http://www.PdfSharpCore.com
// http://www.migradoc.com
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

using CodeBrix.PdfDocCreate.DocumentObjectModel.Internals;
using CodeBrix.PdfDocCreate.DocumentObjectModel.Resources;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace CodeBrix.PdfDocCreate.DocumentObjectModel; //Was previously: namespace MigraDocCore.DocumentObjectModel;

/// <summary>
/// The Color class represents an ARGB color value.
/// </summary>
[DebuggerDisplay("(A={A}, R={R}, G={G}, B={B} C={C}, M={M}, Y={Y}, K={K})")]
public struct Color : INullableValue
{
    /// <summary>
    /// Initializes a new instance of the Color class.
    /// </summary>
    public Color(uint argb)
    {
        this.isCmyk = false;
        this.argb = argb;
        this.a = this.c = this.m = this.y = this.k = 0f; // Compiler enforces this line of code
        InitCmykFromRgb();
    }

    /// <summary>
    /// Initializes a new instance of the Color class.
    /// </summary>
    public Color(byte r, byte g, byte b)
    {
        this.isCmyk = false;
        this.argb = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
        this.a = this.c = this.m = this.y = this.k = 0f; // Compiler enforces this line of code
        InitCmykFromRgb();
    }

    /// <summary>
    /// Initializes a new instance of the Color class.
    /// </summary>
    public Color(byte a, byte r, byte g, byte b)
    {
        this.isCmyk = false;
        this.argb = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
        this.a = this.c = this.m = this.y = this.k = 0f; // Compiler enforces this line of code
        InitCmykFromRgb();
    }

    /// <summary>
    /// Initializes a new instance of the Color class with a CMYK color.
    /// All values must be in a range between 0 to 100 percent.
    /// </summary>
    public Color(double alpha, double cyan, double magenta, double yellow, double black)
    {
        this.isCmyk = true;
        this.a = (float)(alpha > 100 ? 100 : (alpha < 0 ? 0 : alpha));
        this.c = (float)(cyan > 100 ? 100 : (cyan < 0 ? 0 : cyan));
        this.m = (float)(magenta > 100 ? 100 : (magenta < 0 ? 0 : magenta));
        this.y = (float)(yellow > 100 ? 100 : (yellow < 0 ? 0 : yellow));
        this.k = (float)(black > 100 ? 100 : (black < 0 ? 0 : black));
        this.argb = 0; // Compiler enforces this line of code
        InitRgbFromCmyk();
    }

    /// <summary>
    /// Initializes a new instance of the Color class with a CMYK color.
    /// All values must be in a range between 0 to 100 percent.
    /// </summary>
    public Color(double cyan, double magenta, double yellow, double black)
        : this(100, cyan, magenta, yellow, black)
    { }

    void InitCmykFromRgb()
    {
        // Similar formula as in PDFsharp
        this.isCmyk = false;
        int c = 255 - (int)R;
        int m = 255 - (int)G;
        int y = 255 - (int)B;
        int k = Math.Min(c, Math.Min(m, y));
        if (k == 255)
            this.c = this.m = this.y = 0;
        else
        {
            float black = 255f - k;
            this.c = 100f * (c - k) / black;
            this.m = 100f * (m - k) / black;
            this.y = 100f * (y - k) / black;
        }
        this.k = 100f * k / 255f;
        this.a = A / 2.55f;
    }

    void InitRgbFromCmyk()
    {
        // Similar formula as in PDFsharp
        this.isCmyk = true;
        float black = this.k * 2.55f + 0.5f;
        float factor = (255f - black) / 100f;
        byte a = (byte)(this.a * 2.55 + 0.5);
        byte r = (byte)(255 - Math.Min(255f, this.c * factor + black));
        byte g = (byte)(255 - Math.Min(255f, this.m * factor + black));
        byte b = (byte)(255 - Math.Min(255f, this.y * factor + black));
        this.argb = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | (uint)b;
    }

    /// <summary>
    /// Gets a value indicating whether this instance is a CMYK color.
    /// </summary>
    public bool IsCmyk
    {
        get { return this.isCmyk; }
    }

    /// <summary>
    /// Determines whether this color is empty.
    /// </summary>
    public bool IsEmpty
    {
        get { return this == Color.Empty; }
    }

    /// <summary>
    /// Returns the value.
    /// </summary>
    object INullableValue.GetValue()
    {
        return this;
    }

    /// <summary>
    /// Sets the given value.
    /// </summary>
    void INullableValue.SetValue(object value)
    {
        if (value is uint)
            this.argb = (uint)value;
        else
            this = Color.Parse(value.ToString());
    }

    /// <summary>
    /// Resets this instance, i.e. IsNull() will return true afterwards.
    /// </summary>
    void INullableValue.SetNull()
    {
        this = Color.Empty;
    }

    /// <summary>
    /// Determines whether this instance is null (not set).
    /// </summary>
    bool INullableValue.IsNull
    {
        get { return this == Color.Empty; }
    }

    /// <summary>
    /// Determines whether this instance is null (not set).
    /// </summary>
    internal bool IsNull
    {
        get { return this == Color.Empty; }
    }

    /// <summary>
    /// Gets or sets the ARGB value.
    /// </summary>
    public uint Argb
    {
        get { return argb; }
        set
        {
            if (this.isCmyk)
                throw new InvalidOperationException("Cannot change a CMYK color.");
            argb = value;
            InitCmykFromRgb();
        }
    }

    /// <summary>
    /// Gets or sets the RGB value.
    /// </summary>
    public uint RGB
    {
        get { return argb; }
        set
        {
            if (this.isCmyk)
                throw new InvalidOperationException("Cannot change a CMYK color.");
            argb = value;
            InitCmykFromRgb();
        }
    }

    /// <summary>
    /// Calls base class Equals.
    /// </summary>
    public override bool Equals(Object obj)
    {
        if (obj is Color)
        {
            Color color = (Color)obj;
            if (this.isCmyk ^ color.isCmyk)
                return false;
            if (this.isCmyk)
                return this.a == color.a && this.c == color.c && this.m == color.m && this.y == color.y && this.k == color.k;
            else
                return this.argb == color.argb;
        }
        return false;
    }

    /// <summary>
    /// Gets the ARGB value that this Color instance represents.
    /// </summary>
    public override int GetHashCode()
    {
        return (int)this.argb ^ this.a.GetHashCode() ^ this.c.GetHashCode() ^ this.m.GetHashCode() ^ this.y.GetHashCode() ^ this.k.GetHashCode();
    }

    /// <summary>
    /// Compares two color objects. True if both argb values are equal, false otherwise.
    /// </summary>
    public static bool operator ==(Color color1, Color color2)
    {
        if (color1.isCmyk ^ color2.isCmyk)
            return false;
        if (color1.isCmyk)
            return color1.a == color2.a && color1.c == color2.c && color1.m == color2.m && color1.y == color2.y && color1.k == color2.k;
        else
            return color1.argb == color2.argb;
    }

    /// <summary>
    /// Compares two color objects. True if both argb values are not equal, false otherwise.
    /// </summary>
    public static bool operator !=(Color color1, Color color2)
    {
        return !(color1 == color2);
    }

    /// <summary>
    /// Parses the string and returns a color object.
    /// Throws ArgumentException if color is invalid.
    /// </summary>
    /// <param name="color">A color name, a CSS hex color, a <c>"0x"</c>-prefixed
    /// packed ARGB value, or a decimal integer.</param>
    /// <remarks>
    /// The two hex spellings deliberately follow different, well-established conventions, and
    /// the prefix indicates which one applies:
    /// <list type="bullet">
    /// <item><description><c>"#rrggbb"</c> and the CSS shorthand <c>"#rgb"</c> are CSS colors and
    /// are always FULLY OPAQUE. <c>"#c0c0c0"</c> is light grey.</description></item>
    /// <item><description><c>"0x..."</c> is a packed ARGB integer in the form <c>0xAARRGGBB</c>,
    /// so the alpha byte comes FIRST and is NOT defaulted. <c>"0xc0c0c0"</c> has an alpha of
    /// zero and is therefore fully TRANSPARENT - write <c>"0xFFC0C0C0"</c> for opaque light grey.
    /// This matches the behaviour of the upstream library.</description></item>
    /// </list>
    /// An eight-digit <c>"#"</c> value is rejected rather than guessed at: CSS writes
    /// <c>#rrggbbaa</c> with the alpha LAST, which is the opposite order from <c>0xAARRGGBB</c>,
    /// so silently accepting it would produce the wrong color.
    /// </remarks>
    public static Color Parse(string color)
    {
        if (color == null)
            throw new ArgumentNullException("color");
        if (color == "")
            throw new ArgumentException("color");

        if (color[0] == '#')
            return ParseCssHexColor(color);

        try
        {
            uint clr = 0;
            // Must use Enum.Parse because Enum.IsDefined is case sensitive
            try
            {
                object obj = Enum.Parse(typeof(ColorName), color, true);
                clr = (uint)obj;
                return new Color(clr);
            }
            catch
            {
                //ignore exception cause it's not a ColorName.
            }

            System.Globalization.NumberStyles numberStyle = System.Globalization.NumberStyles.Integer;
            string number = color.ToLower();
            if (number.StartsWith("0x"))
            {
                numberStyle = System.Globalization.NumberStyles.HexNumber;
                number = color.Substring(2);
            }
            clr = uint.Parse(number, numberStyle);
            return new Color(clr);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException(DomSR.InvalidColorString(color), ex);
        }
    }

    /// <summary>
    /// Parses a CSS hex color - "#rrggbb", or the "#rgb" shorthand - as a fully opaque color.
    /// </summary>
    /// <remarks>
    /// CSS hex colors have no alpha component in these two forms, so the result is always opaque.
    /// This is deliberately different from the "0x" form handled by <see cref="Parse"/>, which is
    /// a packed 0xAARRGGBB integer and does not default the alpha byte.
    /// </remarks>
    private static Color ParseCssHexColor(string color)
    {
        string digits = color.Substring(1);

        if (digits.Length == 8)
            throw new ArgumentException(DomSR.InvalidColorString(color)
                + " Eight-digit CSS colors (#rrggbbaa) are not supported, because CSS places the"
                + " alpha component last while the \"0x\" form places it first (0xAARRGGBB), so the"
                + " two cannot be told apart. Use \"0x\" with an eight-digit AARRGGBB value to"
                + " specify alpha.");

        if (digits.Length != 3 && digits.Length != 6)
            throw new ArgumentException(DomSR.InvalidColorString(color));

        // Validated explicitly rather than left to uint.Parse, because NumberStyles.HexNumber also
        // allows surrounding whitespace - which would let "#ab " through as a 3-digit shorthand.
        foreach (char digit in digits)
        {
            if (!((digit >= '0' && digit <= '9')
                  || (digit >= 'a' && digit <= 'f')
                  || (digit >= 'A' && digit <= 'F')))
                throw new ArgumentException(DomSR.InvalidColorString(color));
        }

        uint value = uint.Parse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        byte r, g, b;
        if (digits.Length == 3)
        {
            // CSS shorthand doubles each digit, so "#abc" means "#aabbcc".
            r = (byte)(((value & 0xF00) >> 8) * 0x11);
            g = (byte)(((value & 0x0F0) >> 4) * 0x11);
            b = (byte)((value & 0x00F) * 0x11);
        }
        else
        {
            r = (byte)((value & 0xFF0000) >> 16);
            g = (byte)((value & 0x00FF00) >> 8);
            b = (byte)(value & 0x0000FF);
        }

        return new Color(r, g, b);
    }

    /// <summary>
    /// Gets the alpha (transparency) part of the RGB Color.
    /// The values is in the range between 0 to 255.
    /// </summary>
    public uint A
    {
        get { return (this.argb & 0xFF000000) >> 24; }
    }

    /// <summary>
    /// Gets the red part of the Color.
    /// The values is in the range between 0 to 255.
    /// </summary>
    public uint R
    {
        get { return (this.argb & 0xFF0000) >> 16; }
    }

    /// <summary>
    /// Gets the green part of the Color.
    /// The values is in the range between 0 to 255.
    /// </summary>
    public uint G
    {
        get { return (this.argb & 0x00FF00) >> 8; }
    }

    /// <summary>
    /// Gets the blue part of the Color.
    /// The values is in the range between 0 to 255.
    /// </summary>
    public uint B
    {
        get { return this.argb & 0x0000FF; }
    }

    /// <summary>
    /// Gets the alpha (transparency) part of the CMYK Color.
    /// The values is in the range between 0 (transparent) to 100 (opaque) percent.
    /// </summary>
    public double Alpha
    {
        get { return this.a; }
    }

    /// <summary>
    /// Gets the cyan part of the Color.
    /// The values is in the range between 0 to 100 percent.
    /// </summary>
    public double C
    {
        get { return this.c; }
    }

    /// <summary>
    /// Gets the magenta part of the Color.
    /// The values is in the range between 0 to 100 percent.
    /// </summary>
    public double M
    {
        get { return this.m; }
    }

    /// <summary>
    /// Gets the yellow part of the Color.
    /// The values is in the range between 0 to 100 percent.
    /// </summary>
    public double Y
    {
        get { return this.y; }
    }

    /// <summary>
    /// Gets the key (black) part of the Color.
    /// The values is in the range between 0 to 100 percent.
    /// </summary>
    public double K
    {
        get { return this.k; }
    }

    /// <summary>
    /// Gets a non transparent color brightened in terms of transparency if any is given(A &lt; 255),
    /// otherwise this instance itself.
    /// </summary>
    public Color GetMixedTransparencyColor()
    {
        int alpha = (int)A;
        if (alpha == 0xFF)
            return this;

        int red = (int)R;
        int green = (int)G;
        int blue = (int)B;

        double whiteFactor = 1 - alpha / 255.0;

        red = (int)(red + (255 - red) * whiteFactor);
        green = (int)(green + (255 - green) * whiteFactor);
        blue = (int)(blue + (255 - blue) * whiteFactor);
        return new Color((uint)(0xFF << 24 | (red << 16) | (green << 8) | blue));
    }

    /// <summary>
    /// Writes the Color object in its hexadecimal value.
    /// </summary>
    public override string ToString()
    {
        if (stdColors == null)
        {
            Array colorNames = Enum.GetNames(typeof(ColorName));
            Array colorValues = Enum.GetValues(typeof(ColorName));
            int count = colorNames.GetLength(0);
            stdColors = new Hashtable(count);
            for (int index = 0; index < count; index++)
            {
                string c = (string)colorNames.GetValue(index);
                uint d = (uint)colorValues.GetValue(index);
                // Some color are double named...
                // Aqua == Cyan
                // Fuchsia == Magenta
                if (!stdColors.ContainsKey(d))
                    stdColors.Add(d, c);
            }
        }
        if (this.isCmyk)
        {
            string s;
            if (Alpha == 100.0)
                s = String.Format(CultureInfo.InvariantCulture, "CMYK({0:0.##},{1:0.##},{2:0.##},{3:0.##})", C, M, Y, K);
            else
                s = String.Format(CultureInfo.InvariantCulture, "CMYK({0:0.##},{1:0.##},{2:0.##},{3:0.##},{4:0.##})", Alpha, C, M, Y, K);
            return s;
        }
        else
        {
            if (stdColors.ContainsKey(argb))
                return (string)stdColors[argb];
            else
            {
                if ((this.argb & 0xFF000000) == 0xFF000000)
                    return "RGB(" +
                           ((this.argb & 0xFF0000) >> 16).ToString(CultureInfo.InvariantCulture) + "," +
                           ((this.argb & 0x00FF00) >> 8).ToString(CultureInfo.InvariantCulture) + "," +
                           (this.argb & 0x0000FF).ToString(CultureInfo.InvariantCulture) + ")";
                else
                    return "0x" + argb.ToString("X");
            }
        }
    }
    static Hashtable stdColors;

    /// <summary>
    /// Creates a fully opaque Color structure from the specified red, green and blue values.
    /// </summary>
    /// <param name="r">The red component, in a range between 0 and 255.</param>
    /// <param name="g">The green component, in a range between 0 and 255.</param>
    /// <param name="b">The blue component, in a range between 0 and 255.</param>
    /// <remarks>
    /// Equivalent to <c>new Color(r, g, b)</c>. Use <see cref="FromArgb(byte, byte, byte, byte)"/>
    /// to supply an alpha value.
    /// </remarks>
    public static Color FromRgb(byte r, byte g, byte b)
    {
        return new Color(r, g, b);
    }

    /// <summary>
    /// Creates a Color structure from the specified alpha, red, green and blue values.
    /// </summary>
    /// <param name="a">The alpha (opacity) component, in a range between 0 (fully transparent)
    /// and 255 (fully opaque).</param>
    /// <param name="r">The red component, in a range between 0 and 255.</param>
    /// <param name="g">The green component, in a range between 0 and 255.</param>
    /// <param name="b">The blue component, in a range between 0 and 255.</param>
    /// <remarks>
    /// Equivalent to <c>new Color(a, r, g, b)</c>.
    /// </remarks>
    public static Color FromArgb(byte a, byte r, byte g, byte b)
    {
        return new Color(a, r, g, b);
    }

    /// <summary>
    /// Creates an RGB color from an existing color with a new alpha (transparency) value.
    /// </summary>
    public static Color FromRgbColor(byte a, Color color)
    {
        return new Color(a, (byte)color.R, (byte)color.G, (byte)color.B);
    }

    /// <summary>
    /// Creates a Color structure from the specified CMYK values.
    /// All values must be in a range between 0 to 100 percent.
    /// </summary>
    public static Color FromCmyk(double cyan, double magenta, double yellow, double black)
    {
        return new Color(cyan, magenta, yellow, black);
    }

    /// <summary>
    /// Creates a Color structure from the specified CMYK values.
    /// All values must be in a range between 0 to 100 percent.
    /// </summary>
    public static Color FromCmyk(double alpha, double cyan, double magenta, double yellow, double black)
    {
        return new Color(alpha, cyan, magenta, yellow, black);
    }

    /// <summary>
    /// Creates a CMYK color from an existing color with a new alpha (transparency) value.
    /// </summary>
    public static Color FromCmykColor(double alpha, Color color)
    {
        return new Color(alpha, color.C, color.M, color.Y, color.K);
    }

    uint argb; // ARGB
    bool isCmyk;
    private float a; // \
    private float c; // |
    private float m; // |--- alpha + CMYK
    private float y; // |
    private float k; // /

    /// <summary>
    /// Represents a null color.
    /// </summary>
    public static readonly Color Empty = new Color(0);
}