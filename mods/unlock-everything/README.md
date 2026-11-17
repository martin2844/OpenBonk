# Unlock Everything (experimental)

A Lua adaptation of [Unlock Everything by lukeod](https://github.com/lukeod/megabonk_unlockEverything), copyright 2025 lukeod, [MIT license](LICENSE).
It overrides the same seven availability, purchase, achievement, visibility and
eligibility queries as the original while enabled. It does not write unlocks to
the profile. Actual gameplay and purchase actions can still modify progression,
so keep a profile backup. Disable it in the launcher and restart the game to
restore normal queries.

This package requires a future OpenBonk build with the `unlock_all` hook; beta
0.1.3 cannot import or run it. It is not in the public catalog. Package only
`manifest.json` and `main.lua` at the root of a ZIP for testing.

On a Xiaomi 22126RN91Y, an isolated experimental IL2CPP build showed locked
characters and skins without the mod, showed them when enabled, restored the
locked state after disabling and restarting, and showed them again after
reenabling and restarting. The progression file remained byte-identical across
those menu-only checks. Calcium and Desert could be selected and entered in a
brief run. This test manually staged the mod and its enabled preference; ZIP
import and preference updates were exercised separately in the JVM harness,
not in the final Android launcher. It does not establish every unlock path,
purchase or long-run behavior.

Reproducible local packages for both examples can be generated with
`python tools/package-example-mods.py --output /path/to/output`. An integrated
Xiaomi build imported the packages through the launcher and loaded both hooks
when enabled; after disabling both and restarting, neither hook loaded.
The generated ZIPs contain exactly two files because the API 1 importer rejects
additional files. Attribution and any applicable license notice are in `main.lua`.
