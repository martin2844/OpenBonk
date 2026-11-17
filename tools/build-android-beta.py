#!/usr/bin/env python3
"""Run the dual-input Android build with the 0.1.2 beta feature selection.

All paths, package identity and version are supplied to the underlying builder.
This preset does not sign or publish the result.
"""
import os
from pathlib import Path
import sys


BETA_012_OPTIONS = (
    '--include-rewired', '--quality-textures', '--normal-fallback',
    '--normal-block-fallback', '--normal-block-extra', '--color-block-fallback',
    '--emission-block-fallback', '--bullseye-block-fallback',
    '--animation-curve-fallback', '--animation-curve-extended',
    '--animation-curve-hybrid', '--animation-curve-reordered',
    '--reordered-animation-bindings', '--neighboring-animation-keys',
    '--font-fallback', '--resource-fallback', '--nodraw-fallback',
    '--atlas-fallback', '--lightmap-fallback', '--emission-fallback',
    '--fox-fallback', '--bounded-fallback', '--shadow-fallback',
    '--emoji-fallback', '--partial-rgba', '--partial-normal-rgba',
    '--localization-cab', '--localization-small', '--partial-etc2',
    '--partial-etc2-small', '--partial-etc2-tiny', '--partial-etc2-aliases',
    '--partial-emission1-alias', '--partial-etc2-fast', '--partial-etc2-blend',
    '--partial-etc2-byte', '--partial-etc2-rgba', '--partial-etc2-extra',
    '--color-block-extra', '--small-color-block-fallback',
    '--small-color-block-aliases', '--lightmap-block-fallback', '--shared0-fonts',
)


def main():
    builder = Path(__file__).resolve().with_name('build-steam-research-launcher.py')
    os.execv(sys.executable, [sys.executable, str(builder), *BETA_012_OPTIONS, *sys.argv[1:]])


if __name__ == '__main__':
    main()
