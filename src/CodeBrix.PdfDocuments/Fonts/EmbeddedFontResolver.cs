using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CodeBrix.PdfDocuments.Fonts;

public record EmbeddedResourceFontFace(string FaceName, string EmbeddedResourceName);

public class EmbeddedFontResolver : IFontResolver
{
    // ReSharper disable once IdentifierTypo
    private readonly IReadOnlyDictionary<string, Lazy<byte[]>> _fontDatas;

    public EmbeddedFontResolver(
        string fontFamilyName,
        IList<EmbeddedResourceFontFace> fontFaceResources,
        Assembly fontEmbeddedResourceAssembly)
    {
        DefaultFontName = (!string.IsNullOrWhiteSpace(fontFamilyName))
            ? fontFamilyName.Trim()
            : throw new ArgumentException("Value cannot be null or blank.", nameof(fontFamilyName));

        ArgumentNullException.ThrowIfNull(fontFaceResources);

        if (!fontFaceResources.Any(a => (!string.IsNullOrWhiteSpace(a?.FaceName))
                                        && (!string.IsNullOrWhiteSpace(a.EmbeddedResourceName))))
        {
            throw new ArgumentException("No valid resource entries were found.", nameof(fontFaceResources));
        }

        ArgumentNullException.ThrowIfNull(fontEmbeddedResourceAssembly);

        var fontFaceDict = new Dictionary<string, Lazy<byte[]>>();

        foreach (var resourceFont in fontFaceResources.Where(w => (!string.IsNullOrWhiteSpace(w?.FaceName))
                                                                  && (!string.IsNullOrWhiteSpace(w.EmbeddedResourceName))))
        {
            var key = resourceFont.FaceName.Trim();
            if (!fontFaceDict.ContainsKey(key))
            {
                Lazy<byte[]> fontData = new(() =>
                {
                    using var stream = fontEmbeddedResourceAssembly.GetManifestResourceStream(resourceFont.EmbeddedResourceName.Trim())
                                       ?? throw new InvalidOperationException($"Embedded resource '{resourceFont.EmbeddedResourceName.Trim()}' not found.");
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return ms.ToArray();
                });
                fontFaceDict.Add(key, fontData);
            }
        }

        _fontDatas = fontFaceDict;
    }

    #region | IFontResolver implementation |

    /// <inheritdoc />
    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (familyName.Equals(DefaultFontName, StringComparison.OrdinalIgnoreCase))
        {
            string key = null;
            var allKeys = _fontDatas.Keys.ToArray();

            do
            {
                if (isBold && isItalic)
                {
                    key = allKeys.FirstOrDefault(f =>
                        f.Contains("bold", StringComparison.InvariantCultureIgnoreCase)
                        && f.Contains("italic", StringComparison.InvariantCultureIgnoreCase));
                }
                if (key != null) { break; }

                if (isBold)
                {
                    if (!isItalic)
                    {
                        //If Italic was not specified, first try to find Bold, but not Italic
                        key = allKeys.FirstOrDefault(f =>
                            f.Contains("bold", StringComparison.InvariantCultureIgnoreCase)
                            && (!f.Contains("italic", StringComparison.InvariantCultureIgnoreCase)));
                    }
                    if (key != null) { break; }

                    key = allKeys.FirstOrDefault(f =>
                        f.Contains("bold", StringComparison.InvariantCultureIgnoreCase));
                    if (key != null) { break; }
                }

                if (isItalic)
                {
                    if (!isBold)
                    {
                        //If Bold was not specified, first try to find Italic, but not Bold
                        key = allKeys.FirstOrDefault(f =>
                            (!f.Contains("bold", StringComparison.InvariantCultureIgnoreCase))
                            && f.Contains("italic", StringComparison.InvariantCultureIgnoreCase));
                    }
                    if (key != null) { break; }

                    key = allKeys.FirstOrDefault(f =>
                        f.Contains("italic", StringComparison.InvariantCultureIgnoreCase));
                    if (key != null) { break; }
                }

                if ((!isBold) && (!isItalic))
                {
                    key = allKeys.FirstOrDefault(f =>
                        (!f.Contains("bold", StringComparison.InvariantCultureIgnoreCase))
                        && (!f.Contains("italic", StringComparison.InvariantCultureIgnoreCase)));
                    if (key != null) { break; }
                }
            } while (false);

            return (key != null)
                ? new FontResolverInfo(key)
                : new FontResolverInfo(allKeys[0]);
        }

        return null;
    }

    /// <inheritdoc />
    public byte[] GetFont(string faceName)
    {
        if (!string.IsNullOrWhiteSpace(faceName))
        {
            var key = faceName.Trim();
            if (_fontDatas.TryGetValue(key, out var value))
            {
                return value.Value;
            }
        }

        throw new InvalidOperationException($"Font '{faceName}' is not available in the embedded font resolver.");
    }

    /// <inheritdoc />
    public string DefaultFontName { get; }

    #endregion
}
