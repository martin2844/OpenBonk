#!/usr/bin/env python3
"""Export a local Steam installation with AssetRipper 2.0.0.

Game-derived output stays local. This experimental export is only one input
for OpenBonk and does not create a working or redistributable APK by itself.
"""
import argparse
import http.client
import os
from pathlib import Path
import shutil
import signal
import socket
import subprocess
import time
from urllib.parse import urlencode

ROOT = Path(__file__).resolve().parent.parent


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game', required=True, type=Path, help='local Megabonk Steam installation')
    parser.add_argument('--assetripper', required=True, type=Path, help='AssetRipper 2.0.0 executable')
    parser.add_argument('--output', required=True, type=Path, help='new local export directory')
    parser.add_argument('--runner', help='optional executable used to launch AssetRipper, e.g. steam-run')
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    game = args.game.expanduser().resolve(strict=True)
    executable = args.assetripper.expanduser().resolve(strict=True)
    output = args.output.expanduser().absolute()
    if not game.is_dir() or not (game / 'Megabonk_Data/globalgamemanagers.assets').is_file():
        parser.error('game must contain Megabonk_Data/globalgamemanagers.assets')
    if not executable.is_file() or not os.access(executable, os.X_OK):
        parser.error('AssetRipper executable is not available')
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new path in an existing directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(path) or path.is_relative_to(output) for path in (game, ROOT)):
        parser.error('output must be separate from the game and source checkout')
    # AssetRipper clears its export directory, so the log must be a sibling.
    log = output.with_name(output.name + '.assetripper.log')
    if log.exists() or log.is_symlink():
        parser.error('the sibling AssetRipper log already exists')
    runner = shutil.which(args.runner) if args.runner else None
    if args.runner and not runner:
        parser.error('configured runner not found')
    prefix = ([runner] if runner else []) + [str(executable)]
    version = subprocess.run(prefix + ['--version'], capture_output=True, text=True, timeout=30)
    if version.returncode or not version.stdout.startswith('AssetRipper.GUI.Web 2.0.0+'):
        parser.error('AssetRipper.GUI.Web 2.0.0 is required')
    print('Local game and AssetRipper prerequisites found.', flush=True)
    if args.check:
        print('Check only; no files were written.')
        return
    output.mkdir(mode=0o700)
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.bind(('127.0.0.1', 0))
        port = sock.getsockname()[1]
    with log.open('x', encoding='utf-8') as log_file:
        process = subprocess.Popen(prefix + ['--headless', '--port', str(port), '--log=false'],
                                   stdout=log_file, stderr=subprocess.STDOUT, start_new_session=True)
        try:
            deadline = time.monotonic() + 60
            started = False
            while time.monotonic() < deadline:
                if process.poll() is not None:
                    raise RuntimeError('AssetRipper exited before starting')
                if f'Now listening on: http://127.0.0.1:{port}' in log.read_text(errors='replace'):
                    started = True
                    break
                time.sleep(0.2)
            if not started:
                raise RuntimeError('AssetRipper did not start its loopback server')
            def post(path, payload):
                conn = http.client.HTTPConnection('127.0.0.1', port, timeout=1800)
                try:
                    conn.request('POST', path, body=urlencode({'Path': str(payload)}),
                                 headers={'Content-Type': 'application/x-www-form-urlencoded'})
                    response = conn.getresponse()
                    response.read()
                    if response.status != 302:
                        raise RuntimeError(f'AssetRipper {path} returned HTTP {response.status}')
                finally:
                    conn.close()
            print('Reading local Steam data...', flush=True)
            post('/LoadFolder', game)
            print('Exporting the local Unity project...', flush=True)
            post('/Export/UnityProject', output)
            log_file.flush()
            text = log.read_text(errors='replace')
            if 'Finished post-export' not in text:
                raise RuntimeError('AssetRipper did not confirm completion')
            project = output / 'ExportedProject'
            if not all((project / item).exists() for item in ('Assets', 'Packages', 'ProjectSettings/ProjectVersion.txt')):
                raise RuntimeError('Exported Unity project is incomplete')
        finally:
            if process.poll() is None:
                os.killpg(process.pid, signal.SIGINT)
                try:
                    process.wait(timeout=15)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGTERM)
                    process.wait(timeout=15)
    print('Local Unity export completed. Do not publish the game-derived output.')


if __name__ == '__main__':
    main()
