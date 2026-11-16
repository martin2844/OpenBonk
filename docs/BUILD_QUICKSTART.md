# Build a local Android research APK

This guide is for developers on Linux x86-64. It builds OpenBonk from this checkout and your own compatible Steam game files. **The resulting APK contains game-derived data and is for local research, not redistribution.** It is not a byte-for-byte rebuild of the published beta. For the release installation instructions, see the [tester guide](../BETA_TESTER_GUIDE.md); for detailed build evidence and limitations, see [BUILDING.md](BUILDING.md).

## Prerequisites

- Git, Python 3.13 with `venv`, a C++17 compiler, `tar`, and substantial free disk space for Unity and intermediate files. Allow at least 35 GB for a dual-platform build; local tests recovered space during the run by removing verified intermediate Gradle exports. The output must be on a filesystem that permits executing generated scripts; a `noexec` mount caused Gradle's Android build to fail in a local test.
- [Unity 2023.2.22f1](https://unity.com/releases/editor/archive) with Android Build Support, its SDK, NDK and OpenJDK. Activate Unity in Unity Hub before building.
- [AssetRipper 2.0.0](https://github.com/AssetRipper/AssetRipper/releases/tag/2.0.0), Linux x64 free edition. The executable must report `AssetRipper.GUI.Web 2.0.0`.
- A compatible **Linux Steam installation** of Megabonk. Optionally provide a compatible Windows installation as a second input to make a launcher that accepts either platform's Steam ZIP. Each installation needs its own `Megabonk_Data` directory. The build does not download the game.
- The [`org.json:json:20180813` JAR](https://central.sonatype.com/artifact/org.json/json/20180813). For example, download it from [Maven Central](https://repo.maven.apache.org/maven2/org/json/json/20180813/json-20180813.jar). The unmodified JAR has SHA-256 `518080049ba83181914419d11a25d9bc9833a2d729b6a6e7469fa52851356da8`.

The build creates its Lua bridge, host and Android texture converters, Crunch decoder, Steam ZIPs, Unity workspace and launcher from local inputs. It also uses Unity and Gradle dependencies; `--gradle-online` permits Gradle to download missing dependencies during launcher packaging. Unity's earlier build stages have their own dependency requirements. Game data, generated projects and APKs stay local.

When the build creates a Steam ZIP, its initial check verifies the required game files against the pinned inventory before starting the export. A supplied ZIP is checked against the corresponding local game files instead; this does not establish compatibility with every game version.

## Build

Clone the repository and create the pinned conversion environment:

```sh
git clone https://github.com/martin2844/OpenBonk.git
cd OpenBonk
python3.13 -m venv .venv
.venv/bin/python -m pip install -r tools/requirements-conversion.txt
```

To check the included Python tools before a long build, run:

```sh
.venv/bin/python -m unittest discover -s tools/tests
```

The suite uses temporary fixtures and does not build the APK or check gameplay. On the tested NixOS host it required `steam-run` before the Python command so that native libraries could load. A new Python 3.13 virtual environment installed these pinned requirements without the pip cache, passed all 336 tests and passed the build's initial check at commit `f875627`. The host still used existing Unity, Android tools and other local inputs; this is not a substitute for the build and device checks below.

In Bash, replace the example paths below. `OPENBONK_OUTPUT` must be a new path outside the checkout and game installations. The optional Windows directory must contain `Megabonk_Data`; omit the `--windows-game` line for a Linux-only research APK.

```sh
OPENBONK_GAME=/path/to/linux/Megabonk
OPENBONK_WINDOWS_GAME=/path/to/windows/Megabonk
OPENBONK_RIPPER=/path/to/AssetRipper.GUI.Free
OPENBONK_UNITY=/path/to/Unity/2023.2.22f1/Editor/Unity
OPENBONK_JSON=/path/to/json-20180813.jar
OPENBONK_OUTPUT=/path/to/new-local-build

build_args=(
  --game "$OPENBONK_GAME"
  --windows-game "$OPENBONK_WINDOWS_GAME"
  --assetripper "$OPENBONK_RIPPER"
  --unity "$OPENBONK_UNITY"
  --json-jar "$OPENBONK_JSON"
  --application-id com.example.openbonkresearch
  --output "$OPENBONK_OUTPUT"
  --include-rewired --quality-textures --font-fallback
  --resource-fallback --shadow-fallback --partial-rgba --shared0-fonts
  --gradle-online
)

# Checks initial paths, Unity startup, required compilers, executable Android tools, JSON JAR contents and Python conversion imports; it does not build or verify an APK.
.venv/bin/python tools/build-steam-research-launcher.py "${build_args[@]}" --check

# Runs the export, Unity builds, data conversion checks and launcher packaging.
.venv/bin/python tools/build-steam-research-launcher.py "${build_args[@]}"
```

To experiment with reconstructing additional exact texture fragments from the Steam ZIP, add `--partial-etc2` to `build_args`. It requires the `--partial-rgba` and `--quality-textures` options above and can increase preparation time. Its restoration stages have been checked on desktop and a Retroid for both Steam platforms, but a complete interactive launcher import using this exact build configuration has not been validated. The APK still contains assets and must remain private.

On systems that require `steam-run` for native binaries, add `--runner steam-run` to `build_args` and run Python in a compatible environment (for example, prefix both Python commands with `steam-run` if its conversion modules need libraries provided by that environment). One NixOS host also needed `libxml2.so.2` exposed to Unity through a local runner wrapper and a higher open-file limit (`ulimit -n 65536`); these are host-specific, not guarantees. The conversion options are experimental and can make first import take several minutes.

The initial check runs the configured Unity editor with `-version` through the same runner and requires `2023.2.22f1`. If it reports a missing shared library, fix the environment or runner before starting the build. This checks editor startup, not license activation, Android compilation or package downloads.

The initial check creates and removes a temporary file in the output's parent to test executable permission. If it reports `noexec`, choose a different output location; Android Gradle generates scripts that must run from the build directory.

For controlled update experiments, `--version-code 2 --version-name 0.1.1-beta` overrides the Android version metadata in each launcher produced by the build. Omit both flags to keep the Unity export's defaults. Android also requires the same application ID and signing certificate as the installed app for an in-place update. These flags do not sign the APK with a release key or establish equivalence with the published beta; keep test packages separate and export your profile before changing installations.

The local dual-platform project used to assemble the published 0.1.1 APK also enabled `--normal-fallback --animation-curve-fallback --nodraw-fallback --atlas-fallback --lightmap-fallback --emission-fallback --fox-fallback --emoji-fallback --partial-etc2 --partial-etc2-small`, in addition to the options in the example above. Those switches reconstruct additional data from Steam and are experimental. Subsequent changes to the research tools, generated inputs, package identity and signing can change the output, so these options are not a byte-for-byte reproduction recipe. The release was assembled from existing local intermediate projects and was not independently rebuilt from a clean checkout.

A later local clone at `276b8d0` completed a new dual-platform build without interruption with those switches, `--version-code 2` and `--version-name 0.1.1-beta`, using installed Unity, AssetRipper, dependency caches and local Steam data. The Windows input was extracted from an existing ZIP, not obtained directly on a Windows PC. Both platform chains produced the same final OBB SHA-256, and an independent checker used the plans inside the combined APK to restore that OBB exactly from each generated ZIP. On a Retroid Pocket 5, an isolated installation of that APK imported each ZIP in separate tests and produced the expected OBB. Both tests reached short Forest runs with Fox and enemy defeats; the Windows-data run reached level one, selected an upgrade and resumed. The temporary APK and copied ZIPs were removed. This is not a complete gameplay test, a clean second computer or a byte-for-byte rebuild of the published APK. An inline texture audit still found game-derived payload bytes, so the generated APK remains local.

For that build, the source checker found no changed, missing or unexpected C#, Java or shader files in its documented scope; the binary checker also found no mismatches against the local export and generated plugins. Those checks do not prove that every source was compiled, establish the provenance of all inputs or verify complete gameplay equivalence.

To compare ARM64 executable-section bytes and virtual addresses in two APKs, run `python3 tools/compare-apk-executable-sections.py /path/to/first.apk /path/to/second.apk`. Matching sections do not prove that ELF data, relocations, game metadata, assets or behavior match. The tool is intended for unextended little-endian ELF64 files and exits nonzero if sections differ or an input is invalid.

To compare selected method records in Unity 2023.2 version-29 IL2CPP metadata, run `python3 tools/compare-apk-method-identities.py /path/to/first.apk /path/to/second.apk`. It compares names, owners, tokens, flags, slot, parameter names and raw type and generic-container indices as a multiset. It does not resolve those indices to their runtime meanings or compare attributes, pointers or behavior.

If both builds retain their generated `Il2CppTypeDefinitions.c` files, `python3 tools/compare-generated-type-tables.py first.apk first-Il2CppTypeDefinitions.c second.apk second-Il2CppTypeDefinitions.c` compares table entries by index, resolving direct class and value-type metadata references to image, namespace and type name. Other initializers are compared as generated text; their referenced generic objects and runtime behavior remain unverified. Use the generated file corresponding to each APK's build, because a workspace can be rebuilt later.

For dual-platform builds, the initial check warns if the output filesystem has less than 35 GiB free. This is an estimate, not a reservation or a guarantee: available space can change and dependencies or intermediate files may require more. The warning does not stop the build.

The example includes the experimental `--shared0-fonts` option. It relocates Unity's `sharedassets0` fragments into the prepared data and reconstructs selected font atlases, font metadata, embedded font files and the exact default skybox payload from Steam. It leaves the original runtime and Gradle export untouched and creates relocated copies inside the output. A fresh-clone build at `37c62dd` used these conversion options; after an interruption, its build resumed using existing intermediate outputs. Its combined APK imported a Linux Steam ZIP on a Retroid Pocket 5 and reached a short Forest run. Joystick movement, jumping and pause were also confirmed on that device using a prepared bundle, and the Pikachu mod reached level one. An independent desktop check reproduced the expected OBB from both Linux and Windows ZIPs; the Windows input came from an earlier prepared ZIP, not a Windows PC. These checks do not establish full gameplay parity or a distributable release. The option may require additional build space and make first import slower. See [BUILDING.md](BUILDING.md) for the tested scope.

A later fresh clone at `2aadaec` completed all seven stages without interruption using those options, existing host tools and dependency caches. Its APK imported both generated ZIPs in an Android emulator and reached short Forest runs; the Pikachu example was also imported and played briefly using prepared data from that build. This is not a test on a second clean computer or proof of full parity with the published beta.

On success, the dual-platform APK is under `dual-launcher/gradle/launcher/build/outputs/apk/release/launcher-release.apk` within the output directory. For Linux-only builds it is under `research/launcher/gradle/launcher/build/outputs/apk/release/launcher-release.apk`. Before reporting success, the builder uses Unity's `aapt2` to check the final package ID and require the Internet and package-install permissions used by the launcher. Failure logs and intermediate data remain in the output directory. Do not publish this APK, the output directory, your Steam ZIPs, saves, or signing keys.

For a dual-platform build, verify the plans actually packaged in that APK against both generated Steam ZIPs:

```sh
.venv/bin/python tools/check-dual-research-launcher.py \
  --apk "$OPENBONK_OUTPUT/dual-launcher/gradle/launcher/build/outputs/apk/release/launcher-release.apk" \
  --linux-zip "$OPENBONK_OUTPUT/research/steam.zip" \
  --windows-zip "$OPENBONK_OUTPUT/windows-research/steam.zip" \
  --expected-obb "$OPENBONK_OUTPUT/research/canonical/final.obb" \
  --host-library "$OPENBONK_OUTPUT/research/native/libopenbonk_texture.so" \
  --json-jar "$OPENBONK_JSON" \
  --java-home "$(dirname "$OPENBONK_UNITY")/Data/PlaybackEngines/AndroidPlayer/OpenJDK" \
  --workdir "$OPENBONK_OUTPUT"
```

Add `--runner steam-run` if Java needs that wrapper. This compiles the checkout's Java import sources, extracts the packaged diagnostic plans into a temporary directory under `--workdir`, and checks that each ZIP reconstructs the expected OBB byte-for-byte. It removes its temporary files and does not install an APK. `--check` validates initial inputs only. These checks do not exercise Android UI, game rendering, physical controls or mods.

The APK can be installed on an isolated Android test package and supplied with a Steam ZIP through the launcher. Short emulator runs have covered both ZIP variants, Forest, touch movement and an upgrade; a Retroid test also covered the Linux ZIP and the controls described above. These tests do not establish full game parity or cover every physical control. See [BUILDING.md](BUILDING.md) for exactly which revisions and configurations were tested.

To check that the checkout's C#, Java and shader sources were copied unchanged into the generated workspace:

```sh
.venv/bin/python tools/check-unity-workspace-sources.py \
  --project "$OPENBONK_OUTPUT/steam/build/workspace" \
  --launcher-gradle "$OPENBONK_OUTPUT/dual-launcher/gradle"
```

Omit `--launcher-gradle` for a Linux-only build, or point it to `$OPENBONK_OUTPUT/research/launcher/gradle`. The check flags additional C#, Java or shader source files in the workspace, and permits three water diagnostic shaders to be absent because preparation removes those probes. It excludes the launcher checksum placeholder, which is regenerated for each APK. It checks source files only, not managed or native binaries; it does not prove that every source is compiled or that game assets and behavior match the beta.

To check the workspace's managed and native binary files against its local export, prepared plugins and Lua bridge:

```sh
.venv/bin/python tools/check-unity-workspace-binaries.py \
  --project "$OPENBONK_OUTPUT/steam/build/workspace" \
  --export "$OPENBONK_OUTPUT/steam/export/ExportedProject" \
  --plugins "$OPENBONK_OUTPUT/steam/build/managed-plugins" \
  --lua-library "$OPENBONK_OUTPUT/steam/lua/libmegabonk_lua.so"
```

The check flags workspace binary files that differ from or have no corresponding local input, as well as missing generated plugins and missing exported binaries. It permits the known Unity package duplicates removed by workspace preparation. It does not validate the creation of those inputs, Unity package-cache binaries, provenance, redistribution rights or behavior of the resulting APK.
