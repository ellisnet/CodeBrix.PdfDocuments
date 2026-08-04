using System;
using System.Collections.Generic;

namespace CodeBrix.PdfDocCreate.Html2Pdf;

/// <summary>
/// Collects the non-fatal issues encountered while rendering - unsupported CSS,
/// missing images, unresolvable fonts, skipped elements - so authors can inspect what
/// did not apply and adjust. Duplicate messages are collapsed.
/// </summary>
public sealed class RenderWarnings
{
    internal const string CategoryCss = "css";
    internal const string CategoryImage = "image";
    internal const string CategoryFont = "font";
    internal const string CategoryHtml = "html";

    private readonly List<string> _messages = new List<string>();
    private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
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

    internal void Add(string category, string message)
    {
        var formatted = $"[{category}] {message}";
        lock (_sync)
        {
            if (_seen.Add(formatted)) { _messages.Add(formatted); }
        }
    }
}
