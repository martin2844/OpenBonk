# MewtwoStatue source and authoring receipt

The editable `MewtwoStatue.blend` was made from the existing local archive
`/home/martin/Downloads/assets-main.zip`, member
`assets-main/models/opt/regular/150.glb`. The original archive and member were
preserved; no model was downloaded for this work.

| Input | Bytes | SHA-256 |
| --- | ---: | --- |
| Original archive member `150.glb` | 7,600,268 | `98ed03ce2dda7979f0ef0edf804671a3c3823a495dbe7fd230386122ebf77e69` |
| Draco-decoded `150-decoded.glb` | — | `d3c32d8db9b99f549304e809be4a166048023699457e27bd405fd2eb09b5db04` |
| Archive member `assets-main/LICENSE` | 1,075 | `46e64d1eda93b16d8b91186a72262dbba3a07835fa31979c839503f6ef41f4ca` |

The archive's license is reproduced byte-for-byte in
`MewtwoStatue.SOURCE-LICENSE.txt`: MIT License, copyright (c) 2024 Sudhanshu
Ambastha. This records the notice supplied with the archive; it does not infer
additional asset-specific or Pokémon character rights.

## Editable source and reproduction

Blender 5.1.1 retains the supplied 88-bone rig and packed source images in a
hidden source collection, plus nine editable stone parts in a separate visible
collection. The sampled pose is frame 1 of
`pm0150_00_00_00000_defaultwait01_loop`. The tail's existing joints are rotated
into a compact loop without stretching the bones. Stone colors, subtle cavity
shading, a chamfered plinth, and the `150` plaque are authored additions.

Extract the original archive member and decode its Draco compression with
glTF-Transform 4.5.1 before running the script:

```sh
gltf-transform copy 150-original.glb 150-decoded.glb
blender -b --threads 4 --python mewtwo_statue.py -- 150-decoded.glb OUTPUT_DIR --render
```

The script writes `mewtwo-statue.blend`, `MewtwoStatue.json`, validation metrics,
camera metadata, and eight fixed-angle PNG views. The checked-in Blender file
is the same editable sculpture saved as `MewtwoStatue.blend`, without review
lights, cameras, or floor. Source GLBs are not required in the repository.

## Export and fit verification

The exported single-mesh visual has 4,214 triangles and 12,642 triangle-corner
vertices, opaque vertex colors, no exported collider, no degenerate triangles,
and its plinth floor at Unity Y=0. Unity dimensions are
1.973001 × 3.972135 × 1.741711 (width × height × depth).
The reviewed export SHA-256 is
`cfad252f772a5b09841c59a71bd6b1f017630db50075fadca953e4b20d53c427`.

Uniformly fitting the whole visual envelope into the native `ShrineMaoi`
render bounds gives scale 2.045536 and dimensions 4.035846 × 8.125146 ×
3.562733 m. The foundation then measures 3.4365 × 2.6183 m. Because the tail
makes the visual bounds asymmetric, centering the whole visual places the
plinth center −0.1029 m across and +0.4722 m forward relative to the old visual
center. Including the native visual/collider center difference, the plinth
center is approximately +0.5926 m forward of the native collider center:
the plinth extends about 0.535 m beyond the collider at the front and 0.016 m
at one side. Whole-envelope fitting is intentional to contain the tail within
the old visual envelope. Native collision, interaction, rewards, icons, and
outline behavior remain the runtime integration's responsibility and are
preserved; no larger replacement collider is supplied.

All eight Blender views were inspected for the face, hands, feet, tail loop,
plinth, and ground contact. Small facet seams remain at a few source surface
boundaries; the reviewed geometry was retained. Actual Unity and device
verification are recorded separately by the integration workflow.
