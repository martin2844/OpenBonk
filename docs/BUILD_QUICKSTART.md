# Build a local Android research APK

This guide is for developers on Linux x86-64. It builds OpenBonk from this checkout and your own compatible Steam game files. **The resulting APK contains game-derived data and is for local research, not redistribution.** It is not a byte-for-byte rebuild of the published beta. For the release installation instructions, see the [tester guide](../BETA_TESTER_GUIDE.md); for detailed build evidence and limitations, see [BUILDING.md](BUILDING.md).

## Prerequisites

- Git, Python 3.13 with `venv`, a C++17 compiler, `tar`, and sufficient free disk space for Unity and intermediate files. The complete local test used more than 20 GB of free space.
- [Unity 2023.2.22f1](https://unity.com/releases/editor/archive) with Android Build Support, its SDK, NDK and OpenJDK. Activate Unity in Unity Hub before building.
- [AssetRipper 2.0.0](https://github.com/AssetRipper/AssetRipper/releases/tag/2.0.0), Linux x64 free edition. The executable must report `AssetRipper.GUI.Web 2.0.0`.
- A compatible **Linux Steam installation** of Megabonk. Optionally provide a compatible Windows installation as a second input to make a launcher that accepts either platform's Steam ZIP. Each installation needs its own `Megabonk_Data` directory. The build does not download the game.
- The [`org.json:json:20180813` JAR](https://central.sonatype.com/artifact/org.json/json/20180813). For example, download it from [Maven Central](https://repo.maven.apache.org/maven2/org/json/json/20180813/json-20180813.jar). The unmodified JAR has SHA-256 `518080049ba83181914419d11a25d9bc9833a2d729b6a6e7469fa52851356da8`.

The build creates its Lua bridge, host and Android texture converters, Crunch decoder, Steam ZIPs, Unity workspace and launcher from local inputs. It also uses Unity and Gradle dependencies; `--gradle-online` permits Gradle to download missing dependencies during launcher packaging. Unity's earlier build stages have their own dependency requirements. Game data, generated projects and APKs stay local.

## Build

Clone the repository and create the pinned conversion environment:

```sh
git clone https://github.com/martin2844/OpenBonk.git
cd OpenBonk
python3.13 -m venv .venv
.venv/bin/python -m pip install -r tools/requirements-conversion.txt
```

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
  --resource-fallback --shadow-fallback --partial-rgba --partial-etc2
  --gradle-online
)

# Checks initial paths and tools only; it does not build or verify an APK.
.venv/bin/python tools/build-steam-research-launcher.py "${build_args[@]}" --check

# Runs the export, Unity builds, data conversion checks and launcher packaging.
.venv/bin/python tools/build-steam-research-launcher.py "${build_args[@]}"
```

On systems that require `steam-run` for native binaries, add `--runner steam-run` to `build_args` and run Python in a compatible environment. One Linux host needed a higher open-file limit (`ulimit -n 65536`) for Unity; this is host-specific, not a guarantee. The conversion options are experimental and can make first import take several minutes.

On success, the dual-platform APK is under `dual-launcher/gradle/launcher/build/outputs/apk/release/launcher-release.apk` within the output directory. For Linux-only builds it is under `research/launcher/gradle/launcher/build/outputs/apk/release/launcher-release.apk`. Failure logs and intermediate data remain in the output directory. Do not publish this APK, the output directory, your Steam ZIPs, saves, or signing keys.

The APK can be installed on an isolated Android test package and supplied with a Steam ZIP through the launcher. Short emulator runs have covered both ZIP variants, Forest, touch movement and an upgrade; they do not establish full game parity or physical-controller behavior. See [BUILDING.md](BUILDING.md) for exactly which revisions and configurations were tested.
