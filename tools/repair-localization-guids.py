#!/usr/bin/env python3
"""Restore original shared-table asset GUIDs in an isolated Unity workspace.

This edits local, game-derived files. It saves every changed original in a new
backup directory and does not redistribute the localization content.
"""
import argparse
from collections import defaultdict
import json
from pathlib import Path
import re
import shutil
import uuid

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
META_GUID = re.compile(rb'^guid:\s*([0-9a-fA-F]{32})\s*$', re.M)


def repair(project, export, backup):
    project = project.resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER:
        raise ValueError('project must be an isolated workspace')
    if (project / 'Temp/UnityLockfile').exists(): raise ValueError('Unity workspace is locked')
    export = export.resolve(strict=True)
    backup = backup.absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir(): raise ValueError('backup must be new')
    if backup.is_relative_to(project) or project.is_relative_to(backup): raise ValueError('backup must be separate from workspace')
    data = json.loads(export.read_text()); tables = data.get('tables')
    if not isinstance(tables, list) or not tables: raise ValueError('no localization tables')
    collections = {}
    for row in tables:
        name = row['collection']
        if not isinstance(name, str) or not name or any(c in name for c in '/\\\0') or name in ('.', '..'):
            raise ValueError('invalid collection name')
        guid = uuid.UUID(row['collectionGuid']).hex
        if name in collections and collections[name] != guid: raise ValueError('inconsistent collection GUID')
        collections[name] = guid
    if len(set(collections.values())) != len(collections): raise ValueError('duplicate collection GUID')
    roots = [project / name for name in ('Assets', 'Packages', 'ProjectSettings')]
    owners = defaultdict(list); files = []
    for root in roots:
        if not root.is_dir() or root.is_symlink(): raise ValueError('missing project directory')
        for path in root.rglob('*'):
            if path.is_symlink(): raise ValueError('symbolic links are not supported')
            if not path.is_file(): continue
            files.append(path)
            if path.suffix == '.meta':
                matches = META_GUID.findall(path.read_bytes())
                if len(matches) != 1: raise ValueError('invalid Unity metadata: ' + str(path.relative_to(project)))
                owners[matches[0].lower()].append(path)
    mapping = {}
    for name, guid in collections.items():
        meta = project / 'Assets/Localization/String Tables' / (name + ' Shared Data.asset.meta')
        if not meta.is_file() or meta.is_symlink(): raise ValueError('missing shared-table metadata: ' + name)
        old = META_GUID.search(meta.read_bytes()).group(1).lower(); new = guid.encode('ascii')
        if owners[old] != [meta] or any(path != meta for path in owners.get(new, [])):
            raise ValueError('shared-table GUID collision: ' + name)
        if old != new: mapping[old] = new
    if any(new in mapping for new in mapping.values()): raise ValueError('chained GUID mappings are unsupported')
    if not mapping:
        print('Shared-table asset GUIDs already match their collections.')
        return 0
    pattern = re.compile(rb'(?i)(?<![0-9a-f])(?:' + b'|'.join(mapping) + rb')(?![0-9a-f])')
    changes = []
    for path in files:
        data = path.read_bytes()
        # Most project files are binary payloads or unrelated assets. Check
        # with fast byte searches before running the boundary-aware regex.
        folded = data.lower()
        if not any(guid in folded for guid in mapping): continue
        if not pattern.search(data): continue
        try: data.decode('utf-8')
        except UnicodeDecodeError as e: raise ValueError('GUID occurs in non-text file: ' + str(path.relative_to(project))) from e
        if b'\0' in data: raise ValueError('GUID occurs in binary file: ' + str(path.relative_to(project)))
        updated = pattern.sub(lambda match: mapping[match.group(0).lower()], data)
        changes.append((path, data, updated))
    backup.mkdir(mode=0o700)
    written = []
    try:
        for path, original, updated in changes:
            save = backup / path.relative_to(project)
            save.parent.mkdir(parents=True, exist_ok=True)
            save.write_bytes(original)
            written.append((path, original))
            path.write_bytes(updated)
    except BaseException:
        for path, original in reversed(written): path.write_bytes(original)
        shutil.rmtree(backup)
        raise
    print(f'Restored {len(mapping)} collection GUIDs in {len(changes)} local files; originals remain in the backup.')
    return len(mapping)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', required=True, type=Path)
    p.add_argument('--export', required=True, type=Path)
    p.add_argument('--backup', required=True, type=Path)
    a = p.parse_args()
    repair(a.project, a.export, a.backup)


if __name__ == '__main__': main()
