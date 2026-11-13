#!/usr/bin/env python3
"""Compile two local shaders for private comparison, without changing the project."""
import os
from pathlib import Path
import argparse
import shutil
import subprocess
import sys

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--package-probuilder', action='store_true', help='compile the installed Unity ProBuilder package shader')
parser.add_argument('--keep-instancing', action='store_true', help='include instancing variants')
parser.add_argument('--fog-exp2', action='store_true', help='keep exponential squared fog variants')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
project = root / 'unity/OpenBonk'
source = root / 'analysis/port-inventory/OpenBonkStandaloneShaderStudy.cs'
temporary = project / 'Assets/Editor/OpenBonkStandaloneShaderStudy.cs'
meta = Path(str(temporary) + '.meta')
editor = Path.home() / 'Unity/Hub/Editor/2023.2.22f1/Editor/Unity'
output = root / ('build/android/standalone-package-fog-study' if args.fog_exp2
                 else 'build/android/standalone-package-instancing-study' if args.keep_instancing
                 else 'build/android/standalone-package-shader-study' if args.package_probuilder
                 else 'build/android/standalone-shader-regeneration-study')
if args.keep_instancing and not args.package_probuilder:
    parser.error('--keep-instancing requires --package-probuilder')
if args.fog_exp2 and (not args.package_probuilder or args.keep_instancing):
    parser.error('--fog-exp2 requires --package-probuilder without --keep-instancing')
if any(p.exists() or p.is_symlink() for p in (temporary, meta, output)):
    sys.exit('Study paths already exist')
if not editor.is_file() or not shutil.which('steam-run'):
    sys.exit('Unity or steam-run unavailable')
for proc in Path('/proc').iterdir():
    if not proc.name.isdigit() or int(proc.name) == os.getpid():
        continue
    try:
        proc_args = (proc / 'cmdline').read_bytes().split(b'\0')
        if (str(project).encode() in proc_args and any(
                os.path.realpath(os.fsdecode(arg)) == str(editor.resolve()) for arg in proc_args if arg)):
            sys.exit('Unity is already running this project')
    except (OSError, PermissionError):
        pass
contents = source.read_bytes()
graphics = project / 'ProjectSettings/GraphicsSettings.asset'
graphics_before = graphics.read_bytes()
output.mkdir(mode=0o700)
with temporary.open('xb') as f:
    f.write(contents)
try:
    env = os.environ.copy()
    env['OPENBONK_STANDALONE_SHADER_OUT'] = str(output)
    env['OPENBONK_PACKAGE_PROBUILDER'] = '1' if args.package_probuilder else '0'
    env['OPENBONK_KEEP_INSTANCING'] = '1' if args.keep_instancing else '0'
    env['OPENBONK_FOG_EXP2'] = '1' if args.fog_exp2 else '0'
    xml = Path('/nix/store/5f5fnvixyi4zlkp122ypzkmjnvphi4bs-libxml2-2.13.9/lib')
    if xml.is_dir():
        env['LD_LIBRARY_PATH'] = str(xml) + (':' + env['LD_LIBRARY_PATH'] if env.get('LD_LIBRARY_PATH') else '')
    proc = subprocess.run(['steam-run', str(editor), '-batchmode', '-nographics', '-quit',
                           '-projectPath', str(project), '-executeMethod',
                           'OpenBonkStandaloneShaderStudy.Build', '-logFile', str(output / 'unity.log')], env=env)
    log = (output / 'unity.log').read_text(errors='replace') if (output / 'unity.log').exists() else ''
    if proc.returncode or 'OPENBONK_STANDALONE_SHADER_STUDY_OK' not in log:
        raise RuntimeError('Unity shader study failed; see log')
finally:
    if temporary.is_file() and temporary.read_bytes() == contents:
        temporary.unlink()
        meta.unlink(missing_ok=True)
    else:
        print('Temporary editor source changed; retained for inspection', file=sys.stderr)
    if graphics.read_bytes() != graphics_before:
        recovery = output / 'graphics-settings-before.asset'
        recovery.write_bytes(graphics_before)
        raise RuntimeError('Graphics settings changed; original saved at ' + str(recovery))
print('Built local shader bundle for private comparison.')
