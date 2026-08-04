namespace CodeBrix.PdfDocCreate.Html2Pdf.Css;

/// <summary>
/// The built-in default stylesheet: neutral, document-ish element defaults that sit at
/// the bottom of the cascade so any author CSS overrides them. Written strictly in the
/// documented Html2Pdf CSS dialect.
/// </summary>
internal static class DefaultStylesheet
{
    public const string Css = @"
html { font-family: sans-serif; font-size: 11pt; color: #1a1a1a; line-height: 1.5; }
body { margin: 0; }

h1 { font-size: 1.9em; font-weight: bold; margin: 0.6em 0 0.35em 0; }
h2 { font-size: 1.45em; font-weight: bold; margin: 0.9em 0 0.35em 0; }
h3 { font-size: 1.2em; font-weight: bold; margin: 0.8em 0 0.3em 0; }
h4 { font-size: 1.05em; font-weight: bold; margin: 0.7em 0 0.25em 0; }
h5 { font-size: 1em; font-weight: bold; margin: 0.6em 0 0.25em 0; }
h6 { font-size: 0.9em; font-weight: bold; color: #555555; margin: 0.6em 0 0.25em 0; }

p { margin: 0 0 0.65em 0; }
ul, ol { margin: 0 0 0.65em 0; }
li { margin: 0 0 0.2em 0; }

blockquote { margin: 0.4em 0 0.8em 1.4em; padding-left: 0.7em; border-left: 2pt solid #c9d1d9; color: #555555; }

pre { font-family: monospace; font-size: 0.85em; line-height: 1.35; background-color: #f6f8fa; border: 0.6pt solid #d0d7de; padding: 6pt 8pt; margin: 0.4em 0 0.8em 0; white-space: pre; }
code { font-family: monospace; font-size: 0.92em; }
kbd { font-family: monospace; font-size: 0.92em; }
samp { font-family: monospace; font-size: 0.92em; }

table { margin: 0.4em 0 0.8em 0; }
th, td { padding: 3pt 5pt; border: 0.5pt solid #c9d1d9; }
th { font-weight: bold; background-color: #eef1f4; }
caption { font-size: 0.85em; color: #555555; text-align: center; margin: 0 0 0.25em 0; }

hr { border-top: 0.75pt solid #c9d1d9; margin: 0.9em 0; }

a { color: #1a5fb4; text-decoration: underline; }
strong, b { font-weight: bold; }
em, i, dfn, cite, var { font-style: italic; }
u, ins { text-decoration: underline; }
s, del, strike { text-decoration: line-through; }
small { font-size: 0.85em; }
sub, sup { font-size: 0.75em; }

figure { margin: 0.6em 0 0.9em 0; }
figcaption { font-size: 0.85em; color: #555555; text-align: center; margin: 0.25em 0 0 0; }

dt { font-weight: bold; }
dd { margin: 0 0 0.4em 1.5em; }
";
}
