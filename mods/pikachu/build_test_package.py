#!/usr/bin/env python3
"""Build the experimental local API 3 package; do not redistribute pending rights review."""
import hashlib, json, pathlib, zipfile
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
output = root / 'study/pikachu-content-local-test.zip'
with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as package:
    package.writestr('manifest.json', json.dumps(manifest, separators=(',', ':')))
    for name, path in files.items(): package.write(path, name)
print(output, output.stat().st_size, hashlib.sha256(output.read_bytes()).hexdigest())
