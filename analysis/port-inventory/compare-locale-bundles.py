"""Compare the packaged locale bundle and its named objects in two APKs."""
import collections
import hashlib
import sys
import zipfile
import UnityPy

key = 'assets/aa/Android/localization-locales_assets_all.bundle'
results = []
for path in sys.argv[1:]:
    with open(path, 'rb') as stream:
        apk_hash = hashlib.file_digest(stream, 'sha256').hexdigest()
    with zipfile.ZipFile(path) as archive:
        payload = archive.read(key)
    objects = collections.Counter(
        (item.path_id, item.read().m_Name)
        for item in UnityPy.load(payload).objects
        if item.type.name == 'MonoBehaviour'
    )
    digest = hashlib.sha256(payload).hexdigest()
    print(f'{path}: apk={apk_hash} locale_bundle={digest} locale_objects={sum(objects.values())}')
    results.append((digest, objects))
if len(results) != 2:
    raise SystemExit('Supply exactly two APKs')
print('removed:', list((results[0][1] - results[1][1]).elements()))
print('added:', list((results[1][1] - results[0][1]).elements()))
print('bundle_equal:', results[0][0] == results[1][0])
