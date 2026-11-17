#!/usr/bin/env python3
"""Inspect custom-character metadata without importing a PC mod into OpenBonk.

This reports features that require manual compatibility review. It does not
test an APK, inspect or extract Unity assets, or prove distribution rights.
"""
import argparse
import json
from pathlib import Path
import zipfile

MAX_ENTRIES = 2048
MAX_METADATA = 256 * 1024


def inspect_package(path):
    with zipfile.ZipFile(path) as archive:
        entries = archive.infolist()
        if len(entries) > MAX_ENTRIES:
            raise ValueError('package has too many entries')
        matches = [entry for entry in entries if not entry.is_dir() and entry.filename.lower().endswith('.custom.json')]
        if len(matches) != 1:
            raise ValueError('package must contain exactly one custom-character metadata file')
        entry = matches[0]
        if entry.file_size > MAX_METADATA:
            raise ValueError('metadata file is too large')
        with archive.open(entry) as stream:
            raw = stream.read(MAX_METADATA + 1)
        if len(raw) > MAX_METADATA:
            raise ValueError('metadata file is too large')
    data = json.loads(raw.decode('utf-8-sig'))
    if not isinstance(data, dict):
        raise ValueError('metadata root must be an object')
    character = data.get('character')
    if not isinstance(character, dict) or not isinstance(character.get('characterName'), str):
        raise ValueError('invalid character metadata')
    stats = character.get('statModifiers', [])
    if not isinstance(stats, list) or any(not isinstance(item, dict) for item in stats):
        raise ValueError('invalid character stat modifiers')
    weapon = data.get('weapon')
    passive = data.get('passive')
    if weapon is not None and not isinstance(weapon, dict):
        raise ValueError('invalid weapon metadata')
    if passive is not None and not isinstance(passive, dict):
        raise ValueError('invalid passive metadata')
    requirements = ['android_asset_bundle', 'material_and_animation_review']
    if stats:
        requirements.append('character_specific_base_stats')
    if character.get('colliderHeight') is not None or character.get('colliderWidth') is not None:
        requirements.append('character_specific_collider_review')
    if weapon:
        requirements.append('weapon_behavior_and_upgrade_review')
        if weapon.get('weaponAttackPath'):
            requirements.append('custom_projectile_or_equivalent')
    if passive:
        requirements.append('passive_behavior_review')
    if data.get('skins'):
        requirements.append('skin_support_review')
    return {
        'metadata_path': entry.filename,
        'character_name': character['characterName'],
        'author': character.get('author', ''),
        'bundle': character.get('assetBundleName', ''),
        'prefab': character.get('prefabPath', ''),
        'stat_modifier_count': len(stats),
        'collider_height': character.get('colliderHeight'),
        'collider_width': character.get('colliderWidth'),
        'passive_name': passive.get('passiveName', '') if passive else '',
        'weapon_name': weapon.get('weaponName', '') if weapon else '',
        'weapon_attack_path': weapon.get('weaponAttackPath', '') if weapon else '',
        'requirements': requirements,
        'status': 'manual_review_required',
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('package', type=Path, help='local third-party mod ZIP')
    args = parser.parse_args()
    try:
        print(json.dumps(inspect_package(args.package), indent=2, ensure_ascii=False))
    except (OSError, ValueError, KeyError, RuntimeError, UnicodeError, zipfile.BadZipFile) as exc:
        parser.exit(1, 'Cannot inspect package: ' + str(exc) + '\n')


if __name__ == '__main__':
    main()
