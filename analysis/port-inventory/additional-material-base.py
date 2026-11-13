#!/usr/bin/env python3
"""Blank/recover three sets of Steam-derived material values in a local pair.

Original and third-party material remain. This is not distributable.
"""
import argparse,copy,hashlib,importlib.util,json,os,shutil,subprocess,sys,tempfile,zipfile,zlib
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('repair',Path(__file__).with_name('repair-split-guid-probe.py'));repair=importlib.util.module_from_spec(spec);spec.loader.exec_module(repair)
OLD_APK='f756235db25a5aed06afb34ec7e0267aa49cf770eb148e0e3cd048674b13e93a';OLD_OBB='caba193e78eef5c4b2b4d4e5dd066306584ef151832e5f4c9f9d1ecfb4ca64fb'
OLD_COMMENT=b'0000001d'
MANIFESTS={0:'8214d9517d17771a29347983f34c2916eba126f6f70cbcd82b9b43bc5899b976',2:'04f6eb9faac05dddd8e5ce4650733b666eb09bc430c98e00fe43be7740804a0f',4:'7bcd0832f3d79e5a2cbcda3fecc2deb6ab487d7ea03ac1c8a4d3102631aebe84'}
NEW_APK='5dbd643c996c805e6e58815fd00be3f61737fb54fabf27aa610c07d39427c460';NEW_OBB='ca62c7846a0eccbc058c3976293018c701260a4c0dce697dfcd03c9013702d9b'
def sha(b):return hashlib.sha256(b).hexdigest()
def fragment_names(z,i):
 prefix='assets/bin/Data/sharedassets%d.assets.split'%i
 names=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
 if not names or [int(n[len(prefix):]) for n in names]!=list(range(len(names))):raise ValueError('invalid fragments')
 return names

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('base',type=Path);p.add_argument('output',type=Path);p.add_argument('--steam-data',type=Path);p.add_argument('--windows-source',action='store_true',help='use pinned Windows material data for restore');a=p.parse_args();out=a.output.absolute()
 if out.exists() or out.is_symlink() or not out.parent.is_dir():p.error('output must be new')
 if (a.action=='restore')!=(a.steam_data is not None):p.error('restore requires --steam-data')
 if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
 for src in [a.base]+([a.steam_data] if a.steam_data else []):
  src=src.resolve()
  if out.resolve().is_relative_to(src) or src.is_relative_to(out.resolve()):p.error('output must be separate from inputs')
 expected_apk=OLD_APK if a.action=='template' else NEW_APK;expected_obb=OLD_OBB if a.action=='template' else NEW_OBB
 for path,h in ((a.base/'base.apk',expected_apk),(a.base/'base.obb',expected_obb)):
  if not path.is_file() or repair.sha(path)!=h:raise ValueError('incompatible base: '+str(path))
 templates={i:(ROOT/'build/android'/f'additional-material-template-{i}' if a.action=='template' else a.base/f'material-extra-{i}') for i in MANIFESTS}
 for i,folder in templates.items():
  if repair.sha(folder/'manifest.json')!=MANIFESTS[i]:raise ValueError('incompatible manifest')
 with tempfile.TemporaryDirectory(prefix='.material-pair-',dir=out.parent) as t:
  t=Path(t);new_apk=t/'base.apk';new_obb=t/'base.obb';apk_repl={};obb_repl={};stats={}
  with zipfile.ZipFile(a.base/'base.apk') as az,zipfile.ZipFile(a.base/'base.obb') as oz:
   for z in (az,oz):repair.unique(z)
   if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('base GUID differs')
   if a.action=='template' and oz.comment!=OLD_COMMENT:raise ValueError('unexpected old comment')
   for i,folder in templates.items():
    z=az if i==0 else oz;names=fragment_names(z,i);raw=b''.join(z.read(n) for n in names);doc=json.loads((folder/'manifest.json').read_text());name=f'sharedassets{i}.assets'
    if a.action=='template':
     blank=(folder/name).read_bytes()
     if sha(raw)!=doc['original_sha256'] or sha(blank)!=doc['template_sha256'] or len(raw)!=len(blank):raise ValueError('material template differs')
     objects={o.path_id:o for o in UnityPy.load(raw).objects};spans=[]
     for row in doc['objects']:
      obj=objects[row['target_id']]
      if obj.type.name!='Material':raise ValueError('wrong type')
      start=obj.byte_start+obj.byte_size-row['length'];end=obj.byte_start+obj.byte_size
      if start<obj.byte_start or sha(raw[start:end])!=row['value_sha256']:raise ValueError('invalid material values')
      spans.append((start,end))
     cursor=0;nonzero=0
     for start,end in sorted(spans):
      if start<cursor or raw[cursor:start]!=blank[cursor:start] or any(blank[start:end]):raise ValueError('outside values differ')
      nonzero+=sum(x!=0 for x in raw[start:end]);cursor=end
     if raw[cursor:]!=blank[cursor:] or nonzero!=doc['nonzero_bytes']:raise ValueError('invalid material template')
     result=blank
    else:
     stage=t/f'template-{i}';stage.mkdir();(stage/name).write_bytes(raw);shutil.copyfile(folder/'manifest.json',stage/'manifest.json');restored=t/f'restored-{i}'
     subprocess.run([sys.executable,str(ROOT/'analysis/data-conversion/shared1-material-values-template.py'),'restore',str(a.steam_data),str(stage),str(restored),'--asset',name]+(['--windows-source'] if a.windows_source else []),check=True)
     result=(restored/name).read_bytes()
     if sha(result)!=doc['original_sha256']:raise ValueError('recovered asset differs')
    replacements=apk_repl if i==0 else obb_repl;pos=0
    for part in names:
     length=z.getinfo(part).file_size;replacements[part]=result[pos:pos+length];pos+=length
    stats[str(i)]={'objects':len(doc['objects']),'selected_bytes':doc['selected_bytes'],'nonzero_bytes':doc['nonzero_bytes']}
   dex=az.read('classes.dex');previous,following=repair.dex_bounds(dex,expected_obb.encode())
   with zipfile.ZipFile(new_obb,'x',allowZip64=True) as dst:
    for info in oz.infolist():dst.writestr(copy.copy(info),obb_repl.get(info.filename,oz.read(info)))
    dst.comment=b'00000000' if a.action=='template' else OLD_COMMENT
   if a.action=='template':
    with new_obb.open('rb') as f:
     h=hashlib.sha256();remaining=new_obb.stat().st_size-8
     while remaining:
      b=f.read(min(1048576,remaining))
      if not b:raise ValueError('short OBB')
      h.update(b);remaining-=len(b)
    for nonce in range(1000000):
     c=f'{nonce:08x}'.encode();x=h.copy();x.update(c);new_hash=x.hexdigest().encode()
     if previous<new_hash<following:break
    else:raise ValueError('cannot preserve DEX string ordering')
    with new_obb.open('r+b') as f:f.seek(-8,2);f.write(c)
   else:new_hash=OLD_OBB.encode()
   if repair.sha(new_obb).encode()!=new_hash or not previous<new_hash<following:raise ValueError('output OBB differs')
   patched=bytearray(dex.replace(expected_obb.encode(),new_hash));patched[12:32]=hashlib.sha1(patched[32:]).digest();patched[8:12]=(zlib.adler32(patched[12:])&0xffffffff).to_bytes(4,'little')
   with zipfile.ZipFile(new_apk,'x',allowZip64=True) as dst:
    for info in az.infolist():
     if not repair.jar_signature(info.filename):dst.writestr(copy.copy(info),bytes(patched) if info.filename=='classes.dex' else apk_repl.get(info.filename,az.read(info)))
  if a.action=='restore' and repair.sha(new_apk)!=OLD_APK:raise ValueError('restored APK differs')
  with zipfile.ZipFile(new_apk) as az,zipfile.ZipFile(new_obb) as oz:
   for z in (az,oz):repair.unique(z)
   if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid') or az.read('classes.dex').count(new_hash)!=1:raise ValueError('output pair mismatch')
  out.mkdir(mode=0o700)
  try:
   for f in a.base.iterdir():
    if f.is_file() and f.name not in ('base.apk','base.obb','report.json'):shutil.copyfile(f,out/f.name)
   shutil.copyfile(new_apk,out/'base.apk');shutil.copyfile(new_obb,out/'base.obb')
   if a.action=='template':
    for i,folder in templates.items():
     target=out/f'material-extra-{i}';target.mkdir();shutil.copyfile(folder/'manifest.json',target/'manifest.json')
   report={'kind':'local research only, not for distribution','apk_sha256':repair.sha(new_apk),'obb_sha256':repair.sha(new_obb),'materials':stats,'limitations':['Other original game and third-party data remain.']}
   (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
  except BaseException:shutil.rmtree(out);raise
 print(json.dumps(report,indent=2))
if __name__=='__main__':main()
