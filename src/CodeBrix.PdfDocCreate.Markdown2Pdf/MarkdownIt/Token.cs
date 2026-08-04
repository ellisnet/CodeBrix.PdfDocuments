// ============================================================================
// C# port of markdown-it v14.1.0 - lib/token.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// https://github.com/markdown-it/markdown-it
// ============================================================================

using System.Collections.Generic;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

/// <summary>One parsed markdown token (block- or inline-level).</summary>
public sealed class Token
{
    /// <summary>Creates a new token and fills the passed properties.</summary>
    public Token(string type, string tag, int nesting)
    {
        Type = type;
        Tag = tag;
        Nesting = nesting;
    }

    /// <summary>Type of the token, e.g. "paragraph_open".</summary>
    public string Type { get; set; }

    /// <summary>HTML tag name, e.g. "p".</summary>
    public string Tag { get; set; }

    /// <summary>HTML attributes as [name, value] pairs; null when there are none.</summary>
    public List<string[]> Attrs { get; set; }

    /// <summary>Source map info as [line_begin, line_end]; null when unknown.</summary>
    public int[] Map { get; set; }

    /// <summary>Level change: 1 opening, 0 self-closing, -1 closing.</summary>
    public int Nesting { get; set; }

    /// <summary>Nesting level, the same as state.Level.</summary>
    public int Level { get; set; }

    /// <summary>Child nodes for inline and img tokens; null otherwise.</summary>
    public List<Token> Children { get; set; }

    /// <summary>Contents of a self-closing tag (code, html, fence, ...).</summary>
    public string Content { get; set; } = "";

    /// <summary>'*' or '_' for emphasis, the fence string for fences, etc.</summary>
    public string Markup { get; set; } = "";

    /// <summary>
    /// Additional information: the info string for fences, "auto" for autolinks, the
    /// item marker string for ordered-list items.
    /// </summary>
    public string Info { get; set; } = "";

    /// <summary>A place for plugins to store arbitrary data.</summary>
    public Dictionary<string, object> Meta { get; set; }

    /// <summary>True for block-level tokens; the renderer uses this for line breaks.</summary>
    public bool Block { get; set; }

    /// <summary>When true the renderer ignores this token (tight-list paragraphs).</summary>
    public bool Hidden { get; set; }

    /// <summary>Searches the attribute index by name; -1 when absent.</summary>
    public int AttrIndex(string name)
    {
        if (Attrs == null) { return -1; }
        for (var i = 0; i < Attrs.Count; i++)
        {
            if (Attrs[i][0] == name) { return i; }
        }
        return -1;
    }

    /// <summary>Adds a [name, value] attribute, initializing the list if necessary.</summary>
    public void AttrPush(string[] attrData)
    {
        Attrs ??= new List<string[]>();
        Attrs.Add(attrData);
    }

    /// <summary>Sets an attribute, overriding any existing value.</summary>
    public void AttrSet(string name, string value)
    {
        var idx = AttrIndex(name);
        var attrData = new[] { name, value };
        if (idx < 0) { AttrPush(attrData); }
        else { Attrs[idx] = attrData; }
    }

    /// <summary>Gets the value of an attribute, or null when it does not exist.</summary>
    public string AttrGet(string name)
    {
        var idx = AttrIndex(name);
        return idx >= 0 ? Attrs[idx][1] : null;
    }

    /// <summary>Joins a value to an existing attribute with a space (useful for classes).</summary>
    public void AttrJoin(string name, string value)
    {
        var idx = AttrIndex(name);
        if (idx < 0) { AttrPush(new[] { name, value }); }
        else { Attrs[idx][1] = Attrs[idx][1] + " " + value; }
    }
}
