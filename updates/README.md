# Android update metadata

`android.json` points to the current public APK on GitHub Releases. OpenBonk checks it at launcher startup at most once per day and offers an explicit check in the launcher. The current public beta predates this feature and will not update itself; users must install the first release containing the updater manually.

For each **validated, distributable** APK release:

1. Build with a monotonically increasing `OPENBONK_ANDROID_VERSION_CODE`, the existing public application ID and the same signing certificate as the previously published APK. Keep the signing key private. For example, the release after version code 1 needs code 2 or greater.
2. Test an upgrade from the public version, including saves, mod state and game-data compatibility. OpenBonk copies an older OBB only when its hash is accepted by the new runtime. If the game data is incompatible, users need to prepare or import compatible data again.
3. Upload the APK as a GitHub release asset. Once it is publicly accessible, update `android.json` with its exact tag, version code, HTTPS GitHub release-asset URL, byte size and lowercase SHA-256, then commit and push the manifest. Do not advertise a candidate, game-data-containing research APK, or unverified build.

The app verifies the downloaded size and SHA-256, Android package ID, version code and signing certificate before passing the file to Android's installer. The user must authorize installations from OpenBonk and confirm the update. GitHub hosts metadata and files; private Steam game files and profile data are not uploaded.
