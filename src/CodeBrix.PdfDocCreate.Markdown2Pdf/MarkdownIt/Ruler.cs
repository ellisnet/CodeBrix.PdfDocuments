// ============================================================================
// C# port of markdown-it v14.1.0 - lib/ruler.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// https://github.com/markdown-it/markdown-it
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

/// <summary>
/// Manages a sequence of named rules: keeps their order, supports enable/disable,
/// insertion before/after by name, alternate chains, and caches the active rule lists.
/// </summary>
/// <typeparam name="TRule">The rule delegate type of this chain.</typeparam>
public sealed class Ruler<TRule> where TRule : Delegate
{
    private sealed class RuleEntry
    {
        public string Name;
        public bool Enabled;
        public TRule Fn;
        public List<string> Alt;
    }

    private readonly List<RuleEntry> _rules = new List<RuleEntry>();
    private Dictionary<string, TRule[]> _cache;

    private int Find(string name)
    {
        for (var i = 0; i < _rules.Count; i++)
        {
            if (_rules[i].Name == name) { return i; }
        }
        return -1;
    }

    private void Compile()
    {
        var chains = new List<string> { "" };
        foreach (var rule in _rules.Where(r => r.Enabled))
        {
            foreach (var alt in rule.Alt.Where(alt => !chains.Contains(alt)))
            {
                chains.Add(alt);
            }
        }

        _cache = new Dictionary<string, TRule[]>(StringComparer.Ordinal);
        foreach (var chain in chains)
        {
            _cache[chain] = _rules
                .Where(r => r.Enabled && (chain.Length == 0 || r.Alt.Contains(chain)))
                .Select(r => r.Fn)
                .ToArray();
        }
    }

    /// <summary>Replaces the rule with the given name. Throws when the name is unknown.</summary>
    public void At(string name, TRule fn, IEnumerable<string> alt = null)
    {
        var index = Find(name);
        if (index == -1) { throw new InvalidOperationException("Parser rule not found: " + name); }
        _rules[index].Fn = fn;
        _rules[index].Alt = alt?.ToList() ?? new List<string>();
        _cache = null;
    }

    /// <summary>Adds a new rule before the one with the given name.</summary>
    public void Before(string beforeName, string ruleName, TRule fn, IEnumerable<string> alt = null)
    {
        var index = Find(beforeName);
        if (index == -1) { throw new InvalidOperationException("Parser rule not found: " + beforeName); }
        _rules.Insert(index, new RuleEntry { Name = ruleName, Enabled = true, Fn = fn, Alt = alt?.ToList() ?? new List<string>() });
        _cache = null;
    }

    /// <summary>Adds a new rule after the one with the given name.</summary>
    public void After(string afterName, string ruleName, TRule fn, IEnumerable<string> alt = null)
    {
        var index = Find(afterName);
        if (index == -1) { throw new InvalidOperationException("Parser rule not found: " + afterName); }
        _rules.Insert(index + 1, new RuleEntry { Name = ruleName, Enabled = true, Fn = fn, Alt = alt?.ToList() ?? new List<string>() });
        _cache = null;
    }

    /// <summary>Appends a new rule to the end of the chain.</summary>
    public void Push(string ruleName, TRule fn, IEnumerable<string> alt = null)
    {
        _rules.Add(new RuleEntry { Name = ruleName, Enabled = true, Fn = fn, Alt = alt?.ToList() ?? new List<string>() });
        _cache = null;
    }

    /// <summary>Enables rules by name, returning the names that were found.</summary>
    public List<string> Enable(IEnumerable<string> list, bool ignoreInvalid = false)
    {
        var result = new List<string>();
        foreach (var name in list)
        {
            var idx = Find(name);
            if (idx < 0)
            {
                if (ignoreInvalid) { continue; }
                throw new InvalidOperationException("Rules manager: invalid rule name " + name);
            }
            _rules[idx].Enabled = true;
            result.Add(name);
        }
        _cache = null;
        return result;
    }

    /// <summary>Enables only the named rules, disabling everything else.</summary>
    public void EnableOnly(IEnumerable<string> list, bool ignoreInvalid = false)
    {
        foreach (var rule in _rules) { rule.Enabled = false; }
        Enable(list, ignoreInvalid);
    }

    /// <summary>Disables rules by name, returning the names that were found.</summary>
    public List<string> Disable(IEnumerable<string> list, bool ignoreInvalid = false)
    {
        var result = new List<string>();
        foreach (var name in list)
        {
            var idx = Find(name);
            if (idx < 0)
            {
                if (ignoreInvalid) { continue; }
                throw new InvalidOperationException("Rules manager: invalid rule name " + name);
            }
            _rules[idx].Enabled = false;
            result.Add(name);
        }
        _cache = null;
        return result;
    }

    /// <summary>Returns the active rules for a chain ("" is the default chain).</summary>
    public TRule[] GetRules(string chainName)
    {
        if (_cache == null) { Compile(); }
        return _cache.TryGetValue(chainName, out var rules) ? rules : Array.Empty<TRule>();
    }
}
