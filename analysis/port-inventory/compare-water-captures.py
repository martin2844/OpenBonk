"""Compare rough color statistics of controlled but non-matched PC/Android captures."""
import sys
from PIL import Image
paths=sys.argv[1:]
if len(paths)!=3: raise SystemExit('Expected original PC, Android clamped and Android full captures')
box=(50,300,1230,700)
images=[]
for path in paths:
    im=Image.open(path).convert('RGB')
    if im.size != (1280,720): raise SystemExit(f'unexpected size {path}: {im.size}')
    images.append(list(im.crop(box).get_flattened_data()))
for path,pixels in zip(paths,images):
    mean=tuple(round(sum(p[c] for p in pixels)/len(pixels),2) for c in range(3))
    dark=sum(max(p)<30 for p in pixels); white=sum(min(p)>245 for p in pixels)
    print(path,'region',box,'mean_rgb',mean,'dark',dark,'near_white',white,'pixels',len(pixels))
for path,pixels in zip(paths[1:],images[1:]):
    delta=sum(abs(a[c]-b[c]) for a,b in zip(images[0],pixels) for c in range(3))/(3*len(pixels))
    print('diagnostic_rgb_mae',path,round(delta,3))
print('LIMIT: region excludes most HUD, but images are separate runs with different backend, animation time, postprocessing and potentially scene/camera state. RGB MAE is descriptive, not a parity or perceptual-quality metric.')
