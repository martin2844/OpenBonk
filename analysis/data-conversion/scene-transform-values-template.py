#!/usr/bin/env python3
"""Blank and exactly restore scene transform values, preserving pointers.

Research only: templates retain game, Unity and third-party content and are
not cleared for distribution. Exact bytes do not establish object identity.
"""
import argparse
from collections import defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import zipfile
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('audit_transform',Path(__file__).with_name('audit-scene-transform-values.py'))
audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
SCENES=audit.SCENES
EXPECTED={'level1':{'Transform':293,'RectTransform':1834},'level2':{'Transform':1966,'RectTransform':1398},'level4':{'Transform':379,'RectTransform':1431}}
ORIGINAL={'level1':'164012e485062f572bba37de85aafe42a494810202d80e13d4f72dbf20ce24dc',
          'level2':'bbee6f3c886a795f0029626ed62ccc6d569fca3ce9ee821876619d3009a55dea',
          'level4':'665e167cbcc2fd9c43c46e1d2c84f83c6ca9e0ad6c9756f439e10b90d5758972'}
PARTIAL_BLANK={'level1':'36836545a5b43f63a3fd9da78122523ffb24520c63dc47e730da171e148d47b9',
               'level2':'b892998b260b74b1d772904db1c7c05d34d1d235f17393cad992e8eaf53d3c1b',
               'level4':'db89e220410dc4eaee34b6b3e8bd4d1a197ccf6866c2e75bdb4b49b3e1c89cb7'}
PARTIAL_RESTORED={'level1':'e41d19c6c793d02c4cb9feb91ef36705a11a0e6cffbe94761a0e5438406b8ec1',
                  'level2':'586774ac78a422b4c099e96708f149bc1ad44ac1ff3072d2d6b4f1104738304d',
                  'level4':'c0d640370c10021028599405116831995c31734a74e3a183446a73f72798589b'}


def sha(data):return hashlib.sha256(data).hexdigest()


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('action',choices=('template','restore'));p.add_argument('steam_data',type=Path)
    p.add_argument('input',type=Path,help='pinned original OBB or template directory');p.add_argument('output',type=Path)
    p.add_argument('--partial-base',action='store_true',help='restore transform values in the pinned base with other bodies still blank')
    p.add_argument('--windows-scenes',action='store_true',help='restore selected values from pinned Windows scenes')
    a=p.parse_args();out=a.output.absolute()
    if a.partial_base and a.action!='restore':p.error('--partial-base requires restore')
    if a.windows_scenes and a.action!='restore':p.error('--windows-scenes requires restore')
    if out.exists() or out.is_symlink() or not out.parent.is_dir():p.error('output must be new')
    for src in (a.steam_data,a.input):
        src=src.resolve()
        if out.resolve().is_relative_to(src) or src.is_relative_to(out.resolve()):p.error('output must be separate from inputs')
    pins=json.loads((ROOT/'tools/steam-linux-diagnostic-inputs.json').read_text())
    if a.windows_scenes:
        spec=importlib.util.spec_from_file_location('windows_scenes',Path(__file__).with_name('no-rewired-scene-bodies-prototype.py'))
        windows=importlib.util.module_from_spec(spec);spec.loader.exec_module(windows)
    original_manifest=None;archive=None
    if a.action=='template':
        with a.input.open('rb') as f:
            if hashlib.file_digest(f,'sha256').hexdigest()!=audit.OBB_SHA:raise ValueError('incompatible OBB')
        archive=zipfile.ZipFile(a.input)
        if len(archive.namelist())!=len(set(archive.namelist())):raise ValueError('duplicate OBB entries')
    else:
        original_manifest=json.loads((a.input/'manifest.json').read_text())
        if original_manifest.get('version')!=1 or original_manifest.get('obb_sha256')!=audit.OBB_SHA or set(original_manifest.get('scenes',{}))!=set(SCENES):raise ValueError('invalid manifest')
    results={};files={}
    try:
        for scene in SCENES:
            pc=(a.steam_data/scene).read_bytes()
            expected=windows.WINDOWS_SCENE_PINS[scene] if a.windows_scenes else pins[scene]['sha256']
            if sha(pc)!=expected:raise ValueError('incompatible Steam scene: '+scene)
            index=defaultdict(list);by_id={}
            for obj in UnityPy.load(pc).objects:
                if obj.type.name in ('Transform','RectTransform'):
                    values=audit.vectors(obj);index[(obj.type.name,values)].append(obj.path_id);by_id[obj.path_id]=(obj.type.name,values)
            if archive:
                prefix='assets/bin/Data/'+scene+'.split'
                parts=sorted((n for n in archive.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('invalid scene fragments')
                raw=b''.join(archive.read(n) for n in parts)
                if sha(raw)!=ORIGINAL[scene]:raise ValueError('incompatible Android scene: '+scene)
            else:
                raw=(a.input/scene).read_bytes();previous=original_manifest['scenes'][scene]
                if sha(raw)!=(PARTIAL_BLANK[scene] if a.partial_base else previous['template_sha256']) or previous['original_sha256']!=ORIGINAL[scene] or previous['steam_sha256']!=pins[scene]['sha256']:raise ValueError('incompatible template: '+scene)
            env=UnityPy.load(raw);work=bytearray(raw);rows=[];counts=defaultdict(int);ranges=[];nonzero=0
            prior={} if archive else {row['target_id']:row for row in previous['objects']}
            if not archive and len(prior)!=len(previous['objects']):raise ValueError('duplicate manifest target')
            for obj in env.objects:
                kind=obj.type.name
                if kind not in ('Transform','RectTransform'):continue
                actual=audit.vectors(obj);size=obj.byte_size
                if archive:
                    candidates=index.get((kind,actual),[])
                    if not candidates:raise ValueError('no exact value match: '+scene)
                    source_id=min(candidates);values=actual
                else:
                    row=prior.get(obj.path_id)
                    if row is None:raise ValueError('missing target record')
                    source_id=row['steam_id'];source_kind,values=by_id[source_id]
                    if source_kind!=kind or any(any(span) for span in actual):raise ValueError('incompatible blank object')
                spans=[(obj.byte_start+12,obj.byte_start+52,values[0])]
                if kind=='RectTransform':spans.append((obj.byte_start+size-40,obj.byte_start+size,values[1]))
                row={'target_id':obj.path_id,'type':kind,'steam_id':source_id,'value_sha256':sha(b''.join(values))}
                if not archive and row!=prior[obj.path_id]:raise ValueError('manifest value mismatch')
                for start,end,value in spans:
                    if start<obj.byte_start or end>obj.byte_start+size or end-start!=40:raise ValueError('invalid value range')
                    ranges.append((start,end))
                    if archive:
                        if bytes(work[start:end])!=value:raise ValueError('original value mismatch')
                        nonzero+=sum(x!=0 for x in value);work[start:end]=bytes(40)
                    else:
                        if any(work[start:end]):raise ValueError('nonblank template range')
                        work[start:end]=value
                rows.append(row);counts[kind]+=1
            if dict(counts)!=EXPECTED[scene] or (not archive and len(rows)!=len(prior)):raise ValueError('unexpected scene coverage')
            spans=sorted(ranges)
            if any(spans[i][1]>spans[i+1][0] for i in range(len(spans)-1)):raise ValueError('overlapping ranges')
            result=bytes(work)
            if not archive and sha(result)!=(PARTIAL_RESTORED[scene] if a.partial_base else ORIGINAL[scene]):raise ValueError('restored scene differs')
            # Every byte outside these exact ranges must remain untouched.
            cursor=0
            for start,end in spans:
                if raw[cursor:start]!=result[cursor:start]:raise ValueError('outside value range changed')
                cursor=end
            if raw[cursor:]!=result[cursor:]:raise ValueError('trailing data changed')
            files[scene]=result
            results[scene]={'original_sha256':ORIGINAL[scene], 'template_sha256':sha(result) if archive else previous['template_sha256'],
                            'steam_sha256':pins[scene]['sha256'], 'objects':rows, 'counts':dict(counts),
                            'selected_bytes':sum(end-start for start,end in spans), 'nonzero_bytes':nonzero if archive else previous['nonzero_bytes']}
        if not archive and results!=original_manifest['scenes']:raise ValueError('restored records differ')
        report={'version':1,'obb_sha256':audit.OBB_SHA,'scenes':results,'limitations':'Local research only; references and other game and third-party content remain.'}
        out.mkdir(mode=0o700)
        try:
            for name,data in files.items():(out/name).write_bytes(data)
            (out/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
        except BaseException:shutil.rmtree(out);raise
    finally:
        if archive:archive.close()
    print(a.action, sum(sum(x['counts'].values()) for x in results.values()),'objects;',sum(x['selected_bytes'] for x in results.values()),'value bytes; research only')


if __name__=='__main__':main()
