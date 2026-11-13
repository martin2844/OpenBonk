#!/usr/bin/env python3
"""Create a local Steam data ZIP for the experimental OpenBonk launcher."""
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import tempfile
import zipfile

MANIFEST = Path(__file__).with_name('launcher-steam-inputs.json')
ZIP_LIMIT = 1024 * 1024 * 1024
BLOCK = 1024 * 1024


def digest_file(path):
    h = hashlib.sha256()
    total = 0
    with path.open('rb') as src:
        while chunk := src.read(BLOCK):
            total += len(chunk)
            h.update(chunk)
    return total, h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__, epilog='This ZIP contains game files for personal use. The experimental APK and ZIP are not a public release.')
    parser.add_argument('data_dir', type=Path, help='Megabonk_Data directory from a supported Steam installation')
    parser.add_argument('output', type=Path, help='new ZIP file outside the game directory')
    parser.add_argument('--platform', choices=('linux', 'windows'), help='choose explicitly if both platform folders are present')
    args = parser.parse_args()
    root = args.data_dir.resolve(strict=True)
    if not root.is_dir():
        parser.error('data_dir must be a directory')
    output = args.output.absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new file in an existing directory')
    if output.resolve().is_relative_to(root):
        parser.error('output must be outside the game data directory')
    platforms = {'linux': 'StandaloneLinux64', 'windows': 'StandaloneWindows64'}
    present = [key for key, folder in platforms.items() if (root / 'StreamingAssets' / 'aa' / folder).is_dir()]
    platform = args.platform
    if platform is None:
        if len(present) != 1:
            parser.error('cannot determine platform; use --platform linux or --platform windows')
        platform = present[0]
    if platform not in present:
        parser.error('selected platform folder is missing')
    inventory = json.loads(MANIFEST.read_text(encoding='utf-8'))['platforms'][platform]
    if len(inventory) != 39:
        raise ValueError('invalid input inventory')
    files = []
    for relative, expected in sorted(inventory.items()):
        parts = PurePosixPath(relative).parts
        if not parts or relative.startswith('/') or any(p in ('', '.', '..') or '\\' in p or ':' in p for p in parts):
            raise ValueError('invalid input inventory path')
        path = root.joinpath(*parts)
        if not path.resolve().is_relative_to(root) or not path.is_file():
            raise ValueError('missing file: ' + relative)
        if digest_file(path) != (expected['size'], expected['sha256']):
            raise ValueError('unsupported or damaged file: ' + relative)
        files.append((relative, path, expected))
    print('Validated', len(files), platform, 'Steam files.', flush=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(prefix='.openbonk-steam-', suffix='.tmp', dir=output.parent, delete=False) as tmp:
            temporary = Path(tmp.name)
        with zipfile.ZipFile(temporary, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as archive:
            for relative, path, expected in files:
                with path.open('rb') as src, archive.open('Megabonk_Data/' + relative, 'w', force_zip64=True) as dst:
                    h = hashlib.sha256(); size = 0
                    while chunk := src.read(BLOCK):
                        size += len(chunk); h.update(chunk); dst.write(chunk)
                if (size, h.hexdigest()) != (expected['size'], expected['sha256']):
                    raise ValueError('file changed while creating ZIP: ' + relative)
        if temporary.stat().st_size > ZIP_LIMIT:
            raise ValueError('ZIP exceeds the launcher size limit')
        with zipfile.ZipFile(temporary) as archive:
            for relative, _, expected in files:
                h = hashlib.sha256(); size = 0
                with archive.open('Megabonk_Data/' + relative) as src:
                    while chunk := src.read(BLOCK):
                        size += len(chunk); h.update(chunk)
                if (size, h.hexdigest()) != (expected['size'], expected['sha256']):
                    raise ValueError('ZIP verification failed: ' + relative)
        with temporary.open('r+b') as f:
            os.fsync(f.fileno())
        os.link(temporary, output)  # Atomic publication; never overwrite an existing file.
        print('Created', output, '(' + str(temporary.stat().st_size) + ' bytes). Keep this game-data ZIP private.')
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


if __name__ == '__main__':
    try:
        main()
    except (OSError, ValueError, KeyError, zipfile.BadZipFile) as error:
        raise SystemExit('Could not create Steam data ZIP: ' + str(error)) from error
