#!/usr/bin/env python3
"""Install the tested Android input axes into an isolated Unity workspace."""
import argparse
from collections import Counter
import json
import os
from pathlib import Path
import re
import tempfile

ROOT = Path(__file__).resolve().parent.parent
CONFIG = ROOT / 'tools/android-input-axes.json'
MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
FIELDS = ('m_Name', 'descriptiveName', 'descriptiveNegativeName', 'negativeButton', 'positiveButton',
          'altNegativeButton', 'altPositiveButton', 'gravity', 'dead', 'sensitivity', 'snap', 'invert', 'type', 'axis', 'joyNum')
NAMES = {'Horizontal': 4, 'Vertical': 4, 'ControllerLookHorizontal': 1, 'ControllerLookVertical': 1,
         'NativeMoveHorizontal': 1, 'NativeMoveVertical': 1, 'NativeStickMoveHorizontal': 1,
         'NativeStickMoveVertical': 1, 'NativeAimTrigger': 1, 'NativeMapTrigger': 1, 'NativeChestHorizontal': 4}
AXIS_START = re.compile(r'^  - serializedVersion: 3\n', re.M)
FIELD = re.compile(r'^    ([A-Za-z_][A-Za-z0-9_]*): ?([^\n]*)$', re.M)


def definitions():
    data = json.loads(CONFIG.read_text())
    rows = data.get('axes')
    if data.get('schema') != 1 or not isinstance(rows, list): raise ValueError('invalid input axis configuration')
    for row in rows:
        if not isinstance(row, dict) or set(row) != set(FIELDS): raise ValueError('invalid input axis fields')
        for value in row.values():
            if not isinstance(value, str) or not re.fullmatch(r'[A-Za-z0-9_. -]*', value): raise ValueError('invalid input axis value')
    if Counter(row['m_Name'] for row in rows) != Counter(NAMES): raise ValueError('invalid input axis inventory')
    return rows


def render(text, rows):
    marker = '  m_Axes:\n'
    if text.count(marker) != 1: raise ValueError('unsupported InputManager format')
    prefix, rest = text.split(marker, 1)
    suffix_at = re.search(r'^  m_UsePhysicalKeys: [01]\n?\Z', rest, re.M)
    if not suffix_at: raise ValueError('missing InputManager physical-key setting')
    body, suffix = rest[:suffix_at.start()], rest[suffix_at.start():]
    starts = list(AXIS_START.finditer(body))
    if body and (not starts or starts[0].start() != 0): raise ValueError('unsupported input axis layout')
    kept = []
    for index, match in enumerate(starts):
        end = starts[index + 1].start() if index + 1 < len(starts) else len(body)
        block = body[match.start():end]
        fields = FIELD.findall(block)
        if len(fields) != len(FIELDS) or set(k for k, _ in fields) != set(FIELDS): raise ValueError('unsupported input axis fields')
        if dict(fields)['m_Name'] not in NAMES: kept.append(block)
    generated = ['  - serializedVersion: 3\n' + ''.join('    ' + field + ':' + (' ' + row[field] if row[field] else '') + '\n' for field in FIELDS) for row in rows]
    return prefix + marker + ''.join(kept) + ''.join(generated) + suffix


def verify(project):
    path = project / 'ProjectSettings/InputManager.asset'
    if not path.is_file() or path.is_symlink(): raise ValueError('InputManager.asset missing')
    text = path.read_text(encoding='utf-8')
    if render(text, definitions()) != text: raise ValueError('Android input axes are missing or differ; run prepare-android-input.py')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', required=True, type=Path)
    p.add_argument('--backup', type=Path, help='new backup file outside the workspace')
    p.add_argument('--check', action='store_true')
    a = p.parse_args(); project = a.project.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER: p.error('project must be an isolated workspace')
    if (project / 'Temp/UnityLockfile').exists(): p.error('Unity workspace is locked')
    path = project / 'ProjectSettings/InputManager.asset'
    if not path.is_file() or path.is_symlink(): p.error('InputManager.asset missing')
    original = path.read_text(encoding='utf-8'); updated = render(original, definitions())
    if a.check:
        if updated != original: p.error('Android input axes are missing or differ')
        print('Android input axes match the tested configuration.'); return
    if updated == original:
        print('Android input axes already match the tested configuration.'); return
    if not a.backup: p.error('a new backup file is required')
    backup = a.backup.expanduser().absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir(): p.error('backup must be a new file outside the workspace')
    backup = backup.parent.resolve() / backup.name
    if backup.is_relative_to(project): p.error('backup must be a new file outside the workspace')
    with backup.open('x', encoding='utf-8', newline='') as f: f.write(original)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile('w', encoding='utf-8', newline='', dir=path.parent, prefix='.openbonk-input-', delete=False) as f:
            temporary = Path(f.name); f.write(updated); f.flush(); os.fsync(f.fileno())
        os.chmod(temporary, path.stat().st_mode & 0o777)
        os.replace(temporary, path); temporary = None
    finally:
        if temporary is not None: temporary.unlink(missing_ok=True)
    print('Prepared Android input axes; the previous InputManager remains in the local backup.')


if __name__ == '__main__': main()
