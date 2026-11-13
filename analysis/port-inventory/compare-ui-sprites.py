"""Compare selected sprite images and geometry in two packaged Unity builds."""
import collections
import hashlib
import re
import sys
import tempfile
import zipfile
from pathlib import Path
import UnityPy

NAMES = {'Border2', 'Border2_Gray', 'BorderNew', 'BorderOnly', 'BorderOnlyWithShadow',
         'BorderOnlyWithShadow2', 'BorderSelectionArrow', 'BorderSimple', 'Crosshair',
         'DropShadow', 'DropShadow_0', 'GamepadTemplate', 'HollowCircle', 'HollowCircleOuter',
         'MyButton', 'MyButtonNew', 'MyButtonOverlay', 'RoundedSquare', 'SquareOutline',
         'VignettePixelatedShorter', 'WindowOverlaySides', 'arrow', 'psd_arrow'}

def fields(value, names):
    return None if value is None else tuple(getattr(value, name) for name in names)

def extract(apk):
    found, errors = collections.defaultdict(list), []
    with tempfile.TemporaryDirectory(prefix='openbonk-ui-sprites-') as temp, zipfile.ZipFile(apk) as z:
        names = z.namelist()
        for group in range(5):
            stem = f'assets/bin/Data/sharedassets{group}.assets'
            parts = sorted((x for x in names if re.fullmatch(re.escape(stem) + r'\.split\d+', x)), key=lambda x: int(x.rsplit('split', 1)[1]))
            if not parts and stem in names:
                parts = [stem]
            if not parts:
                continue
            path = Path(temp) / f'sharedassets{group}.assets'
            with path.open('wb') as out:
                for part in parts:
                    out.write(z.read(part))
            resource = f'assets/bin/Data/sharedassets{group}.resource'
            if resource in names:
                (Path(temp) / Path(resource).name).write_bytes(z.read(resource))
            for obj in UnityPy.load(str(path)).objects:
                if obj.type.name != 'Sprite':
                    continue
                sprite = obj.read()
                if sprite.m_Name not in NAMES:
                    continue
                try:
                    image = sprite.image.convert('RGBA')
                    found[sprite.m_Name].append((group, fields(sprite.m_Rect, ('x', 'y', 'width', 'height')),
                        fields(sprite.m_Pivot, ('x', 'y')), fields(sprite.m_Border, ('x', 'y', 'z', 'w')),
                        sprite.m_PixelsToUnits, image.size, hashlib.sha256(image.tobytes()).hexdigest()))
                except Exception as exc:
                    errors.append(f'{sprite.m_Name} group={group}: {type(exc).__name__}: {exc}')
    return {k: sorted(v) for k, v in found.items()}, errors

if len(sys.argv) != 3:
    raise SystemExit('Supply old and new APKs')
old, old_errors = extract(sys.argv[1])
new, new_errors = extract(sys.argv[2])
for name in sorted(NAMES):
    status = 'NOT_FOUND_IN_SCANNED_FILES' if name not in old and name not in new else ('MATCH' if name in old and old.get(name) == new.get(name) else 'DIFF_OR_MISSING')
    print(name, status, 'old=', old.get(name), 'new=', new.get(name))
print('old_errors:', old_errors)
print('new_errors:', new_errors)
print('scope: selected sprites in sharedassets0-4; excludes UI bindings and rendered layouts')
