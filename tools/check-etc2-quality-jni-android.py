#!/usr/bin/env python3
"""Compare experimental ETC2 JNI outputs on host and an authorized Android device."""
import argparse
from pathlib import Path
import re
import secrets
import shlex
import shutil
import struct
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
JAVA = ROOT / 'tools/texture-native-comparison/com/openbonk/dataprep'


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('host-library', 'android-library', 'fixtures', 'sdk', 'java-home'):
        p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--device', required=True); p.add_argument('--runner')
    a = p.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9._:-]+', a.device): p.error('invalid device serial')
    for name in ('host_library', 'android_library', 'fixtures', 'sdk', 'java_home'):
        value = getattr(a, name).expanduser()
        if value.is_symlink(): p.error(name + ' must not be a symbolic link')
        setattr(a, name, value.resolve(strict=True))
    for file in (a.host_library, a.android_library, a.fixtures):
        if not file.is_file(): p.error('library or fixture missing')
    if not 12 <= a.fixtures.stat().st_size <= 256 * 1024 * 1024: p.error('invalid fixture size')
    with a.fixtures.open('rb') as stream: header = stream.read(12)
    count = struct.unpack('>I', header[8:])[0]
    if header[:8] != b'OBHQ0001' or not 0 < count <= 1000: p.error('invalid fixture header')
    java, javac, d8 = a.java_home / 'bin/java', a.java_home / 'bin/javac', a.sdk / 'build-tools/34.0.0/lib/d8.jar'
    sources = [JAVA / 'SteamConvertedTextures.java', JAVA / 'SteamQualityTextureCheck.java']
    if any(not path.is_file() for path in (java, javac, d8, *sources)): p.error('Java tools or sources missing')
    adb = shutil.which('adb'); runner = shutil.which(a.runner) if a.runner else None
    if not adb or (a.runner and not runner): p.error('adb or runner missing')
    adb_cmd = [adb, '-s', a.device]; prefix = [runner] if runner else []
    state = subprocess.run(adb_cmd + ['get-state'], capture_output=True, text=True)
    if state.returncode or state.stdout.strip() != 'device': p.error('device is not connected and authorized')
    remote = '/data/local/tmp/openbonk-quality-jni-' + secrets.token_hex(12)
    with tempfile.TemporaryDirectory(prefix='.openbonk-quality-jni-', dir=a.fixtures.parent) as temp:
        root = Path(temp); classes = root / 'classes'; dex = root / 'dex'; classes.mkdir(); dex.mkdir()
        subprocess.run(prefix + [str(javac), '--release', '8', '-d', str(classes), *map(str, sources)], check=True)
        class_files = sorted(classes.rglob('*.class'))
        subprocess.run(prefix + [str(java), '-cp', str(d8), 'com.android.tools.r8.D8', '--min-api', '26', '--output', str(dex), *map(str, class_files)], check=True)
        host = subprocess.run(prefix + [str(java), '-cp', str(classes), 'com.openbonk.dataprep.SteamQualityTextureCheck', str(a.host_library), str(a.fixtures)], check=True, capture_output=True, text=True)
        subprocess.run(adb_cmd + ['shell', 'mkdir', remote], check=True, capture_output=True)
        try:
            for local, name in ((dex / 'classes.dex', 'classes.dex'), (a.android_library, 'library.so'), (a.fixtures, 'fixtures.bin')):
                subprocess.run(adb_cmd + ['push', str(local), remote + '/' + name], check=True, capture_output=True)
            cmd = ['env', 'CLASSPATH=' + remote + '/classes.dex', 'app_process', '/system/bin',
                   'com.openbonk.dataprep.SteamQualityTextureCheck', remote + '/library.so', remote + '/fixtures.bin']
            device = subprocess.run(adb_cmd + ['shell', shlex.join(cmd)], check=True, capture_output=True, text=True)
            if host.stdout != device.stdout or not re.fullmatch(r'PASS ' + str(count) + r' [0-9a-f]{64}\n', host.stdout):
                raise ValueError('host and Android ETC2 JNI outputs differ')
            print(f'Host and Android JNI output matched for all {count} fixtures.', flush=True)
        finally:
            if subprocess.run(adb_cmd + ['shell', 'rm', '-rf', remote], capture_output=True).returncode:
                print('Could not remove device temporary directory:', remote, flush=True)


if __name__ == '__main__': main()
