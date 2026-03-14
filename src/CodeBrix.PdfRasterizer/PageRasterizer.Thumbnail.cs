using CodeBrix.Imaging;
using CodeBrix.Imaging.Formats;
using CodeBrix.Imaging.Processing;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfRasterizer.Pdfium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CodeBrix.PdfRasterizer;

/// <summary>
/// Specifies the maximum pixel dimensions for a thumbnail image.
/// The actual thumbnail size preserves the source aspect ratio while fitting
/// within a bounding box of <see cref="MaxHorizontalPixels"/> × <see cref="MaxVerticalPixels"/>.
/// </summary>
/// <param name="MaxHorizontalPixels">Maximum width in pixels. Must be at least 1.</param>
/// <param name="MaxVerticalPixels">Maximum height in pixels. Must be at least 1.</param>
public record ThumbnailMaxDimensions(int MaxHorizontalPixels, int MaxVerticalPixels);

public sealed partial class PageRasterizer
{
    private const int DefaultMaxHorizontalPixels = 200;
    private const int DefaultMaxVerticalPixels = 260;

    private static void ValidateMaxDimensions(ThumbnailMaxDimensions maxDimensions)
    {
        ArgumentNullException.ThrowIfNull(maxDimensions);
        if (maxDimensions.MaxHorizontalPixels < 1)
            throw new ArgumentException(
                $"{nameof(ThumbnailMaxDimensions.MaxHorizontalPixels)} must be at least 1, but was {maxDimensions.MaxHorizontalPixels}.",
                nameof(maxDimensions));
        if (maxDimensions.MaxVerticalPixels < 1)
            throw new ArgumentException(
                $"{nameof(ThumbnailMaxDimensions.MaxVerticalPixels)} must be at least 1, but was {maxDimensions.MaxVerticalPixels}.",
                nameof(maxDimensions));
    }

    private static void ResizeToThumbnail(Image image, ThumbnailMaxDimensions maxDimensions)
    {
        var scaleX = (double)maxDimensions.MaxHorizontalPixels / image.Width;
        var scaleY = (double)maxDimensions.MaxVerticalPixels / image.Height;
        var scale = Math.Min(scaleX, scaleY);

        // Only downscale; if the image already fits within the bounding box, leave it as-is.
        if (scale >= 1.0) return;

        var width = Math.Max(1, (int)(image.Width * scale));
        var height = Math.Max(1, (int)(image.Height * scale));
        image.Mutate(x => x.Resize(width, height));
    }

    /// <summary>
    /// Default thumbnail bounding box: 200×260 pixels, sized for an 8.5×11 inch page at
    /// a comfortable preview size while preserving the portrait aspect ratio.
    /// </summary>
    private static readonly ThumbnailMaxDimensions DefaultThumbnailMaxDimensions = new(DefaultMaxHorizontalPixels, DefaultMaxVerticalPixels);

    /// <summary>
    /// Gets or sets the default maximum dimensions for thumbnail images.
    /// Used when <c>maxDimensions</c> is not specified (or is <c>null</c>) in a call
    /// to a thumbnail rasterization method. Setting to <c>null</c> reverts to the
    /// built-in default (200×260 pixels).
    /// </summary>
    public ThumbnailMaxDimensions ThumbnailMaxDimensions
    {
        get;
        set
        {
            if (value is not null) ValidateMaxDimensions(value);
            field = value;
        }
    }

    private ThumbnailMaxDimensions ResolveMaxDimensions(ThumbnailMaxDimensions maxDimensions)
    {
        if (maxDimensions is not null)
        {
            ValidateMaxDimensions(maxDimensions);
            return maxDimensions;
        }
        return ThumbnailMaxDimensions ?? DefaultThumbnailMaxDimensions;
    }

    #region | RasterizeToThumbnailFiles |

    /// <summary>
    /// Rasterizes pages of a PDF file to thumbnail image files.
    /// Each thumbnail preserves the page's aspect ratio while fitting within the specified
    /// <paramref name="maxDimensions"/> bounding box.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="outputDirectory">
    /// Directory to write the thumbnail files. When <c>null</c>, the <see cref="OutputDirectory"/> property
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
    /// The desired image format of the thumbnail files to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToThumbnailFiles(
        string pdfPath,
        ThumbnailMaxDimensions maxDimensions = null,
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
        await RasterizeToThumbnailFiles(pdfBytes, maxDimensions, outputDirectory, dpi, password, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes pages of a PDF (from a byte array) to thumbnail image files.
    /// Each thumbnail preserves the page's aspect ratio while fitting within the specified
    /// <paramref name="maxDimensions"/> bounding box.
    /// </summary>
    /// <param name="pdfBytes">The PDF file content as a byte array.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="outputDirectory">
    /// Directory to write the thumbnail files. When <c>null</c>, the <see cref="OutputDirectory"/> property
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
    /// The desired image format of the thumbnail files to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToThumbnailFiles(
        byte[] pdfBytes,
        ThumbnailMaxDimensions maxDimensions = null,
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

        var effectiveMaxDimensions = ResolveMaxDimensions(maxDimensions);

        if (desiredImageFormat is UnknownImageFormat)
        {
            throw new ArgumentException($"Invalid image format: {nameof(UnknownImageFormat)}", nameof(desiredImageFormat));
        }

        var effectiveDirectory = outputDirectory ?? OutputDirectory
            ?? throw new ArgumentException(
                $"No output directory specified. Either pass a value for '{nameof(outputDirectory)}' " +
                $"or set the '{nameof(OutputDirectory)}' property.",
                nameof(outputDirectory));

        if (!Directory.Exists(effectiveDirectory)) { Directory.CreateDirectory(effectiveDirectory); }

        var imageFormat = desiredImageFormat ?? RasterizedImageFormat;
        var fileExtension = imageFormat.DefaultFileExtension;

        var images = await RasterizeToImages(pdfBytes, dpi, password, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);

        try
        {
            _logger($"Creating thumbnails (max {effectiveMaxDimensions.MaxHorizontalPixels}x{effectiveMaxDimensions.MaxVerticalPixels}) for {images.Count} pages...");

            for (var i = 0; i < images.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pageNumber = pageNumbers is not null ? pageNumbers[i] : i + 1;
                using var image = images[i];
                ResizeToThumbnail(image, effectiveMaxDimensions);

                var fileName = $"{FileNameGenerator(pageNumber)}.{fileExtension}";
                var pagePath = Path.Combine(effectiveDirectory, fileName);

                if (!AllowOverwriteFiles && File.Exists(pagePath))
                    throw new IOException(
                        $"Output file already exists and {nameof(AllowOverwriteFiles)} is false: {pagePath}");

                await using var fs = new FileStream(pagePath, FileMode.Create, FileAccess.Write);
                await image.SaveAsync(fs, imageFormat, cancellationToken);
                _logger($"  Page {pageNumber}: {pagePath} ({image.Width}x{image.Height})");
            }

            _logger($"Created {images.Count} thumbnail files.");
        }
        finally
        {
            foreach (var image in images) { image.Dispose(); }
        }
    }

    /// <summary>
    /// Rasterizes pages of a PDF document to thumbnail image files.
    /// Each thumbnail preserves the page's aspect ratio while fitting within the specified
    /// <paramref name="maxDimensions"/> bounding box.
    /// </summary>
    /// <param name="pdfDocument">The PDF document to rasterize.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="outputDirectory">
    /// Directory to write the thumbnail files. When <c>null</c>, the <see cref="OutputDirectory"/> property
    /// value is used. If neither provides a directory, an <see cref="ArgumentException"/> is thrown.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the thumbnail files to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToThumbnailFiles(
        PdfDocument pdfDocument,
        ThumbnailMaxDimensions maxDimensions = null,
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
        await RasterizeToThumbnailFiles(pdfBytes, maxDimensions, outputDirectory, dpi, null, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    #endregion

    #region | RasterizeToThumbnails |

    /// <summary>
    /// Rasterizes pages of a PDF file to in-memory thumbnail images.
    /// Each thumbnail preserves the page's aspect ratio while fitting within the specified
    /// <paramref name="maxDimensions"/> bounding box.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the thumbnail images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of thumbnail <see cref="Image"/> objects, one per requested page. The caller is responsible for disposing each image.</returns>
    public async Task<IList<Image>> RasterizeToThumbnails(
        string pdfPath,
        ThumbnailMaxDimensions maxDimensions = null,
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
        return await RasterizeToThumbnails(pdfBytes, maxDimensions, dpi, password, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes pages of a PDF (from a byte array) to in-memory thumbnail images.
    /// Each thumbnail preserves the page's aspect ratio while fitting within the specified
    /// <paramref name="maxDimensions"/> bounding box.
    /// </summary>
    /// <param name="pdfBytes">The PDF file content as a byte array.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the thumbnail images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of thumbnail <see cref="Image"/> objects, one per requested page. The caller is responsible for disposing each image.</returns>
    public async Task<IList<Image>> RasterizeToThumbnails(
        byte[] pdfBytes,
        ThumbnailMaxDimensions maxDimensions = null,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        IReadOnlyList<int> pageNumbers = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var effectiveMaxDimensions = ResolveMaxDimensions(maxDimensions);

        var images = await RasterizeToImages(pdfBytes, dpi, password, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);

        try
        {
            _logger($"Resizing {images.Count} pages to thumbnails (max {effectiveMaxDimensions.MaxHorizontalPixels}x{effectiveMaxDimensions.MaxVerticalPixels})...");

            for (var i = 0; i < images.Count; i++)
            {
                ResizeToThumbnail(images[i], effectiveMaxDimensions);
                var pageNumber = pageNumbers is not null ? pageNumbers[i] : i + 1;
                _logger($"  Page {pageNumber}: thumbnail {images[i].Width}x{images[i].Height}");
            }

            _logger($"Created {images.Count} thumbnails.");
            return images;
        }
        catch
        {
            foreach (var image in images) { image.Dispose(); }
            throw;
        }
    }

    /// <summary>
    /// Rasterizes pages of a PDF document to in-memory thumbnail images.
    /// Each thumbnail preserves the page's aspect ratio while fitting within the specified
    /// <paramref name="maxDimensions"/> bounding box.
    /// </summary>
    /// <param name="pdfDocument">The PDF document to rasterize.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the thumbnail images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of thumbnail <see cref="Image"/> objects, one per requested page. The caller is responsible for disposing each image.</returns>
    public async Task<IList<Image>> RasterizeToThumbnails(
        PdfDocument pdfDocument,
        ThumbnailMaxDimensions maxDimensions = null,
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
        return await RasterizeToThumbnails(pdfBytes, maxDimensions, dpi, null, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    #endregion

    #region | RasterizeToThumbnail |

    /// <summary>
    /// Rasterizes a single page of a PDF file to an in-memory thumbnail image.
    /// The thumbnail preserves the page's aspect ratio while fitting within the specified
    /// <paramref name="maxDimensions"/> bounding box.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the thumbnail image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A thumbnail <see cref="Image"/> of the requested page. The caller is responsible for disposing the image.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<Image> RasterizeToThumbnail(
        string pdfPath,
        int pageNumber,
        ThumbnailMaxDimensions maxDimensions = null,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        return await RasterizeToThumbnail(pdfBytes, pageNumber, maxDimensions, dpi, password, desiredImageFormat, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF (from a byte array) to an in-memory thumbnail image.
    /// The thumbnail preserves the page's aspect ratio while fitting within the specified
    /// <paramref name="maxDimensions"/> bounding box.
    /// </summary>
    /// <param name="pdfBytes">The PDF file content as a byte array.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the thumbnail image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A thumbnail <see cref="Image"/> of the requested page. The caller is responsible for disposing the image.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<Image> RasterizeToThumbnail(
        byte[] pdfBytes,
        int pageNumber,
        ThumbnailMaxDimensions maxDimensions = null,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var effectiveMaxDimensions = ResolveMaxDimensions(maxDimensions);

        var image = await RasterizeToImage(pdfBytes, pageNumber, dpi, password, desiredImageFormat, renderFlags, cancellationToken);

        try
        {
            ResizeToThumbnail(image, effectiveMaxDimensions);
            _logger($"  Page {pageNumber}: thumbnail {image.Width}x{image.Height}");
            return image;
        }
        catch
        {
            image.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Rasterizes a single page of a PDF document to an in-memory thumbnail image.
    /// The thumbnail preserves the page's aspect ratio while fitting within the specified
    /// <paramref name="maxDimensions"/> bounding box.
    /// </summary>
    /// <param name="pdfDocument">The PDF document to rasterize.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the thumbnail image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A thumbnail <see cref="Image"/> of the requested page. The caller is responsible for disposing the image.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<Image> RasterizeToThumbnail(
        PdfDocument pdfDocument,
        int pageNumber,
        ThumbnailMaxDimensions maxDimensions = null,
        int? dpi = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();
        ArgumentNullException.ThrowIfNull(pdfDocument);

        var pdfBytes = SerializePdfDocument(pdfDocument);
        return await RasterizeToThumbnail(pdfBytes, pageNumber, maxDimensions, dpi, null, desiredImageFormat, renderFlags, cancellationToken);
    }

    #endregion

    #region | Stream thumbnail overloads |

    /// <summary>
    /// Rasterizes pages of a PDF stream to thumbnail image files.
    /// </summary>
    /// <param name="pdfStream">A readable stream containing the PDF data.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="outputDirectory">
    /// Directory to write the thumbnail files. When <c>null</c>, the <see cref="OutputDirectory"/> property
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
    /// The desired image format of the thumbnail files to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToThumbnailFiles(
        Stream pdfStream,
        ThumbnailMaxDimensions maxDimensions = null,
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
        await RasterizeToThumbnailFiles(pdfBytes, maxDimensions, outputDirectory, dpi, password, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes pages of a PDF stream to in-memory thumbnail images.
    /// </summary>
    /// <param name="pdfStream">A readable stream containing the PDF data.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the thumbnail images to be created.
    /// </param>
    /// <param name="pageNumbers">
    /// Optional 1-based page numbers to rasterize. When <c>null</c>, all pages are rasterized.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of thumbnail <see cref="Image"/> objects, one per requested page. The caller is responsible for disposing each image.</returns>
    public async Task<IList<Image>> RasterizeToThumbnails(
        Stream pdfStream,
        ThumbnailMaxDimensions maxDimensions = null,
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
        return await RasterizeToThumbnails(pdfBytes, maxDimensions, dpi, password, desiredImageFormat, pageNumbers, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF stream to an in-memory thumbnail image.
    /// </summary>
    /// <param name="pdfStream">A readable stream containing the PDF data.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the thumbnail image to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A thumbnail <see cref="Image"/> of the requested page. The caller is responsible for disposing the image.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<Image> RasterizeToThumbnail(
        Stream pdfStream,
        int pageNumber,
        ThumbnailMaxDimensions maxDimensions = null,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await ReadStreamToBytesAsync(pdfStream, cancellationToken);
        return await RasterizeToThumbnail(pdfBytes, pageNumber, maxDimensions, dpi, password, desiredImageFormat, renderFlags, cancellationToken);
    }

    #endregion

    #region | RasterizeToThumbnailFile |

    /// <summary>
    /// Rasterizes a single page of a PDF file to a thumbnail image file.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="outputDirectory">
    /// Directory to write the thumbnail file. When <c>null</c>, the <see cref="OutputDirectory"/> property
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
    /// The desired image format of the thumbnail file to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToThumbnailFile(
        string pdfPath,
        int pageNumber,
        ThumbnailMaxDimensions maxDimensions = null,
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
        await RasterizeToThumbnailFile(pdfBytes, pageNumber, maxDimensions, outputDirectory, dpi, password, desiredImageFormat, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF (from a byte array) to a thumbnail image file.
    /// </summary>
    /// <param name="pdfBytes">The PDF file content as a byte array.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="outputDirectory">
    /// Directory to write the thumbnail file. When <c>null</c>, the <see cref="OutputDirectory"/> property
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
    /// The desired image format of the thumbnail file to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToThumbnailFile(
        byte[] pdfBytes,
        int pageNumber,
        ThumbnailMaxDimensions maxDimensions = null,
        string outputDirectory = null,
        int? dpi = null,
        string password = null,
        IImageFormat desiredImageFormat = null,
        PdfRenderFlags renderFlags = PdfRenderFlags.RenderAnnotations | PdfRenderFlags.RenderForPrinting,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        await RasterizeToThumbnailFiles(pdfBytes, maxDimensions, outputDirectory, dpi, password, desiredImageFormat, [pageNumber], renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF stream to a thumbnail image file.
    /// </summary>
    /// <param name="pdfStream">A readable stream containing the PDF data.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="outputDirectory">
    /// Directory to write the thumbnail file. When <c>null</c>, the <see cref="OutputDirectory"/> property
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
    /// The desired image format of the thumbnail file to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToThumbnailFile(
        Stream pdfStream,
        int pageNumber,
        ThumbnailMaxDimensions maxDimensions = null,
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
        await RasterizeToThumbnailFile(pdfBytes, pageNumber, maxDimensions, outputDirectory, dpi, password, desiredImageFormat, renderFlags, cancellationToken);
    }

    /// <summary>
    /// Rasterizes a single page of a PDF document to a thumbnail image file.
    /// </summary>
    /// <param name="pdfDocument">The PDF document to rasterize.</param>
    /// <param name="pageNumber">The 1-based page number to rasterize.</param>
    /// <param name="maxDimensions">
    /// Maximum thumbnail dimensions. When <c>null</c>, the <see cref="ThumbnailMaxDimensions"/> property
    /// value is used. If neither provides dimensions, a built-in default of 200×260 pixels is used.
    /// </param>
    /// <param name="outputDirectory">
    /// Directory to write the thumbnail file. When <c>null</c>, the <see cref="OutputDirectory"/> property
    /// value is used. If neither provides a directory, an <see cref="ArgumentException"/> is thrown.
    /// </param>
    /// <param name="dpi">
    /// Rendering resolution in dots per inch. When <c>null</c> or less than 1,
    /// the <see cref="Dpi"/> property value is used.
    /// </param>
    /// <param name="desiredImageFormat">
    /// The desired image format of the thumbnail file to be created.
    /// </param>
    /// <param name="renderFlags">
    /// Render flags controlling output quality and features.
    /// Defaults to RenderAnnotations | RenderForPrinting.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task RasterizeToThumbnailFile(
        PdfDocument pdfDocument,
        int pageNumber,
        ThumbnailMaxDimensions maxDimensions = null,
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
        await RasterizeToThumbnailFile(pdfBytes, pageNumber, maxDimensions, outputDirectory, dpi, null, desiredImageFormat, renderFlags, cancellationToken);
    }

    #endregion
}
