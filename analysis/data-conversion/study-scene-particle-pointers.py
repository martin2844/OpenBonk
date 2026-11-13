#!/usr/bin/env python3
"""Locate differences inside paired serialized sub-emitter PPtr fields.

Name matches are clues, not proof of identity, provenance or rights. The result
identifies byte ranges only when serialized references can be located uniquely.
"""
import collections,json,struct,sys,zipfile
from pathlib import Path
import UnityPy
pc=Path(sys.argv[1]);obb=Path(sys.argv[2]);inventory=Path(sys.argv[3]);output=Path(sys.argv[4]);data=json.loads(inventory.read_text());results=[]
def positions(raw,pattern):
 out=[];i=0
 while True:
  i=raw.find(pattern,i)
  if i<0:return out
  out.append(i);i+=1
def refname(p):
 if not p.m_PathID:return '<null>'
 obj=p.read();return obj.m_GameObject.read().m_Name
with zipfile.ZipFile(obb) as z:
 for scene in data['scenes']:
  name=scene['scene'];pre='assets/bin/Data/'+name+'.split';ns=sorted((n for n in z.namelist() if n.startswith(pre)),key=lambda n:int(n[len(pre):]));android=UnityPy.load(b''.join(z.read(n) for n in ns));steam=UnityPy.load(str(pc/name));dst={o.path_id:o for o in android.objects};src={o.path_id:o for o in steam.objects}
  for r in scene['rows']:
   if any(q['different_bytes_after_12']==0 for q in r['matches']):continue
   ao=dst[r['id']];a=ao.get_raw_data();d=ao.read();candidates=[]
   for q in r['matches']:
    so=src[q['id']];b=so.get_raw_data();s=so.read();aa=d.SubModule.subEmitters or [];bb=s.SubModule.subEmitters or []
    if len(aa)!=len(bb):continue
    ranges=[];okay=True
    for x,y in zip(aa,bb):
     ap=x.emitter;bp=y.emitter
     try:an=refname(ap);bn=refname(bp)
     except Exception:okay=False;break
     if an!=bn or (x.type,x.properties,x.emitProbability)!=(y.type,y.properties,y.emitProbability):okay=False;break
     ab=struct.pack('<iq',ap.m_FileID,ap.m_PathID);bbin=struct.pack('<iq',bp.m_FileID,bp.m_PathID)
     if ab==bbin:continue
     possible=[off for off in positions(a,ab) if off>=12 and b[off:off+12]==bbin]
     if len(possible)!=1:okay=False;break
     ranges.append({'offset':possible[0],'size':12,'android_file_id':ap.m_FileID,'android_path_id':ap.m_PathID,'pc_file_id':bp.m_FileID,'pc_path_id':bp.m_PathID,'referenced_name':an})
    if not okay or len({x['offset'] for x in ranges})!=len(ranges):continue
    mask=set(i for x in ranges for i in range(x['offset'],x['offset']+12));diff={i for i in range(12,len(a)) if a[i]!=b[i]}
    if not diff or not diff<=mask:continue
    candidates.append({'source_id':q['id'],'references':ranges,'diff_bytes':len(diff)})
   results.append({'scene':name,'target_id':r['id'],'name':r['name'],'size':len(a),'candidates':candidates})
output.write_text(json.dumps({'results':results},indent=2)+'\n');print('objects',len(results),'matched uniquely',sum(len(r['candidates'])==1 for r in results),'ambiguous',sum(len(r['candidates'])>1 for r in results),'unresolved',sum(not r['candidates'] for r in results))
for r in results:print(r['scene'],r['name'],r['target_id'],'candidates',len(r['candidates']),'refs',len(r['candidates'][0]['references']) if r['candidates'] else 0)
