#!/usr/bin/env python3
"""Independently compare original, blank and restored local scene bytes."""
import hashlib, json, pathlib, zipfile
import UnityPy
root=pathlib.Path(__file__).resolve().parents[2]
template=root/'build/android/scene-gameobject-values-template'
restored=root/'build/android/scene-gameobject-values-restored'
obb=root/'build/android/no-rewired-animation-hybrid-test/main.1.com.megabonk.nativeport.splittest.obb'
manifest=json.loads((template/'manifest.json').read_text())
report={}
with zipfile.ZipFile(obb) as z:
    for scene,record in manifest['scenes'].items():
        prefix='assets/bin/Data/'+scene+'.split'
        names=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
        original=b''.join(z.read(n) for n in names); blank=(template/scene).read_bytes(); result=(restored/scene).read_bytes()
        assert original==result and len(original)==len(blank)
        ids={r['target_id'] for r in record['objects']}; ranges=[]
        for obj in UnityPy.load(original).objects:
            if obj.type.name=='GameObject' and obj.path_id in ids:
                start=obj.byte_start+4+12*len(obj.read_typetree()['m_Component']); end=obj.byte_start+obj.byte_size
                ranges.append((start,end))
        assert len(ranges)==len(ids)
        mask=bytearray(len(original))
        for start,end in ranges:
            assert not any(mask[start:end]) and not any(blank[start:end]); mask[start:end]=b'\1'*(end-start)
        changed=sum(a!=b for a,b in zip(original,blank))
        assert all(a==b or mask[i] for i,(a,b) in enumerate(zip(original,blank)))
        assert changed==record['nonzero_bytes']
        report[scene]={'objects':len(ranges),'selected_bytes':sum(b-a for a,b in ranges),'changed_bytes':changed,'restored_sha256':hashlib.sha256(result).hexdigest()}
out=root/'build/android/scene-gameobject-values-verification.json'
if out.exists(): raise ValueError('output exists')
out.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
