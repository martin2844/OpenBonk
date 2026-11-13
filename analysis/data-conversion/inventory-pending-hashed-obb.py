#!/usr/bin/env python3
"""Classify differing hashed entries in a private diagnostic OBB."""
import collections,hashlib,re,zipfile
from pathlib import Path
import UnityPy
B=Path(__file__).resolve().parents[2]/'build/android'
def main():
 counts=collections.Counter();sizes=collections.Counter(); rows=[]
 with zipfile.ZipFile(B/'localization-diagnostic-obb/linux-v2.obb') as a,zipfile.ZipFile(B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb') as b:
  for n in a.namelist():
   if not re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}',n):continue
   x,y=a.read(n),b.read(n)
   if hashlib.sha256(x).digest()==hashlib.sha256(y).digest():continue
   try:
    objects=UnityPy.load(y).objects;types=collections.Counter(o.type.name for o in objects);kind=','.join(sorted(types)) or 'empty'
   except Exception as e:kind='unreadable:'+type(e).__name__
   counts[kind]+=1;sizes[kind]+=len(y);rows.append((kind,n,len(x),len(y)))
 print('Differing hashed files',len(rows))
 for k,num in counts.most_common():print('TYPE',k,'files',num,'reference_bytes',sizes[k])
 for kind,n,x,y in rows:print('ENTRY',kind,n,'diagnostic_bytes',x,'reference_bytes',y)
if __name__=='__main__':main()
