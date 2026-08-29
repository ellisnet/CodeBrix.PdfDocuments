// ============================================================================
// PDFium P/Invoke bindings for PDF page rasterization.
//
// Derived from Docnet.Core (https://github.com/GowenGit/docnet)
// Original copyright (c) 2018 Modestas Petravicius, MIT License.
//
// The original Docnet.Core library uses CppSharp-generated wrappers around
// PDFium. This file is a simplified, hand-written version that exposes only
// the PDFium functions needed for rendering PDF pages to bitmaps.
//
// PDFium itself is copyright 2014 The PDFium Authors, BSD License.
// See runtimes/*/native/LICENSE-Pdfium.txt for the full PDFium license text.
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace CodeBrix.PdfRasterizer.Pdfium;

/// <summary>
/// Low-level P/Invoke declarations for the PDFium native library.
/// PDFium is NOT thread-safe — all calls must be serialized externally.
/// </summary>
internal static class PdfiumBindings
{
    private const string PdfiumLib = "pdfium";

    // ── Library lifecycle ────────────────────────────────────────────

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDF_InitLibrary();

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDF_DestroyLibrary();

    // ── Document operations ──────────────────────────────────────────

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr FPDF_LoadMemDocument(
        IntPtr data_buf, int size, [MarshalAs(UnmanagedType.LPStr)] string password);

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDF_CloseDocument(IntPtr document);

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint FPDF_GetLastError();

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int FPDF_GetPageCount(IntPtr document);

    // ── Page operations ──────────────────────────────────────────────

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr FPDF_LoadPage(IntPtr document, int page_index);

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDF_ClosePage(IntPtr page);

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern double FPDF_GetPageWidth(IntPtr page);

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern double FPDF_GetPageHeight(IntPtr page);

    /// <summary>
    /// Returns non-zero if the page has transparency.
    /// Derived from Docnet.Core's FPDFPageHasTransparency binding.
    /// </summary>
    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "FPDFPage_HasTransparency")]
    internal static extern int FPDFPage_HasTransparency(IntPtr page);

    // ── Bitmap operations ────────────────────────────────────────────

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "FPDFBitmap_Create")]
    internal static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "FPDFBitmap_GetStride")]
    internal static extern int FPDFBitmap_GetStride(IntPtr bitmap);

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "FPDFBitmap_GetBuffer")]
    internal static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "FPDFBitmap_Destroy")]
    internal static extern void FPDFBitmap_Destroy(IntPtr bitmap);

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "FPDFBitmap_FillRect")]
    internal static extern void FPDFBitmap_FillRect(
        IntPtr bitmap, int left, int top, int width, int height, uint color);

    // ── Rendering ────────────────────────────────────────────────────

    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDF_RenderPageBitmap(
        IntPtr bitmap, IntPtr page, int start_x, int start_y,
        int size_x, int size_y, int rotate, int flags);

    /// <summary>
    /// Renders a page using a transformation matrix and clipping rectangle.
    /// Derived from Docnet.Core's FPDF_RenderPageBitmapWithMatrix binding.
    /// This provides more flexible rendering than FPDF_RenderPageBitmap,
    /// supporting arbitrary rotation and scaling via a 2D affine matrix.
    /// </summary>
    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "FPDF_RenderPageBitmapWithMatrix")]
    internal static extern void FPDF_RenderPageBitmapWithMatrix(
        IntPtr bitmap, IntPtr page, ref FsMatrix matrix, ref FsRectF clipping, int flags);

    // ── Form fill ────────────────────────────────────────────────────
    // Form fill support derived from Docnet.Core's FormWrapper and
    // PdfiumWrapper bindings. Required to render fillable form fields
    // (text inputs, checkboxes, dropdowns) that appear on PDF pages.

    /// <summary>
    /// Initializes the form fill environment for a document.
    /// The formInfo parameter must point to an allocated FPDF_FORMFILLINFO structure.
    /// </summary>
    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "FPDFDOC_InitFormFillEnvironment")]
    internal static extern IntPtr FPDFDOC_InitFormFillEnvironment(IntPtr document, IntPtr formInfo);

    /// <summary>
    /// Releases the form fill environment handle.
    /// </summary>
    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "FPDFDOC_ExitFormFillEnvironment")]
    internal static extern void FPDFDOC_ExitFormFillEnvironment(IntPtr formHandle);

    /// <summary>
    /// Renders form field annotations on top of a previously rendered page bitmap.
    /// Must be called after FPDF_RenderPageBitmap / FPDF_RenderPageBitmapWithMatrix.
    /// </summary>
    [SuppressUnmanagedCodeSecurity, DllImport(PdfiumLib, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "FPDF_FFLDraw")]
    internal static extern void FPDF_FFLDraw(
        IntPtr formHandle, IntPtr bitmap, IntPtr page,
        int start_x, int start_y, int size_x, int size_y,
        int rotate, int flags);
}

/// <summary>
/// 2D affine transformation matrix for FPDF_RenderPageBitmapWithMatrix.
/// Layout: | a b 0 |
///         | c d 0 |
///         | e f 1 |
/// Derived from Docnet.Core's FS_MATRIX_ (MIT License, copyright 2018 Modestas Petravicius).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FsMatrix
{
    public float A;
    public float B;
    public float C;
    public float D;
    public float E;
    public float F;
}

/// <summary>
/// Floating-point rectangle for clipping in FPDF_RenderPageBitmapWithMatrix.
/// Derived from Docnet.Core's FS_RECTF_ (MIT License, copyright 2018 Modestas Petravicius).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FsRectF
{
    public float Left;
    public float Top;
    public float Right;
    public float Bottom;
}

/// <summary>
/// FPDF_FORMFILLINFO structure for FPDFDOC_InitFormFillEnvironment.
/// All callback pointers are set to IntPtr.Zero (not needed for rendering).
/// Derived from Docnet.Core's FPDF_FORMFILLINFO (MIT License, copyright 2018 Modestas Petravicius).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct FpdfFormFillInfo
{
    public int Version;
    // Callback function pointers — all set to IntPtr.Zero for rendering-only use.
    // PDFium requires these fields to exist in the struct layout but does not
    // require them to be non-null for basic form rendering via FPDF_FFLDraw.
    private IntPtr Release;
    private IntPtr FFI_Invalidate;
    private IntPtr FFI_OutputSelectedRect;
    private IntPtr FFI_SetCursor;
    private IntPtr FFI_SetTimer;
    private IntPtr FFI_KillTimer;
    private IntPtr FFI_GetLocalTime;
    private IntPtr FFI_OnChange;
    private IntPtr FFI_GetPage;
    private IntPtr FFI_GetCurrentPage;
    private IntPtr FFI_GetRotation;
    private IntPtr FFI_ExecuteNamedAction;
    private IntPtr FFI_SetTextFieldFocus;
    private IntPtr FFI_DoURIAction;
    private IntPtr FFI_DoGoToAction;
    private IntPtr m_pJsPlatform;
    // XFA support fields (version 2)
    private IntPtr FFI_DisplayCaret;
    private IntPtr FFI_GetCurrentPageIndex;
    private IntPtr FFI_SetCurrentPage;
    private IntPtr FFI_GotoURL;
    private IntPtr FFI_GetPageViewRect;
    private IntPtr FFI_PageEvent;
    private IntPtr FFI_PopupMenu;
    private IntPtr FFI_OpenFile;
    private IntPtr FFI_EmailTo;
    private IntPtr FFI_UploadTo;
    private IntPtr FFI_GetPlatform;
    private IntPtr FFI_GetLanguage;
    private IntPtr FFI_DownloadFromURL;
    private IntPtr FFI_PostRequestURL;
    private IntPtr FFI_PutRequestURL;
}

/// <summary>
/// Render flags for FPDF_RenderPageBitmap and FPDF_RenderPageBitmapWithMatrix.
/// Values from Docnet.Core (MIT License, copyright 2018 Modestas Petravicius).
/// </summary>
[Flags]
public enum PdfRenderFlags
{
    None = 0x00,
    RenderAnnotations = 0x01,
    OptimizeTextForLcd = 0x02,
    NoNativeText = 0x04,
    Grayscale = 0x08,
    LimitImageCacheSize = 0x200,
    ForceHalftone = 0x400,
    RenderForPrinting = 0x800,
    DisableTextAntialiasing = 0x1000,
    DisableImageAntialiasing = 0x2000,
    DisablePathAntialiasing = 0x4000,
}
