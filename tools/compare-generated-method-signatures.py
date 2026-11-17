#!/usr/bin/env python3
"""Compare selected IL2CPP method signatures using generated Unity type tables.

Generated type source files must correspond to their APKs. Direct class and
value types are resolved against each APK's metadata; other initializers are
compared as generated text. Generic containers, referenced generic objects,
method bodies, tokens and runtime behavior are not fully resolved.
"""
import argparse
from collections import Counter
import importlib.util
import json
from pathlib import Path
import struct
import zipfile


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(filename))
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


metadata = load('openbonk_method_metadata', 'compare-apk-method-identities.py')
types = load('openbonk_generated_types', 'compare-generated-type-tables.py')


def signatures(methods, type_table):
    """Resolve selected method types to their generated symbol and initializer."""
    def resolve(index):
        if type(index) is not int or not 0 <= index < len(type_table):
            raise ValueError('method references an invalid generated type')
        return type_table[index]
    result = []
    for identity, result_type, generic, parameters in methods:
        if type(generic) is not int or generic < -1: raise ValueError('invalid generic container index')
        result.append((identity[:4], identity[5:8], resolve(result_type),
                       tuple((name, resolve(type_index)) for name, _, type_index in parameters), generic >= 0))
    return result


def unmatched_owners(first, second):
    groups = Counter()
    for signature, count in (first - second).items():
        image, namespace, owner, _ = signature[0]
        groups[(image, namespace, owner)] += count
    return [{'image': image.decode('utf-8', errors='replace'),
             'namespace': namespace.decode('utf-8', errors='replace'),
             'type': owner.decode('utf-8', errors='replace'), 'count': count}
            for (image, namespace, owner), count in sorted(groups.items())]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('first_apk', 'first_types', 'second_apk', 'second_types'):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    try:
        first_methods = metadata.apk_methods(args.first_apk); second_methods = metadata.apk_methods(args.second_apk)
        first_types = types.read_pair(args.first_apk, args.first_types); second_types = types.read_pair(args.second_apk, args.second_types)
        a = Counter(signatures(first_methods, first_types)); b = Counter(signatures(second_methods, second_types))
    except (OSError, ValueError, zipfile.BadZipFile, struct.error) as error:
        parser.exit(2, f'Cannot compare generated signatures: {error}\n')
    report = {'match': a == b, 'first_methods': len(first_methods), 'second_methods': len(second_methods),
              'first_types': len(first_types), 'second_types': len(second_types),
              'unmatched_first': sum((a - b).values()), 'unmatched_second': sum((b - a).values()),
              'unmatched_first_by_owner': unmatched_owners(a, b), 'unmatched_second_by_owner': unmatched_owners(b, a)}
    print(json.dumps(report, indent=2))
    if a != b: parser.exit(1, 'Selected generated method signatures differ.\n')
    print('Selected signatures match; generic objects, method bodies and behavior remain unverified.')


if __name__ == '__main__': main()
