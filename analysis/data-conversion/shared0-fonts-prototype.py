#!/usr/bin/env python3
"""Strip/rebuild seven byte-identical font payloads from pinned Steam data.

Research only: templates still contain other original assets and code, and
are not redistribution artifacts. Font metadata and references are retained.
"""
import argparse,collections,hashlib,json,shutil
from pathlib import Path
import UnityPy
PC='01ff22507aa0ef446543b038429039deb92f817b89668bed858bafe99235c19a'
TARGET='637c514a66dd6ece82805b1265fdf1ed4079aba7fc4fa79282f09eb2c2c194dc'
EXPECTED={'NotoSansTC-Bold','NotoSansSC-Bold','alagard','NotoSansThai_Condensed-Bold','NotoSansJP-Bold','NotoSansKR-Bold','NotoSans-Bold'}
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'unsupported input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--manifest',type=Path);a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets0.assets',PC)
if a.action=='template':require(a.input_asset,TARGET)
else:
 if not a.manifest:p.error('restore requires --manifest')
 m=json.loads(a.manifest.read_text());recs=m.get('fonts',[])
 if m.get('version')!=1 or m.get('target_sha256')!=TARGET or len(recs)!=7 or {r['name'] for r in recs}!=EXPECTED:raise ValueError('invalid manifest')
 require(a.input_asset,m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets0.assets'));env=UnityPy.load(str(a.input_asset));sources=collections.defaultdict(list)
for obj in pc.objects:
 if obj.type.name=='Font':
  f=obj.read();sources[f.m_Name].append((obj.path_id,f))
records=[]
for obj in env.objects:
 if obj.type.name!='Font':continue
 dst=obj.read();matches=sources[dst.m_Name]
 if len(matches)!=1 or dst.m_Name not in EXPECTED:raise ValueError('font source not unique')
 sid,src=matches[0];data=bytes(src.m_FontData);old=bytes(dst.m_FontData)
 if not data or len(data)!=len(old):raise ValueError('font payload size mismatch')
 rec={'target_id':obj.path_id,'source_id':sid,'name':dst.m_Name,'size':len(data),'sha256':hashlib.sha256(data).hexdigest()}
 if a.action=='template':
  if data!=old:raise ValueError('font source not byte-identical')
  records.append(rec);dst.m_FontData=bytes(len(data))
 else:
  if next((r for r in recs if r['target_id']==obj.path_id),None)!=rec or any(old):raise ValueError('font source or template mismatch')
  dst.m_FontData=data
 dst.save()
if a.action=='template' and (len(records)!=7 or {r['name'] for r in records}!=EXPECTED):raise ValueError('unexpected font inventory')
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets0.assets';out.write_bytes(env.file.save());h=sha(out)
 if a.action=='template':(a.output_dir/'font-manifest.json').write_text(json.dumps({'version':1,'target_sha256':TARGET,'template_sha256':h,'fonts':records},indent=2)+'\n')
 print(a.action,7,'fonts,',sum(r['size'] for r in (records if a.action=='template' else recs)),'payload bytes; asset SHA256',h)
 print('Other original content remains: not for distribution.')
except Exception:shutil.rmtree(a.output_dir);raise
