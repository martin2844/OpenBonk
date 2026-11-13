#!/usr/bin/env python3
"""Find possible PC source textures missed by strict name and metadata matching.

Comparisons are evidence for investigation, not proof of identity, license or
visual parity. Only first mip is compared; mip counts and object references
must be checked separately before replacing anything.
"""
import collections,json,re,sys,zipfile
from pathlib import Path
from PIL import ImageChops,ImageStat
import UnityPy
from UnityPy.export import Texture2DConverter as C
pc=Path(sys.argv[1]);obb=Path(sys.argv[2]);out=Path(sys.argv[3])
if out.exists():raise SystemExit('output already exists')
records=json.loads(Path('build/android/share-source-experiment/hashed-texture-comparison.json').read_text())['results'];matched={r['file'] for r in records};targets=[]
with zipfile.ZipFile(obb) as z:
 for n in z.namelist():
  if not re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}',n) or Path(n).name in matched:continue
  try:
   objs=list(UnityPy.load(z.read(n)).objects)
   if len(objs)!=1 or objs[0].type.name!='Texture2D':continue
   t=objs[0].read();targets.append((Path(n).name,t))
  except Exception as ex:print('target error',n,str(ex))
if len(targets)!=14:raise ValueError('unexpected target count')
target_sizes={(t.m_Width,t.m_Height) for _,t in targets};sources=[];errors=[];envs=[]
for f in ('globalgamemanagers','globalgamemanagers.assets','resources.assets','sharedassets0.assets','sharedassets1.assets','sharedassets2.assets','sharedassets3.assets','sharedassets4.assets','level0','level1','level2','level3','level4'):
 env=UnityPy.load(str(pc/f));envs.append(env)
 for o in env.objects:
  if o.type.name!='Texture2D':continue
  try:
   t=o.read()
   if (t.m_Width,t.m_Height) in target_sizes:sources.append((f,o.path_id,t))
  except Exception as ex:errors.append([f,o.path_id,str(ex)[:150]])
print('targets',len(targets),'sources matching dimensions',len(sources),'errors',len(errors),flush=True)
def primary(t):
 raw=t.get_image_data();w=t.m_Width;h=t.m_Height;fmt=int(t.m_TextureFormat)
 sizes={1:w*h,3:w*h*3,4:w*h*4,10:((w+3)//4)*((h+3)//4)*8,12:((w+3)//4)*((h+3)//4)*16,45:((w+3)//4)*((h+3)//4)*8,47:((w+3)//4)*((h+3)//4)*16}
 if fmt not in sizes:raise ValueError('unsupported format '+str(fmt))
 return C.parse_image_data(raw[:sizes[fmt]],w,h,fmt,t.object_reader.version,t.object_reader.platform).convert('RGBA')
results=[]
for name,t in targets:
 row={'file':name,'name':t.m_Name,'size':[t.m_Width,t.m_Height],'format':int(t.m_TextureFormat),'mips':t.m_MipCount,'candidates':[]}
 try:dst=primary(t)
 except Exception as ex:row['error']=str(ex);results.append(row);continue
 for file,id,s in sources:
  if (s.m_Width,s.m_Height)!=(t.m_Width,t.m_Height):continue
  try:
   im=primary(s);delta=ImageChops.difference(dst,im);stat=ImageStat.Stat(delta);score=sum(stat.mean)/4
   row['candidates'].append({'file':file,'path_id':id,'name':s.m_Name,'format':int(s.m_TextureFormat),'mips':s.m_MipCount,'mean_rgba':[round(x,3) for x in stat.mean],'score':round(score,3)})
  except Exception as ex:errors.append([file,id,str(ex)[:150]])
 row['candidates'].sort(key=lambda r:r['score']);row['candidates']=row['candidates'][:8];results.append(row)
 print(name,t.m_Name,[(c['name'],c['file'],c['path_id'],c['score']) for c in row['candidates'][:3]],flush=True)
out.write_text(json.dumps({'results':results,'errors':errors},indent=2)+'\n')
