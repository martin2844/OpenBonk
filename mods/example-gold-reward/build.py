#!/usr/bin/env python3
"""Build an example Lua item with a procedural icon and no game assets."""
import hashlib
import json
from pathlib import Path
import runpy
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo

root = Path(__file__).resolve().parent
icon = runpy.run_path(str(root.parent / 'example-healing-item' / 'build.py'))['icon']()
payloads = {'main.lua': (root / 'main.lua').read_bytes(), 'icon.png': icon}
manifest = {'api': 3, 'kind': 'content', 'id': 'openbonk.example.goldreward',
    'item': {'id': 'gold-reward', 'name': 'Golden Heart',
             'description': 'Gain 2 gold per stack when an enemy dies.',
             'icon': 'icon.png', 'rarity': 'Rare', 'inPool': True,
             'bonuses': [], 'hooks': ['enemy_killed']},
    'files': [{'path': name, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest()}
              for name, data in payloads.items()]}
output = root / 'example-gold-reward.zip'
with ZipFile(output, 'x', compression=ZIP_DEFLATED) as archive:
    for name, data in [('manifest.json', (json.dumps(manifest, indent=2) + '\n').encode()), *payloads.items()]:
        entry = ZipInfo(name, (2020, 1, 1, 0, 0, 0))
        entry.compress_type = ZIP_DEFLATED
        archive.writestr(entry, data)
print(output)
