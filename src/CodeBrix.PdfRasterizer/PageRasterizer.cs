// ============================================================================
// PDF page rasterizer using PDFium native library.
//
// Rendering approach derived from Docnet.Core (https://github.com/GowenGit/docnet)
// Original copyright (c) 2018 Modestas Petravicius, MIT License.
//
// This implementation simplifies the Docnet.Core rendering pipeline to use
// direct P/Invoke with IntPtr handles instead of CppSharp-generated wrappers.
// PNG encoding is handled by CodeBrix.Imaging instead of raw byte output.
//
// Key rendering features derived from Docnet.Core:
//   - Matrix-based rendering (FPDF_RenderPageBitmapWithMatrix) for proper
//     scaling and rotation support
//   - Form fill rendering (FPDFDOC_InitFormFillEnvironment + FPDF_FFLDraw)
//     for PDFs with fillable form fields
//   - Page transparency detection (FPDFPage_HasTransparency) to correctly
//     handle pages with transparent content
//
// PDFium itself is copyright 2014 The PDFium Authors, BSD License.
// ============================================================================

using CodeBrix.Imaging;
using CodeBrix.Imaging.Formats;
using CodeBrix.Imaging.Formats.Png;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfRasterizer.Pdfium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace CodeBrix.PdfRasterizer;

/// <summary>
/// Rasterizes PDF pages to image files using the PDFium rendering engine.
/// </summary>
/// <remarks>
/// Create an instance with an optional logger, then call one of the Rasterize methods to render pages.
/// The output image format is controlled by the <see cref="RasterizedImageFormat"/> property (default PNG).
/// PDFium native library resolution and initialization happens once for the lifetime of the application.
/// </remarks>
public sealed partial class PageRasterizer : IDisposable
{
    private static readonly SemaphoreSlim _pdfiumLocker = new (1,1);
    private static bool IsPdfiumEngineInitialized => PdfiumEngine._isInitialized;

    /// <summary>
    /// Manages one-time PDFium native library resolution and initialization.
    /// Thread-safe; initialization occurs at most once per application lifetime.
    /// </summary>
    private static class PdfiumEngine
    {
        private static SemaphoreSlim _locker;
        internal static bool _isInitialized;

        internal static void EnsureInitialized(SemaphoreSlim locker)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            if (_isInitialized) return;

            ArgumentNullException.ThrowIfNull(locker);
            Interlocked.CompareExchange(ref _locker, locker, null);

            var isLocked = false;

            try
            {
                isLocked = _locker.Wait(TimeSpan.FromSeconds(5));

                if (!isLocked)
                {
                    throw new TimeoutException(
                        "Timed out waiting to initialize the PDFium native library.");
                }

                if (_isInitialized) return;
                NativeLibrary.SetDllImportResolver(
                    typeof(PageRasterizer).Assembly, ResolvePdfium);
                PdfiumBindings.FPDF_InitLibrary();
                _isInitialized = true;
            }
            finally
            {
                if (isLocked) { _locker.Release(); }
            }
        }

        private static IntPtr ResolvePdfium(
            string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName != "pdfium")
                return IntPtr.Zero;

            // Determine the platform-specific native library path
            var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? ".";

            string rid;
            string fileName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                rid = RuntimeInformation.OSArchitecture switch
                {
                    Architecture.Arm64 => "win-arm64",
                    Architecture.X86 => "win-x86",
                    _ => "win-x64"
                };
                fileName = "pdfium.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                rid = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
                fileName = "pdfium.dylib";
            }
            else
            {
                rid = RuntimeInformation.OSArchitecture switch
                {
                    Architecture.Arm64 => "linux-arm64",
                    Architecture.Arm => "linux-arm",
                    _ => "linux-x64"
                };
                fileName = "pdfium.so";
            }

            var nativePath = Path.Combine(assemblyDir, "runtimes", rid, "native", fileName);
            if (NativeLibrary.TryLoad(nativePath, out var handle))
                return handle;

            // Fallback: win-arm64 can run win-x64 binaries via emulation
            if (rid == "win-arm64")
            {
                nativePath = Path.Combine(assemblyDir, "runtimes", "win-x64", "native", fileName);
                if (NativeLibrary.TryLoad(nativePath, out handle))
                    return handle;
            }

            // Fallback: try the "linux" RID (as used by Docnet.Core's original structure)
            if (rid == "linux-x64")
            {
                nativePath = Path.Combine(assemblyDir, "runtimes", "linux", "native", fileName);
                if (NativeLibrary.TryLoad(nativePath, out handle))
                    return handle;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Initializes the PDFium form fill environment for a document.
        /// Follows Docnet.Core's FormWrapper pattern: tries version 1, then version 2
        /// of the FPDF_FORMFILLINFO structure to maximize compatibility.
        /// </summary>
        /// <returns>The form handle and the allocated formInfo pointer (both must be freed by the caller).</returns>
        internal static (IntPtr formHandle, IntPtr formInfoPtr) InitFormFillEnvironment(IntPtr document)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    $"{nameof(InitFormFillEnvironment)} cannot be called before {nameof(EnsureInitialized)} has been called.");
            }

            var formInfo = new FpdfFormFillInfo();
            var formInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(formInfo));

            for (var version = 1; version <= 2; version++)
            {
                formInfo.Version = version;
                Marshal.StructureToPtr(formInfo, formInfoPtr, false);
                var handle = PdfiumBindings.FPDFDOC_InitFormFillEnvironment(document, formInfoPtr);
                if (handle != IntPtr.Zero)
                    return (handle, formInfoPtr);
            }

            // Form fill initialization failed — not fatal, we just won't render form fields
            return (IntPtr.Zero, formInfoPtr);
        }

        /// <summary>
        /// Renders a single PDF page to an image.
        /// Uses FPDF_RenderPageBitmapWithMatrix for proper scaling/rotation support,
        /// derived from Docnet.Core's PageReader.WriteImageToBufferInternal method.
        /// </summary>
        internal static Image RenderPageToImage(
            IntPtr document, int pageIndex, double scaling,
            PdfRenderFlags renderFlags, IntPtr formHandle, IImageFormat imageFormat,
            uint backgroundColor)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    $"{nameof(RenderPageToImage)} cannot be called before {nameof(EnsureInitialized)} has been called.");
            }

            var page = PdfiumBindings.FPDF_LoadPage(document, pageIndex);
            if (page == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to load page {pageIndex}");

            try
            {
                var pageWidthPt = PdfiumBindings.FPDF_GetPageWidth(page);
                var pageHeightPt = PdfiumBindings.FPDF_GetPageHeight(page);
                var width = (int)(pageWidthPt * scaling);
                var height = (int)(pageHeightPt * scaling);

                // Detect page transparency to decide whether to create the bitmap
                // with an alpha channel. Derived from Docnet.Core's transparency handling.
                var hasTransparency = PdfiumBindings.FPDFPage_HasTransparency(page) != 0;
                var bitmap = PdfiumBindings.FPDFBitmap_Create(width, height, hasTransparency ? 1 : 0);
                if (bitmap == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to create PDFium bitmap");

                try
                {
                    // Fill with the specified background color (ARGB format)
                    // For transparent pages, this provides a base layer
                    PdfiumBindings.FPDFBitmap_FillRect(bitmap, 0, 0, width, height, backgroundColor);

                    // Build the transformation matrix for scaling.
                    // Layout: | a b 0 |   where a = x-scale, d = y-scale
                    //         | c d 0 |   b,c = shear (0 for no rotation)
                    //         | e f 1 |   e,f = translation (0 for origin)
                    // Derived from Docnet.Core's PageReader matrix setup.
                    var matrix = new FsMatrix
                    {
                        A = (float)scaling,
                        B = 0,
                        C = 0,
                        D = (float)scaling,
                        E = 0,
                        F = 0
                    };

                    var clipping = new FsRectF
                    {
                        Left = 0,
                        Top = 0,
                        Right = width,
                        Bottom = height
                    };

                    // Render the page using matrix-based rendering for proper scaling
                    PdfiumBindings.FPDF_RenderPageBitmapWithMatrix(
                        bitmap, page, ref matrix, ref clipping, (int)renderFlags);

                    // Render form fields on top of the page bitmap if form fill is available.
                    // Derived from Docnet.Core's PageReader: conditionally calls FPDF_FFLDraw
                    // when RenderAnnotations flag is set and form handle was successfully created.
                    if (formHandle != IntPtr.Zero)
                    {
                        PdfiumBindings.FPDF_FFLDraw(
                            formHandle, bitmap, page, 0, 0, width, height, 0, (int)renderFlags);
                    }

                    // Get the rendered pixel data (BGRA format from PDFium)
                    var buffer = PdfiumBindings.FPDFBitmap_GetBuffer(bitmap);
                    var stride = PdfiumBindings.FPDFBitmap_GetStride(bitmap);

                    // Copy BGRA pixels from the native buffer into a contiguous byte array,
                    // stripping any stride padding (stride may be larger than width * 4).
                    var rowBytes = width * 4;
                    var bgraData = new byte[rowBytes * height];
                    for (var y = 0; y < height; y++)
                    {
                        Marshal.Copy(buffer + y * stride, bgraData, y * rowBytes, rowBytes);
                    }

                    // Use LoadPixelDataFromBgra which handles BGRA-to-RGBA conversion
                    // internally using SIMD-optimized (AVX2/SSSE3) channel reordering.
                    return Image.LoadPixelDataFromBgra(bgraData, width, height, imageFormat);
                }
                finally
                {
                    PdfiumBindings.FPDFBitmap_Destroy(bitmap);
                }
            }
            finally
            {
                PdfiumBindings.FPDF_ClosePage(page);
            }
        }
    }

    private const int DefaultDpi = 300;
    private static readonly Func<int, string> DefaultFileNameGenerator = pageNumber => $"Rasterized_Page_{pageNumber}";

    private Action<string> _logger;
    private bool _disposed;

    /// <summary>
    /// Gets or sets the default rendering resolution in dots per inch.
    /// Used when <c>dpi</c> is not specified (or is <c>null</c> / less than 1) in a call
    /// to rasterize images.
    /// Default value is 300. Setting a value less than 1 resets to the default.
    /// </summary>
    public int Dpi
    {
        get;
        set => field = value < 1 ? DefaultDpi : value;
    } = DefaultDpi;

    /// <summary>
    /// Gets or sets a default password for encrypted PDFs.
    /// Used when <c>password</c> is not specified (or is <c>null</c>) in a call
    /// to rasterize images.
    /// </summary>
    public string Password
    {
        get;
        set => field = (string.IsNullOrEmpty(value)) ? null : value;
    }

    /// <summary>
    /// Gets or sets the default output directory for rasterized image files.
    /// Used when <c>outputDirectory</c> is not specified (or is <c>null</c>) in a call
    /// to <see cref="RasterizeToImageFiles(string, string, int?, string, IImageFormat, PdfRenderFlags)"/>.
    /// If neither this property nor the method argument provides a directory,
    /// an <see cref="ArgumentException"/> is thrown.
    /// </summary>
    public string OutputDirectory
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    /// Gets or sets a function that generates the file name (without extension) for each
    /// rasterized page. The function receives the 1-based page number and returns the
    /// file name stem. Setting to <c>null</c> resets to the default generator
    /// (<c>"Rasterized_Page_{pageNumber}"</c>).
    /// </summary>
    public Func<int, string> FileNameGenerator
    {
        get;
        set => field = value ?? DefaultFileNameGenerator;
    } = DefaultFileNameGenerator;

    /// <summary>
    /// Gets or sets the image format used for rasterized output files.
    /// The format's <see cref="IImageFormat.DefaultFileExtension"/> determines the file extension.
    /// Default is <see cref="PngFormat"/>. Setting to <see cref="UnknownImageFormat"/> throws
    /// an <see cref="ArgumentException"/>.
    /// </summary>
    public IImageFormat RasterizedImageFormat
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value, nameof(RasterizedImageFormat));
            if (value is UnknownImageFormat)
                throw new ArgumentException(
                    $"{nameof(RasterizedImageFormat)} cannot be set to {nameof(UnknownImageFormat)}.",
                    nameof(RasterizedImageFormat));
            field = value;
        }
    } = PngFormat.Instance;

    /// <summary>
    /// Gets or sets a value indicating whether existing files in the output directory
    /// may be overwritten. When <c>false</c> (the default), an <see cref="IOException"/>
    /// is thrown if a target file already exists.
    /// </summary>
    public bool AllowOverwriteFiles { get; set; }

    /// <summary>
    /// Gets or sets the ARGB background color used when rasterizing PDF pages.
    /// The default is <c>0xFFFFFFFF</c> (opaque white). Changing this affects the
    /// base layer drawn behind all page content.
    /// </summary>
    public uint BackgroundColor { get; set; } = 0xFFFFFFFF;

    private void CheckDisposed([CallerMemberName] string caller = null)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(objectName: GetType().Name,
                message: (string.IsNullOrWhiteSpace(caller))
                    ? $"This {GetType().Name} instance has been disposed."
                    : $"Cannot call {caller.Trim()} on a {GetType().Name} instance that has been disposed.");
        }
    }

    private static void CheckPdfiumEngineIsInitialized()
    {
        if (!IsPdfiumEngineInitialized)
        {
            throw new InvalidOperationException(
                "The PDFium native library has not been initialized. " +
                "Ensure a PageRasterizer instance was successfully constructed before calling this method.");
        }
    }

    /// <summary>
    /// Creates a new <see cref="PageRasterizer"/> instance.
    /// </summary>
    /// <param name="logger">Optional logging callback for rasterization progress messages.</param>
    /// <param name="password">Optional default password for encrypted PDFs.</param>
    public PageRasterizer(Action<string> logger = null, string password = null)
    {
        _logger = logger ?? (_ => { });
        Password = password;
        PdfiumEngine.EnsureInitialized(_pdfiumLocker);
    }

    /// <summary>
    /// Rasterizes pages of a PDF file to image files.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="outputDirectory">
    /// Directory to write the image files. When <c>null</c>, the <see cref="OutputDirectory"/> property
    /// value is used. If neither provides a directory, an <see cref="ArgumentException"/> is thrown.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToImageFiles(
        string pdfPath, 
        string outputDirectory = null, 
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        await RasterizeToImageFiles(pdfBytes, outputDirectory, dpi, password, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes pages of a PDF (from a byte array) to image files.
    /// </summary>
    /// <param name="pdfBytes">The PDF file content as a byte array.</param>
    /// <param name="outputDirectory">
    /// Directory to write the image files. When <c>null</c>, the <see cref="OutputDirectory"/> property
    /// value is used. If neither provides a directory, an <see cref="ArgumentException"/> is thrown.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToImageFiles(
        byte[] pdfBytes, 
        string outputDirectory = null, 
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        if (desiredImageFormat is UnknownImageFormat)
        {
            throw new ArgumentException($"Invalid image format: {nameof(UnknownImageFormat)}", nameof(desiredImageFormat));
        }

        var pdfPtr = default(nint);
        var isLocked = false;

        try
        {
            isLocked = await _pdfiumLocker.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

            if (!isLocked)
            {
                throw new TimeoutException($"Timeout waiting for {nameof(PdfiumEngine)} lock.");
            }

            var effectiveDirectory = outputDirectory ?? OutputDirectory
                ?? throw new ArgumentException(
                    $"No output directory specified. Either pass a value for '{nameof(outputDirectory)}' " +
                    $"or set the '{nameof(OutputDirectory)}' property.",
                    nameof(outputDirectory));

            if (!Directory.Exists(effectiveDirectory)) { Directory.CreateDirectory(effectiveDirectory); }

            var effectiveDpi = (dpi is > 0) ? dpi.Value : Dpi;
            var effectivePassword = password ?? Password;
            var scaling = effectiveDpi / 72.0;
            var imageFormat = desiredImageFormat ?? RasterizedImageFormat;
            var fileExtension = imageFormat.DefaultFileExtension;

            // Pin the PDF bytes in unmanaged memory for PDFium
            pdfPtr = Marshal.AllocHGlobal(pdfBytes.Length);
            Marshal.Copy(pdfBytes, 0, pdfPtr, pdfBytes.Length);

            var document = PdfiumBindings.FPDF_LoadMemDocument(pdfPtr, pdfBytes.Length, effectivePassword);
            if (document == IntPtr.Zero)
            {
                var error = PdfiumBindings.FPDF_GetLastError();
                throw new InvalidOperationException(
                    $"PDFium failed to load document (error code: {error})");
            }

            try
            {
                // Initialize form fill environment if we need to render annotations.
                // Derived from Docnet.Core's FormWrapper pattern: allocate an
                // FPDF_FORMFILLINFO struct, try version 1 then 2, and use the
                // resulting handle for FPDF_FFLDraw calls.
                var formHandle = IntPtr.Zero;
                var formInfoPtr = IntPtr.Zero;
                var renderAnnotations = renderFlags.HasFlag(PdfRenderFlags.RenderAnnotations);

                if (renderAnnotations)
                {
                    (formHandle, formInfoPtr) = PdfiumEngine.InitFormFillEnvironment(document);
                }

                try
                {
                    var pageCount = PdfiumBindings.FPDF_GetPageCount(document);

                    if (pageNumbers is not null)
                    {
                        foreach (var pn in pageNumbers)
                        {
                            if (pn < 1 || pn > pageCount)
                                throw new ArgumentException(
                                    $"Page number {pn} is out of range. The document has {pageCount} page(s).",
                                    nameof(pageNumbers));
                        }
                    }

                    var renderCount = pageNumbers?.Count ?? pageCount;
                    _logger($"Rasterizing {renderCount} pages at {effectiveDpi} DPI...");

                    for (var idx = 0; idx < renderCount; idx++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var pageNumber = pageNumbers is not null ? pageNumbers[idx] : idx + 1;
                        var fileName = $"{FileNameGenerator(pageNumber)}.{fileExtension}";
                        var pagePath = Path.Combine(effectiveDirectory, fileName);

                        if (!AllowOverwriteFiles && File.Exists(pagePath))
                            throw new IOException(
                                $"Output file already exists and {nameof(AllowOverwriteFiles)} is false: {pagePath}");

                        using var image = PdfiumEngine.RenderPageToImage(
                            document, pageNumber - 1, scaling, renderFlags, formHandle, imageFormat, BackgroundColor);
                        await using var fs = new FileStream(pagePath, FileMode.Create, FileAccess.Write);
                        await image.SaveAsync(fs, imageFormat, cancellationToken);
                        _logger($"  Page {pageNumber}: {pagePath}");
                    }

                    _logger($"Rasterized {renderCount} pages.");
                }
                finally
                {
                    if (formHandle != IntPtr.Zero)
                        PdfiumBindings.FPDFDOC_ExitFormFillEnvironment(formHandle);
                    if (formInfoPtr != IntPtr.Zero)
                        Marshal.FreeHGlobal(formInfoPtr);
                }
            }
            finally
            {
                PdfiumBindings.FPDF_CloseDocument(document);
            }
        }
        finally
        {
            // ReSharper disable once PreferConcreteValueOverDefault
            if (pdfPtr != default)
            {
                Marshal.FreeHGlobal(pdfPtr);
            }

            if (isLocked)
            {
                _pdfiumLocker.Release();
            }
        }
    }

    /// <summary>
    /// Rasterizes pages of a PDF file to in-memory images.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of <see cref="Image"/> objects, one per requested page. The caller is responsible for disposing each image.</returns>
    public async Task<IList<Image>> RasterizeToImages(
        string pdfPath,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        return await RasterizeToImages(pdfBytes, dpi, password, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes pages of a PDF (from a byte array) to in-memory images.
    /// </summary>
    /// <param name="pdfBytes">The PDF file content as a byte array.</param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of <see cref="Image"/> objects, one per requested page. The caller is responsible for disposing each image.</returns>
    public async Task<IList<Image>> RasterizeToImages(
        byte[] pdfBytes,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        if (desiredImageFormat is UnknownImageFormat)
        {
            throw new ArgumentException($"Invalid image format: {nameof(UnknownImageFormat)}", nameof(desiredImageFormat));
        }

        var pdfPtr = default(nint);
        var isLocked = false;
        var images = new List<Image>();

        try
        {
            isLocked = await _pdfiumLocker.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

            if (!isLocked)
            {
                throw new TimeoutException($"Timeout waiting for {nameof(PdfiumEngine)} lock.");
            }

            var effectiveDpi = (dpi is > 0) ? dpi.Value : Dpi;
            var effectivePassword = password ?? Password;
            var scaling = effectiveDpi / 72.0;
            var imageFormat = desiredImageFormat ?? RasterizedImageFormat;

            // Pin the PDF bytes in unmanaged memory for PDFium
            pdfPtr = Marshal.AllocHGlobal(pdfBytes.Length);
            Marshal.Copy(pdfBytes, 0, pdfPtr, pdfBytes.Length);

            var document = PdfiumBindings.FPDF_LoadMemDocument(pdfPtr, pdfBytes.Length, effectivePassword);
            if (document == IntPtr.Zero)
            {
                var error = PdfiumBindings.FPDF_GetLastError();
                throw new InvalidOperationException(
                    $"PDFium failed to load document (error code: {error})");
            }

            try
            {
                var formHandle = IntPtr.Zero;
                var formInfoPtr = IntPtr.Zero;
                var renderAnnotations = renderFlags.HasFlag(PdfRenderFlags.RenderAnnotations);

                if (renderAnnotations)
                {
                    (formHandle, formInfoPtr) = PdfiumEngine.InitFormFillEnvironment(document);
                }

                try
                {
                    var pageCount = PdfiumBindings.FPDF_GetPageCount(document);

                    if (pageNumbers is not null)
                    {
                        foreach (var pn in pageNumbers)
                        {
                            if (pn < 1 || pn > pageCount)
                                throw new ArgumentException(
                                    $"Page number {pn} is out of range. The document has {pageCount} page(s).",
                                    nameof(pageNumbers));
                        }
                    }

                    var renderCount = pageNumbers?.Count ?? pageCount;
                    _logger($"Rasterizing {renderCount} pages at {effectiveDpi} DPI...");

                    for (var idx = 0; idx < renderCount; idx++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var pageNumber = pageNumbers is not null ? pageNumbers[idx] : idx + 1;
                        var image = PdfiumEngine.RenderPageToImage(
                            document, pageNumber - 1, scaling, renderFlags, formHandle, imageFormat, BackgroundColor);
                        images.Add(image);
                        _logger($"  Page {pageNumber}: rendered to image");
                    }

                    _logger($"Rasterized {renderCount} pages.");
                }
                finally
                {
                    if (formHandle != IntPtr.Zero)
                        PdfiumBindings.FPDFDOC_ExitFormFillEnvironment(formHandle);
                    if (formInfoPtr != IntPtr.Zero)
                        Marshal.FreeHGlobal(formInfoPtr);
                }
            }
            finally
            {
                PdfiumBindings.FPDF_CloseDocument(document);
            }
        }
        catch
        {
            // Dispose any images that were successfully created before the failure
            foreach (var image in images) { image.Dispose(); }
            throw;
        }
        finally
        {
            // ReSharper disable once PreferConcreteValueOverDefault
            if (pdfPtr != default)
            {
                Marshal.FreeHGlobal(pdfPtr);
            }

            if (isLocked)
            {
                _pdfiumLocker.Release();
            }
        }

        return images;
    }

    /// <summary>
    /// Rasterizes a single page of a PDF file to an in-memory image.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="Image"/> of the requested page. The caller is responsible for disposing the image.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<Image> RasterizeToImage(
        string pdfPath,
        int pageNumber,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        return await RasterizeToImage(pdfBytes, pageNumber, dpi, password, desiredImageFormat, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF (from a byte array) to an in-memory image.
    /// </summary>
    /// <param name="pdfBytes">The PDF file content as a byte array.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="Image"/> of the requested page. The caller is responsible for disposing the image.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<Image> RasterizeToImage(
        byte[] pdfBytes,
        int pageNumber,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        if (pageNumber < 1)
        {
            throw new ArgumentException(
                $"Page number must be at least 1, but was {pageNumber}.",
                nameof(pageNumber));
        }

        if (desiredImageFormat is UnknownImageFormat)
        {
            throw new ArgumentException($"Invalid image format: {nameof(UnknownImageFormat)}", nameof(desiredImageFormat));
        }

        var pdfPtr = default(nint);
        var isLocked = false;

        try
        {
            isLocked = await _pdfiumLocker.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

            if (!isLocked)
            {
                throw new TimeoutException($"Timeout waiting for {nameof(PdfiumEngine)} lock.");
            }

            var effectiveDpi = (dpi is > 0) ? dpi.Value : Dpi;
            var effectivePassword = password ?? Password;
            var scaling = effectiveDpi / 72.0;
            var imageFormat = desiredImageFormat ?? RasterizedImageFormat;

            // Pin the PDF bytes in unmanaged memory for PDFium
            pdfPtr = Marshal.AllocHGlobal(pdfBytes.Length);
            Marshal.Copy(pdfBytes, 0, pdfPtr, pdfBytes.Length);

            var document = PdfiumBindings.FPDF_LoadMemDocument(pdfPtr, pdfBytes.Length, effectivePassword);
            if (document == IntPtr.Zero)
            {
                var error = PdfiumBindings.FPDF_GetLastError();
                throw new InvalidOperationException(
                    $"PDFium failed to load document (error code: {error})");
            }

            try
            {
                var pageCount = PdfiumBindings.FPDF_GetPageCount(document);

                if (pageNumber > pageCount)
                {
                    throw new ArgumentException(
                        $"Page number {pageNumber} exceeds the document's page count of {pageCount}.",
                        nameof(pageNumber));
                }

                var formHandle = IntPtr.Zero;
                var formInfoPtr = IntPtr.Zero;
                var renderAnnotations = renderFlags.HasFlag(PdfRenderFlags.RenderAnnotations);

                if (renderAnnotations)
                {
                    (formHandle, formInfoPtr) = PdfiumEngine.InitFormFillEnvironment(document);
                }

                try
                {
                    _logger($"Rasterizing page {pageNumber} at {effectiveDpi} DPI...");
                    var image = PdfiumEngine.RenderPageToImage(
                        document, pageNumber - 1, scaling, renderFlags, formHandle, imageFormat, BackgroundColor);
                    _logger($"  Page {pageNumber}: rendered to image");
                    return image;
                }
                finally
                {
                    if (formHandle != IntPtr.Zero)
                        PdfiumBindings.FPDFDOC_ExitFormFillEnvironment(formHandle);
                    if (formInfoPtr != IntPtr.Zero)
                        Marshal.FreeHGlobal(formInfoPtr);
                }
            }
            finally
            {
                PdfiumBindings.FPDF_CloseDocument(document);
            }
        }
        finally
        {
            // ReSharper disable once PreferConcreteValueOverDefault
            if (pdfPtr != default)
            {
                Marshal.FreeHGlobal(pdfPtr);
            }

            if (isLocked)
            {
                _pdfiumLocker.Release();
            }
        }
    }

    /// <summary>
    /// Rasterizes pages of a PDF document to image files.
    /// </summary>
    /// <param name="pdfDocument">The PDF document to rasterize.</param>
    /// <param name="outputDirectory">
    /// Directory to write the image files. When <c>null</c>, the <see cref="OutputDirectory"/> property
    /// value is used. If neither provides a directory, an <see cref="ArgumentException"/> is thrown.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToImageFiles(
        PdfDocument pdfDocument,
        string outputDirectory = null,
        int? dpi = null,
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();
        ArgumentNullException.ThrowIfNull(pdfDocument);

        var pdfBytes = SerializePdfDocument(pdfDocument);
        await RasterizeToImageFiles(pdfBytes, outputDirectory, dpi, null, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes pages of a PDF document to in-memory images.
    /// </summary>
    /// <param name="pdfDocument">The PDF document to rasterize.</param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of <see cref="Image"/> objects, one per requested page. The caller is responsible for disposing each image.</returns>
    public async Task<IList<Image>> RasterizeToImages(
        PdfDocument pdfDocument,
        int? dpi = null,
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();
        ArgumentNullException.ThrowIfNull(pdfDocument);

        var pdfBytes = SerializePdfDocument(pdfDocument);
        return await RasterizeToImages(pdfBytes, dpi, null, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF document to an in-memory image.
    /// </summary>
    /// <param name="pdfDocument">The PDF document to rasterize.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="Image"/> of the requested page. The caller is responsible for disposing the image.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<Image> RasterizeToImage(
        PdfDocument pdfDocument,
        int pageNumber,
        int? dpi = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();
        ArgumentNullException.ThrowIfNull(pdfDocument);

        var pdfBytes = SerializePdfDocument(pdfDocument);
        return await RasterizeToImage(pdfBytes, pageNumber, dpi, null, desiredImageFormat, renderFlags, cancellationToken);
    }

    private static byte[] SerializePdfDocument(PdfDocument pdfDocument)
    {
        using var ms = new MemoryStream();
        pdfDocument.Save(ms);
        return ms.ToArray();
    }

    private static async Task<byte[]> ReadStreamToBytesAsync(Stream pdfStream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pdfStream);
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    #region | Stream overloads |

    /// <summary>
    /// Rasterizes pages of a PDF stream to image files.
    /// </summary>
    /// <param name="pdfStream">A readable stream containing the PDF data.</param>
    /// <param name="outputDirectory">
    /// Directory to write the image files. When <c>null</c>, the <see cref="OutputDirectory"/> property
    /// value is used. If neither provides a directory, an <see cref="ArgumentException"/> is thrown.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToImageFiles(
        Stream pdfStream,
        string outputDirectory = null,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await ReadStreamToBytesAsync(pdfStream, cancellationToken);
        await RasterizeToImageFiles(pdfBytes, outputDirectory, dpi, password, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes pages of a PDF stream to in-memory images.
    /// </summary>
    /// <param name="pdfStream">A readable stream containing the PDF data.</param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of <see cref="Image"/> objects, one per requested page. The caller is responsible for disposing each image.</returns>
    public async Task<IList<Image>> RasterizeToImages(
        Stream pdfStream,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await ReadStreamToBytesAsync(pdfStream, cancellationToken);
        return await RasterizeToImages(pdfBytes, dpi, password, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF stream to an in-memory image.
    /// </summary>
    /// <param name="pdfStream">A readable stream containing the PDF data.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An <see cref="Image"/> of the requested page. The caller is responsible for disposing the image.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<Image> RasterizeToImage(
        Stream pdfStream,
        int pageNumber,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await ReadStreamToBytesAsync(pdfStream, cancellationToken);
        return await RasterizeToImage(pdfBytes, pageNumber, dpi, password, desiredImageFormat, renderFlags, cancellationToken);
    }

    #endregion

    #region | RasterizeToImageFile |

    /// <summary>
    /// Rasterizes a single page of a PDF file to an image file.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="outputDirectory">
    /// Directory to write the image file. When <c>null</c>, the <see cref="OutputDirectory"/> property
    /// value is used. If neither provides a directory, an <see cref="ArgumentException"/> is thrown.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToImageFile(
        string pdfPath,
        int pageNumber,
        string outputDirectory = null,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        await RasterizeToImageFile(pdfBytes, pageNumber, outputDirectory, dpi, password, desiredImageFormat, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF (from a byte array) to an image file.
    /// </summary>
    /// <param name="pdfBytes">The PDF file content as a byte array.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="outputDirectory">
    /// Directory to write the image file. When <c>null</c>, the <see cref="OutputDirectory"/> property
    /// value is used. If neither provides a directory, an <see cref="ArgumentException"/> is thrown.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToImageFile(
        byte[] pdfBytes,
        int pageNumber,
        string outputDirectory = null,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        await RasterizeToImageFiles(pdfBytes, outputDirectory, dpi, password, desiredImageFormat, [pageNumber], renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF stream to an image file.
    /// </summary>
    /// <param name="pdfStream">A readable stream containing the PDF data.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="outputDirectory">
    /// Directory to write the image file. When <c>null</c>, the <see cref="OutputDirectory"/> property
    /// value is used. If neither provides a directory, an <see cref="ArgumentException"/> is thrown.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToImageFile(
        Stream pdfStream,
        int pageNumber,
        string outputDirectory = null,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await ReadStreamToBytesAsync(pdfStream, cancellationToken);
        await RasterizeToImageFile(pdfBytes, pageNumber, outputDirectory, dpi, password, desiredImageFormat, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF document to an image file.
    /// </summary>
    /// <param name="pdfDocument">The PDF document to rasterize.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="outputDirectory">
    /// Directory to write the image file. When <c>null</c>, the <see cref="OutputDirectory"/> property
    /// value is used. If neither provides a directory, an <see cref="ArgumentException"/> is thrown.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToImageFile(
        PdfDocument pdfDocument,
        int pageNumber,
        string outputDirectory = null,
        int? dpi = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();
        ArgumentNullException.ThrowIfNull(pdfDocument);

        var pdfBytes = SerializePdfDocument(pdfDocument);
        await RasterizeToImageFile(pdfBytes, pageNumber, outputDirectory, dpi, null, desiredImageFormat, renderFlags, cancellationToken);
    }

    #endregion

    #region | IDisposable implementation |

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
        _logger = _ => { };
    }

    #endregion
}
