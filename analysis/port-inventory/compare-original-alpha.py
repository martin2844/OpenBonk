"""Compare same-name, same-size decoded texture alpha in PC and Android assets.

Name/dimension matching is heuristic, not evidence of actual PPtr bindings.
"""
import collections, hashlib, re, sys, tempfile, zipfile, zlib
from pathlib import Path
import UnityPy

pc = Path(sys.argv[1]); apk = Path(sys.argv[2]); channels = {}

def digest_file(path, records, errors):
    try:
        objects = UnityPy.load(str(path)).objects
        for obj in objects:
            if obj.type.name != 'Texture2D': continue
            t = obj.read()
            try:
                im = t.image.convert('RGBA')
                a = im.getchannel('A')
                alpha = a.tobytes()
                key = hashlib.sha256(alpha).hexdigest()
                records[(t.m_Name, *im.size)].add(key)
                if key not in channels: channels[key] = zlib.compress(alpha, 1)
            except Exception as exc:
                errors.append((path.name, t.m_Name, t.m_Width, t.m_Height, type(exc).__name__, str(exc)))
    except Exception as exc:
        errors.append((path.name, '<file>', 0, 0, type(exc).__name__))

left = collections.defaultdict(set); right = collections.defaultdict(set); errors1=[]; errors2=[]
for path in [pc/'resources.assets', *sorted(pc.glob('sharedassets*.assets')), *sorted(pc.glob('level[0-9]*'))]:
    digest_file(path, left, errors1)
with tempfile.TemporaryDirectory(prefix='openbonk-alpha-') as tmp, zipfile.ZipFile(apk) as z:
    names = z.namelist()
    for group in range(5):
        stem=f'assets/bin/Data/sharedassets{group}.assets'
        parts=sorted((n for n in names if re.fullmatch(re.escape(stem)+r'\.split\d+',n)),key=lambda n:int(n.rsplit('split',1)[1]))
        if not parts and stem in names: parts=[stem]
        if not parts: continue
        path=Path(tmp)/Path(stem).name
        with path.open('wb') as output:
            for p in parts: output.write(z.read(p))
        res=stem+'.resource'
        if res in names: (Path(tmp)/Path(res).name).write_bytes(z.read(res))
        digest_file(path,right,errors2)
    for name in names:
        if not re.fullmatch(r'assets/bin/Data/[a-f0-9]{32}',name): continue
        path=Path(tmp)/Path(name).name; path.write_bytes(z.read(name))
        if name+'.resource' in names: (Path(tmp)/(path.name+'.resource')).write_bytes(z.read(name+'.resource'))
        digest_file(path,right,errors2)
common=set(left)&set(right); unique={k for k in common if len(left[k])==1 and len(right[k])==1}
if not unique:
    raise SystemExit('No comparable decoded textures; check image decoder and its native libraries')
diff=sorted(k for k in unique if left[k]!=right[k])
for k in diff:
    src = zlib.decompress(channels[next(iter(left[k]))]); dst = zlib.decompress(channels[next(iter(right[k]))])
    delta = [abs(a-b) for a,b in zip(src,dst)]
    print('ALPHA_DIFF',repr(k),'max=',max(delta, default=0),'changed=',sum(x>0 for x in delta),
          'over16=',sum(x>16 for x in delta),'transparent_to_opaque=',sum(a==0 and b==255 for a,b in zip(src,dst)),
          'opaque_to_transparent=',sum(a==255 and b==0 for a,b in zip(src,dst)),
          'near_clear_to_opaque=',sum(a<=8 and b>=247 for a,b in zip(src,dst)),
          'near_opaque_to_clear=',sum(a>=247 and b<=8 for a,b in zip(src,dst)))
print('SUMMARY', 'pc_keys',len(left),'android_keys',len(right),'common',len(common),'unique_pairs',len(unique),'unique_equal',len(unique)-len(diff),'unique_alpha_diff',len(diff),'ambiguous',len(common)-len(unique))
print('PC_ERRORS',len(errors1),'sample',errors1[:10])
print('ANDROID_ERRORS',len(errors2),'sample',errors2[:10])
print('SCOPE: exact alpha hash on decodable Texture2D objects by name and dimensions; ignores RGB, PPtr bindings, sprite atlases, shader, GPU output, runtime loads and unscanned containers.')
