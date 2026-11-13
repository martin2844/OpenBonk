"""Compare all decodable Texture2D pixels and selected settings in two Android APKs."""
import collections
import hashlib
import re
import sys
import tempfile
import zipfile
from pathlib import Path
import UnityPy

def extract(apk):
    records = collections.defaultdict(list)
    errors = []
    with tempfile.TemporaryDirectory(prefix='openbonk-ui-textures-') as temp, zipfile.ZipFile(apk) as archive:
        names = archive.namelist()
        for group in range(5):
            stem = f'assets/bin/Data/sharedassets{group}.assets'
            parts = sorted((x for x in names if re.fullmatch(re.escape(stem) + r'\.split\d+', x)), key=lambda x: int(x.rsplit('split', 1)[1]))
            if not parts and stem in names:
                parts = [stem]
            if not parts:
                continue
            path = Path(temp) / f'sharedassets{group}.assets'
            with path.open('wb') as out:
                for part in parts:
                    out.write(archive.read(part))
            resource = f'assets/bin/Data/sharedassets{group}.resource'
            if resource in names:
                (Path(temp) / Path(resource).name).write_bytes(archive.read(resource))
            for obj in UnityPy.load(str(path)).objects:
                if obj.type.name != 'Texture2D':
                    continue
                tex = obj.read()
                try:
                    image = tex.image.convert('RGBA')
                    settings = tex.m_TextureSettings
                    entry = (f'sharedassets{group}', tex.m_Width, tex.m_Height, tex.m_TextureFormat,
                             settings.m_FilterMode, settings.m_Aniso, settings.m_WrapU,
                             settings.m_WrapV, hashlib.sha256(image.tobytes()).hexdigest())
                    records[tex.m_Name].append(entry)
                except Exception as exc:
                    stream = getattr(tex, 'm_StreamData', None)
                    errors.append((tex.m_Name, group, obj.path_id, tex.m_Width, tex.m_Height,
                                   tex.m_TextureFormat, len(tex.image_data or b''),
                                   None if stream is None else (stream.path, stream.offset, stream.size),
                                   type(exc).__name__))
        # Unity also stores individual serialized assets under 32-character names.
        for item in names:
            if not re.fullmatch(r'assets/bin/Data/[a-f0-9]{32}', item):
                continue
            path = Path(temp) / Path(item).name
            path.write_bytes(archive.read(item))
            resource = item + '.resource'
            if resource in names:
                (Path(temp) / Path(resource).name).write_bytes(archive.read(resource))
            try:
                objects = UnityPy.load(str(path)).objects
                for obj in objects:
                    if obj.type.name != 'Texture2D':
                        continue
                    tex = obj.read()
                    image = tex.image.convert('RGBA')
                    settings = tex.m_TextureSettings
                    records[tex.m_Name].append((item, tex.m_Width, tex.m_Height, tex.m_TextureFormat,
                        settings.m_FilterMode, settings.m_Aniso, settings.m_WrapU, settings.m_WrapV,
                        hashlib.sha256(image.tobytes()).hexdigest()))
            except Exception as exc:
                errors.append(f'{item}: {type(exc).__name__}: {exc}')
    return {k: sorted(v) for k, v in records.items()}, errors

if len(sys.argv) != 3:
    raise SystemExit('Supply old and new APKs')
old, old_errors = extract(sys.argv[1])
new, new_errors = extract(sys.argv[2])
names = sorted(set(old) | set(new))
counts = collections.Counter()
for name in names:
    if name not in old: status = "ONLY_NEW"
    elif name not in new: status = "ONLY_OLD"
    elif old[name] == new[name]: status = "MATCH"
    else: status = "DIFF"
    counts[status] += 1
    if status != "MATCH": print(name, status, "old=", old.get(name), "new=", new.get(name))
print("counts:", dict(counts), "old_instances:", sum(map(len, old.values())), "new_instances:", sum(map(len, new.values())))
print("old_errors:", old_errors)
print("new_errors:", new_errors)
print("scope: Texture2D in sharedassets0-4 and individual 32-character serialized assets; excludes sprites, other containers, bindings and runtime GPU rendering")
