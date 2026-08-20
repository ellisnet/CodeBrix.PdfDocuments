using System;
using System.Runtime.InteropServices;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// Recognizes the one environmental failure the SVG path cannot recover from - the
/// SkiaSharp native library not being present - and turns it into guidance the caller
/// can act on.
/// <para>
/// SVG rasterization is the only part of this library that needs SkiaSharp. SkiaSharp's
/// own package brings the Windows and macOS natives transitively, so nothing extra is
/// required there. It never brings a Linux native, because two mutually exclusive
/// variants exist and only the consuming application can choose between them. This
/// library deliberately does not declare either one: doing so would force the choice on
/// every consumer and conflict with applications that already reference the other
/// variant. The consequence is that a Linux application which renders SVG must
/// reference one itself. Both variants serve this library equally well - it never
/// consults system fonts, which is the only thing they differ about - so the guidance
/// must never steer a consumer toward one of them.
/// </para>
/// </summary>
internal static class SkiaNativeLibrary
{
    /// <summary>
    /// The actionable part of the diagnostic - which packages resolve the failure, and
    /// the fact that only Linux is affected. Kept free of any per-image detail so that
    /// a document full of SVGs collapses to a single collected warning.
    /// </summary>
    public const string Guidance =
        "SVG rendering requires the SkiaSharp native library, which was not found. " +
        "On Linux the consuming application must reference one of the two SkiaSharp Linux " +
        "native-assets packages itself: 'SkiaSharp.NativeAssets.Linux' or " +
        "'SkiaSharp.NativeAssets.Linux.NoDependencies'. Either one satisfies this library " +
        "equally - reference whichever suits your application, and only one of them. If your " +
        "application already references either package for its own reasons, keep that one; " +
        "nothing needs to change. (They differ only in how the native obtains font services, " +
        "which does not affect this library: it never consults system fonts.) " +
        "CodeBrix.PdfDocCreate.Html2Pdf cannot declare the dependency itself without " +
        "conflicting with applications that need the other variant. Windows and macOS need " +
        "no extra package. Everything other than SVG content renders normally without it.";

    /// <summary>
    /// True when <paramref name="exception"/> - or anything it wraps - is the native
    /// library failing to load. The failure surfaces as a
    /// <see cref="DllNotFoundException"/>, but the first touch of SkiaSharp happens
    /// inside a static constructor, so the runtime wraps it in one or more
    /// <see cref="TypeInitializationException"/> layers; every later touch rethrows the
    /// cached type-initializer failure. The inner chain is therefore walked rather than
    /// the outermost type being tested.
    /// </summary>
    public static bool IsMissingNativeLibrary(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is DllNotFoundException) { return true; }

            // A native present but unloadable - wrong architecture, or the dependency-bearing
            // variant without libfontconfig installed - arrives as an EntryPointNotFoundException
            // or a bare load failure naming the library. The guidance still applies.
            if (current is EntryPointNotFoundException
                && current.Message.IndexOf("SkiaSharp", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds the message carried by the translated exception and the collected warning.
    /// The platform sentence leads, because on Windows and macOS a missing native means
    /// something quite different - a broken or trimmed deployment rather than a package
    /// the application forgot to reference.
    /// </summary>
    public static string BuildMessage()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Guidance;
        }

        return "SVG rendering requires the SkiaSharp native library, which was not found. " +
               "SkiaSharp supplies the native for this platform through its own package, so a " +
               "missing one usually means the application output is incomplete - for example a " +
               "trimmed or single-file publish that dropped the runtimes folder. On Linux only, " +
               "the application must additionally reference either 'SkiaSharp.NativeAssets.Linux' " +
               "or 'SkiaSharp.NativeAssets.Linux.NoDependencies' - either one is equally " +
               "acceptable. Everything other than SVG content renders normally without it.";
    }
}

/// <summary>
/// Thrown by the SVG rasterizer when rendering fails because the SkiaSharp native
/// library is unavailable, so that the reason is legible from the message alone rather
/// than from an inner <see cref="TypeInitializationException"/> chain. The composer
/// catches this and records it as a rendering warning; SVG content is skipped and the
/// rest of the document still renders.
/// </summary>
internal sealed class SkiaNativeLibraryMissingException : InvalidOperationException
{
    /// <summary>Creates the exception, wrapping the original load failure.</summary>
    public SkiaNativeLibraryMissingException(Exception innerException)
        : base(SkiaNativeLibrary.BuildMessage(), innerException)
    {
    }
}
