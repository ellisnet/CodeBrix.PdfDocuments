using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeBrix.PdfDocCreate.Html2Pdf;

/// <summary>Category of a collected rendering warning.</summary>
public enum RenderWarningCategory
{
    /// <summary>Unsupported or invalid CSS.</summary>
    Css,

    /// <summary>An image that could not be resolved, decoded or rendered.</summary>
    Image,

    /// <summary>Font resolution or glyph-coverage issues.</summary>
    Font,

    /// <summary>Unsupported HTML constructs.</summary>
    Html,
}

/// <summary>
/// One structured rendering warning. The stable <see cref="Code"/> and the payload
/// properties exist so test baselines can assert on warnings without string-matching
/// display prose.
/// </summary>
public sealed class RenderWarning
{
    internal RenderWarning(RenderWarningCategory category, string code, string message, int occurrences, int? codePoint)
    {
        Category = category;
        Code = code;
        Message = message;
        Occurrences = occurrences;
        CodePoint = codePoint;
    }

    /// <summary>The warning's category.</summary>
    public RenderWarningCategory Category { get; }

    /// <summary>
    /// Stable machine-readable code, e.g. "font.uncovered.removed" or
    /// "image.format.unsupported". Codes are part of the library's compatibility
    /// surface; display prose is not.
    /// </summary>
    public string Code { get; }

    /// <summary>The display message, identical to the corresponding Messages entry.</summary>
    public string Message { get; }

    /// <summary>How many times this exact warning was raised during the render.</summary>
    public int Occurrences { get; }

    /// <summary>The Unicode code point involved, for glyph-coverage warnings; else null.</summary>
    public int? CodePoint { get; }
}

/// <summary>
/// Collects the non-fatal issues encountered while rendering - unsupported CSS,
/// missing images, unresolvable fonts, skipped elements - so authors can inspect what
/// did not apply and adjust. Duplicate messages are collapsed in <see cref="Messages"/>;
/// the structured <see cref="Items"/> view keeps one entry per distinct
/// (code, code point, message) with an occurrence count.
/// </summary>
public sealed class RenderWarnings
{
    internal const string CategoryCss = "css";
    internal const string CategoryImage = "image";
    internal const string CategoryFont = "font";
    internal const string CategoryHtml = "html";

    private sealed class Entry
    {
        public RenderWarningCategory Category;
        public string Code;
        public string Message;
        public int Count;
        public int? CodePoint;
    }

    private readonly List<string> _messages = new List<string>();
    private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<Entry> _entries = new List<Entry>();
    private readonly Dictionary<string, Entry> _entryIndex = new Dictionary<string, Entry>(StringComparer.Ordinal);
    private readonly object _sync = new object();

    /// <summary>The collected warning messages, in first-occurrence order.</summary>
    public IReadOnlyList<string> Messages
    {
        get { lock (_sync) { return _messages.ToArray(); } }
    }

    /// <summary>The number of distinct warnings collected.</summary>
    public int Count
    {
        get { lock (_sync) { return _messages.Count; } }
    }

    /// <summary>
    /// The structured warnings, in first-occurrence order. Finer-grained than
    /// <see cref="Messages"/>: warnings that share one display message but concern
    /// different code points (e.g. several distinct characters outside the Basic
    /// Multilingual Plane) appear as separate items.
    /// </summary>
    public IReadOnlyList<RenderWarning> Items
    {
        get
        {
            lock (_sync)
            {
                return _entries
                    .Select(e => new RenderWarning(e.Category, e.Code, e.Message, e.Count, e.CodePoint))
                    .ToArray();
            }
        }
    }

    internal void Add(string category, string message, string code = null, int? codePoint = null)
    {
        var formatted = $"[{category}] {message}";
        var effectiveCode = code ?? category;
        var key = effectiveCode + "\u0001" + (codePoint?.ToString() ?? "") + "\u0001" + formatted;
        lock (_sync)
        {
            if (_seen.Add(formatted)) { _messages.Add(formatted); }

            if (_entryIndex.TryGetValue(key, out var entry))
            {
                entry.Count++;
            }
            else
            {
                entry = new Entry
                {
                    Category = MapCategory(category),
                    Code = effectiveCode,
                    Message = formatted,
                    Count = 1,
                    CodePoint = codePoint,
                };
                _entryIndex.Add(key, entry);
                _entries.Add(entry);
            }
        }
    }

    private static RenderWarningCategory MapCategory(string category) => category switch
    {
        CategoryCss => RenderWarningCategory.Css,
        CategoryImage => RenderWarningCategory.Image,
        CategoryFont => RenderWarningCategory.Font,
        _ => RenderWarningCategory.Html,
    };
}
