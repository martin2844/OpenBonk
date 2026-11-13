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

To include the Steam export in the experimental local build, use:

```sh
python3 tools/build-local-from-steam.py \
  --game /path/to/Steam/steamapps/common/Megabonk \
  --assetripper /path/to/AssetRipper.GUI.Free \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --output /path/to/new-local-build \
  --application-id com.example.openbonktest
```

This builds the Lua bridge with Unity's Android NDK, then runs the export and local Unity build described below. Alternatively, supply `--lua-library /path/to/libmegabonk_lua.so` to use a locally built bridge. One run using an existing bridge completed from a local Steam installation and its APK reached a Forest run on an iPlay tablet. On that APK, the included Pikachu mod was imported, enabled and selected, and a brief Forest run showed its Thunderbolt icon and enemy defeats. Its animations, sound and level-up bonuses were not comprehensively tested. A separate run from a clean clone at commit `8c829be` automatically built the Lua bridge and the embedded APK, which installed on the iPlay under a separate package ID; interactive gameplay of that specific build has not been verified. Logs and intermediate files stay inside the new output directory, which contains game-derived data and must stay local. Add `--runner steam-run` if needed; `--check` only verifies initial inputs. This is not the data-separated release build and the smoke test does not establish full feature parity.

For an experimental local, data-embedded APK, `tools/build-local-unity-apk.py` runs the preparation stages described below in sequence from an AssetRipper 2.0.0 `ExportedProject` and a locally built Lua bridge:

```sh
python3 tools/build-local-unity-apk.py \
  --base-project /path/to/ExportedProject \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --lua-library /path/to/libmegabonk_lua.so \
  --output /path/to/new-local-build \
  --application-id com.example.openbonktest
```

Add `--runner steam-run` if needed. `--check` validates the initial inputs only. The tool requires a new output directory, retains local intermediates and backups on failure, and does not install the APK. A run from a fresh local export completed and its APK reached a Forest run on an iPlay tablet. The example Forest mod was also imported and enabled from the launcher, appeared in map selection and started a run with its configured five-minute timer; its Lua multipliers were not measured. The output contains game-derived assets; it is not the data-separated release APK. The sequence remains experimental and the smoke tests do not establish full feature parity.

An experimental local export step uses AssetRipper.GUI.Web 2.0.0 and a game installation provided by the user:

```sh
python3 tools/export-steam-unity-project.py \
  --game /path/to/Steam/steamapps/common/Megabonk \
  --assetripper /path/to/AssetRipper.GUI.Free \
  --output /path/to/new-local-export
```

On systems that need it, add `--runner steam-run`. The tool starts AssetRipper on a loopback-only server, retains a local log, and requires a new output directory. `--check` validates prerequisites. The exported game assets and log stay local; this does not by itself produce an APK.

The checkout can overlay its code into a separate local copy of a compatible exported Unity project with `Assets`, `Packages` and `ProjectSettings`:

```sh
python3 tools/assemble-unity-workspace.py \
  --base-project /path/to/reconstructed-unity-project \
  --output /path/to/new-local-workspace
```

Use `--check` to validate without writing. Both projects must specify Unity 2023.2.22f1; the tool checks GUIDs for metadata present in both, rejects symbolic links and existing output paths, and leaves the base untouched. The workspace includes the base project's data, which should remain local. This tool does not generate the base project, reconstruct all missing build intermediates or build an APK.

A fresh local Steam export with AssetRipper 1.3.14 was also tried: 69 GUIDs differed. The optional `--allow-guid-remap` reconciled those GUID references in 623 local text files while preserving the original export. This is experimental: it does not prove that every asset or binary reference is correct, and the resulting fresh-export project still lacks configuration and build inputs. Without the flag the tool rejects mismatched GUIDs.

AssetRipper 2.0.0 was also tested with a fresh local export. In a comparison of 1,093 `MonoBehaviour` assets, none of the 741 assets that were empty in the 1.3.14 export remained empty while populated in the reference project. Its export still required the same 69 GUID remappings. A local APK built from a 2.0.0 export reached a brief Forest run; this does not establish full gameplay fidelity.

To generate the managed Android build inputs locally from the exported project rather than copying another developer's DLLs:

```sh
python3 tools/prepare-managed-plugins.py \
  --project /path/to/exported-project \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --output /path/to/new-managed-plugins
```

The tool uses Unity's Mono and Mono.Cecil to normalize two assemblies and create the Android Steamworks stub; `--runner steam-run` is available where needed. The generated inputs contain derived third-party code and should remain local. Outputs generated from a fresh Steam export are not byte-identical to the previous local reference; a local APK built with them reached a brief Forest run, but full gameplay fidelity remains unverified.

Build the Lua bridge as described below, then prepare the isolated workspace before opening it in Unity:

```sh
python3 tools/prepare-unity-workspace.py \
  --project /path/to/new-local-workspace \
  --plugins /path/to/new-managed-plugins \
  --lua-library /path/to/libmegabonk_lua.so \
  --remove-duplicate-package-dlls
```

This installs locally generated plugins, restores their checked-in metadata and backs up replaced files in `.openbonk-original-plugins` inside the workspace. The optional removal flag is intended for the observed AssetRipper 1.3.14 export: it backs up and removes specific DLLs that duplicate Unity packages. Use `--check` first and keep the original export. Preparing these files does not reconstruct the missing project configuration and assets.

The clean Steam export does not contain the Android input axes used by the native controller code, and several existing joystick axes differ from the locally tested project. Before importing Unity, restore the checked-in Android input configuration in the isolated workspace:

```sh
python3 tools/prepare-android-input.py \
  --project /path/to/new-local-workspace \
  --backup /path/to/new-local-input-manager-backup.asset
```

The tool preserves unrelated axes and writes the original file to the local backup. `--check` verifies the expected configuration without changing it. `build-local-unity-apk.py` runs this step automatically, and `build-unity-runtime.py` rejects a workspace whose Android axes are absent or different. In editor tests, the clean export failed the chest navigation axis check before preparation; after preparation, chest navigation, controller bindings, menu A/B inversion, challenge-shrine and boss-bar checks passed. These checks do not replace physical controller testing on Android.

As an additional comparison, the serialized settings in a clean embedded APK built at `240e2b8` were checked against the locally installed `com.megabonk.nativeport.splittest` version `1.0.69`. Physics 2D, 3D physics, tags and layers, and time settings matched. All 519 named input-axis groups also matched, including the order of entries within each group; the overall order of the groups differed. This comparison verifies these settings only: it does not establish identical gameplay, rendering, or hardware controller behavior, and the installed package is not necessarily the same as the published release.

An end-to-end run from a clean clone at commit `240e2b8` exported Steam data, applied the axes before Unity import, rebuilt localization and built an embedded local APK. The axis check still matched after the build, and the chest navigation play-mode check passed against that fresh workspace. The APK installed and started its launcher on an iPlay; physical input and gameplay were not tested. The APK and local workspace contain game data and are not distributable artifacts.

The observed 2.0.0 export contains three pairs of textures whose paths differ only in letter case; Unity can fail to import both. Before opening the workspace in Unity, rename the conflicting texture copies and their metadata together:

```sh
python3 tools/resolve-unity-texture-case-collisions.py \
  --project /path/to/new-local-workspace \
  --manifest /path/to/new-local-texture-renames.json
```

The tool preserves asset GUIDs, records the local renames and hashes, rejects unsupported collision types, and supports `--check`. The original export is unchanged. Paths should be reviewed if a different game version adds case collisions.

Import the prepared workspace in Unity with the required build defines:

```sh
python3 tools/import-unity-workspace.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --log /path/to/new-local-import-log
```

The import keeps game data local, offers `--check` and `--runner steam-run`, and retains the log if Unity fails. After importing, remap references from the exported package DLLs to scripts in the imported Unity packages:

```sh
python3 tools/remap-unity-package-scripts.py \
  --project /path/to/new-local-workspace \
  --backup /path/to/new-local-script-backup
```

The 43 mappings were compared against corresponding component references in the existing working project. On the 1.3.14 export, 41 mappings covered 13,738 references in 86 files; the 2.0.0 export also needed two localization mappings. The tool checks the installed package GUIDs, rejects unknown references to removed package DLLs, backs up every changed file, and supports `--check`. This is a version-specific reconstruction step, not proof that every scene or component behaves correctly.

On the populated 2.0.0 export, generate the local character Resources after importing and remapping the workspace:

```sh
python3 tools/prepare-unity-characters.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --log /path/to/new-local-character-log \
  --backup /path/to/new-local-character-backup
```

This verifies the 21 character assets and their model, weapon, passive and icon references, backs up Bandit and Clank before correcting their default meshes to match their animated prefabs, then creates Resources copies and a fallback prefab locally. `--check` validates prerequisites without writing, and `--runner steam-run` is available where needed. One character was exercised in a short Android smoke test; the full roster and gameplay remain unverified.

Restore post-processing shader references to the imported Unity package, preserving the original local asset:

```sh
python3 tools/prepare-unity-postprocessing.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --log /path/to/new-local-postprocessing-log \
  --backup /path/to/new-local-postprocessing-backup
```

A local test restored the same 24 package shaders and 12 compute shaders as the reference project. `--check` and `--runner steam-run` are supported; this check does not establish rendering equivalence.

Restore the eight exported skybox materials to OpenBonk's reconstructed skybox shader:

```sh
python3 tools/prepare-unity-skyboxes.py \
  --project /path/to/new-local-workspace \
  --backup /path/to/new-local-skybox-backup
```

The step backs up the materials and metadata, checks their existing shader references, and changes only those shader references. It does not redistribute cubemaps; `--check` makes no changes.

An experimental localization stage reconstructs Android localization from the 19 supported local Linux Steam bundles, without adding game text to this checkout:

```sh
python3 tools/rebuild-unity-localization.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --build-root /path/to/new-localization-output
```

The project must already be prepared, and must contain the supported bundles under `Assets/StreamingAssets/aa/StandaloneLinux64`. The tool verifies their hashes, requires a new output directory, and offers `--check` and `--runner steam-run`. It modifies the isolated workspace and stores derived game data, backups and logs locally. It rebuilds the tables, repairs shared-table asset GUIDs to match their original collection GUIDs, restores editor collections, rebuilds Android Addressables again and checks a scene lookup and 17 item descriptions in English and Spanish using Unity's default table lookup. On the supported local input this restored 17 locales and 867 tables; the checks verified 51 collection GUIDs. An embedded local APK subsequently built and its launcher started on an iPlay. The previous build log contained 13 localization `NullReferenceException` entries; the repaired build log contained none. These editor checks do not verify all translations or interactive gameplay.

The corrected GUID and collection preparation also completed through `build-local-from-steam.py` from a clean clone at commit `9bf971e`: the resulting local embedded APK installed and its launcher started on the iPlay, and the player build log had no localization `NullReferenceException` entries. The newer default-table item check was then run separately against that fresh workspace and passed for the 17 English and Spanish descriptions. The APK embeds game data and remains private; neither launcher startup nor editor checks establish gameplay parity.

During Android scene preparation and the player build, the build entry point temporarily uses the reconstructed project localization settings and restores the previous editor instance afterward. An editor check opened all five scenes without a missing-settings error. A subsequent local embedded APK built and started its launcher on an iPlay; its build log contained zero `There is no active LocalizationSettings` errors, compared with 13 in the earlier build, and the GUID and item checks passed afterward. The runtime builder now rejects a successful Unity exit if this specific error occurs in its log. In-game localization remains unverified by these checks.

The Android build filters the desktop-only `Assets/MonoBehaviour/UIParticle.asset` preload by its project path rather than an export-specific GUID, while preserving the localization preload and restoring the original project settings afterward. In one clean-export build, Unity reported one excluded preload; inspection of the resulting APK showed `Localization Settings` still preloaded and `UIParticle` absent from that list. The project settings file matched its pre-build copy byte for byte, and the private APK installed and started its launcher on an iPlay. Particle rendering in gameplay was not tested by this check.

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

On a system that needs a compatibility wrapper, add e.g. `--runner steam-run`; any required system libraries must also be configured locally. The tool has a `--check` option, refuses an existing output or response file, and retains its output and log on build failure. By default it invokes Unity's ARM64 runtime entry point with Android target SDK 35 and explicit no-Rewired and no-probe defines. `--include-rewired` experimentally includes the locally generated Rewired plugins: one local embedded APK built, contained `Rewired_Core.dll` and `Rewired_Linux.dll`, installed and started its launcher on an iPlay, but its controller and gameplay behavior were not tested. The same optional switch is accepted by the higher-level local Steam and research-launcher build commands; the default remains the no-Rewired path. Inputs and outputs can contain game-derived and third-party material. A runtime APK plus OBB is an intermediate stage, not the final OpenBonk launcher APK; reproducing the published APK remains unverified.

A separate experiment used `--include-rewired` for both the runtime and Gradle export from a localization-repaired workspace, then generated the research launcher from those outputs. Its seven preparation stages reproduced the expected OBB checksum on an iPlay, and its isolated APK installed and started the launcher. The option's propagation through the higher-level commands is covered by unit tests; a complete one-command Steam build with this option and interactive controller behavior remain unverified. The experimental APK still contains game data.

That Rewired research experiment predates Android input-axis preparation and does not validate physical controls. Rebuild it with the current preparation stage before testing input.

Including the Rewired assemblies does not switch Android gameplay input to Rewired: `MyInputManager` delegates Android actions to `AndroidNativeInput`, because the available Rewired integration has desktop backends. Both runtime variants apply the Android scene preparation that removes desktop manager components and enables the standalone UI input module. A local Rewired-inclusive embedded APK built with this scene preparation and started its launcher; its menu and controller input have not been tested interactively.

An optional `--application-id com.example.openbonktest` builds an isolated local test package without changing the default runtime ID. This does not install anything and is not a release signing configuration.

For local experiments, `--embedded` instead requests a single APK containing the exported game data. A locally generated APK installed under an isolated package ID on an iPlay tablet and reached a Forest run with touch controls without a separate OBB. This output contains game-derived material and is not the data-separated APK intended for distribution. The short smoke test does not establish feature parity, and this mode does not resolve the conversion-plan or release-signing gaps.

To generate a local Android Gradle project instead, use the same command with `--gradle-project`. A local Unity export created the expected Gradle modules and Unity library; integrating it with the launcher, generated conversion plans and signing has not yet been validated. The exported project contains game-derived data and should remain local.

The experimental `tools/build-private-launcher-prototype.sh` accepts `OPENBONK_GRADLE_TEMPLATE` and `OPENBONK_UNITY_PROJECT` for these local paths, and `OPENBONK_TEXTURE_LIBRARY` for the locally built ARM64 texture adapter. It still requires its pinned runtime, OBB and conversion plans and intentionally rejects incompatible inputs; setting those paths does not make it a verified clean APK build.

Three version-specific position-only scrub plans are included under `analysis/data-conversion/`; `python3 tools/check-position-plans.py` validates their hashes and structure. They contain offsets, hashes and bit masks rather than the masked asset bytes. They are pinned to older intermediate builds and do not resolve the remaining plan generation and compatibility work for the newly built runtime.

Two further version-specific metadata plans are in `analysis/data-conversion/metadata-plans/`: `exact.bin` covers 56 texture payloads and `audio.bin` covers 63 audio resources. Their checked-in generators were run against the pinned local inputs and reproduced these files byte-for-byte; they contain positions, sizes and hashes rather than texture or audio payloads. Run `python3 tools/check-conversion-metadata.py` to verify them. The prototype packager uses these copies when they are absent from the local plan directory and rejects mismatched overrides. These plans still target the old intermediate build and do not make the fresh runtime compatible or supply the other required inputs.

`tools/package-conversion-plans.py PLAN_DIRECTORY --check --shader-dir SHADER_DIRECTORY` checks that all 50 expected filenames are present and verifies the two checked-in metadata plans and both shader pack hashes before the prototype build starts. It does not validate the contents of the remaining 46 plans or establish that they match a new runtime.

`tools/experimental-animation-mask.py` investigates generating masks for a newly built OBB instead of using the old animation layout. It requires UnityPy and local Steam `sharedassets1.assets` and `sharedassets2.assets`. On one new local build, it removed 2,056,359 nonzero bytes shared at identical positions across 108 animation clips; restoring those bytes from the same Steam input reproduced all 922 archive entries byte-for-byte. The generated template retains other game data and must stay local. This is not integrated with the Android launcher, does not remove all animation data, and does not reconstruct the other asset types or establish a redistributable APK.

`tools/experimental-animation-fragment-plan.py` extends the same equal-byte mask approach to animation clips in both standalone and split asset files. One local test masked 2,329,748 nonzero bytes from 134 clips across 135 fragments and restored all 922 archive entries. `SteamPlannedMeshes.restoreAnimations` implements the plan on Android; a standalone iPlay test produced the same full OBB checksum as the tested desktop JDK and rejected altered plans, masks, templates and output checksums without publishing a partial file. The prior mesh path was also rechecked on desktop. Other animation bytes and game assets remain in the template, which must stay private; this path has not been gameplay-tested.

The research launcher builder accepts `--animation-plan` and `--animation-intermediate-obb` with the cubemap, mesh, texture and audio inputs. Its experimental launcher restores animation fragments first, then the five stages described below. Desktop integration tested plan failure and rollback across all six stages; a standalone iPlay test ran the complete chain and matched the desktop OBB checksum. The APK compiled, but interactive import and gameplay are not yet verified. The template still contains other game data and is not redistributable.

`tools/experimental-exact-object-plan.py` scans standalone and split serialized assets for whole objects that exactly match local Steam objects. One local test masked 6,684,460 bytes from 69 objects (42 meshes, 24 particle systems, two materials and one text asset) and restored all 922 archive entries. `SteamPlannedMeshes.restoreExactObjects` implements the plan on Android; a standalone iPlay test matched the desktop OBB checksum and rejected altered plan hashes, templates, overlapping ranges and source checksums without output. Existing mesh and animation restoration were rechecked on desktop. Exact matches do not establish provenance or reconstruct other game assets.

The research launcher builder accepts `--object-plan` and `--object-intermediate-obb` together with the animation, cubemap, mesh, texture and audio inputs. The experimental launcher restores exact objects first, followed by the six stages described here. Desktop integration checked each plan's failure and rollback and successful installation; a standalone iPlay test ran all seven stages and matched the desktop OBB checksum. The APK compiled, but interactive import and gameplay have not been tested. Other game data remains in the template, so it must stay private.

`tools/build-local-research-chain.py` automates the private research sequence from an already-built local runtime APK and OBB, matching Gradle export and isolated Unity workspace. It generates the conversion studies, seven plans and canonical intermediates, verifies each reconstruction, then builds the debug-signed research launcher. For example:

```sh
python3 tools/build-local-research-chain.py \
  --steam-data /path/to/Megabonk_Data \
  --runtime-obb /path/to/local-runtime.main.obb \
  --runtime-apk /path/to/local-runtime.apk \
  --gradle-project /path/to/local-gradle-export \
  --unity-project /path/to/isolated-unity-workspace \
  --unity-editor /path/to/Unity/2023.2.22f1/Editor \
  --json-jar /path/to/json-20180813.jar \
  --application-id com.example.openbonkresearch \
  --output /path/to/new-local-research-directory
```

The active Python environment needs UnityPy and its texture dependencies; a C++ compiler, the Unity Android NDK and an existing compatible Gradle cache are also required. Add `--runner steam-run` where needed, and run Python within a compatible environment if its native libraries require one. Optional `--steam-zip`, `--host-library`, `--android-library`, `--decoder` and `--gradle-user-home` reuse local inputs; otherwise the script creates the ZIP and builds its native tools. `--check` validates only initial inputs. One local run with supplied ZIP and libraries completed all 26 stages; another generated those inputs and completed all 30 stages. Both seven-stage preparations reproduced the expected OBB on an iPlay. The command does not create the initial Unity workspace, runtime or Gradle export, establish visual/gameplay fidelity, or remove all game content. Its output directory, logs, ZIP and APK must remain private.

`tools/build-steam-research-launcher.py` also orchestrates the preceding Steam export, isolated Unity workspace preparation, runtime build and Gradle export before invoking the research chain. From the repository root, with the dependencies described above available:

```sh
python3 -m venv /path/to/openbonk-venv
/path/to/openbonk-venv/bin/python -m pip install -r tools/requirements-conversion.txt
/path/to/openbonk-venv/bin/python tools/build-steam-research-launcher.py \
  --game /path/to/Steam/steamapps/common/Megabonk \
  --assetripper /path/to/AssetRipper.GUI.Free \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --json-jar /path/to/json-20180813.jar \
  --application-id com.example.openbonkresearch \
  --output /path/to/new-local-research-directory
```

The pinned conversion environment was tested on Linux x86-64 with Python 3.13. Unity needs its Android build support, SDK, NDK and OpenJDK modules; the native conversion tools also need a host C++ compiler. The JSON JAR and an existing compatible Gradle cache are local prerequisites. Optional `--gradle-user-home` selects that cache and `--runner steam-run` wraps tools where needed. Use a new output directory outside the checkout and game installation. `--check` validates initial inputs only; `--help` lists the other options. On success the APK is at `research/launcher/gradle/launcher/build/outputs/apk/release/launcher-release.apk` inside the output directory. The APK and intermediate files contain game data and must remain private; this workflow does not yet establish a redistributable build or gameplay fidelity.

A clean local run completed all of these steps and produced a private research APK. Its seven data-preparation stages were also checked on an Android device against the expected OBB checksum; this does not verify interactive gameplay. Unity's native dependencies, including `libxml2.so.2`, must be available in the environment running the editor.

An end-to-end run from a clean clone at commit `3f06c4b` also completed with automatic localization GUID repair and both editor checks: the scene entry and 17 item descriptions in English and Spanish. The runtime build log had no localization `NullReferenceException` entries. Its seven data-preparation stages reproduced the expected OBB checksum on an iPlay, and the resulting isolated APK installed and started its launcher there. The interactive import, visual quality and gameplay of this particular build have not been verified. Its template and APK still contain game data and must remain private.

That `3f06c4b` research build predates the Android input-axis preparation documented above and must not be used as evidence that controller input works. The later clean embedded build at `240e2b8` includes the corrected input configuration, but its physical controls remain untested.

A subsequent local end-to-end research build at `04b9e84` included the input-axis preparation, updated texture quality gate and smaller exact objects. It reused a locally built Lua library, texture libraries, decoder, Steam ZIP and Gradle cache. Each of the seven desktop Java restoration checks reproduced all 922 expected OBB entries, and the resulting private APK installed and started a process under a separate package ID on an iPlay. The device was locked: the launcher UI, import, gameplay, visual fidelity and physical controls were not verified. Its template and APK still contain game data and must remain private.

The same build's seven restoration stages were also run in sequence on the iPlay; each reproduced the 922 expected entries. To repeat this private device check with a completed research directory and authorized Android device:

```sh
python3 tools/check-research-chain-android.py \
  --research /path/to/local-build/research \
  --steam-zip /path/to/private-steam.zip \
  --library /path/to/arm64/libopenbonk_texture_arm64.so \
  --sdk /path/to/Unity/Editor/Data/PlaybackEngines/AndroidPlayer/SDK \
  --java-home /path/to/Unity/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK \
  --device DEVICE_SERIAL
```

Add `--runner steam-run` when needed. The check transfers game data to a temporary directory on the selected device and attempts to remove it afterward. It checks reconstruction only; it does not test the launcher, appearance, controls or gameplay. All build artifacts and game data remain private.

The exact-object stage now considers objects as small as 64 bytes and selects source files only from those listed for both supported minimal Steam ZIPs. In a local study it masked 92 exact objects versus 43 with the previous 256-byte threshold; the desktop Java restorer reproduced all 922 archive entries from the minimal Linux ZIP. This is a small reduction in retained data, not a distributable template or an Android gameplay test.

The same stage can now remove exact matching shader fragments even when the Steam and Android object offsets differ. Against the later quality-gated research build, it found 4,825 fragments and reduced nonzero bytes in the private template from 15,857,441 to 14,667,817. The desktop restorer and all seven Android restoration stages reproduced the expected 922 entries. These fragments do not reconstruct the remaining shader data or establish distribution rights; the template and resulting APK remain private, and gameplay has not been tested.

At `64c2dfd`, the complete 26-stage research chain was rerun using the existing `04b9e84` Unity workspace, runtime, Gradle export and locally built dependencies. It assembled a private APK containing the resulting 4,919-record object plan, including the 4,825 shader fragments. The seven stages again reproduced all 922 entries on the iPlay, and the APK installed over the separate research package. This does not test the installed launcher's import or gameplay, and is not a fresh Unity build or a redistributable APK.

The object stage also reconstructs byte-identical Linux localization bundles from the matching minimal Linux Steam ZIP. A local study found 18 bundles totaling 593,465 bytes (543,334 nonzero bytes). The desktop builder validation and all seven restoration checks on an iPlay reproduced all 922 expected entries. This specifically tests the Linux inputs, not Windows compatibility, launcher import or translated text during play; the remaining template still contains game data.

The full Steam-to-Unity research builder currently requires Linux data. The later `build-local-research-chain.py` stage also accepts `--platform windows` when supplied with an already prepared runtime and Gradle export. A local 26-stage run used Windows Steam data and a Windows ZIP with the previously built Linux-derived Unity runtime; all seven restoration stages reproduced the 922 expected entries on an iPlay. Those final entries also matched the Linux-derived research output.

To package both sets of plans into one private research APK, supply `--windows-research /path/to/windows-research-output` to `build-generated-audio-launcher.py` alongside the complete Linux seven-stage arguments. The builder checks both plans against the supported platform inventories, validates all Windows intermediates and requires byte-identical final OBBs. In a desktop launcher integration check using the packaged assets, both Linux and Windows ZIPs selected the appropriate plan and produced the same expected OBB. Platform selection was also tested on an iPlay. The APK installed on the iPlay, but interactive import and gameplay were not tested. Both templates still contain game data; the APK is private, not redistributable or evidence of compatibility with the published beta.

The research builders check the selected platform's data and compare a supplied ZIP with its required local files before starting expensive work. A ZIP for the other platform or a mismatched game version is rejected; `--check` does not establish that conversion or gameplay will succeed.

The current texture study applies the same level-zero mean-error threshold to shared textures and standalone textures before allowing aliases. In a local run it converted 783 candidates and rejected 174 high-error candidates; in particular, the shared `sprite_0091` and its standalone copy stayed unchanged rather than receiving the visibly banded conversion described in earlier studies below. This keeps more original game data in the private template. The threshold is only a heuristic: accepted conversions may still have visible defects, and gameplay has not been verified.

The texture conversion study also handles the `Tiles1_Normal` and `Tiles4_Normal` DXT5nm maps, reconstructing their alpha/green normal channels before encoding to ETC1. In one local build their 5,592,432 image bytes were excluded from the template, reducing its nonzero bytes from 23,160,778 to 19,549,303. The regenerated seven-stage chain reproduced its expected OBB checksum on an iPlay and built a private research APK. This conversion is lossy and has not been reviewed visually in gameplay. Other game data remains in the template; the output is not redistributable.

The study can choose between multiple Steam texture candidates only when their compatible metadata, format and image payload match. In one local run this enabled three more conversions, including `sprite_0091`; the resulting template contained 19,011,916 nonzero bytes and the seven-stage Android checksum test passed. A side-by-side image comparison revealed visible banding on `sprite_0091`. This is an experimental conversion and requires improvement or gameplay review before a release; checksum equality verifies the reconstruction, not visual fidelity.

Standalone Unity texture files can reuse a converted shared texture only when their original image payload and relevant texture metadata match, and all matching conversions agree. In the tested build this covered a 1,398,128-byte copy of `sprite_0091`; the template then contained 18,477,000 nonzero bytes. The regenerated research APK built and the seven-stage Android checksum test passed. The same visible-banding limitation applies to the copy, and other game data remains in the template.

The study also attempts direct Steam matches for standalone textures using name, dimensions and compatible metadata, rejecting ambiguous source payloads. For these direct matches it retains the original when any channel's average level-zero error exceeds 10 on the 0–255 scale. In one run, 97 standalone textures were converted and 16 were left in the template by this filter; the template contained 16,802,539 nonzero bytes. The research APK built and the seven-stage Android checksum test passed. The error threshold is a heuristic, not visual or gameplay validation, and does not eliminate all image artifacts. The prior `sprite_0091` alias uses the separate exact-payload alias path and retains its documented banding.

The animation fragment plan also masks equal nonzero bytes in particle-system objects of matching length, selecting Steam candidates by matching blocks. It restores and verifies the complete original fragment checksum, so candidate selection does not change its bytes. In one local build, 696 particle systems were covered and the template contained 15,712,691 nonzero bytes. The research APK built and the complete seven-stage checksum test passed on Android. Other game data remains, and this does not verify how the effects appear during play.

The same fragment handling also covers scene and additional shared-asset entries. In a later local run it covered 1,157 particle systems, including objects in scene files, and the template contained 15,043,782 nonzero bytes. The research APK compiled and its seven-stage Android checksum test passed with a 2.8 MB plan. Unmatched and other game-derived data remain local; gameplay and visual fidelity were not tested by this check.

The exact-byte fragment plan also handles matching materials, MonoBehaviour components, animator controllers and terrain data. It groups candidates by object type and size and verifies the complete restored fragment checksum. In one local build it covered 542 materials, 1,804 MonoBehaviour components, 32 animator controllers and one terrain object alongside the existing animation and particle records; the template contained 13,401,831 nonzero bytes. The private research APK compiled and the seven-stage Android checksum test passed with a 4.2 MB plan. Mismatched bytes and other data still remain in the template.

For local audio research, `tools/experimental-steam-audio.py` substitutes matching Steam FSB audio into a newly built OBB after checking the clip metadata and FSB format. `tools/verify-experimental-steam-audio.py` checks the substitution and decodes both versions with UnityPy's `fmod_toolkit` dependency. In one local test, all 63 pairs decoded with matching channel count, sample rate, sample width and frame count; the remaining 796 OBB entries matched. The modified build reached Forest on an iPlay tablet without a recorded audio error. This does not establish audible or perceptual equivalence.

`tools/experimental-steam-audio-plan.py` can create a local offset-and-hash plan and an OBB template with those 63 audio resources blanked, then restore them from the matching Steam data. One local round trip restored 9,996,992 audio bytes and reproduced all 922 OBB entries byte-for-byte. The template still contains other game data, and the plan targets that specific build and Steam input; neither tool produces a redistributable APK.

`tools/inspect-packed-steam-audio.py --obb /path/to/local.obb --steam-data /path/to/Megabonk_Data` reports aggregate matches for audio embedded in the two split shared asset files. One local inspection found 240 uniquely matched clips with compatible metadata, object layout and FSB codec, accounting for 30,393,696 Android bytes; two clips totaling 26,304 bytes had no match. This only identifies candidates: it does not replace the clips, check how they sound, or remove them from the template.

`tools/experimental-packed-steam-audio.py` can produce a local OBB with those packed clips replaced from Steam and adjusted audio offsets and sizes. `tools/verify-packed-steam-audio.py` checks the Steam payloads, preserved unmatched clips, other serialized bytes and unrelated OBB entries. In one test, all 240 replacements matched their Steam sources, both unmatched clips and 817 unrelated entries were preserved, and decoding each old/new pair produced matching WAV channel, rate, sample width and frame counts. Decoding is optional and requires `fmod_toolkit` and its native dependencies; matching properties do not establish audible equivalence. Playback has not been tested in Android. Its output still contains other game data and must remain local.

`tools/experimental-packed-audio-plan.py` generates an offset-and-hash plan and a template from that local packed-audio OBB, then restores the matching bytes from Steam. In one round trip it masked 56,167,904 bytes of Steam-encoded audio from 240 clips and restored all 922 OBB entries byte-for-byte. This plan is specific to its exact input files, and its template retains other game data and must remain local.

`SteamPackedAudio.java` implements experimental restoration of that plan on Android with bounded temporary files and a caller-supplied trusted plan hash. A standalone iPlay test restored the same 922 entries and produced a byte-identical complete OBB to the tested desktop JDK. Incorrect plan hash, overlapping records, template and Steam input were rejected without output. This does not test playback or make the template redistributable.

For research, `tools/build-generated-audio-launcher.py` also accepts `--packed-plan` and `--intermediate-obb` together. The launcher restores packed audio first and individual clips second, verifies each trusted plan, and installs only the validated final OBB. The intermediate OBB must use the Java converter's canonical ZIP representation; `tools/rebind-audio-plan.py` can bind the second plan to it only after checking that every entry matches the original intermediate template. An integration test on the desktop checked both failure paths, rollback, cleanup and successful import. The resulting research APK still embeds other game assets and is not redistributable.

`tools/experimental-exact-texture-plan.py` investigates uniquely matched, byte-identical Steam texture payloads inside split Unity asset files. After handling Steam's external texture streams, a local test masked and restored 4,062,116 bytes from eight textures across 12 fragments, reproducing all 922 OBB entries. Most other textures used different formats and were not covered. This is a local experiment, not integrated with Android; its template retains other game data and must remain private.

`tools/experimental-converted-textures.py` evaluates heuristic name-and-dimension matches by converting Steam mipmaps to the Android texture formats and measuring level-zero RGBA differences. In one local study it converted 839 candidates totaling 43,668,436 bytes; 819 unrelated archive entries and all other serialized bytes were unchanged. For 445 candidates the largest mean channel error was at most 5 on a 0–255 scale, while some differed much more. Mean error can hide important localized defects and does not prove correct mapping, alpha, normal-map handling, visual quality, gameplay behavior or deterministic Android output. Its OBB and detailed report remain private.

`tools/inspect-converted-texture-quality.py` verifies the exact edited ranges and also compares each converted level-zero image composited on black and white, including the proportion of pixels with a channel difference above 32. This avoids treating colors of fully transparent pixels as visible differences but remains an imperfect quality measure; both the report and its input OBBs stay local.

The verifier accepts both shared and standalone texture groups from the current study report. On the `04b9e84` research build it verified all 783 converted texture ranges and 722 unrelated archive entries. Of those textures, 123 had a channel error above 32 on more than 5% of pixels on at least one of the two backgrounds. This is a diagnostic comparison against the prior Android build, not a count of confirmed visible defects; it shows why the mean-error filter alone is insufficient. A separate trial disabling the ETC encoder's heuristics did not meaningfully improve the previously banded sprite, which remains excluded from conversion.

`tools/prepare-texture-native-comparison.py` creates private per-mip fixtures from the local Steam input and converted study; `tools/run-texture-native-comparison.py` runs them on an explicitly selected Android device and cleans up the transferred files. In one iPlay test, all 3,462 mip conversions from those 839 candidates matched the tested desktop converter byte-for-byte. This validates only those inputs and conversion outputs, not the heuristic mapping, game rendering, visual fidelity or the final launcher. The fixtures contain game data and must not be shared.

`tools/experimental-converted-texture-plan.py` generates local offset-and-hash reconstruction plans for the converted study, including textures spanning split files. One round trip masked 43,668,436 bytes from 839 converted textures and restored all 922 archive entries byte-for-byte from Steam. `SteamPlannedTextures.java` implements the same plan using the native texture converter and bounded temporary files. A standalone iPlay test restored all 922 entries and rejected invalid plan hashes, incompatible templates, overlapping fragments and modified source hashes without producing an output. This reproduces the experimental conversion, including its visual differences, and has not been tested in gameplay.

The research launcher builder also accepts `--texture-plan`, `--texture-intermediate-obb` and `--texture-library` together with the packed-audio options. It verifies the texture fragments, intermediate archives, ARM64 library and final OBB before building. The experimental launcher then reconstructs textures, packed audio and individual audio in that order. A desktop launcher integration test checked all three failure paths and successful transactional installation; a standalone iPlay test ran all three stages and reproduced the desktop OBB checksum. An APK was built, but its interactive import and gameplay have not been tested. Its template still contains other game assets and must remain private.

`tools/experimental-mesh-buffer-plan.py` generates a local offset-and-hash plan for exact vertex and index buffers found in Steam. In one study it masked 34,724,826 bytes from 673 meshes, then restored 1,379 fragments and reproduced all 922 archive entries. Nineteen empty meshes were not selected. `SteamPlannedMeshes.java` implements this plan on Android; a standalone iPlay test restored the same entries and produced the same full OBB checksum as the tested desktop JDK. Invalid plan hashes, templates, overlapping ranges and source checksums were rejected without output. Matching buffers does not reconstruct other mesh fields or establish gameplay fidelity.

The research launcher builder accepts `--mesh-plan` and `--mesh-intermediate-obb` together with the texture and audio options. It verifies the mesh stage before the texture stage, then builds a private APK that restores meshes, textures, packed audio and individual audio in order. A desktop launcher integration test exercised all four plan failure paths and successful installation; a standalone iPlay test ran the four stages and reproduced the desktop OBB checksum. The APK compiled, but its interactive import and gameplay remain untested. Other game data remains in the template, so this is not distributable.

`tools/experimental-converted-cubemaps.py` evaluates seven six-face Steam sky cubemaps converted to the Android format using a locally built Unity Crunch decoder. In one study every face had a largest mean RGBA channel error below 5 on a 0–255 scale relative to the existing Android texture; this is not a visual fidelity or gameplay guarantee. `tools/experimental-cubemap-plan.py` then masked and restored 5,505,024 converted bytes from those seven cubemaps, reproducing all 922 archive entries in a local round trip. The `SteamPlannedTextures.restoreCubemaps` experimental Android path uses the native Crunch decoder and texture converter; a standalone iPlay test restored the 922 entries and matched the desktop OBB checksum. Invalid plan hashes, incompatible templates, overlapping fragments and altered decoded hashes were rejected without output. The previous texture path was also rechecked on the desktop.

The research launcher builder accepts `--cubemap-plan` and `--cubemap-intermediate-obb` together with the mesh, texture and audio options. Its experimental launcher restores cubemaps, meshes, textures, packed audio and individual audio in that order. Desktop integration checked failure and rollback for each plan and successful installation; a standalone iPlay test ran all five stages and reproduced the desktop OBB checksum. The APK compiled, but its interactive import and gameplay remain untested. The template retains other game data and must stay private.

The experimental Android preparation source `SteamGeneratedAudio.java` can restore that generated plan from a Steam ZIP into a new OBB. A standalone run on an iPlay tablet reproduced the same 922 archive entries, and rejected an incorrect plan, template and Steam ZIP without creating output. It validates the expected plan and input hashes; the caller must supply a trusted expected plan hash. Its stored ZIP output was byte-identical between the tested desktop JDK and iPlay, allowing the launcher to validate the complete OBB hash.

`tools/build-generated-audio-launcher.py` builds an isolated, debug-signed research APK that invokes this path from the launcher. It requires a local Unity Gradle export, the corresponding runtime APK and workspace, the generated audio template and plan, and a locally reconstructed expected OBB. It checks the Unity expansion identifier, code metadata and all OBB entries before building; `--help` lists the paths, and `--check` validates without building. In a local iPlay test, importing the Steam ZIP through this APK generated the expected OBB and started a Forest run. The APK's template still contains other game assets, the sound was not audibly verified, and this is not the complete data-separated or redistributable build.

The ARM64 texture adapter can be built locally from the pinned source archive and the Unity Android NDK:

```sh
python3 tools/build-android-texture-library.py \
  --ndk /path/to/Unity/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK \
  --output /path/to/new/libopenbonk_texture.so \
  --runner steam-run
```

The build requires Python 3.12 or newer and validates source hashes before compiling. The resulting scalar ARM64 library matched 269 local game-texture fixtures, six Crunch cubemaps and 20 synthetic cases on Android; this does not cover every input or game version. See `third_party/` for source provenance and license notices.

To repeat the synthetic checks on an authorized ARM64 device without installing an app or transferring game data:

```sh
python3 tools/test-android-texture-library.py \
  --library /path/to/libopenbonk_texture.so \
  --device YOUR_ADB_SERIAL \
  --sdk /path/to/Unity/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK \
  --java-home /path/to/JDK
```

The tool removes its temporary device files after the test. It offers `--check` and an optional `--runner` for the JDK.

A local runtime build from the 1.3.14 export succeeded after the package-script and Addressables preparation above. Its script registry matched the 2,349 script IDs and identities in the previous runtime, but three empty script components remained in the scene build and its OBB had 207 ZIP entries versus 946 in the reference. One concrete gap is that the fresh export lacks the 21 populated `Resources/AndroidCharacters` assets used by the gameplay bootstrap. A comparison of 1,093 corresponding `MonoBehaviour` assets also found 741 whose 1.3.14 exports contain only an empty script header while the reference assets have additional serialized fields. Many asset entries therefore remain different or absent; neither gameplay equivalence nor a distributable APK has been established. The local Steam bundles must not be published as part of this experiment.

A later local runtime build using the 2.0.0 export and regenerated character resources also succeeded. Its script registry matched all 2,349 IDs and identities; its OBB had 919 ZIP entries versus 946 in the reference. A subsequent build with corrected character meshes and post-processing references produced 921 entries. After package remapping, 1,091 of 1,093 `MonoBehaviour` files matched byte-for-byte when GUID values were normalized. Those checks do not establish that GUIDs resolve to equivalent assets or that gameplay matches, and the remaining differences need investigation.

An isolated Android test package built from the local export reached the launcher, character selection and a Forest run with touch input. The 3D view was initially black; restoring the package post-processing shaders made the world visible. Restoring the skybox shader references fixed the white sky in the menu and in a Forest run on an iPlay tablet. This is a limited device smoke test, not full gameplay validation or proof that the final APK can be assembled.

`tools/extract-steam-character-data.py` is an experimental investigation into this gap. With local `GameAssemblies` and Python dependencies `UnityPy==1.25.4` and `TypeTreeGeneratorAPI==0.0.10`, it decoded 21 character records from a local Steam installation. Their character IDs and collider values matched the 21 reference resources. The output contains game data and must stay local; its Unity pointers have not yet been mapped to project assets, so it cannot currently restore the characters or make the build playable.

## Lua bridge

On a Linux x86-64 host with an Android NDK, set `ANDROID_NDK_HOME` or `ANDROID_NDK_ROOT`, then run `bash native/lua/build.sh`. It checks the Lua archive hash and builds the ARM64 library and smoke executable in `build/lua`. This builds the bridge only; it does not build the OpenBonk APK. The APK and game integration require the missing Unity project inputs described above.

The project includes reconstructed game-derived material and third-party components of different provenance. No blanket source license is asserted here; check the applicable rights and component notices before redistribution or reuse.
