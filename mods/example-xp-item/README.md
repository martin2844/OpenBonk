# XP Spark: example Lua item

XP Spark grants 3 XP per held stack each unpaused game second while the player
is alive. The game applies its normal XP multiplier and challenge rules.

Run `python3 mods/example-xp-item/build.py`, import the generated ZIP from the
OpenBonk launcher, and enable it in Manage Mods. It requires the experimental
build that supports the `xp` tick action; earlier builds ignore that action.
This sample uses a geometric icon and has no game assets. It is independent of
Pikachu and does not make the game or its other assets distributable.
