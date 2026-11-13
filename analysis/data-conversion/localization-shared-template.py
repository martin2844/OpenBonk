#!/usr/bin/env python3
"""Rebuild shared localization keys from a pinned Steam copy; local research only."""
import argparse,hashlib,importlib.util,json,shutil,struct,zlib,zipfile
from pathlib import Path
import lz4.block
import UnityPy
ROOT=Path(__file__).resolve().parents[2]
def load(n,p):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
locale=load('locale',ROOT/'analysis/data-conversion/localization-text-template.py')
BASE_SHA='ca78de997deb2ba43e2ea6c25ce44339934e355726b6c71aa71e96f343d9544c'
NAME='localization-assets-shared_assets_all.bundle'
MANIFEST_SHA='035de511eae73fde3c913869f38203294ce2177db672fbb3eac59ae34e372722'

def sha(b):return hashlib.sha256(b).hexdigest()
def source(steam):
 m=load('normalizer',ROOT/'tools/normalize-windows-localization.py');raw=m.MANIFEST.read_bytes()
 if sha(raw)!=m.MANIFEST_SHA:raise ValueError('Windows locale inventory differs')
 row=next(r for r in json.loads(raw)['bundles'] if r['name']==NAME)
 return locale.read_source(steam,NAME,row)
def tables(env):
 groups={}
 for ptr in env.container.values():
  t=ptr.read_typetree();g=t.get('m_TableCollectionNameGuidString')
  if not g:continue
  if g in groups:raise ValueError('duplicate collection GUID')
  groups[g]=t
 return groups

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('steam',type=Path);p.add_argument('input',type=Path);p.add_argument('output',type=Path);a=p.parse_args();out=a.output.absolute();src=a.steam.resolve();inp=a.input.resolve()
 if not src.is_dir() or not out.parent.is_dir() or out.exists() or out.is_symlink():p.error('input must exist and output must be new')
 for q in (src,inp):
  if out.resolve().is_relative_to(q) or q.is_relative_to(out.resolve()):p.error('output must be separate')
 groups=tables(source(src))
 if a.action=='template':
  if not inp.is_file() or locale.file_sha(inp)!=BASE_SHA:raise ValueError('incompatible base')
  with zipfile.ZipFile(inp) as z:bundle=z.read(locale.ANDROID+NAME)
  header,raw=locale.envelope(bundle);env=UnityPy.load(bundle);blank=bytearray(raw);ranges=[];spans=[];stats={'matched':0,'ambiguous':0,'different_or_empty':0}
  for path,ptr in env.container.items():
   obj=ptr.deref();target=obj.read_typetree();guid=target.get('m_TableCollectionNameGuidString');peer=groups.get(guid)
   if not guid or not peer:raise ValueError('missing shared collection: '+path)
   pairs={r['m_Id']:r['m_Key'] for r in peer['m_Entries']}
   if len(pairs)!=len(peer['m_Entries']):raise ValueError('duplicate source keys')
   data=obj.get_raw_data();offset=obj.byte_start
   if raw[offset:offset+len(data)]!=data:raise ValueError('object offset differs')
   for entry in target['m_Entries']:
    value=entry['m_Key'];v=value.encode('utf8')
    if not v or pairs.get(entry['m_Id'])!=value:stats['different_or_empty']+=1;continue
    pat=struct.pack('<I',len(v))+v
    if data.count(pat)!=1:stats['ambiguous']+=1;continue
    start=offset+data.index(pat)+4;end=start+len(v)
    if raw[start:end]!=v or any(start<e and s<end for s,e in spans):raise ValueError('overlapping key range')
    spans.append((start,end));blank[start:end]=bytes(len(v));ranges.append({'guid':guid,'id':entry['m_Id'],'offset':start,'length':len(v),'sha256':sha(v)});stats['matched']+=1
  if stats['matched']!=1127:raise ValueError('unexpected key coverage')
  packed=zlib.compress(bytes(blank),9);doc={'version':1,'base_obb_sha256':BASE_SHA,'bundle_sha256':sha(bundle),'header_sha256':sha(header),'raw_sha256':sha(raw),'blank_sha256':sha(blank),'packed_sha256':sha(packed),'raw_size':len(raw),'ranges':ranges,'stats':stats,'limitations':['Other content and metadata remain.','Not distributable.']}
  out.mkdir(mode=0o700)
  try:(out/'shared.header').write_bytes(header);(out/'shared.zlib').write_bytes(packed);(out/'manifest.json').write_text(json.dumps(doc,indent=2)+'\n')
  except BaseException:shutil.rmtree(out);raise
  print('Blanked',len(ranges),'shared keys;',sum(r['length'] for r in ranges),'bytes')
 else:
  manifest=inp/'manifest.json'
  if MANIFEST_SHA is None or locale.file_sha(manifest)!=MANIFEST_SHA:raise ValueError('incompatible shared template')
  doc=json.loads(manifest.read_text());header=(inp/'shared.header').read_bytes();packed=(inp/'shared.zlib').read_bytes()
  if sha(header)!=doc['header_sha256'] or sha(packed)!=doc['packed_sha256']:raise ValueError('shared template differs')
  raw=bytearray(zlib.decompress(packed))
  if len(raw)!=doc['raw_size'] or sha(raw)!=doc['blank_sha256']:raise ValueError('blank shared data differs')
  pairs={g:{r['m_Id']:r['m_Key'] for r in t['m_Entries']} for g,t in groups.items()};spans=[]
  for item in doc['ranges']:
   value=pairs[item['guid']][item['id']].encode('utf8');start=item['offset'];end=start+item['length']
   if len(value)!=item['length'] or sha(value)!=item['sha256'] or start<0 or end>len(raw) or any(raw[start:end]) or any(start<e and s<end for s,e in spans):raise ValueError('invalid shared key range')
   spans.append((start,end));raw[start:end]=value
  if sha(raw)!=doc['raw_sha256']:raise ValueError('restored shared data differs')
  bundle=header+lz4.block.compress(bytes(raw),mode='high_compression',compression=12,store_size=False)
  if sha(bundle)!=doc['bundle_sha256']:raise ValueError('restored shared bundle differs')
  out.mkdir(mode=0o700)
  try:(out/NAME).write_bytes(bundle)
  except BaseException:shutil.rmtree(out);raise
  print('Restored shared bundle exactly')
if __name__=='__main__':main()
