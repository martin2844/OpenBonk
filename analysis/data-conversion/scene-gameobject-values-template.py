#!/usr/bin/env python3
"""Test exact recovery of GameObject value tails without changing component pointers.

Research only: the resulting templates contain other game and third-party data.
"""
import argparse
from collections import defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import struct
import zipfile
import UnityPy

ROOT = Path(__file__).resolve().parents[2]
SCENES = ('level1', 'level2', 'level4')
OBB_SHA = 'ca916fd870e4546c70a66428591272ef155987140495c1a0dd446a332b578cc2'
ORIGINAL = {'level1':'164012e485062f572bba37de85aafe42a494810202d80e13d4f72dbf20ce24dc', 'level2':'bbee6f3c886a795f0029626ed62ccc6d569fca3ce9ee821876619d3009a55dea', 'level4':'665e167cbcc2fd9c43c46e1d2c84f83c6ca9e0ad6c9756f439e10b90d5758972'}
COUNTS = {'level1':2126, 'level2':3364, 'level4':1810}
PARTIAL_BLANK = {'level1':'4ef79f82763aeacdae8a974b1c7271b5dc326135239b24cf52a1a58ba615ed49','level2':'3f16cc994c2de27827b8ff48b1d99854af42235bf5445d7012204c7510dd705d','level4':'a77f4803f3eea8f7e627884448f0c2a0f5c51d606ab664e96575f006f6323e43'}
PARTIAL_RESTORED = {'level1':'36836545a5b43f63a3fd9da78122523ffb24520c63dc47e730da171e148d47b9','level2':'b892998b260b74b1d772904db1c7c05d34d1d235f17393cad992e8eaf53d3c1b','level4':'db89e220410dc4eaee34b6b3e8bd4d1a197ccf6866c2e75bdb4b49b3e1c89cb7'}

def sha(b): return hashlib.sha256(b).hexdigest()

def value(obj):
    tree = obj.read_typetree(); raw = obj.get_raw_data()
    offset = 4 + 12 * len(tree['m_Component'])
    name = tree['m_Name'].encode('utf-8')
    expected = (struct.pack('<iI', tree['m_Layer'], len(name)) + name +
                bytes((-len(name)) % 4) + struct.pack('<HB', tree['m_Tag'], tree['m_IsActive']))
    if raw[offset:] != expected: raise ValueError('unexpected GameObject serialization')
    return offset, expected

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('action',choices=('template','restore')); p.add_argument('steam_data',type=Path)
    p.add_argument('input',type=Path); p.add_argument('output',type=Path)
    p.add_argument('--partial-base',action='store_true')
    p.add_argument('--windows-scenes',action='store_true',help='restore selected values from pinned Windows scenes')
    a=p.parse_args(); out=a.output.absolute()
    if a.partial_base and a.action!='restore': p.error('--partial-base requires restore')
    if a.windows_scenes and a.action!='restore': p.error('--windows-scenes requires restore')
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    for src in (a.steam_data,a.input):
        src=src.resolve()
        if out.resolve().is_relative_to(src) or src.is_relative_to(out.resolve()): p.error('output must be separate from inputs')
    pins=json.loads((ROOT/'tools/steam-linux-diagnostic-inputs.json').read_text())
    if a.windows_scenes:
        spec=importlib.util.spec_from_file_location('windows_scenes',Path(__file__).with_name('no-rewired-scene-bodies-prototype.py'))
        windows=importlib.util.module_from_spec(spec);spec.loader.exec_module(windows)
    archive=None; previous=None
    if a.action=='template':
        if sha(a.input.read_bytes())!=OBB_SHA: raise ValueError('incompatible OBB')
        archive=zipfile.ZipFile(a.input)
        if len(archive.namelist())!=len(set(archive.namelist())): raise ValueError('duplicate entries')
    else:
        previous=json.loads((a.input/'manifest.json').read_text())
        if previous.get('version')!=1 or previous.get('obb_sha256')!=OBB_SHA or set(previous.get('scenes',{}))!=set(SCENES): raise ValueError('invalid manifest')
    files={}; results={}
    try:
        for scene in SCENES:
            pc=(a.steam_data/scene).read_bytes()
            expected=windows.WINDOWS_SCENE_PINS[scene] if a.windows_scenes else pins[scene]['sha256']
            if sha(pc)!=expected: raise ValueError('incompatible Steam scene')
            index=defaultdict(list); by_id={}
            for obj in UnityPy.load(pc).objects:
                if obj.type.name=='GameObject':
                    _,v=value(obj); index[v].append(obj.path_id); by_id[obj.path_id]=v
            if archive:
                prefix='assets/bin/Data/'+scene+'.split'
                parts=sorted((n for n in archive.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))): raise ValueError('invalid fragments')
                raw=b''.join(archive.read(n) for n in parts)
                if sha(raw)!=ORIGINAL[scene]: raise ValueError('incompatible Android scene')
                prior={}
            else:
                raw=(a.input/scene).read_bytes(); manifest=previous['scenes'][scene]
                if sha(raw)!=(PARTIAL_BLANK[scene] if a.partial_base else manifest['template_sha256']) or manifest['original_sha256']!=ORIGINAL[scene] or manifest['steam_sha256']!=pins[scene]['sha256']: raise ValueError('incompatible template')
                prior={row['target_id']:row for row in manifest['objects']}
                if len(prior)!=len(manifest['objects']): raise ValueError('duplicate target')
            work=bytearray(raw); rows=[]; ranges=[]; nonzero=0
            for obj in UnityPy.load(raw).objects:
                if obj.type.name!='GameObject': continue
                if archive:
                    offset,v=value(obj); candidates=index.get(v,[])
                    if not candidates: continue
                    source_id=min(candidates)
                else:
                    row=prior.get(obj.path_id)
                    if row is None: continue
                    source_id=row['steam_id']; v=by_id[source_id]
                    # The array count is intact; do not parse the blanked value tail.
                    data=obj.get_raw_data(); count=struct.unpack_from('<I',data)[0]; offset=4+12*count
                    if offset+len(v)!=len(data): raise ValueError('unexpected blank object length')
                start=obj.byte_start+offset; end=start+len(v)
                if start<obj.byte_start or end!=obj.byte_start+obj.byte_size: raise ValueError('invalid range')
                record={'target_id':obj.path_id,'steam_id':source_id,'value_sha256':sha(v)}
                if not archive and record!=row: raise ValueError('manifest mismatch')
                if archive:
                    if bytes(work[start:end])!=v: raise ValueError('original differs')
                    nonzero+=sum(c!=0 for c in v); work[start:end]=bytes(len(v))
                else:
                    if any(work[start:end]): raise ValueError('range is not blank')
                    work[start:end]=v
                ranges.append((start,end)); rows.append(record)
            if len(rows)!=COUNTS[scene] or (not archive and len(rows)!=len(prior)): raise ValueError('unexpected coverage')
            ranges.sort()
            if any(ranges[i][1]>ranges[i+1][0] for i in range(len(ranges)-1)): raise ValueError('overlapping ranges')
            result=bytes(work)
            if not archive and sha(result)!=(PARTIAL_RESTORED[scene] if a.partial_base else ORIGINAL[scene]): raise ValueError('restored scene differs')
            cursor=0
            for start,end in ranges:
                if raw[cursor:start]!=result[cursor:start]: raise ValueError('outside range changed')
                cursor=end
            if raw[cursor:]!=result[cursor:]: raise ValueError('outside range changed')
            files[scene]=result
            results[scene]={'original_sha256':ORIGINAL[scene], 'template_sha256':sha(result) if archive else manifest['template_sha256'], 'steam_sha256':pins[scene]['sha256'], 'objects':rows, 'selected_bytes':sum(e-s for s,e in ranges), 'nonzero_bytes':nonzero if archive else manifest['nonzero_bytes']}
        if not archive and results!=previous['scenes']: raise ValueError('restored manifest differs')
        out.mkdir(mode=0o700)
        try:
            for name,data in files.items(): (out/name).write_bytes(data)
            (out/'manifest.json').write_text(json.dumps({'version':1,'obb_sha256':OBB_SHA,'scenes':results,'limitations':'Research only; references and other game and third-party content remain.'},indent=2)+'\n')
        except BaseException:
            shutil.rmtree(out); raise
    finally:
        if archive: archive.close()
    print(a.action, sum(len(x['objects']) for x in results.values()),'objects;',sum(x['selected_bytes'] for x in results.values()),'bytes')

if __name__=='__main__': main()
