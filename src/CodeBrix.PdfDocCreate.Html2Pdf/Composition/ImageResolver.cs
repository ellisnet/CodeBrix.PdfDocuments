using System;
using System.IO;
using System.Net.Http;
using CodeBrix.Imaging;
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Utils;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Composition;

/// <summary>
/// Resolves img src references - local paths relative to the document base directory,
/// data: URIs, and (only when enabled) http(s) URLs - into embeddable image sources
/// with their natural size. Every failure is a warning, never an exception: a missing
/// image must not sink the document.
/// </summary>
internal sealed class ImageResolver
{
    private static readonly object BackendSync = new object();
    private static HttpClient _httpClient;

    private readonly string _baseDirectory;
    private readonly bool _allowRemote;
    private readonly RenderWarnings _warnings;
    private int _counter;

    public ImageResolver(string baseDirectory, bool allowRemote, RenderWarnings warnings)
    {
        _baseDirectory = baseDirectory;
        _allowRemote = allowRemote;
        _warnings = warnings;
        EnsureImagingBackend();
    }

    /// <summary>Wires the CodeBrix.Imaging-backed image source implementation exactly once.</summary>
    public static void EnsureImagingBackend()
    {
        lock (BackendSync)
        {
            ImageSource.ImageSourceImpl ??= new ImagingImageSource<CodeBrix.Imaging.PixelFormats.Rgba32>();
        }
    }

    /// <summary>
    /// Attempts to resolve an image reference. On success returns true with the image
    /// source and the image's natural size in points (1 CSS px = 0.75 pt).
    /// </summary>
    public bool TryResolve(string src, out ImageSource.IImageSource source, out double naturalWidthPoints, out double naturalHeightPoints)
    {
        source = null;
        naturalWidthPoints = 0;
        naturalHeightPoints = 0;

        if (string.IsNullOrWhiteSpace(src))
        {
            _warnings.Add(RenderWarnings.CategoryImage, "An img element without a usable src attribute was skipped.");
            return false;
        }

        var reference = src.Trim();
        byte[] bytes;

        try
        {
            if (reference.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                bytes = DecodeDataUri(reference);
            }
            else if (reference.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                     || reference.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (!_allowRemote)
                {
                    _warnings.Add(RenderWarnings.CategoryImage,
                        $"Remote image '{Truncate(reference)}' was skipped; enable AllowRemoteImages to fetch http(s) images.");
                    return false;
                }
                bytes = DownloadBytes(reference);
            }
            else
            {
                bytes = ReadLocalFile(reference);
            }
        }
        catch (Exception ex)
        {
            _warnings.Add(RenderWarnings.CategoryImage,
                $"Image '{Truncate(reference)}' could not be loaded and was skipped ({ex.GetType().Name}).");
            return false;
        }

        if (bytes == null || bytes.Length == 0)
        {
            _warnings.Add(RenderWarnings.CategoryImage, $"Image '{Truncate(reference)}' could not be loaded and was skipped.");
            return false;
        }

        try
        {
            using (var image = Image.Load(bytes))
            {
                naturalWidthPoints = image.Width * 0.75;
                naturalHeightPoints = image.Height * 0.75;
            }
        }
        catch (Exception ex)
        {
            _warnings.Add(RenderWarnings.CategoryImage,
                $"Image '{Truncate(reference)}' is not in a supported format and was skipped ({ex.GetType().Name}).");
            return false;
        }

        var name = $"html2pdf-img-{++_counter}";
        var captured = bytes;
        source = ImageSource.FromBinary(name, () => captured, quality: 90);
        return true;
    }

    private byte[] ReadLocalFile(string reference)
    {
        var candidate = Path.IsPathRooted(reference)
            ? reference
            : Path.Combine(_baseDirectory ?? "", reference.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(candidate))
        {
            // href/src values are often percent-encoded ("my%20image.png").
            var unescaped = Uri.UnescapeDataString(candidate);
            if (File.Exists(unescaped)) { candidate = unescaped; }
        }

        return File.ReadAllBytes(candidate);
    }

    private static byte[] DecodeDataUri(string reference)
    {
        var comma = reference.IndexOf(',');
        if (comma < 0) { throw new FormatException("The data: URI has no payload."); }

        var header = reference.Substring(0, comma);
        var payload = reference.Substring(comma + 1);

        return header.Contains(";base64", StringComparison.OrdinalIgnoreCase)
            ? Convert.FromBase64String(payload)
            : System.Text.Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
    }

    private static byte[] DownloadBytes(string url)
    {
        lock (BackendSync)
        {
            _httpClient ??= new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        using (var response = _httpClient.Send(new HttpRequestMessage(HttpMethod.Get, url)))
        {
            response.EnsureSuccessStatusCode();
            using (var ms = new MemoryStream())
            {
                response.Content.ReadAsStream().CopyTo(ms);
                return ms.ToArray();
            }
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 80 ? value : value.Substring(0, 77) + "...";
}
