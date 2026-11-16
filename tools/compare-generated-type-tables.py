#!/usr/bin/env python3
"""Compare generated Unity 2023.2 IL2CPP type tables with APK metadata.

Direct class/value-type indices are resolved to image, namespace and type
names. Other initializers are compared as generated text; referenced generic
objects and runtime behavior are not resolved by this check.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import re
import zipfile

spec = importlib.util.spec_from_file_location('openbonk_metadata_compare', Path(__file__).with_name('compare-apk-method-identities.py'))
metadata = importlib.util.module_from_spec(spec); spec.loader.exec_module(metadata)
MAX_SOURCE = 128 * 1024 * 1024


def normalized_type_table(source, types):
    table_pattern = r'const Il2CppType\* const\s+g_Il2CppTypeTable\[(\d+)\]\s*=\s*\{(.*?)\};'
    tables = list(re.finditer(table_pattern, source, re.S))
    if len(tables) != 1: raise ValueError('missing or ambiguous generated type table')
    count = int(tables[0].group(1)); table = tables[0].group(2)
    symbols = re.findall(r'&([A-Za-z0-9_]+)\s*,', table)
    if not 0 < count <= 1000000 or len(symbols) != count or re.sub(r'&[A-Za-z0-9_]+\s*,', '', table).strip():
        raise ValueError('invalid generated type table')
    entries = re.findall(r'^(?:const )?Il2CppType\s+([A-Za-z0-9_]+)\s*=\s*\{([^\n]+)\};', source, re.M)
    definitions = dict(entries)
    if len(entries) != len(definitions) or any(name not in definitions for name in symbols):
        raise ValueError('missing or duplicate generated type definition')
    result = []
    for symbol in symbols:
        body = definitions[symbol]
        direct = re.fullmatch(r'\s*\(void\*\)(\d+),\s*(.*?IL2CPP_TYPE_(?:CLASS|VALUETYPE)\b.*?)\s*', body)
        if direct:
            index = int(direct.group(1))
            if index >= len(types): raise ValueError('invalid direct metadata type index')
            body = ('direct', types[index], direct.group(2))
        result.append((symbol, body))
    return result


def read_pair(apk_path, source_path):
    if not source_path.is_file() or source_path.stat().st_size > MAX_SOURCE:
        raise ValueError('invalid generated type definitions file')
    with zipfile.ZipFile(apk_path) as archive:
        entries = [entry for entry in archive.infolist() if entry.filename == metadata.ENTRY]
        if len(entries) != 1 or not 0 < entries[0].file_size <= metadata.MAX_METADATA:
            raise ValueError('missing, duplicate or oversized metadata')
        _, types = metadata.metadata_types(archive.read(entries[0]))
    return normalized_type_table(source_path.read_text(encoding='utf-8-sig'), types)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('first_apk', type=Path); parser.add_argument('first_types', type=Path)
    parser.add_argument('second_apk', type=Path); parser.add_argument('second_types', type=Path)
    args = parser.parse_args()
    try:
        first = read_pair(args.first_apk, args.first_types)
        second = read_pair(args.second_apk, args.second_types)
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        parser.exit(2, f'Cannot compare type tables: {error}\n')
    differences = sum(a != b for a, b in zip(first, second)) + abs(len(first) - len(second))
    print(json.dumps({'match': differences == 0, 'first_types': len(first), 'second_types': len(second), 'different_indices': differences}, indent=2))
    if differences: parser.exit(1, 'Generated type tables differ.\n')
    print('Selected type mappings match; referenced generic objects and runtime behavior remain unverified.')


if __name__ == '__main__': main()
