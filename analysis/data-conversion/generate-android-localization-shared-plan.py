#!/usr/bin/env python3
"""Append Steam-derived shared locale keys to the private text-bundle plan."""
import hashlib,importlib.util,json,struct,zipfile,zlib
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';TD=B/'localization-shared-template'; TEXT=B/'localization-text-android-plan/plan.bin';OUT=B/'localization-complete-android-plan/plan.bin';NAME='localization-assets-shared_assets_all.bundle'
ZIPS=[B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip']
s=importlib.util.spec_from_file_location('locale',ROOT/'analysis/data-conversion/localization-text-template.py');locale=importlib.util.module_from_spec(s);s.loader.exec_module(locale)
def sha(b):return hashlib.sha256(b).digest()
def p32(*n):return struct.pack('>'+'I'*len(n),*n)
def main():
 existing=TEXT.read_bytes()
 if sha(existing).hex()!='99a68d678b9d6a0489e652310c0e6269ccf4ba45148de1ab9aa34e03a54ac715' or existing[:12]!=b'OBLOC001'+p32(17):raise ValueError('wrong text plan')
 manifest=(TD/'manifest.json').read_bytes()
 if sha(manifest).hex()!='035de511eae73fde3c913869f38203294ce2177db672fbb3eac59ae34e372722':raise ValueError('wrong shared manifest')
 rec=json.loads(manifest);sources=[]
 for path,platform in zip(ZIPS,('StandaloneLinux64','StandaloneWindows64')):
  with zipfile.ZipFile(path) as z:data=z.read('Megabonk_Data/StreamingAssets/aa/'+platform+'/'+NAME)
  header,raw=locale.envelope(data);env=UnityPy.load(data);groups={}
  for ptr in env.container.values():
   obj=ptr.deref();tree=obj.read_typetree();guid=tree.get('m_TableCollectionNameGuidString')
   if not guid:continue
   if guid in groups:raise ValueError('duplicate shared collection')
   blob=obj.get_raw_data();items={}
   for r in tree['m_Entries']:
    val=r['m_Key'].encode('utf8');idx=blob.find(struct.pack('<I',len(val))+val)
    if idx<0 or r['m_Id'] in items:raise ValueError('invalid shared entry')
    items[r['m_Id']]=(obj.byte_start+idx+4,val)
   groups[guid]=items
  sources.append((data,header,raw,groups))
 blank=zlib.decompress((TD/'shared.zlib').read_bytes());head=(TD/'shared.header').read_bytes()
 if len(blank)!=rec['raw_size'] or sha(blank).hex()!=rec['blank_sha256'] or sha(head).hex()!=rec['header_sha256']:raise ValueError('invalid shared template')
 work=bytearray(blank);rows=[]
 for x in rec['ranges']:
  dst,n=x['offset'],x['length'];offs=[];value=None
  for data,header,raw,groups in sources:
   off,v=groups[x['guid']][x['id']]
   if len(v)!=n or sha(v).hex()!=x['sha256'] or raw[off:off+n]!=v or value is not None and value!=v:raise ValueError('shared source differs')
   value=v;offs.append(off)
  if any(work[dst:dst+n]):raise ValueError('shared destination not blank')
  work[dst:dst+n]=value;rows.append((dst,n,*offs))
 if sha(work).hex()!=rec['raw_sha256'] or len(rows)!=1127:raise ValueError('shared reconstruction differs')
 packed=zlib.compress(blank,9);name=NAME.encode('ascii');out=bytearray(b'OBLOC001'+p32(18)+existing[12:]);out+=p32(len(name))+name+p32(len(blank),len(packed),len(rows))+sha(blank)+sha(work)+packed+p32(len(head))+head
 for data,header,raw,groups in sources:out+=p32(len(data),len(header),len(raw))+sha(data)+sha(raw)
 for row in rows:out+=p32(*row)
 OUT.parent.mkdir(parents=True,exist_ok=True)
 if OUT.exists():raise ValueError('output exists')
 OUT.write_bytes(out);print('PASS:',len(rows),'shared keys; combined plan sha256',sha(out).hex(),'bytes',len(out))
if __name__=='__main__':main()
