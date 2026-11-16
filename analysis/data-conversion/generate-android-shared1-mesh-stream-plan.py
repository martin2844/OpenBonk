#!/usr/bin/env python3
"""Derive metadata for shared1 meshes whose Steam vertices are streamed."""
import argparse,hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-mesh-flags-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');NAMES=('sharedassets1.assets','sharedassets1.assets.resS','level1','level1.resS');MANIFEST=B/'share-source-experiment/full-bc-template/mesh-manifest.json';OUT=B/'shared1-mesh-stream-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--base',type=Path,default=BASE);p.add_argument('--linux-zip',type=Path,default=ZIPS[0]);p.add_argument('--windows-zip',type=Path,default=ZIPS[1]);p.add_argument('--manifest',type=Path,default=MANIFEST);p.add_argument('--output',type=Path,default=OUT)
 target_group=p.add_mutually_exclusive_group();target_group.add_argument('--target-obb',type=Path);target_group.add_argument('--target-asset',type=Path);args=p.parse_args()
 if args.output.exists() or args.output.is_symlink():raise ValueError('output exists')
 base=args.base.read_bytes()
 if sha(base).hex()!='83e78c94e692808d7d39ae57d5abe0bdd8aaea7be6b64baad7a3c27263ca7624':raise ValueError('invalid base')
 if args.target_asset:
  target=args.target_asset.read_bytes()
  if sha(target).hex()!='b6b1d5fd2d261c1163608dfcdf354eca87afbec514840b963a9b5c50ce7fd33e':raise ValueError('reconstructed target asset differs')
 else:
  with zipfile.ZipFile(args.target_obb or TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in (args.linux_zip,args.windows_zip):
  with zipfile.ZipFile(path) as z:files.append([z.read('Megabonk_Data/'+n) for n in NAMES])
  sources.append({n:{o.path_id:o for o in UnityPy.load(files[-1][NAMES.index(n)]).objects} for n in ('sharedassets1.assets','level1')})
 recs=json.loads(args.manifest.read_text())['meshes'];work=bytearray(base);ranges=[];sizes=[];completed=0
 for rec in recs:
  dst=objects[rec['target_id']];raw=dst.get_raw_data();start=dst.byte_start
  if work[start:start+len(raw)]==raw:continue
  m=dst.read();v=bytes(m.m_VertexData.m_DataSize);idx=bytes(m.m_IndexBuffer);ip=raw.index(idx);vp=raw.index(v)
  if ip>=vp:continue
  prefix=raw[:ip];mid=raw[ip+len(idx):vp];tail=raw[vp+len(v):]
  if len(mid)<4 or len(tail)<16 or tail[-16:]!=bytes(16) or int.from_bytes(mid[-4:],'little')!=len(v) or work[start:start+len(prefix)]!=prefix:continue
  selected=[s[rec['source_file']][rec['source_id']] for s in sources];positions=[];valid=True
  for obj in selected:
   sm=obj.read();sr=obj.get_raw_data();ib=bytes(sm.m_IndexBuffer or []);si=sr.find(ib)
   if not sm.m_StreamData or not sm.m_StreamData.path or sm.m_VertexData.m_DataSize or si<0 or sm.m_Name!=m.m_Name:valid=False;break
   midStart=si+len(ib);tailStart=midStart+len(mid)
   if sr[midStart:midStart+len(mid)-4]!=mid[:-4] or sr[midStart+len(mid)-4:tailStart]!=bytes(4) or sr[tailStart:tailStart+len(tail)-16]!=tail[:-16]:valid=False;break
   positions.append((midStart,tailStart))
  if not valid:continue
  fid=NAMES.index(rec['source_file'])
  def commit(pos,value,offsets):
   if not any(value) or work[pos:pos+len(value)]==value:return
   if any(work[pos:pos+len(value)]):raise ValueError('metadata target not blank')
   refs=[]
   for f,obj,offset in zip(files,selected,offsets):
    src=obj.byte_start+offset
    if f[fid][src:src+len(value)]!=value:raise ValueError('stream metadata differs')
    refs.append((fid,src))
   work[pos:pos+len(value)]=value;ranges.append((pos,len(value),refs,sha(value)))
  commit(start+ip+len(idx),mid[:-4],[x[0] for x in positions]);where=start+vp-4
  if work[where:where+4] not in (bytes(4),struct.pack('<I',len(v))):raise ValueError('vertex size target differs')
  if work[where:where+4]==bytes(4):struct.pack_into('<I',work,where,len(v));sizes.append((where,len(v)))
  commit(start+vp+len(v),tail[:-16],[x[1] for x in positions])
  if work[start:start+len(raw)]!=raw:raise ValueError('mesh reconstruction differs')
  completed+=1
 ranges.sort(key=lambda r:r[0]);sizes.sort();out=bytearray(b'OBS1K001'+struct.pack('>II',len(base),len(ranges))+sha(base)+sha(work))
 for platform in files:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for dst,size,refs,h in ranges:out.extend(struct.pack('>IIIQIQ',dst,size,refs[0][0],refs[0][1],refs[1][0],refs[1][1])+h)
 out.extend(struct.pack('>I',len(sizes)))
 for off,size in sizes:out.extend(struct.pack('>II',off,size))
 args.output.parent.mkdir(parents=True,exist_ok=True)
 with args.output.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'partial',sha(work).hex(),'extra complete meshes',completed,'ranges',len(ranges),'sizes',len(sizes))
if __name__=='__main__':main()
