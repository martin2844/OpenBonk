#!/usr/bin/env python3
"""Check workspace binary inputs against the local export and generated plugins.

This is an integrity check, not a provenance, licensing, compilation or gameplay
verification. All inputs can contain game-derived material and must stay local.
"""
import argparse
import hashlib
import json
from pathlib import Path


MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
EXTENSIONS = {'.dll', '.so', '.aar', '.jar', '.a', '.dylib'}
PLUGINS = ('Rewired_Core.dll', 'Coffee.UIParticle.dll', 'com.rlabrecque.steamworks.net.dll')


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').digest()


def compare(workspace_assets, export_assets, plugins, lua):
    generated = {Path('Plugins') / name: plugins / name for name in PLUGINS}
    generated[Path('Plugins/Android/com.rlabrecque.steamworks.net.dll')] = plugins / PLUGINS[2]
    generated[Path('Plugins/Android/arm64-v8a/libmegabonk_lua.so')] = lua
    result = {'export': [], 'generated': [], 'missing_input': [], 'changed': []}
    found = set()
    for path in sorted(workspace_assets.rglob('*')):
        if not path.is_file() or path.suffix.lower() not in EXTENSIONS:
            continue
        relative = path.relative_to(workspace_assets)
        source = generated.get(relative, export_assets / relative)
        if path.is_symlink() or source.is_symlink():
            raise ValueError('symbolic link in binary inputs: ' + relative.as_posix())
        found.add(relative)
        label = 'generated' if relative in generated else 'export'
        if not source.is_file():
            result['missing_input'].append(relative.as_posix())
        elif digest(path) != digest(source):
            result['changed'].append(relative.as_posix())
        else:
            result[label].append(relative.as_posix())
    result['missing_workspace'] = sorted(str(p) for p in generated.keys() - found)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name, help_text in (
        ('project', 'isolated assembled Unity workspace'),
        ('export', 'original local AssetRipper ExportedProject'),
        ('plugins', 'directory of locally prepared managed plugins'),
        ('lua-library', 'locally built ARM64 Lua bridge'),
    ):
        parser.add_argument('--' + name, type=Path, required=True, help=help_text)
    args = parser.parse_args()
    try:
        project, export, plugins, lua = (p.expanduser().resolve(strict=True) for p in
                                         (args.project, args.export, args.plugins, args.lua_library))
        if json.loads((project / '.openbonk-workspace.json').read_text()) != MARKER:
            raise ValueError('invalid workspace marker')
        if not (project / 'Assets').is_dir() or not (export / 'Assets').is_dir() or not plugins.is_dir() or not lua.is_file():
            raise ValueError('required project assets, plugin directory or Lua library missing')
        result = compare(project / 'Assets', export / 'Assets', plugins, lua)
    except (OSError, ValueError, json.JSONDecodeError) as error:
        parser.error(str(error))
    print(json.dumps(result, indent=2, sort_keys=True))
    if result['missing_input'] or result['changed'] or result['missing_workspace']:
        raise SystemExit(1)
    print('PASS: workspace binaries match local inputs; provenance and runtime behavior are not established.')


if __name__ == '__main__':
    main()
