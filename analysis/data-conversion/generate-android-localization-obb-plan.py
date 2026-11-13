#!/usr/bin/env python3
"""Generate private checksums for the 18 reconstructed diagnostic locale bundles."""
import hashlib,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android/localization-complete-android-plan';OUT=B/'obb-plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 files=sorted((B/'linux').glob('*.bundle'));other=B/'windows'
 if len(files)!=18 or not (B/'linux/localization-assets-shared_assets_all.bundle').exists():raise ValueError('missing locales')
 out=bytearray(b'OBLCO001'+struct.pack('>I',18))
 for f in files:
  value=f.read_bytes()
  if value!=(other/f.name).read_bytes():raise ValueError('platform difference')
  name=f.name.encode('ascii');out+=struct.pack('>I',len(name))+name+struct.pack('>I',len(value))+sha(value)
 if OUT.exists():raise ValueError('output exists')
 OUT.write_bytes(out);print('PASS 18 entries; plan sha256',sha(out).hex())
if __name__=='__main__':main()
