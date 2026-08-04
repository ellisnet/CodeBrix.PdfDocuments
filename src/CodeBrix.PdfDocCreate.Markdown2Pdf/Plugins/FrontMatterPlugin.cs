// ============================================================================
// C# port of markdown-it-front-matter (MIT License, copyright (c) 2016-2020
// ParkSB). https://github.com/ParkSB/markdown-it-front-matter
// ============================================================================

using System;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.Plugins;

/// <summary>
/// YAML front matter: a document-leading block fenced by --- lines is consumed as a
/// hidden token and its raw content is handed to a callback.
/// </summary>
public static class FrontMatterPlugin
{
    /// <summary>Installs the plugin; the callback receives the raw front-matter text.</summary>
    public static void Apply(MarkdownParser md, Action<string> callback)
    {
        bool FrontMatter(StateBlock state, int startLine, int endLine, bool silent)
        {
            var autoClosed = false;
            var start = state.BMarks[startLine] + state.TShift[startLine];
            var max = state.EMarks[startLine];

            // The first character of the first line quickly filters out non-front-matter.
            if (startLine != 0 || start >= state.Src.Length || state.Src[0] != '-')
            {
                return false;
            }

            // Check out the rest of the marker string.
            int pos;
            var startContent = start + 1;
            for (pos = start + 1; pos <= max; pos++)
            {
                if (pos >= state.Src.Length || state.Src[pos] != '-')
                {
                    startContent = pos + 1;
                    break;
                }
            }

            var markerCount = pos - start;
            if (markerCount < 3) { return false; }

            // Since the start is found, report success in validation mode.
            if (silent) { return true; }

            // Search for the end of the block.
            var nextLine = startLine;

            for (; ; )
            {
                nextLine++;
                if (nextLine >= endLine)
                {
                    // an unclosed block is autoclosed by the end of the document
                    break;
                }

                if (state.Src.Substring(start, Math.Max(0, max - start)) == "...")
                {
                    break;
                }

                start = state.BMarks[nextLine] + state.TShift[nextLine];
                max = state.EMarks[nextLine];

                if (start < max && state.SCount[nextLine] < state.BlkIndent)
                {
                    // a non-empty line with negative indent stops the block
                    break;
                }

                if (start >= state.Src.Length || state.Src[start] != '-') { continue; }

                if (state.SCount[nextLine] - state.BlkIndent >= 4)
                {
                    // the closing fence should be indented less than 4 spaces
                    continue;
                }

                for (pos = start + 1; pos <= max; pos++)
                {
                    if (pos >= state.Src.Length || state.Src[pos] != '-') { break; }
                }

                // the closing fence must be at least as long as the opening one
                if (pos - start < markerCount) { continue; }

                // make sure the tail has spaces only
                pos = state.SkipSpaces(pos);
                if (pos < max) { continue; }

                autoClosed = true;
                break;
            }

            var oldParent = state.ParentType;
            var oldLineMax = state.LineMax;
            state.ParentType = "container";

            // prevent lazy continuations from going past our end marker
            state.LineMax = nextLine;

            var token = state.Push("front_matter", "", 0);
            token.Hidden = true;
            token.Block = true;
            token.Map = new[] { startLine, nextLine + (autoClosed ? 1 : 0) };
            var metaEnd = Math.Max(startContent, start - 1);
            token.Meta = new System.Collections.Generic.Dictionary<string, object>
            {
                ["content"] = startContent <= state.Src.Length && metaEnd <= state.Src.Length && metaEnd >= startContent
                    ? state.Src.Substring(startContent, metaEnd - startContent)
                    : "",
            };

            state.ParentType = oldParent;
            state.LineMax = oldLineMax;
            state.Line = nextLine + (autoClosed ? 1 : 0);

            callback((string)token.Meta["content"]);

            return true;
        }

        md.Block.Ruler.Before("table", "front_matter", FrontMatter,
            new[] { "paragraph", "reference", "blockquote", "list" });
    }
}
