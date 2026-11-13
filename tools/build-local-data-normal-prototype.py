#!/usr/bin/env python3
"""Experimental local generator for 756 textures, 422 meshes and 118 clips.

This version includes two lossy reconstructed normal maps. Its template and
outputs still contain game content and are not distributable artifacts.
"""
import argparse,hashlib,json,shutil,subprocess,sys,tempfile
from pathlib import Path
PINS={'normal':'29f4710de970c454f2e3d9260898d1b2a1c73f8e256052c5aca9d2c621bdba7c','exact':'b48fbbace26a0acb206ad61c047284835375552fb167d03306d1d3368b0f1c7d','alpha':'dfc908b0e8769b14ebeba47e65aca0d79ba04f4cc8300e68dec4472651254d83','bc':'472eaed7761456acd4731ca965f3378e8dddc4f1af98129600a081c3b172f27c','final':'7c4707a77cb58d5a15f8ee6768dbbbd952fd3ec5f4065ad7552da5aedd55ab93','audio':'93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6'}
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
 tmp=Path(tmp);normal=tmp/'normal';exact=tmp/'exact';alpha=tmp/'alpha';bc=tmp/'bc';outline=tmp/'outline';mesh=tmp/'mesh';stage=tmp/'stage'
 run('normal-map-staged-prototype.py','restore',a.pc_data,a.template,normal);check(normal,'normal')
 modify(normal/'exact-renamed.json',{'base_sha256':PINS['exact'],'stripped_sha256':PINS['normal']})
 run('exact-renamed-normal-staged-prototype.py','restore',a.pc_data,normal,exact);check(exact,'exact')
 modify(exact/'manifest.json',{'target_sha256':PINS['exact'],'template_sha256':PINS['exact']})
 run('alpha8-normal-staged-prototype.py','restore',a.pc_data,exact,alpha);check(alpha,'alpha')
 modify(alpha/'additional.json',{'stripped_sha256':PINS['alpha']})
 run('strip-bc-rgba-template.py','restore',a.pc_data,alpha,a.additional_manifest,bc);check(bc,'bc')
 modify(bc/'outline.json',{'template_sha256':PINS['bc']})
 run('rebuild-outline-bc-prototype.py','restore',a.pc_data,bc,outline)
 run('cross-scene-mesh-normal-staged-prototype.py','restore',a.pc_data,outline,mesh)
 run('combined-texture-normal-outline-prototype.py','restore',a.pc_data,mesh,a.direct_manifest,a.rgba_manifest,a.etc_manifest,a.bc_manifest,stage);check(stage,'final')
 run('build-pc-audio-resource-prototype.py',a.pc_data,mesh/'sharedassets1.assets',stage/'sharedassets1.resource')
 if sha(stage/'sharedassets1.resource')!=PINS['audio']:raise ValueError('audio digest mismatch')
 a.output_dir.mkdir()
 try:
  for n in ('sharedassets1.assets','sharedassets1.resource'):shutil.move(str(stage/n),str(a.output_dir/n))
 except Exception:shutil.rmtree(a.output_dir);raise
print('Built local experimental data: 756 textures, 422 mesh buffers, 118 audio streams; not for distribution.')
