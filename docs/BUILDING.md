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

## Isolated Unity workspace

If you already have a compatible reconstructed Unity project with `Assets`, `Packages` and `ProjectSettings`, the checkout can overlay its code into a separate local copy:

```sh
python3 tools/assemble-unity-workspace.py \
  --base-project /path/to/reconstructed-unity-project \
  --output /path/to/new-local-workspace
```

Use `--check` to validate without writing. Both projects must specify Unity 2023.2.22f1; the tool checks GUIDs for metadata present in both, rejects symbolic links and existing output paths, and leaves the base untouched. The workspace includes the base project's data, which should remain local. This tool does not generate the base project, reconstruct all missing build intermediates or build an APK.

A fresh local Steam export with AssetRipper 1.3.14 was also tried: 69 GUIDs differed. The optional `--allow-guid-remap` reconciled those GUID references in 623 local text files while preserving the original export. This is experimental: it does not prove that every asset or binary reference is correct, and the resulting fresh-export project still lacks configuration and build inputs. Without the flag the tool rejects mismatched GUIDs.

To generate the managed Android build inputs locally from the exported project rather than copying another developer's DLLs:

```sh
python3 tools/prepare-managed-plugins.py \
  --project /path/to/exported-project \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --output /path/to/new-managed-plugins
```

The tool uses Unity's Mono and Mono.Cecil to normalize two assemblies and create the Android Steamworks stub; `--runner steam-run` is available where needed. The generated inputs contain derived third-party code and should remain local. Outputs generated from a fresh Steam export are not byte-identical to the previous local reference and have not yet been validated in an end-to-end build.

Build the Lua bridge as described below, then prepare the isolated workspace before opening it in Unity:

```sh
python3 tools/prepare-unity-workspace.py \
  --project /path/to/new-local-workspace \
  --plugins /path/to/new-managed-plugins \
  --lua-library /path/to/libmegabonk_lua.so \
  --remove-duplicate-package-dlls
```

This installs locally generated plugins, restores their checked-in metadata and backs up replaced files in `.openbonk-original-plugins` inside the workspace. The optional removal flag is intended for the observed AssetRipper 1.3.14 export: it backs up and removes specific DLLs that duplicate Unity packages. Use `--check` first and keep the original export. Preparing these files does not reconstruct the missing project configuration and assets.

After importing the prepared workspace once in Unity, remap references from those exported package DLLs to scripts in the imported Unity packages:

```sh
python3 tools/remap-unity-package-scripts.py \
  --project /path/to/new-local-workspace \
  --backup /path/to/new-local-script-backup
```

The 41 mappings were compared against corresponding component references in the existing working project. On one fresh export they covered 13,738 references in 86 files. The tool checks the installed package GUIDs, rejects unknown references to removed package DLLs, backs up every changed file, and supports `--check`. This is a version-specific reconstruction step, not proof that every scene or component behaves correctly.

An experimental localization stage reconstructs Android localization from the 19 supported local Linux Steam bundles, without adding game text to this checkout:

```sh
python3 tools/rebuild-unity-localization.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --build-root /path/to/new-localization-output
```

The project must already be prepared, and must contain the supported bundles under `Assets/StreamingAssets/aa/StandaloneLinux64`. The tool verifies their hashes, requires a new output directory, and offers `--check` and `--runner steam-run`. It modifies the isolated workspace and stores derived game data and logs locally. A manual run on a fresh export restored 17 locales and 867 tables and built Android Addressables; the complete APK and gameplay still need separate validation.

The observed Steam export also has Addressables files that conflict with Unity's newly generated Android files at player build time. After successfully rebuilding Android Addressables, back up and remove conflicting exported files from the isolated workspace:

```sh
python3 tools/prepare-android-addressables.py \
  --project /path/to/new-local-workspace \
  --backup /path/to/new-local-catalog-backup
```

This checks for generated Android catalogs, compares their relative paths against exported files, and records hashes of conflicting files and their metadata in the local backup; `--check` makes no changes. The original Steam export remains untouched. Retain the backup and the Steam-derived bundles locally.

The experimental runtime build stage can use that isolated workspace and a local directory containing the three managed DLLs named by the tool:

```sh
python3 tools/build-unity-runtime.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --plugins /path/to/local/managed-plugins \
  --build-root /path/to/new-runtime-output
```

On a system that needs a compatibility wrapper, add e.g. `--runner steam-run`; any required system libraries must also be configured locally. The tool has a `--check` option, refuses an existing output or response file, and retains its output and log on build failure. It invokes Unity's ARM64 runtime entry point with Android target SDK 35 and explicit no-Rewired and no-probe defines. Its inputs and outputs can contain game-derived and third-party material. A runtime APK plus OBB is an intermediate stage, not the final OpenBonk launcher APK; the clean end-to-end build and independent reconstruction of the inputs remain unverified.

A local runtime build from a fresh export succeeded after the package-script and Addressables preparation above. Its script registry matched the 2,349 script IDs and identities in the previous runtime, but three empty script components remained in the scene build and its OBB had 207 ZIP entries versus 946 in the reference. One concrete gap is that the fresh export lacks the 21 populated `Resources/AndroidCharacters` assets used by the gameplay bootstrap. A comparison of 1,093 corresponding `MonoBehaviour` assets also found 741 whose fresh exports contain only an empty script header while the reference assets have additional serialized fields. Many asset entries therefore remain different or absent; neither gameplay equivalence nor a distributable APK has been established. The local Steam bundles must not be published as part of this experiment.

`tools/extract-steam-character-data.py` is an experimental investigation into this gap. With local `GameAssemblies` and Python dependencies `UnityPy==1.25.4` and `TypeTreeGeneratorAPI==0.0.10`, it decoded 21 character records from a local Steam installation. Their character IDs and collider values matched the 21 reference resources. The output contains game data and must stay local; its Unity pointers have not yet been mapped to project assets, so it cannot currently restore the characters or make the build playable.

## Lua bridge

On a Linux x86-64 host with an Android NDK, set `ANDROID_NDK_HOME` or `ANDROID_NDK_ROOT`, then run `bash native/lua/build.sh`. It checks the Lua archive hash and builds the ARM64 library and smoke executable in `build/lua`. This builds the bridge only; it does not build the OpenBonk APK. The APK and game integration require the missing Unity project inputs described above.

The project includes reconstructed game-derived material and third-party components of different provenance. No blanket source license is asserted here; check the applicable rights and component notices before redistribution or reuse.
