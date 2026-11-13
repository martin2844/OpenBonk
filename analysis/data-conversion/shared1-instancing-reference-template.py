#!/usr/bin/env python3
"""Rebuild local shader bodies while preserving Android dependency pointers.

Local research only: source and runtime rights remain subject to review.
"""
import argparse,hashlib,json,shutil,struct
from pathlib import Path
import UnityPy
ORIGINAL='8bbf34cf2bc089235e29b1b668acedbd0fb66fa71ca8d68e67a0373e12af0bb5'
IDS={1720:'Toon/Lit Tri Planar Normal'}
def sha(b):return hashlib.sha256(b).hexdigest()
def sources(folder):
 out={}
 for obj in UnityPy.load(str(folder/'localshader-instance')).objects:
  if obj.type.name!='Shader':continue
  tree=obj.read_typetree();name=tree['m_ParsedForm']['m_Name']
  if name not in IDS.values():continue
  if name in out:raise ValueError('duplicate shader')
  out[name]=(obj.get_raw_data(),tree)
 if set(out)!=set(IDS.values()):raise ValueError('missing generated shaders')
 return out

def refs(raw,tree):
 deps=tree['m_Dependencies']
 if not deps or tree['m_NonModifiableTextures'] or tree['m_ShaderIsBaked'] not in (True,False):raise ValueError('unexpected shader dependency layout')
 pointers=b''.join(struct.pack('<iq',p['m_FileID'],p['m_PathID']) for p in deps)
 suffix=struct.pack('<I',len(deps))+pointers+struct.pack('<I?',0,tree['m_ShaderIsBaked'])+bytes(3)
 if not raw.endswith(suffix):raise ValueError('unexpected shader dependency serialization')
 start=len(raw)-len(suffix)+4
 return start,start+len(pointers)

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
  prior={r['target_id']:r for r in previous['objects']}
  if len(prior)!=len(previous['objects']):raise ValueError('duplicate target')
 work=bytearray(raw);rows=[];spans=[];nonzero=0
 for obj in UnityPy.load(raw).objects:
  if obj.type.name!='Shader' or obj.path_id not in IDS:continue
  name=IDS[obj.path_id];gen,gen_tree=source[name];start=obj.byte_start;end=start+obj.byte_size
  if end>len(raw) or len(gen)!=obj.byte_size:raise ValueError('shader size mismatch')
  gs,ge=refs(gen,gen_tree)
  if previous:
   row=prior.get(obj.path_id)
   if row is None:raise ValueError('missing shader record')
   rs,re=row['reference_start'],row['reference_end']
   if (rs,re)!=(gs,ge):raise ValueError('generated reference layout differs')
  else:
   old_tree=obj.read_typetree();rs,re=refs(raw[start:end],old_tree)
   if (rs,re)!=(gs,ge):raise ValueError('reference layout differs')
  if not 0<=rs<re<=len(gen):raise ValueError('invalid reference range')
  ref=raw[start+rs:start+re]
  candidate=gen[:rs]+ref+gen[re:]
  row={'target_id':obj.path_id,'name':name,'size':len(gen),'reference_start':rs,'reference_end':re,'reference_sha256':sha(ref),'generated_sha256':sha(gen),'original_sha256':sha(candidate)}
  if previous:
   if row!=prior[obj.path_id] or any(raw[start:start+rs]) or any(raw[start+re:end]):raise ValueError('template or shader mismatch')
   work[start:end]=candidate
  else:
   if raw[start:end]!=candidate:raise ValueError('generated shader differs beyond references')
   nonzero+=sum(x!=0 for x in raw[start:start+rs])+sum(x!=0 for x in raw[start+re:end])
   work[start:start+rs]=bytes(rs);work[start+re:end]=bytes(len(gen)-re)
  spans.extend(((start,start+rs),(start+re,end)));rows.append(row)
 if len(rows)!=len(IDS) or (previous and len(rows)!=len(prior)):raise ValueError('unexpected coverage')
 spans.sort();cursor=0
 for start,end in spans:
  if start<cursor or raw[cursor:start]!=work[cursor:start]:raise ValueError('outside ranges changed')
  cursor=end
 if raw[cursor:]!=work[cursor:]:raise ValueError('outside ranges changed')
 result=bytes(work)
 if previous and sha(result)!=ORIGINAL:raise ValueError('restored asset differs')
 doc={'version':1,'original_sha256':ORIGINAL,'template_sha256':sha(result) if not previous else previous['template_sha256'],'objects':rows,'selected_bytes':sum(e-s for s,e in spans),'nonzero_bytes':nonzero if not previous else previous['nonzero_bytes'],'limitations':'Android dependency pointers retained; provenance and rights not established.'}
 if previous and doc!=previous:raise ValueError('manifest differs')
 out.mkdir(mode=0o700)
 try:
  (out/'sharedassets1.assets').write_bytes(result);(out/'manifest.json').write_text(json.dumps(doc,indent=2)+'\n')
 except BaseException:shutil.rmtree(out);raise
 print(a.action,len(rows),'shaders;',doc['selected_bytes'],'bytes;',doc['nonzero_bytes'],'nonzero')
if __name__=='__main__':main()
