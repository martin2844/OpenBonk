#!/usr/bin/env python3
"""Blank/restore Shader bodies generated from local Unity sources.

Research only. The Unity package and remaining data require rights review.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
ORIGINAL='b7783754429d13610d4e9aa463faafc8ab48217ac840e60576eb3147d5b08e18'
IDS={1:'Standard',2:'Legacy Shaders/Particles/Alpha Blended',3:'Unlit/Texture'}
NAMES=tuple(IDS.values())
def sha(b):return hashlib.sha256(b).hexdigest()
def sources(folder):
 out={}
 for obj in UnityPy.load(str(folder/'builtin-probe')).objects:
  if obj.type.name!='Shader':continue
  name=obj.read_typetree()['m_ParsedForm']['m_Name']
  if name not in NAMES:continue
  if name in out:raise ValueError('duplicate generated shader')
  out[name]=obj.get_raw_data()
 if set(out)!=set(NAMES):raise ValueError('missing generated shaders')
 return out

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('shader_bundles',type=Path);p.add_argument('input',type=Path);p.add_argument('output',type=Path);a=p.parse_args();out=a.output.absolute()
 if out.exists() or out.is_symlink() or not out.parent.is_dir():p.error('output must be new')
 for src in (a.shader_bundles,a.input):
  src=src.resolve()
  if out.resolve().is_relative_to(src) or src.is_relative_to(out.resolve()):p.error('output must be separate from inputs')
 source=sources(a.shader_bundles);previous=None
 if a.action=='template':
  raw=a.input.read_bytes()
  if sha(raw)!=ORIGINAL:raise ValueError('incompatible original')
  prior={}
 else:
  previous=json.loads((a.input/'manifest.json').read_text());raw=(a.input/'sharedassets1.assets').read_bytes()
  if previous.get('version')!=1 or previous.get('original_sha256')!=ORIGINAL or sha(raw)!=previous['template_sha256']:raise ValueError('incompatible template')
  prior={row['target_id']:row for row in previous['objects']}
  if len(prior)!=len(previous['objects']):raise ValueError('duplicate target')
 work=bytearray(raw);rows=[];spans=[];nonzero=0
 for obj in UnityPy.load(raw).objects:
  if obj.type.name!='Shader' or obj.path_id not in IDS:continue
  if previous:
   row=prior.get(obj.path_id)
   if row is None:continue
   name=row['name'];value=source[name]
  else:
   name=IDS[obj.path_id];value=source[name]
  start=obj.byte_start;end=start+obj.byte_size
  if start<0 or end>len(raw) or len(value)!=obj.byte_size:raise ValueError('invalid shader range')
  row={'target_id':obj.path_id,'name':name,'size':len(value),'sha256':sha(value)}
  if previous and row!=prior[obj.path_id]:raise ValueError('shader manifest mismatch')
  if previous:
   if any(raw[start:end]):raise ValueError('shader range not blank')
   work[start:end]=value
  else:
   if raw[start:end]!=value:raise ValueError('generated shader differs: '+name)
   nonzero+=len(value)-value.count(0);work[start:end]=bytes(len(value))
  rows.append(row);spans.append((start,end))
 if len(rows)!=len(NAMES) or {row['name'] for row in rows}!=set(NAMES):raise ValueError('unexpected shader inventory')
 spans.sort();cursor=0
 for start,end in spans:
  if start<cursor or raw[cursor:start]!=work[cursor:start]:raise ValueError('outside ranges changed')
  cursor=end
 if raw[cursor:]!=work[cursor:]:raise ValueError('outside ranges changed')
 result=bytes(work)
 if previous and sha(result)!=ORIGINAL:raise ValueError('restored asset differs')
 doc={'version':1,'original_sha256':ORIGINAL,'template_sha256':sha(result) if not previous else previous['template_sha256'],'objects':rows,'selected_bytes':sum(e-s for s,e in spans),'nonzero_bytes':nonzero if not previous else previous['nonzero_bytes'],'limitations':'Unity source and other game and third-party data remain subject to review.'}
 if previous and doc!=previous:raise ValueError('manifest differs')
 out.mkdir(mode=0o700)
 try:
  (out/'sharedassets1.assets').write_bytes(result);(out/'manifest.json').write_text(json.dumps(doc,indent=2)+'\n')
 except BaseException:shutil.rmtree(out);raise
 print(a.action,len(rows),'shaders;',doc['selected_bytes'],'bytes;',doc['nonzero_bytes'],'nonzero')
if __name__=='__main__':main()
