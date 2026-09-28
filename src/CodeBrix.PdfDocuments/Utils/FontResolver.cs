using CodeBrix.Imaging.Fonts;
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Fonts;
using CodeBrix.PdfDocuments.Internal;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CodeBrix.PdfDocuments.Utils; //Was previously: namespace PdfSharpCore.Utils;

/// <summary>
/// The system-font resolver: discovers the TrueType (.ttf) fonts installed on the host the first time the type is used,
/// and serves them by family name. Font sources per platform:
/// <list type="table">
/// <listheader><term>Platform</term><description>Fonts discovered</description></listheader>
/// <item><term>Windows</term><description>%SystemRoot%\Fonts and %LOCALAPPDATA%\Microsoft\Windows\Fonts</description></item>
/// <item><term>macOS</term><description>/Library/Fonts</description></item>
/// <item><term>Linux</term><description>fontconfig's font list (fallback: the directories named in /etc/fonts/fonts.conf)</description></item>
/// <item><term>Android</term><description>/system/fonts, plus /system/font and /product/fonts where they exist</description></item>
/// <item><term>Any other platform</term><description>none - only faces registered through an <see cref="IFontResolver"/>
/// (for example an <see cref="EmbeddedFontResolver"/> on <see cref="MetaFontResolver"/>) are available</description></item>
/// </list>
/// An unrecognised platform does not throw: it gets an empty font set. With no fonts discovered, <see cref="ResolveTypeface"/> throws a
/// <see cref="System.IO.FileNotFoundException"/> for any family that is asked of it.
/// </summary>
public class FontResolver 
    : IFontResolver
{
    public string DefaultFontName => "Arial";

    private static readonly Dictionary<string, FontFamilyModel> InstalledFonts = new Dictionary<string, FontFamilyModel>();

    private static readonly string[] SSupportedFonts;

    public FontResolver()
    {
    }

    static FontResolver()
    {
        string fontDir;

        bool isOSX = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
        if (isOSX)
        {
            fontDir = "/Library/Fonts/";
            SSupportedFonts = System.IO.Directory.GetFiles(fontDir, "*.ttf", System.IO.SearchOption.AllDirectories);
            SetupFontsFiles(SSupportedFonts);
            return;
        }

        bool isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
        if (isLinux)
        {
            SSupportedFonts = LinuxSystemFontResolver.Resolve();
            SetupFontsFiles(SSupportedFonts);
            return;
        }

        bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        if (isWindows)
        {
            fontDir = System.Environment.ExpandEnvironmentVariables(@"%SystemRoot%\Fonts");
            var fontPaths = new List<string>();

            var systemFontPaths = System.IO.Directory.GetFiles(fontDir, "*.ttf", System.IO.SearchOption.AllDirectories);
            fontPaths.AddRange(systemFontPaths);

            var appdataFontDir = System.Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Microsoft\Windows\Fonts");
            if(System.IO.Directory.Exists(appdataFontDir))
            {
                var appdataFontPaths = System.IO.Directory.GetFiles(appdataFontDir, "*.ttf", System.IO.SearchOption.AllDirectories);
                fontPaths.AddRange(appdataFontPaths);
            }

            SSupportedFonts = fontPaths.ToArray();
            SetupFontsFiles(SSupportedFonts);
            return;
        }

        if (System.OperatingSystem.IsAndroid())
        {
            SSupportedFonts = ScanFontFolders(AndroidFontFolders);
            SetupFontsFiles(SSupportedFonts);
            return;
        }

        // Any other platform (iOS, browser, unknown): no system font source is known, so the set is empty.
        // Faces registered through an IFontResolver keep working; a system-face lookup fails at ResolveTypeface.
        SSupportedFonts = System.Array.Empty<string>();
        SetupFontsFiles(SSupportedFonts);
    }

    /// <summary>
    /// The folders Android keeps its system fonts in. Only the ones that exist on the device are scanned.
    /// </summary>
    internal static readonly string[] AndroidFontFolders = { "/system/fonts", "/system/font", "/product/fonts" };

    /// <summary>
    /// Returns every TrueType (.ttf) file under the given folders (recursively), skipping folders that do not exist
    /// and sub-folders that cannot be read. Never throws.
    /// </summary>
    /// <param name="folders">The folders to scan.</param>
    /// <returns>The full paths of the .ttf files found; empty when there are none.</returns>
    internal static string[] ScanFontFolders(IEnumerable<string> folders)
    {
        var fontPaths = new List<string>();
        var options = new System.IO.EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            MatchCasing = System.IO.MatchCasing.CaseInsensitive
        };

        foreach (string folder in folders ?? Enumerable.Empty<string>())
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(folder) && System.IO.Directory.Exists(folder))
                    fontPaths.AddRange(System.IO.Directory.GetFiles(folder, "*.ttf", options));
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (System.Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
            {
#if DEBUG
                System.Console.Error.WriteLine(e);
#endif
            }
        }

        return fontPaths.ToArray();
    }


    private readonly struct FontFileInfo
    {
        private FontFileInfo(string path, FontDescription fontDescription)
        {
            this.Path = path;
            this.FontDescription = fontDescription;
        }

        public string Path { get; }

        public FontDescription FontDescription { get; }

        public string FamilyName => this.FontDescription.FontFamilyInvariantCulture;


        public XFontStyle GuessFontStyle()
        {
            switch (this.FontDescription.Style)
            {
                case FontStyle.Bold:
                    return XFontStyle.Bold;
                case FontStyle.Italic:
                    return XFontStyle.Italic;
                case FontStyle.BoldItalic:
                    return XFontStyle.BoldItalic;
                default:
                    return XFontStyle.Regular;
            }
        }

        public static FontFileInfo Load(string path)
        {
            FontDescription fontDescription = FontDescription.LoadDescription(path);
            return new FontFileInfo(path, fontDescription);
        }
    }


    public static void SetupFontsFiles(string[] sSupportedFonts)
    {
        List<FontFileInfo> tempFontInfoList = new List<FontFileInfo>();
        foreach (string fontPathFile in sSupportedFonts)
        {
            try
            {
                FontFileInfo fontInfo = FontFileInfo.Load(fontPathFile);
                Debug.WriteLine(fontPathFile);
                tempFontInfoList.Add(fontInfo);
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (System.Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
            {
#if DEBUG
                System.Console.Error.WriteLine(e);
#endif
            }
        }

        // Deserialize all font families
        foreach (IGrouping<string, FontFileInfo> familyGroup in tempFontInfoList.GroupBy(info => info.FamilyName))
            try
            {
                string familyName = familyGroup.Key;
                FontFamilyModel family = DeserializeFontFamily(familyName, familyGroup);
                InstalledFonts.Add(familyName.ToLower(), family);
            }
#pragma warning disable CS0168 // Variable is declared but never used
            catch (System.Exception e)
#pragma warning restore CS0168 // Variable is declared but never used
            {
#if DEBUG
                System.Console.Error.WriteLine(e);
#endif
            }
    }


    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    private static FontFamilyModel DeserializeFontFamily(string fontFamilyName, IEnumerable<FontFileInfo> fontList)
    {
        FontFamilyModel font = new FontFamilyModel { Name = fontFamilyName };

        // there is only one font
        if (fontList.Count() == 1)
            font.FontFiles.Add(XFontStyle.Regular, fontList.First().Path);
        else
        {
            foreach (FontFileInfo info in fontList)
            {
                XFontStyle style = info.GuessFontStyle();
                if (!font.FontFiles.ContainsKey(style))
                    font.FontFiles.Add(style, info.Path);
            }
        }

        return font;
    }

    public virtual byte[] GetFont(string faceFileName)
    {
        using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
        {
            string ttfPathFile = "";
            try
            {
                ttfPathFile = SSupportedFonts.ToList().First(x => x.ToLower().Contains(
                    System.IO.Path.GetFileName(faceFileName).ToLower())
                );

                using (System.IO.Stream ttf = System.IO.File.OpenRead(ttfPathFile))
                {
                    ttf.CopyTo(ms);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e);
                throw new System.Exception("No Font File Found - " + faceFileName + " - " + ttfPathFile);
            }
        }
    }

    public bool NullIfFontNotFound { get; set; } = false;

    public virtual FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (InstalledFonts.Count == 0)
            throw new System.IO.FileNotFoundException("No Fonts installed on this device!");

        if (InstalledFonts.TryGetValue(familyName.ToLower(), out FontFamilyModel family))
        {
            if (isBold && isItalic)
            {
                if (family.FontFiles.TryGetValue(XFontStyle.BoldItalic, out string boldItalicFile))
                    return new FontResolverInfo(System.IO.Path.GetFileName(boldItalicFile));
            }
            else if (isBold)
            {
                if (family.FontFiles.TryGetValue(XFontStyle.Bold, out string boldFile))
                    return new FontResolverInfo(System.IO.Path.GetFileName(boldFile));
            }
            else if (isItalic)
            {
                if (family.FontFiles.TryGetValue(XFontStyle.Italic, out string italicFile))
                    return new FontResolverInfo(System.IO.Path.GetFileName(italicFile));
            }

            if (family.FontFiles.TryGetValue(XFontStyle.Regular, out string regularFile))
                return new FontResolverInfo(System.IO.Path.GetFileName(regularFile));

            return new FontResolverInfo(System.IO.Path.GetFileName(family.FontFiles.First().Value));
        }

        if (NullIfFontNotFound)
            return null;

        string ttfFile = InstalledFonts.First().Value.FontFiles.First().Value;
        return new FontResolverInfo(System.IO.Path.GetFileName(ttfFile));
    }
}