#!/usr/bin/env python3
"""Build a local experimental Android APK with Rewired excluded.

The APK and source still contain game, Unity and other third-party material.
This is not a release build or a distributable package. It never installs to a
device. A separate log is retained beside the output, including on failure.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import signal
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / 'reconstruction/AssetRipperExport/ExportedProject'
EDITOR = Path.home() / 'Unity/Hub/Editor/2023.2.22f1/Editor/Unity'
PLAYER = EDITOR.parent / 'Data/PlaybackEngines/AndroidPlayer'
BUILD_APK = ROOT / 'build/android/Megabonk-arm64-launcher-test.apk'
SPLIT_APK = ROOT / 'build/android/Megabonk-arm64-split-test.apk'
SPLIT_OBB = ROOT / 'build/android/Megabonk-arm64-split-test.main.obb'
RUNTIME_APK = ROOT / 'build/android/Megabonk-arm64-runtime-research.apk'
RUNTIME_OBB = ROOT / 'build/android/Megabonk-arm64-runtime-research.main.obb'
RSP = PROJECT / 'Assets/csc.rsp'
OWNED_RSP = b'-define:OPENBONK_NO_REWIRED\n'
TRACKED = [PROJECT / 'ProjectSettings/ProjectSettings.asset', PROJECT / 'Assets/AndroidSplitLauncher/SplitExpectedData.java',
           *(PROJECT / 'Assets/Plugins' / n for n in ('Rewired_Core.dll', 'Rewired_Core.dll.meta', 'Rewired_Linux.dll.meta',
                'Coffee.UIParticle.dll', 'com.rlabrecque.steamworks.net.dll'))]

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()

def other_unity_for_project():
    for proc in Path('/proc').iterdir():
        if not proc.name.isdigit() or int(proc.name) == os.getpid(): continue
        try:
            args = (proc / 'cmdline').read_bytes().split(b'\0')
            if any(b'/Editor/Unity' in x for x in args) and str(PROJECT).encode() in args: return True
        except (OSError, PermissionError): pass
    return False

def changed_files(snapshots):
    changed = []
    for path, before in snapshots.items():
        try:
            if not path.is_file() or path.read_bytes() != before: changed.append(str(path))
        except OSError:
            changed.append(str(path))
    return changed

def run_child(cmd, env, stdout=None):
    proc = subprocess.Popen(cmd, env=env, stdout=stdout, stderr=subprocess.STDOUT if stdout else None, start_new_session=True)
    try:
        return proc.wait()
    except BaseException:
        if proc.poll() is None:
            try: os.killpg(proc.pid, signal.SIGTERM)
            except ProcessLookupError: pass
            try: proc.wait(timeout=30)
            except subprocess.TimeoutExpired:
                try: os.killpg(proc.pid, signal.SIGKILL)
                except ProcessLookupError: pass
                proc.wait()
        raise

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--output', type=Path, default=ROOT / 'build/android/OpenBonk-no-rewired-local.apk', help='new APK path')
    p.add_argument('--check', action='store_true', help='check prerequisites without building')
    p.add_argument('--split', action='store_true', help='build a diagnostic APK and OBB pair')
    p.add_argument('--runtime-research', action='store_true', help='build a single-pass research runtime and matching OBB')
    a = p.parse_args(); out = a.output.expanduser().absolute(); log = out.with_suffix('.build.log')
    if a.split and a.runtime_research: p.error('--split and --runtime-research cannot be combined')
    if out.suffix.lower() != '.apk': p.error('output must have an .apk extension')
    has_obb = a.split or a.runtime_research
    out_obb = out.with_suffix('.main.obb') if has_obb else None
    build_apk = RUNTIME_APK if a.runtime_research else SPLIT_APK if a.split else BUILD_APK
    build_obb = RUNTIME_OBB if a.runtime_research else SPLIT_OBB
    destinations = [out, log] + ([out_obb] if out_obb else [])
    if any(path.exists() or path.is_symlink() for path in destinations) or not out.parent.is_dir(): p.error('APK, OBB and log destinations must be new and output parent must exist')
    if RSP.exists() or RSP.is_symlink() or Path(str(RSP) + '.meta').exists() or Path(str(RSP) + '.meta').is_symlink(): p.error('Assets/csc.rsp or its metadata already exists; preserve it and use a separate checkout')
    build_outputs = [build_apk] + ([build_obb] if has_obb else [])
    if any(path.is_symlink() or (path.exists() and not path.is_file()) for path in build_outputs): p.error('a Unity build output is not a regular file; use a separate checkout')
    if other_unity_for_project(): p.error('Unity is already running this project')
    for path in [EDITOR, PLAYER / 'SDK/build-tools/34.0.0/apksigner', PLAYER / 'OpenJDK/bin/java', *TRACKED]:
        if not path.is_file(): p.error('missing prerequisite: ' + str(path))
    if not shutil.which('steam-run'): p.error('steam-run is not in PATH')
    if a.check:
        print('Prerequisites found; this is a local diagnostic, not a distributable APK.')
        return
    work = Path(tempfile.mkdtemp(prefix='.openbonk-rewired-', dir=out.parent))
    staged = work / 'probe.apk'
    backups = [(path, work / f'previous-{i}', path.is_file()) for i, path in enumerate(build_outputs)]
    try:
        snapshots = {path: path.read_bytes() for path in TRACKED}
        for i, path in enumerate(TRACKED): (work / f'input-{i}').write_bytes(snapshots[path])
        (work / 'recovery-manifest.json').write_text(json.dumps([{'path': str(path), 'backup': f'input-{i}', 'sha256': hashlib.sha256(snapshots[path]).hexdigest()} for i, path in enumerate(TRACKED)], indent=2) + '\n')
        for path, backup, existed in backups:
            if existed: shutil.copy2(path, backup)
    except BaseException:
        shutil.rmtree(work)
        raise
    fd = None; created_rsp = False; preserve = False; success = False; launched = False; rsp = OWNED_RSP
    previous_term = signal.getsignal(signal.SIGTERM)
    def terminate(signum, frame): raise InterruptedError('terminated by signal')
    signal.signal(signal.SIGTERM, terminate)
    try:
        with log.open('x'): pass
        probe_option = os.environ.get('OPENBONK_BETA_NO_PROBES', '0')
        if probe_option not in ('0', '1'): raise ValueError('invalid OPENBONK_BETA_NO_PROBES')
        rsp = OWNED_RSP + b'-define:OPENBONK_BETA_NO_PROBES\n' if probe_option == '1' else OWNED_RSP
        fd = os.open(RSP, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600); created_rsp = True
        with os.fdopen(fd, 'wb') as f: fd = None; f.write(rsp)
        env = os.environ.copy(); env['OPENBONK_NO_REWIRED_CODE'] = '1'; env['OPENBONK_NO_REWIRED_PLUGINS'] = '1'
        if env.get('OPENBONK_ANDROID_TARGET_SDK') == '35':
            # Unity's bundled AGP uses an older aapt2 that cannot read the
            # Android 35 resource table. Use the installed build-tools copy.
            aapt2 = PLAYER / 'SDK/build-tools/34.0.0/aapt2'
            if not aapt2.is_file() or any(c.isspace() for c in str(aapt2)):
                raise RuntimeError('Android 35 requires the compatible aapt2 at a path without spaces')
            env['OPENBONK_AAPT2_OVERRIDE'] = str(aapt2)
        if a.split: env['OPENBONK_SPLIT_DIAGNOSTIC_DIR'] = str(work)
        env['LD_LIBRARY_PATH'] = '/nix/store/5f5fnvixyi4zlkp122ypzkmjnvphi4bs-libxml2-2.13.9/lib' + (':' + env['LD_LIBRARY_PATH'] if env.get('LD_LIBRARY_PATH') else '')
        cmd = ['steam-run', str(EDITOR), '-batchmode', '-nographics', '-quit', '-projectPath', str(PROJECT),
               '-executeMethod', 'NativeAndroidBuild.BuildArm64RuntimeResearchApk' if a.runtime_research else 'NativeAndroidBuild.BuildArm64SplitDataProbeApk' if a.split else 'NativeAndroidBuild.BuildArm64EmbeddedLauncherApk', '-logFile', str(log)]
        print('Building local diagnostic; log:', log, flush=True)
        launched = True
        if a.runtime_research:
            for path, backup, existed in backups:
                if existed: path.unlink()
        status = run_child(cmd, env)
        text = log.read_text(errors='replace')
        if a.split and 'Split data changed between build passes' in text:
            preserve = True
            print('Split OBBs differ; diagnostic copies retained in', work, flush=True)
        if status or re.search(r'error CS\d+|Tundra build failed|Script compilation failed|Addressables build failed|Error building Player', text, re.IGNORECASE) or 'Build Finished, Result: Success.' not in text:
            raise RuntimeError('Unity failed or reported compilation errors; inspect ' + str(log))
        for needle in ('Rewired plugin probe: requested build exclusion for Assets/Plugins/Rewired_Core.dll',
                       'Rewired plugin probe: requested build exclusion for Assets/Plugins/Rewired_Linux.dll',
                       'Rewired scene probe: removed Rewired manager and store; retained Android input and vibration on MainMenu'):
            if needle not in text: raise RuntimeError('expected build stage not observed: ' + needle)
        if not build_apk.is_file(): raise RuntimeError('Unity did not create an APK')
        if a.split:
            if not SPLIT_OBB.is_file() or ('Split-data expected SHA-256: ' + sha(SPLIT_OBB)) not in text:
                raise RuntimeError('Unity did not produce the verified split OBB')
            with zipfile.ZipFile(SPLIT_OBB) as z:
                if z.testzip() is not None or len(z.namelist()) != len(set(z.namelist())): raise RuntimeError('invalid OBB archive')
        if a.runtime_research:
            if not build_obb.is_file(): raise RuntimeError('Unity did not produce an OBB')
            with zipfile.ZipFile(build_obb) as z:
                if z.testzip() is not None or len(z.namelist()) != len(set(z.namelist())): raise RuntimeError('invalid OBB archive')
                obb_guid = z.read('unity_obb_guid')
        with zipfile.ZipFile(build_apk) as z:
            if z.testzip() is not None or len(z.namelist()) != len(set(z.namelist())): raise RuntimeError('invalid APK archive')
            if a.split and sha(SPLIT_OBB).encode('ascii') not in z.read('classes.dex'): raise RuntimeError('APK does not reference the split OBB hash')
            if a.runtime_research and (len(obb_guid) != 36 or z.read('assets/unity_obb_guid') != obb_guid): raise RuntimeError('runtime and OBB GUIDs differ')
            names = json.loads(z.read('assets/bin/Data/ScriptingAssemblies.json'))['names']
            metadata = z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
            native = z.read('lib/arm64-v8a/libil2cpp.so')
            if any('rewired' in n.lower() for n in names) or any('rewired' in n.lower() for n in z.namelist()) or b'Rewired_Core.dll' in metadata or b'Rewired_Core' in native:
                raise RuntimeError('Rewired assembly or expected literal still present')
        verify_env = env.copy(); verify_env['JAVA_HOME'] = str(PLAYER / 'OpenJDK'); verify_env['PATH'] = str(PLAYER / 'OpenJDK/bin') + os.pathsep + env.get('PATH', '')
        with log.open('a') as f:
            if run_child(['steam-run', str(PLAYER / 'SDK/build-tools/34.0.0/apksigner'), 'verify', '--verbose', str(build_apk)], verify_env, f):
                raise RuntimeError('APK signature verification failed')
        changed = changed_files(snapshots)
        if changed:
            preserve = True
            raise RuntimeError('project files changed during build; recovery copies retained in ' + str(work) + ': ' + ', '.join(changed))
        shutil.copyfile(build_apk, staged)
        if has_obb: shutil.copyfile(build_obb, work / 'probe.obb')
        # Exclusive hard link prevents overwriting a destination created during the build.
        os.link(staged, out)
        if has_obb:
            try: os.link(work / 'probe.obb', out_obb)
            except BaseException:
                out.unlink()
                raise
        success = True
        print('Local diagnostic:', out, 'SHA-256', sha(out), flush=True)
        if has_obb: print('Local diagnostic OBB:', out_obb, 'SHA-256', sha(out_obb), flush=True)
        print('No device modified. Other game data and dependencies remain; do not distribute.', flush=True)
    finally:
        signal.signal(signal.SIGTERM, previous_term)
        if fd is not None: os.close(fd)
        if created_rsp:
            try: response_changed = RSP.exists() and RSP.read_bytes() != rsp
            except OSError: response_changed = True
            if response_changed:
                preserve = True
                print('WARNING: csc.rsp changed externally; leaving it and recovery files in', work, flush=True)
            else:
                try: RSP.unlink(missing_ok=True); Path(str(RSP) + '.meta').unlink(missing_ok=True)
                except OSError:
                    preserve = True
                    print('WARNING: could not remove temporary response files; recovery copies retained in', work, flush=True)
        changed = changed_files(snapshots)
        if changed:
            preserve = True
            print('WARNING: project files changed; numbered recovery copies retained in', work, 'for', ', '.join(changed), flush=True)
        if launched:
            for path, backup, existed in backups:
                if existed and backup.exists(): os.replace(backup, path)
                elif not existed and path.exists(): path.unlink()
        if not preserve: shutil.rmtree(work)
        if not success: print('Build did not publish an APK; see', log, flush=True)

if __name__ == '__main__': main()
