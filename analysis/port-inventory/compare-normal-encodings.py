"""Compare candidate unpacking of selected same-name normal maps.

The actual compiled Android shader variant is not determined by this script.
"""
import collections, hashlib, math, re, sys, tempfile, zipfile
from pathlib import Path
import UnityPy
from PIL import Image
NAMES={'IL3DN_WaterNormal','Tiles1_Normal','Tiles4_Normal'}
source=Path(sys.argv[1]); apk=Path(sys.argv[2]); records=[collections.defaultdict(dict),collections.defaultdict(dict)]

def scan(path, dest):
    for obj in UnityPy.load(str(path)).objects:
        if obj.type.name!='Texture2D': continue
        t=obj.read()
        if t.m_Name not in NAMES: continue
        try:
            img=t.image.convert('RGBA'); raw=img.tobytes(); h=hashlib.sha256(raw).hexdigest()
            dest[t.m_Name][h]=(img.size,raw, t.m_TextureFormat, path.name)
        except Exception as e: print('ERROR',path.name,t.m_Name,repr(e))
for p in [source/'resources.assets',*sorted(source.glob('sharedassets*.assets')),*sorted(source.glob('level[0-9]*'))]: scan(p,records[0])
with tempfile.TemporaryDirectory(prefix='openbonk-normals-') as tmp, zipfile.ZipFile(apk) as z:
    names=z.namelist()
    for group in range(5):
        stem=f'assets/bin/Data/sharedassets{group}.assets'; parts=sorted((n for n in names if re.fullmatch(re.escape(stem)+r'\.split\d+',n)),key=lambda n:int(n.rsplit('split',1)[1]))
        if not parts and stem in names: parts=[stem]
        if not parts: continue
        path=Path(tmp)/Path(stem).name
        with path.open('wb') as out:
            for part in parts: out.write(z.read(part))
        if stem+'.resource' in names: (Path(tmp)/(path.name+'.resource')).write_bytes(z.read(stem+'.resource'))
        scan(path,records[1])
    for name in names:
        if not re.fullmatch(r'assets/bin/Data/[a-f0-9]{32}',name): continue
        path=Path(tmp)/Path(name).name; path.write_bytes(z.read(name))
        if name+'.resource' in names: (Path(tmp)/(path.name+'.resource')).write_bytes(z.read(name+'.resource'))
        scan(path,records[1])

def unpack(px, encoding):
    r,g,b,a=[v/255 for v in px]
    if encoding=='rg_or_ag': x,y=2*r*a-1,2*g-1
    elif encoding=='ag': x,y=2*a-1,2*g-1
    elif encoding=='rgb':
        x,y,z=2*r-1,2*g-1,2*b-1
        length=math.sqrt(x*x+y*y+z*z)
        return (x/length,y/length,z/length) if length else (0,0,1)
    z=math.sqrt(max(0,1-min(1,x*x+y*y)))
    length=math.sqrt(x*x+y*y+z*z)
    return (x/length,y/length,z/length) if length else (0,0,1)

for name in sorted(NAMES):
    left=records[0][name];right=records[1][name]
    print('TEXTURE',name,'pc_variants',[(k[:12],v[0],v[2],v[3]) for k,v in left.items()],'android_variants',[(k[:12],v[0],v[2],v[3]) for k,v in right.items()])
    if len(left)!=1 or len(right)!=1: print('INCONCLUSIVE multiple or missing variants');continue
    pc=next(iter(left.values())); android=next(iter(right.values()))
    if pc[0]!=android[0]: print('INCONCLUSIVE sizes differ');continue
    stride=max(1,(pc[0][0]*pc[0][1])//65536); pairs=[]
    for i in range(0,len(pc[1]),4*stride):
        a=unpack(pc[1][i:i+4], 'rg_or_ag'); pairs.append((a,android[1][i:i+4]))
    for encoding in ['rg_or_ag','ag','rgb']:
        angles=[]
        for a,rgba in pairs:
            b=unpack(rgba,encoding); dot=max(-1,min(1,sum(x*y for x,y in zip(a,b))))
            angles.append(math.degrees(math.acos(dot)))
        angles.sort()
        print('CANDIDATE',encoding,'samples',len(angles),'mean_degrees',round(sum(angles)/len(angles),3),'p95_degrees',round(angles[int(.95*(len(angles)-1))],3),'max_degrees',round(angles[-1],3))
    source_png = Path('unity/OpenBonk/Assets/Texture2D') / (name + '.png')
    image = Image.open(source_png).convert('RGBA')
    if image.size != pc[0]:
        print('SOURCE_PNG_INCONCLUSIVE', image.size)
        continue
    raw = image.tobytes()
    for encoding in ['rg_or_ag','ag','rgb']:
        angles=[]
        for i in range(0,len(pc[1]),4*stride):
            a=unpack(pc[1][i:i+4], 'rg_or_ag'); b=unpack(raw[i:i+4],encoding)
            angles.append(math.degrees(math.acos(max(-1,min(1,sum(x*y for x,y in zip(a,b)))))))
        angles.sort()
        print('SOURCE_PNG_CANDIDATE',encoding,'mean_degrees',round(sum(angles)/len(angles),3),'p95_degrees',round(angles[int(.95*(len(angles)-1))],3),'max_degrees',round(angles[-1],3))
print('SCOPE: unique decoded RGBA candidates by name and size; PC assumes the observed water RG*alpha/green shader convention; Android encodings are hypotheses, not compiled-shader evidence. Sampled mip 0 texels, no filtering, texture orientation, GPU rendering, object binding or runtime material proof.')
