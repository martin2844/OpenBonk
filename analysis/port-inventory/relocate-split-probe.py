#!/usr/bin/env python3
"""Create an unsigned, experimental APK/OBB with sharedassets0 in the OBB.

For isolated testing only. This changes a DEX constant and is not a release build.
"""
import hashlib
import copy
import re
from pathlib import Path
import struct
import sys
import zipfile
import zlib

if len(sys.argv) not in (5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20):
    raise SystemExit('usage: relocate-split-probe.py INPUT.apk INPUT.obb OUTPUT-UNSIGNED.apk OUTPUT.obb [AUDIO_DIR SHARED0 SHARED2 SHARED2_RESOURCE MESH_DIR ANIMATION_DIR TEXTURE_DIR MATCHED_DIR RGBA_DIR EXTRA_DIR FINAL_DIR HASHED_AUDIO_DIR SCENE_DIR SHARED4_DIR PRELOAD_PARTICLE_DIR]')
apk, obb, out_apk, out_obb = map(Path, sys.argv[1:5])
audio = Path(sys.argv[5]) if len(sys.argv) >= 6 else None
shared0_path = Path(sys.argv[6]) if len(sys.argv) >= 7 else None
shared2_path = Path(sys.argv[7]) if len(sys.argv) >= 8 else None
shared2_resource_path = Path(sys.argv[8]) if len(sys.argv) >= 9 else None
hashed_mesh_dir = Path(sys.argv[9]) if len(sys.argv) >= 10 else None
hashed_animation_dir = Path(sys.argv[10]) if len(sys.argv) >= 11 else None
hashed_texture_dir = Path(sys.argv[11]) if len(sys.argv) >= 12 else None
matched_texture_dir = Path(sys.argv[12]) if len(sys.argv) >= 13 else None
rgba_texture_dir = Path(sys.argv[13]) if len(sys.argv) >= 14 else None
extra_texture_dir = Path(sys.argv[14]) if len(sys.argv) >= 15 else None
final_texture_dir = Path(sys.argv[15]) if len(sys.argv) >= 16 else None
hashed_audio_dir = Path(sys.argv[16]) if len(sys.argv) >= 17 else None
scene_particle_dir = Path(sys.argv[17]) if len(sys.argv) >= 18 else None
shared4_dir = Path(sys.argv[18]) if len(sys.argv) >= 19 else None
preload_particle_dir = Path(sys.argv[19]) if len(sys.argv) == 20 else None
if out_apk.exists() or out_obb.exists():
    raise SystemExit('refusing to overwrite existing files')

def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda: source.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest().encode('ascii')

def jar_signature(name):
    upper = name.upper()
    if not upper.startswith('META-INF/'):
        return False
    leaf = upper[len('META-INF/'):]
    return '/' not in leaf and (leaf == 'MANIFEST.MF' or leaf.endswith(('.SF', '.RSA', '.DSA', '.EC')) or leaf.startswith('SIG-'))

old_hash = digest(obb)
with zipfile.ZipFile(apk) as src_apk, zipfile.ZipFile(obb) as src_obb:
    notice = 'assets/ThirdPartyNotices/Lua.txt'
    if notice not in src_obb.namelist() or notice in src_apk.namelist():
        raise SystemExit('expected Lua notice in OBB only')
    dex = src_apk.read('classes.dex')
    strings_count, strings_offset = struct.unpack_from('<II', dex, 56)
    offsets = struct.unpack_from(f'<{strings_count}I', dex, strings_offset)
    def dex_string(offset):
        while dex[offset] & 0x80:
            offset += 1
        offset += 1
        return dex[offset:dex.index(b'\0', offset)]
    indexes = [i for i, offset in enumerate(offsets) if dex_string(offset) == old_hash]
    if len(indexes) != 1 or indexes[0] == 0 or indexes[0] + 1 == len(offsets):
        raise SystemExit('could not identify unique bounded DEX hash string')
    index = indexes[0]
    previous, following = dex_string(offsets[index - 1]), dex_string(offsets[index + 1])
    moving = [n for n in src_apk.namelist() if n.startswith('assets/bin/Data/sharedassets0.assets')]
    if not moving or set(moving) & set(src_obb.namelist()):
        raise SystemExit('missing assets or duplicate destination entries')
    shared0 = None
    if shared0_path is not None:
        shared0 = shared0_path.read_bytes()
        try:
            numbers = sorted(int(n.rsplit('split', 1)[1]) for n in moving)
        except ValueError:
            raise SystemExit('unexpected sharedassets0 fragment names')
        if numbers != list(range(len(moving))) or len(shared0) != sum(src_apk.getinfo(n).file_size for n in moving):
            raise SystemExit('sharedassets0 fragments or replacement size mismatch')
        for name in moving:
            index = int(name.rsplit('split', 1)[1]); start = index * 1048576
            if len(shared0[start:start+1048576]) != src_apk.getinfo(name).file_size:
                raise SystemExit('sharedassets0 fragment size mismatch')
    replacement = None
    if audio is not None:
        replacement = (audio / 'sharedassets1.assets').read_bytes()
        audio_bytes = (audio / 'sharedassets1.resource').read_bytes()
        part_names = [n for n in src_obb.namelist() if n.startswith('assets/bin/Data/sharedassets1.assets.split')]
        indexes = sorted(int(n.rsplit('split', 1)[1]) for n in part_names)
        if not part_names or indexes != list(range(len(part_names))):
            raise SystemExit('original asset fragments are missing or duplicated')
    shared2_parts = []
    shared2_resource = None
    if shared2_resource_path is not None:
        if src_obb.namelist().count('assets/bin/Data/sharedassets2.resource') != 1:
            raise SystemExit('sharedassets2 resource missing or duplicated')
        shared2_resource = shared2_resource_path.read_bytes()
        if not shared2_resource:
            raise SystemExit('sharedassets2 resource empty')
    if shared2_path is not None:
        shared2 = shared2_path.read_bytes()
        prefix = 'assets/bin/Data/sharedassets2.assets.split'
        shared2_parts = [n for n in src_obb.namelist() if n.startswith(prefix)]
        try:
            indexes = sorted(int(n[len(prefix):]) for n in shared2_parts)
        except ValueError:
            raise SystemExit('invalid sharedassets2 fragment names')
        if not shared2_parts or indexes != list(range(len(shared2_parts))) or not (len(shared2_parts)-1)*1048576 < len(shared2) <= len(shared2_parts)*1048576:
            raise SystemExit('sharedassets2 fragments or replacement size mismatch')
        for name in shared2_parts:
            index = int(name[len(prefix):])
            if index < len(shared2_parts)-1 and src_obb.getinfo(name).file_size != 1048576:
                raise SystemExit('original sharedassets2 fragment size mismatch')
    shared4_parts = []
    shared4_resource = None
    if shared4_dir is not None:
        shared4 = (shared4_dir / 'sharedassets4.assets').read_bytes()
        shared4_resource = (shared4_dir / 'sharedassets4.resource').read_bytes()
        prefix = 'assets/bin/Data/sharedassets4.assets.split'
        shared4_parts = sorted((n for n in src_obb.namelist() if n.startswith(prefix)), key=lambda n:int(n[len(prefix):]))
        if not shared4_parts or [int(n[len(prefix):]) for n in shared4_parts]!=list(range(len(shared4_parts))) or not (len(shared4_parts)-1)*1048576 < len(shared4) <= len(shared4_parts)*1048576 or not shared4_resource or src_obb.namelist().count('assets/bin/Data/sharedassets4.resource')!=1:
            raise SystemExit('invalid sharedassets4 data or fragments')
        if any(src_obb.getinfo(n).file_size != 1048576 for n in shared4_parts[:-1]):raise SystemExit('invalid sharedassets4 fragment size')
    hashed_meshes = {}
    if hashed_mesh_dir is not None:
        if not hashed_mesh_dir.is_dir():
            raise SystemExit('hashed mesh directory missing')
        entries = list(hashed_mesh_dir.iterdir())
        if len(entries) != 50 or any(not p.is_file() or not re.fullmatch(r'[0-9a-f]{32}',p.name) for p in entries):
            raise SystemExit('invalid hashed mesh files')
        for path in entries:
            name = 'assets/bin/Data/' + path.name
            if src_obb.namelist().count(name) != 1:
                raise SystemExit('hashed mesh entry missing or duplicated')
            data = path.read_bytes()
            if len(data) != src_obb.getinfo(name).file_size:
                raise SystemExit('hashed mesh file size mismatch')
            hashed_meshes[name] = data
    hashed_animations = {}
    if hashed_animation_dir is not None:
        if not hashed_animation_dir.is_dir():
            raise SystemExit('hashed animation directory missing')
        entries = list(hashed_animation_dir.iterdir())
        if len(entries) != 108 or any(not p.is_file() or not re.fullmatch(r'[0-9a-f]{32}',p.name) for p in entries):
            raise SystemExit('invalid hashed animation files')
        for path in entries:
            name = 'assets/bin/Data/' + path.name
            if name in hashed_meshes or src_obb.namelist().count(name) != 1:
                raise SystemExit('hashed animation entry missing or duplicated')
            data = path.read_bytes()
            if len(data) != src_obb.getinfo(name).file_size:
                raise SystemExit('hashed animation file size mismatch')
            hashed_animations[name] = data
    hashed_textures = {}
    if hashed_texture_dir is not None:
        if not hashed_texture_dir.is_dir():
            raise SystemExit('hashed texture directory missing')
        entries = list(hashed_texture_dir.iterdir())
        if len(entries) != 56 or any(not p.is_file() or not re.fullmatch(r'[0-9a-f]{32}',p.name) for p in entries):
            raise SystemExit('invalid hashed texture files')
        for path in entries:
            name = 'assets/bin/Data/' + path.name
            if name in hashed_meshes or name in hashed_animations or src_obb.namelist().count(name) != 1:
                raise SystemExit('hashed texture entry missing or duplicated')
            data = path.read_bytes()
            if len(data) != src_obb.getinfo(name).file_size:
                raise SystemExit('hashed texture file size mismatch')
            hashed_textures[name] = data
    matched_textures = {}
    if matched_texture_dir is not None:
        if not matched_texture_dir.is_dir():
            raise SystemExit('matched texture directory missing')
        entries = list(matched_texture_dir.iterdir())
        if len(entries) != 53 or any(not p.is_file() or not re.fullmatch(r'[0-9a-f]{32}',p.name) for p in entries):
            raise SystemExit('invalid matched texture files')
        for path in entries:
            name = 'assets/bin/Data/' + path.name
            if name in hashed_meshes or name in hashed_animations or name in hashed_textures or src_obb.namelist().count(name) != 1:
                raise SystemExit('matched texture entry missing or duplicated')
            data = path.read_bytes()
            if len(data) != src_obb.getinfo(name).file_size:
                raise SystemExit('matched texture file size mismatch')
            matched_textures[name] = data
    rgba_textures = {}
    if rgba_texture_dir is not None:
        entries = [p for p in rgba_texture_dir.iterdir() if p.name != 'report.json']
        if len(entries) != 10 or any(not p.is_file() or not re.fullmatch(r'[0-9a-f]{32}', p.name) for p in entries):
            raise SystemExit('invalid experimental RGBA textures')
        for path in entries:
            name = 'assets/bin/Data/' + path.name
            if name in hashed_meshes or name in hashed_animations or name in hashed_textures or name in matched_textures or src_obb.namelist().count(name) != 1:
                raise SystemExit('experimental RGBA entry missing or duplicated')
            rgba_textures[name] = path.read_bytes()
            if not rgba_textures[name]:
                raise SystemExit('empty experimental RGBA texture')
    extra_textures = {}
    if extra_texture_dir is not None:
        entries = [p for p in extra_texture_dir.iterdir() if p.name != 'report.json']
        if len(entries) != 13 or any(not p.is_file() or not re.fullmatch(r'[0-9a-f]{32}', p.name) for p in entries):
            raise SystemExit('invalid experimental extra textures')
        for path in entries:
            name = 'assets/bin/Data/' + path.name
            if name in hashed_meshes or name in hashed_animations or name in hashed_textures or name in matched_textures or name in rgba_textures or src_obb.namelist().count(name) != 1:
                raise SystemExit('experimental extra texture entry missing or duplicated')
            extra_textures[name] = path.read_bytes()
            if not extra_textures[name]:
                raise SystemExit('empty experimental extra texture')
    final_textures = {}
    if final_texture_dir is not None:
        entries = list(final_texture_dir.iterdir())
        if len(entries) != 2 or any(not p.is_file() or not re.fullmatch(r'[0-9a-f]{32}', p.name) for p in entries):
            raise SystemExit('invalid final candidate textures')
        for path in entries:
            name = 'assets/bin/Data/' + path.name
            if name in hashed_meshes or name in hashed_animations or name in hashed_textures or name in matched_textures or name in rgba_textures or name in extra_textures or src_obb.namelist().count(name) != 1:
                raise SystemExit('final candidate texture entry missing or duplicated')
            final_textures[name] = path.read_bytes()
            if not final_textures[name]:
                raise SystemExit('empty final candidate texture')
    hashed_audio = {}
    if hashed_audio_dir is not None:
        entries = [p for p in hashed_audio_dir.iterdir() if p.name != 'report.json']
        files = {p.name for p in entries}
        if len(entries) != 126 or any(not p.is_file() or not re.fullmatch(r'[0-9a-f]{32}(?:\.resource)?', p.name) for p in entries):
            raise SystemExit('invalid experimental hashed audio files')
        asset_names = {n for n in files if not n.endswith('.resource')}
        if len(asset_names) != 63 or files != asset_names | {n+'.resource' for n in asset_names}:
            raise SystemExit('hashed audio assets and resources must be paired')
        for path in entries:
            name = 'assets/bin/Data/' + path.name
            if name in hashed_meshes or name in hashed_animations or name in hashed_textures or name in matched_textures or name in rgba_textures or name in extra_textures or name in final_textures or src_obb.namelist().count(name) != 1:
                raise SystemExit('hashed audio entry missing or duplicated')
            hashed_audio[name] = path.read_bytes()
            if not hashed_audio[name]:
                raise SystemExit('empty hashed audio entry')
    scene_particles = {}
    if scene_particle_dir is not None:
        if {p.name for p in scene_particle_dir.iterdir()} != {'level1', 'level2', 'level4'}:
            raise SystemExit('invalid scene particle files')
        for scene in ('level1', 'level2', 'level4'):
            prefix = 'assets/bin/Data/' + scene + '.split'
            parts = sorted((n for n in src_obb.namelist() if n.startswith(prefix)), key=lambda n: int(n[len(prefix):]))
            if not parts or [int(n[len(prefix):]) for n in parts] != list(range(len(parts))):
                raise SystemExit('scene fragments missing')
            data = (scene_particle_dir / scene).read_bytes()
            if len(data) != sum(src_obb.getinfo(n).file_size for n in parts):
                raise SystemExit('scene particle file size mismatch')
            offset = 0
            for name in parts:
                size = src_obb.getinfo(name).file_size
                scene_particles[name] = data[offset:offset+size]; offset += size
    preload_particles = {}
    if preload_particle_dir is not None:
        entries = [p for p in preload_particle_dir.iterdir() if p.name != 'manifest.json']
        if len(entries) not in (31, 47, 68) or any(not p.is_file() or not re.fullmatch(r'[0-9a-f]{32}', p.name) for p in entries):
            raise SystemExit('invalid preload particle files')
        occupied = set().union(hashed_meshes, hashed_animations, hashed_textures, matched_textures,
                               rgba_textures, extra_textures, final_textures, hashed_audio, scene_particles)
        for path in entries:
            name = 'assets/bin/Data/' + path.name
            if name in occupied or src_obb.namelist().count(name) != 1:
                raise SystemExit('preload particle file missing or overlapping')
            data = path.read_bytes()
            if len(data) != src_obb.getinfo(name).file_size:
                raise SystemExit('preload particle file size mismatch')
            preload_particles[name] = data
    with zipfile.ZipFile(out_obb, 'x', allowZip64=True) as dest:
        for info in src_obb.infolist():
            name = info.filename
            if replacement is not None and name in part_names:
                continue
            if name in preload_particles:
                contents = preload_particles[name]
            elif name in scene_particles:
                contents = scene_particles[name]
            elif name in hashed_audio:
                contents = hashed_audio[name]
            elif name in final_textures:
                contents = final_textures[name]
            elif name in extra_textures:
                contents = extra_textures[name]
            elif name in rgba_textures:
                contents = rgba_textures[name]
            elif name in matched_textures:
                contents = matched_textures[name]
            elif name in hashed_textures:
                contents = hashed_textures[name]
            elif name in hashed_animations:
                contents = hashed_animations[name]
            elif name in hashed_meshes:
                contents = hashed_meshes[name]
            elif shared2_resource is not None and name == 'assets/bin/Data/sharedassets2.resource':
                contents = shared2_resource
            elif shared4_resource is not None and name == 'assets/bin/Data/sharedassets4.resource':
                contents = shared4_resource
            elif name in shared4_parts:
                start = int(name.rsplit('split',1)[1]) * 1048576
                contents = shared4[start:start+1048576]
            elif name in shared2_parts:
                start = int(name.rsplit('split',1)[1]) * 1048576
                contents = shared2[start:start+1048576]
            elif replacement is not None and name == 'assets/bin/Data/sharedassets1.resource':
                contents = audio_bytes
            else:
                contents = src_obb.read(info)
            if (name in hashed_audio and name.endswith('.resource')) or (replacement is not None and name == 'assets/bin/Data/sharedassets1.resource') or (shared2_resource is not None and name == 'assets/bin/Data/sharedassets2.resource') or (shared4_resource is not None and name == 'assets/bin/Data/sharedassets4.resource'):
                info = copy.copy(info)
                info.compress_type = zipfile.ZIP_STORED
            dest.writestr(info, contents)
        if replacement is not None:
            prototype = src_obb.getinfo(part_names[0])
            for index, start in enumerate(range(0, len(replacement), 1048576)):
                info = copy.copy(prototype)
                info.filename = f'assets/bin/Data/sharedassets1.assets.split{index}'
                dest.writestr(info, replacement[start:start+1048576])
        for name in moving:
            if shared0 is None:
                contents = src_apk.read(name)
            else:
                start = int(name.rsplit('split', 1)[1]) * 1048576
                contents = shared0[start:start+1048576]
            dest.writestr(src_apk.getinfo(name), contents)
        # A fixed-length ZIP comment lets us vary the hash without rebuilding
        # or rehashing the entire OBB. DEX requires string IDs to stay sorted.
        dest.comment = b'00000000'
    prefix_hash = hashlib.sha256()
    with out_obb.open('rb') as source:
        remaining = out_obb.stat().st_size - 8
        while remaining:
            block = source.read(min(1024 * 1024, remaining))
            if not block:
                raise SystemExit('short OBB read')
            prefix_hash.update(block); remaining -= len(block)
    for nonce in range(1_000_000):
        comment = f'{nonce:08x}'.encode('ascii')
        h = prefix_hash.copy(); h.update(comment); new_hash = h.hexdigest().encode('ascii')
        if previous < new_hash < following:
            break
    else:
        out_obb.unlink(missing_ok=True)
        raise SystemExit('could not retain sorted DEX strings')
    with out_obb.open('r+b') as dest:
        dest.seek(-8, 2); dest.write(comment)
    if digest(out_obb) != new_hash:
        raise SystemExit('OBB digest validation failed')
    replacements = 0
    with zipfile.ZipFile(out_apk, 'x', allowZip64=True) as dest:
        for info in src_apk.infolist():
            name = info.filename
            if name in moving or jar_signature(name):
                continue
            contents = src_apk.read(info)
            if name.endswith('.dex') and old_hash in contents:
                replacements += contents.count(old_hash)
                contents = bytearray(contents.replace(old_hash, new_hash))
                contents[12:32] = hashlib.sha1(contents[32:]).digest()
                contents[8:12] = (zlib.adler32(contents[12:]) & 0xffffffff).to_bytes(4, 'little')
                contents = bytes(contents)
            dest.writestr(info, contents)
        # The Lua native library remains in the APK, so the corresponding
        # notice must remain with it even before the user supplies an OBB.
        dest.writestr(src_obb.getinfo(notice), src_obb.read(notice))
    if replacements != 1:
        out_apk.unlink(missing_ok=True)
        out_obb.unlink(missing_ok=True)
        raise SystemExit(f'expected one DEX digest, found {replacements}')
print('relocated', len(moving), 'entries; old OBB', old_hash.decode(), 'new OBB', new_hash.decode())
print('Unsigned diagnostic only. It requires separate alignment/signing and does not establish distribution rights.')
