#!/usr/bin/env python3
"""Build a local research diagnostic from a ZIP of pinned Steam data.

This needs the same private local templates and tools as the directory builder.
The result contains original game data and is not a distributable release.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path, PurePosixPath
import stat
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
INPUTS = json.loads((ROOT / 'tools/steam-linux-diagnostic-inputs.json').read_text())
WINDOWS_INPUTS = json.loads((ROOT / 'tools/steam-windows-diagnostic-inputs.json').read_text())


def locale_inputs(windows):
    if not windows:
        return json.loads((ROOT/'tools/steam-linux-locale-inputs.json').read_text())
    spec=importlib.util.spec_from_file_location('windows_locales',ROOT/'tools/normalize-windows-localization.py')
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    with module.MANIFEST.open('rb') as f:
        if hashlib.file_digest(f,'sha256').hexdigest()!=module.MANIFEST_SHA:raise ValueError('incompatible Windows localization inventory')
    rows=json.loads(module.MANIFEST.read_text())['bundles']
    result={'StreamingAssets/aa/StandaloneWindows64/'+r['name']:{'size':r['windows_size'],'sha256':r['windows_sha256']} for r in rows if r['name'].startswith('localization-string-tables-') or r['name']=='localization-assets-shared_assets_all.bundle'}
    if len(result)!=18:raise ValueError('incompatible Windows locale count')
    return result


def extract(source, destination, inputs=INPUTS):
    with zipfile.ZipFile(source) as archive:
        entries = archive.infolist()
        if len(entries) > 100000:
            raise ValueError('demasiadas entradas en el ZIP')
        seen = set()
        by_name = {}
        for entry in entries:
            name = entry.filename
            parts = name.rstrip('/').split('/')
            if ('\\' in name or name.startswith('/') or any(p in ('', '.', '..') or ':' in p for p in parts)):
                raise ValueError('ruta insegura en el ZIP: ' + name)
            key = name.rstrip('/').casefold()
            if key in seen:
                raise ValueError('ruta duplicada en el ZIP: ' + name)
            seen.add(key)
            if not entry.is_dir():
                by_name[name] = entry
        marker = 'sharedassets1.assets'
        roots = {name[:-len(marker)] for name in by_name
                 if name == marker or name.endswith('/Megabonk_Data/' + marker)
                 or name == 'Megabonk_Data/' + marker}
        if len(roots) != 1:
            raise ValueError('el ZIP debe contener una única carpeta Megabonk_Data completa; el paquete opcional de textos no alcanza')
        prefix = roots.pop()
        missing = [name for name in inputs if prefix + name not in by_name]
        if missing:
            raise ValueError('faltan archivos del juego en el ZIP: ' + ', '.join(missing))
        for name, expected in inputs.items():
            entry = by_name[prefix + name]
            mode = entry.external_attr >> 16
            if (entry.flag_bits & 1 or stat.S_ISLNK(mode) or entry.file_size != expected['size']):
                raise ValueError('archivo cifrado, enlace o tamaño incompatible: ' + name)
            target = destination.joinpath(*PurePosixPath(name).parts)
            target.parent.mkdir(parents=True, exist_ok=True)
            digest = hashlib.sha256()
            size = 0
            with archive.open(entry) as src, target.open('xb') as dst:
                while chunk := src.read(1024 * 1024):
                    size += len(chunk)
                    if size > expected['size']:
                        raise ValueError('archivo demasiado grande: ' + name)
                    digest.update(chunk)
                    dst.write(chunk)
            if size != expected['size'] or digest.hexdigest() != expected['sha256']:
                raise ValueError('versión o contenido incompatible: ' + name)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('steam_zip', type=Path, help='ZIP of the supported Steam game data')
    p.add_argument('output', type=Path, help='new local diagnostic output directory')
    p.add_argument('--windows-source', action='store_true', help='use the pinned Windows Steam build instead of Linux')
    p.add_argument('--windows-launcher', action='store_true', help='include the experimental Windows ZIP reader')
    a = p.parse_args()
    source = a.steam_zip.expanduser().resolve()
    out = a.output.expanduser().absolute()
    if not source.is_file():
        p.error('no se encuentra el ZIP')
    if out.exists() or out.is_symlink() or not out.parent.is_dir():
        p.error('la carpeta de salida debe ser nueva y su carpeta padre debe existir')
    try:
        with tempfile.TemporaryDirectory(prefix='.openbonk-steam-zip-', dir=out.parent) as tmp:
            data = Path(tmp) / 'Megabonk_Data'
            data.mkdir()
            extract(source, data, (WINDOWS_INPUTS if a.windows_source else INPUTS) | locale_inputs(a.windows_source))
            print('ZIP de Steam verificado; construyendo diagnóstico local.', flush=True)
            command = [sys.executable, str(ROOT / 'tools/build-current-runtime-diagnostic.py'), str(data), str(out)]
            if a.windows_source: command.append('--windows-only')
            if a.windows_launcher: command.append('--windows-launcher')
            subprocess.run(command, check=True)
    except (ValueError, OSError, zipfile.BadZipFile) as exc:
        p.error(str(exc))


if __name__ == '__main__':
    main()
