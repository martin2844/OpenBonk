#!/usr/bin/env python3
"""Measure exact changed byte positions for remaining matched particle objects.

Byte differences do not identify their meaning or determine data rights.
"""
import collections,json,sys,zipfile
from pathlib import Path
import UnityPy
pc=Path(sys.argv[1]);obb=Path(sys.argv[2]);inventory=Path(sys.argv[3]);output=Path(sys.argv[4]);data=json.loads(inventory.read_text());results=[];counter=collections.Counter()
with zipfile.ZipFile(obb) as z:
 for scene in data['scenes']:
  name=scene['scene'];pre='assets/bin/Data/'+name+'.split';ns=sorted((n for n in z.namelist() if n.startswith(pre)),key=lambda n:int(n[len(pre):]));android=UnityPy.load(b''.join(z.read(n) for n in ns));steam=UnityPy.load(str(pc/name));dst={o.path_id:o for o in android.objects};src={o.path_id:o for o in steam.objects}
  for r in scene['rows']:
   if any(q['different_bytes_after_12']==0 for q in r['matches']):continue
   candidate=min(r['matches'],key=lambda x:(x['different_bytes_after_12'],x['id']));a=dst[r['id']].get_raw_data();b=src[candidate['id']].get_raw_data();diff=[i for i in range(12,len(a)) if a[i]!=b[i]]
   if len(diff)!=candidate['different_bytes_after_12']:raise ValueError('candidate mismatch')
   for i in diff:counter[i]+=1
   results.append({'scene':name,'id':r['id'],'name':r['name'],'size':len(a),'source_id':candidate['id'],'changes':[{'offset':i,'android':a[i],'steam':b[i]} for i in diff]})
output.write_text(json.dumps({'results':results,'offset_counts':counter.most_common()},indent=2)+'\n')
print('objects',len(results),'changed bytes',sum(len(x['changes']) for x in results),'most common offsets',counter.most_common(20))
for r in results:print(r['scene'],r['name'],r['id'],r['source_id'],[(x['offset'],x['steam'],x['android']) for x in r['changes']])
