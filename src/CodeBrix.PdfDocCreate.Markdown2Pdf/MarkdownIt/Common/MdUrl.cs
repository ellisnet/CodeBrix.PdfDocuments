// ============================================================================
// C# port of mdurl v2.0.0 (MIT) - lib/parse.mjs, encode.mjs, decode.mjs, format.mjs
// mdurl copyright (c) 2015 Vitaly Puzrin, Alex Kocharin.
// parse.mjs derives from the Node.js url parser, copyright Joyent, Inc. and
// other Node contributors, MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

/// <summary>The pieces of a loosely parsed URL (markdown flavored - very tolerant).</summary>
public sealed class MdUrlParts
{
    /// <summary>The scheme including its trailing colon, e.g. "https:".</summary>
    public string Protocol { get; set; }

    /// <summary>True when the URL had a "//" after the scheme.</summary>
    public bool Slashes { get; set; }

    /// <summary>The user-info portion before '@'.</summary>
    public string Auth { get; set; }

    /// <summary>The port digits (without the colon).</summary>
    public string Port { get; set; }

    /// <summary>The host name (brackets stripped for IPv6).</summary>
    public string Hostname { get; set; }

    /// <summary>The fragment including '#'.</summary>
    public string Hash { get; set; }

    /// <summary>The query string including '?'.</summary>
    public string Search { get; set; }

    /// <summary>The path portion.</summary>
    public string Pathname { get; set; }
}

/// <summary>Markdown-tolerant URL parse/format/encode/decode (the mdurl library).</summary>
public static class MdUrl
{
    /// <summary>Characters Encode leaves unencoded by default.</summary>
    public const string EncodeDefaultChars = ";/?:@&=+$,-_.!~*'()#";

    /// <summary>Characters Decode keeps percent-encoded by default.</summary>
    public const string DecodeDefaultChars = ";/?:@&=+$,#";

    private static readonly Regex ProtocolPattern = new Regex("^([a-z0-9.+-]+:)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PortPattern = new Regex(":[0-9]*$", RegexOptions.Compiled);
    private static readonly Regex SimplePathPattern = new Regex(@"^(\/\/?(?!\/)[^\?\s]*)(\?[^\s]*)?$", RegexOptions.Compiled);
    private static readonly Regex HostnamePartPattern = new Regex("^[+a-z0-9A-Z_-]{0,63}$", RegexOptions.Compiled);
    private static readonly Regex HostnamePartStart = new Regex("^([+a-z0-9A-Z_-]{0,63})(.*)$", RegexOptions.Compiled);
    private static readonly Regex PercentSeqRe = new Regex("(%[a-f0-9]{2})+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex HexPairRe = new Regex("^[0-9a-f]{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> HostlessProtocol = new HashSet<string>(StringComparer.Ordinal)
    {
        "javascript", "javascript:",
    };

    private static readonly HashSet<string> SlashedProtocol = new HashSet<string>(StringComparer.Ordinal)
    {
        "http", "https", "ftp", "gopher", "file",
        "http:", "https:", "ftp:", "gopher:", "file:",
    };

    private const int HostnameMaxLen = 255;

    /// <summary>Parses a URL string very tolerantly (never throws).</summary>
    public static MdUrlParts Parse(string url, bool slashesDenoteHost)
    {
        var u = new MdUrlParts();
        var rest = url.Trim();

        if (!slashesDenoteHost && url.Split('#').Length == 1)
        {
            // Try fast path regexp
            var simplePath = SimplePathPattern.Match(rest);
            if (simplePath.Success)
            {
                u.Pathname = simplePath.Groups[1].Value;
                if (simplePath.Groups[2].Success && simplePath.Groups[2].Value.Length > 0)
                {
                    u.Search = simplePath.Groups[2].Value;
                }
                return u;
            }
        }

        string proto = null;
        string lowerProto = null;
        var protoMatch = ProtocolPattern.Match(rest);
        if (protoMatch.Success)
        {
            proto = protoMatch.Groups[0].Value;
            lowerProto = proto.ToLowerInvariant();
            u.Protocol = proto;
            rest = rest.Substring(proto.Length);
        }

        // figure out if it's got a host
        var slashes = false;
        if (slashesDenoteHost || proto != null || Regex.IsMatch(rest, @"^\/\/[^@\/]+@[^@\/]+"))
        {
            slashes = rest.Length >= 2 && rest[0] == '/' && rest[1] == '/';
            if (slashes && !(proto != null && HostlessProtocol.Contains(proto)))
            {
                rest = rest.Substring(2);
                u.Slashes = true;
            }
        }

        var hostEndingChars = new[] { '/', '?', '#' };
        var nonHostChars = new[] { '%', '/', '?', ';', '#', '\'', '{', '}', '|', '\\', '^', '`', '<', '>', '"', ' ', '\r', '\n', '\t' };

        if (!HostlessProtocol.Contains(proto ?? "")
            && (slashes || (proto != null && !SlashedProtocol.Contains(proto))))
        {
            // there's a hostname; the first instance of /, ?, ;, or # ends the host
            var hostEnd = -1;
            foreach (var hec in hostEndingChars)
            {
                var index = rest.IndexOf(hec);
                if (index != -1 && (hostEnd == -1 || index < hostEnd)) { hostEnd = index; }
            }

            // the auth portion cannot go past hostEnd, or the last @ char is the decider
            var atSign = hostEnd == -1
                ? rest.LastIndexOf('@')
                : rest.LastIndexOf('@', hostEnd);

            if (atSign != -1)
            {
                u.Auth = rest.Substring(0, atSign);
                rest = rest.Substring(atSign + 1);
            }

            // the host is the remaining to the left of the first non-host char
            hostEnd = -1;
            foreach (var hec in nonHostChars)
            {
                var index = rest.IndexOf(hec);
                if (index != -1 && (hostEnd == -1 || index < hostEnd)) { hostEnd = index; }
            }
            if (hostEnd == -1) { hostEnd = rest.Length; }

            if (hostEnd > 0 && rest[hostEnd - 1] == ':') { hostEnd--; }
            var host = rest.Substring(0, hostEnd);
            rest = rest.Substring(hostEnd);

            ParseHost(u, host);

            // even if the hostname is empty, it has to be present
            u.Hostname ??= "";

            // if the hostname begins with [ and ends with ], assume an IPv6 address
            var ipv6Hostname = u.Hostname.Length >= 2
                && u.Hostname[0] == '['
                && u.Hostname[u.Hostname.Length - 1] == ']';

            if (!ipv6Hostname)
            {
                var hostparts = u.Hostname.Split('.');
                for (var i = 0; i < hostparts.Length; i++)
                {
                    var part = hostparts[i];
                    if (part.Length == 0) { continue; }
                    if (!HostnamePartPattern.IsMatch(part))
                    {
                        var newpart = new StringBuilder();
                        foreach (var c in part)
                        {
                            // replace non-ASCII chars with a placeholder so the size check holds
                            newpart.Append(c > 127 ? 'x' : c);
                        }
                        if (!HostnamePartPattern.IsMatch(newpart.ToString()))
                        {
                            var validParts = new List<string>();
                            for (var v = 0; v < i; v++) { validParts.Add(hostparts[v]); }
                            var notHost = new List<string>();
                            for (var v = i + 1; v < hostparts.Length; v++) { notHost.Add(hostparts[v]); }
                            var bit = HostnamePartStart.Match(part);
                            if (bit.Success)
                            {
                                validParts.Add(bit.Groups[1].Value);
                                notHost.Insert(0, bit.Groups[2].Value);
                            }
                            if (notHost.Count > 0)
                            {
                                rest = string.Join(".", notHost) + rest;
                            }
                            u.Hostname = string.Join(".", validParts);
                            break;
                        }
                    }
                }
            }

            if (u.Hostname.Length > HostnameMaxLen) { u.Hostname = ""; }

            // strip [ and ] from the hostname
            if (ipv6Hostname)
            {
                u.Hostname = u.Hostname.Substring(1, u.Hostname.Length - 2);
            }
        }

        // chop off from the tail first
        var hash = rest.IndexOf('#');
        if (hash != -1)
        {
            u.Hash = rest.Substring(hash);
            rest = rest.Substring(0, hash);
        }
        var qm = rest.IndexOf('?');
        if (qm != -1)
        {
            u.Search = rest.Substring(qm);
            rest = rest.Substring(0, qm);
        }
        if (rest.Length > 0) { u.Pathname = rest; }
        if (SlashedProtocol.Contains(lowerProto ?? "") && u.Hostname != null && u.Hostname.Length > 0 && u.Pathname == null)
        {
            u.Pathname = "";
        }

        return u;
    }

    private static void ParseHost(MdUrlParts u, string host)
    {
        var portMatch = PortPattern.Match(host);
        if (portMatch.Success)
        {
            var port = portMatch.Value;
            if (port != ":") { u.Port = port.Substring(1); }
            host = host.Substring(0, host.Length - port.Length);
        }
        if (host.Length > 0) { u.Hostname = host; }
    }

    /// <summary>Reassembles parsed URL pieces.</summary>
    public static string Format(MdUrlParts url)
    {
        var result = new StringBuilder();
        result.Append(url.Protocol ?? "");
        result.Append(url.Slashes ? "//" : "");
        if (!string.IsNullOrEmpty(url.Auth)) { result.Append(url.Auth).Append('@'); }

        if (url.Hostname != null && url.Hostname.IndexOf(':') != -1)
        {
            result.Append('[').Append(url.Hostname).Append(']'); // ipv6 address
        }
        else
        {
            result.Append(url.Hostname ?? "");
        }

        if (!string.IsNullOrEmpty(url.Port)) { result.Append(':').Append(url.Port); }
        result.Append(url.Pathname ?? "");
        result.Append(url.Search ?? "");
        result.Append(url.Hash ?? "");
        return result.ToString();
    }

    private static readonly Dictionary<string, string[]> EncodeCaches = new Dictionary<string, string[]>(StringComparer.Ordinal);

    private static string[] GetEncodeCache(string exclude)
    {
        lock (EncodeCaches)
        {
            if (EncodeCaches.TryGetValue(exclude, out var cached)) { return cached; }

            var cache = new string[128];
            for (var i = 0; i < 128; i++)
            {
                var ch = (char)i;
                cache[i] = char.IsAsciiLetterOrDigit(ch)
                    ? ch.ToString()
                    : "%" + i.ToString("X2", CultureInfo.InvariantCulture);
            }

            foreach (var c in exclude)
            {
                if (c < 128) { cache[c] = c.ToString(); }
            }

            EncodeCaches[exclude] = cache;
            return cache;
        }
    }

    /// <summary>
    /// Percent-encodes unsafe characters, skipping already-encoded sequences when
    /// <paramref name="keepEscaped"/> is true.
    /// </summary>
    public static string Encode(string str, string exclude = EncodeDefaultChars, bool keepEscaped = true)
    {
        var cache = GetEncodeCache(exclude);
        var result = new StringBuilder(str.Length + 16);

        for (var i = 0; i < str.Length; i++)
        {
            var code = (int)str[i];

            if (keepEscaped && code == 0x25 /* % */ && i + 2 < str.Length
                && HexPairRe.IsMatch(str.Substring(i + 1, 2)))
            {
                result.Append(str, i, 3);
                i += 2;
                continue;
            }

            if (code < 128)
            {
                result.Append(cache[code]);
                continue;
            }

            if (code >= 0xD800 && code <= 0xDFFF)
            {
                if (code <= 0xDBFF && i + 1 < str.Length)
                {
                    var nextCode = (int)str[i + 1];
                    if (nextCode >= 0xDC00 && nextCode <= 0xDFFF)
                    {
                        result.Append(Uri.EscapeDataString(str.Substring(i, 2)));
                        i++;
                        continue;
                    }
                }
                result.Append("%EF%BF%BD");
                continue;
            }

            result.Append(Uri.EscapeDataString(str[i].ToString()));
        }

        return result.ToString();
    }

    /// <summary>Decodes a percent-encoded string, keeping excluded characters encoded.</summary>
    public static string Decode(string str, string exclude = DecodeDefaultChars)
    {
        return PercentSeqRe.Replace(str, match =>
        {
            var seq = match.Value;
            var result = new StringBuilder();

            for (var i = 0; i < seq.Length; i += 3)
            {
                var b1 = int.Parse(seq.Substring(i + 1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                if (b1 < 0x80)
                {
                    var ch = (char)b1;
                    result.Append(exclude.IndexOf(ch) >= 0
                        ? "%" + b1.ToString("X2", CultureInfo.InvariantCulture)
                        : ch.ToString());
                    continue;
                }

                if ((b1 & 0xE0) == 0xC0 && i + 3 < seq.Length)
                {
                    // 110xxxxx 10xxxxxx
                    var b2 = int.Parse(seq.Substring(i + 4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    if ((b2 & 0xC0) == 0x80)
                    {
                        var chr = ((b1 << 6) & 0x7C0) | (b2 & 0x3F);
                        result.Append(chr < 0x80 ? "��" : ((char)chr).ToString());
                        i += 3;
                        continue;
                    }
                }

                if ((b1 & 0xF0) == 0xE0 && i + 6 < seq.Length)
                {
                    // 1110xxxx 10xxxxxx 10xxxxxx
                    var b2 = int.Parse(seq.Substring(i + 4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    var b3 = int.Parse(seq.Substring(i + 7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    if ((b2 & 0xC0) == 0x80 && (b3 & 0xC0) == 0x80)
                    {
                        var chr = ((b1 << 12) & 0xF000) | ((b2 << 6) & 0xFC0) | (b3 & 0x3F);
                        result.Append(chr < 0x800 || (chr >= 0xD800 && chr <= 0xDFFF)
                            ? "���"
                            : ((char)chr).ToString());
                        i += 6;
                        continue;
                    }
                }

                if ((b1 & 0xF8) == 0xF0 && i + 9 < seq.Length)
                {
                    // 111110xx 10xxxxxx 10xxxxxx 10xxxxxx
                    var b2 = int.Parse(seq.Substring(i + 4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    var b3 = int.Parse(seq.Substring(i + 7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    var b4 = int.Parse(seq.Substring(i + 10, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    if ((b2 & 0xC0) == 0x80 && (b3 & 0xC0) == 0x80 && (b4 & 0xC0) == 0x80)
                    {
                        var chr = ((b1 << 18) & 0x1C0000) | ((b2 << 12) & 0x3F000) | ((b3 << 6) & 0xFC0) | (b4 & 0x3F);
                        if (chr < 0x10000 || chr > 0x10FFFF)
                        {
                            result.Append("����");
                        }
                        else
                        {
                            chr -= 0x10000;
                            result.Append((char)(0xD800 + (chr >> 10)));
                            result.Append((char)(0xDC00 + (chr & 0x3FF)));
                        }
                        i += 9;
                        continue;
                    }
                }

                result.Append('�');
            }

            return result.ToString();
        });
    }
}
