// ============================================================================
// C# port of markdown-it-task-lists (ISC License, copyright (c) 2016 Revin
// Guillen), adapted for PDF rendering: the checkbox is emitted as a styleable
// span with a geometric-shape glyph instead of an <input> element, because the
// Html2Pdf dialect has no form controls.
// https://github.com/revin/markdown-it-task-lists
// ============================================================================

using System.Collections.Generic;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesCore;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.Plugins;

/// <summary>
/// GitHub-style task lists: list items starting with [ ] or [x] render with a
/// checkbox glyph (□ / ■) wrapped in a span carrying the classes
/// "task-list-item-checkbox" and "task-checked"/"task-unchecked".
/// </summary>
public static class TaskListPlugin
{
    /// <summary>Installs the plugin into a parser.</summary>
    public static void Apply(MarkdownParser md)
    {
        md.Core.Ruler.After("inline", "github-task-lists", Rule);
    }

    private static void Rule(StateCore state)
    {
        var tokens = state.Tokens;
        for (var i = 2; i < tokens.Count; i++)
        {
            if (IsTodoItem(tokens, i))
            {
                Todoify(tokens[i]);
                tokens[i - 2].AttrSet("class", "task-list-item");
                var parent = ParentToken(tokens, i - 2);
                if (parent >= 0)
                {
                    tokens[parent].AttrSet("class", "contains-task-list");
                }
            }
        }
    }

    private static int ParentToken(List<Token> tokens, int index)
    {
        var targetLevel = tokens[index].Level - 1;
        for (var i = index - 1; i >= 0; i--)
        {
            if (tokens[i].Level == targetLevel) { return i; }
        }
        return -1;
    }

    private static bool IsTodoItem(List<Token> tokens, int index) =>
        tokens[index].Type == "inline"
        && tokens[index - 1].Type == "paragraph_open"
        && tokens[index - 2].Type == "list_item_open"
        && StartsWithTodoMarkdown(tokens[index]);

    private static bool StartsWithTodoMarkdown(Token token) =>
        token.Content.StartsWith("[ ] ", System.StringComparison.Ordinal)
        || token.Content.StartsWith("[x] ", System.StringComparison.Ordinal)
        || token.Content.StartsWith("[X] ", System.StringComparison.Ordinal);

    private static void Todoify(Token token)
    {
        var checkbox = MakeCheckbox(token);
        token.Children.Insert(0, checkbox);
        token.Children[1].Content = token.Children[1].Content.Substring(3);
        token.Content = token.Content.Substring(3);
    }

    private static Token MakeCheckbox(Token token)
    {
        var checkbox = new Token("html_inline", "", 0);
        checkbox.Content = token.Content.StartsWith("[ ] ", System.StringComparison.Ordinal)
            ? "<span class=\"task-list-item-checkbox task-unchecked\">□</span>"
            : "<span class=\"task-list-item-checkbox task-checked\">■</span>";
        return checkbox;
    }
}
