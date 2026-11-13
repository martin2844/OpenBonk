#!/usr/bin/env python3
"""Compare two research OBBs; require only the three water probe files absent."""
from hashlib import sha256
from pathlib import Path
import sys
from zipfile import ZipFile

REMOVED = {'assets/bin/Data/' + n for n in ('df272285d21fd527096b3c5d8a8c9adf', 'baf39c96675258361b63538c61a6f54e', 'f85e78285b9fb8553bc9aff72231fd4d')}
if len(sys.argv) != 3: raise ValueError('usage: SCRIPT ORIGINAL REDUCED')
a, b = map(Path, sys.argv[1:])
with ZipFile(a) as original, ZipFile(b) as reduced:
 old, new = original.namelist(), reduced.namelist()
 if len(old)!=len(set(old)) or len(new)!=len(set(new)) or set(old)-set(new)!=REMOVED or set(new)-set(old): raise ValueError('unexpected inventory difference')
 for name in new:
  if sha256(original.read(name)).digest()!=sha256(reduced.read(name)).digest(): raise ValueError('content differs: '+name)
 print('PASS:',len(new),'entries match; exactly three probe files removed')
with b.open('rb') as f: print('reduced SHA-256:',sha256(f.read()).hexdigest())
