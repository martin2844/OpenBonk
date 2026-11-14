#!/usr/bin/env python3
"""Remove three unused diagnostic shaders from an isolated no-probes workspace.

The checked-in sources remain intact. Files and metadata move to a new local
backup; unexpected content or serialized GUID references abort preparation.
"""
import argparse
import json
import os
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parent.parent
MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
NAMES = ('OpenBonkWaterBrdf1Probe.shader', 'OpenBonkWaterClampedProbe.shader', 'OpenBonkWaterRawProbe.shader')
TEXT_EXTENSIONS = {'.unity', '.prefab', '.asset', '.mat', '.controller', '.overrideController', '.playable', '.vfx', '.shader', '.compute', '.meta', '.cs'}


def inspect(project):
    assets = project / 'Assets'
    resource = assets / 'Resources'
    source = ROOT / 'unity/OpenBonk/Assets/Resources'
    files = [resource / (name + suffix) for name in NAMES for suffix in ('', '.meta')]
    existing = [p for p in files if p.exists() or p.is_symlink()]
    if not existing:
        return []
    if len(existing) != len(files): raise ValueError('incomplete diagnostic shader set')
    guids = []
    for name in NAMES:
        path = resource / name
        for suffix in ('', '.meta'):
            target = Path(str(path) + suffix)
            expected = source / (name + suffix)
            if target.is_symlink() or not target.is_file() or target.read_bytes() != expected.read_bytes():
                raise ValueError('diagnostic shader differs from checked-in source: ' + target.name)
        match = re.search(rb'^guid: ([a-f0-9]{32})$', Path(str(path) + '.meta').read_bytes(), re.MULTILINE)
        if not match: raise ValueError('diagnostic shader metadata lacks GUID')
        guids.append(match.group(1))
    ignored = set(files)
    for directory, names, filenames in os.walk(assets, followlinks=False):
        if any((Path(directory) / name).is_symlink() for name in names): raise ValueError('symbolic link in Assets')
        for name in filenames:
            path = Path(directory) / name
            if path in ignored or path.suffix not in TEXT_EXTENSIONS: continue
            if path.is_symlink(): raise ValueError('symbolic link in Assets')
            data = path.read_bytes()
            if any(guid in data for guid in guids): raise ValueError('diagnostic shader has a serialized reference: ' + str(path.relative_to(assets)))
            if path.suffix == '.cs' and any(name.removesuffix('.shader').encode() in data for name in NAMES):
                if (path.relative_to(assets).as_posix() != 'Scripts/Assembly-CSharp/AndroidWaterNormalGpuProbe.cs' or
                        not data.startswith(b'#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES\n')):
                    raise ValueError('diagnostic shader has an unexpected code reference: ' + str(path.relative_to(assets)))
    return files


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', type=Path, required=True)
    p.add_argument('--backup', type=Path)
    p.add_argument('--check', action='store_true')
    a = p.parse_args(); project = a.project.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if marker.is_symlink() or not marker.is_file() or json.loads(marker.read_text()) != MARKER: p.error('project must be an isolated workspace')
    if (project / 'Temp/UnityLockfile').exists(): p.error('Unity workspace is locked')
    files = inspect(project)
    if a.check:
        if files: p.error('diagnostic water shaders remain in workspace')
        print('Diagnostic water shaders are absent.'); return
    if not files:
        print('Diagnostic water shaders are already absent.'); return
    if not a.backup: p.error('new backup directory required')
    backup = a.backup.expanduser().absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir(): p.error('backup must be new')
    backup = backup.parent.resolve() / backup.name
    if backup.is_relative_to(project) or os.stat(backup.parent).st_dev != os.stat(files[0]).st_dev:
        p.error('backup must be outside workspace on the same filesystem')
    backup.mkdir(mode=0o700)
    moved = []
    try:
        for path in files:
            dest = backup / path.name
            os.replace(path, dest); moved.append((path, dest))
    except BaseException:
        for path, dest in reversed(moved): os.replace(dest, path)
        raise
    print('Moved three diagnostic water shaders and metadata to local backup.')


if __name__ == '__main__': main()
