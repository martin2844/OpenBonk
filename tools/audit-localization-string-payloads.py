#!/usr/bin/env python3
"""Measure UTF-8 string bytes left in a private localization-stage template.

Reports counts, not text. The reference and template still contain game data.
This covers readable StringTable m_TableData values only; it is not an asset,
provenance, translation, or distribution audit.
"""
import argparse
import base64
from collections import Counter
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import struct
import zipfile

ROOT = Path(__file__).resolve().parent.parent


def sha(data):
    return hashlib.sha256(data).hexdigest()


def file_sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def string_offsets(body, values):
    """Find aligned length-prefixed UTF-8 strings, rejecting ambiguous groups."""
    if any(not isinstance(value, str) for value in values):
        raise ValueError('invalid localized string')
    groups = Counter(v for v in values if v)
    matches = []; skipped = 0
    for value, count in groups.items():
        data = value.encode('utf-8')
        needle = struct.pack('<I', len(data)) + data
        offsets = [i + 4 for i in range(0, len(body) - len(needle) + 1, 4)
                   if body[i:i + len(needle)] == needle]
        if len(offsets) != count:
            skipped += count
        else:
            matches.extend((offset, data) for offset in offsets)
    matches.sort(key=lambda item: item[0])
    if any(a + len(x) > b for (a, x), (b, _) in zip(matches, matches[1:])):
        raise ValueError('overlapping string payloads')
    return matches, skipped


def check_packaged_template(apk, template, entries, platform):
    name = 'assets/openbonk-diagnostic/' + ('windows/' if platform == 'windows' else '') + 'generated-audio-template.openbonk'
    with zipfile.ZipFile(apk) as outer, zipfile.ZipFile(template) as stage:
        infos = [info for info in outer.infolist() if info.filename == name]
        if len(infos) != 1 or infos[0].file_size > 1024 * 1024 * 1024:
            raise ValueError('invalid packaged template')
        with zipfile.ZipFile(io.BytesIO(outer.read(infos[0]))) as packaged:
            stage_names = stage.namelist(); packaged_names = packaged.namelist()
            for entry in entries:
                if stage_names.count(entry) != 1 or packaged_names.count(entry) != 1:
                    raise ValueError('missing or duplicate packaged localization entry')
                if sha(stage.read(entry)) != sha(packaged.read(entry)):
                    raise ValueError('packaged localization differs from audited template')


def audit(reference, template, plan):
    import UnityPy
    spec = importlib.util.spec_from_file_location('localization_cab_audit', ROOT / 'tools/experimental-localization-cab-plan.py')
    helper = importlib.util.module_from_spec(spec); spec.loader.exec_module(helper)
    metadata = json.loads(plan.read_text(encoding='utf-8'))
    if (metadata.get('schema') != 1 or metadata.get('kind') != 'experimental-localization-cab-obb' or
            metadata.get('platform') not in ('linux', 'windows') or file_sha(template) != metadata.get('template_sha256')):
        raise ValueError('localization plan or template mismatch')
    records = metadata.get('records')
    if not isinstance(records, list) or not records or len(records) > 100:
        raise ValueError('invalid localization records')
    rows = []; seen = set()
    with zipfile.ZipFile(reference) as original, zipfile.ZipFile(template) as masked:
        for record in records:
            entry = record['entry']
            if entry in seen: raise ValueError('duplicate localization entry')
            seen.add(entry)
            if 'localization-string-tables-' not in entry: continue
            raw_plan = base64.b64decode(record['plan'], validate=True)
            if len(raw_plan) < 104 or raw_plan[:4] != b'OBL1' or sha(raw_plan) != record['plan_sha256']:
                raise ValueError('invalid localization CAB plan')
            raw, masked_bytes = original.read(entry), masked.read(entry)
            env = UnityPy.load(raw)
            if len(env.files) != 1: raise ValueError('invalid bundle')
            bundle = next(iter(env.files.values()))
            if len(bundle.files) != 1: raise ValueError('invalid bundle entries')
            name, child = next(iter(bundle.files.items()))
            cab = bytes(child.reader.bytes)
            plain, offset = helper.plain_bundle(bundle, name, cab)
            if (sha(masked_bytes) != raw_plan[36:68].hex() or sha(plain) != raw_plan[68:100].hex() or
                    len(plain) != len(masked_bytes) or any(x and x != y for x, y in zip(masked_bytes, plain))):
                raise ValueError('localization reference or mask mismatch')
            masked_cab = masked_bytes[offset:offset + len(cab)]
            row = {'entry': entry, 'strings': 0, 'empty_strings': 0, 'skipped_strings': 0, 'skipped_objects': 0,
                   'total_nonzero_utf8_bytes': 0, 'retained_nonzero_utf8_bytes': 0}
            for obj in env.objects:
                if obj.type.name != 'MonoBehaviour': continue
                try:
                    tree = obj.read_typetree(); table = tree.get('m_TableData')
                    if table is None: continue
                    if not isinstance(table, list): raise ValueError('invalid table')
                    values = [item.get('m_Localized') for item in table if isinstance(item, dict)]
                    if len(values) != len(table): raise ValueError('invalid table entry')
                    body = obj.get_raw_data(); matches, skipped = string_offsets(body, values)
                    row['empty_strings'] += values.count('')
                    row['skipped_strings'] += skipped
                    for position, data in matches:
                        start = obj.byte_start + position
                        if start < 0 or cab[start:start + len(data)] != data:
                            raise ValueError('string location mismatch')
                        row['strings'] += 1
                        row['total_nonzero_utf8_bytes'] += sum(x != 0 for x in data)
                        row['retained_nonzero_utf8_bytes'] += sum(x != 0 for x in masked_cab[start:start + len(data)])
                except (ValueError, TypeError, IndexError, KeyError, AttributeError, NotImplementedError):
                    row['skipped_objects'] += 1
            rows.append(row)
    if not rows: raise ValueError('no string-table bundles found')
    return {'platform': metadata['platform'], 'bundles': rows, 'limitations': 'Only readable, uniquely located nonempty StringTable m_TableData UTF-8 values in selected localization-stage bundles are measured. Other text, object fields, bundles, APK entries and provenance are outside scope.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('reference-obb', 'template-obb', 'plan', 'output'):
        parser.add_argument('--' + name, required=True, type=Path)
    parser.add_argument('--apk', type=Path, help='also compare audited entries with the packaged template')
    parser.add_argument('--platform', choices=('linux', 'windows'), default='linux')
    a = parser.parse_args()
    inputs = [a.reference_obb.resolve(strict=True), a.template_obb.resolve(strict=True), a.plan.resolve(strict=True)]
    out = a.output.expanduser().absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir() or out.resolve().is_relative_to(ROOT) or out.resolve() in inputs:
        parser.error('output must be a new file outside the checkout and inputs')
    result = audit(*inputs)
    if a.apk:
        if a.platform != result['platform']: parser.error('platform does not match localization plan')
        apk = a.apk.resolve(strict=True)
        if out.resolve() == apk: parser.error('output must be separate from APK')
        check_packaged_template(apk, inputs[1], [row['entry'] for row in result['bundles']], a.platform)
        result['packaged_entries_verified'] = len(result['bundles'])
    with out.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2); stream.write('\n')
    rows = result['bundles']
    print('String-table bundles:', len(rows), 'strings:', sum(x['strings'] for x in rows),
          'retained nonzero UTF-8 bytes:', sum(x['retained_nonzero_utf8_bytes'] for x in rows),
          'skipped strings:', sum(x['skipped_strings'] for x in rows), 'skipped objects:', sum(x['skipped_objects'] for x in rows))


if __name__ == '__main__': main()
