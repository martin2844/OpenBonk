# Nimbo District 2.2 validation

Updated 2026-11-20 for OpenBonk **0.1.20-beta** and the optional Pokémon
**2.2.0 / content API 4** package. The displayed map and package name is now
**Nimbo District**. Stable IDs remain unchanged so existing installs, progress
and mod history can be reused.

Evidence paths are relative to `build/distrito-nimbo/release-020/` in the
producing workspace; logs and captures are not embedded in this document.

## Schedule correction

Version 2.1 flattened Forest's 10/9/8-minute stages into three nine-minute
stages. Although the total remained 27 minutes, this moved the first and third
stages' swarms/minibosses, scaled their event durations and reinforcement
intervals, and changed the map-level duration used by enemy density.

Version 2.2 declares `stageDurations: [600, 540, 480]` while keeping the
map-level `stageDuration: 600`. The cloned stages now retain the native Forest
schedule and 60-second reinforcement checks exactly:

| District | Stage clock | Swarms, elapsed time | Minibosses, elapsed time |
| --- | --- | --- | --- |
| Terminal | 10:00 | 4:00, 7:00 | 3:00, 8:00 |
| Interchange | 9:00 | 3:00, 6:00 | 2:00, 7:00 |
| Relay | 8:00 | 2:30, 4:00 | 1:30, 5:00 |

These are scheduled stage times, not a measured run duration. Stage bosses
remain tied to their native altar interactions. `OpenBonkMapScheduleCheck`
passed in `schedule-check.log`: all four events and their durations per stage,
the 600-second density denominator, independent timeline/event clones,
unchanged Forest assets, legacy scalar-only recipes, and rejection of eight
invalid duration arrays.

## Fearow whirlwind attack

Fearow now declares `attack: "whirlwind"`. The enemy attack reuses the native
Tornado weapon's visual meshes, materials and particles while removing player
weapon behaviors and colliders. It uses an enemy-owned damage path and cannot
hit other enemies or award player weapon damage credit.

The implementation staggers cooldowns between 12 and 16 seconds without
consuming map/spawn RNG. A 1.2-second direction warning precedes a dodgeable,
non-homing whirlwind moving at 8 m/s for at most 4.5 seconds. A whirlwind can
hit the player once; at most four can exist simultaneously. It stops at
obstacles or missing floor and cancels when its owner dies, is reused or
changes allegiance. Simulation respects gameplay pause/time freeze.

`EnemyWhirlwindCheck` passed in `terrain-combat-check.log`, covering native
visual identity, ranged enemy spacing, warning/cadence, locked travel,
player-only single damage, dodging, elevation exclusion, pause, wall impact,
lifetime, owner death/reuse/charm, the active-count cap and preservation of the
original weapon prefab. The Android observation is recorded below; neither
check establishes crowded-combat balance or endurance.

The clone explicitly activates its native visual root and disables particle
GPU instancing while retaining the same meshes, materials and particle
settings. `whirlwind-visible-particles-check.log` extends the Editor check
with active, nonzero-size/alpha particles, nonzero bounds, ordinary rendering
and 5 ms simulation increments. A reference/bounds-only Android check was
insufficient visual evidence; the final probe also requires live visible mesh
particles and captures the effect after the player sidesteps its locked path.

## Terrain, streets and new building kit

The three profiles now use **28/30/32 m tile widths** and **8/9/10 m rises**.
Each height change still uses one connected ramp tile; the approved grades
stay below 0.313. The terrace target spans −5 to +5 levels and changes somewhat
more frequently across the district. Turns and branches retain their inherited
height. Native Forest dispatch and generation are unchanged.

The isolated height study compiles the actual `Maze`, `NodeTree` and height
functions against a recording tile sink. It compares 1,003 seeds on a 15×15
grid at volatility 0.8; it measures layout, not GPU performance:

| Mean metric | Nimbo 2.1 | Nimbo 2.2 |
| --- | ---: | ---: |
| Flat-terrain height span, in steps | 7.09 | 7.90 |
| Ramp tiles out of 225 | 42.64 | 47.62 |
| Elevation levels containing at least ten flat tiles | 6.39 | 6.39 |
| Share of flat tiles on the most populated level | 25% | 25% |

The ramp count increases by 11.7%, while the larger physical rises make those
changes more visible. Evidence is under `landscape/`: `Measure.cs`, the
previous height-function snapshot/executable, and `elevation-final.txt`.
The native seed-42 Forest distribution remains exactly unchanged.

`NimboTerrain` now uses neutral-gray asphalt, contrasting curbs, coherent
green pockets, patchy grass colors and dirt/gravel paths. A continuous world
field is shared with placement logic, keeping taller buildings in built-up
blocks and low grass in park corners. The shader detects the shallow city
ramps correctly, so their avenue follows the ramp direction. These surface
changes add no terrain obstruction: the full 12 m crossing stays available
for combat even where the visible path is narrower.

New original assets supplement the existing low-rise shops and street props:

| Asset | Height | Triangles | Solid box colliders |
| --- | ---: | ---: | ---: |
| `NimboApartmentCorner` | 17.05 m | 3,444 | 5 |
| `NimboShopTower` | 20.525 m | 3,360 | 5 |
| `NimboGrassPatch` | 0.577 m | 864 | 0 |

The buildings have separate floor tiers, framed/recessed opaque glazing,
ground-floor shops, cornices, rooftop service rooms and caps; the apartment
also has balconies. Rooftop structures contact their supporting deck, and
their solid colliders fit inside the visual bounds. Awnings and balcony
apertures remain nonblocking. Horizontal placement limits stay at 8 m; the
vertical envelope expands to 24 m. Existing source props are preserved.

Editable sources are `authoring/landscape_additions.py` and
`authoring/NimboLandscapeKit.blend`. The helper exports
`landscape/landscape-props.json`; `landscape/landscape-metrics.json` records
the source geometry and collider inventory.

## Completed geometry and visual review

- `OpenBonkStreetLayoutCheck` passed in `terrain-combat-check.log`:
  **2,883 actual-collider tree connections across 13 unique maps** and
  **3,702 deterministic frontage placements**. It checks center/shoulder
  floor continuity, blocking-wall absence, populated elevation levels,
  12 m lanes, clear spawn/boss tiles, odd-width/even-grid alignment,
  deterministic generation and unchanged Forest. A separate mixed-profile
  fixture produced 139 low-rise shops, 119 tall buildings and 80 grass patches,
  with taller buildings grounded and excluded from planted park corners.
- `bundle.log` records the final landscape bundle build PASS, including all
  three new assets and the existing kit/creatures. The script reopens the
  Android bundle and checks required assets, collider counts, shaders and
  draw modes.
- `asset-review/review.json` records **three assets, four views each** under
  llvmpipe/OpenGLCore. All three contact sheets were inspected: upright,
  grounded buildings with clear floor tiers and the low grass patch.
  `landscape/*-source.png` also records inspected Blender source views.
- `map-review.log` and `map-review/landscape-review.json` record a standalone
  review of the actual 2.2 ZIP's profiles, textures and bundle meshes with the
  production generator and shipped Forest terrain meshes. Android shader
  programs are rebound to the same source shaders for desktop rendering.
  There are **ten inspected images**: street, reverse, park, elevated and
  overhead views for stages 1 and 2 at seed 42. The images show gray roads,
  dirt paths/patchy green pockets, distinct taller fronts and real elevation
  changes without a visible blocker in those views.

| Actual generated review | Flat tiles / ramps | Frontages | Taller buildings | Grass patches |
| --- | ---: | ---: | ---: | ---: |
| Stage 1, seed 42 | 188 / 37 | 338 | 45 | 72 |
| Stage 2, seed 42 | 188 / 37 | 328 | 66 | 70 |

The initial combined review attempt failed because `NewScene` unloaded the
Forest asset held in a managed local variable. The review checker now creates
its scene before loading the fixtures. The standalone rerun passed; this
required no production generation change. The combined log's earlier gallery
failure is not represented as an all-tests PASS.

## Import and compatibility checks

- `importer-check.log`: current Java importer accepts supported API 4 content
  and preserves installed bytes/enabled state across **34 malformed updates**.
  Standalone shrine visuals still require their bundle, and API 3 imports
  remain supported.
- `package-14-check.log` and `package-21-check.log`: actual **1.4.0 → 2.2.0**
  and **2.1.0 → 2.2.0** ZIP updates pass. Checks include the English name,
  native stage clocks, Fearow attack, Mewtwo shrine, exact new profiles and
  frontage names, all declared payload hashes, automatic enabling from both
  previous enabled states, retained history, recovery and restoration.
- `previous-client-check.log`: the 0.1.19 importer rejects the new whirlwind
  attack before installation, preventing silent use of the wrong stage
  clocks. Install OpenBonk **0.1.20-beta or newer** before updating this mod.

## Mewtwo placement on slopes

Expanded slope checks exposed an existing replacement bug: the code selected
the largest world-space bounding box, so a rotated minimap quad could outweigh
the actual head on a ramp. At an 8/28 grade and 19-degree roll, the quad's
world-box volume was 4,919 while the head's was 543. Mewtwo was attached to the
icon, leaving the native head visible. `moai-slopes-diagnostic.log` records
the failure with the native prefab.

Selection now uses the mesh-local bounding volume with scale applied. Planar
icons retain zero volume under rotation; the solid head is selected. Native
colliders, icons, interaction and rewards are preserved. The imported +Y axis
follows native +Z, which follows the sampled ground normal. The Android probe
now checks that alignment rather than requiring world-up on sloped ground.
The Editor regression passed 26 fits, including all three city ramp grades,
both 44-degree boundary directions, different rolls and off-center source
meshes; the final result is recorded in `moai-slopes-fixed.log`.

## Android and final release artifacts

The signed APK is version **0.1.20-beta / code 22**. It keeps the existing
beta signing certificate (SHA-256
`bb2167e814b376d57c1f6a685a9255953f07efe64a91725e56e59f6cebcd8774`).

| Artifact | Bytes | SHA-256 |
| --- | ---: | --- |
| `OpenBonk.apk` | 56,695,530 | `d39083737e6cc4e5f9243d47a70af1b0019d9d26389de40fbb5e16284df82b8f` |
| `nimbo-district-pokemon-2.2.0.zip` | 19,752,988 | `d43a974c0327776643faaf5f391bcc097041aed882b466758cef0f9c4282733d` |

Android checks use the signed release APK on an Android 15/API 35 emulator
with SwiftShader/OpenGLES3:

- APK installation preserved the existing game files, saves and enabled mod.
  The actual ZIP was imported through the launcher file picker and enabled
  automatically. `installed-mod-english.png` and `map-picker.png` show the
  English name in both interfaces. `final-startup.log` records normal startup.
- `android-fearow.log`: ordinary Fearow AI warned at 16.033 seconds and cast
  at 17.417 seconds, with no attack-clock acceleration. The final capture
  contains the native white funnel. Runtime checks found eight visible live
  mesh particles plus four smoke particles, native mesh/material identity,
  no inherited player projectile scripts/colliders, pause freeze and bounded
  cleanup. `fearow-whirlwind-warning.png` and `fearow-whirlwind.png` were
  inspected. The capture avoids hero occlusion and uses ordinary particle
  rendering; the earlier ring-only capture is not counted as visual proof.
- `android-schedule.log`: all three loaded schedules match Forest. All four
  stage-one event boundaries (180/240/420/480 seconds) dispatched exactly
  once through ordinary `EnemyManager.FixedUpdate`; no event fired early or
  while paused. The probe advances the stage clock for boundary testing.
- `android-campaign.log`: all three local encounters, native boss volleys,
  Gyarados sweep, Rhydon pulse, capsule break, district rewards, two stage
  transitions and saved victory passed. The Mewtwo check confirmed the
  original head was hidden, native icons/collision remained, and the reward
  succeeded exactly once. The probe triggers boss encounters and advances
  their defeat for QA; this is not a real-time 27-minute playthrough.
- The installed game-data OBB retained SHA-256
  `5e57197c6ed213095b3dc66fb518b9d1cfd6318d8961baed50a7a187383e445d`.
  No game-data reimport was needed.

Release artifacts are published under `v0.1.20-beta`; the APK update feed and
Nimbo catalog entry use the identities above. Install the APK before updating
Nimbo to 2.2.0.

Editor geometry and desktop captures do not establish physical-device FPS,
thermal behavior, crowded-combat performance or a full 27-minute balance run.
