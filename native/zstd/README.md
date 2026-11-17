# Beta game-data migration

The 0.1.1-to-0.1.2 patch lets the Android launcher reuse a compatible 0.1.1
OBB without asking the player to import game files again. The launcher checks
the source, patch and destination SHA-256 hashes, writes to a new file, and
keeps the original. If migration cannot complete, manual import remains
available. The patch is specific to these two exact data files; future builds
with different game data need their own tested migration.

To build the ARM64 decoder, set `ANDROID_NDK_HOME` and run
`bash native/zstd/build.sh NEW_OUTPUT_DIRECTORY`. Pass the resulting
`libopenbonk_migration.so` to `tools/build-generated-audio-launcher.py` with
`--obb-migration-library` when building with the pinned 0.1.2 OBB. The build
checks the patch and target hashes. Zstandard's source archive and license are
included here; its SHA-256 is verified by the build script.

The Android release workflow verifies the published APK's size and SHA-256,
reads its version code, and updates `updates/android.json` so launchers can
discover it. Check that the workflow succeeds after each release; it can also
be run manually for a published tag. As a local fallback, run
`tools/update-android-manifest.py` with the tag, signed APK and
`--aapt /path/to/aapt` (plus `--runner` if needed), then commit and push the
resulting manifest.
