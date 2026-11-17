#!/usr/bin/env python3
"""Save package metadata from Thunderstore without downloading mod payloads."""
import argparse
import datetime
import json
from pathlib import Path
import urllib.request

URL = 'https://thunderstore.io/c/megabonk/api/v1/package/'
MAX_BYTES = 16 * 1024 * 1024


def snapshot(packages, date):
    if not isinstance(packages, list):
        raise ValueError('expected a package list')
    result = []
    ids = set()
    for package in packages:
        versions = package.get('versions', [])
        if not versions:
            continue
        package_id = package['full_name']
        if package_id in ids:
            raise ValueError('duplicate package: ' + package_id)
        ids.add(package_id)
        count = sum(v.get('downloads', 0) for v in versions)
        if not isinstance(count, int) or count < 0:
            raise ValueError('invalid download count')
        result.append({
            'id': package_id, 'url': package['package_url'], 'downloads': count,
            'deprecated': package['is_deprecated'],
            'categories': sorted(package.get('categories', [])),
            'version': versions[0]['version_number'],
            'source': versions[0].get('website_url', ''),
        })
    result.sort(key=lambda item: (-item['downloads'], item['id']))
    return {'source': URL, 'retrieved_utc': date,
            'method': 'Sum downloads across versions; includes deprecated packages and tools; not a cross-site ranking.',
            'packages': result}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path, help='new JSON file')
    parser.add_argument('--input', type=Path, help='read a previously downloaded API response instead of making a request')
    args = parser.parse_args()
    if args.input:
        raw = args.input.read_bytes()
    else:
        request = urllib.request.Request(URL, headers={'User-Agent': 'OpenBonk-mod-research/1.0'})
        with urllib.request.urlopen(request, timeout=30) as response:
            raw = response.read(MAX_BYTES + 1)
    if len(raw) > MAX_BYTES:
        parser.error('API response is too large')
    result = snapshot(json.loads(raw), datetime.datetime.now(datetime.timezone.utc).date().isoformat())
    with args.output.open('x', encoding='utf-8') as file:
        json.dump(result, file, indent=2, ensure_ascii=True)
        file.write('\n')
    print(f"Wrote metadata for {len(result['packages'])} packages to {args.output}")


if __name__ == '__main__':
    main()
