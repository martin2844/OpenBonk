#!/usr/bin/env python3
"""Index local dependency and notice evidence for one exact diagnostic APK.

A package in a lock file, an assembly name or a native library does not prove
which code survived linking, its provenance, license or permission to ship.
No notices are added to the APK by this script.
"""
import argparse,collections,hashlib,json,re,zipfile
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('apk',type=Path);p.add_argument('project',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
if a.output.exists() or not a.output.parent.is_dir():p.error('output must not exist and parent must exist')
def sha(data):return hashlib.sha256(data).hexdigest()
def artifact(path):return {'path':str(path.relative_to(a.project)),'size':path.stat().st_size,'sha256':sha(path.read_bytes())}
with zipfile.ZipFile(a.apk) as z:
 names=z.namelist()
 if len(names)!=len(set(names)):raise ValueError('duplicate APK entries')
 assemblies=json.loads(z.read('assets/bin/Data/ScriptingAssemblies.json'))['names'];metadata=z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat')
 libs=[{'path':n,'size':z.getinfo(n).file_size,'sha256':sha(z.read(n))} for n in names if n.startswith('lib/') and n.endswith('.so')]
 notices=[{'path':n,'size':z.getinfo(n).file_size,'sha256':sha(z.read(n))} for n in names if re.search(r'(license|licence|notice|copyright)',n,re.I)]
plugins=[]
for path in sorted((a.project/'Assets/Plugins').rglob('*.dll')):
 plugins.append({**artifact(path),'listed_in_apk_assemblies':path.name in assemblies,'name_in_il2cpp_metadata':path.name.encode() in metadata})
lock=json.loads((a.project/'Packages/packages-lock.json').read_text())['dependencies'];packages=[]
for name,entry in sorted(lock.items()):
 version=entry['version'];cache=a.project/'Library/PackageCache'/f'{name}@{version}';documents=[]
 if cache.is_dir():
  for path in sorted(cache.rglob('*')):
   if path.is_file() and not path.name.endswith('.meta') and re.search(r'(^|[._ -])(licen[cs]e|notice|copyright)',path.name,re.I):
    documents.append(artifact(path))
 packages.append({'name':name,'version':version,'source':entry.get('source'),'cache_present':cache.is_dir(),'notice_documents':documents})
result={'apk':str(a.apk),'apk_sha256':sha(a.apk.read_bytes()),'native_libraries':libs,'existing_apk_notices':notices,'project_dlls':plugins,'resolved_packages':packages,'limitations':__doc__};a.output.write_text(json.dumps(result,indent=2)+'\n')
print('APK SHA256',result['apk_sha256']);print('native libraries',len(libs),'APK notice files',len(notices),'project DLLs',len(plugins),'resolved packages',len(packages));print('packages with local notice docs',sum(bool(x['notice_documents']) for x in packages),'documents',sum(len(x['notice_documents']) for x in packages))
for x in notices:print('APK_NOTICE',x['path'],x['sha256'])
for x in plugins:print('DLL',x['path'],'assembly_list',x['listed_in_apk_assemblies'],'metadata_name',x['name_in_il2cpp_metadata'])
for x in packages:
 if x['notice_documents']:print('PACKAGE',x['name'],x['version'],'notices',len(x['notice_documents']))
