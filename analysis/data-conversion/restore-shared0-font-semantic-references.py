#!/usr/bin/env python3
"""Rebuild six font objects using Steam bytes and resolved Unity references.

Reference offsets were measured from one pair of builds. No target reference
values are stored in the offset manifest. Experimental; other game material
remains and neither inputs nor outputs are cleared for distribution.
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import shutil
import struct
import zipfile

import UnityPy

ROOT=Path(__file__).resolve().parent
OFFSETS=ROOT/'no-rewired-font-reference-offsets.json'
OFFSETS_SHA='b7c07fd58ed100af55cb02737a2c76a642dd76a0fd9fed1d5eb92d231a23c866'
PC_GLOBAL_SHA='b3f7db2d847edd89f66e387a9bbc926b5c555314a23ca55a0531cf662c89bbe0'
WINDOWS_ASSET_SHA='772b0a4fc30592c6da17667e532efd0674f76eb7eb5ad24fc5673229b8fe525d'
WINDOWS_GLOBAL_SHA='9d7146c2d338034b798aec477ce5c3e494a9cc0969861c2706996bf69ae16915'
WINDOWS_FONT_SHA={
    133:'d4e328733901478d41de448493639687b616afb454e75c8e3089262d64fbf02f',
    134:'47a61ef26a6ffa775a6bbfb4af7af40085b81970f9eb585191374c80a388ab0a',
    135:'ec581932bed1d2c602688d2dd36c37f04cba631270ca2a2fa92cf60845e482b0',
    136:'47f6c79f158812becc7563f22220631a3eefaf7323451ae716d36f489c6edc4a',
    137:'982b0bf222549b2d1201a6174ebb05a5b6256c6331763e554023dc0d08f116f7',
    138:'0cd388d71d605d9325a596cbbb92d4b5aa2d24aeb2e2b69f53a6332b911f68ba',
}
APK_SHAS={'8e7fb232d255905840cc34b8efd3a72b0dbc3693c8a34635ff85f5c95e3b0892',
          'dad55ad51e47593bf457538bc20d2078bbaa545627505681fc89088a4c68b37c',
          'fc550a92a5afd94af4a47076b6f9bad034d764708c617fc9a65cb13e8fe27d86',
          'd20c58a2548196270e57b6e0392b4a103e09014ee4f01d4f4b970bb2924026a2',
          '6b8ccf42cf51cdcac9bbfab3a2ced1ad7a0e5b0485c2efa671d2d26d57cf8b27',
          '5e251631fe41877dcd53f101346a3c19e75714971b4e0f72d765083012a4f27a', '9ae20660c192b251fc63a7831eb6c44a61d724d4b961e0a58bb4da47b3fc6bd4','f756235db25a5aed06afb34ec7e0267aa49cf770eb148e0e3cd048674b13e93a'}


def digest(b):return hashlib.sha256(b).hexdigest()


def sha(p):
    with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()


def objects(env):
    result={o.path_id:o for o in env.objects}
    if len(result)!=len(env.objects):raise ValueError('duplicate object ID')
    return result


def named(obj):
    typ=obj.type.name
    if typ not in ('Texture2D','Material','Font','MonoBehaviour'):return None
    raw=obj.get_raw_data();offset=28 if typ=='MonoBehaviour' else 0
    if len(raw)<offset+4:return None
    size=struct.unpack_from('<I',raw,offset)[0]
    if size>256 or offset+4+size>len(raw):return None
    try:value=raw[offset+4:offset+4+size].decode('utf-8')
    except UnicodeError:return None
    return (typ,value) if value else None


def script_identity(obj):
    if obj.type.name!='MonoScript':return None
    x=obj.read()
    return (x.m_Name,x.m_ClassName,x.m_Namespace,x.m_AssemblyName)


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('android_apk',type=Path);p.add_argument('blank_asset',type=Path);p.add_argument('output',type=Path);p.add_argument('--windows-source',action='store_true',help='use pinned Windows font metadata and script references');a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    out=a.output.resolve()
    for path in (a.pc_data,a.android_apk,a.blank_asset):
        path=path.resolve()
        if out.is_relative_to(path) or path.is_relative_to(out):p.error('output must be separate from inputs')
    if sha(OFFSETS)!=OFFSETS_SHA or sha(a.android_apk) not in APK_SHAS:raise ValueError('unexpected APK or offset manifest')
    manifest=json.loads(OFFSETS.read_text());steam=a.pc_data/'sharedassets0.assets';global_pc=a.pc_data/'globalgamemanagers.assets'
    if sha(steam)!=(WINDOWS_ASSET_SHA if a.windows_source else manifest['source_asset_sha256']) or sha(global_pc)!=(WINDOWS_GLOBAL_SHA if a.windows_source else PC_GLOBAL_SHA) or sha(a.blank_asset)!=manifest['target_blank_sha256']:raise ValueError('unexpected data input')
    pc_env=UnityPy.load(str(steam));blank=a.blank_asset.read_bytes();dst_env=UnityPy.load(blank)
    if not pc_env.file.externals or not dst_env.file.externals or pc_env.file.externals[0].path!='globalgamemanagers.assets' or dst_env.file.externals[0].path!='globalgamemanagers.assets':raise ValueError('unexpected external reference')
    source,dest=objects(pc_env),objects(dst_env)
    with zipfile.ZipFile(a.android_apk) as z:
        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate APK entry')
        prefix='assets/bin/Data/globalgamemanagers.assets.split';parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
        if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('missing script metadata')
        android_global=objects(UnityPy.load(b''.join(z.read(n) for n in parts)))
    pc_global=objects(UnityPy.load(str(global_pc)));script_index=collections.defaultdict(list)
    for i,o in android_global.items():
        if o.type.name=='MonoScript':script_index[script_identity(o)].append(i)
    reserved={r['target_id']:('MonoBehaviour',r['name']) for r in manifest['objects']}
    index=collections.defaultdict(list)
    for i,o in dest.items():
        key=reserved.get(i) or named(o)
        if key:index[key].append(i)
    if len(manifest['objects'])!=6 or len(reserved)!=6:raise ValueError('invalid font inventory')
    work=bytearray(blank);reports=[]
    for rec in manifest['objects']:
        tid,sid=rec['target_id'],rec['source_id'];target,src=dest[tid],source[sid]
        raw=src.get_raw_data();old=target.get_raw_data()
        if (target.type.name!='MonoBehaviour' or src.type.name!='MonoBehaviour' or named(src)!=('MonoBehaviour',rec['name'])
                or len(raw)!=rec['size'] or len(old)!=len(raw) or any(old) or digest(raw)!=(WINDOWS_FONT_SHA.get(sid) if a.windows_source else rec['source_sha256'])):raise ValueError('unexpected font object')
        rebuilt=bytearray(raw);offsets=rec['reference_offsets']
        if len(offsets)!=len(set(offsets)) or offsets!=sorted(offsets):raise ValueError('invalid reference offsets')
        for offset in offsets:
            if offset<12 or offset+12>len(raw):raise ValueError('reference outside object')
            file_id,path_id=struct.unpack_from('<iq',raw,offset)
            if file_id==0:
                if path_id not in source:raise ValueError('missing Steam local reference')
                key=named(source[path_id]);choices=index.get(key,[])
                if key is None or len(choices)!=1:raise ValueError('ambiguous local reference: '+str(key))
                target_id=choices[0]
            elif file_id==1:
                if path_id not in pc_global:raise ValueError('missing Steam script reference')
                key=script_identity(pc_global[path_id]);choices=script_index.get(key,[])
                if key!=('TMP_FontAsset','TMP_FontAsset','TMPro','Unity.TextMeshPro') or len(choices)!=1:raise ValueError('ambiguous font script')
                target_id=choices[0]
            else:raise ValueError('unexpected reference file')
            struct.pack_into('<iq',rebuilt,offset,file_id,target_id)
        if digest(rebuilt)!=rec['target_sha256']:raise ValueError('semantic reconstruction differs: '+rec['name'])
        start=target.byte_start
        if start+len(rebuilt)>len(work):raise ValueError('font outside asset')
        work[start:start+len(rebuilt)]=rebuilt
        reports.append({'target_id':tid,'source_id':sid,'name':rec['name'],'resolved_references':len(offsets),'sha256':digest(rebuilt)})
    if digest(work)!=manifest['target_full_sha256']:raise ValueError('reconstructed asset mismatch')
    a.output.mkdir()
    try:
        (a.output/'sharedassets0.assets').write_bytes(work)
        report={'kind':'local diagnostic only, not distributable','sha256':digest(work),'fonts':reports,
                'reference_count':sum(r['resolved_references'] for r in reports),
                'limitations':['Offsets were derived from Android build comparisons.','Names and types may not establish provenance or rights.','Other game material and code remain.']}
        (a.output/'report.json').write_text(json.dumps(report,indent=2)+'\n')
        print('reconstructed',len(reports),'fonts and resolved',report['reference_count'],'references; SHA256',digest(work),flush=True)
    except BaseException:
        shutil.rmtree(a.output);raise


if __name__=='__main__':main()
