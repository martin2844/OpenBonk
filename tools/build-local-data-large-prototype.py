#!/usr/bin/env python3
"""Experimental local generator for 758 textures, 422 meshes and 118 clips.

This version includes two lossy reconstructed normal maps. Its template and
outputs still contain game content and are not distributable artifacts.
"""
import argparse,hashlib,json,shutil,subprocess,sys,tempfile
from pathlib import Path
PINS={'large':'260b8deaeb24d29884439084cc74db2f3b67be55c5a9e3ac4bff733306c92a4a','normal':'a77518d629dab39b20349704f981fb1a2958d1833fc4f6f72ad542f3cd782981','exact':'35019740b8aecf8a1cc3858779b52636f5fb8c9aeba63ccba02a618d434e6c2b','alpha':'5f3763ac936f55a5fd0a173f87560bf8cdb8d0e31d966d77a64a0641360f033e','bc':'0515842976c3828b66269cfd1554f7c1b539e2f8bb5b2223627be400d2ffd53b','final':'549a5ee239c912713ebf5386f70f2355a5ef176b642733dc1be350e800221e18','audio':'93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6'}
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def check(directory,key):
 if sha(directory/'sharedassets1.assets')!=PINS[key]:raise ValueError(f'{key} digest mismatch')
def modify(path,updates):
 m=json.loads(path.read_text());m.update(updates);path.write_text(json.dumps(m,indent=2)+'\n')
p=argparse.ArgumentParser(description=__doc__)
for n in ('pc_data','template','direct_manifest','rgba_manifest','etc_manifest','bc_manifest','additional_manifest','output_dir'):p.add_argument(n,type=Path)
a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
scripts=Path(__file__).resolve().parent.parent/'analysis/data-conversion'
def run(n,*v):subprocess.run([sys.executable,str(scripts/n),*[str(x) for x in v]],check=True)
with tempfile.TemporaryDirectory(prefix='.openbonk-normal-',dir=a.output_dir.parent) as tmp:
 tmp=Path(tmp);large=tmp/'large';normal=tmp/'normal';exact=tmp/'exact';alpha=tmp/'alpha';bc=tmp/'bc';outline=tmp/'outline';mesh=tmp/'mesh';stage=tmp/'stage'
 run('large-textures-staged-prototype.py','restore',a.pc_data,a.template,large);check(large,'large')
 modify(large/'normal.json',{'template_sha256':PINS['large']})
 run('normal-map-staged-prototype.py','restore',a.pc_data,large,normal);check(normal,'normal')
 modify(normal/'exact-renamed.json',{'base_sha256':PINS['exact'],'stripped_sha256':PINS['normal']})
 run('exact-renamed-large-staged-prototype.py','restore',a.pc_data,normal,exact);check(exact,'exact')
 modify(exact/'manifest.json',{'target_sha256':PINS['exact'],'template_sha256':PINS['exact']})
 run('alpha8-large-staged-prototype.py','restore',a.pc_data,exact,alpha);check(alpha,'alpha')
 modify(alpha/'additional.json',{'stripped_sha256':PINS['alpha']})
 run('strip-bc-rgba-template.py','restore',a.pc_data,alpha,a.additional_manifest,bc);check(bc,'bc')
 modify(bc/'outline.json',{'template_sha256':PINS['bc']})
 run('rebuild-outline-bc-prototype.py','restore',a.pc_data,bc,outline)
 run('cross-scene-mesh-large-staged-prototype.py','restore',a.pc_data,outline,mesh)
 run('combined-texture-large-outline-prototype.py','restore',a.pc_data,mesh,a.direct_manifest,a.rgba_manifest,a.etc_manifest,a.bc_manifest,stage);check(stage,'final')
 run('build-pc-audio-resource-prototype.py',a.pc_data,mesh/'sharedassets1.assets',stage/'sharedassets1.resource')
 if sha(stage/'sharedassets1.resource')!=PINS['audio']:raise ValueError('audio digest mismatch')
 a.output_dir.mkdir()
 try:
  for n in ('sharedassets1.assets','sharedassets1.resource'):shutil.move(str(stage/n),str(a.output_dir/n))
 except Exception:shutil.rmtree(a.output_dir);raise
print('Built local experimental data: 758 textures, 422 mesh buffers, 118 audio streams; not for distribution.')
