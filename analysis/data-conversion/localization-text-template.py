#!/usr/bin/env python3
"""Experimental lossless Windows-Steam reconstruction of Android locale text.

Only uniquely located, matching translated strings are blanked. The template
retains other game content and is not distributable.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import struct
import zlib
import zipfile
import lz4.block
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
BASE_SHA='ca0fc86fa3f1344191b04e4be6f0fb05a97fc4bbdb6e0f4dac105b94b3aa7f21'
MANIFEST_SHA='0d3a6cc6ae23ea7b915664358b0cc619a1f5f450b595c1b9029249ac67e42cef'
PREFIX='StreamingAssets/aa/StandaloneWindows64/'
ANDROID='assets/aa/Android/'


def sha(data):return hashlib.sha256(data).hexdigest()
def file_sha(path):
 with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()


def source_rows():
 spec=importlib.util.spec_from_file_location('locale_normalizer',ROOT/'tools/normalize-windows-localization.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
 raw=m.MANIFEST.read_bytes()
 if sha(raw)!=m.MANIFEST_SHA:raise ValueError('localization inventory mismatch')
 rows=json.loads(raw)['bundles']
 return {r['name']:r for r in rows if r['name'].startswith('localization-string-tables-')}


def envelope(data):
 pos=0
 def text():
  nonlocal pos
  end=data.index(b'\0',pos);v=data[pos:end];pos=end+1;return v
 def num(fmt):
  nonlocal pos
  n=struct.calcsize(fmt);v=struct.unpack_from(fmt,data,pos)[0];pos+=n;return v
 if text()!=b'UnityFS' or num('>I')!=8 or text()!=b'5.x.x' or text()!=b'2023.2.22f1':raise ValueError('unexpected UnityFS header')
 if num('>Q')!=len(data):raise ValueError('bundle size differs')
 ci,ui,flags=num('>I'),num('>I'),num('>I')
 if flags!=0x243 or not 0<ci<1024*1024 or not 0<ui<1024*1024:raise ValueError('unsupported bundle flags')
 pos=(pos+15)&~15;packed_info=data[pos:pos+ci];info=lz4.block.decompress(packed_info,uncompressed_size=ui);pos=(pos+ci+15)&~15
 if len(info)<34 or struct.unpack_from('>I',info,16)[0]!=1:raise ValueError('unsupported block count')
 unc,compressed,kind=struct.unpack_from('>IIH',info,20)
 if kind!=3 or pos+compressed!=len(data) or unc>8*1024*1024:raise ValueError('unsupported data block')
 raw=lz4.block.decompress(data[pos:],uncompressed_size=unc)
 if lz4.block.compress(raw,mode='high_compression',compression=12,store_size=False)!=data[pos:]:raise ValueError('original compression cannot be reproduced')
 return data[:pos],raw


def read_source(source,name,row):
 p=source/PREFIX/name
 if not p.is_file():
  p=source/'StreamingAssets/aa/StandaloneLinux64'/name
  spec=importlib.util.spec_from_file_location('original_data',ROOT/'tools/prepare-original-data.py');original=importlib.util.module_from_spec(spec);spec.loader.exec_module(original)
  expected=dict(zip(original.BUNDLES,original.BUNDLE_SHA256)).get(name)
  if not p.is_file() or not 0<p.stat().st_size<=8*1024*1024 or file_sha(p)!=expected:raise ValueError('unsupported Steam locale: '+name)
 elif p.stat().st_size!=row['windows_size'] or file_sha(p)!=row['windows_sha256']:raise ValueError('unsupported Steam locale: '+name)
 return UnityPy.load(str(p))


def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('steam',type=Path);p.add_argument('input',type=Path);p.add_argument('output',type=Path);a=p.parse_args();out=a.output.absolute();source=a.steam.resolve();inp=a.input.resolve()
 if not source.is_dir() or not out.parent.is_dir() or out.exists() or out.is_symlink():p.error('source and parent must exist; output must be new')
 for path in (source,inp):
  if out.resolve().is_relative_to(path) or path.is_relative_to(out.resolve()):p.error('output must be separate from inputs')
 rows=source_rows();records=[];result={}
 if a.action=='template':
  if not inp.is_file() or file_sha(inp)!=BASE_SHA:raise ValueError('incompatible base OBB')
  with zipfile.ZipFile(inp) as z:
   for name,source_spec in rows.items():
    bundle=z.read(ANDROID+name);header,raw=envelope(bundle);dst=UnityPy.load(bundle);src=read_source(source,name,source_spec);src_containers={k.lower():v for k,v in src.container.items()};work=bytearray(raw);changes=[];used=[];stats={'equal':0,'ambiguous':0,'empty_or_different':0}
    for path,ptr in dst.container.items():
     peer=src_containers.get(path.lower())
     if peer is None:raise ValueError('missing matching table: '+path)
     obj=ptr.deref();target=obj.read_typetree();steam=peer.read_typetree()
     if 'm_TableData' not in target or 'm_TableData' not in steam:continue
     lookup={item['m_Id']:item['m_Localized'] for item in steam['m_TableData']}
     if len(lookup)!=len(steam['m_TableData']):raise ValueError('duplicate source ids')
     objraw=obj.get_raw_data();start=obj.byte_start
     if raw[start:start+len(objraw)]!=objraw:raise ValueError('serialized object offset differs')
     for item in target['m_TableData']:
      value=item['m_Localized'];encoded=value.encode('utf-8')
      if not encoded or lookup.get(item['m_Id'])!=value:stats['empty_or_different']+=1;continue
      pattern=struct.pack('<I',len(encoded))+encoded
      if objraw.count(pattern)!=1:stats['ambiguous']+=1;continue
      offset=start+objraw.index(pattern)+4;end=offset+len(encoded)
      if any(offset<e and s<end for s,e in used) or raw[offset:end]!=encoded:raise ValueError('overlapping text range')
      used.append((offset,end));work[offset:end]=bytes(len(encoded));changes.append({'table':path,'id':item['m_Id'],'offset':offset,'length':len(encoded),'sha256':sha(encoded)});stats['equal']+=1
    if not changes:raise ValueError('no matching text: '+name)
    blank=bytes(work);packed=zlib.compress(blank,9)
    records.append({'name':name,'bundle_sha256':sha(bundle),'source_sha256':source_spec['windows_sha256'],'header_sha256':sha(header),'raw_sha256':sha(raw),'blank_sha256':sha(blank),'packed_sha256':sha(packed),'raw_size':len(raw),'ranges':changes,'stats':stats})
    result[name]=(header,packed)
    print(name,stats,'removed bytes',sum(x['length'] for x in changes),flush=True)
  if len(records)!=len(rows):raise ValueError('locale inventory mismatch')
  doc={'version':1,'base_obb_sha256':BASE_SHA,'locales':records,'limitations':['Android metadata and unmatched content remain.','Template contains game material.','Not a distributable package.']}
  out.mkdir(mode=0o700)
  try:
   for name,(header,packed) in result.items():
    (out/(name+'.header')).write_bytes(header);(out/(name+'.zlib')).write_bytes(packed)
   (out/'manifest.json').write_text(json.dumps(doc,indent=2,ensure_ascii=True)+'\n')
  except BaseException:shutil.rmtree(out);raise
 else:
  manifest=inp/'manifest.json'
  if MANIFEST_SHA is None or file_sha(manifest)!=MANIFEST_SHA:raise ValueError('incompatible template manifest')
  doc=json.loads(manifest.read_text());records=doc['locales']
  if doc['version']!=1 or doc['base_obb_sha256']!=BASE_SHA or {r['name'] for r in records}!=set(rows):raise ValueError('incompatible locale inventory')
  for rec in records:
   name=rec['name'];src=read_source(source,name,rows[name]);tables={k.lower():v for k,v in src.container.items()};cache={}
   header=(inp/(name+'.header')).read_bytes();packed=(inp/(name+'.zlib')).read_bytes()
   if sha(header)!=rec['header_sha256'] or sha(packed)!=rec['packed_sha256']:raise ValueError('template hash differs: '+name)
   raw=bytearray(zlib.decompress(packed));
   if len(raw)!=rec['raw_size'] or sha(raw)!=rec['blank_sha256']:raise ValueError('blank locale differs: '+name)
   spans=[]
   for item in rec['ranges']:
    table=item['table'].lower()
    if table not in cache:
     t=tables[table].read_typetree();cache[table]={v['m_Id']:v['m_Localized'] for v in t['m_TableData']}
    value=cache[table][item['id']].encode('utf-8');start=item['offset'];end=start+item['length']
    if len(value)!=item['length'] or sha(value)!=item['sha256'] or start<0 or end>len(raw) or any(raw[start:end]) or any(start<e and s<end for s,e in spans):raise ValueError('invalid text range: '+name)
    spans.append((start,end));raw[start:end]=value
   if sha(raw)!=rec['raw_sha256']:raise ValueError('restored serialized data differs: '+name)
   bundle=header+lz4.block.compress(bytes(raw),mode='high_compression',compression=12,store_size=False)
   if sha(bundle)!=rec['bundle_sha256']:raise ValueError('restored bundle differs: '+name)
   result[name]=bundle;print('Restored',name,flush=True)
  out.mkdir(mode=0o700)
  try:
   for name,bundle in result.items():(out/name).write_bytes(bundle)
  except BaseException:shutil.rmtree(out);raise
 print(a.action,len(records),'locales; private research only')
if __name__=='__main__':main()
