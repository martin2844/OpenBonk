#!/usr/bin/env python3
"""Generate a private Android localization-text reconstruction plan from pinned inputs."""
import hashlib, importlib.util, json, struct, zipfile, zlib
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2]
B=ROOT/'build/android'; TEMPLATE=B/'localization-text-template'; OUT=B/'localization-text-android-plan/plan.bin'
ZIPS=[B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip']
S=importlib.util.spec_from_file_location('localetemplate',ROOT/'analysis/data-conversion/localization-text-template.py'); m=importlib.util.module_from_spec(S);S.loader.exec_module(m)
def sha(b):return hashlib.sha256(b).digest()
def p32(*n):return struct.pack('>'+'I'*len(n),*n)
def main():
 manifest=(TEMPLATE/'manifest.json').read_bytes()
 if sha(manifest).hex()!=m.MANIFEST_SHA:raise ValueError('unexpected template manifest')
 records=json.loads(manifest)['locales']; out=bytearray(b'OBLOC001'+p32(len(records))); stats=0
 with zipfile.ZipFile(ZIPS[0]) as linux,zipfile.ZipFile(ZIPS[1]) as windows:
  for rec in records:
   name=rec['name'];sources=[]
   for z,platform in ((linux,'StandaloneLinux64'),(windows,'StandaloneWindows64')):
    data=z.read('Megabonk_Data/StreamingAssets/aa/'+platform+'/'+name);header,raw=m.envelope(data);env=UnityPy.load(data);tables={k.lower():v for k,v in env.container.items()};cache={}
    for path,ptr in tables.items():
     obj=ptr.deref();tree=obj.read_typetree()
     if 'm_TableData' not in tree:continue
     blob=obj.get_raw_data();lookup={}
     for row in tree['m_TableData']:
      val=row['m_Localized'].encode('utf8'); pattern=struct.pack('<I',len(val))+val; idx=blob.find(pattern)
      if idx<0:raise ValueError('source string absent')
      lookup[row['m_Id']]=(obj.byte_start+idx+4,val)
     cache[path]=lookup
    sources.append((data,header,raw,cache))
   blank=zlib.decompress((TEMPLATE/(name+'.zlib')).read_bytes())
   if len(blank)!=rec['raw_size'] or sha(blank).hex()!=rec['blank_sha256']:raise ValueError('blank mismatch')
   work=bytearray(blank); rows=[]
   for item in rec['ranges']:
    dst,n=item['offset'],item['length'];offs=[];value=None
    for data,header,raw,cache in sources:
     off,v=cache[item['table'].lower()][item['id']]
     if len(v)!=n or sha(v).hex()!=item['sha256'] or raw[off:off+n]!=v:raise ValueError('source value mismatch')
     if value is not None and value!=v:raise ValueError('platform text mismatch')
     value=v;offs.append(off)
    if any(work[dst:dst+n]):raise ValueError('destination not blank')
    work[dst:dst+n]=value;rows.append((dst,n,*offs))
   if sha(work).hex()!=rec['raw_sha256']:raise ValueError('reconstructed text mismatch')
   nameb=name.encode('ascii');packed=zlib.compress(blank,9)
   target_header=(TEMPLATE/(name+'.header')).read_bytes()
   if sha(target_header).hex()!=rec['header_sha256']:raise ValueError('target header mismatch')
   out+=p32(len(nameb))+nameb+p32(len(blank),len(packed),len(rows))+sha(blank)+sha(work)+packed+p32(len(target_header))+target_header
   for data,header,raw,cache in sources:out+=p32(len(data),len(header),len(raw))+sha(data)+sha(raw)
   for row in rows:out+=p32(*row)
   stats+=len(rows);print(name,len(rows),flush=True)
 OUT.parent.mkdir(parents=True,exist_ok=True)
 if OUT.exists():raise ValueError('output exists')
 OUT.write_bytes(out);print('total',len(records),'tables',stats,'ranges','bytes',len(out),'sha256',sha(out).hex())
if __name__=='__main__':main()
