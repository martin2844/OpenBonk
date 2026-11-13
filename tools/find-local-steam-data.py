#!/usr/bin/env python3
"""Find local Steam library copies; never modify the libraries."""
import re
from pathlib import Path

APP_DIR = Path('steamapps/common/Megabonk/Megabonk_Data')
PATH_LINE = re.compile(r'^\s*"path"\s*"((?:\\.|[^"\\])*)"\s*$', re.IGNORECASE)


def default_roots():
    home = Path.home()
    return [home/'.local/share/Steam', home/'.steam/steam',
            home/'.var/app/com.valvesoftware.Steam/.local/share/Steam']


def library_roots(steam_roots):
    """Yield standard library paths and paths listed in libraryfolders.vdf."""
    seen = set()
    for root in steam_roots:
        root = Path(root).expanduser()
        paths = [root]
        manifest = root/'steamapps/libraryfolders.vdf'
        try:
            if manifest.is_file() and manifest.stat().st_size <= 8*1024*1024:
                for line in manifest.read_text(encoding='utf-8-sig').splitlines():
                    match = PATH_LINE.fullmatch(line)
                    if match:
                        # VDF quotes and backslashes are escaped; leave other
                        # backslash sequences untouched instead of decoding them.
                        value = re.sub(r'\\([\\"])', r'\1', match.group(1))
                        path = Path(value).expanduser()
                        if path.is_absolute(): paths.append(path)
        except (OSError, UnicodeError):
            pass
        for path in paths:
            path = path.resolve()
            if path not in seen:
                seen.add(path)
                yield path


def discover(steam_roots, verify):
    """Return one validated data directory; reject absent or ambiguous copies."""
    candidates = []
    for library in library_roots(steam_roots):
        data = library/APP_DIR
        if data.is_dir(): candidates.append(data.resolve())
    candidates = list(dict.fromkeys(candidates))
    compatible = []
    for data in candidates:
        try:
            verify(data)
        except (ValueError, OSError):
            continue
        compatible.append(data)
    if len(compatible) > 1:
        raise ValueError('hay varias copias compatibles de Steam; elegí una con --steam-data: ' + ', '.join(map(str, compatible)))
    if not compatible:
        raise ValueError('no se encontró una copia compatible en las bibliotecas de Steam; indicá la carpeta con --steam-data o usá --steam-zip')
    return compatible[0]
