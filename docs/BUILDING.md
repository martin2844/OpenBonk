# Source and build status

This checkout exposes the current reconstruction and tools for inspection and development. **A clean checkout is not yet sufficient to rebuild the published APK.** The original local build depended on an exported Unity project, original game assets, third-party plugins, generated plans and binary intermediates that are not all included here. Some editor and research diagnostics still contain `/home/your-user` placeholders where local paths were removed; configure them for your environment before using them. Do not interpret a missing local input or an untested build as a working release pipeline.

## Layout

- `unity/OpenBonk/Assets/Scripts/` — reconstructed C# game and Android integration scripts.
- `unity/OpenBonk/Assets/AndroidSplitLauncher/` — Android launcher, Steam ZIP import, profiles and mods.
- `unity/OpenBonk/Assets/AndroidModPicker/` — Android mod picker integration.
- `unity/OpenBonk/Assets/Editor/` — editor tooling and diagnostic scripts; some require omitted assets or local inputs.
- `unity/OpenBonk/Assets/Shader/` — available reconstructed shader sources and metadata; provenance varies.
- `native/lua/` — Lua bridge, Lua 5.4.9 source archive, smoke test and example packages.
- `native/stable-zlib/`, `tools/`, `analysis/data-conversion/` — native support and experimental preparation, validation and reconstruction tools. Many research scripts depend on local generated intermediates and are not standalone build commands.
- `mods/` — example Lua mods and the experimental Pikachu package.

The local runtime work used Unity 2023.2.22f1 and Android ARM64. Reconstructing a working project also requires the appropriate Unity Android toolchain, compatible game data and unresolved third-party components. No personal Steam data, saves or signing keys are included. Reproducible clean setup, dependency provenance and licensing remain work to do.

The Steam data-preparation Java sources and three inputs used to generate position-mask constants are included. To check this component with Python 3, a JDK and an existing org.json 20180813 JAR:

```sh
python3 tools/check-data-prep-sources.py --json-jar /path/to/json-20180813.jar
```

This compiles the `Steam*.java` sources and checks that the three generated Java files match their pinned JSON inputs. It does not assemble an APK or supply missing Unity assets, plans, third-party components or build intermediates. The older `tools/isolated-workspace-inputs.json` records research inputs; it is not a manifest for a verified clean release build.

## Lua bridge

On a Linux x86-64 host with an Android NDK, set `ANDROID_NDK_HOME` or `ANDROID_NDK_ROOT`, then run `bash native/lua/build.sh`. It checks the Lua archive hash and builds the ARM64 library and smoke executable in `build/lua`. This builds the bridge only; it does not build the OpenBonk APK. The APK and game integration require the missing Unity project inputs described above.

The project includes reconstructed game-derived material and third-party components of different provenance. No blanket source license is asserted here; check the applicable rights and component notices before redistribution or reuse.
