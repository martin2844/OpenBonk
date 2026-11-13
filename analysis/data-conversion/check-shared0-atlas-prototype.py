import json,sys,collections
import UnityPy
from PIL import ImageChops,ImageStat
from pathlib import Path
pc,orig,template,restored,manifest,fonts=map(Path,sys.argv[1:])
p=UnityPy.load(str(pc));o=UnityPy.load(str(orig));t=UnityPy.load(str(template));r=UnityPy.load(str(restored))
pi={x.path_id:x for x in p.objects};oi={x.path_id:x for x in o.objects};ti={x.path_id:x for x in t.objects};ri={x.path_id:x for x in r.objects}
records=json.loads(manifest.read_text())['atlases'];fontrecs=json.loads(fonts.read_text())['fonts'];ids={x['target_id'] for x in records};fids={x['target_id'] for x in fontrecs}
assert set(oi)==set(ti)==set(ri)
changed_template=[];changed_restore=[]
for k in oi:
 old=oi[k].get_raw_data()
 if old!=ti[k].get_raw_data():changed_template.append(k)
 if old!=ri[k].get_raw_data():changed_restore.append(k)
assert set(changed_template)==ids,(len(changed_template),len(ids|fids))
assert set(changed_restore)==ids|fids,(len(changed_restore),len(ids))
for v in fontrecs:assert bytes(ri[v['target_id']].read().m_FontData)==bytes(pi[v['source_id']].read().m_FontData)
measures=[]
for v in records:
 ot=oi[v['target_id']].read();tt=ti[v['target_id']].read();rt=ri[v['target_id']].read()
 assert not any(tt.image_data)
 a=ot.image.getchannel('A');b=rt.image.getchannel('A')
 diff=ImageChops.difference(a,b);hist=diff.histogram();mean=ImageStat.Stat(diff).mean[0]
 aa=a.point(lambda x:255 if x>=128 else 0);bb=b.point(lambda x:255 if x>=128 else 0)
 cross=ImageChops.difference(aa,bb).histogram()[255]
 measures.append((mean,cross,v['name'],max(i for i,n in enumerate(hist) if n)))
print('changed template objects',len(changed_template),'expected 94 atlases')
print('changed restored objects',len(changed_restore),'expected 94 atlases and seven restored fonts; fonts byte identical')
print('alpha mean difference across atlases',sum(x[0] for x in measures)/len(measures))
print('alpha mean difference min/max',min(x[0] for x in measures),max(x[0] for x in measures))
print('pixels crossing 128 total/max',sum(x[1] for x in measures),max(x[1] for x in measures),'out of',94*1024*1024)
print('largest alpha mean differences:')
for x in sorted(measures,reverse=True)[:12]:print(x)
