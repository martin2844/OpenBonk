# Updating data from beta 0.1.0

The published `v0.1.0-beta` Android APK uses version code 1. In a local
study its packaged preparation inputs and a compatible Linux game ZIP
reconstructed an OBB with SHA-256
`c367ae38db2ba09fc3ddb1de428bb1782664916905565bb68b1e850ac4ee09f5`.
That digest also appears in the published APK's dex and is the historical
`VIDEO_ONLY_NO_PROBES_SHA` in the repository's preparation tools. This is
evidence for the reconstructed file, not a test of all 0.1.0 installations.

A zstd patch from that OBB to the locally generated integrated candidate OBB
(`c7e714c4fe5be7d147e0225720dd4bc5d6d9f790432a016ab1ac99dedd67f60c`)
was applied successfully on the desktop and produced the target hash exactly.
The patch is 70,927,808 bytes, with SHA-256
`59f69047446e8d89a618e8387dbeeac146e5aa9aa048054e49fa956157de4a4a`.
It contains game-derived data and remains local. It is not included in the
candidate launcher, whose current patch limits are smaller.

The proposed launcher migration downloads a pinned 70,982,639-byte patch only
when a version-code-1 OBB matches the digest above. The downloaded patch
(`data-0.1.0-to-0.1.4.zst`, SHA-256
`5a2f4b34cef6d0d288e8ca63d895a5515880d0649c3b5145f60612b9a7583907`)
produces the 0.1.4 OBB; the existing bundled patch then produces the integrated
0.1.5 OBB. Each output is checked before publication and the original is
retained. The download is hosted with the 0.1.5 beta and the migration code
was published in the 0.1.6 beta.

On a Xiaomi 22126RN91Y, an isolated launcher package with the real 0.1.0 OBB
downloaded the patch, produced the expected 0.1.5 OBB, reached `Data ready`
and opened Unity. A second isolated build pointed to a missing asset: the
launcher recovered to manual import and the original OBB's hash was unchanged.
These tests did not install the original 0.1.0 APK or use a real player profile.
Other 0.1.0 data variants need separate validation or a game ZIP import.

An additional isolated-package test installed a version-code-6 build, played a
short Forest run, then installed a version-code-7 build over it. The original
and copied OBBs had the same expected SHA-256, `progression.json` and
`stats.json` were byte-identical before and after the update, and Unity opened.
The exact production-signed APKs and a user's personal profile were not used.
