#!/usr/bin/env python3
"""Inventory dependency evidence for a local Unity Android build.

Assembly lists and metadata strings are indicators, not proof of executable
contents, provenance, license scope, or redistribution rights.
"""
import argparse
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('apk', type=Path)
parser.add_argument('project', type=Path)
parser.add_argument('pc_data', type=Path)
args = parser.parse_args()
def digest(data): return hashlib.sha256(data).hexdigest()

with ZipFile(args.apk) as archive:
    assemblies = json.loads(archive.read('assets/bin/Data/ScriptingAssemblies.json'))['names']
    metadata = archive.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
    libraries = [{'path': n, 'sha256': digest(archive.read(n))} for n in archive.namelist() if n.startswith('lib/') and n.endswith('.so')]
pc_assemblies = set(json.loads((args.pc_data/'ScriptingAssemblies.json').read_text())['names'])
plugins = args.project/'Assets/Plugins'
project_dlls = {}
for path in sorted(plugins.rglob('*.dll')):
    project_dlls.setdefault(path.name, []).append({'path': str(path.relative_to(args.project)), 'sha256': digest(path.read_bytes())})
assembly_evidence = []
for name in assemblies:
    assembly_evidence.append({'name': name, 'listed_by_pc': name in pc_assemblies,
                              'name_in_android_metadata': name.encode() in metadata,
                              'project_plugin_files': project_dlls.get(name, [])})
manifest = json.loads((args.project/'Packages/manifest.json').read_text())['dependencies']
package_lock = json.loads((args.project/'Packages/packages-lock.json').read_text())['dependencies']
packages = []
for name, entry in sorted(package_lock.items()):
    version = entry['version']; cache = args.project/'Library/PackageCache'/f'{name}@{version}'
    candidates = [p for p in (cache/'LICENSE.md', cache/'LICENSE', cache/'LICENSE.txt') if p.is_file()]
    packages.append({'name': name, 'version': version, 'direct_manifest_version': manifest.get(name),
                     'source': entry.get('source'), 'license_files': [
                         {'path': str(p.relative_to(args.project)), 'sha256': digest(p.read_bytes())} for p in candidates]})
print(json.dumps({'apk_sha256':digest(args.apk.read_bytes()), 'assemblies':assembly_evidence,
                  'native_libraries':libraries, 'resolved_packages':packages,
                  'limitations':'Assembly list and metadata are not proof of retained executable code or redistribution rights; license texts and provenance require independent review.'}, indent=2))
