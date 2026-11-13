#!/usr/bin/env python3
"""Rebuild 748 selected textures and 422 mesh buffers from local PC data.

Pinned research prototype; its template retains other game resources and is not
for distribution. Audio is also rebuilt locally from 118 original PC streams.
"""
import argparse, hashlib, json, shutil, subprocess, sys, tempfile
from pathlib import Path
FIRST='cbd325c0dc6807acb73a41fc3aa29f54e1f22a5a35d422a6c2b3da29202f312f'
SECOND='9ee7a65bce3673a066edbcfb8980c5c4cb62271c297b32809518ade3cad1fec7'
FINAL='52dcef8997902ac9681a45ead95909b123bc9838584c3a665fb5b162d0a7afbc'
AUDIO='93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
p=argparse.ArgumentParser(description=__doc__)
for n in ('pc_data','template','direct_manifest','rgba_manifest','etc_manifest','bc_manifest','additional_manifest','output_dir'):p.add_argument(n,type=Path)
a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
scripts=Path(__file__).resolve().parent.parent/'analysis/data-conversion'
def run(n,*args):subprocess.run([sys.executable,str(scripts/n),*[str(v) for v in args]],check=True)
with tempfile.TemporaryDirectory(prefix='.openbonk-alpha-',dir=a.output_dir.parent) as tmp:
 tmp=Path(tmp);alpha=tmp/'alpha';bc=tmp/'bc';outline=tmp/'outline';mesh=tmp/'mesh';stage=tmp/'stage'
 run('alpha8-staged-prototype.py','restore',a.pc_data,a.template,alpha)
 if sha(alpha/'sharedassets1.assets')!=FIRST:raise ValueError('unexpected alpha reconstruction')
 p=alpha/'additional.json';m=json.loads(p.read_text());m['stripped_sha256']=FIRST;p.write_text(json.dumps(m,indent=2)+'\n')
 run('strip-bc-rgba-template.py','restore',a.pc_data,alpha,a.additional_manifest,bc)
 if sha(bc/'sharedassets1.assets')!=SECOND:raise ValueError('unexpected BC reconstruction')
 p=bc/'outline.json';m=json.loads(p.read_text());m['template_sha256']=SECOND;p.write_text(json.dumps(m,indent=2)+'\n')
 run('rebuild-outline-bc-prototype.py','restore',a.pc_data,bc,outline)
 run('cross-scene-mesh-alpha8-staged-prototype.py','restore',a.pc_data,outline,mesh)
 run('combined-texture-alpha8-outline-prototype.py','restore',a.pc_data,mesh,a.direct_manifest,a.rgba_manifest,a.etc_manifest,a.bc_manifest,stage)
 run('build-pc-audio-resource-prototype.py',a.pc_data,mesh/'sharedassets1.assets',stage/'sharedassets1.resource')
 if sha(stage/'sharedassets1.assets')!=FINAL or sha(stage/'sharedassets1.resource')!=AUDIO:raise ValueError('unexpected final hashes')
 a.output_dir.mkdir()
 try:
  for name in ('sharedassets1.assets','sharedassets1.resource'):shutil.move(str(stage/name),str(a.output_dir/name))
 except Exception:shutil.rmtree(a.output_dir);raise
print('Built 748 textures, 422 mesh buffers and 118 audio streams; local experiment only.')
