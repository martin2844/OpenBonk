#!/usr/bin/env python3
"""Check supported ZIP layouts using a verified local Steam-data ZIP."""
import argparse
import importlib.util
from pathlib import Path
import shutil
import zipfile

root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('data_bundle', root/'tools/prepare-android-data-bundle.py')
bundle = importlib.util.module_from_spec(spec); spec.loader.exec_module(bundle)

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('steam_zip', type=Path)
parser.add_argument('output_dir', type=Path, help='new empty directory for local private test fixtures')
args = parser.parse_args()
source = args.steam_zip.resolve(strict=True)
out = args.output_dir.absolute()
if out.exists() or out.is_symlink() or not out.parent.is_dir():
    parser.error('output must be a new directory in an existing parent')
bundle.verify_steam_zip(source)
out.mkdir(mode=0o700)
with zipfile.ZipFile(source) as src:
    for label, prefix in [('root', ''), ('nested', 'Megabonk/Megabonk_Data/')]:
        with zipfile.ZipFile(out/(label+'.zip'), 'x', compression=zipfile.ZIP_STORED, allowZip64=True) as dst:
            for info in src.infolist():
                if info.is_dir(): continue
                if not info.filename.startswith('Megabonk_Data/'):
                    raise ValueError('test source must have a Megabonk_Data root')
                name = prefix + info.filename[len('Megabonk_Data/'):]
                with src.open(info) as inp, dst.open(name, 'w', force_zip64=True) as target:
                    shutil.copyfileobj(inp, target, 1024*1024)
        bundle.verify_steam_zip(out/(label+'.zip'))
    # Adding a second recognized root must not make the selected files ambiguous.
    with zipfile.ZipFile(out/'ambiguous.zip', 'x') as z:
        z.writestr('Megabonk_Data/sharedassets1.assets', b'one')
        z.writestr('backup/Megabonk_Data/sharedassets1.assets', b'two')
    try:
        bundle.verify_steam_zip(out/'ambiguous.zip')
    except ValueError:
        pass
    else:
        raise AssertionError('ambiguous roots accepted')
print('PC Steam ZIP layouts: PASS')
