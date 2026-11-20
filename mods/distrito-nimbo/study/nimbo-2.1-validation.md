# Nimbo 2.1 validation

Updated 2026-11-20 for OpenBonk **0.1.18-beta / version code 20** and the
optional Pokémon **2.1.0 / content API 4** package. Terrain, pacing, package
import, desktop shrine review, launcher UI QA and the final three-stage
Android campaign passed. The signed APK launches through normal **Play**;
the Mewtwo shrine renders correctly, saves its capture and grants its native
reward exactly once. The final artifact hashes are recorded below.

Local evidence paths are relative to `build/distrito-nimbo/` in the producing
workspace. Logs, captures and release binaries are not embedded here.

## Pacing

The shipped Forest stage assets use 600, 540 and 480 seconds. Nimbo 2.0's
manifest sets `stageDuration: 420`, and the existing map recipe applies that
value to all three cloned timelines. Version 2.1 sets the existing field to
540; no timing schema or content API change is needed.

| Scheduled stage time | Terminal / Forest 1 | Interchange / Forest 2 | Relay / Forest 3 | Total |
|---|---:|---:|---:|---:|
| Native Forest | 10:00 | 9:00 | 8:00 | 27:00 |
| Nimbo 2.0 | 7:00 | 7:00 | 7:00 | 21:00 |
| Nimbo 2.1 | 9:00 | 9:00 | 9:00 | 27:00 |

The 2.0 clock was 22.2% shorter overall than Forest's. These are stage clocks,
not measured run lengths: pauses and early boss interactions affect elapsed
play. The recipe scales inherited event times, event durations and the enemy
reinforcement interval together. Version 2.1 uses factors of 0.9, 1 and 1.125
against Forest; event times below include runtime rounding to whole seconds.

| District | Swarms | Minibosses | Reinforcement check |
|---|---|---|---|
| Terminal | 3:36, 6:18 | 2:42, 7:12 | 54 s |
| Interchange | 3:00, 6:00 | 2:00, 7:00 | 60 s |
| Relay | 2:49, 4:30 | 1:41, 5:38 | 67.5 s |

Encounter rosters, enemy statistics, Lua source, attacks and rewards are
preserved by the packager. The checked final ZIP also sets
`moaiVisual: "MewtwoStatue"` in all three environments. Bundle, desktop and
final Android shrine checks passed.

## Elevation findings and change

Nimbo retains block widths of 26/28/30 m and rises of 6/7/8 m, versus Forest's
40/44/48 m and 20/22/24 m. The native height function ignores `slopeStrength`.
With the package's `flatMapBias: 0`, each new ramp run is pulled toward zero
height, so most flat terrain concentrates on a few levels. Merely reducing
the native rises made that concentration less visually distinct.

The new generator applies only when a streetscape profile is present. A
seeded broad height field targets levels from −4 to +4; the route approaches
each target by single-step ramps. Heights persist through turns and branch
junctions. Only straight, single-child tiles can ramp, preserving the maze's
connected routes. The 12 m crossing and spawn/boss clearance rules remain.
The native Forest height function and profile-free dispatch are unchanged.

An isolated Mono harness compiled the actual `Maze`, `NodeTree` and both
height functions with a recording tile sink. It compared seeds 0–999 plus
20261119, 563994575 and 1782247032 on a 15×15 grid at volatility 0.8.
This measures generated layout, not rendered pixels or performance.

| Metric across 1,003 seeds | Previous heights | Terraced heights |
|---|---:|---:|
| Mean flat-terrain height span, in steps | 5.44 | 7.09 |
| Minimum / maximum flat height span | 2 / 11 | 4 / 8 |
| Mean levels containing at least 10 flat tiles | 3.89 | 6.39 |
| Minimum populated levels under that criterion | 2 | 4 |
| Mean share of flat tiles on the largest level | 41% | 25% |
| Mean ramp tiles out of 225 | 57.75 | 42.64 |

For seed 42, the old flat-level counts were `−3:3, −2:4, −1:86, 0:61, 1:20`.
The new counts are `−3:15, −2:26, −1:52, 0:20, 1:47, 2:16, 3:10, 4:7`.
The new layout creates sustained upper and lower districts without increasing
individual ramp steepness. Evidence: `elevation-study/Measure.cs` and
`elevation-study/distribution.txt`.

## Completed checks

- **Actual terrain geometry:** `release-018/terrain-check.log` records
  `OpenBonkStreetLayoutCheck.Run` PASS: **2,883 combined-collider tree
  connections, 3,774 deterministic frontages, 13 unique maps**. The check
  samples the center and both shoulders of each connection, verifies floor
  continuity and absence of blocking walls, and preserves 12 m lanes and
  clear entry/boss tiles. It uses the shipped tile meshes and a synthetic
  full-size frontage envelope, not the final Mewtwo or shop artwork.
- **Elevation regression:** the same Unity check verifies several inhabited
  elevation levels, the ±4-step bounds, deterministic repeat generation and
  the exact native Forest seed-42 flat-height distribution. The odd-width,
  even-grid case retains shader-grid alignment.
- **Timeline regression:** the same Unity check verifies nine-minute cloned
  timelines, scaled wave times/durations and reinforcement checks, and
  unchanged native Forest 10/9/8-minute stage assets.
- **Shrine replacement geometry:** `release-018/moai-check.log` reports
  six native-prefab fit cases, using two synthetic +Y-up shapes at three
  rotations. The check verifies upright orientation and floor alignment,
  unchanged native collider references/bounds, icon references/renderers,
  interaction state and transforms, plus unchanged Forest/undeclared stages,
  duplicate-replacement prevention and unchanged source/prefab data. This
  check did not exercise the real Mewtwo asset or award an in-game reward.
- **Mewtwo art and bundle:** `release-018/bundle.log` records the Android
  prop bundle PASS, including `MewtwoStatue`. The bundle in the final ZIP
  exactly matches `release-018/bundle/nimbo-props` (SHA-256
  `7f9972186cb034eb17c935a7697594471958cfdbf8d4de513acd54c64655b57d`).
  `release-018/unity-statue-review/review.json` records **one asset, four
  rendered views**, with 12,642 vertices, 4,214 triangles, the
  `OpenBonk/NimboProp` shader and no added colliders. The four-view contact
  sheet was inspected: the stone Mewtwo, tail and pedestal render upright
  with opaque surfaces and shadows. This review used saved authoring assets
  on an AMD Radeon RX 7800 XT, not Android rendering. The review JSON and
  four image files define the scope of this filtered asset review.
- **Manifest and actual-package import:** `release-018/package-check.log`
  reports PASS for the real 1.4.0/API 3 → 2.1.0/API 4 ZIP update. It checks
  540-second stages, exact frontage widths/rises/coverage and curated names,
  `MewtwoStatue` in all three environments, all payload hashes, automatic
  enabling from both prior enabled states, retained history, recovery and
  restoration to API 3. The source 1.4.0 package is unchanged. The checked
  2.1 ZIP hash is recorded below.
- **Importer failure cases:** `StreetProfileCheck` passed 25 malformed
  update cases while preserving installed bytes and enabled state, including
  numeric/object and overlong `moaiVisual` values. A standalone moai-only
  environment imports with a bundle and rejects a missing bundle for the
  expected reason. API 3 still imports. This exercised current Java sources
  with modified manifests around the existing 2.0 bundle bytes; it does not
  validate asset rendering.
- **Launcher UI QA:** inspected captures show installed Nimbo first in the
  catalog with an orange **Update** action (`mod-update-orange.png`), an
  active progress spinner and **Wait…** during download (`mod-spinner.png`
  and `mod-spinner-second.png`), and **Installed** after completion
  (`mod-update-result.png`). `app-spinner.png` shows the app-download spinner
  and **Updating…** state; `app-spinner-second.png` shows the Android update
  installer prompt afterward. These files are in `release-018/` and were
  captured with `ui-qa.apk`, whose displayed version is `0.1.16-uiqa`, using
  the published 0.1.17 update. This UI review found no blockers.
- **Final Android startup:** the signed 0.1.18 APK identified below is
  installed and normal launcher **Play** boots successfully.
- **Final Android campaign:** `release-018/android-campaign.log` records
  PASS at **10:55:31.347 on 2026-11-20** for all three local encounters,
  Charizard/Gyarados/Rhydon boss fights, rewards and portals, both stage
  transitions and saved victory. The capsule break, boss projectiles and
  sweep/pulse warnings also pass. Frontage counts were 330/342/288 at seeds
  512640472/362917800/1960992490. The first-stage diagnostic records 330
  active/enabled renderers, with 160 in the camera frustum and 160 reported
  visible. The final log has no exception, failed-write, GLSL, uniform-limit
  or shader-error matches. `release-018/campaign-result.png` retains the
  final completed-tier result.
- **Final Android shrine and capture:** the same log records Moai PASS at
  **10:54:48.058**: Mewtwo visible and upright, the native head hidden,
  native icons/collider retained, first interaction accepted, second
  interaction rejected, and the native reward accepted. The saved and
  inspected `release-018/mewtwo-statue.png` is 933,251 bytes and shows the
  statue, visible Japanese storefronts and distinct street elevations.
  Its elapsed clock of 0:05 and remaining clock of 8:55 confirm the
  nine-minute first stage. All three nine-minute timelines pass the Editor
  check, and the second Android stage display was also inspected.

The final campaign ran on the `Google sdk_gphone64_x86_64` emulator with
Android 15 (API 35). The automated probe advances boss fights
programmatically; this validates campaign progression rather than a full
27-minute balance or endurance run.

To reproduce the actual-package check, run from the repository root:

```sh
/nix/store/0if41r2dp11y0v833p5yrpgr8mdanqjk-python3-3.13.15-env/bin/python3 \
  tools/mod-import-check/run-streetscape-package.py \
  /home/martin/megabonk/build/distrito-nimbo/private-pokemon/distrito-nimbo-pokemon-1.4.0.zip \
  /home/martin/megabonk/build/distrito-nimbo/release-018/distrito-nimbo-pokemon-2.1.0.zip \
  --java-home /nix/store/2cv1b5y6h029cvg07c944pq89fszwbya-openjdk-21.0.12.1+1 \
  --json-jar /home/martin/.gradle/caches/modules-2/files-2.1/org.json/json/20180813/8566b2b0391d9d4479ea225645c6ed47ef17fe41/json-20180813.jar
```

## Final artifacts

Sizes and SHA-256 were independently rechecked from `release-018/`. These
identify the final signed APK and package used by the successful campaign.

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| `OpenBonk.apk` | 56,634,090 | `ddc1ec357719737b41bac62da5955f13e8c7f0dfed9d642ea6a69b5cbc1134c3` |
| `distrito-nimbo-pokemon-2.1.0.zip` | 19,650,070 | `faeceb59555c2f653a7ef8910a6fbac1bd1455f903c937b2704111b6e4dd1133` |

No physical Retroid performance, thermal or crowded-encounter measurements
are claimed by the desktop, launcher or emulator checks.

Build instructions: [Japanese neighborhood 2.1](../README.md#japanese-neighborhood-21).
