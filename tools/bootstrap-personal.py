#!/usr/bin/env python3
"""Prepare one private local OpenBonk diagnostic installation on Linux.

Requires CPython 3.13, Java/keytool, a C++ compiler, network access and the
pinned research inputs in this workspace. The result contains game and
third-party material and is not a public installer or distribution package.
It installs on Android only when explicitly requested and never modifies Steam.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def verify_local_inputs(compile_shaders=False, unity_editor=None):
    current = load('bootstrap_current', ROOT/'tools/build-current-runtime-diagnostic.py')
    for path, expected in current.PINS.items():
        if not path.is_file(): raise ValueError('falta un archivo local: '+str(path))
        with path.open('rb') as f:
            if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                raise ValueError('archivo local incompatible: '+str(path))
    current.checked_manifest_root()
    cache = None
    if compile_shaders:
        editor = Path(unity_editor or os.environ.get('OPENBONK_UNITY_EDITOR', str(Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor'))).expanduser().resolve()
        project = ROOT/'unity/OpenBonk'
        if not (editor/'Unity').is_file() or not (project/'ProjectSettings/GraphicsSettings.asset').is_file() or not (ROOT/'analysis/port-inventory/OpenBonkComputeStudy.cs').is_file():
            raise ValueError('faltan el editor Unity o el proyecto y fuentes de shaders')
        builder = load('bootstrap_shader_builder', ROOT/'tools/build-compute-shaders-local.py')
        if (builder.TEMP_SCRIPT.exists() or builder.TEMP_SCRIPT.is_symlink() or
                builder.META.exists() or builder.META.is_symlink()): raise ValueError('ya existe el archivo temporal de shaders')
        if builder.FEATURE_TEMP.exists() or builder.FEATURE_TEMP.is_symlink() or Path(str(builder.FEATURE_TEMP)+'.meta').exists() or Path(str(builder.FEATURE_TEMP)+'.meta').is_symlink():
            raise ValueError('ya existe la carpeta temporal de materiales de shaders')
        if builder.BUILTIN_TEMP.exists() or builder.BUILTIN_TEMP.is_symlink() or Path(str(builder.BUILTIN_TEMP)+'.meta').exists() or Path(str(builder.BUILTIN_TEMP)+'.meta').is_symlink():
            raise ValueError('ya existe la carpeta temporal de materiales integrados')
        if builder.unity_in_use(): raise ValueError('Unity ya está usando este proyecto')
    else:
        shaders = load('bootstrap_shaders', ROOT/'tools/pinned-shader-bundles.py')
        cache = Path(os.environ.get('OPENBONK_SHADER_BUNDLES', str(ROOT/'build/android/pinned-shader-cache-test'))).expanduser().resolve()
        shaders.verify(cache)
    decoder = load('bootstrap_decoder', ROOT/'tools/build-crunch-decoder.py')
    decoder.check()
    if not (ROOT/'tools/requirements-conversion.txt').is_file(): raise ValueError('falta la lista de dependencias Python')
    return cache


def java_home(explicit):
    value = explicit or os.environ.get('OPENBONK_JAVA_HOME') or os.environ.get('JAVA_HOME')
    if value:
        home = Path(value).expanduser().resolve()
    else:
        java = shutil.which('java')
        if not java: raise ValueError('no se encontró Java; indicá --java-home')
        home = Path(java).resolve().parent.parent
    for name in ('java', 'keytool'):
        if not (home/'bin'/name).is_file(): raise ValueError('falta '+name+' en '+str(home))
    return home


def main():
    p = argparse.ArgumentParser(description=__doc__)
    source = p.add_mutually_exclusive_group()
    source.add_argument('--steam-data', type=Path, help='compatible Linux or Windows Megabonk_Data directory')
    source.add_argument('--steam-zip', type=Path, help='ZIP containing compatible Linux or Windows Steam data')
    p.add_argument('--work-dir', required=True, type=Path, help='new directory for tools, local key and result')
    p.add_argument('--java-home', help='JDK/JRE directory containing java and keytool')
    p.add_argument('--keystore', type=Path, help='existing Android debug keystore; otherwise create a new one')
    p.add_argument('--compile-shaders', action='store_true', help='build shaders using the local Unity project instead of the pinned shader cache')
    p.add_argument('--install', action='store_true', help='after preparation, install on the connected Android device using adb')
    p.add_argument('--serial', help='adb device serial when using --install')
    p.add_argument('--adb', default='adb', help='adb executable when using --install')
    profile = p.add_mutually_exclusive_group()
    profile.add_argument('--profile-dir', type=Path, help='optional folder with Steam profile files (requires --install)')
    profile.add_argument('--profile-from-preview', action='store_true', help='copy Preview profile from device (requires --install)')
    a = p.parse_args()
    if not a.install and (a.serial or a.adb != 'adb' or a.profile_dir or a.profile_from_preview):
        p.error('--serial, --adb and profile options require --install')
    work = a.work_dir.expanduser().absolute()
    if work.exists() or work.is_symlink() or not work.parent.is_dir(): p.error('work-dir must be new and its parent must exist')
    if ROOT.resolve().is_relative_to(work.resolve()): p.error('work-dir must not contain the project')
    if sys.implementation.name != 'cpython' or sys.version_info[:2] != (3, 13): p.error('CPython 3.13 is required for the tested dependencies')
    prep = load('bootstrap_preparation', ROOT/'tools/prepare-personal-install.py')
    try:
        if a.install:
            adb = shutil.which(a.adb)
            if not adb: raise ValueError('no se encuentra adb')
            installer = load('bootstrap_installer', ROOT/'tools/install-personal-adb.py')
            serial = installer.select_device(adb, a.serial)
            if not a.keystore and installer.has_previous_install(adb, serial):
                raise ValueError('OpenBonk ya está instalado; indicá --keystore con la clave usada para esa instalación antes de reconstruir. No se desinstaló ni se modificó la aplicación.')
            if a.profile_dir:
                profile_dir = a.profile_dir.expanduser().resolve(strict=True)
                if not profile_dir.is_dir() or any(not (profile_dir/name).is_file() for name in ('progression.json', 'stats.json')):
                    raise ValueError('la carpeta de perfil debe contener progression.json y stats.json')
        cache = verify_local_inputs(a.compile_shaders); java = java_home(a.java_home)
        if a.steam_zip:
            src = a.steam_zip.expanduser().resolve(strict=True)
            if not src.is_file() or not 0 < src.stat().st_size <= 1024*1024*1024: raise ValueError('ZIP de Steam no válido')
            if prep.is_windows_steam_zip(src):
                prep.check_windows_launcher_inputs()
                with tempfile.TemporaryDirectory(prefix='.openbonk-bootstrap-check-') as tmp:
                    prep.extract_windows_steam_zip(src, Path(tmp))
            else:
                load('bootstrap_bundle', ROOT/'tools/prepare-android-data-bundle.py').verify_steam_zip(src)
            source_args = ['--steam-zip', str(src)]
        else:
            if a.steam_data:
                src = a.steam_data.expanduser().resolve(strict=True); prep.check_steam_directory(src)
                if (src/'StreamingAssets/aa/StandaloneWindows64').is_dir() and not (src/'StreamingAssets/aa/StandaloneLinux64').is_dir():
                    prep.check_windows_launcher_inputs()
            else:
                finder = load('bootstrap_steam', ROOT/'tools/find-local-steam-data.py')
                src = finder.discover(finder.default_roots(), prep.check_steam_directory)
            source_args = ['--steam-data', str(src)]
        if work.resolve().is_relative_to(src) or src.is_relative_to(work.resolve()): raise ValueError('work-dir debe estar separado de Steam')
        existing_key = a.keystore.expanduser().resolve(strict=True) if a.keystore else None
        if existing_key and not existing_key.is_file(): raise ValueError('la clave indicada no es un archivo')
    except (ValueError, OSError, zipfile.BadZipFile, subprocess.CalledProcessError) as exc:
        p.error(str(exc))
    work.mkdir(mode=0o700)
    try:
        venv = work/'python'; sdk = work/'android-sdk'; sdk.mkdir(mode=0o700)
        key = existing_key or work/'debug.keystore'; result = work/'result'
        print('Creando entorno Python privado…', flush=True)
        subprocess.run([sys.executable, '-m', 'venv', str(venv)], check=True)
        python = venv/'bin/python'
        subprocess.run([str(python), '-m', 'pip', 'install', '-r', str(ROOT/'tools/requirements-conversion.txt')], check=True)
        subprocess.run([str(python), str(ROOT/'tools/install-android-build-tools-local.py'), str(sdk)], check=True)
        if not existing_key:
            subprocess.run([str(java/'bin/keytool'), '-genkeypair', '-keystore', str(key),
                            '-storepass', 'android', '-keypass', 'android', '-alias', 'androiddebugkey',
                            '-dname', 'CN=OpenBonk Local Debug,O=OpenBonk,C=US', '-keyalg', 'RSA',
                            '-keysize', '2048', '-validity', '3650', '-noprompt'], check=True)
        env = os.environ.copy()
        env.update({'OPENBONK_ANDROID_SDK_ROOT':str(sdk), 'OPENBONK_JAVA_HOME':str(java),
                    'OPENBONK_DEBUG_KEYSTORE':str(key)})
        env.setdefault('OPENBONK_TOOL_RUNNER', 'none')
        if cache is None: env.pop('OPENBONK_SHADER_BUNDLES', None)
        else: env['OPENBONK_SHADER_BUNDLES'] = str(cache)
        command = [str(python), str(ROOT/'tools/prepare-personal-install.py'), '--compile-decoder', *source_args]
        subprocess.run([*command, '--check'], check=True, env=env)
        subprocess.run([*command, '--output', str(result)], check=True, env=env)
        receipt = {'kind':'local research only, not for distribution', 'result':'result/READY.json',
                   'debug_key':str(key), 'steam_source':str(src)}
        (work/'BOOTSTRAP_READY.json').write_text(json.dumps(receipt, indent=2)+'\n')
    except (OSError, ValueError, subprocess.CalledProcessError) as exc:
        p.error(f'falló la preparación; la carpeta parcial se conservó en {work}: {exc}')
    print('Preparación personal terminada:', result, flush=True)
    if a.install:
        install = [sys.executable, str(ROOT/'tools/install-personal-adb.py'), str(result), '--adb', adb, '--serial', serial]
        if a.profile_dir: install += ['--profile-dir', str(profile_dir)]
        if a.profile_from_preview: install += ['--profile-from-preview']
        try:
            subprocess.run(install, check=True)
        except (OSError, subprocess.CalledProcessError) as exc:
            p.error(f'la preparación terminó, pero falló la instalación; podés reintentar con tools/install-personal-adb.py {result}: {exc}')
    else:
        print('La APK y los datos son privados y no se instalaron en ningún dispositivo.', flush=True)


if __name__ == '__main__': main()
