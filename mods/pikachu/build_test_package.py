#!/usr/bin/env python3
"""Build the experimental local API 3 package; do not redistribute pending rights review."""
import argparse, hashlib, json, pathlib, zipfile
root = pathlib.Path(__file__).resolve().parent
files = {
    'main.lua': root / 'content/main.lua',
    'assets/pikachu-model': root / 'study/android-bundle/pikachu-model',
    'assets/portrait.png': root / 'content/portrait.png',
    'assets/thunderbolt-icon.png': root / 'content/thunderbolt-icon.png',
}
manifest = {
    'api': 3, 'kind': 'content', 'id': 'pikachu',
    'files': [{'path': name, 'size': path.stat().st_size, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()} for name, path in files.items()],
    'character': {'id': 'pikachu', 'name': 'Pikachu', 'description': 'An energetic Pokémon that battles with Thunderbolt.',
                  'bundle': 'assets/pikachu-model', 'model': 'pikachu-study', 'walk': 'Armature|OpenBonk walk study',
                  'idle': 'Armature|OpenBonk idle', 'run': 'Armature|OpenBonk run', 'jump': 'Armature|OpenBonk jump', 'surf': 'Armature|OpenBonk surf', 'surfboard': 'OpenBonkSurfboard',
                  'portrait': 'assets/portrait.png', 'scale': .24},
    'weapon': {'id': 'thunderbolt', 'name': 'Thunderbolt', 'description': 'Strikes an enemy and chains lightning; every second strike is stronger.',
               'effect': 'lightning', 'icon': 'assets/thunderbolt-icon.png', 'lightningColor': '#3DC9FF', 'soundClip': 'thunderbolt-zap',
               'damage': 14, 'cooldown': 1.1, 'range': 24, 'chainRange': 7, 'chainProc': .5, 'bounces': 2},
    'passive': {'name': 'Electric Energy', 'description': 'Gain 1% power-up drop chance and 1% duration per level gained.',
                'perLevel': [{'stat': 'PowerupChance', 'amount': .01}, {'stat': 'DurationMultiplier', 'amount': .01}]},
}
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', type=pathlib.Path, default=root / 'pikachu-mod.zip',
                    help='new ZIP path in an existing directory')
output = parser.parse_args().output.expanduser().absolute()
if output.exists() or output.is_symlink() or not output.parent.is_dir():
    parser.error('output must be a new path in an existing directory')
with zipfile.ZipFile(output, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as package:
    for name, data in [('manifest.json', json.dumps(manifest, separators=(',', ':')).encode()),
                       *((name, path.read_bytes()) for name, path in files.items())]:
        info = zipfile.ZipInfo(name, (2020, 1, 1, 0, 0, 0))
        info.compress_type = zipfile.ZIP_DEFLATED
        package.writestr(info, data, compresslevel=9)
print(output, output.stat().st_size, hashlib.sha256(output.read_bytes()).hexdigest())
