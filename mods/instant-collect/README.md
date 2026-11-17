# Instant XP collection (experimental)

An independently written Lua policy inspired by [InstantCollect by China1](https://thunderstore.io/c/megabonk/p/China1/InstantCollect/).
It does not include the original package, code or icon; the linked original
source repository was unavailable and no original license was identified.

The original package's managed binary statically shows a `SpawnPickup` postfix
that invokes `ApplyPickup` when the result is non-null and the pickup type is
XP. This is broader than the package description's enemy-kill example: XP
spawned by other sources is included. This implementation applies the normal
pickup path to each newly spawned or merged XP pickup, preserving existing XP
modifiers, events and pooling. It does not affect gold or other pickups.
The original Windows mod was not executed in this study, so full equivalence
with its runtime behavior is not established.

Requires a future OpenBonk APK with `collect_spawned_xp`; beta 0.1.3 does not
support it. The mod is disabled on import; enable it in the launcher and start
the game. Disable and restart to restore normal pickup behavior. It is not in
the public catalog. To create a local test ZIP, include only `manifest.json`
and `main.lua` at its root.

On a Xiaomi 22126RN91Y, an isolated IL2CPP test build loaded the Lua policy.
With the policy off, a 5-XP orb remained active; after enabling it, spawning
another 7 XP at the same location merged and collected the resulting 12 XP
with one pickup event and no duplicate after two seconds. Turning the policy
off in the same process restored ordinary collection: a 3-XP orb remained
active until collected and produced exactly one additional event and 3 XP.
The mod and enabled preference were staged manually; launcher import and
disable/restart were not tested on Android. The JVM harness separately tested
package import and the initial disabled preference. The temporary installation
was removed. This short controlled test does not establish all XP sources,
pool exhaustion, interactions with every item, or long-run fidelity.
