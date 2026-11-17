# Android integration study: 0.1.3 update

This is a local compatibility experiment, not a release build. No APK or mod
catalog update resulted from it. The device was a Xiaomi 22126RN91Y running
Lineage Android 17 with approximately 4 GB RAM. The official installation and
its files were not uninstalled or modified; the test used the separate package
`com.openbonk.integrationtest` and a local test signing key.

The baseline used the 0.1.3 launcher/runtime, repackaged under the test ID,
and the existing prepared 0.1.3 OBB (SHA-256
`9011ea73423c8418db7f7d0895c515b98a23001e947fd227b83c221a04e37475`).
The launcher verified it and the game entered Forest; a brief run generated
test progression and statistics. A real ZIP import was started but interrupted
after several minutes of preparation; the verified OBB was supplied directly,
so this does not establish ZIP-import behavior.

The experimental update incorporated the source hooks for Unlock Everything
and instant XP, the actual launcher validation code, and a separately compiled
Unity runtime with Rewired. It retained the old expected OBB hash. After an
in-place update, the launcher copied the OBB from version 4 to version 5; its
SHA-256 remained identical. Test `progression.json` and `stats.json` were
byte-identical to the saved pre-update copies, including after the failures
below. The launcher reported data ready.

Two partial runtime combinations failed:

1. Replacing only `libil2cpp.so` and `global-metadata.dat` in the baseline APK
   produced a native SIGSEGV at Unity startup.
2. Also replacing `globalgamemanagers`, its asset splits, `level0`,
   `boot.config` and `unity_app_guid` led to the system killing the game for
   memory pressure before reaching the menu. The device log reported about
   2.4 GB anonymous RSS for the test process at the kill.

These observations do **not** isolate the exact cause or prove the old OBB is
intrinsically incompatible with every correct rebuild. They show that these
mixed-artifact combinations are unsuitable for release. The standalone hook
probes used a separate newly generated OBB and did run; that is not evidence
of update compatibility. A candidate needs the full reconstruction pipeline,
matching runtime and game data, an audited update path from 0.1.3 (including a
verified delta if the OBB changes), and device tests before release. Launcher
import, enable/disable and gameplay for the two mods remain untested in a
working integrated APK. The temporary test app and its copied files were
removed from the device.
