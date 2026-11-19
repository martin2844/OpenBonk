# Japanese neighborhood 2.0 validation

Validation record updated on 2026-11-19 for OpenBonk **0.1.17-beta** and the
optional Pokémon **2.0.0 / content API 4** package. The final artifact pair passed
the three-stage Android campaign. Local evidence paths below are relative to
`build/distrito-nimbo/` in the producing workspace; logs, captures and binaries
are not embedded in this note.

## Terrain and population

The former 1.4.0 package inherited Forest's large tiles and elevation steps,
removed Forest's decorative layers, and requested five scattered prop types.
The final package uses six Japanese frontage clusters and shallower steps,
with the original random-object list removed. Both use a 15×15 tile grid.

| District | Former tile width / rise | Final width / rise | Former scatter requests | Observed generated frontages |
|---|---:|---:|---:|---:|
| Terminal | 40 / 20 m | 26 / 6 m | 146 | 308 |
| Interchange | 44 / 22 m | 28 / 7 m | 126 | 298 |
| Relay | 48 / 24 m | 30 / 8 m | 100 | 284 |

Former counts are manifest placement budgets, not measured successful spawns.
Observed counts are actual cluster placements in the final Android campaign,
at seeds `93314851`, `846003426` and `1142177623`, respectively;
each cluster contains multiple props.
These are different counting units, so the table does not establish a density
multiplier. Final frontage coverage settings are 0.95, 0.90 and 0.85.

Evidence: the 1.4.0 and final ZIP manifests, Forest `ParamForest1/2/3.asset`
parameters, and `neighborhood/android-release-campaign.log`.

## Checks completed

- **Terrain and access:** `street-check-final.log` reports 2,883 actual-collider
  tree connections across 13 generated maps, 3,504 deterministic frontages,
  clear spawn/boss areas and 12 m lanes, with Forest unchanged.
  `recipe-check-neighborhood.log` also passes cloned-parameter preservation
  and invalid/incompatible recipe rejection.
- **Props:** `neighborhood/props.validation.json` records six new clusters,
  17,103 triangles and 30 explicit solid boxes, all within visual bounds plus
  0.125 m. The gate retains a 3.4 m clear opening. New geometry and ten legacy
  props remain unchanged; new colors are opaque and have baked ambient
  occlusion. Repeated preparation produced identical prop JSON bytes.
- **Creatures and playback:** ten species use UV atlases and 312 shared indexed
  mesh frames. `private-pokemon/animation-validation.json` verifies articulated
  deformation, including bird wing travel. `animation-check.log` passes
  12 fps playback at 15/30/60 render Hz, hitches, pause/resume, speed and clip
  changes, pooled resets, deterministic phases and RNG independence.
- **Unity asset review:** `neighborhood/unity-review-final.log` reports PASS
  after generating 80 creature pose views and 24 views of the six prefabs.
  `neighborhood/unity-review/review.json` identifies the meshes, atlases and
  collider counts. This review used an AMD Radeon RX 7800 XT.
- **Android visibility correction:** prop and creature materials now disable
  GPU instancing; terrain retains its bounded 16-instance shader variant.
  `neighborhood/no-instance-current.png` shows visible street frontages,
  and `neighborhood/visible-boss.png` / `visible-boss3.png` show visible
  Gyarados and Rhydon with the surrounding props. These captures were inspected.
  The final campaign's spawn diagnostic records 308 active/enabled frontage
  renderers, with nine in the camera frustum and nine visible.
  The final ZIP also passed the actual-package import checker.
- **Final Android campaign:** `neighborhood/android-release-campaign.log`
  records all three local encounters and landmarks, capsule break,
  Charizard/Gyarados/Rhydon projectiles, sweep/pulse warnings, all boss rewards
  and portals, both stage transitions, and saved victory. The district probe
  reports PASS at 21:26:41, with no GLSL, uniform or shader errors in the log.
  The result capture is `neighborhood/release-campaign-result.png`; the
  recorded run is `neighborhood/nimbo-release-campaign.mp4`.

The campaign ran on the Android 15/API 35 `sdk_gphone64_x86_64` emulator with
the ARM64 IL2CPP release player. This is functional evidence, not a Retroid
performance result. Frame rate, thermal behavior and crowded encounters on a
physical Retroid device remain unverified. The desktop view-generation pass
also does not establish mobile performance or exhaustive visual correctness.

## Current artifacts

File sizes and SHA-256 were independently checked against `release-017/`:

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| `OpenBonk.apk` | 56,621,802 | `e246564b850385a641e28aa9d1b797847886420632ed5f68d064cdd19ca62e50` |
| `distrito-nimbo-pokemon-2.0.0.zip` | 19,459,525 | `2be530d1eba900face0fd6d65c755bccb4f5f96a457934ec026a858d94b204eb` |

Build and packaging commands are documented in the
[neighborhood workflow](../README.md#japanese-neighborhood-20).
