#!/usr/bin/env python3
"""Rebuild a pinned local data pair with 742 textures and 422 mesh buffers.

Research only: the template still retains other game data and the output is
not a distributable installer. Requires a specific Steam build and template.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

RESTORED_TEMPLATE='3eb596fab122925c5d5f39be61a4b29dbfd780fb0d490947428719986eb741a1'
EXPECTED_ASSET='9110d3bb6b658c5582e4f0c3fb45e17b6599bc7f002aab161e1c2c854d0a0744'
EXPECTED_AUDIO='93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6'
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for part in iter(lambda:f.read(1024*1024),b''):h.update(part)
 return h.hexdigest()
p=argparse.ArgumentParser(description=__doc__)
for name in ('pc_data','stripped_template','direct_manifest','rgba_manifest','etc_manifest','bc_manifest','additional_manifest','output_dir'):
 p.add_argument(name,type=Path)
a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and its parent must exist')
root=Path(__file__).resolve().parent.parent;scripts=root/'analysis/data-conversion'
def run(name,*args):subprocess.run([sys.executable,str(scripts/name),*[str(x) for x in args]],check=True)
with tempfile.TemporaryDirectory(prefix='.openbonk-clean-',dir=a.output_dir.parent) as tmp:
 tmp=Path(tmp);early=tmp/'early';outlines=tmp/'outlines';meshes=tmp/'meshes';stage=tmp/'data'
 run('strip-bc-rgba-template.py','restore',a.pc_data,a.stripped_template,a.additional_manifest,early)
 if sha(early/'sharedassets1.assets')!=RESTORED_TEMPLATE:raise ValueError('unexpected reconstructed input template')
 outline=early/'outline.json';m=json.loads(outline.read_text());m['template_sha256']=RESTORED_TEMPLATE;outline.write_text(json.dumps(m,indent=2)+'\n')
 run('rebuild-outline-bc-prototype.py','restore',a.pc_data,early,outlines)
 run('cross-scene-mesh-bc-rgba-staged-prototype.py','restore',a.pc_data,outlines,meshes)
 run('combined-texture-bc-rgba-outline-prototype.py','restore',a.pc_data,meshes,a.direct_manifest,a.rgba_manifest,a.etc_manifest,a.bc_manifest,stage)
 run('build-pc-audio-resource-prototype.py',a.pc_data,meshes/'sharedassets1.assets',stage/'sharedassets1.resource')
 if sha(stage/'sharedassets1.assets')!=EXPECTED_ASSET or sha(stage/'sharedassets1.resource')!=EXPECTED_AUDIO:raise ValueError('unexpected final data hash')
 a.output_dir.mkdir()
 try:
  for name in ('sharedassets1.assets','sharedassets1.resource'):shutil.move(str(stage/name),str(a.output_dir/name))
 except Exception:
  shutil.rmtree(a.output_dir);raise
print('Built 742 textures, 422 mesh buffers and 118 audio streams from local Steam data.')
print('Template and outputs still contain other game data; not for distribution.')
