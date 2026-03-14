using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfRasterizer.Pdfium;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace CodeBrix.PdfRasterizer;

/// <summary>
/// Represents the dimensions of a single PDF page in points (1/72 inch).
/// </summary>
/// <param name="WidthInPoints">Page width in points.</param>
/// <param name="HeightInPoints">Page height in points.</param>
public record PdfPageDimensions(double WidthInPoints, double HeightInPoints)
{
    /// <summary>Page width in inches.</summary>
    public double WidthInInches => WidthInPoints / 72.0;

    /// <summary>Page height in inches.</summary>
    public double HeightInInches => HeightInPoints / 72.0;

    /// <summary>Page width in pixels at the specified DPI.</summary>
    public int GetWidthInPixels(int dpi) => (int)(WidthInPoints * dpi / 72.0);

    /// <summary>Page height in pixels at the specified DPI.</summary>
    public int GetHeightInPixels(int dpi) => (int)(HeightInPoints * dpi / 72.0);
}

public sealed partial class PageRasterizer
{
    #region | GetPageCount |

    /// <summary>
    /// Returns the number of pages in a PDF file.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of pages in the document.</returns>
    public async Task<int> GetPageCount(
        string pdfPath,
        string password = null,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        return await GetPageCount(pdfBytes, password, cancellationToken);
    }

    /// <summary>
    /// Returns the number of pages in a PDF byte array.
    /// </summary>
    /// <param name="pdfBytes">The PDF file content as a byte array.</param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of pages in the document.</returns>
    public async Task<int> GetPageCount(
        byte[] pdfBytes,
        string password = null,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfPtr = default(nint);
        var isLocked = false;

        try
        {
            isLocked = await _pdfiumLocker.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

            if (!isLocked)
            {
                throw new TimeoutException($"Timeout waiting for {nameof(PdfiumEngine)} lock.");
            }

            var effectivePassword = password ?? Password;

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
                return PdfiumBindings.FPDF_GetPageCount(document);
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
    /// Returns the number of pages in a PDF stream.
    /// </summary>
    /// <param name="pdfStream">A readable stream containing the PDF data.</param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of pages in the document.</returns>
    public async Task<int> GetPageCount(
        Stream pdfStream,
        string password = null,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await ReadStreamToBytesAsync(pdfStream, cancellationToken);
        return await GetPageCount(pdfBytes, password, cancellationToken);
    }

    /// <summary>
    /// Returns the number of pages in a PDF document.
    /// </summary>
    /// <param name="pdfDocument">The PDF document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of pages in the document.</returns>
    public async Task<int> GetPageCount(
        PdfDocument pdfDocument,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();
        ArgumentNullException.ThrowIfNull(pdfDocument);

        var pdfBytes = SerializePdfDocument(pdfDocument);
        return await GetPageCount(pdfBytes, null, cancellationToken);
    }

    #endregion

    #region | GetPageDimensions |

    /// <summary>
    /// Returns the dimensions of a single page in a PDF file.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page dimensions in points.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<PdfPageDimensions> GetPageDimensions(
        string pdfPath,
        int pageNumber,
        string password = null,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await File.ReadAllBytesAsync(pdfPath, cancellationToken);
        return await GetPageDimensions(pdfBytes, pageNumber, password, cancellationToken);
    }

    /// <summary>
    /// Returns the dimensions of a single page in a PDF byte array.
    /// </summary>
    /// <param name="pdfBytes">The PDF file content as a byte array.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page dimensions in points.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<PdfPageDimensions> GetPageDimensions(
        byte[] pdfBytes,
        int pageNumber,
        string password = null,
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

        var pdfPtr = default(nint);
        var isLocked = false;

        try
        {
            isLocked = await _pdfiumLocker.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

            if (!isLocked)
            {
                throw new TimeoutException($"Timeout waiting for {nameof(PdfiumEngine)} lock.");
            }

            var effectivePassword = password ?? Password;

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

                var page = PdfiumBindings.FPDF_LoadPage(document, pageNumber - 1);
                if (page == IntPtr.Zero)
                    throw new InvalidOperationException($"Failed to load page {pageNumber - 1}");

                try
                {
                    var width = PdfiumBindings.FPDF_GetPageWidth(page);
                    var height = PdfiumBindings.FPDF_GetPageHeight(page);
                    return new PdfPageDimensions(width, height);
                }
                finally
                {
                    PdfiumBindings.FPDF_ClosePage(page);
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
    /// Returns the dimensions of a single page in a PDF stream.
    /// </summary>
    /// <param name="pdfStream">A readable stream containing the PDF data.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="password">
    /// Password for encrypted PDFs. When <c>null</c>, the <see cref="Password"/> property value is used.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page dimensions in points.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<PdfPageDimensions> GetPageDimensions(
        Stream pdfStream,
        int pageNumber,
        string password = null,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();

        var pdfBytes = await ReadStreamToBytesAsync(pdfStream, cancellationToken);
        return await GetPageDimensions(pdfBytes, pageNumber, password, cancellationToken);
    }

    /// <summary>
    /// Returns the dimensions of a single page in a PDF document.
    /// </summary>
    /// <param name="pdfDocument">The PDF document.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page dimensions in points.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pageNumber"/> is less than 1 or greater than the number of pages in the document.</exception>
    public async Task<PdfPageDimensions> GetPageDimensions(
        PdfDocument pdfDocument,
        int pageNumber,
        CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        CheckPdfiumEngineIsInitialized();
        ArgumentNullException.ThrowIfNull(pdfDocument);

        var pdfBytes = SerializePdfDocument(pdfDocument);
        return await GetPageDimensions(pdfBytes, pageNumber, null, cancellationToken);
    }

    #endregion
}
