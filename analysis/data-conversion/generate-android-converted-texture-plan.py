#!/usr/bin/env python3
"""Generate a pinned, offset-only plan for 53 locally converted textures."""
import argparse
import hashlib
import importlib.util
import json
import struct
import zipfile
from pathlib import Path
import UnityPy

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('exact_plan', Path(__file__).with_name('generate-android-texture-copy-plan.py'))
exact = importlib.util.module_from_spec(spec)
spec.loader.exec_module(exact)
MANIFEST = ROOT / 'build/android/local-research-manifests/hashed-matched53-template/manifest.json'
RESTORED = ROOT / 'build/android/share-source-experiment/hashed-matched53-restored'
OUTPUT = ROOT / 'build/android/converted-texture-android-plan/plan.bin'

def sha(b): return hashlib.sha256(b).hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=OUTPUT)
    output = parser.parse_args().output
    if output.exists() or output.is_symlink(): raise ValueError('output exists')
    if exact.file_sha(exact.BASE) != exact.BASE_SHA: raise ValueError('base mismatch')
    records = json.loads(MANIFEST.read_text())['textures']
    if len(records) != 53 or len({r['file'] for r in records}) != 53: raise ValueError('inventory mismatch')
    plan = bytearray(b'OBSCV001' + struct.pack('>I', len(records)))
    for h in (*exact.LINUX_SHA, *exact.WINDOWS_SHA): plan.extend(bytes.fromhex(h))
    with zipfile.ZipFile(exact.BASE) as base, zipfile.ZipFile(exact.LINUX) as linux, zipfile.ZipFile(exact.WINDOWS) as windows:
        sources, objects = [], []
        for z, pins in ((linux, exact.LINUX_SHA), (windows, exact.WINDOWS_SHA)):
            data = [z.read('Megabonk_Data/' + n) for n in exact.NAMES]
            if tuple(sha(b) for b in data) != pins: raise ValueError('Steam source mismatch')
            sources.append(data)
            objects.append({name: {o.path_id: o for o in UnityPy.load(data[i]).objects} for name, i in ((exact.NAMES[0], 0), (exact.NAMES[2], 2))})
        for rec in records:
            name = rec['file']
            if len(name) != 32 or any(c not in '0123456789abcdef' for c in name): raise ValueError('invalid name')
            template = base.read('assets/bin/Data/' + name)
            restored = (RESTORED / name).read_bytes()
            if sha(template) != rec['template_sha256'] or sha(restored) != rec['converted_sha256']: raise ValueError('target mismatch')
            if rec['offset'] < 0 or rec['size'] <= 0 or rec['offset']+rec['size'] > len(template) or any(template[rec['offset']:rec['offset']+rec['size']]) or len(template) != len(restored): raise ValueError('target not blank')
            if template[:rec['offset']] != restored[:rec['offset']] or template[rec['offset']+rec['size']:] != restored[rec['offset']+rec['size']:]: raise ValueError('unexpected target changes')
            target = UnityPy.load(template).objects[0].read()
            refs, properties = [], []
            for data, index in zip(sources, objects):
                obj = index[rec['source_file']][rec['source_id']]
                if obj.type.name != 'Texture2D': raise ValueError('wrong source type')
                tex = obj.read(); raw = obj.get_raw_data(); payload = bytes(tex.image_data or b'')
                if payload:
                    at = raw.find(payload)
                    if at < 0: raise ValueError('inline data missing')
                    fid = exact.NAMES.index(rec['source_file']); offset = obj.byte_start + at
                else:
                    stream = tex.m_StreamData
                    if not stream or stream.path not in exact.NAMES or stream.offset < 0 or stream.size <= 0: raise ValueError('invalid stream')
                    fid = exact.NAMES.index(stream.path); offset = stream.offset; payload = data[fid][offset:offset+stream.size]
                if sha(payload) != rec['source_sha256'] or data[fid][offset:offset+len(payload)] != payload: raise ValueError('payload mismatch')
                if tex.m_Name != rec['name'] or (tex.m_Width,tex.m_Height,tex.m_MipCount)!=(target.m_Width,target.m_Height,target.m_MipCount): raise ValueError('texture metadata mismatch')
                refs.append((fid,offset,payload)); properties.append((tex.m_Width,tex.m_Height,tex.m_MipCount,tex.m_TextureFormat,target.m_TextureFormat))
            if refs[0][2] != refs[1][2] or properties[0] != properties[1]: raise ValueError('platform texture differs')
            w,h,mips,source_fmt,target_fmt = properties[0]
            if source_fmt not in (3,4,10,12) or target_fmt not in (4,45,47): raise ValueError('unsupported formats')
            if w <= 0 or h <= 0 or w > 4096 or h > 4096 or mips <= 0 or mips > max(w,h).bit_length(): raise ValueError('invalid texture dimensions')
            sizes = [(max(1,w>>i),max(1,h>>i)) for i in range(mips)]
            src_size = sum(x*y*source_fmt if source_fmt in (3,4) else ((x+3)//4)*((y+3)//4)*(8 if source_fmt==10 else 16) for x,y in sizes)
            dst_size = sum(x*y*4 if target_fmt==4 else ((x+3)//4)*((y+3)//4)*(8 if target_fmt==45 else 16) for x,y in sizes)
            if src_size != len(refs[0][2]) or dst_size != rec['size']: raise ValueError('inconsistent mip lengths')
            plan.extend(name.encode('ascii'))
            for key in ('template_sha256','converted_sha256','source_sha256'): plan.extend(bytes.fromhex(rec[key]))
            plan.extend(struct.pack('>IIIIIIIIIQIQ',rec['offset'],rec['size'],len(refs[0][2]),w,h,mips,source_fmt,target_fmt,refs[0][0],refs[0][1],refs[1][0],refs[1][1]))
    output.parent.mkdir(parents=True,exist_ok=True)
    with output.open('xb') as f: f.write(plan)
    print('53 textures, plan',len(plan),'bytes, SHA-256',sha(plan),'; offsets, formats and hashes only')

if __name__ == '__main__': main()
