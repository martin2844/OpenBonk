# Distrito Nimbo

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
map encounter support, enable the package, and select **Distrito Nimbo** in the
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
renders all ten species in four poses from front/side cameras and all six
Japanese prop prefabs from four directions, with the saved materials,
daylight and shadows. Contact sheets, individual PNGs, `index.html` and
`review.json` record the results. Optionally set
`OPENBONK_NIMBO_REVIEW_BUNDLE` to a bundle path to review reopened assets
instead of the saved authoring assets.

Rebuild the Android bundle after rebaking. The optional neighborhood upgrade
below packages these species materials with the new street profiles.

## Japanese neighborhood 2.1

The optional Pokémon neighborhood package uses **content API 4** and requires
**OpenBonk 0.1.18-beta or newer** for the terrace generator and Mewtwo shrine
visual. APKs earlier than 0.1.17 reject API 4; 0.1.17 can import this format but
does not implement those two additions. Its sunny Japanese streets combine
shops, vending machines, gachapon, lanterns, notice boards and a shrine, with
gentle ramps, sustained upper and lower districts, and distinct frontage sets.
The earlier `build.py` instructions and original-roster authoring section remain
the base-package workflow; this upgrade starts from the existing optional
Pokémon **1.4.0 ZIP**.

From this directory, generate the editable Japanese kit and prepare the merged
prop input. `NIMBO_PRIVATE` contains the previously exported
`nimbo-props-pokeballs.json` and the locally supplied Pokémon rigs.

```sh
NIMBO_PRIVATE=/path/to/private-pokemon
NIMBO_STUDY=/path/to/japan-study
NIMBO_NEIGHBORHOOD=/path/to/neighborhood
blender -b --python authoring/japanese_streetscape.py -- "$NIMBO_STUDY"
blender -b --python authoring/prepare_neighborhood.py -- \
  --input-old "$NIMBO_PRIVATE/nimbo-props-pokeballs.json" \
  --input-new "$NIMBO_STUDY/japanese-props.json" \
  --collision "$NIMBO_STUDY/collision-contract.json" \
  --output "$NIMBO_NEIGHBORHOOD/props.json"
blender -b --python authoring/bake_private_creatures.py -- "$NIMBO_PRIVATE"
```

Preparation preserves the ten legacy props and all new mesh geometry. It adds
solid-part collision boxes, keeps the lantern gate's 3.4 m opening clear, and
bakes vertex ambient occlusion on the six Japanese clusters. The adjacent
`props.validation.json` records input hashes, collision envelopes, opaque
colors and AO measurements.

Create the shrine visual from the supplied regular Mewtwo GLB using the
[statue source and reproduction notes](authoring/MewtwoStatue.SOURCE.md).
After decoding the source GLB, export and merge its collision-free visual:

```sh
blender -b --python authoring/mewtwo_statue.py -- /path/to/150-decoded.glb "$NIMBO_STUDY/mewtwo" --render
python3 - "$NIMBO_NEIGHBORHOOD/props.json" "$NIMBO_STUDY/mewtwo/MewtwoStatue.json" <<'PYMERGE'
import json, pathlib, sys
output, source = map(pathlib.Path, sys.argv[1:])
kit, statue = json.loads(output.read_text()), json.loads(source.read_text())
assert [p["name"] for p in statue["props"]] == ["MewtwoStatue"]
kit["props"] = [p for p in kit["props"] if p["name"] != "MewtwoStatue"] + statue["props"]
output.write_text(json.dumps(kit, separators=(",", ":"), allow_nan=False))
PYMERGE
```

The six-frontage preparation receipt describes the pre-statue input; the
statue's own receipt records its geometry and native-shrine fit. For a focused
Unity review, set `OPENBONK_NIMBO_REVIEW_ONLY=MewtwoStatue` with
`OPENBONK_NIMBO_REVIEW` and run `ReviewNimboAssets.Run`.

Import the current builder and shaders into the dedicated Unity authoring
project described above. Set `OPENBONK_NIMBO_PROPS` to the merged `props.json`,
`OPENBONK_NIMBO_CREATURE` to `NIMBO_PRIVATE/creatures`, and
`OPENBONK_NIMBO_BUNDLE` to `NIMBO_NEIGHBORHOOD/bundle`. Run
`BuildDistrictBundle.Run` for Android, then inspect the resulting props and
textured creatures in Unity before packaging. The bundle must also contain
the reviewed `MewtwoStatue` prefab, which all three stages reference as their
moai shrine visual. This replaces only the shrine's appearance; its interaction,
collider and rewards retain the game's existing behavior.

```sh
python3 authoring/package_neighborhood.py \
  "$NIMBO_PRIVATE/distrito-nimbo-pokemon-1.4.0.zip" \
  "$NIMBO_NEIGHBORHOOD/bundle/nimbo-props" \
  "$NIMBO_NEIGHBORHOOD/distrito-nimbo-pokemon-2.1.0.zip"
```

The packager sets API 4/version 2.1.0 and a 540-second stage duration, replaces
the bundle and file hashes, assigns each enemy its `Nimbo` + species material,
and adds `moaiVisual: "MewtwoStatue"` to each stage environment. It preserves
encounter rosters, enemy statistics, attacks, rewards, scripts and other payloads. Random object
placements are removed so old houses do not obstruct the new street fronts;
the optional `objects` property is omitted because the importers reject an
empty array. ZIP entries use sorted names and fixed timestamps, and an existing
output ZIP is never overwritten.

| District | Tile width | Elevation step | Coverage | Frontages (`Nimbo` prefix) |
|---|---:|---:|---:|---|
| Terminal | 26 m | 6 m | 0.95 | VendingCorner, GachaCorner, RamenFront, PosterGarden |
| Interchange | 28 m | 7 m | 0.90 | VendingCorner, GachaCorner, RamenFront, LanternGate, PosterGarden |
| Relay | 30 m | 8 m | 0.85 | RamenFront, LanternGate, PosterGarden, ShrineCorner |

### Pacing and elevation

Forest's three stage clocks are 10/9/8 minutes. Nimbo 2.0 shortened them to
7/7/7 minutes; 2.1 uses 9/9/9 minutes, restoring the same 27-minute total
scheduled stage time as Forest. Pauses and the player's choice of when to fight
the boss still affect actual run length.

The existing map recipe scales inherited event times, event durations and
reinforcement intervals together. Against Forest, the three Nimbo stages use
factors of 0.9, 1 and 1.125:

| District | Stage clock | Swarm times | Miniboss times | Reinforcement check |
|---|---|---|---|---|
| Terminal | 9:00 | 3:36, 6:18 | 2:42, 7:12 | 54 s |
| Interchange | 9:00 | 3:00, 6:00 | 2:00, 7:00 | 60 s |
| Relay | 9:00 | 2:49, 4:30 | 1:41, 5:38 | 67.5 s |

The streetscape-only generator follows a seeded, broad height field bounded
to four steps above or below the entry level. Elevation persists through turns
and branch junctions. Ramps occupy only straight, single-child maze tiles,
preserving all connected routes and the 12 m clear street crossing. The
6/7/8 m rises above are unchanged; variety comes from sustained elevation
districts. Forest and maps without a streetscape retain the native generator.

`OpenBonkStreetLayoutCheck.Run` exercises the actual combined terrain collider,
spawn/boss clearance, deterministic elevations and frontages, native Forest
regression, and the scaled nine-minute timelines. See
[the 2.1 validation receipt](study/nimbo-2.1-validation.md) for measured results
and final release validation.

### Building visibility regression check

`BuildDistrictBundle` matches triangle winding to the supplied outward normals;
it must not reverse every triangle. The prop exporter uses a rotation, not a
reflection, so unconditional reversal hid the near walls of small houses.

Copy `CheckDistrictMeshWinding.cs` into the authoring project's `Assets/Editor`.
With graphics enabled, set `OPENBONK_NIMBO_PROPS` to the prop JSON and
`OPENBONK_NIMBO_WINDING_CHECK` to an output directory, then execute
`CheckDistrictMeshWinding.Run`. It renders both sides of the actual access
houses and checks that a near wall occludes a far wall, including reflected
source coordinates.
