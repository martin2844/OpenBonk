#!/usr/bin/env python3
"""Package a two-file Lua mod deterministically for launcher import."""
import argparse
import json
from pathlib import Path
import re
import zipfile


def package(source: Path, output: Path) -> None:
    manifest_path = source / 'manifest.json'
    lua_path = source / 'main.lua'
    if any(path.is_symlink() or not path.is_file() for path in (manifest_path, lua_path)):
        raise ValueError('mod must contain regular manifest.json and main.lua files')
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    mod_id = manifest.get('id') if isinstance(manifest, dict) else None
    if not isinstance(mod_id, str) or not re.fullmatch(r'[a-z0-9][a-z0-9_-]{0,63}', mod_id):
        raise ValueError('invalid mod id')
    if not lua_path.read_bytes().strip():
        raise ValueError('main.lua is empty')
    if output.resolve() in (manifest_path.resolve(), lua_path.resolve()):
        raise ValueError('output must not overwrite mod inputs')
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name in ('manifest.json', 'main.lua'):
            info = zipfile.ZipInfo(name, (1980, 1, 1, 0, 0, 0))
            info.create_system = 3
            info.external_attr = 0o100644 << 16
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, (source / name).read_bytes(), compress_type=zipfile.ZIP_DEFLATED, compresslevel=9)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    package(args.source, args.output)


if __name__ == '__main__':
    main()
