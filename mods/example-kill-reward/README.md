# Victory Heart: experimental Lua item

Victory Heart requests 1 health and 1 XP per stack whenever an enemy dies while
the item is held. This event includes enemy deaths that may not be credited to
the player. Normal healing and XP rules apply. The experimental build with
`enemy_killed` action-table support is required; older builds cannot run this
mod correctly.

Run `python3 mods/example-kill-reward/build.py`, import the generated ZIP in
the OpenBonk launcher, and enable it under Manage Mods. The example's geometric
icon contains no game assets. This does not make the game APK distributable.
