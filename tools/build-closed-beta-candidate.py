#!/usr/bin/env python3
"""Prepare a local, reviewable closed-beta candidate. Does not publish or send it."""
import argparse
import hashlib
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parent.parent
STATUS = ("# Candidato de beta de OpenBonk\n\n"
          "OpenBonk es un port comunitario no oficial para Android. Cada tester aporta "
          "sus propios archivos compatibles de Steam. No se adjuntan el ZIP de Steam, "
          "el perfil de juego ni la clave de firma.\n\n"
          "La APK contiene código reconstruido del juego, una plantilla de datos y "
          "shaders compilados. La revisión de derechos y componentes de terceros sigue "
          "pendiente. La carpeta completa de Windows y Windows PowerShell 5.1 no se "
          "han probado en una PC Windows real. Las búsquedas de privacidad son "
          "acotadas y no garantizan la ausencia de todos los datos personales.\n\n"
          "Este candidato es local y está pendiente de revisión; no se ha subido ni enviado.\n").encode('utf-8')


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path, help='new ZIP path in an existing directory')
    args = parser.parse_args()
    output = args.output.absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new file in an existing directory')
    # Reuse the pinned APK, signature and privacy checks of the review builder.
    with tempfile.TemporaryDirectory(prefix='.openbonk-candidate-', dir=output.parent) as temp:
        checked = Path(temp) / 'checked.zip'
        subprocess.run([sys.executable, str(ROOT/'tools/build-private-beta-review.py'), str(checked)],
                       check=True, capture_output=True, text=True, timeout=120)
        with zipfile.ZipFile(checked) as source:
            required = {'OpenBonk.apk', 'BETA_TESTER_GUIDE.md', 'tools/create-launcher-steam-zip.py',
                        'tools/create-launcher-steam-zip.ps1', 'tools/launcher-steam-inputs.json'}
            if not required <= set(source.namelist()) or source.testzip() is not None:
                raise ValueError('review bundle does not have the expected contents')
            files = {name: source.read(name) for name in required}
    files['BETA-STATUS.md'] = STATUS
    files['SHA256SUMS.txt'] = ''.join(f'{sha(data)}  {name}\n' for name, data in sorted(files.items())).encode('ascii')
    created = False
    try:
        fd = os.open(output, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
        created = True
        with os.fdopen(fd, 'wb') as stream, zipfile.ZipFile(stream, 'w', allowZip64=True) as z:
            for name, data in sorted(files.items()):
                z.writestr(name, data, compress_type=zipfile.ZIP_STORED if name.endswith('.apk') else zipfile.ZIP_DEFLATED)
        with zipfile.ZipFile(output) as z:
            if set(z.namelist()) != set(files) or z.testzip() is not None:
                raise ValueError('candidate inventory or CRC mismatch')
            if any(sha(z.read(name)) != sha(data) for name, data in files.items()):
                raise ValueError('candidate content mismatch')
    except BaseException:
        if created: output.unlink(missing_ok=True)
        raise
    with output.open('rb') as f: digest = hashlib.file_digest(f, 'sha256').hexdigest()
    print('Local beta candidate:', output)
    print('SHA-256:', digest)
    print('Not uploaded or sent. See BETA-STATUS.md for unresolved review items.')


if __name__ == '__main__': main()
