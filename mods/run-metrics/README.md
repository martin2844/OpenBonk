# Run Metrics (experimental)

An independent adaptation inspired by [StatHUD](https://github.com/Helix128/StatHUD). It displays current XP and gold, positive XP and gold gained per second, and weapon damage per second. The original source does not identify a license; this implementation does not copy its code or artwork.

This mod requires an OpenBonk host supporting `run_metrics`. It is not compatible with older published APKs. The launcher installs it disabled; enable or disable it in Manage Mods before starting the game.

Resource rates sum positive applied changes in approximately the last second. DPS sums actual enemy health removed via the game's weapon-damage path over approximately the last three seconds and divides by three. Armor reduces the amount, overkill is capped at remaining health, and rejected or deferred hits count only if damage is later applied. This includes Echo releases routed through that path, but excludes damage routed through player-other and external damage paths; it is not total damage from all sources. Buckets have 100 ms precision. The original smooths its DPS and polls resources; this adaptation uses bounded event buckets and does not smooth the display, so values need not match it exactly. The clock freezes while paused, rates reset between runs, and the HUD never captures touch or controller input. Enemy damage ranges and advanced StatHUD features are not implemented.

Experimental implementation. The importer, enable/disable flow, coexistence with Chest Tracker, and touch HUD were exercised on a Xiaomi Android device using an isolated development build. Additional devices, screen layouts and long runs still need testing.
