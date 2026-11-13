#!/usr/bin/env python3
"""Make an isolated Unity workspace from a local base and this checkout's sources.

The base is a compatible reconstructed Unity project, not a Steam ZIP. This
does not generate the base project, build an APK, or publish game data.
"""
import argparse
import json
import os
from pathlib import Path
import re
import shutil


ROOT = Path(__file__).resolve().parent.parent
OVERLAY = ROOT / 'unity/OpenBonk'
PROJECT_DIRS = ('Assets', 'Packages', 'ProjectSettings')
GUID = re.compile(rb'^guid:\s*([0-9a-fA-F]{32})\s*$', re.MULTILINE)
GUID_REFERENCE = re.compile(rb'(?i)(?<![a-z0-9_])guid\s*:\s*([0-9a-f]{32})(?![a-z0-9])')
VERSION = 'm_EditorVersion: 2023.2.22f1'
LEGACY_AUDIO = Path('Assets/Editor/AssetRipperPatches')
AUDIO = Path('Assets/Editor/AudioImport')


def overlay_path(relative):
    if relative == LEGACY_AUDIO.with_suffix('.meta'):
        return AUDIO.with_suffix('.meta')
    if relative.is_relative_to(LEGACY_AUDIO):
        return AUDIO / relative.relative_to(LEGACY_AUDIO)
    return relative


def reject_links(root):
    for directory, names, files in os.walk(root, followlinks=False):
        for name in names + files:
            if (Path(directory) / name).is_symlink():
                raise ValueError('symbolic links are not supported in project inputs')


def meta_guid(path):
    match = GUID.search(path.read_bytes())
    return match.group(1).lower() if match else None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base-project', required=True, type=Path, help='local compatible reconstructed Unity project')
    parser.add_argument('--output', required=True, type=Path, help='new directory outside the input project')
    parser.add_argument('--check', action='store_true', help='validate inputs without writing output')
    parser.add_argument('--allow-guid-remap', action='store_true', help='experimentally remap conflicting Unity text GUID references in the local copy')
    args = parser.parse_args()
    base = args.base_project.expanduser().resolve(strict=True)
    output = args.output.expanduser().absolute()
    if not base.is_dir() or any(not (base / name).is_dir() for name in PROJECT_DIRS):
        parser.error('base project must contain Assets, Packages and ProjectSettings')
    if not OVERLAY.is_dir():
        parser.error('checkout Unity source overlay is missing')
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new path in an existing directory')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(base) or base.is_relative_to(output) or output.is_relative_to(OVERLAY):
        parser.error('output must be separate from the base and source overlay')
    for project in (base, OVERLAY):
        version = project / 'ProjectSettings/ProjectVersion.txt'
        if not version.is_file() or VERSION not in version.read_text(encoding='utf-8').splitlines():
            parser.error('base and checkout must use Unity 2023.2.22f1')
    for name in PROJECT_DIRS:
        reject_links(base / name)
    reject_links(OVERLAY)
    legacy = base / LEGACY_AUDIO
    legacy_meta = base / LEGACY_AUDIO.with_suffix('.meta')
    if legacy.exists() or legacy_meta.exists():
        if not legacy.is_dir() or (legacy_meta.exists() and not legacy_meta.is_file()):
            parser.error('incomplete legacy audio patch directory')
        if (base / AUDIO).exists() or (base / AUDIO.with_suffix('.meta')).exists():
            parser.error('both legacy and renamed audio patch directories exist')
        names = {p.name for p in legacy.iterdir()}
        if 'AudioMixerPostprocessor.cs' not in names or names - {'AudioMixerPostprocessor.cs', 'AudioMixerPostprocessor.cs.meta'}:
            parser.error('unexpected files in legacy audio patch directory')
    checked = 0
    mapping, destinations, overlay_owners = {}, {}, {}
    for meta in OVERLAY.rglob('*.meta'):
        relative = meta.relative_to(OVERLAY)
        replacement = meta_guid(meta)
        if not replacement:
            parser.error('Unity metadata GUID missing: ' + str(relative))
        if replacement in overlay_owners and overlay_owners[replacement] != relative:
            parser.error('duplicate GUID in source overlay')
        overlay_owners[replacement] = relative
        counterpart = base / meta.relative_to(OVERLAY)
        if not counterpart.exists() and legacy.is_dir():
            relative = meta.relative_to(OVERLAY)
            if relative == AUDIO.with_suffix('.meta'):
                counterpart = legacy_meta
            elif relative.is_relative_to(AUDIO):
                counterpart = legacy / relative.relative_to(AUDIO)
        if not counterpart.exists():
            continue
        if not counterpart.is_file():
            parser.error('base has a non-file metadata path')
        original = meta_guid(counterpart)
        if not original:
            parser.error('Unity metadata GUID missing: ' + str(meta.relative_to(OVERLAY)))
        if original != replacement:
            if not args.allow_guid_remap:
                parser.error('Unity metadata GUID mismatch: ' + str(meta.relative_to(OVERLAY)))
            if original in mapping and mapping[original] != replacement:
                parser.error('one original GUID maps to multiple targets')
            if replacement in destinations and destinations[replacement] != meta.relative_to(OVERLAY):
                parser.error('multiple files map to the same target GUID')
            mapping[original] = replacement
            destinations[replacement] = meta.relative_to(OVERLAY)
        else:
            checked += 1
    rewrites = []
    owners = {}
    for name in PROJECT_DIRS:
        for meta in (base / name).rglob('*.meta'):
            guid = meta_guid(meta)
            relative = meta.relative_to(base)
            if guid in overlay_owners and overlay_owners[guid] != overlay_path(relative):
                parser.error('source GUID is already owned by a different base asset')
            if guid in mapping:
                owners.setdefault(guid, []).append(relative)
    if mapping:
        if any(value in mapping for value in mapping.values()):
            parser.error('chained GUID mappings are not supported')
        for original in mapping:
            if len(owners.get(original, ())) != 1:
                parser.error('an original GUID is missing or has multiple owners')
        for name in PROJECT_DIRS:
            for path in (base / name).rglob('*'):
                if not path.is_file():
                    continue
                data = path.read_bytes()
                matches = [m for m in GUID_REFERENCE.finditer(data) if m.group(1).lower() in mapping]
                if not matches:
                    continue
                try:
                    data.decode('utf-8')
                except UnicodeDecodeError:
                    parser.error('GUID reference found in non-UTF-8 file: ' + str(path.relative_to(base)))
                if b'\0' in data:
                    parser.error('GUID reference found in binary file: ' + str(path.relative_to(base)))
                rewrites.append((path.relative_to(base), len(matches)))
    print(f'Compatible Unity version; {checked} existing metadata GUIDs match; '
          f'{len(mapping)} GUID mappings in {len(rewrites)} text files.')
    if args.check:
        print('Check only: no files written. A full build has not been verified.')
        return
    output.mkdir(mode=0o700)
    try:
        for name in PROJECT_DIRS:
            shutil.copytree(base / name, output / name, copy_function=shutil.copy2)
        for relative, expected_count in rewrites:
            path = output / relative
            data = path.read_bytes()
            count = [0]
            def replace(match):
                old = match.group(1).lower()
                if old not in mapping:
                    return match.group(0)
                count[0] += 1
                return match.group(0)[:match.start(1) - match.start()] + mapping[old]
            updated = GUID_REFERENCE.sub(replace, data)
            if count[0] != expected_count:
                raise ValueError('base project changed during copy: ' + str(relative))
            path.write_bytes(updated)
        if legacy.is_dir():
            shutil.move(str(output / LEGACY_AUDIO), str(output / AUDIO))
            if legacy_meta.is_file():
                shutil.move(str(output / LEGACY_AUDIO.with_suffix('.meta')), str(output / AUDIO.with_suffix('.meta')))
        for name in PROJECT_DIRS:
            source = OVERLAY / name
            if source.is_dir():
                shutil.copytree(source, output / name, dirs_exist_ok=True, copy_function=shutil.copy2)
        (output / '.openbonk-workspace.json').write_text(
            json.dumps({'schema': 1, 'kind': 'openbonk-local-unity-workspace'}) + '\n', encoding='utf-8')
    except BaseException:
        shutil.rmtree(output)
        raise
    print('Created isolated local workspace:', output)
    print('Contains local game-derived data. Do not publish it without review.')
    print('This workspace alone does not supply all APK build inputs.')


if __name__ == '__main__':
    main()
