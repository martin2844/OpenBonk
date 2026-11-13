#!/usr/bin/env python3
"""Rebuild 754 selected textures, meshes and audio from pinned local data.

Research only: the template still contains other game data; not a release.
"""
import argparse, subprocess, sys, tempfile
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__)
for name in ('pc_data','template','direct_manifest','rgba_manifest','etc_manifest','bc_manifest','additional_manifest','output_dir'):p.add_argument(name,type=Path)
a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
root=Path(__file__).resolve().parent.parent
with tempfile.TemporaryDirectory(prefix='.openbonk-exact-',dir=a.output_dir.parent) as tmp:
 restored=Path(tmp)/'restored'
 subprocess.run([sys.executable,str(root/'analysis/data-conversion/exact-renamed-textures-prototype.py'),'restore',str(a.pc_data),str(a.template),str(restored)],check=True)
 subprocess.run([sys.executable,str(root/'tools/build-local-data-alpha8-prototype.py'),str(a.pc_data),str(restored),str(a.direct_manifest),str(a.rgba_manifest),str(a.etc_manifest),str(a.bc_manifest),str(a.additional_manifest),str(a.output_dir)],check=True)
print('Rebuilt 754 texture payloads, 422 mesh buffers and 118 audio streams; research only.')
