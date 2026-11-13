#!/usr/bin/env python3
"""Derive the 94 Steam atlas ranges for the private Android conversion study."""
import hashlib
import json
import struct
import zipfile
from pathlib import Path

import UnityPy

ROOT = Path(__file__).resolve().parents[2]
B = ROOT / 'build/android'
BASE = B / 'shared0-font-android-plan/linux.assets'
TARGET = B / 'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
ZIPS = (B / 'linux-steam-regression.zip', B / 'windows-steam-full-input.zip')
MANIFEST = B / 'local-research-manifests/shared0-atlas-template/atlas-manifest.json'
OUTPUT = B / 'shared0-atlas-android-plan/plan.bin'

def sha(data): return hashlib.sha256(data).digest()

def main():
    if OUTPUT.exists(): raise ValueError('output exists')
    base = BASE.read_bytes()
    if sha(base).hex() != '9beaf7fe1b324129e7a977cb54830d924991e8a0dd90d8087a5063f4de04e1d0': raise ValueError('incorrect base')
    with zipfile.ZipFile(TARGET) as z:
        target = b''.join(z.read(f'assets/bin/Data/sharedassets0.assets.split{i}') for i in range(126))
    targets = {o.path_id: o for o in UnityPy.load(target).objects}
    files, sources = [], []
    for path in ZIPS:
        with zipfile.ZipFile(path) as z: files.append(z.read('Megabonk_Data/sharedassets0.assets'))
        sources.append({o.path_id: o for o in UnityPy.load(files[-1]).objects})
    records = json.loads(MANIFEST.read_text())['atlases']
    if len(records) != 94 or len({r['target_id'] for r in records}) != 94: raise ValueError('incorrect inventory')
    result = bytearray(base); rows = []
    for r in records:
        obj = targets[r['target_id']]; d = obj.read(); raw = obj.get_raw_data(); pixels = bytes(d.image_data); header = raw.find(pixels)
        if obj.type.name != 'Texture2D' or (d.m_Width,d.m_Height,d.m_MipCount,d.m_TextureFormat) != (1024,1024,1,47) or len(pixels) != 1048576 or header < 80 or raw.count(pixels) != 1 or raw[header+len(pixels):] != bytes(16) or any(base[obj.byte_start:obj.byte_start+len(raw)]): raise ValueError('invalid target texture')
        offsets=[]; images=[]; heads=[]
        for f,index in zip(files,sources):
            so=index[r['source_id']]; s=so.read(); sr=so.get_raw_data(); data=bytes(s.image_data); off=sr.find(data)
            if s.m_Name != d.m_Name or (s.m_Width,s.m_Height,s.m_MipCount,s.m_TextureFormat) != (1024,1024,1,1) or len(data)!=1048576 or sha(data).hex()!=r['pc_sha256'] or off!=header or sr.count(data)!=1: raise ValueError('invalid Steam atlas')
            offsets.append(so.byte_start);images.append(data);heads.append(sr[:off])
        if images[0]!=images[1] or heads[0]!=heads[1]: raise ValueError('platform source differs')
        header_data=bytearray(heads[0]); name_end=4+((len(d.m_Name.encode('utf8'))+3)&~3)
        struct.pack_into('<I',header_data,name_end+20,47);header_data[name_end+30]=0
        if bytes(header_data)!=raw[:header]: raise ValueError('target metadata differs')
        result[obj.byte_start:obj.byte_start+len(raw)]=raw
        rows.append((obj.byte_start,header,offsets[0],offsets[1],sha(heads[0]),sha(images[0]),sha(raw)))
    rows.sort();out=bytearray(b'OBS0A001'+struct.pack('>II',len(base),len(rows))+sha(base)+sha(result))
    for f in files: out.extend(struct.pack('>Q',len(f))+sha(f))
    for dst,header,linux,windows,hh,ih,oh in rows:out.extend(struct.pack('>IIQQ',dst,header,linux,windows)+hh+ih+oh)
    OUTPUT.parent.mkdir(parents=True,exist_ok=True)
    with OUTPUT.open('xb') as f:f.write(out)
    print('atlas plan',sha(out).hex(),'result',sha(result).hex(),'count',len(rows))

if __name__=='__main__':main()
