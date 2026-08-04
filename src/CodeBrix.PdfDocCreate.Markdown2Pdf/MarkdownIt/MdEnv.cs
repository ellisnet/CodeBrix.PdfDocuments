// ============================================================================
// Support types for the C# port of markdown-it v14.1.0 (the JS original passes
// plain objects; these give the port equivalent shapes).
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System;
using System.Collections.Generic;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

/// <summary>
/// The environment sandbox passed through a parse/render run. Rules and plugins store
/// arbitrary data here (link reference definitions, footnote state, front matter, ...).
/// </summary>
public sealed class MdEnv : Dictionary<string, object>
{
    /// <summary>Creates an empty environment.</summary>
    public MdEnv() : base(StringComparer.Ordinal) { }

    /// <summary>Link reference definitions collected by the reference block rule.</summary>
    public Dictionary<string, LinkReference> References
    {
        get
        {
            if (!TryGetValue("references", out var value) || value is not Dictionary<string, LinkReference> refs)
            {
                return null;
            }
            return refs;
        }
        set => this["references"] = value;
    }
}

/// <summary>One link reference definition ([label]: href "title").</summary>
public sealed class LinkReference
{
    /// <summary>The optional link title.</summary>
    public string Title { get; set; } = "";

    /// <summary>The normalized link destination.</summary>
    public string Href { get; set; } = "";
}

/// <summary>An emphasis-like delimiter found during inline parsing.</summary>
public sealed class Delimiter
{
    /// <summary>Char code of the delimiter character ('*', '_', '~', ...).</summary>
    public int Marker { get; set; }

    /// <summary>Total length of the delimiter run (0 when the token is a subsequent part of a run).</summary>
    public int Length { get; set; }

    /// <summary>Index of the token this delimiter corresponds to.</summary>
    public int Token { get; set; }

    /// <summary>Index of the matching closing delimiter; -1 when unmatched.</summary>
    public int End { get; set; } = -1;

    /// <summary>True when this delimiter could open an emphasis sequence.</summary>
    public bool Open { get; set; }

    /// <summary>True when this delimiter could close an emphasis sequence.</summary>
    public bool Close { get; set; }
}

/// <summary>Metadata attached to opening inline tokens (their delimiter list).</summary>
public sealed class TokenMeta
{
    /// <summary>The delimiter list collected while the token was open.</summary>
    public List<Delimiter> Delimiters { get; set; }
}

/// <summary>Result of StateInline.ScanDelims.</summary>
public readonly struct ScanDelimsResult
{
    /// <summary>Creates a scan result.</summary>
    public ScanDelimsResult(bool canOpen, bool canClose, int length)
    {
        CanOpen = canOpen;
        CanClose = canClose;
        Length = length;
    }

    /// <summary>True when the run can start an emphasis sequence.</summary>
    public bool CanOpen { get; }

    /// <summary>True when the run can end an emphasis sequence.</summary>
    public bool CanClose { get; }

    /// <summary>The length of the scanned delimiter run.</summary>
    public int Length { get; }
}
