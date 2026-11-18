"""Create editable rigs for the rooftop firefly and the coil mastiff."""
import math
from pathlib import Path
import sys

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from beacon_beetle import block, color, joint, preview, segment, volume


def firefly():
    shell = color("Midnight plum", (.14, .09, .25, 1))
    glass = color("Luminous citrine", (1, .76, .19, 1))
    membrane = color("Mint wing membrane", (.42, .79, .75, 1))
    vein = color("Blue wing frame", (.16, .36, .56, 1))
    root = joint("Rooftop firefly", None, (0, 0, 0))
    core = joint("Flight response", root, (0, 0, 0))
    volume("Thorax", (0, 0, .38), (.22, .31, .24), shell, core)
    volume("Lantern abdomen", (0, .38, .36), (.23, .32, .22), glass, core)
    block("Abdomen band", (0, .36, .51), (.29, .11, .09), shell, core)
    volume("Head", (0, -.28, .42), (.19, .18, .17), shell, core)
    wings = []
    for side in (-1, 1):
        volume("Eye", (side * .122, -.402, .46), (.075, .06, .08), glass, core)
        segment("Antenna", (side * .09, -.30, .55), (side * .21, -.49, .68), (.028, .012), vein, core)
        volume("Antenna lamp", (side * .21, -.49, .68), (.037, .039, .032), glass, core)
        hinge = joint("Wing hinge", core, (side * .16, 0, .51))
        wings.append((side, hinge))
        wing = volume("Faceted primary wing", (side * .40, .10, 0), (.43, .20, .023), membrane, hinge)
        wing.rotation_euler.z = side * .22
        segment("Primary wing spar", (0, 0, 0), (side * .78, .17, 0), (.044, .015), vein, hinge)
        wing = volume("Trailing wing", (side * .27, .31, -.019), (.33, .14, .018), membrane, hinge)
        wing.rotation_euler.z = side * .48
        for y in (-.08, .10):
            segment("Folded leg", (side * .13, y, .24), (side * .25, y + .14, .13), (.028, .015), vein, core)

    def pose(name, phase, frame):
        core.location.z = .07 * math.sin(phase)
        core.rotation_euler.x = .17 * math.sin(phase) - (.14 if name == "Walk" else 0)
        core.keyframe_insert("location", frame=frame)
        core.keyframe_insert("rotation_euler", frame=frame)
        for side, hinge in wings:
            hinge.rotation_euler.y = side * (.11 + .82 * math.sin(3 * phase))
            hinge.keyframe_insert("rotation_euler", frame=frame)

    animate(("Walk", "Idle"), pose)


def mastiff():
    armor = color("Clay-red ceramic armor", (.71, .26, .11, 1))
    chassis = color("Graphite joints", (.059, .095, .14, 1))
    enamel = color("Unpainted ceramic", (.76, .66, .43, 1))
    current = color("Amber coils", (1, .56, .077, 1))
    lens = color("Ice-blue optic", (.50, .92, 1, 1))
    root = joint("Coil mastiff", None, (0, 0, 0))
    body = joint("Body pitch", root, (0, 0, 0))
    volume("Armored barrel chest", (0, -.16, .96), (.49, .87, .43), armor, body)
    volume("Narrow hip housing", (0, .64, .76), (.36, .43, .3), chassis, body)
    segment("Coil housing", (0, -.62, 1.26), (0, .63, 1.26), (.31, .26), chassis, body)
    for index in range(6):
        y = -.49 + index * .18
        segment("Coil bank", (0, y, 1.26), (0, y + .10, 1.26), (.32, .32), current, body)
    block("Dorsal armor strip", (0, .12, 1.59), (.23, 1.16, .11), enamel, body)
    neck = joint("Head lift", body, (0, -.79, 1.0))
    volume("Snout case", (0, -.2, .015), (.39, .4, .26), enamel, neck)
    block("Lower jaw", (0, -.36, -.155), (.52, .40, .16), chassis, neck)
    block("Nose", (0, -.585, -.046), (.32, .14, .125), armor, neck)
    tail = joint("Antenna tail", body, (0, .87, .9))
    segment("Tail conductor", (0, 0, 0), (0, .50, .32), (.092, .052), chassis, tail)
    volume("Tail warning lamp", (0, .51, .327), (.095, .095, .08), current, tail)
    legs = []
    for side in (-1, 1):
        segment("Ceramic ear plate", (side * .24, -.04, .18), (side * .32, -.005, .51),
                (.13, .038), armor, neck)
        block("Slit optic", (side * .259, -.422, .077), (.14, .09, .065), lens, neck)
        for hind, y in enumerate((-.52, .57)):
            hip = joint("Hip", body, (side * .30, y, .80))
            bend = .15 if hind else -.13
            knee = (side * .14, bend, -.37)
            segment("Ceramic upper leg", (0, 0, 0), knee, (.18, .126), armor, hip)
            lower = joint("Hock", hip, knee)
            segment("Hock actuator", (0, 0, 0), (0, -.10, -.34), (.112, .087), chassis, lower)
            block("Foot", (0, -.154, -.345), (.24, .33, .145), enamel, lower)
            legs.append((side, hind, hip, lower))

    def pose(name, phase, frame):
        walk, charge = name == "Walk", name == "Charge"
        crouch = math.sin(phase / 2) if charge else 0
        body.location.z = -.16 * crouch + (.028 if walk else .012) * math.sin(phase * 2)
        body.rotation_euler.x = -.25 * crouch + .035 * math.sin(phase)
        body.keyframe_insert("location", frame=frame)
        body.keyframe_insert("rotation_euler", frame=frame)
        neck.rotation_euler.x = -.2 * crouch + .06 * math.sin(phase)
        neck.keyframe_insert("rotation_euler", frame=frame)
        tail.rotation_euler.z = .22 * math.sin(phase)
        tail.keyframe_insert("rotation_euler", frame=frame)
        for side, hind, hip, lower in legs:
            cycle = phase + ((side > 0) + hind) * math.pi
            hip.rotation_euler.x = .41 * math.sin(cycle) if walk else (.26 if hind else -.16) * crouch
            lower.rotation_euler.x = -.32 * max(0, math.cos(cycle)) if walk else .24 * crouch
            hip.keyframe_insert("rotation_euler", frame=frame)
            lower.keyframe_insert("rotation_euler", frame=frame)

    animate(("Walk", "Idle", "Charge"), pose)


def reed_runner():
    leaf = color("Green ceramic", (.09, .51, .27, 1))
    clay = color("Ochre belly", (.73, .35, .09, 1))
    seed = color("Ivory ear tips", (.86, .81, .40, 1))
    eye = color("Plum eyes", (.098, .045, .14, 1))
    root = joint("Reed runner", None, (0, 0, 0))
    body = joint("Spring hips", root, (0, 0, .48))
    volume("Pear body", (0, .1, 0), (.29, .37, .36), leaf, body)
    volume("Mask", (0, -.27, .22), (.32, .23, .27), clay, body)
    fans, feet = [], []
    for side in (-1, 1):
        volume("Dark eye", (side * .16, -.44, .29), (.066, .062, .085), eye, body)
        fan = joint("Leaf vane", body, (side * .14, -.18, .40))
        segment("Stem", (0, 0, 0), (side * .19, .18, .50), (.09, .039), leaf, fan)
        volume("Seed tip", (side * .21, .20, .50), (.12, .13, .16), seed, fan)
        fans.append((side, fan))
        foot = joint("Foot", root, (side * .22, .03, .18))
        volume("Ankle", (0, 0, 0), (.13, .16, .15), leaf, foot)
        volume("Padded toes", (0, -.065, -.08), (.16, .19, .097), clay, foot)
        feet.append((side, foot))

    def pose(name, phase, frame):
        walk = name == "Walk"
        body.location.z = .48 + (.10 if walk else .023) * abs(math.sin(phase))
        body.rotation_euler.z = (.14 if walk else .055) * math.sin(phase)
        body.keyframe_insert("location", frame=frame)
        body.keyframe_insert("rotation_euler", frame=frame)
        for side, fan in fans:
            fan.rotation_euler.x = .28 * math.sin(phase + side)
            fan.keyframe_insert("rotation_euler", frame=frame)
        for side, foot in feet:
            foot.location.y = (.11 * side * math.sin(phase)) if walk else .03
            foot.location.z = .18 + (.13 * max(0, side * math.cos(phase)) if walk else 0)
            foot.keyframe_insert("location", frame=frame)

    animate(("Walk", "Idle"), pose)


def river_manta():
    skin = color("Ink blue", (.054, .17, .32, 1))
    fin = color("Green-blue fins", (.05, .45, .46, 1))
    edge = color("Pearl trim", (.69, .87, .76, 1))
    warm = color("Amber eyes", (1, .51, .09, 1))
    root = joint("River manta", None, (0, 0, 0))
    core = joint("Glide pivot", root, (0, 0, .61))
    volume("Spade-shaped torso", (0, -.04, 0), (.59, .88, .30), skin, core)
    volume("Pale underside", (0, -.33, -.19), (.51, .63, .16), edge, core)
    rudder = joint("Tail pivot", core, (0, .60, 0))
    segment("Tail", (0, 0, 0), (0, 1.19, -.19), (.11, .042), skin, rudder)
    volume("Tail fin", (0, 1.19, -.19), (.20, .22, .08), fin, rudder)
    wings = []
    for side in (-1, 1):
        volume("Amber eye", (side * .29, -.69, .09), (.094, .071, .069), warm, core)
        wing = joint("Wing root", core, (side * .37, -.14, -.03))
        plane = volume("Wing", (side * .76, .29, -.03), (.83, .47, .106), fin, wing)
        plane.rotation_euler.z = side * .32
        segment("Fin bar", (0, -.26, .05), (side * 1.48, .47, .02), (.075, .024), edge, wing)
        wings.append((side, wing))

    def pose(name, phase, frame):
        core.location.z = .61 + .07 * math.sin(phase)
        core.rotation_euler.x = (.10 if name == "Walk" else -.11) + .05 * math.cos(phase)
        core.keyframe_insert("location", frame=frame)
        core.keyframe_insert("rotation_euler", frame=frame)
        rudder.rotation_euler.z = .24 * math.sin(phase)
        rudder.keyframe_insert("rotation_euler", frame=frame)
        for side, wing in wings:
            wing.rotation_euler.y = side * .48 * math.sin(phase)
            wing.keyframe_insert("rotation_euler", frame=frame)

    animate(("Walk", "Idle"), pose)


def glass_spark():
    violet = color("Smoky violet glass", (.22, .14, .50, 1))
    blue = color("Glacial glass", (.18, .65, .88, 1))
    white = color("Charged edge", (.86, .97, 1, 1))
    root = joint("Glass spark", None, (0, 0, 0))
    core = joint("Glass wheel", root, (0, 0, .70))
    volume("Inner prism", (0, 0, 0), (.29, .22, .32), blue, core)
    for index in range(5):
        angle = math.tau * index / 5
        x, z = math.cos(angle), math.sin(angle)
        segment("Prism spine", (.15 * x, 0, .15 * z), (.59 * x, 0, .59 * z), (.15, .025), violet, core)
    volume("Lamp face", (0, -.195, 0), (.15, .049, .17), white, core)

    def pose(name, phase, frame):
        core.location.z = .70 + .12 * math.sin(phase)
        core.rotation_euler.y = phase * (1 if name == "Walk" else .0) + .07 * math.sin(phase)
        core.keyframe_insert("location", frame=frame)
        core.keyframe_insert("rotation_euler", frame=frame)

    animate(("Walk", "Idle"), pose)


def relay_keeper():
    stone = color("Plum armor", (.25, .135, .37, 1))
    ridge = color("Red-violet ridge", (.60, .19, .32, 1))
    glow = color("Blue-white current", (.47, .83, 1, 1))
    root = joint("Relay keeper", None, (0, 0, 0))
    torso = joint("Torso", root, (0, 0, 1.04))
    volume("Tall core", (0, 0, .36), (.54, .46, .78), stone, torso)
    block("Bright core door", (0, -.39, .57), (.54, .17, .63), glow, torso)
    crown = joint("Fin crest", torso, (0, .02, .89))
    segment("Large crest", (0, 0, 0), (0, .29, .79), (.29, .06), ridge, crown)
    legs = []
    for side in (-1, 1):
        for row in (-1, 1):
            leg = joint("Tripod strut", torso, (side * .36, row * .27, -.18))
            segment("Long strut", (0, 0, 0), (side * .32, row * .38, -.71), (.18, .127), ridge, leg)
            volume("Foot", (side * .32, row * .42, -.73), (.22, .21, .13), stone, leg)
            legs.append((side * row, leg))
        segment("Signal comb", (side * .27, 0, .22), (side * .67, .16, .83), (.21, .059), stone, torso)
        volume("Signal gem", (side * .67, .16, .83), (.13, .13, .19), glow, torso)

    def pose(name, phase, frame):
        walk = name == "Walk"
        torso.location.z = 1.04 + (.12 if walk else .04) * math.sin(phase * 2)
        torso.rotation_euler.z = (.15 if walk else .045) * math.sin(phase)
        torso.keyframe_insert("location", frame=frame)
        torso.keyframe_insert("rotation_euler", frame=frame)
        crown.rotation_euler.x = .23 * math.sin(phase)
        crown.keyframe_insert("rotation_euler", frame=frame)
        for stride, leg in legs:
            leg.rotation_euler.x = (.34 if walk else .068) * stride * math.sin(phase)
            leg.keyframe_insert("rotation_euler", frame=frame)

    animate(("Walk", "Idle"), pose)


def animate(names, pose):
    scene = bpy.context.scene
    scene.render.fps = 12
    for index, name in enumerate(names):
        start = 1 + index * 16
        scene.timeline_markers.new(name, frame=start)
        for step in range(13):
            pose(name, step * math.tau / 12, start + step)
    scene.frame_start, scene.frame_end = 1, start + 12
    scene.frame_set(1)


def main():
    destination = Path(sys.argv[sys.argv.index("--") + 1])
    destination.mkdir(parents=True, exist_ok=True)
    creatures = (
        ("rooftop-firefly", firefly, 2.6), ("coil-mastiff", mastiff, 4.0),
        ("reed-runner", reed_runner, 2.0), ("river-manta", river_manta, 4.8),
        ("glass-spark", glass_spark, 2.1), ("relay-keeper", relay_keeper, 3.2),
    )
    for name, create, scale in creatures:
        if (destination / (name + ".blend")).exists():
            continue
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.context.scene.world = bpy.data.worlds.new("Studio")
        create()
        preview(destination / (name + ".png"))
        bpy.context.scene.camera.data.ortho_scale = scale
        bpy.ops.wm.save_as_mainfile(filepath=str(destination / (name + ".blend")))


if __name__ == "__main__":
    main()
