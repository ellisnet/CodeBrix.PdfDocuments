namespace CodeBrix.PdfDocCreate.Markdown2Pdf;

/// <summary>
/// The built-in house style: a polished, print-oriented look applied to every generated
/// document. Serif body text (Merriweather), sans-serif headings (Roboto), monospace
/// code (Roboto Mono). Written strictly in the CodeBrix.PdfDocCreate.Html2Pdf CSS
/// dialect, at author level - consumers who want a different look can take the
/// generated HTML and replace this stylesheet entirely.
/// </summary>
internal static class DefaultTheme
{
    public const string Css = @"
html { font-family: serif; font-size: 11pt; color: #1c1c1c; line-height: 1.55; }
body { margin: 0; }

h1, h2, h3, h4, h5, h6 { font-family: sans-serif; color: #223142; }
h1 { font-size: 1.85em; margin: 0.15em 0 0.5em 0; }
h2 { font-size: 1.4em; border-bottom: 0.75pt solid #d8dee4; padding-bottom: 2pt; margin: 1.15em 0 0.45em 0; }
h3 { font-size: 1.15em; margin: 0.95em 0 0.35em 0; }
h4 { font-size: 1.02em; margin: 0.85em 0 0.3em 0; }
h5 { font-size: 0.95em; margin: 0.8em 0 0.3em 0; }
h6 { font-size: 0.88em; color: #55606a; margin: 0.8em 0 0.3em 0; }

p { margin: 0 0 0.7em 0; }
ul, ol { margin: 0 0 0.7em 0; }
li { margin: 0 0 0.22em 0; }

a { color: #1a5fb4; text-decoration: none; }

code { font-family: monospace; font-size: 0.9em; color: #24467c; }
kbd, samp { font-family: monospace; font-size: 0.9em; }

pre { font-family: monospace; font-size: 0.82em; line-height: 1.42; color: #1c1c1c; background-color: #f6f8fa; border: 0.6pt solid #d8dee4; padding: 7pt 9pt; margin: 0.5em 0 0.9em 0; white-space: pre; }
pre code { color: #1c1c1c; font-size: 1em; }

blockquote { border-left: 2.25pt solid #b9c4cf; color: #4a5560; margin: 0.5em 0 0.9em 0.2em; padding-left: 0.8em; }

table { margin: 0.5em 0 0.9em 0; }
th { font-family: sans-serif; font-size: 0.85em; font-weight: bold; background-color: #eef1f4; }
td { font-size: 0.92em; }
th, td { border: 0.5pt solid #c9d1d9; padding: 3.5pt 6pt; }

hr { border-top: 0.75pt solid #c9d1d9; margin: 1.1em 0; }

img { margin: 0.4em 0; }
figure { margin: 0.6em 0 0.9em 0; }
figcaption { font-family: sans-serif; font-size: 0.82em; color: #55606a; text-align: center; }

/* syntax highlighting - restrained so it reads well in print */
.hl-keyword { color: #0d47a1; }
.hl-string { color: #a31515; }
.hl-comment { color: #178026; font-style: italic; }
.hl-number { color: #098658; }
.hl-type { color: #2b91af; }
.hl-attribute { color: #7a3e9d; }

/* GitHub-style task lists */
.task-list-item-checkbox { color: #57606a; }
.task-checked { color: #1a7f37; }

/* footnotes */
hr.footnotes-sep { margin: 1.6em 0 0.5em 0; }
.footnotes { font-size: 0.85em; color: #4a5560; }
.footnote-item { margin: 0 0 0.3em 0; }
.footnote-ref { font-family: sans-serif; }
.footnote-backref { text-decoration: none; }
";
}
