"""Compare captured UnpackNormal output to original water map mip 0."""
import math,sys
from pathlib import Path
import UnityPy
from PIL import Image
pc=Path(sys.argv[1]); capture=Image.open(sys.argv[2]).convert('RGB')
found=[]
for obj in UnityPy.load(str(pc/'sharedassets1.assets')).objects:
    if obj.type.name == 'Texture2D':
        tex=obj.read()
        if tex.m_Name=='IL3DN_WaterNormal': found.append(tex.image.convert('RGBA'))
if len(found)!=1 or found[0].size!=capture.size: raise SystemExit('missing, ambiguous or wrong-size original texture')
original=found[0]
for flip in [False,True]:
    angles=[]
    for y in range(original.height):
        for x in range(original.width):
            r,g,b,a=original.getpixel((x,y)); nx=2*r*a/(255*255)-1; ny=2*g/255-1
            nz=math.sqrt(max(0,1-min(1,nx*nx+ny*ny))); nlen=math.sqrt(nx*nx+ny*ny+nz*nz)
            gx,gy,gz=capture.getpixel((x,original.height-1-y if flip else y)); gx=gx/127.5-1; gy=gy/127.5-1; gz=gz/127.5-1
            glen=math.sqrt(gx*gx+gy*gy+gz*gz)
            if not nlen or not glen: raise SystemExit('zero vector')
            dot=max(-1,min(1,(nx*gx+ny*gy+nz*gz)/(nlen*glen)))
            angles.append(math.degrees(math.acos(dot)))
    angles.sort()
    print('vertical_flip',flip,'samples',len(angles),'mean_degrees',round(sum(angles)/len(angles),3),'p95_degrees',round(angles[int(.95*(len(angles)-1))],3),'max_degrees',round(angles[-1],3))
print('SCOPE: diagnostic shader with Unity UnpackNormal, point sampling and 8-bit RGB readback; original PC mip-0 decoded texture with observed red*alpha,green formula. No filtering, actual water lighting, full shader, other mips, original live GPU or Retroid GPU comparison.')
