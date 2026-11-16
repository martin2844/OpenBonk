"""Validate the three private converted assets needed by legacy transplants."""
import hashlib
from pathlib import Path
import zipfile

PINS = {
    'sharedassets0.assets': '5e76282651d6e6f120d1362277aea74a68bfa482ea8737101092f027d5d374b8',
    'sharedassets1.assets': 'b10fd4de8a0da336f39dff3dddbab994499b229fa5b3f443ab8adbcc16b2082e',
    'sharedassets2.assets': '42a617421d3feaca2907addb8362f0613ca48d5675171a677f8b669576a502e2',
}
PREFIX = 'assets/bin/Data/'


def validate_archive(path):
    with zipfile.ZipFile(path) as z:
        names = z.namelist()
        if len(names) != len(set(names)) or set(names) != {PREFIX+n for n in PINS}:
            raise ValueError('unexpected converted asset archive entries')
        for name, expected in PINS.items():
            with z.open(PREFIX+name) as f:
                if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                    raise ValueError('converted asset differs: '+name)


def make_archive(source, target):
    source, target = Path(source), Path(target)
    if not source.is_dir() or target.exists() or target.is_symlink():
        raise ValueError('converted source must exist and archive must be new')
    for name, expected in PINS.items():
        path = source/name
        if path.is_symlink() or not path.is_file():
            raise ValueError('missing converted asset: '+name)
        with path.open('rb') as f:
            if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                raise ValueError('converted asset differs: '+name)
    created = False
    try:
        with zipfile.ZipFile(target, 'x', compression=zipfile.ZIP_STORED, allowZip64=True) as z:
            created = True
            for name in PINS:
                z.write(source/name, PREFIX+name)
        validate_archive(target)
    except BaseException:
        if created:
            target.unlink(missing_ok=True)
        raise
