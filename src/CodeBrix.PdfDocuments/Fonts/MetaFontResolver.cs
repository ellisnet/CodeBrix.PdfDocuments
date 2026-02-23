using CodeBrix.PdfDocuments.Utils;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace CodeBrix.PdfDocuments.Fonts;

public class MetaFontResolver : IFontResolver
{
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<MetaFontResolver> _lazy =
        new(() => new MetaFontResolver());
    public static MetaFontResolver Instance => _lazy.Value;

    private readonly ConcurrentDictionary<string, IFontResolver> _registeredResolvers = new();
    private readonly IFontResolver _lastResortResolver = new FontResolver();

    public void RegisterFontResolver(string faceName, IFontResolver resolver)
    {
        if (string.IsNullOrWhiteSpace(faceName))
        {
            throw new ArgumentException("Value cannot be null or blank.", nameof(faceName));
        }

        ArgumentNullException.ThrowIfNull(resolver);

        _registeredResolvers.TryAdd(faceName.Trim(), resolver);
    }

    #region | IFontResolver implementation |

    /// <inheritdoc />
    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (!string.IsNullOrWhiteSpace(familyName))
        {
            var trimmed = familyName.Trim();
            var resolver = _registeredResolvers.Values
                .FirstOrDefault(r => r.DefaultFontName.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

            if (resolver is not null)
            {
                return resolver.ResolveTypeface(familyName, isBold, isItalic);
            }
        }

        return _lastResortResolver.ResolveTypeface(familyName, isBold, isItalic);
    }

    /// <inheritdoc />
    public byte[] GetFont(string faceName)
    {
        if ((!string.IsNullOrWhiteSpace(faceName))
            && _registeredResolvers.TryGetValue(faceName.Trim(), out var resolver))
        {
            return resolver.GetFont(faceName.Trim());
        }
        else
        {
            return _lastResortResolver.GetFont(faceName);
        }
    }

    /// <inheritdoc />
    public string DefaultFontName => _lastResortResolver.DefaultFontName;

    #endregion
}
