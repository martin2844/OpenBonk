# GitHub issue audit — Android 0.1.19

Audit date: 2026-11-20. Target release: **0.1.19-beta, version code 21**.
The inventory in `build/issue-audit-019/issues.json` contains all 24 issues:
7 open and 17 already closed at the start of this review. The counts describe
that snapshot, not the state after release publication.

## Open issues reviewed

| Issue | Disposition | Evidence and limits |
| --- | --- | --- |
| [#26](https://github.com/martin2844/OpenBonk/issues/26) — Aim/Slide camera drag | Requested behavior implemented | Added camera drag to the touch Aim and Slide buttons, preserving their held actions and separate pointer ownership. Jump retains the same behavior. Tests cover simultaneous actions, cancellation and focus reset. |
| [#25](https://github.com/martin2844/OpenBonk/issues/25) — black map/chest panels | Compatibility fix; affected-device outcome unverified | Screenshots show black minimap, full-map and chest-preview panels while the world renders. Added supported RGBA8 color, supported depth **with stencil**, allocation checks and recovery for those render textures and the enemy preview. Dimensions, alpha and bindings are retained; unrelated targets/world cameras are excluded. The reported Redmi Note 8 Pro driver failure has not been reproduced. See [UI target evidence](ui-render-target-compatibility.md). |
| [#10](https://github.com/martin2844/OpenBonk/issues/10) — held jump and touch camera | Existing fix; regression checked | Queued first-physics-step jump handling was already introduced in `d0f8d49`. Added coverage for held and short-tap presses before render sampling, with no automatic repeat. Free screen dragging is already available. |
| [#12](https://github.com/martin2844/OpenBonk/issues/12) — Moto G34 menu slowdown | Existing menu fix; device performance unverified | Report concerns 0.1.1. Current menu code disables its post-processing layer, HDR and MSAA. Expanded checks cover fresh menu instances, removed effect command buffers, live settings changes, and unchanged shared profiles and preview/gameplay cameras. No Moto G34 FPS improvement is established. |
| [#21](https://github.com/martin2844/OpenBonk/issues/21) — Moto G Stylus menu slowdown | Existing menu fix; device performance unverified | Also reported on 0.1.1; the maintainer directed the reporter to a newer release. The same menu checks pass. No Moto G Stylus FPS improvement is established and no additional quality reduction was introduced. |
| [#5](https://github.com/martin2844/OpenBonk/issues/5) — crash after relaunch | Not reproduced; original cause unknown | Current code includes previously shipped startup cache refresh and permission-dialog fixes. Normal Play and force-stop → launcher → Play pass on the final APK, reusing imported data and retaining progress. This does not identify or confirm a fix for the reported Ayn failure. |
| [#22](https://github.com/martin2844/OpenBonk/issues/22) — rejected game ZIP | Unsupported archive; no importer defect established | Supported Windows/Linux ZIP validation fixtures and `SteamZipDiagnosticCheck` pass. A third-party archive running on PC does not establish that it contains the supported files required by the importer. No validation bypass or support for unknown archive variants was added. |

## Previously closed issues

All 17 retain their existing status. Review found no reason to reopen them:

- **#1, #3, #4, #8, #24:** resolution controls, menu rendering changes and the
  in-game FPS cap are present. Historical Retroid/Xiaomi samples do not prove
  performance on the devices in other reports.
- **#2, #6, #9, #15, #18:** existing touch movement/camera separation,
  screen dragging, layout controls and Jump-button dragging remain available;
  the current touch checks cover their shared input path.
- **#11, #14, #16, #20:** retain their prior resolutions for mod discovery/import
  compatibility, launcher updates/data reuse and boss-portal placement. These
  are not newly claimed fixes in 0.1.19.
- **#7:** fixed-height/custom resolution choices and native aspect ratio are
  present. Comments explicitly deferred forced 16:9 pending the choice between
  bars and cropping; the audit does **not** mark every suggestion implemented.
- **#17:** multiplayer remains not planned. **#19:** another port and a social
  channel request are outside this bug-fix release.

## Completed validation

- `build/issue-audit-019/editor-checks.log` records PASS for
  `AndroidTouchInputCheck`, `AndroidMenuPerformanceCheck`,
  `AndroidPerformanceSettingsCheck` and `SettingsEffectsPortCheck`.
  The checks cover touch timing/ownership, menu effect isolation, resolution
  arithmetic and application of FPS/graphics settings.
- `build/issue-audit-019/ui-target-check.log` records the complete
  `UiRenderTargetCompatibilityCheck` PASS under Unity 2023.2.22f1 with
  Xvfb/llvmpipe/OpenGLCore: all four imported targets render color/alpha, retain
  stencil and shared bindings, recover after `Release`, and leave an unrelated
  camera unchanged. This separate rerun corrected the original control
  fixture's assumption; the earlier combined log is not an all-tests PASS.
- Supported Windows/Linux ZIP fixtures and `SteamZipDiagnosticCheck` pass.
  These validate supported layouts/content and rejection diagnostics; they do
  not validate the reporter's third-party archive.

## Final APK and Android validation

`build/issue-audit-019/OpenBonk.apk` is signed and installed as **0.1.19-beta,
version code 21**, with the existing signing identity. The following checks
use this artifact:

| Artifact | Bytes | SHA-256 |
| --- | ---: | --- |
| `OpenBonk.apk` | 56,658,666 | `d8076bf12539c2dfc8e4b6fe971600cbf46c9ef9dec0f97335ba6fbc1908a13e` |

Normal launcher **Play** and **force-stop → launcher → Play** both reached the
menu on `Google sdk_gphone64_x86_64`, Android 15/API 35. Evidence:
`build/issue-audit-019/launch-final-first.log`, `launch-final-reopen.log`,
`launch-final-first.png` and `launch-final-reopen.png`. Both screenshots were
inspected: the menu shows 0.1.19-beta and retains currency 10. Installed Distrito
Nimbo remains present. Imported data is reused without reimport; the migrated
`main.21` OBB retains SHA-256
`5e57197c6ed213095b3dc66fb518b9d1cfd6318d8961baed50a7a187383e445d`.

`build/issue-audit-019/ui-target-android.log` records all four UI checks passing:
minimap at **11:29:28.068**, full map at **11:29:28.941**, chest preview at
**11:29:30.815**, and chest allocation recovery at **11:29:31.212**. The overall
PASS is **11:29:31.535**. Actual rendered pixels vary in color, targets use
RGBA8 with D24 depth/stencil, and UI references remain valid. The full map has
both direct and material-based texture bindings; the active material fallback
is included in the check. The GPU is Android Emulator OpenGL ES Translator
(Google SwiftShader), using OpenGLES3.

All six captures were pulled to `build/issue-audit-019/`:
`ui-target-minimap-pixels.png`, `ui-target-map-pixels.png`,
`ui-target-chest-pixels.png`, `ui-target-chest-recovered-pixels.png`,
`ui-target-map.png` and `ui-target-chest.png`. The full map and chest-panel
screenshots were visually inspected and render correctly.

All seven touch checks also pass on this APK in
`build/issue-audit-019/touch-final-android.log`: held/tap jump timing at
**11:30:43.516**, multitouch at **11:30:45.832**, dual-stick ownership at
**11:30:45.833**, Jump/Aim/Slide camera dragging at **11:30:45.834–836**, and
focus reset at **11:30:46.731**. Both jump presses reached the first physics
step in **0.0200 s**, matching the fixed step; continuing to hold after landing
did not jump again. These exercise actual UI handlers with supplied motion,
not physical touchscreen-driver timing. `touch-final.png` was inspected and
shows the Forest world, touch controls and minimap.

No reported Mali, Motorola or Ayn device has been tested in this audit.
Emulator checks do not establish performance or driver
compatibility on those devices, long-run stability, or the original cause of
the startup report.
