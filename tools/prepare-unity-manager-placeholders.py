#!/usr/bin/env python3
"""Remove the verified empty AlwaysManagers placeholder from isolated scenes.

The original scene files and backups contain game data and remain local. Any
unexpected missing manager component or extra reference is rejected for manual review.
"""
import argparse
import json
import os
from pathlib import Path
import re
import tempfile

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
SCENES = ('MainMenu', 'GeneratedMap', 'FinalBossMap')
DOC = re.compile(rb'^--- !u!(\d+) &(-?\d+)(?: stripped)?\n', re.MULTILINE)


def render(data):
    headers = list(DOC.finditer(data)); docs = {}
    for index, match in enumerate(headers):
        end = headers[index + 1].start() if index + 1 < len(headers) else len(data)
        key = int(match.group(2))
        if key in docs: raise ValueError('duplicate scene object id')
        docs[key] = (int(match.group(1)), match.start(), end, data[match.start():end])
    managers = [(key, item) for key, item in docs.items() if item[0] == 1 and re.search(rb'^  m_Name: AlwaysManagers$', item[3], re.MULTILINE)]
    if len(managers) != 1: raise ValueError('expected exactly one AlwaysManagers object')
    owner, (_, go_start, go_end, go) = managers[0]
    component_ids = [int(v) for v in re.findall(rb'^  - component: \{fileID: (-?\d+)\}$', go, re.MULTILINE)]
    if len(component_ids) != len(set(component_ids)): raise ValueError('duplicate manager component')
    missing = []
    for key in component_ids:
        if key not in docs: raise ValueError('missing manager component object')
        kind, start, end, block = docs[key]
        if kind != 114 or not re.search(rb'^  m_Script: \{fileID: 0\}$', block, re.MULTILINE): continue
        expected = (f'--- !u!114 &{key}\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n'
                    f'  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n'
                    f'  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {owner}}}\n'
                    f'  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 0}}\n'
                    f'  m_Name: \n  m_EditorClassIdentifier: \n').encode()
        if block != expected: raise ValueError('missing manager component is not an empty placeholder')
        if len(re.findall(rb'(?<!\d)fileID: ' + str(key).encode() + rb'(?=[,}])', data)) != 1:
            raise ValueError('placeholder has unexpected references')
        missing.append((key, start, end))
    if len(missing) > 1: raise ValueError('multiple empty manager components')
    if not missing: return data
    key, start, end = missing[0]
    component = f'  - component: {{fileID: {key}}}\n'.encode()
    position = go.find(component)
    if position < 0 or go.count(component) != 1: raise ValueError('manager component list differs')
    ranges = sorted(((start, end), (go_start + position, go_start + position + len(component))), reverse=True)
    result = data
    for lo, hi in ranges: result = result[:lo] + result[hi:]
    return result


def verify(project):
    for name in SCENES:
        path = project / 'Assets/ScenesAndroid' / (name + '.unity')
        if path.is_symlink() or not path.is_file(): raise ValueError('Android scene missing: ' + name)
        data = path.read_bytes()
        if render(data) != data: raise ValueError('empty AlwaysManagers component remains in ' + name)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', type=Path, required=True)
    p.add_argument('--backup', type=Path, help='new directory outside the workspace')
    p.add_argument('--check', action='store_true')
    a = p.parse_args(); project = a.project.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER: p.error('project must be an isolated workspace')
    if (project / 'Temp/UnityLockfile').exists(): p.error('Unity workspace is locked')
    originals, updates = {}, {}
    for name in SCENES:
        path = project / 'Assets/ScenesAndroid' / (name + '.unity')
        if path.is_symlink() or not path.is_file(): p.error('Android scene missing: ' + name)
        originals[path] = path.read_bytes(); updates[path] = render(originals[path])
    changed = [path for path in originals if originals[path] != updates[path]]
    if a.check:
        if changed: p.error('empty AlwaysManagers component remains in Android scenes')
        print('No empty AlwaysManagers components remain.'); return
    if not changed:
        print('Android scenes already have no empty AlwaysManagers components.'); return
    if not a.backup: p.error('a new backup directory is required')
    backup = a.backup.expanduser().absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir(): p.error('backup must be new and outside workspace')
    backup = backup.parent.resolve() / backup.name
    if backup.is_relative_to(project): p.error('backup must be outside workspace')
    backup.mkdir(mode=0o700)
    for path in changed: (backup / path.name).write_bytes(originals[path])
    applied = []; temporary = None
    try:
        for path in changed:
            with tempfile.NamedTemporaryFile('wb', dir=path.parent, prefix='.openbonk-scene-', delete=False) as stream:
                temporary = Path(stream.name); stream.write(updates[path]); stream.flush(); os.fsync(stream.fileno())
            os.chmod(temporary, path.stat().st_mode & 0o777)
            os.replace(temporary, path); temporary = None; applied.append(path)
    except BaseException:
        for path in applied: path.write_bytes(originals[path])
        raise
    finally:
        if temporary is not None: temporary.unlink(missing_ok=True)
    print(f'Removed {len(changed)} empty manager placeholders; original scenes are backed up locally.')


if __name__ == '__main__': main()
