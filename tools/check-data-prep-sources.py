#!/usr/bin/env python3
"""Compile the Steam data-preparation Java and verify generated mask sources.

This checks one build component, not a complete APK or the proprietary inputs.
"""
import argparse
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile


ROOT = Path(__file__).resolve().parent.parent
SOURCES = ROOT / 'analysis/data-conversion'
MASKS = (
    ('generate-shared2-animation-masks.py', 'shared2-animation-position-mask.json', 'SteamShared2AnimationMasks.java'),
    ('generate-shared2-particle-masks.py', 'shared2-particle-position-mask.json', 'SteamShared2ParticleMasks.java'),
    ('generate-shared1-particle-masks.py', 'shared1-particle-position-mask.json', 'SteamShared1ParticleMasks.java'),
)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--json-jar', required=True, type=Path, help='path to org.json 20180813 JAR')
    parser.add_argument('--javac', default='javac', help='JDK compiler executable')
    args = parser.parse_args()
    if not args.json_jar.is_file():
        parser.error('org.json JAR not found')
    compiler = shutil.which(args.javac)
    if not compiler:
        parser.error('javac not found')
    files = sorted((SOURCES / 'android').glob('Steam*.java'))
    if not files:
        parser.error('Steam Java source files not found')
    with tempfile.TemporaryDirectory(prefix='openbonk-data-prep-check-') as temp:
        work = Path(temp)
        for script, mask, output in MASKS:
            generated = work / output
            subprocess.run([sys.executable, str(SOURCES / script), str(SOURCES / mask), str(generated)], check=True)
            if generated.read_bytes() != (SOURCES / 'android' / output).read_bytes():
                raise ValueError('generated source differs: ' + output)
        classes = work / 'classes'
        classes.mkdir()
        subprocess.run([compiler, '-encoding', 'UTF-8', '-cp', str(args.json_jar.resolve()),
                        '-d', str(classes), *(str(path) for path in files)], check=True)
    print(f'PASS: {len(files)} Java sources compiled; three generated mask sources match.')
    print('This does not build or verify an APK.')


if __name__ == '__main__':
    main()
