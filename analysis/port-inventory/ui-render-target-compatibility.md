# Android UI render targets: issue 25

Issue: <https://github.com/martin2844/OpenBonk/issues/25> (OpenBonk 0.1.13,
reported Redmi Note 8 Pro / Android 9).

The three supplied screenshots show the world terrain, trees, enemy and a world
chest rendering. The black areas are the chest-opening preview, circular
minimap, and large map panel. This is distinct from the Nimbo mesh-instancing
issue fixed previously.

The assembled source assets still contain these settings:

| Target asset | Size | Color format | Depth/stencil |
| --- | --- | --- | --- |
| MinimapTexture | 256 × 256 | R16G16B16A16_UNorm (24) | D32_SFloat_S8_UInt (94) |
| MapTexture | 1024 × 1024 | R16G16B16A16_UNorm (24) | D32_SFloat_S8_UInt (94) |
| ChestOpening | 1920 × 1080 | R8G8B8A8_UNorm (8) | D32_SFloat_S8_UInt (94) |
| EnemyDisplay | 256 × 256 | R8G8B8A8_UNorm (8) | D32_SFloat_S8_UInt (94) |

Sources are `Assets/RenderTexture/*.renderTexture` in the assembled workspace;
camera and RawImage references are serialized in `GeneratedMap.unity`,
`FinalBossMap.unity`, and `MainMenu.unity`. A source audit found no existing
runtime compatibility selection for these imported targets. The scene cameras
also enabled HDR/MSAA. Numeric format names are confirmed against the
[Unity 2023.2 enum source](https://github.com/Unity-Technologies/UnityCsReference/blob/2023.2/Runtime/Export/Graphics/GraphicsEnums.cs).
The imported desktop-format dependency is confirmed; a driver failure on the
reporter's device has **not** been reproduced.

`OpenBonkUiRenderTargets` now prepares these four exact target names on Android
scene load, including inactive cameras, and before subsequent camera renders.
It keeps each RenderTexture object, dimensions, filtering, sRGB choice and alpha
channel, so existing camera, RawImage and material references stay valid.
Color becomes RGBA8 after checking both rendering and sampling support. Depth
prefers supported D24_UNorm_S8_UInt, with supported D32_SFloat_S8_UInt as the
alternative; stencil is never silently discarded. These LDR UI cameras use no
HDR intermediate or MSAA resolve, and the targets use no mipmaps or memoryless
attachments. Creation is checked, lost allocations are recreated in place,
and repeated failure reports are throttled. Other targets and world cameras
are untouched.

Validation entrypoints:

- `UiRenderTargetCompatibilityCheck.Run` (Unity Editor with graphics): clones
  all four imported assets; verifies GPU allocation, pixel color/alpha,
  dimensions, stencil, stable RawImage/material/camera references, repeated
  preparation without reallocation, recovery after `Release`, and exclusion
  of an unrelated camera. It does not change the imported assets.
- Debug APK intent extra `megabonk_ui_targets_probe=true`: after entering normal
  gameplay, checks actual minimap, full-map and chest-preview pixels and active
  UI bindings, then releases the chest target and checks automatic recovery.
  It saves `ui-target-*-pixels.png`, `ui-target-map.png` and
  `ui-target-chest.png` under `Application.persistentDataPath`. The probe is
  excluded by `OPENBONK_BETA_NO_PROBES` unless explicitly enabled with
  `OPENBONK_UI_TARGET_QA`, and inactive in ordinary launches even in QA builds.

Validation result: `UiRenderTargetCompatibilityCheck.Run` passed all four
imported targets and the unrelated-camera control under Unity 2023.2.22f1,
Xvfb / llvmpipe / OpenGLCore. Release integration log:
`build/issue-audit-019/ui-target-check.log`. This verifies color/alpha pixels,
stencil retention, allocation recovery and stable bindings in that environment.

Final Android validation passed on 2026-11-20 using Android 15 / API 35,
`sdk_gphone64_x86_64`, Google SwiftShader, OpenGLES3. All four targets were
created as R8G8B8A8_UNorm with D24_UNorm_S8_UInt, including the EnemyDisplay
menu target. The gameplay probe recorded:

| View | Camera frames | Bright pixels | Distinct quantized colors | Result |
| --- | ---: | ---: | ---: | --- |
| Minimap | 44 | 1,148 | 87 | PASS; one visible UI binding |
| Full map | 67 | 31,589 | 138 | PASS; two bindings, one visible |
| Chest preview | 6 | 10,058 | 259 | PASS |
| Chest after allocation release | 7 | 9,700 | 263 | PASS; same target instance ID 45628 |

The full map's visible pause-panel RawImage inherits its texture from
`MapFogOfWar._MainTex`; its direct `texture` field is null. The inactive
hold-map overlay has a direct texture binding. The final probe checks
`RawImage.mainTexture`, which UGUI actually submits to CanvasRenderer, together
with canvas activity, culling, inherited alpha, nonzero panel size, camera
frames and pixel variation. This corrects the earlier probe's false failure
from counting only direct texture fields; it required no rendering change.

Overall Android PASS was logged at 11:29:31.535 in
`build/issue-audit-019/ui-target-android.log`. Six final screen/pixel captures
are retained as `build/issue-audit-019/ui-target-*.png`. Visual inspection of
the full screenshots confirmed the revealed map area, icons and fog, and the
3D chest preview.

Tested signed artifact: `build/issue-audit-019/OpenBonk.apk`, version
`0.1.19-beta` / version code 21, 56,658,666 bytes, SHA-256
`d8076bf12539c2dfc8e4b6fe971600cbf46c9ef9dec0f97335ba6fbc1908a13e`.

The reported Redmi Note 8 Pro / Android 9 device was unavailable. These Editor
and emulator results validate the compatibility path and actual UI output in
those environments; the reporter's Mali-driver outcome remains unverified.
