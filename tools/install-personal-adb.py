#!/usr/bin/env python3
"""Install one pinned local OpenBonk diagnostic to a selected Android device.

Copies the prepared data ZIP to a new Downloads subdirectory and runs adb
install -r. This may close a running game. Android rejects an update signed by a
different key; this program never uninstalls an existing app. The result remains
a private research build and the launcher still requires selecting the ZIP.
Its package is com.megabonk.nativeport.splittest: an app with another package
name is not updated and its profile is not transferred.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import uuid
import zipfile

ROOT = Path(__file__).resolve().parents[1]
APK_CONTENT = '56c61efba14d4cc987a867a215cf56bbd39197829c4b50095bccdaeef290d98b'
OBB_SHA = '00a50941a48f3263569b0f2ed4aad64d57c9187165ba545e5dd6aa944147e2cf'
SUPPORTED_PAIRS = {
    APK_CONTENT: OBB_SHA,
    'e9524f7015f9507ec66fa7f8614c7daa6e2252c1ff1c1468480d8242c5cb4aa2': 'b0588720eec5ff380b29e2a91baa63aa223a1d62eca942d122a24be72a59a0db',
    'e6fdf30376940ca26dad63b6a9384f734a288920446d366379613e435ad5427c': 'ff37e1c7d2408e0eb25babaec6805c160c8f6419c85fe87c4be0f9a9765a32b9',
    '557d14a4632e6c08004ebad37204b804b17cacbd855fb6c0e579385044c3fe38': 'e13453698c86d8082a7c4183a905a9f189cf9d660257b85a062f160044538378',
    '92f2c58d6d8eb82b5b32114ba84cb191ecabee03d922c321869843a2b91a251a': 'abea33cdc20042f03f434bd1eb554cd095e7a5f2e8162fa9bc8038cbdf539fb0',
    '3211adfea4ac9ad4569ed99b2a02498adfda92ab3829adba98508a9abbd97a34': 'e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691',
}
WINDOWS_LAUNCHER_APK = '557d14a4632e6c08004ebad37204b804b17cacbd855fb6c0e579385044c3fe38'
DIRECT_WINDOWS_APK = '92f2c58d6d8eb82b5b32114ba84cb191ecabee03d922c321869843a2b91a251a'
STEAM_PICKER_APK = '3211adfea4ac9ad4569ed99b2a02498adfda92ab3829adba98508a9abbd97a34'
PREVIEW_PACKAGE = 'com.megabonk.nativeport.preview'
PACKAGE = 'com.megabonk.nativeport.splittest'
PREVIEW_SAVES = '/sdcard/Android/data/'+PREVIEW_PACKAGE+'/files/Saves/CloudDir/0'


def sha(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()


def validate_result(folder):
    folder = Path(folder).resolve(strict=True)
    receipt_path = folder/'READY.json'
    if not receipt_path.is_file() or receipt_path.stat().st_size > 1024*1024:
        raise ValueError('falta READY.json o es demasiado grande')
    receipt = json.loads(receipt_path.read_text())
    if receipt.get('kind') != 'local diagnostic only, not for distribution':
        raise ValueError('la carpeta no contiene la preparación local esperada')
    paths = []
    for name in ('OpenBonk-local.apk', 'OpenBonk-data.zip'):
        path = folder/name
        if not path.is_file() or not path.resolve().is_relative_to(folder): raise ValueError('falta '+name)
        expected = receipt.get('files', {}).get(name)
        if not isinstance(expected, str) or not re.fullmatch('[0-9a-f]{64}', expected) or sha(path) != expected:
            raise ValueError('hash incompatible: '+name)
        paths.append(path)
    apk, bundle = paths
    spec = importlib.util.spec_from_file_location('apk_content', ROOT/'tools/apk-content-digest.py')
    digest = importlib.util.module_from_spec(spec); spec.loader.exec_module(digest)
    apk_content = digest.digest(apk)
    expected_obb = SUPPORTED_PAIRS.get(apk_content)
    if not expected_obb: raise ValueError('la APK no corresponde a una versión compatible de OpenBonk')
    with zipfile.ZipFile(bundle) as z:
        names = z.namelist()
        if len(names) != 2 or set(names) != {'openbonk-data.obb', 'steam-data.zip'}:
            raise ValueError('paquete de datos incompatible')
        if not 0 < z.getinfo('openbonk-data.obb').file_size <= 512*1024*1024 or not 0 < z.getinfo('steam-data.zip').file_size <= 1024*1024*1024:
            raise ValueError('tamaño de datos incompatible')
        with z.open('openbonk-data.obb') as f:
            if hashlib.file_digest(f, 'sha256').hexdigest() != expected_obb:
                raise ValueError('los datos Android no coinciden con esta APK')
        # Verify the actual nested Steam entries, not just the outer receipt:
        # otherwise a damaged or incompatible original-data ZIP is copied and
        # installed only to be rejected later by the Android launcher.
        spec = importlib.util.spec_from_file_location('bundle_validator', ROOT/'tools/prepare-android-data-bundle.py')
        validator = importlib.util.module_from_spec(spec); spec.loader.exec_module(validator)
        with z.open('steam-data.zip') as f:
            try:
                validator.verify_steam_zip(f)
            except ValueError:
                if apk_content not in (WINDOWS_LAUNCHER_APK, DIRECT_WINDOWS_APK, STEAM_PICKER_APK): raise
                f.seek(0)
                try:
                    validator.verify_steam_zip(f, windows_launcher=True)
                except ValueError:
                    if apk_content not in (DIRECT_WINDOWS_APK, STEAM_PICKER_APK): raise
                    f.seek(0)
                    validator.verify_steam_zip(f, windows_launcher=True, windows_raw=True)
    return apk, bundle, receipt['files']['OpenBonk-data.zip']


def command(adb, serial, *args, capture=False):
    result = subprocess.run([adb, '-s', serial, *map(str, args)], text=True,
                            capture_output=capture, check=True)
    return result.stdout.strip() if capture else ''


def select_device(adb, requested=None):
    """Find a single authorized, booted device before starting a transfer."""
    result = subprocess.run([adb, 'devices'], text=True, capture_output=True, check=True)
    devices = [line.split()[0] for line in result.stdout.splitlines()[1:]
               if len(line.split()) >= 2 and line.split()[1] == 'device']
    if requested:
        if requested not in devices: raise ValueError('el dispositivo indicado no está conectado y autorizado')
        serial = requested
    else:
        if len(devices) != 1: raise ValueError('conectá un solo dispositivo o indicá --serial')
        serial = devices[0]
    if command(adb, serial, 'shell', 'getprop', 'sys.boot_completed', capture=True) != '1':
        raise ValueError('Android todavía no terminó de iniciar')
    return serial


def has_previous_install(adb, serial):
    """Include packages uninstalled for this user but retained by Android."""
    result = command(adb, serial, 'shell', 'pm', 'list', 'packages', '-u', PACKAGE, capture=True)
    lines = result.splitlines()
    if any(not line.startswith('package:') for line in lines):
        raise ValueError('no se pudo comprobar la instalación anterior de OpenBonk')
    return 'package:'+PACKAGE in lines


def make_profile(directory, output):
    """Copy the two saves into a private ZIP without changing the originals."""
    directory = Path(directory).expanduser().resolve(strict=True)
    if not directory.is_dir(): raise ValueError('la ruta del perfil no es una carpeta')
    with zipfile.ZipFile(output, 'x', zipfile.ZIP_DEFLATED) as archive:
        for name in ('progression.json', 'stats.json'):
            path = directory/name
            if not path.is_file(): raise ValueError('falta el archivo de perfil '+name)
            with path.open('rb') as source:
                data = source.read(8*1024*1024 + 1)
            if not 0 < len(data) <= 8*1024*1024: raise ValueError('tamaño de perfil incompatible: '+name)
            archive.writestr('profile/'+name, data)
    if output.stat().st_size > 16*1024*1024:
        raise ValueError('el ZIP de perfil supera el límite de 16 MiB del launcher')


def require_preview_stopped(adb, serial):
    result = subprocess.run([adb, '-s', serial, 'shell', 'pidof', PREVIEW_PACKAGE],
                            text=True, capture_output=True)
    if result.returncode not in (0, 1) or result.stderr.strip():
        raise ValueError('no se pudo comprobar si Preview está abierto')
    if result.stdout.strip(): raise ValueError('cerrá completamente OpenBonk Preview antes de copiar su perfil')


def copy_preview_profile(adb, serial, directory):
    """Snapshot Preview's default account into an already-private directory."""
    require_preview_stopped(adb, serial)
    def remote_sha(path):
        words = command(adb, serial, 'shell', 'sha256sum', path, capture=True).split()
        if not words or not re.fullmatch('[0-9a-f]{64}', words[0]):
            raise ValueError('no se pudo verificar el archivo de Preview')
        return words[0]
    snapshots = {}
    for name in ('progression.json', 'stats.json'):
        remote = PREVIEW_SAVES+'/'+name
        try:
            size = command(adb, serial, 'shell', 'stat', '-c', '%s', remote, capture=True)
        except subprocess.CalledProcessError as exc:
            raise ValueError('no se puede leer el archivo de Preview: '+name) from exc
        if not size.isdecimal() or not 0 < int(size) <= 8*1024*1024:
            raise ValueError('archivo de Preview ausente o demasiado grande: '+name)
        before = remote_sha(remote)
        local = directory/name
        command(adb, serial, 'pull', remote, local, capture=True)
        after = remote_sha(remote)
        if local.stat().st_size != int(size) or sha(local) != before or after != before:
            raise ValueError('el perfil de Preview cambió mientras se copiaba: '+name)
        snapshots[remote] = before
    require_preview_stopped(adb, serial)
    if any(remote_sha(path) != expected for path, expected in snapshots.items()):
        raise ValueError('el perfil de Preview cambió mientras se copiaba')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('result', type=Path, help='prepared result directory containing READY.json')
    p.add_argument('--serial', help='adb device serial; required when more than one device is connected')
    p.add_argument('--adb', default='adb', help='path to adb')
    profile_source = p.add_mutually_exclusive_group()
    profile_source.add_argument('--profile-dir', type=Path, help='optional Steam save folder containing progression.json and stats.json')
    profile_source.add_argument('--profile-from-preview', action='store_true', help='copy the default profile from the Preview app on the selected device')
    a = p.parse_args(); adb = shutil.which(a.adb)
    if not adb: p.error('no se encuentra adb')
    try:
        apk, bundle, expected = validate_result(a.result)
        serial = select_device(adb, a.serial)
        with tempfile.TemporaryDirectory(prefix='openbonk-profile-') as temp:
            temp = Path(temp)
            profile = temp/'OpenBonk-profile.zip' if a.profile_dir or a.profile_from_preview else None
            if a.profile_from_preview:
                source = temp/'preview'; source.mkdir(mode=0o700)
                print('Copiando el perfil de Preview sin modificarlo…', flush=True)
                copy_preview_profile(adb, serial, source)
                make_profile(source, profile)
            elif profile: make_profile(a.profile_dir, profile)
            remote = '/sdcard/Download/OpenBonk-'+uuid.uuid4().hex
            command(adb, serial, 'shell', 'mkdir', remote)
            destination = remote+'/OpenBonk-data.zip'
            print('Copiando datos a', destination, flush=True)
            for source, dest, digest in [(bundle, destination, expected)] + ([(profile, remote+'/OpenBonk-profile.zip', sha(profile))] if profile else []):
                command(adb, serial, 'push', source, dest)
                result = command(adb, serial, 'shell', 'sha256sum', dest, capture=True)
                if not result or result.split()[0] != digest:
                    raise ValueError('la copia Android no coincide; la APK no se instaló')
                # Ask Android to index adb-pushed files so the document picker
                # can find them; indexing is asynchronous on some devices.
                command(adb, serial, 'shell', 'am', 'broadcast', '-a',
                        'android.intent.action.MEDIA_SCANNER_SCAN_FILE', '-d', 'file://'+dest, capture=True)
            print('Instalando com.megabonk.nativeport.splittest; una partida abierta puede cerrarse…', flush=True)
            command(adb, serial, 'install', '-r', apk)
            print('Instalación terminada. En el launcher, seleccioná:', destination, flush=True)
            if profile:
                print('Para importar el progreso, elegí Importar perfil ZIP y seleccioná:', remote+'/OpenBonk-profile.zip', flush=True)
                print('El perfil se copió a Descargas; borrá esa copia cuando ya no la necesites.', flush=True)
            print('No se desinstaló ninguna aplicación. El paquete de datos es privado.', flush=True)
    except (OSError, ValueError, KeyError, json.JSONDecodeError, zipfile.BadZipFile, subprocess.CalledProcessError) as exc:
        p.error(str(exc))


if __name__ == '__main__': main()
