# Optional UI mod integration candidate

The candidate combines Chest Tracker, Minimap Zoom and Toggle Everything with the existing runtime policies. These are independent adaptations with the limitations described in each mod's README; none is a complete port of its upstream inspiration. The catalog must not offer their hooks to releases older than this candidate.

Build their ZIPs reproducibly with `python3 tools/package-optional-ui-mods.py --output /path/to/output`. Each archive contains only `manifest.json` and `main.lua`, starts disabled, and is generated without upstream code or art. Candidate bytes tested on Android:

| ZIP | Bytes | SHA-256 |
| --- | ---: | --- |
| chest-tracker.zip | 451 | `ef3baee66c22806413446e0c1fd37e4a2bdfe4c6c8b80f5802d5c03a4cf52e93` |
| minimap-zoom.zip | 534 | `3a098b367ddbee5d1da3eee3a6199b402cb26cb653942e4f6cd5a63bac0d3d77` |
| toggle-everything.zip | 460 | `26e7a7dbd0534d5efef2f3d782806771e8c5d8783d54c77837de2c7640030992` |

On a Xiaomi running LineageOS, an isolated package upgraded from 0.1.6 using `adb install -r`: the six files of a game-generated test profile were byte-identical immediately after update; the old OBB remained intact and the new OBB matched `2d39f7ded1fd3892d5e8391f4d7b98756afccad52c1196bc878395ffd125ff99`. Reconstruction from local Linux and Windows game files produced that same 374,365,431-byte OBB. Both sources were verified offline; this run did not re-import both original ZIPs through the device launcher.

All three exact ZIPs were imported through the device launcher and appeared disabled. Enabling them together loaded the three expected Lua hooks; Forest showed the chest counter and enlarged minimap. After disabling them and restarting, none of the hooks loaded, the counter disappeared and the minimap returned to its default scale. Toggle Everything's detailed filter behavior was previously tested on the device with a synthetic profile and diagnostic manifest; it was not repeated in this integrated launcher run. Physical controller input, extended gameplay, full-map navigation in this combined build, and remote catalog delivery of these three ZIPs remain untested.

Each embedded migration delta was reconstructed offline and tested in an isolated Android package for version codes 2–7. All produced the expected OBB hash and retained the original. The 0.1.0 remote route was tested with a separate intermediate APK using the same migration code, native library, and delta bytes: the real hosted patch and local delta produced the target hash while preserving the original. A deliberately invalid URL produced HTTP 404 and left the old OBB intact without creating the new one. The final dual-source APK was tested for the 0.1.6 upgrade; the 0.1.0 and older-version device tests used the intermediate Linux-source APK, not the final APK.
