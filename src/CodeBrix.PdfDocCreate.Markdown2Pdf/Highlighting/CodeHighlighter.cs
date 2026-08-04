using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.Highlighting;

/// <summary>Token categories the highlighter distinguishes.</summary>
internal enum CodeTokenKind
{
    Plain,
    Keyword,
    String,
    Comment,
    Number,
    Type,
    Attribute,
}

/// <summary>
/// A small hand-rolled tokenizer for the languages commonly found in fenced code
/// blocks: C#, bash/PowerShell, JSON, XML/HTML, JavaScript/TypeScript, Python, SQL and
/// YAML. Intentionally approximate - the goal is readable color in a printed document,
/// not a compiler front end. Emits HTML spans with hl-* classes that the default
/// stylesheet colors. Block-comment state carries across lines within one code block.
/// </summary>
internal sealed class CodeHighlighter
{
    private readonly HashSet<string> _keywords;
    private readonly HashSet<string> _types;
    private readonly string[] _lineComments;
    private readonly bool _blockComments;
    private readonly char[] _stringDelimiters;
    private readonly bool _xmlMode;
    private readonly bool _jsonMode;
    private readonly bool _caseInsensitiveKeywords;

    private bool _inBlockComment;

    private CodeHighlighter(
        IEnumerable<string> keywords,
        IEnumerable<string> types,
        string[] lineComments,
        bool blockComments,
        char[] stringDelimiters,
        bool xmlMode = false,
        bool jsonMode = false,
        bool caseInsensitiveKeywords = false)
    {
        _caseInsensitiveKeywords = caseInsensitiveKeywords;
        var comparer = caseInsensitiveKeywords ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        _keywords = new HashSet<string>(keywords, comparer);
        _types = new HashSet<string>(types, comparer);
        _lineComments = lineComments;
        _blockComments = blockComments;
        _stringDelimiters = stringDelimiters;
        _xmlMode = xmlMode;
        _jsonMode = jsonMode;
    }

    /// <summary>
    /// Highlights fenced code content to escaped HTML with hl-* spans; returns null when
    /// the language has no highlighter (the caller escapes plainly).
    /// </summary>
    public static string Highlight(string content, string languageName)
    {
        var highlighter = For(languageName);
        if (highlighter == null) { return null; }

        var builder = new StringBuilder(content.Length + 64);
        var lines = content.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) { builder.Append('\n'); }
            foreach (var (text, kind) in highlighter.TokenizeLine(lines[i]))
            {
                if (kind == CodeTokenKind.Plain)
                {
                    builder.Append(MdUtils.EscapeHtml(text));
                }
                else
                {
                    builder.Append("<span class=\"hl-").Append(ClassName(kind)).Append("\">")
                        .Append(MdUtils.EscapeHtml(text)).Append("</span>");
                }
            }
        }
        return builder.ToString();
    }

    private static string ClassName(CodeTokenKind kind) => kind switch
    {
        CodeTokenKind.Keyword => "keyword",
        CodeTokenKind.String => "string",
        CodeTokenKind.Comment => "comment",
        CodeTokenKind.Number => "number",
        CodeTokenKind.Type => "type",
        _ => "attribute",
    };

    /// <summary>Returns null when the language has no highlighting.</summary>
    internal static CodeHighlighter For(string language) => (language ?? "").Trim().ToLowerInvariant() switch
    {
        "csharp" or "c#" or "cs" => CSharp(),
        "bash" or "sh" or "shell" or "zsh" or "powershell" or "ps1" or "console" => Shell(),
        "json" or "jsonc" => Json(),
        "xml" or "csproj" or "html" or "xaml" or "svg" => Xml(),
        "typescript" or "ts" or "javascript" or "js" or "jsx" or "tsx" => TypeScript(),
        "python" or "py" => Python(),
        "sql" => Sql(),
        "yaml" or "yml" => Yaml(),
        "c" or "cpp" or "c++" or "h" or "hpp" => Cpp(),
        _ => null,
    };

    private static CodeHighlighter CSharp() => new CodeHighlighter(
        keywords: new[]
        {
            "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
            "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
            "in", "init", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
            "operator", "out", "override", "params", "private", "protected", "public", "readonly", "record", "ref",
            "required", "return", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
            "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "var", "virtual", "void", "volatile", "when", "where", "while", "yield", "global", "nameof", "with",
        },
        types: new[]
        {
            "Console", "DateTime", "DateTimeOffset", "TimeSpan", "Task", "List", "Dictionary", "IEnumerable",
            "IReadOnlyList", "String", "Int32", "Guid", "Exception", "ArgumentException", "StringBuilder",
            "CancellationToken", "Math", "Path", "File", "Directory", "Stream", "MemoryStream",
        },
        lineComments: new[] { "//" },
        blockComments: true,
        stringDelimiters: new[] { '"', '\'' });

    private static CodeHighlighter Cpp() => new CodeHighlighter(
        keywords: new[]
        {
            "auto", "bool", "break", "case", "catch", "char", "class", "const", "constexpr", "continue", "default",
            "delete", "do", "double", "else", "enum", "explicit", "extern", "false", "float", "for", "friend", "goto",
            "if", "inline", "int", "long", "namespace", "new", "nullptr", "operator", "private", "protected",
            "public", "return", "short", "signed", "sizeof", "static", "struct", "switch", "template", "this",
            "throw", "true", "try", "typedef", "typename", "union", "unsigned", "using", "virtual", "void",
            "volatile", "while", "include", "define", "ifdef", "ifndef", "endif", "pragma",
        },
        types: new[] { "std", "size_t", "int32_t", "uint32_t", "int64_t", "uint64_t", "string", "vector", "map" },
        lineComments: new[] { "//" },
        blockComments: true,
        stringDelimiters: new[] { '"', '\'' });

    private static CodeHighlighter Shell() => new CodeHighlighter(
        keywords: new[]
        {
            "cd", "ls", "dir", "echo", "export", "set", "if", "then", "else", "elif", "fi", "for", "in", "do",
            "done", "while", "function", "return", "source", "sudo", "mkdir", "rm", "cp", "mv", "cat", "grep",
            "dotnet", "git", "npm", "node", "curl", "wget", "pwsh", "python", "pip", "pytest", "make", "case",
            "esac", "local", "read", "exit", "true", "false", "chmod", "chown", "tar", "find", "sed", "awk",
        },
        types: Array.Empty<string>(),
        lineComments: new[] { "#" },
        blockComments: false,
        stringDelimiters: new[] { '"', '\'' });

    private static CodeHighlighter Json() => new CodeHighlighter(
        keywords: new[] { "true", "false", "null" },
        types: Array.Empty<string>(),
        lineComments: new[] { "//" },
        blockComments: false,
        stringDelimiters: new[] { '"' },
        jsonMode: true);

    private static CodeHighlighter Xml() => new CodeHighlighter(
        keywords: Array.Empty<string>(),
        types: Array.Empty<string>(),
        lineComments: Array.Empty<string>(),
        blockComments: false,
        stringDelimiters: new[] { '"', '\'' },
        xmlMode: true);

    private static CodeHighlighter TypeScript() => new CodeHighlighter(
        keywords: new[]
        {
            "as", "async", "await", "break", "case", "catch", "class", "const", "continue", "default", "delete",
            "do", "else", "enum", "export", "extends", "false", "finally", "for", "from", "function", "if",
            "implements", "import", "in", "instanceof", "interface", "let", "new", "null", "of", "private",
            "protected", "public", "readonly", "return", "static", "super", "switch", "this", "throw", "true",
            "try", "type", "typeof", "undefined", "var", "void", "while", "yield", "boolean", "number", "string",
            "any", "unknown", "never",
        },
        types: new[] { "Promise", "Array", "Record", "Map", "Set", "console", "Math", "JSON", "Object", "Date" },
        lineComments: new[] { "//" },
        blockComments: true,
        stringDelimiters: new[] { '"', '\'', '`' });

    private static CodeHighlighter Python() => new CodeHighlighter(
        keywords: new[]
        {
            "and", "as", "assert", "async", "await", "break", "class", "continue", "def", "del", "elif", "else",
            "except", "False", "finally", "for", "from", "global", "if", "import", "in", "is", "lambda", "None",
            "nonlocal", "not", "or", "pass", "raise", "return", "True", "try", "while", "with", "yield", "match",
            "case", "self",
        },
        types: new[] { "print", "len", "range", "str", "int", "float", "list", "dict", "set", "tuple", "type", "open" },
        lineComments: new[] { "#" },
        blockComments: false,
        stringDelimiters: new[] { '"', '\'' });

    private static CodeHighlighter Sql() => new CodeHighlighter(
        keywords: new[]
        {
            "select", "from", "where", "insert", "into", "values", "update", "delete", "create", "table", "drop",
            "alter", "index", "join", "inner", "left", "right", "outer", "on", "as", "and", "or", "not", "null",
            "order", "by", "group", "having", "limit", "offset", "distinct", "union", "all", "exists", "in",
            "like", "between", "is", "primary", "key", "foreign", "references", "constraint", "default", "unique",
            "case", "when", "then", "else", "end", "begin", "commit", "rollback", "transaction",
        },
        types: new[] { "integer", "int", "text", "varchar", "nvarchar", "datetime", "timestamp", "real", "blob", "boolean" },
        lineComments: new[] { "--" },
        blockComments: true,
        stringDelimiters: new[] { '\'' },
        caseInsensitiveKeywords: true);

    private static CodeHighlighter Yaml() => new CodeHighlighter(
        keywords: new[] { "true", "false", "null", "yes", "no" },
        types: Array.Empty<string>(),
        lineComments: new[] { "#" },
        blockComments: false,
        stringDelimiters: new[] { '"', '\'' });

    private List<(string Text, CodeTokenKind Kind)> TokenizeLine(string line)
    {
        var tokens = new List<(string, CodeTokenKind)>();
        var i = 0;

        void Emit(string text, CodeTokenKind kind)
        {
            if (text.Length == 0) { return; }
            if (tokens.Count > 0 && tokens[tokens.Count - 1].Item2 == kind)
            {
                tokens[tokens.Count - 1] = (tokens[tokens.Count - 1].Item1 + text, kind);
            }
            else
            {
                tokens.Add((text, kind));
            }
        }

        while (i < line.Length)
        {
            // continuation of a /* ... */ comment from a previous line
            if (_inBlockComment)
            {
                var close = line.IndexOf("*/", i, StringComparison.Ordinal);
                if (close < 0) { Emit(line.Substring(i), CodeTokenKind.Comment); break; }
                Emit(line.Substring(i, close + 2 - i), CodeTokenKind.Comment);
                i = close + 2;
                _inBlockComment = false;
                continue;
            }

            var c = line[i];

            // line comment
            var lineComment = _lineComments.FirstOrDefault(p =>
                line.AsSpan(i).StartsWith(p, StringComparison.Ordinal));
            if (lineComment != null)
            {
                Emit(line.Substring(i), CodeTokenKind.Comment);
                break;
            }

            // block comment open
            if (_blockComments && line.AsSpan(i).StartsWith("/*", StringComparison.Ordinal))
            {
                _inBlockComment = true;
                continue;
            }

            // XML comment
            if (_xmlMode && line.AsSpan(i).StartsWith("<!--", StringComparison.Ordinal))
            {
                var close = line.IndexOf("-->", i, StringComparison.Ordinal);
                var end = close < 0 ? line.Length : close + 3;
                Emit(line.Substring(i, end - i), CodeTokenKind.Comment);
                i = end;
                continue;
            }

            // string literal
            if (_stringDelimiters.Contains(c))
            {
                var j = i + 1;
                while (j < line.Length)
                {
                    if (line[j] == '\\' && j + 1 < line.Length) { j += 2; continue; }
                    if (line[j] == c) { j++; break; }
                    j++;
                }
                var literal = line.Substring(i, Math.Min(j, line.Length) - i);

                // In JSON, a string immediately followed by ':' is a property name.
                if (_jsonMode)
                {
                    var after = j;
                    while (after < line.Length && char.IsWhiteSpace(line[after])) { after++; }
                    Emit(literal, after < line.Length && line[after] == ':' ? CodeTokenKind.Attribute : CodeTokenKind.String);
                }
                else
                {
                    Emit(literal, CodeTokenKind.String);
                }
                i = j;
                continue;
            }

            // XML tag name
            if (_xmlMode && c == '<')
            {
                var j = i + 1;
                while (j < line.Length && (char.IsLetterOrDigit(line[j]) || line[j] is '/' or '?' or '!' or '_' or '-' or '.'))
                {
                    j++;
                }
                Emit(line.Substring(i, j - i), CodeTokenKind.Keyword);
                i = j;
                continue;
            }

            // number
            if (char.IsDigit(c) && (i == 0 || !IsWordChar(line[i - 1])))
            {
                var j = i;
                while (j < line.Length && (char.IsDigit(line[j]) || line[j] is '.' or 'x' or 'X'
                       || (line[j] >= 'a' && line[j] <= 'f') || (line[j] >= 'A' && line[j] <= 'F')))
                {
                    j++;
                }
                Emit(line.Substring(i, j - i), CodeTokenKind.Number);
                i = j;
                continue;
            }

            // identifier / keyword / type / xml attribute
            if (IsWordChar(c))
            {
                var j = i;
                while (j < line.Length && (IsWordChar(line[j]) || line[j] == '-')) { j++; }
                var word = line.Substring(i, j - i);

                if (_keywords.Contains(word)) { Emit(word, CodeTokenKind.Keyword); }
                else if (_types.Contains(word)) { Emit(word, CodeTokenKind.Type); }
                else if (_xmlMode) { Emit(word, CodeTokenKind.Attribute); }
                else if (word.Length > 1 && char.IsUpper(word[0]) && !_jsonMode && !_caseInsensitiveKeywords)
                {
                    Emit(word, CodeTokenKind.Type);
                }
                else { Emit(word, CodeTokenKind.Plain); }

                i = j;
                continue;
            }

            Emit(c.ToString(), CodeTokenKind.Plain);
            i++;
        }

        return tokens;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '$';
}
