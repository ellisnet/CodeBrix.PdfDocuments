#!/usr/bin/env python3
# Makes MathJax_AMS-Encoded.otf from MathJax_AMS-Regular.otf so that the CFF subsetter's
# two remaining structural branches have a real fixture:
#
#   * a CUSTOM ENCODING table, rather than the predefined Standard encoding every other
#     fixture here carries, so the subsetter's Encoding slicing is exercised; and
#   * a SEAC COMPOSITE - the deprecated four-operand form of endchar, which draws an
#     accented glyph out of two others named by StandardEncoding code. Nothing in the
#     URW or MathJax families uses it, so without this fixture the seac closure would
#     ship unexercised.
#
# 'Aacute' is added as the composite (bchar 65 = 'A', achar 194 = 'acute') and one spare
# glyph is renamed 'acute' to be its accent, which also gives that glyph the standard SID
# 125 the closure looks the component up by.
#
# Needs fontTools (4.57 was used on 2026-08-30). Run from this folder:
#     python3 make-mathjax-encoded.py
# The derived file is Apache-2.0 like its source; see MathJax_AMS-Regular.NOTICE.txt.
from fontTools.ttLib import TTFont
from fontTools.misc.psCharStrings import T2CharString

f = TTFont('MathJax_AMS-Regular.otf')
cff = f['CFF '].cff
top = cff.topDictIndex[0]
cs = top.CharStrings
names = list(top.charset)

# A spare glyph becomes 'acute', which is a CFF standard string (SID 125).
spare = names[-1]
cs.charStrings['acute'] = cs.charStrings.pop(spare)
names[names.index(spare)] = 'acute'

# The seac composite: adx ady bchar achar endchar.
composite = T2CharString(private=top.Private, globalSubrs=cff.GlobalSubrs)
composite.program = [0, 0, 65, 194, 'endchar']
cs.charStringsIndex.append(composite)
cs.charStrings['Aacute'] = len(cs.charStringsIndex) - 1
names.append('Aacute')
top.charset = names

# A custom Encoding. Only glyphs EARLY in the charset are given codes, because CFF
# Encoding format 0 counts entries from the start of the charset to the last encoded
# glyph and that count is a single byte.
encoding = ['.notdef'] * 256
for code, name in ((65, 'A'), (66, 'B'), (67, 'C'), (68, 'D')):
    encoding[code] = name
top.Encoding = encoding

for rec in f['name'].names:
    try:
        s = rec.toUnicode()
        if 'MathJax_AMS' in s:
            rec.string = s.replace('MathJax_AMS', 'MathJaxAMSEnc')
    except Exception:
        pass
cff.fontNames = ['MathJaxAMSEnc']
f.save('MathJax_AMS-Encoded.otf')

g = TTFont('MathJax_AMS-Encoded.otf')
t = g['CFF '].cff.topDictIndex[0]
print('glyphs', len(t.CharStrings), '| Aacute', t.CharStrings['Aacute'].program,
      '| acute present', 'acute' in t.charset,
      '| Encoding is a table', isinstance(t.Encoding, list),
      '| CFF bytes', len(g.reader['CFF ']))
