# Golden Heart: experimental Lua item

Golden Heart requests two gold per stack when an enemy dies. This event can
include deaths not credited to the player. The game's normal gold multiplier
and per-frame limit apply. It requires a build with the `enemy_killed` gold
action; older builds ignore this field. Run `python3 mods/example-gold-reward/build.py`,
import the ZIP from the launcher and enable it in Manage mods. The icon is
procedural and the example contains no game assets.

On a separate research installation on Retroid, the launcher imported the ZIP
and a controlled test added the item and killed one enemy. Gold increased from
0 to 3 after the game's multiplier. Natural acquisition and a complete run have
not been checked. The normal OpenBonk installation has not been updated with
this feature.
