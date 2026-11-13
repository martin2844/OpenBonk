#!/usr/bin/env python3
"""Compare host and Android ETC2 probe output for private RGBA fixtures."""
import argparse
import json
from pathlib import Path
import re
import secrets
import shutil
import subprocess
import tempfile


def fixture_list(root):
    rows = json.loads((root / 'fixtures.json').read_text(encoding='utf-8'))
    if not isinstance(rows, list) or not 0 < len(rows) <= 1000: raise ValueError('invalid fixture list')
    result = []; seen = set(); total = 0
    for row in rows:
        if not isinstance(row, list) or len(row) != 3: raise ValueError('invalid fixture')
        name, width, height = row
        if (not isinstance(name, str) or not re.fullmatch(r'[A-Za-z0-9_-]{1,80}', name) or name in seen or
                type(width) is not int or type(height) is not int or not 1 <= width <= 4096 or not 1 <= height <= 4096):
            raise ValueError('invalid fixture name or dimensions')
        path = root / (name + '.rgba'); size = width * height * 4; total += size
        if path.is_symlink() or not path.is_file() or path.stat().st_size != size or total > 256 * 1024 * 1024:
            raise ValueError('invalid fixture data')
        result.append((name, width, height, path)); seen.add(name)
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('host', 'android', 'fixtures'): p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--device', required=True); p.add_argument('--runner')
    a = p.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9._:-]+', a.device): p.error('invalid device serial')
    for name in ('host', 'android', 'fixtures'):
        value = getattr(a, name).expanduser()
        if value.is_symlink(): p.error(name + ' must not be a symbolic link')
        setattr(a, name, value.resolve(strict=True))
    if not a.host.is_file() or not a.android.is_file() or not a.fixtures.is_dir(): p.error('probe or fixture inputs missing')
    try: fixtures = fixture_list(a.fixtures)
    except (OSError, ValueError, json.JSONDecodeError) as error: p.error(str(error))
    adb, runner = shutil.which('adb'), shutil.which(a.runner) if a.runner else None
    if not adb or (a.runner and not runner): p.error('adb or runner missing')
    adb_cmd = [adb, '-s', a.device]
    state = subprocess.run(adb_cmd + ['get-state'], capture_output=True, text=True)
    if state.returncode or state.stdout.strip() != 'device': p.error('device is not connected and authorized')
    remote = '/data/local/tmp/openbonk-etc2-probe-' + secrets.token_hex(12)
    def command(*args): subprocess.run(adb_cmd + list(args), check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    with tempfile.TemporaryDirectory(prefix='.openbonk-etc2-check-', dir=a.fixtures.parent) as temp:
        temp = Path(temp); command('shell', 'mkdir', remote)
        try:
            command('push', str(a.android), remote + '/probe'); command('shell', 'chmod', '700', remote + '/probe')
            for name, width, height, path in fixtures:
                host, android = temp / 'host.bin', temp / 'android.bin'
                subprocess.run(([runner] if runner else []) + [str(a.host), str(path), str(host), str(width), str(height)], check=True)
                command('push', str(path), remote + '/input.rgba')
                command('shell', remote + '/probe', remote + '/input.rgba', remote + '/output.bin', str(width), str(height))
                command('pull', remote + '/output.bin', str(android))
                if host.read_bytes() != android.read_bytes(): raise ValueError('ETC2 output differs for fixture: ' + name)
            print(f'Host and Android output matched for all {len(fixtures)} fixtures.', flush=True)
        finally:
            if subprocess.run(adb_cmd + ['shell', 'rm', '-rf', remote], capture_output=True).returncode:
                print('Could not remove device temporary directory:', remote, flush=True)


if __name__ == '__main__': main()
