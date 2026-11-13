#!/usr/bin/env python3
"""Prepare a local OpenBonk diagnostic APK and data bundle in one command.

Local research only: requires this workspace's private pinned templates,
Android build tools and a debug key. Outputs contain original game and
third-party material and are not a public installer or distributable release.
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
OBB = 'main.1.com.megabonk.nativeport.splittest.obb'
LOCAL_LIBS = [Path('/nix/store/wgw25d181h0qks7qsfp1bh7c5nd3f0s7-zlib-1.3.2/lib'),
              Path('/nix/store/0ba5kj6r0zhfwd84l5vzccqvx7amlq2c-gcc-15.3.0-lib/lib')]


def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def run(*args, env_overrides=None):
    print('+', ' '.join(str(a) for a in args), flush=True)
    env = os.environ.copy()
    if env_overrides: env.update(env_overrides)
    libs = [str(path) for path in LOCAL_LIBS if path.is_dir()]
    if env.get('LD_LIBRARY_PATH'): libs.append(env['LD_LIBRARY_PATH'])
    if libs: env['LD_LIBRARY_PATH'] = os.pathsep.join(libs)
    subprocess.run([str(a) for a in args], check=True, env=env)


def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def decoder_choice(force_compile):
    configured = os.environ.get('OPENBONK_CRUNCH_DECODER')
    existing = Path(configured).expanduser() if configured else ROOT/'build/tooling/unpack-unity-crunch-cubemap-unity'
    if not force_compile and existing.is_file():
        return existing.resolve()
    if not force_compile and configured:
        raise ValueError('no se encuentra el conversor configurado: '+str(existing))
    builder = load_module('crunch_builder', ROOT/'tools/build-crunch-decoder.py')
    builder.check()
    return None


def check_local_environment(force_compile=False):
    """Check core prerequisites; conversion still validates further templates."""
    current = load_module('current_runtime_preflight', ROOT/'tools/build-current-runtime-diagnostic.py')
    for path, expected in current.PINS.items():
        if not path.is_file() or sha(path) != expected:
            raise ValueError('falta una plantilla local o es incompatible: ' + str(path))
    current.checked_manifest_root()
    unity = Path(os.environ.get('OPENBONK_UNITY_EDITOR', str(Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor'))).expanduser()
    android = unity/'Data/PlaybackEngines/AndroidPlayer'
    sdk = Path(os.environ.get('OPENBONK_ANDROID_SDK_ROOT', str(android/'SDK'))).expanduser()
    java = Path(os.environ.get('OPENBONK_JAVA_HOME', str(android/'OpenJDK'))).expanduser()
    shader_cache = os.environ.get('OPENBONK_SHADER_BUNDLES')
    for path in (sdk/'build-tools/34.0.0/zipalign',
                 sdk/'build-tools/34.0.0/apksigner', java/'bin/java',
                 Path(os.environ.get('OPENBONK_DEBUG_KEYSTORE', str(Path.home()/'.android/debug.keystore'))).expanduser(),
                 ):
        if not path.is_file(): raise ValueError('falta una herramienta o archivo local: ' + str(path))
    decoder_choice(force_compile)
    runner = os.environ.get('OPENBONK_TOOL_RUNNER', 'steam-run' if shutil.which('steam-run') else 'none')
    if runner != 'none' and not shutil.which(runner): raise ValueError('no se encuentra el ejecutor configurado: ' + runner)
    if shader_cache:
        cached = load_module('shader_cache_preflight', ROOT/'tools/pinned-shader-bundles.py')
        cached.verify(Path(shader_cache).expanduser())
    else:
        if not (unity/'Unity').is_file() or not (ROOT/'analysis/port-inventory/OpenBonkComputeStudy.cs').is_file():
            raise ValueError('faltan el editor de Unity o las fuentes locales de shaders')
        shader_builder = load_module('shader_builder_preflight', ROOT/'tools/build-compute-shaders-local.py')
        if shader_builder.TEMP_SCRIPT.exists() or shader_builder.TEMP_SCRIPT.is_symlink() or shader_builder.META.exists() or shader_builder.META.is_symlink():
            raise ValueError('el archivo temporal de compilación de shaders ya existe; hay que revisarlo')
        if shader_builder.FEATURE_TEMP.exists() or shader_builder.FEATURE_TEMP.is_symlink() or Path(str(shader_builder.FEATURE_TEMP)+'.meta').exists() or Path(str(shader_builder.FEATURE_TEMP)+'.meta').is_symlink():
            raise ValueError('la carpeta temporal de materiales de shaders ya existe; hay que revisarla')
        if shader_builder.BUILTIN_TEMP.exists() or shader_builder.BUILTIN_TEMP.is_symlink() or Path(str(shader_builder.BUILTIN_TEMP)+'.meta').exists() or Path(str(shader_builder.BUILTIN_TEMP)+'.meta').is_symlink():
            raise ValueError('la carpeta temporal de materiales integrados ya existe; hay que revisarla')
        if shader_builder.unity_in_use(): raise ValueError('Unity ya está usando este proyecto; cerralo antes de preparar los datos')
    env = os.environ.copy()
    libs = [str(path) for path in LOCAL_LIBS if path.is_dir()]
    if env.get('LD_LIBRARY_PATH'): libs.append(env['LD_LIBRARY_PATH'])
    if libs: env['LD_LIBRARY_PATH'] = os.pathsep.join(libs)
    result = subprocess.run([sys.executable, '-c', 'import UnityPy; from PIL import Image'], env=env,
                            capture_output=True, text=True)
    if result.returncode: raise ValueError('no se pueden cargar UnityPy o Pillow: ' + result.stderr.strip())


def check_steam_directory(src):
    bundles = src/'StreamingAssets/aa'
    if (bundles/'StandaloneWindows64').is_dir() and not (bundles/'StandaloneLinux64').is_dir():
        converter = load_module('windows_locale_preflight', ROOT/'tools/normalize-windows-localization.py')
        if sha(converter.MANIFEST) != converter.MANIFEST_SHA:
            raise ValueError('falta el inventario local de idiomas Windows o es incompatible')
        inventory = json.loads((ROOT/'tools/steam-windows-diagnostic-inputs.json').read_text())
        for name, row in inventory.items():
            path = src.joinpath(*name.split('/'))
            if not path.is_file() or path.stat().st_size != row['size'] or sha(path) != row['sha256']:
                raise ValueError('archivo de Steam Windows ausente o de versión incompatible: '+name)
        rows = json.loads(converter.MANIFEST.read_text())['bundles']
        original = load_module('original_windows_preflight', ROOT/'tools/prepare-original-data.py')
        if len(rows) != len(original.BUNDLES) or {r['name'] for r in rows} != set(original.BUNDLES):
            raise ValueError('inventario de idiomas Windows inconsistente')
        for row in rows:
            path = bundles/'StandaloneWindows64'/row['name']
            if not path.is_file() or path.stat().st_size != row['windows_size'] or sha(path) != row['windows_sha256']:
                raise ValueError('idioma de Steam Windows ausente o incompatible: '+row['name'])
        return len(inventory) + len(rows)
    original = load_module('original_data_preflight', ROOT/'tools/prepare-original-data.py')
    inventory = json.loads((ROOT/'tools/steam-linux-diagnostic-inputs.json').read_text())
    if len(original.BUNDLES) != len(original.BUNDLE_SHA256): raise ValueError('inventario de idiomas inconsistente')
    items = [(name, row['size'], row['sha256']) for name, row in inventory.items()]
    prefix = 'StreamingAssets/aa/StandaloneLinux64/'
    items += [(prefix+name, None, digest) for name,digest in zip(original.BUNDLES,original.BUNDLE_SHA256)]
    for name, size, expected in items:
        if '\\' in name or any(part in ('', '.', '..') or ':' in part for part in name.split('/')):
            raise ValueError('ruta de inventario no válida: ' + name)
        path = src.joinpath(*name.split('/'))
        if (not path.is_file() or (size is not None and path.stat().st_size != size)
                or (size is None and not 0 < path.stat().st_size <= 8*1024*1024) or sha(path) != expected):
            raise ValueError('archivo de Steam ausente o de versión incompatible: ' + name)
    return len(items)


def check_windows_launcher_inputs():
    current = load_module('windows_launcher_preflight', ROOT/'tools/build-current-runtime-diagnostic.py')
    for path, expected in current.WINDOWS_LAUNCHER_PINS.items():
        if not path.is_file() or sha(path) != expected:
            raise ValueError('falta un parche local del launcher Windows o es incompatible: '+str(path))


def is_windows_steam_zip(path):
    """Classify only; extraction and compatibility checks still run afterwards."""
    with zipfile.ZipFile(path) as z:
        entries = z.infolist()
        if len(entries) > 20000: raise ValueError('demasiadas entradas en el ZIP')
        markers = [e for e in entries if not e.is_dir() and
                   (e.filename == 'sharedassets1.assets' or e.filename == 'Megabonk_Data/sharedassets1.assets'
                    or e.filename.endswith('/Megabonk_Data/sharedassets1.assets'))]
        if len(markers) != 1: raise ValueError('el ZIP debe contener una única carpeta Megabonk_Data')
        windows = json.loads((ROOT/'tools/steam-windows-diagnostic-inputs.json').read_text())
        return (markers[0].file_size == windows['sharedassets1.assets']['size'] or
                any('/StandaloneWindows64/' in e.filename for e in entries))


def extract_windows_steam_zip(src, destination):
    converter = load_module('windows_zip_locale_preflight', ROOT/'tools/normalize-windows-localization.py')
    if sha(converter.MANIFEST) != converter.MANIFEST_SHA:
        raise ValueError('falta el inventario local de idiomas Windows o es incompatible')
    inventory = json.loads((ROOT/'tools/steam-windows-diagnostic-inputs.json').read_text())
    rows = json.loads(converter.MANIFEST.read_text())['bundles']
    original = load_module('windows_zip_locale_inventory', ROOT/'tools/prepare-original-data.py')
    if len(rows) != len(original.BUNDLES) or {row['name'] for row in rows} != set(original.BUNDLES):
        raise ValueError('inventario de idiomas Windows inconsistente')
    for row in rows:
        name = 'StreamingAssets/aa/StandaloneWindows64/' + row['name']
        if name in inventory: raise ValueError('inventario de Steam Windows duplicado')
        inventory[name] = {'size': row['windows_size'], 'sha256': row['windows_sha256']}
    extractor = load_module('windows_zip_extract', ROOT/'tools/build-zip-diagnostic.py')
    extractor.extract(src, destination, inventory)
    return len(inventory)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    source = p.add_mutually_exclusive_group()
    source.add_argument('--steam-data', type=Path, help='supported Linux or Windows Steam Megabonk_Data directory')
    source.add_argument('--steam-zip', type=Path, help='ZIP containing the compatible Linux or Windows Steam Megabonk_Data folder')
    p.add_argument('--output', type=Path, help='new local output directory; required unless using --check')
    p.add_argument('--check', action='store_true', help='check Steam input and core local prerequisites without building')
    p.add_argument('--compile-decoder', action='store_true', help='build the Crunch decoder from pinned source for this run')
    a = p.parse_args()
    if not a.check and a.output is None: p.error('--output is required unless using --check')
    if a.steam_data is None and a.steam_zip is None:
        finder = load_module('local_steam_finder', ROOT/'tools/find-local-steam-data.py')
        try:
            a.steam_data = finder.discover(finder.default_roots(), check_steam_directory)
        except (ValueError, OSError) as exc:
            p.error(str(exc))
        print('Copia de Steam detectada:', a.steam_data, flush=True)
    src = (a.steam_data or a.steam_zip).expanduser().resolve()
    out = a.output.expanduser().absolute() if a.output else None
    if out and (out.exists() or out.is_symlink() or not out.parent.is_dir()): p.error('output must be new and its parent must exist')
    if not src.exists() or (a.steam_data and not src.is_dir()) or (a.steam_zip and not src.is_file()):
        p.error('Steam input is missing or has the wrong type')
    if out and (out.resolve().is_relative_to(src) or src.is_relative_to(out.resolve())):
        p.error('output must be separate from Steam input')
    bundle = load_module('bundle', ROOT/'tools/prepare-android-data-bundle.py')
    print('Diagnóstico local: no compartir el APK ni los datos producidos.', flush=True)
    try:
        if a.check:
            check_local_environment(a.compile_decoder)
            # Both supported Steam platforms use the current launcher.
            check_windows_launcher_inputs()
            if a.steam_data:
                count=check_steam_directory(src)
                print(f'Datos de Steam compatibles: {count} archivos verificados.', flush=True)
            else:
                if not 0 < src.stat().st_size <= 1024 * 1024 * 1024: raise ValueError('tamaño de ZIP no válido')
                if is_windows_steam_zip(src):
                    with tempfile.TemporaryDirectory(prefix='.openbonk-windows-check-') as tmp:
                        extract_windows_steam_zip(src, Path(tmp))
                else: bundle.verify_steam_zip(src)
                print('ZIP de Steam compatible: entradas requeridas verificadas.', flush=True)
            print('Requisitos principales encontrados. La construcción comprueba otras plantillas durante el proceso.', flush=True)
            return
        # Staging and final destination have the same parent so hard links cannot
        # cross filesystems. No existing output is replaced.
        with tempfile.TemporaryDirectory(prefix='.openbonk-personal-', dir=out.parent) as tmp:
            tmp = Path(tmp); steam_zip = src if a.steam_zip else tmp/'steam-data.zip'
            windows = bool(a.steam_data and (src/'StreamingAssets/aa/StandaloneWindows64').is_dir()
                           and not (src/'StreamingAssets/aa/StandaloneLinux64').is_dir())
            windows_source = src
            if a.steam_zip:
                if not 0 < src.stat().st_size <= 1024 * 1024 * 1024: raise ValueError('invalid Steam ZIP size')
                if is_windows_steam_zip(src):
                    windows = True; windows_source = tmp/'Megabonk_Data'; windows_source.mkdir()
                    extract_windows_steam_zip(src, windows_source)
                    steam_zip = tmp/'steam-data.zip'
            check_windows_launcher_inputs()
            if windows:
                check_steam_directory(windows_source)
                run(sys.executable, ROOT/'tools/prepare-windows-launcher-data.py', windows_source, steam_zip)
            elif a.steam_data:
                run(sys.executable, ROOT/'tools/prepare-original-data.py', src, steam_zip, '--complete')
            else:
                if not 0 < steam_zip.stat().st_size <= 1024 * 1024 * 1024: raise ValueError('invalid Steam ZIP size')
                bundle.verify_steam_zip(steam_zip)
            decoder = decoder_choice(a.compile_decoder)
            if decoder is None:
                decoder = tmp/'unpack-unity-crunch'
                builder = load_module('crunch_builder_run', ROOT/'tools/build-crunch-decoder.py')
                builder.build(decoder)
                print('Conversor Crunch compilado desde código fuente verificado.', flush=True)
            diagnostic = tmp/'diagnostic'
            if windows:
                run(sys.executable, ROOT/'tools/build-current-runtime-diagnostic.py', windows_source, diagnostic, '--windows-only', '--windows-launcher',
                    env_overrides={'OPENBONK_CRUNCH_DECODER':str(decoder)})
            else:
                run(sys.executable, ROOT/'tools/build-zip-diagnostic.py', steam_zip, diagnostic, '--windows-launcher',
                    env_overrides={'OPENBONK_CRUNCH_DECODER':str(decoder)})
            report = json.loads((diagnostic/'report.json').read_text())
            apk, obb = diagnostic/'OpenBonk-diagnostic.apk', diagnostic/OBB
            if sha(apk) != report['apk_sha256'] or sha(obb) != report['obb_sha256']:
                raise ValueError('diagnostic report and files differ')
            data_zip = tmp/'OpenBonk-data.zip'
            run(sys.executable, ROOT/'tools/prepare-android-data-bundle.py', obb, steam_zip, data_zip,
                '--diagnostic-report', diagnostic/'report.json', *(['--windows-launcher', '--windows-raw'] if windows else []))
            files = {'OpenBonk-local.apk': apk, 'OpenBonk-data.zip': data_zip,
                     'diagnostic-report.json': diagnostic/'report.json', 'media-audit.json': diagnostic/'media-audit.json',
                     'video-report.json': diagnostic/'video-report.json'}
            receipt = {'kind':'local diagnostic only, not for distribution',
                       'files':{name: sha(path) for name, path in files.items()},
                       'instructions':'Install the local APK, then select OpenBonk-data.zip in its launcher.',
                       'limitations':['Contains original game and third-party material.',
                                      'Depends on private pinned local templates and one pinned Steam version.',
                                      'Debug signature and experimental DEX modification.',
                                      'No complete physical-device validation.']}
            receipt_path = tmp/'READY.json'
            receipt_path.write_text(json.dumps(receipt, indent=2)+'\n')
            # mkdir is exclusive, and READY.json is linked last. A partial folder
            # after a power loss has no completion marker and is not installable.
            out.mkdir(mode=0o700)
            try:
                for name, path in files.items(): os.link(path, out/name)
                os.link(receipt_path, out/'READY.json')
            except OSError as exc:
                raise OSError(f'could not finish publishing {out}; incomplete folder has no READY.json: {exc}') from exc
    except (ValueError, OSError, zipfile.BadZipFile, KeyError) as exc:
        p.error(str(exc))
    except subprocess.CalledProcessError as exc:
        p.error(f'local preparation failed (exit {exc.returncode}); see the output above')
    print('Preparación local terminada:', out, flush=True)
    print('No se instaló nada en ningún dispositivo. Los archivos contienen datos del juego; no distribuir.', flush=True)


if __name__ == '__main__': main()
