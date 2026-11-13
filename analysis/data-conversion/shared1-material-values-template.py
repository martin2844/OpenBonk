#!/usr/bin/env python3
"""Reconstruct exact Material numeric/color tails from pinned Steam data.

Only raw equal tails are selected. Android texture and shader references and
all other bytes stay in place. Local research only, not for distribution.
"""
import argparse, hashlib, json, shutil, struct
from collections import defaultdict
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2]
ORIGINAL='db8bc6b858a9c7ecc684b3dd9775e1d08d7091c39f4fab2803471278dfce22cf'
COUNT=397
ASSETS={
 'sharedassets0.assets':('69ae7f8ccd1048553ac88cb0cfd86d599f1e772bc2b443c0263af5e1890ee844',16),
 'sharedassets1.assets':(ORIGINAL,COUNT),
 'sharedassets2.assets':('77d16b6174cf96eaf0565a5d555235de571de38dde7755096b09220e39f8d464',102),
 'sharedassets4.assets':('2b663dc3d4030228841601cc53ccc2202d6d91be599641e5ba1fa690ab291074',3)}
WINDOWS_ASSETS={
 'sharedassets0.assets':(131699616,'772b0a4fc30592c6da17667e532efd0674f76eb7eb5ad24fc5673229b8fe525d'),
 'sharedassets1.assets':(50251684,'64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'),
 'sharedassets2.assets':(5503192,'cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9'),
 'sharedassets4.assets':(693876,'896ae7ac28e9ff8916121322326842369bcc9616e57ddd7764813085775130d9')}

def sha(b):return hashlib.sha256(b).hexdigest()
def name_bytes(name):
 s=name.encode('utf-8');return struct.pack('<I',len(s))+s+bytes(-len(s)%4)
def tail(obj):
 tree=obj.read_typetree();p=tree['m_SavedProperties'];buf=bytearray()
 for key in ('m_Ints','m_Floats','m_Colors'):
  values=p[key];buf.extend(struct.pack('<i',len(values)))
  for name,val in values:
   buf.extend(name_bytes(name))
   if key=='m_Ints':buf.extend(struct.pack('<i',val))
   elif key=='m_Floats':
    buf.extend(struct.pack('<f',val))
   else:
    fields=[val[x] for x in 'rgba']
    buf.extend(struct.pack('<4f',*fields))
 if tree['m_BuildTextureStacks']:raise ValueError('unexpected texture stacks')
 buf.extend(bytes(4));value=bytes(buf)
 if not obj.get_raw_data().endswith(value):raise ValueError('unexpected material layout')
 return value

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('steam_data',type=Path);p.add_argument('input',type=Path);p.add_argument('output',type=Path);p.add_argument('--asset',choices=ASSETS,default='sharedassets1.assets');p.add_argument('--windows-source',action='store_true',help='use pinned Windows material values for restore');a=p.parse_args();out=a.output.absolute()
 if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
 original,count=ASSETS[a.asset]
 if out.exists() or out.is_symlink() or not out.parent.is_dir():p.error('output must be new')
 for src in (a.steam_data,a.input):
  src=src.resolve()
  if out.resolve().is_relative_to(src) or src.is_relative_to(out.resolve()):p.error('output must be separate from inputs')
 pin=json.loads((ROOT/'tools/steam-linux-diagnostic-inputs.json').read_text())[a.asset];pc=(a.steam_data/a.asset).read_bytes()
 expected_size,expected_hash=WINDOWS_ASSETS[a.asset] if a.windows_source else (pin['size'],pin['sha256'])
 if len(pc)!=expected_size or sha(pc)!=expected_hash:raise ValueError('incompatible Steam data')
 idx=defaultdict(list);by_id={}
 for obj in UnityPy.load(pc).objects:
  if obj.type.name=='Material':
   v=tail(obj);idx[v].append(obj.path_id);by_id[obj.path_id]=v
 previous=None
 if a.action=='template':
  raw=a.input.read_bytes()
  if sha(raw)!=original:raise ValueError('incompatible original asset')
  prior={}
 else:
  previous=json.loads((a.input/'manifest.json').read_text());raw=(a.input/a.asset).read_bytes()
  if previous.get('version')!=1 or previous.get('original_sha256')!=original or previous.get('steam_sha256')!=pin['sha256'] or sha(raw)!=previous['template_sha256']:raise ValueError('incompatible template')
  prior={r['target_id']:r for r in previous['objects']}
  if len(prior)!=len(previous['objects']):raise ValueError('duplicate target')
 work=bytearray(raw);rows=[];spans=[];nonzero=0
 for obj in UnityPy.load(raw).objects:
  if obj.type.name!='Material':continue
  if a.action=='template':
   value=tail(obj);matches=idx.get(value,[])
   if not matches:continue
   source_id=min(matches)
  else:
   row=prior.get(obj.path_id)
   if row is None:continue
   source_id=row['steam_id'];value=by_id[source_id]
  start=obj.byte_start+obj.byte_size-len(value);end=obj.byte_start+obj.byte_size
  if start<obj.byte_start or end>len(raw):raise ValueError('invalid material range')
  row={'target_id':obj.path_id,'steam_id':source_id,'value_sha256':sha(value),'length':len(value)}
  if previous and row!=prior[obj.path_id]:raise ValueError('manifest mismatch')
  if a.action=='template':
   if raw[start:end]!=value:raise ValueError('source differs')
   nonzero+=len(value)-value.count(0);work[start:end]=bytes(len(value))
  else:
   if any(raw[start:end]):raise ValueError('range not blank')
   work[start:end]=value
  spans.append((start,end));rows.append(row)
 if len(rows)!=count or (previous and len(rows)!=len(prior)):raise ValueError('unexpected coverage')
 spans.sort();cursor=0
 for start,end in spans:
  if start<cursor or raw[cursor:start]!=work[cursor:start]:raise ValueError('outside ranges changed')
  cursor=end
 if raw[cursor:]!=work[cursor:]:raise ValueError('outside ranges changed')
 result=bytes(work)
 if previous and sha(result)!=original:raise ValueError('restored asset differs')
 doc={'version':1,'original_sha256':original,'steam_sha256':pin['sha256'],'template_sha256':sha(result) if not previous else previous['template_sha256'], 'objects':rows,'selected_bytes':sum(e-s for s,e in spans),'nonzero_bytes':nonzero if not previous else previous['nonzero_bytes'],'limitations':'Local research only; other game and third-party content remain.'}
 if previous and doc!=previous:raise ValueError('manifest differs')
 out.mkdir(mode=0o700)
 try:
  (out/a.asset).write_bytes(result);(out/'manifest.json').write_text(json.dumps(doc,indent=2)+'\n')
 except BaseException:shutil.rmtree(out);raise
 print(a.action,len(rows),'materials;',doc['selected_bytes'],'bytes;',doc['nonzero_bytes'],'nonzero')
if __name__=='__main__':main()
