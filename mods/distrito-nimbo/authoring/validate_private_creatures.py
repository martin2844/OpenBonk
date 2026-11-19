"""Validate exported creature streams and record deterministic resource costs."""
import hashlib
import json
import math
import sys
from pathlib import Path


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def validate(root):
    metrics = json.loads((root / 'animation-validation.json').read_text())
    receipt = dict(mesh_frames=0, raw_mesh_bytes=0, atlas_gpu_bytes_with_mips=0, species={},
                   limitations=[
                       'Blender previews validate exported geometry/textures; Unity shader and ETC2 decoding require engine verification.',
                       'Resource totals exclude Unity object overhead and any retained CPU copies.',
                       'Each clip retains 12 shared mesh frames; no per-enemy deformation allocation is required.',
                   ])
    for source_id, measured in metrics.items():
        path = root / 'creatures' / f'{source_id}.json'
        data = json.loads(path.read_text())
        first = data['clips'][0]['frames'][0]
        vertices = len(first['points'])
        assert 0 < vertices <= 65535 and len(first['indices']) % 3 == 0
        assert all(0 <= i < vertices for i in first['indices'])
        frames = 0
        for clip in data['clips']:
            assert len(clip['frames']) == 12
            for frame in clip['frames']:
                frames += 1
                assert all(len(frame[k]) == vertices for k in ('points', 'normals', 'uv', 'atlasRects'))
                assert all(frame[k] == first[k] for k in ('indices', 'uv', 'atlasRects'))
                assert all(math.isfinite(value) for key in ('points', 'normals', 'uv', 'atlasRects')
                           for point in frame[key] for value in point.values())
                assert all(.999 < sum(v*v for v in n.values()) < 1.001 for n in frame['normals'])
        atlas = measured['mesh']['atlas']
        w, h = atlas
        gpu_bytes = 0
        while True:
            gpu_bytes += max(1, (w+3)//4) * max(1, (h+3)//4) * (16 if data['alphaClip'] else 8)
            if w == h == 1:
                break
            w, h = max(1, w//2), max(1, h//2)
        texture = root / 'creatures' / data['texture']
        record = dict(material=data['material'], texture=data['texture'], frames=frames,
                      vertices=vertices, triangles=len(first['indices'])//3, atlas=atlas,
                      alpha_clip=data['alphaClip'], double_sided=data['doubleSided'],
                      raw_mesh_bytes=measured['mesh']['raw_mesh_bytes'],
                      atlas_gpu_bytes_with_mips=gpu_bytes,
                      source_sha256=digest(root / 'decoded' / f'{source_id}.glb'),
                      texture_sha256=digest(texture), json_sha256=digest(path))
        receipt['species'][source_id] = record
        receipt['mesh_frames'] += frames
        receipt['raw_mesh_bytes'] += record['raw_mesh_bytes']
        receipt['atlas_gpu_bytes_with_mips'] += gpu_bytes
    assert len(receipt['species']) == 10 and receipt['mesh_frames'] == 312
    (root / 'textured-quality-receipt.json').write_text(json.dumps(receipt, indent=2)+'\n')
    print('PASS:', receipt['mesh_frames'], 'frames;', round(receipt['raw_mesh_bytes']/1024**2, 2),
          'MiB mesh;', round(receipt['atlas_gpu_bytes_with_mips']/1024**2, 2), 'MiB ETC2 including mips')
    return receipt


if __name__ == '__main__':
    validate(Path(sys.argv[1]))
