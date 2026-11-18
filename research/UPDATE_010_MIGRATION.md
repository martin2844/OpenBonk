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

An optional, checksum-verified download only for matching 0.1.0 installations
could avoid increasing the APK for everyone. It would need a published patch,
updated Java and native size limits, failure and storage handling, and tests
on Android using the real launcher before release. Other variants of 0.1.0
data would still need separate validation or a game ZIP import.
