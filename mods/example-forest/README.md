# Example Forest mod

This experimental API 3 map uses the Forest template, a generated icon, and
four Lua callbacks. It requests 10% more ordinary spawns and 10% more enemy
health. Speed and damage stay unchanged. The existing game still controls map
generation, enemy behavior, bosses and progression.

Run `python3 mods/example-forest/build.py` from the project root to create
`example-forest.zip`. Import the ZIP in the OpenBonk launcher and enable it in
Manage Mods, then select Example Forest in the map menu. Rebuilding the ZIP
updates file hashes after changes to `main.lua`. Edit the map recipe in
`build.py`; see the external-map section of `native/lua/README.md` for bounds
and limitations. Give a derivative a different package and map ID.

This example requires an experimental build containing external-map support.
The Retroid APK installed on 2026-11-10 includes that support. On 2026-11-12,
an isolated newer research build on Retroid imported and enabled this ZIP from
the launcher, displayed Example Forest in map selection, and started a short
run with the configured five-minute stage timer. This check did not measure the
Lua multipliers, complete the stage, or verify persistence. It is not an example
of arbitrary new map geometry, custom enemies or unrestricted Lua.
