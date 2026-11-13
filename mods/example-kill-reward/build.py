#!/usr/bin/env python3
"""Build an experimental API 3 kill-reward item without game assets."""
import hashlib
import json
import runpy
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo

root = Path(__file__).resolve().parent
icon = runpy.run_path(str(root.parent / 'example-healing-item' / 'build.py'))['icon']()
payloads = {'main.lua': (root / 'main.lua').read_bytes(), 'icon.png': icon}
manifest = {'api': 3, 'kind': 'content', 'id': 'openbonk.example.killreward',
    'item': {'id': 'kill-reward', 'name': 'Victory Heart',
             'description': 'Gain 1 health and XP per stack when an enemy dies.',
             'icon': 'icon.png', 'rarity': 'Rare', 'inPool': True,
             'bonuses': [], 'hooks': ['enemy_killed']},
    'files': [{'path': name, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest()}
              for name, data in payloads.items()]}
output = root / 'example-kill-reward.zip'
with ZipFile(output, 'x', compression=ZIP_DEFLATED) as archive:
    for name, data in [('manifest.json', (json.dumps(manifest, indent=2) + '\n').encode()), *payloads.items()]:
        entry = ZipInfo(name, (2020, 1, 1, 0, 0, 0)); entry.compress_type = ZIP_DEFLATED
        archive.writestr(entry, data)
print(output)
