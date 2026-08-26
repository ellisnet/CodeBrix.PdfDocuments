#!/usr/bin/env python3
# Makes MathJax_AMS-CID.otf from MathJax_AMS-Regular.otf: the same 261 glyphs, the same
# charstrings and local subroutines, re-expressed as a CID-KEYED CFF program - one Font
# DICT in an FDArray, an FDSelect (format 3) pointing every glyph at it, ROS
# Adobe-Identity-0 and identity CIDs - so that the CFF subsetter's CID branch
# (FDArray / FDSelect / per-FD Private DICT) has a real fixture and a fontTools oracle.
# Needs fontTools (4.57 was used on 2026-08-26). Run from this folder:
#     python3 make-mathjax-cid.py
# The derived file is Apache-2.0 like its source; see MathJax_AMS-Regular.NOTICE.txt.
from fontTools.ttLib import TTFont
from fontTools.cffLib import FDArrayIndex, FontDict, FDSelect

f = TTFont('MathJax_AMS-Regular.otf')
cff = f['CFF '].cff
top = cff.topDictIndex[0]
cs = top.CharStrings
n = len(top.charset)

# fontTools models a CID-keyed font's charset as glyph names of the form cidNNNNN.
old = list(top.charset)
new = ['.notdef'] + ['cid%05d' % i for i in range(1, n)]
for o, nn in zip(old, new):
    if o != nn:
        cs.charStrings[nn] = cs.charStrings.pop(o)
cs.charset = new
top.charset = new

fd = FontDict()
fd.Private = top.Private
fd.FontName = 'MathJaxAMSCid'
fda = FDArrayIndex()
fda.append(fd)
top.FDArray = fda
fds = FDSelect()
fds.format = 3
fds.gidArray = [0] * n
top.FDSelect = fds
top.ROS = ('Adobe', 'Identity', 0)
top.CIDCount = n
delattr(top, 'Private')
for k in ('Private', 'Encoding'):
    top.rawDict.pop(k, None)
if hasattr(top, 'Encoding'):
    delattr(top, 'Encoding')
cff.fontNames = ['MathJaxAMSCid']
for rec in f['name'].names:
    try:
        s = rec.toUnicode()
        if 'MathJax_AMS' in s:
            rec.string = s.replace('MathJax_AMS', 'MathJaxAMSCid')
    except Exception:
        pass
f.save('MathJax_AMS-CID.otf')

g = TTFont('MathJax_AMS-CID.otf')
t = g['CFF '].cff.topDictIndex[0]
print('CID-keyed:', hasattr(t, 'FDArray'), 'FDs', len(t.FDArray), 'glyphs', len(t.CharStrings),
      'CFF bytes', len(g.reader['CFF ']), 'offsets', {k: v for k, v in t.rawDict.items() if k in ('charset', 'CharStrings', 'FDArray', 'FDSelect')},
      'FD0 Private', t.FDArray[0].rawDict.get('Private'))
