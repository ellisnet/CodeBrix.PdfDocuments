using System;
using System.Collections.Generic;

namespace CodeBrix.PdfDocuments.Fonts.CompactFontFormat;

/// <summary>
/// Walks Type 2 charstrings (Adobe TN #5177) to find out what a set of glyphs actually
/// depends on: which subroutines they reach, and which further glyphs they draw with.
/// </summary>
/// <remarks>
/// <para>
/// Two closures are collected. The SUBROUTINE closure follows <c>callsubr</c> and
/// <c>callgsubr</c>, whose operand is the subroutine's index less a bias that depends on
/// how many subroutines its INDEX holds, and follows them again out of every subroutine it
/// reaches. The <c>seac</c> closure catches the deprecated four-operand form of
/// <c>endchar</c>, which builds an accented glyph out of two others named by their
/// StandardEncoding codes: a document that uses the composite uses those two components
/// without ever naming them.
/// </para>
/// <para>
/// ⚠ <c>callsubr</c> POPS ONLY THE SUBROUTINE NUMBER. Everything else on the operand stack
/// stays there and the subroutine goes on to use it - in the URW faces this library is most
/// often asked to embed, about seven calls in ten pass arguments that way, and those
/// arguments are frequently stem hints. A walker that entered a subroutine with an empty
/// stack would count the hints it declares as zero, then read the wrong number of mask
/// bytes at the next <c>hintmask</c> and lose the rest of the charstring. So the stack is
/// carried through calls, and a subroutine is walked again at each call rather than once.
/// </para>
/// <para>
/// ⚠ THE SAME FACT IS WHY NOTHING HERE REWRITES A CHARSTRING. How many mask bytes a
/// subroutine's <c>hintmask</c> takes depends on the stack its CALLER left, and one
/// subroutine can be entered from two glyphs with two different stacks - so a pass that
/// visited each subroutine once, on its own, could not parse it reliably. Walking from the
/// glyphs always has the real state. That rules out RENUMBERING the subroutines, which is
/// why <see cref="CffSubsetter.CreateCompactSubset"/> empties the slots of the ones nothing
/// reaches and keeps the count - so every bias, and every operand written against it, still
/// means what it meant.
/// </para>
/// </remarks>
internal sealed class CffCharStringScanner
{
    // The Type 2 operators this walker has to understand; every other one clears the stack.
    const int OpHStem = 1;
    const int OpVStem = 3;
    const int OpCallSubr = 10;
    const int OpReturn = 11;
    const int OpEscape = 12;
    const int OpEndChar = 14;
    const int OpHStemHm = 18;
    const int OpHintMask = 19;
    const int OpCntrMask = 20;
    const int OpVStemHm = 23;
    const int OpCallGSubr = 29;

    /// <summary>The subroutine nesting limit Adobe TN #5177 sets.</summary>
    const int MaxDepth = 10;

    /// <summary>
    /// How many operators one scan may walk before it is abandoned. A subroutine is walked
    /// again at every call - it has to be, because what it does depends on the stack it
    /// inherits - so this bounds a font built to make that expensive.
    /// </summary>
    const int MaxSteps = 40_000_000;

    readonly byte[] _data;
    readonly CffIndex _globalSubrs;
    readonly Dictionary<CffIndex, HashSet<int>> _usedLocalSubrs = new Dictionary<CffIndex, HashSet<int>>();
    int _steps;

    /// <summary>Initializes a scanner over a CFF program.</summary>
    /// <param name="data">The complete CFF program.</param>
    /// <param name="globalSubrs">The Global Subr INDEX.</param>
    public CffCharStringScanner(byte[] data, CffIndex globalSubrs)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _globalSubrs = globalSubrs ?? throw new ArgumentNullException(nameof(globalSubrs));
    }

    /// <summary>The global subroutines the walked charstrings reach, by index.</summary>
    public HashSet<int> UsedGlobalSubrs { get; } = new HashSet<int>();

    /// <summary>The StandardEncoding codes named by a <c>seac</c>-form <c>endchar</c>.</summary>
    public HashSet<int> SeacCodes { get; } = new HashSet<int>();

    /// <summary>
    /// True when a charstring could not be walked to its end - a call nested past the
    /// spec's limit, a charstring reaching outside the program, or a scan that ran past
    /// <see cref="MaxSteps"/>. The closure is then incomplete and must not be acted on.
    /// </summary>
    public bool Failed { get; private set; }

    /// <summary>The local subroutines of the specified INDEX that the walked charstrings reach.</summary>
    /// <param name="localSubrs">The Local Subr INDEX, or null.</param>
    /// <returns>The used indices; empty when the INDEX is null or nothing reached it.</returns>
    public HashSet<int> UsedLocalSubrs(CffIndex localSubrs)
    {
        HashSet<int> used;
        if (localSubrs != null && _usedLocalSubrs.TryGetValue(localSubrs, out used))
            return used;
        return new HashSet<int>();
    }

    /// <summary>
    /// Walks one glyph's charstring, adding everything it reaches to the closures.
    /// </summary>
    /// <param name="start">The charstring's first byte.</param>
    /// <param name="end">The position just past the charstring.</param>
    /// <param name="localSubrs">The Local Subr INDEX that applies to this glyph, or null.</param>
    public void Collect(int start, int end, CffIndex localSubrs)
    {
        Walk(start, end, localSubrs, new WalkState(), 0);
    }

    /// <summary>The bias added to a subroutine operand, from the number of subroutines.</summary>
    /// <param name="count">The number of subroutines in the INDEX.</param>
    /// <returns>The bias, per Adobe TN #5177.</returns>
    public static int Bias(int count)
    {
        if (count < 1240)
            return 107;
        return count < 33900 ? 1131 : 32768;
    }

    // The interpreter state that survives a call: callsubr pops the subroutine number and
    // nothing else, so both of these are carried in and back out again.
    sealed class WalkState
    {
        public readonly List<double> Stack = new List<double>();
        public int Hints;
    }

    void Walk(int start, int end, CffIndex localSubrs, WalkState state, int depth)
    {
        if (depth > MaxDepth || start < 0 || end > _data.Length || end < start)
        {
            Failed = true;
            return;
        }

        int pos = start;
        while (pos < end)
        {
            if (++_steps > MaxSteps)
            {
                Failed = true;
                return;
            }

            int b0 = _data[pos];
            if (b0 == 28)
            {
                state.Stack.Add((short)((_data[pos + 1] << 8) | _data[pos + 2]));
                pos += 3;
                continue;
            }
            if (b0 >= 32)
            {
                if (b0 <= 246)
                {
                    state.Stack.Add(b0 - 139);
                    pos += 1;
                }
                else if (b0 <= 250)
                {
                    state.Stack.Add((b0 - 247) * 256 + _data[pos + 1] + 108);
                    pos += 2;
                }
                else if (b0 <= 254)
                {
                    state.Stack.Add(-(b0 - 251) * 256 - _data[pos + 1] - 108);
                    pos += 2;
                }
                else
                {
                    int fixed1616 = (_data[pos + 1] << 24) | (_data[pos + 2] << 16) | (_data[pos + 3] << 8) | _data[pos + 4];
                    state.Stack.Add(fixed1616 / 65536.0);
                    pos += 5;
                }
                continue;
            }

            switch (b0)
            {
                case OpHStem:
                case OpVStem:
                case OpHStemHm:
                case OpVStemHm:
                    // An odd operand count means the first one is a width, and the integer
                    // division discards it - which is what a reader does with it too.
                    state.Hints += state.Stack.Count / 2;
                    state.Stack.Clear();
                    pos += 1;
                    break;

                case OpHintMask:
                case OpCntrMask:
                    // Operands standing here are an implicit vstem.
                    state.Hints += state.Stack.Count / 2;
                    state.Stack.Clear();
                    pos += 1 + (state.Hints + 7) / 8;
                    break;

                case OpCallSubr:
                case OpCallGSubr:
                    pos += 1;
                    Call(b0 == OpCallGSubr, localSubrs, state, depth);
                    break;

                case OpReturn:
                    return;

                case OpEndChar:
                    // Four trailing operands - five with a leading width - is the seac form.
                    if (state.Stack.Count >= 4)
                    {
                        SeacCodes.Add((int)state.Stack[state.Stack.Count - 2]);
                        SeacCodes.Add((int)state.Stack[state.Stack.Count - 1]);
                    }
                    return;

                case OpEscape:
                    state.Stack.Clear();
                    pos += 2;
                    break;

                default:
                    state.Stack.Clear();
                    pos += 1;
                    break;
            }
        }
    }

    void Call(bool global, CffIndex localSubrs, WalkState state, int depth)
    {
        CffIndex subrs = global ? _globalSubrs : localSubrs;
        int index = 0;
        bool resolved = false;
        if (state.Stack.Count > 0 && subrs != null)
        {
            index = (int)Math.Round(state.Stack[state.Stack.Count - 1]) + Bias(subrs.Count);
            resolved = index >= 0 && index < subrs.Count;
        }
        // The subroutine number is the ONLY thing the call consumes.
        if (state.Stack.Count > 0)
            state.Stack.RemoveAt(state.Stack.Count - 1);
        if (!resolved)
        {
            // A call whose operand names nothing this INDEX holds could not have run in the
            // original font either, so it is not followed - but the walk of this charstring
            // is now guesswork, and the closure it produces cannot be trusted.
            if (subrs == null || state.Stack.Count > 0)
                Failed = true;
            return;
        }
        (global ? UsedGlobalSubrs : UsedLocalSubrsOf(localSubrs)).Add(index);
        Walk(subrs.ItemStart(index), subrs.ItemEnd(index), localSubrs, state, depth + 1);
    }

    HashSet<int> UsedLocalSubrsOf(CffIndex localSubrs)
    {
        HashSet<int> used;
        if (!_usedLocalSubrs.TryGetValue(localSubrs, out used))
        {
            used = new HashSet<int>();
            _usedLocalSubrs[localSubrs] = used;
        }
        return used;
    }

    /// <summary>
    /// The StandardEncoding code to SID table (Adobe TN #5176 appendices B and C), which is
    /// how a <c>seac</c> composite names its two components.
    /// </summary>
    internal static readonly byte[] StandardEncodingSids =
    {
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
        33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
        49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64,
        65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
        81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110,
        0, 111, 112, 113, 114, 0, 115, 116, 117, 118, 119, 120, 121, 122, 0, 123,
        0, 124, 125, 126, 127, 128, 129, 130, 131, 0, 132, 133, 0, 134, 135, 136,
        137, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 138, 0, 139, 0, 0, 0, 0, 140, 141, 142, 143, 0, 0, 0, 0,
        0, 144, 0, 0, 0, 145, 0, 0, 146, 147, 148, 149, 0, 0, 0, 0,
    };
}
