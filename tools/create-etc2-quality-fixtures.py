#!/usr/bin/env python3
"""Create synthetic RGBA fixtures for the experimental ETC2 quality probe."""
import argparse
import json
from pathlib import Path
import random
import shutil

ROOT = Path(__file__).resolve().parents[1]


def main():
    p = argparse.ArgumentParser(description=__doc__); p.add_argument('--output', required=True, type=Path); a = p.parse_args()
    out = a.output.expanduser().absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    out = out.parent.resolve() / out.name
    if out.is_relative_to(ROOT): p.error('output must be outside the source checkout')
    rng = random.Random(35); rows = []; out.mkdir(mode=0o700)
    try:
        for i, (width, height) in enumerate(((1, 1), (3, 7), (4, 4), (9, 13), (64, 64), (257, 129))):
            name = f'synthetic_{i:02d}'
            (out / (name + '.rgba')).write_bytes(bytes(rng.randrange(256) for _ in range(width * height * 4)))
            rows.append([name, width, height])
        (out / 'fixtures.json').write_text(json.dumps(rows) + '\n', encoding='utf-8')
    except BaseException:
        shutil.rmtree(out); raise
    print(f'Created {len(rows)} synthetic fixtures.')


if __name__ == '__main__': main()
