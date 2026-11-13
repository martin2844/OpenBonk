#!/usr/bin/env python3
"""Decode and compare six local FSB clips with host FMOD in no-sound mode.

This numeric comparison is not an Android playback test, a listening test,
or evidence of rights. No audio files are emitted.
"""
import argparse
import array
import hashlib
import importlib.util
import io
import json
import math
from pathlib import Path
import wave
import zipfile
import UnityPy
import fmod_toolkit
import pyfmodex
import pyfmodex.enums
import pyfmodex.flags
import pyfmodex.structure_declarations

OBB_SHA='ca2280993b055ff7ad2172169ba100c895e69781e67ad4af53b71b027a9159af'
RESOURCE_SHA='2f320598479bd723d63bb920cc582b62c95f5961e59eca85d9ca20f1782824f7'
ASSET_SHA='cd0a2e7c6f93ecc00bdde165a6654b3818c3e4afaaad2eb52068a93a62b1c29a'
spec=importlib.util.spec_from_file_location('compare',Path(__file__).with_name('compare-android-asset-objects.py'));compare=importlib.util.module_from_spec(spec);spec.loader.exec_module(compare)


def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda:f.read(1048576),b''):h.update(b)
    return h.hexdigest()
def require(path,h):
    if not path.is_file() or sha(path)!=h:raise ValueError('incompatible input: '+str(path))


def decode(system,data):
    sound=system.create_sound(data,pyfmodex.flags.MODE.OPENMEMORY,exinfo=pyfmodex.structure_declarations.CREATESOUNDEXINFO(length=len(data)))
    try:
        wavs=fmod_toolkit.sound_to_wav(sound,'clip')
        if len(wavs)!=1:raise ValueError('expected one FSB subsound')
        with wave.open(io.BytesIO(next(iter(wavs.values()))),'rb') as w:
            if w.getsampwidth()!=2:raise ValueError('expected 16-bit PCM')
            frames,channels,rate=w.getnframes(),w.getnchannels(),w.getframerate();samples=array.array('h',w.readframes(frames))
            if len(samples)!=frames*channels or not samples:raise ValueError('invalid decoded PCM')
            return frames,channels,rate,samples
    finally:sound.release()


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('original_obb',type=Path);p.add_argument('rebuilt_dir',type=Path);p.add_argument('report',type=Path);a=p.parse_args()
    if a.report.exists() or a.report.is_symlink() or not a.report.parent.is_dir():p.error('report must be new')
    require(a.original_obb,OBB_SHA);require(a.rebuilt_dir/'sharedassets4.resource',RESOURCE_SHA);require(a.rebuilt_dir/'sharedassets4.assets',ASSET_SHA)
    rebuilt=UnityPy.load(str(a.rebuilt_dir/'sharedassets4.assets'));new={o.read().m_Name:o.read() for o in rebuilt.objects if o.type.name=='AudioClip'}
    with zipfile.ZipFile(a.original_obb) as z:
        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entries')
        original=UnityPy.load(compare.load_archive(z,'sharedassets4.assets'));old_stream=z.read('assets/bin/Data/sharedassets4.resource')
    old={o.read().m_Name:o.read() for o in original.objects if o.type.name=='AudioClip'}
    if len(old)!=6 or old.keys()!=new.keys():raise ValueError('unexpected audio clip set')
    new_stream=(a.rebuilt_dir/'sharedassets4.resource').read_bytes();system=pyfmodex.System();system.output=pyfmodex.enums.OUTPUTTYPE.NOSOUND;system.init(32,pyfmodex.flags.INIT_FLAGS.NORMAL,None);results=[]
    try:
        for name in sorted(old):
            x,y=old[name].m_Resource,new[name].m_Resource
            if x.m_Offset+x.m_Size>len(old_stream) or y.m_Offset+y.m_Size>len(new_stream):raise ValueError('audio stream bounds')
            a0=decode(system,old_stream[x.m_Offset:x.m_Offset+x.m_Size]);a1=decode(system,new_stream[y.m_Offset:y.m_Offset+y.m_Size])
            if a0[:3]!=a1[:3]:raise ValueError('decoded PCM dimensions differ: '+name)
            frames,channels,rate,old_pcm=a0;new_pcm=a1[3];indices=(frame*channels+c for frame in range(0,frames,16) for c in range(channels))
            dot=old_power=new_power=0
            for i in indices:
                u,v=old_pcm[i],new_pcm[i];dot+=u*v;old_power+=u*u;new_power+=v*v
            if not old_power or not new_power:raise ValueError('silent decoded signal: '+name)
            results.append({'name':name,'frames':frames,'channels':channels,'sample_rate':rate,'seconds':frames/rate,'sampled_cosine_similarity':dot/math.sqrt(old_power*new_power),'sampling':'every 16th frame, all channels'})
    finally:system.release()
    report={'kind':'local host diagnostic only','clips':results,'limits':['FMOD desktop decoding, not Android playback or a listening test.','Cosine similarity is a sampled numeric metric, not proof of audible equivalence.','The clips and game content are not distribution artifacts.']}
    with a.report.open('x') as f:json.dump(report,f,indent=2);f.write('\n')
    for r in results:print(r['name'],'frames',r['frames'],'channels',r['channels'],'rate',r['sample_rate'],'sampled cosine',round(r['sampled_cosine_similarity'],5))


if __name__=='__main__':main()
