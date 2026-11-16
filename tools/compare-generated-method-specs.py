#!/usr/bin/env python3
"""Compare generated IL2CPP generic method specs using each APK's metadata.

Method indices are resolved to selected method records; generic instantiation
indices are compared as raw integers. This does not resolve generic arguments
or establish behavior, and generated sources must correspond to the APKs.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import re
import struct
import zipfile

spec = importlib.util.spec_from_file_location('metadata_compare', Path(__file__).with_name('compare-apk-method-identities.py'))
metadata = importlib.util.module_from_spec(spec); spec.loader.exec_module(metadata)
MAX_SOURCE = 128 * 1024 * 1024


def normalized_specs(source, methods):
    pattern = r'const Il2CppMethodSpec\s+g_Il2CppMethodSpecTable\[(\d+)\]\s*=\s*\{(.*?)\};'
    tables = list(re.finditer(pattern, source, re.S))
    if len(tables) != 1: raise ValueError('missing or ambiguous method spec table')
    count = int(tables[0].group(1)); body = tables[0].group(2)
    entry = r'\{\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*\}\s*,'
    rows = list(re.finditer(entry, body))
    if not 0 < count <= 1000000 or len(rows) != count or re.sub(entry, '', body).strip():
        raise ValueError('invalid method spec table')
    result = []
    for row in rows:
        method, class_inst, method_inst = map(int, row.groups())
        if not 0 <= method < len(methods) or class_inst < -1 or method_inst < -1:
            raise ValueError('invalid method spec index')
        result.append((methods[method], class_inst, method_inst))
    return result


def read_pair(apk, source):
    if not source.is_file() or source.stat().st_size > MAX_SOURCE: raise ValueError('invalid source file')
    return normalized_specs(source.read_text(encoding='utf-8-sig'), metadata.apk_methods(apk))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('first_apk', 'first_source', 'second_apk', 'second_source'): parser.add_argument(name, type=Path)
    args = parser.parse_args()
    try:
        first = read_pair(args.first_apk, args.first_source)
        second = read_pair(args.second_apk, args.second_source)
    except (OSError, ValueError, zipfile.BadZipFile, struct.error) as error:
        parser.exit(2, f'Cannot compare method specs: {error}\n')
    differences = sum(a != b for a, b in zip(first, second)) + abs(len(first) - len(second))
    print(json.dumps({'first_specs': len(first), 'second_specs': len(second), 'different_entries': differences}, indent=2))
    if differences: parser.exit(1, 'Selected method specs differ.\n')
    print('Selected method specs match; generic arguments and runtime behavior remain unverified.')


if __name__ == '__main__': main()
