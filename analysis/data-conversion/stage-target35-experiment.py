#!/usr/bin/env python3
"""Stage an unsigned target-35 diagnostic from one exact private APK.

The only payload change is the targetSdkVersion integer in the binary Android
manifest. This experiment must be tested for Android behavior and is not a
release build or a claim of distribution rights.
"""
import copy,hashlib,struct,sys,zipfile
from pathlib import Path

PIN='a2f999c6a0a2ef0eb0e1fb3c3c59013bc970fd1a71f5dcb83bc0b0130fda52ff'

def patch(manifest):
    b=bytearray(manifest);offset=8;resources=[];hits=[]
    if len(b)<8 or struct.unpack_from('<H',b)[0]!=3 or struct.unpack_from('<I',b,4)[0]!=len(b):raise ValueError('invalid manifest')
    while offset<len(b):
        kind,header,size=struct.unpack_from('<HHI',b,offset)
        if header<8 or size<header or offset+size>len(b):raise ValueError('invalid XML chunk')
        if kind==0x180:
            if (size-header)%4:raise ValueError('invalid resource map')
            resources=list(struct.unpack_from('<'+'I'*((size-header)//4),b,offset+header))
        if kind==0x102:
            attr_start,attr_size,count=struct.unpack_from('<HHH',b,offset+24)
            if attr_size!=20 or offset+16+attr_start+attr_size*count>offset+size:raise ValueError('invalid attributes')
            for i in range(count):
                pos=offset+16+attr_start+i*attr_size
                name=struct.unpack_from('<I',b,pos+4)[0]
                if name<len(resources) and resources[name]==0x01010270:
                    value_size,reserved,typ,value=struct.unpack_from('<HBBI',b,pos+12)
                    if (value_size,reserved,typ,value)!=(8,0,0x10,33):raise ValueError('unexpected target SDK')
                    hits.append(pos+16)
        offset+=size
    if len(hits)!=1:raise ValueError('unexpected target SDK count')
    struct.pack_into('<I',b,hits[0],35)
    return bytes(b)

def main():
    if len(sys.argv)!=3:raise ValueError('usage: SCRIPT PINNED_APK NEW_UNSIGNED_APK')
    source,output=map(Path,sys.argv[1:])
    if not source.is_file() or output.exists() or output.is_symlink() or not output.parent.is_dir():raise ValueError('invalid input or output')
    if hashlib.file_digest(source.open('rb'),'sha256').hexdigest()!=PIN:raise ValueError('unexpected APK')
    try:
        with zipfile.ZipFile(source) as src, zipfile.ZipFile(output,'x',allowZip64=True) as dst:
            names=src.namelist()
            if len(names)!=len(set(names)) or names.count('AndroidManifest.xml')!=1:raise ValueError('invalid APK inventory')
            for info in src.infolist():
                if info.filename.startswith('META-INF/'):continue
                value=src.read(info)
                if info.filename=='AndroidManifest.xml':value=patch(value)
                dst.writestr(copy.copy(info),value)
    except BaseException:
        output.unlink(missing_ok=True);raise
    print('Staged private target-35 diagnostic; all other payload entries retained.')

if __name__=='__main__':main()
