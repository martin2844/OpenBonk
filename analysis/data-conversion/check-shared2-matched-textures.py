import UnityPy,json
from PIL import ImageChops,ImageStat
from UnityPy.export import Texture2DConverter as C
p='build/android/share-source-experiment/'
b=UnityPy.load(p+'shared2-atlases-template/sharedassets2.assets');t=UnityPy.load(p+'shared2-matched72-template/sharedassets2.assets');r=UnityPy.load(p+'shared2-matched72-restored/sharedassets2.assets')
a={x.path_id:x for x in b.objects};tt={x.path_id:x for x in t.objects};rr={x.path_id:x for x in r.objects};records=json.load(open(p+'shared2-matched72-template/textures-manifest.json'))['textures'];ids={x['target_id'] for x in records};assert set(a)==set(tt)==set(rr)
assert {k for k in a if a[k].get_raw_data()!=tt[k].get_raw_data()}==ids
assert {k for k in a if a[k].get_raw_data()!=rr[k].get_raw_data()} <= ids
results=[]
for rec in records:
 k=rec['target_id'];old=a[k].read();new=rr[k].read();empty=tt[k].read();assert not any(empty.image_data)
 scores=[];off=0
 for lv in range(old.m_MipCount):
  w=max(1,old.m_Width>>lv);h=max(1,old.m_Height>>lv);fmt=old.m_TextureFormat;n=w*h*4 if fmt==4 else ((w+3)//4)*((h+3)//4)*(8 if fmt==45 else 16)
  x=C.parse_image_data(old.image_data[off:off+n],w,h,fmt,old.object_reader.version,old.object_reader.platform).convert('RGBA');y=C.parse_image_data(new.image_data[off:off+n],w,h,fmt,new.object_reader.version,new.object_reader.platform).convert('RGBA')
  delta=ImageChops.difference(x,y);scores.append([round(z,3) for z in ImageStat.Stat(delta).mean]);off+=n
 assert off==len(old.image_data),(k,off,len(old.image_data))
 results.append({'id':k,'name':old.m_Name,'mips':scores})
print('only selected texture objects changed; remaining objects byte identical')
for x in sorted(results,key=lambda x:max(x['mips'][0]),reverse=True):print(x['id'],repr(x['name']),'level0',x['mips'][0],'max across levels',max(max(z) for z in x['mips']))
json.dump(results,open(p+'shared2-inventory/matched72-quality.json','w'),indent=2)
