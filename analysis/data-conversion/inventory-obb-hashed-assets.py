#!/usr/bin/env python3
"""Summarize hashed serialized Unity files in an OBB without extracting them.

Classification and sizes are not evidence of provenance or redistribution
rights; separate .resource files are not classified by this script.
"""
import argparse,collections,re,zipfile
from pathlib import Path
import UnityPy
p=argparse.ArgumentParser(description=__doc__);p.add_argument('obb',type=Path);a=p.parse_args();counts=collections.Counter();sizes=collections.Counter();errors=[];large=[]
with zipfile.ZipFile(a.obb) as z:
 files=[i for i in z.infolist() if re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}',i.filename)]
 for i in files:
  try:
   env=UnityPy.load(z.read(i));objects=list(env.objects)
   if not objects:counts['no_objects']+=1;sizes['no_objects']+=i.file_size;continue
   types=','.join(sorted(set(x.type.name for x in objects)));counts[types]+=1;sizes[types]+=i.file_size
   if i.file_size>=100000:
    names=[]
    for obj in objects:
     try:names.append((obj.type.name,getattr(obj.read(),'m_Name','')))
     except Exception:names.append((obj.type.name,'<unreadable>'))
    large.append((i.file_size,i.filename,types,names[:5]))
  except Exception as exc:errors.append((i.filename,type(exc).__name__,str(exc)[:120]))
print('files',len(files),'errors',len(errors))
for typ in sorted(sizes,key=lambda x:-sizes[x]):print('TYPE',repr(typ),'files',counts[typ],'bytes',sizes[typ])
for x in sorted(large,reverse=True)[:30]:print('LARGE',x)
for x in errors[:20]:print('ERROR',x)
print('Object classifications do not determine provenance or rights; external resources are counted separately.')
