#!/usr/bin/env python3
"""Create a private, isolated-package research APK; input must be the pinned test APK."""
import copy
import hashlib
from pathlib import Path
import struct
import sys
import zipfile
import zlib

EXPECTED = {'2df84b9404b8d52d1060fc79154e8e14b3c2024d546bbe08d43be03ea59d5ba9',
            'dd595d38f2761364907bd30b0152375009d0caacb244c484096367fc6631a5a1',
            'e15a0189737e9fae5b65bba6fd97bc8cadd3b2508765a4659b038edae52ecc1b',
            '579a8107d17196730ce680bf36c865e3831816843a498a573f00616c2bb6332c',
            'dfe191ff584f54fbd908bd24522ae6cabe3dded822db8245a746da60e3c46fbc',
            'f72385888c9fe6e1bca2748bb030a801c7f76a08e60bb4d8f3b2bfcffbc7799e',
            'f278ea963a20f0251a4bca10a76268a86c8677848c1afdc955af7eff1ca7aced',
            'f0c63df678ff1fba2527e249a8ed973efa6e0de5a998ae0658106a243944b88c',
            '43ab80fceac523e1438308840cd1d696844ca75b1a047ab769062cf8980155b1',
            '5543620d74804f198d8c864dc45962c83b97d82787f13ea21bd7b3f93b19c37f',
            '17edc2d3b34f9b79ed0066d8dccb03e5b4ff01e2170fde676f12070d2cc8c1dd',
            '9ff6d998de740399e08bc57e6aa0ddd8f651c1e6d4ef1fe6f19beb7f61e7149c',
            '79fbdb0ae35e7ef763a7989a7913f92f4c026162c8876091a08fddd6d928f448',
            '4272e881f8531a60d6f24b2df5b87535e82f1481b8341dede998fcc6e8ae35aa',
            '60fef00290eecd20917bd83818143d8cb251af54a5facaaa5785d1f0fa97d0fd',
            '05332e45345113872cdb7c7e22b0c18ad44f130ac7eec3651e2e2cc6ec7ecf6f',
            'c4455728a8d568796256904e0d31b6fb52922c77be8cc0ae041c6d0cb8534a34',
            '9363ce7dc5e8395225a1c12885aa3698393f4759af7689986ec14d59064d9097',
            '37ebf9599e8dd3a9d8abbe5265ecb04cc9e8c4dd352199229d60f980fde663e4',
            '485d4a199ea197d4e9509ac4ab54f0ea904d36c93d334b3dfeb4b7443887ede4',
            'b32a24f2bc0870528bb3e5daccd1862b91c9e785964b485b14caa2908f37b444',
            'a481637465200f7687a97d87981a978d1422ec0507d18e58981a9b249bfc8b7d',
            '55151408123a8387c267cb3dda7cdb9b7eced1f7a6276702b0d40d88ac187563',
            '8acb837a940c6e18d80a8cf71b731fe99415feebaebd523cee21d0c8169eb51d',
            'e8fb78bfe9fdc4f314f069c7446807c5d9733855d9bd02e50dafeb4804f905b7',
            '92903c916ce1284ee6d93c4cedbf58b6703ade5d1eee616afef867d09a28f671',
            '0d349761cd06a2c7ffc40000a30b6f0db69f738a3cd33225f0e28beea4f3ce99',
            '0ec7cc904c4a1d84afc8830c0ead7c6149f1ec62a5a699d5677d8fbc375d2ad3',
            '4051d031843ced116354a0e20ec3e10706e1e6980e21923a675f0cc1a73f4fbf',
            '8e7f14923ed07bb56aef76333b8c49e83e4a30f747522ff3d2cc9045472262c7',
            '056d4cb89ae4e672dc784f2bd447ea6212c8c549a5b0e8f90e0d707ed0099bf1',
            '5e00e36298b1c317dc6500f3bb6b37ea94197e48e9590aa4f23363201255f774',
            '3f24a3cacde4184d85beac0ac6c3a1b33d45c4694e570af3d1419183344065d7',
            'ce5ba72d8ea84eb823a5d0055ba1d6510c361186926f819260d1b891a939a3c3',
            'aa42e24a5c68a335f74e834c3e6521d1757ba80c91db1845d4f6646ee8e5e454',
            '98f99df0ae42af23f3f990667db9b4c52efb489ab76f6109b5cf7a672b7699dd',
            '6b5579e42e212565a3ca8b749fd5dcef9e034b8c8fc5f6f19c2836bd7bfceaaa',
            'f0cea029f3ade1ca67492079ab5f377c259974b9aaccc4ba79b6eb1383bfe3aa',
            'c92c5b92aa255d893b3ceef9a25d776728f7658faaf4b97fad8b4263b23cd036'}
OLD_OBB = b'66ae9c8f5cec738cca1b613f014121de9e93e4ea0c81c698c27ba953fe06aa42'
STORED_VIDEO_OBB = b'6731caf6235a74e594fc6916b20d0f6236a6fc4b7999be473ad415ac28f09785'
OLD = 'com.megabonk.nativeport.splittest'
NEW = 'com.megabonk.nativeport.splitdemo'

def main():
    options = sys.argv[3:]
    if len(sys.argv) < 3 or len(options) != len(set(options)) or any(x not in ('--stored-video', '--dev-package') for x in options):
        raise ValueError('usage: SCRIPT INPUT_APK NEW_UNSIGNED_APK [--stored-video] [--dev-package]')
    source, target = map(Path, sys.argv[1:3])
    package = 'com.megabonk.nativeport.splitdevx' if '--dev-package' in options else NEW
    if not source.is_file() or hashlib.file_digest(source.open('rb'), 'sha256').hexdigest() not in EXPECTED:
        raise ValueError('unexpected private input APK')
    if target.exists() or target.is_symlink() or not target.parent.is_dir() or len(OLD) != len(package):
        raise ValueError('invalid output or package length')
    selected = {
        'AndroidManifest.xml': ('utf-16-le', 3),
        'assets/bin/Data/Managed/Metadata/global-metadata.dat': ('utf-8', 1),
        'classes.dex': ('utf-8', 1),
    }
    try:
        with zipfile.ZipFile(source) as src, zipfile.ZipFile(target, 'x', allowZip64=True) as dst:
            if len(src.namelist()) != len(set(src.namelist())) or not set(selected) <= set(src.namelist()):
                raise ValueError('unexpected APK entries')
            for entry in src.infolist():
                if entry.filename.startswith('META-INF/'): continue
                data = src.read(entry)
                if entry.filename in selected:
                    encoding, count = selected[entry.filename]
                    before, after = OLD.encode(encoding), package.encode(encoding)
                    if data.count(before) != count or after in data:
                        raise ValueError('unexpected package references: ' + entry.filename)
                    data = data.replace(before, after)
                    if entry.filename == 'classes.dex':
                        if '--stored-video' in options:
                            if not (b'665e167cbcc2fd9c43c46e1d2c84f83c6ca9e0ad6c9756f439e10b90d5758972' < STORED_VIDEO_OBB < b'673f1003a80d65b951c96dcb09fa6401fe72d9c6c2f9cffb55658a6da34bd9d1'):
                                raise ValueError('OBB hash would change DEX string ordering')
                            if data.count(OLD_OBB) != 1 or STORED_VIDEO_OBB in data:
                                raise ValueError('unexpected OBB identity in DEX')
                            data = data.replace(OLD_OBB, STORED_VIDEO_OBB)
                        if data[:4] != b'dex\n' or len(data) < 112:
                            raise ValueError('unexpected DEX header')
                        data = bytearray(data)
                        data[12:32] = hashlib.sha1(data[32:]).digest()
                        struct.pack_into('<I', data, 8, zlib.adler32(data[12:]) & 0xffffffff)
                        data = bytes(data)
                dst.writestr(copy.copy(entry), data)
    except BaseException:
        target.unlink(missing_ok=True)
        raise
    print('Staged isolated private diagnostic APK; not distributable.')

if __name__ == '__main__': main()
