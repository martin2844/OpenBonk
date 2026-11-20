# Nimbo District

Development package for a terraced city in OpenBonk. Three districts have
distinct palettes, arena landmarks, local encounters, and boss rewards.
Each seed uses the Forest tile meshes. The optional Pokémon neighborhood
package uses smaller blocks, gentler elevation and authored Japanese shopfronts;
the base kit retains Forest dimensions. Slopes are avenues with sidewalks and
crosswalks, flat tiles are plazas, and cliffs use architectural facades.

## Build the package

1. Open `authoring/distrito-nimbo.blend` in Blender and bake its props with
   `blender -b authoring/distrito-nimbo.blend --python authoring/export_props.py -- /tmp/nimbo-props.json`
   from this directory.
2. In an Android-capable Unity project using the same Unity version and layer
   settings as OpenBonk, put `BuildDistrictBundle.cs` in `Assets/Editor` and
   import `NimboProp.shader`, `NimboArchitecture.shader`, `NimboTerrain.shader`,
   `NimboSky.shader`, `NimboWarning.shader`, and `NimboCreature.shader`. Enable Unity's built-in
   Particle System module for the capsule break effect.
   Use a project dedicated to authoring. The builder
   regenerates assets with the same names in `Assets/NimboAuthoring`.
3. Bake the enemy rigs using the commands below. Set `OPENBONK_NIMBO_PROPS` to
   the prop JSON, `OPENBONK_NIMBO_CREATURE` to the directory containing the
   creature JSON files, and `OPENBONK_NIMBO_BUNDLE` to an output
   directory. Run Unity in batch mode with `-buildTarget Android` and
   `-executeMethod BuildDistrictBundle.Run`.
4. Run `python3 build.py /tmp/distrito-nimbo.zip --props /path/to/nimbo-props`.

Import the ZIP through **Mods** in a development APK with tiled architecture and
map encounter support, enable the package, and select **Nimbo District** in the
game's map menu.

The current roster uses original models. Terminal features the Coil
Mastiff's charge, Interchange features the River Manta's sweep, and Relay
features the Relay Keeper's pulse. A defeated boss grants a district item
and opens an onward portal. The final portal completes and saves the run.
Red-and-white capsules have a custom shatter effect and use the base drop
rules.

## Creature art

The Blender files contain editable rigs and named timelines for the
current roster. Bake their mesh frames from this directory.

```sh
mkdir -p /tmp/nimbo-creatures
blender -b authoring/beacon-beetle.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/beetle.json
blender -b authoring/rooftop-firefly.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/firefly.json "Rooftop firefly" RooftopFirefly
blender -b authoring/coil-mastiff.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/mastiff.json "Coil mastiff" CoilMastiff
blender -b authoring/reed-runner.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/runner.json "Reed runner" ReedRunner
blender -b authoring/river-manta.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/manta.json "River manta" RiverManta
blender -b authoring/glass-spark.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/spark.json "Glass spark" GlassSpark
blender -b authoring/relay-keeper.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/keeper.json "Relay keeper" RelayKeeper
```

### Locally supplied Pokémon rigs

For the optional Pokémon package, place the decoded skinned GLBs in
`PRIVATE_ROOT/decoded/{15,16,19,20,22,24,34,6,112,130}.glb`, then run:

```sh
blender -b --python authoring/bake_private_creatures.py -- /path/to/PRIVATE_ROOT
```

This writes `PRIVATE_ROOT/creatures/*.json` and the companion species atlas
PNGs for `OPENBONK_NIMBO_CREATURE`. Keep each atlas beside its JSON.
Optional model IDs after the root restrict the bake. The baker poses the
weighted skeleton for every frame: birds flap during flight and hovering,
land creatures move their legs, arms, heads and tails, and serpents bend
through their spine chains. Mesh simplification happens once before skinning
so all frames preserve indexed topology, UV seams and their source diffuse
textures. Small meaningful components survive simplification; material-less
import helpers are omitted. Common enemies use 1,200–1,600 triangles, with
larger bosses using up to 3,200. Atlases are at most 512×512, except Charizard's
1024×1024 atlas. Source repeated UVs wrap within their padded atlas region.
The opaque `NimboCreature` shader supports directional light, fog, shadow
casting/receiving and the existing status tint/rim properties. All materials
are named `Nimbo` plus the species name, matching the runtime loader.
Rattata and Gyarados retain hard masks for their iris overlays; all body maps
remain opaque. Beedrill and Charizard retain their source two-sided surfaces.
Android atlases use high-quality ETC2 compression, with alpha only for the
two species that require it. Prop, capsule and creature materials use ordinary
draws (`enableInstancing=false`). This restored visible frontages in Android
GLES gameplay after instanced variants compiled but failed to show the meshes.
Terrain retains GPU instancing with batches capped at 16 objects; all shader
forward/shadow variants also omit per-instance light-probe/lightmap arrays to
fit the 256-vector vertex-uniform limit. The builder checks these draw modes
in both saved materials and the reopened bundle, before any Editor shader
substitution, including the materials referenced by each packed prop.
Charizard's two grayscale flame layers are mapped to a red/orange/yellow
fire palette; its body, wing and eye textures retain their source colors.

Every baked clip must change distances between mesh vertices; a translated or
rotated static mesh fails this check. Pidgey and Fearow must also have at least
0.4 model units of wingtip travel relative to the torso, while never dipping
more than 0.1 below the initial frame's floor. The baker writes
`animation-validation.json` with the measured deformation for all clips.
It also records vertices, triangle count, retained source components, atlas
size and raw mesh data bytes. Render actual exported frames for inspection:

```sh
blender -b --python authoring/render_private_creatures.py -- /path/to/PRIVATE_ROOT 16 112 130
```

This writes fixed-camera walk frames 00, 03, 06 and 09 for Pidgey, Rhydon and
Gyarados to `textured-previews/`; the renderer consumes the JSON and PNGs
used by Unity. Its studio lighting is a geometry/texture check, not a runtime
lighting or GPU performance measurement.

Run `python3 authoring/validate_private_creatures.py /path/to/PRIVATE_ROOT`
after a full bake to verify the indexed streams and save
`textured-quality-receipt.json`, including source/output hashes and calculated
mesh and ETC2 texture memory. These totals exclude Unity object overhead and
any retained CPU copies.

For actual Unity rendering, copy `ReviewNimboAssets.cs` into the authoring
project's `Assets/Editor`, set `OPENBONK_NIMBO_REVIEW` to an output directory,
and run `-executeMethod ReviewNimboAssets.Run` with graphics enabled. It
renders all ten species in four poses from front/side cameras and the
Japanese props, landscape additions and Mewtwo statue from four directions, with the saved materials,
daylight and shadows. Contact sheets, individual PNGs, `index.html` and
`review.json` record the results. Optionally set
`OPENBONK_NIMBO_REVIEW_BUNDLE` to a bundle path to review reopened assets
instead of the saved authoring assets.

Rebuild the Android bundle after rebaking. The optional neighborhood upgrade
below packages these species materials with the new street profiles.

## Japanese neighborhood 2.2

The optional Pokémon package uses **content API 4** and requires **OpenBonk
0.1.20-beta or newer**. Its displayed name is **Nimbo District**; the internal
`distrito-nimbo` identity stays stable so existing installations update in place.
Install the APK before updating the mod. Earlier importers reject the new
`whirlwind` attack, avoiding a silent fallback to incorrect stage timings.

The sunny Japanese environment mixes shops, vending machines, gachapon,
lanterns and Mewtwo shrines with 17 m apartments, 20.5 m shop towers, grassy
pockets and dirt paths. Roads use neutral-gray asphalt. Larger terrain rises
and a more varied height field create sustained upper and lower neighborhoods.
The center crossing stays 12 m wide, and spawn/boss tiles remain clear.

### Rebuild the bundle and package

The upgrade still starts from the optional Pokémon **1.4.0 ZIP**. The original
kit and creatures retain their previously reviewed sources. To reproduce that
base, use `japanese_streetscape.py`, `prepare_neighborhood.py`,
`bake_private_creatures.py` and the [Mewtwo source notes](authoring/MewtwoStatue.SOURCE.md).
The existing merged `props.json` must include the six Japanese frontages and
`MewtwoStatue`. Add the new landscape kit without modifying those meshes:

```sh
blender -b --python authoring/landscape_additions.py -- /path/to/landscape
python3 - /path/to/existing/props.json /path/to/landscape/landscape-props.json /path/to/new/props.json <<'PYMERGE'
import json, pathlib, sys
old, additions, output = map(pathlib.Path, sys.argv[1:])
kit, extra = json.loads(old.read_text()), json.loads(additions.read_text())
assert not ({p["name"] for p in kit["props"]} & {p["name"] for p in extra["props"]})
kit["props"] += extra["props"]
output.parent.mkdir(parents=True, exist_ok=True)
with output.open("x") as stream:
    json.dump(kit, stream, separators=(",", ":"), allow_nan=False)
PYMERGE
```

The helper saves editable `NimboLandscapeKit.blend`, `landscape-props.json`
and a geometry receipt. Apartment/shop floors, balconies, roof caps and service
rooms are modeled separately; their solid colliders keep overhangs clear. The
grass patch is low, opaque and collision-free.

Copy the current `BuildDistrictBundle.cs`, `ReviewNimboAssets.cs` and shaders
into the dedicated Unity authoring project. Set `OPENBONK_NIMBO_PROPS` to the
merged JSON, `OPENBONK_NIMBO_CREATURE` to the existing creature directory, and
`OPENBONK_NIMBO_BUNDLE` to an output directory. Run `BuildDistrictBundle.Run`
for Android. In a fresh process, run `ReviewNimboAssets.Run` with
`OPENBONK_NIMBO_REVIEW` set to a review directory and
`OPENBONK_NIMBO_REVIEW_ONLY=NimboApartmentCorner,NimboShopTower,NimboGrassPatch`
for four views of each added asset.

```sh
python3 authoring/package_neighborhood.py \
  /path/to/private-pokemon/distrito-nimbo-pokemon-1.4.0.zip \
  /path/to/bundle/nimbo-props \
  /path/to/nimbo-district-pokemon-2.2.0.zip
```

The packager sets API 4/version 2.2.0, the English display name, explicit
stage clocks and Fearow's whirlwind attack. It replaces bundle hashes,
assigns species materials and retains Mewtwo shrine visuals. Encounter
rosters, enemy statistics, rewards, Lua and other payloads remain unchanged.
Old scattered-house placements are omitted to keep streets unobstructed.
ZIP entries have stable names/timestamps; existing output ZIPs are not replaced.

| District | Tile width | Elevation step | Coverage | Added frontages |
|---|---:|---:|---:|---|
| Terminal | 28 m | 8 m | 0.95 | ApartmentCorner, GrassPatch |
| Interchange | 30 m | 9 m | 0.90 | ApartmentCorner, ShopTower, GrassPatch |
| Relay | 32 m | 10 m | 0.85 | ApartmentCorner, ShopTower, GrassPatch |

Each stage retains its existing curated shops and landmarks. The planner
matches grass placements to the terrain shader's continuous green-pocket
pattern; built-up areas mix taller blocks with the original low-rise shops.
Elevation targets are bounded to five steps above/below the entry level.
Ramps occur only on eligible connected maze segments; native Forest generation
and maps without street profiles keep their original behavior.

### Pacing and Fearow attacks

Nimbo 2.1 used nine minutes for every stage. Although that matched Forest's
27-minute total, it shifted the first and third stages' events. Version 2.2
uses `stageDurations: [600, 540, 480]` and `stageDuration: 600`, preserving both
native event times and the map-level enemy-density denominator.

| District | Stage clock | Swarms (elapsed) | Minibosses (elapsed) |
|---|---|---|---|
| Terminal | 10:00 | 4:00, 7:00 | 3:00, 8:00 |
| Interchange | 9:00 | 3:00, 6:00 | 2:00, 7:00 |
| Relay | 8:00 | 2:30, 4:00 | 1:30, 5:00 |

Reinforcement checks remain every 60 seconds; event durations match Forest.
The HUD counts down, while the table counts up. Main bosses still begin when
the player activates their altar, and boss death retains the native transition
to the final swarm. Pauses freeze the stage clock.

Fearow uses `attack: "whirlwind"` and the native player tornado's visual. Its
independent enemy behavior provides a warning before a fixed-direction,
dodgeable whirlwind. It keeps distance to cast, does not inherit player weapon
scripts/damage callbacks, and cleans up when its owner dies or leaves play.
Other enemies and the player's tornado retain their existing behavior.

`OpenBonkStreetLayoutCheck.Run` checks real combined terrain collision,
connections, spawn/boss clearance, deterministic placement and native Forest
regressions. `OpenBonkMapScheduleCheck.Run` checks exact clocks/events,
independent cloned timelines, legacy scalar recipes and malformed duration
arrays. `EnemyWhirlwindCheck.Run` checks the enemy-owned tornado behavior.
The opt-in Android schedule, Fearow and district campaign probes exercise the
loaded package and gameplay. See [2.2 validation](study/nimbo-2.2-validation.md)
for final results and limits; [2.1 validation](study/nimbo-2.1-validation.md)
is retained as historical evidence.
