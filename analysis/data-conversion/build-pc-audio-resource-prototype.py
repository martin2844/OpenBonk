#!/usr/bin/env python3
"""Rebuild a pinned experimental Android audio resource from local PC clips.

Research prototype; the output contains original game audio and is not a release.
"""
import collections
import argparse
import hashlib
from pathlib import Path
import sys
import tempfile
import os
import UnityPy

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('pc', type=Path); p.add_argument('template', type=Path); p.add_argument('output', type=Path)
p.add_argument('--windows-source', action='store_true', help='use the pinned Windows build for this audio reconstruction')
a = p.parse_args(); pc, template, output = a.pc, a.template, a.output
pc_assets, pc_audio = pc/'sharedassets1.assets', pc/'sharedassets1.resource'
pins = {pc_assets: '64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a' if a.windows_source else 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35',
        pc_audio: '29c8f2252d881f0ad039c58a30e8773a45b65ed3a3cbee9f31789350dc12d50d' if a.windows_source else '28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c'}
supported_templates = {'bee3b76040b93f56d75b0285ed82f87b98bb98d1f1f070508b27026533e54ced',
                       '6a67b08a6e37daa4689c2e032a0bc025b307797be2b4524f4f98399cd70c9bdd',
                       '79deb0ef0a7004d91826e6e8167349fbae0506c92a8a7d31ae076a92732eee37',
                       '8d0aeb32b37ab732845a22e73ec46d7050d23b011477890c722732449376e82b',
                       'fc8b3b9c3f292b0c27e84748148adfb5329e85e92556bf134cd06a49d025af44',
                       '7dafb88a825844b3730b2e862a16bc6e02bf17245515b2fe6e4beebeecdf4425',
                       'fa763a763c1db77463960dae22ab580338b6f65e4a0a5833552bbae25b178bd5',
                       'ea7774b1dc49e5a48f362bfd88d1a876e53ce04f27bdaf8fb52edb4b4bcdebf6',
                       'acb3f7549cb24e2604b8d5e827c6a79bbe504357a6aa161f163f1e02b49ae92d',
                       '9715a5ee8c6e589ece19b7996e48fa839eb1cf537e29d3a10e403f466fb760cd',
                       'b17ab5d19b53a2c5a076e5d18175a16ddeda9b68cf947add6b901b160466cec3',
                       'cc947601ab5e1d65c06afda412260f36d2d22bc66a818f47ac5b94c7c2227e7b'}
expected_output = '93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6'
def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda: source.read(1024*1024),b''): h.update(block)
    return h.hexdigest()
if output.exists(): raise SystemExit('output already exists')
if not output.parent.is_dir(): raise SystemExit('output parent does not exist')
for path, expected in pins.items():
    if not path.is_file() or digest(path) != expected: raise SystemExit(f'unsupported input: {path}')
if not template.is_file() or digest(template) not in supported_templates:
    raise SystemExit(f'unsupported template: {template}')
sources=collections.defaultdict(list)
for obj in UnityPy.load(str(pc_assets)).objects:
    if obj.type.name=='AudioClip':
        clip=obj.read(); sources[clip.m_Name].append(clip)
clips=[]
attrs=('m_Length','m_Channels','m_Frequency','m_BitsPerSample','m_LoadType','m_CompressionFormat','m_SubsoundIndex')
for obj in UnityPy.load(str(template)).objects:
    if obj.type.name!='AudioClip': continue
    clip=obj.read(); matches=sources[clip.m_Name]
    if len(matches)!=1 or any(getattr(clip,k,None)!=getattr(matches[0],k,None) for k in attrs):
        raise ValueError(f'missing or incompatible source: {clip.m_Name}')
    s,d=matches[0].m_Resource,clip.m_Resource
    if s.m_Source!='sharedassets1.resource' or d.m_Source!='sharedassets1.resource' or s.m_Size!=d.m_Size or s.m_Size<=0 or s.m_Offset+s.m_Size>pc_audio.stat().st_size or d.m_Offset%32:
        raise ValueError(f'invalid audio stream: {clip.m_Name}')
    clips.append((d.m_Offset,s.m_Offset,s.m_Size,clip.m_Name))
clips.sort()
if len(clips)!=118: raise ValueError(f'unexpected clip count: {len(clips)}')
position=0
for dst,src,size,name in clips:
    if dst<position or dst-position>=32: raise ValueError(f'overlapping or unexpected padding: {name}')
    position=dst+size
temporary=None
try:
    with tempfile.NamedTemporaryFile(prefix='.openbonk-audio-',dir=output.parent,delete=False) as tmp:
        temporary=Path(tmp.name)
        with pc_audio.open('rb') as source:
            for dst,src,size,name in clips:
                tmp.write(bytes(dst-tmp.tell())); source.seek(src); data=source.read(size)
                if len(data)!=size or not data.startswith(b'FSB5'): raise ValueError(f'invalid FSB: {name}')
                tmp.write(data)
    if digest(temporary)!=expected_output: raise ValueError('reconstructed audio resource hash mismatch')
    os.link(temporary,output)
    print('Rebuilt',len(clips),'local PC audio streams;',output.stat().st_size,'bytes; sha256',expected_output)
    print('Contains original game audio; research use only, not a release artifact.')
finally:
    if temporary is not None: temporary.unlink(missing_ok=True)
