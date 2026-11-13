# Practice Arrow

Run `python3 build.py` to create `example-arrow-weapon.zip`, then import and
enable it from the OpenBonk launcher. This experimental API 3 weapon uses the
game's Bow attack and projectile; every third Lua attack deals 50% more damage.
It uses an original procedural icon and contains no game assets.

This requires an OpenBonk build with `arrow` host support. The host currently
does not accept a custom projectile model, sound, or arbitrary engine actions.
