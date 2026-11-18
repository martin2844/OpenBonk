"""Editable plaza landmarks, centered at ground level with their fronts facing -Y."""
from beacon_beetle import block, color, joint, segment


def landmarks():
    frame = color("Midnight blue steel", (.035, .075, .108, 1))
    concrete = color("Warm stone", (.72, .63, .46, 1))
    azure = color("Sky glass", (.12, .50, .74, 1))
    jade = color("Jade enamel", (.05, .48, .33, 1))
    coral = color("Coral enamel", (.85, .17, .078, 1))
    gold = color("Warm marker light", (1, .70, .16, 1))
    lilac = color("Lilac porcelain", (.45, .30, .68, 1))

    for name, coating in (("TerminalDepot", coral), ("InterchangePavilion", jade), ("RelayBeacon", lilac)):
        root = joint(name, None, (0, 0, 0))
        block("Ground plinth", (0, 0, .19), (8.5, 6.4, .38), concrete, root)
        if name == "TerminalDepot":
            block("Station hall", (0, 0, 1.78), (7.8, 5.8, 3.1), concrete, root)
            block("Red canopy", (0, -.28, 3.55), (8.3, 6.1, .47), coating, root)
            block("Recessed station front", (0, -2.87, 1.36), (3.1, .26, 2.0), frame, root)
            for x in (-2.8, 2.8):
                block("Ticket window", (x, -2.92, 1.77), (1.42, .15, 1.1), azure, root)
            block("Route board", (0, -3.0, 2.92), (2.8, .18, .52), frame, root)
            for x in (-.68, 0, .68):
                block("Route marker", (x, -3.11, 2.92), (.34, .10, .23), gold, root)
        elif name == "InterchangePavilion":
            block("Lower pavilion", (0, 0, 1.11), (7.5, 4.8, 1.51), concrete, root)
            for x in (-2.75, 0, 2.75):
                for y in (-1.62, 1.62):
                    block("Platform columns", (x, y, 2.80), (.52, .58, 2.8), frame, root)
            block("Upper glass concourse", (0, 0, 3.9), (7.8, 3.8, 1.15), azure, root)
            block("Jade canopy", (0, 0, 4.64), (8.4, 4.55, .35), coating, root)
            for x in (-3.3, -1.1, 1.1, 3.3):
                block("Window mullion", (x, -1.95, 3.9), (.16, .14, 1.05), frame, root)
        else:
            block("Relay housing", (0, 0, 1.42), (5.76, 4.55, 2.30), coating, root)
            segment("Tapered mast", (0, 0, 2.53), (0, 0, 8.1), (.81, .30), frame, root)
            for height, width in ((4.7, 2.9), (6.0, 2.2)):
                segment("Signal boom", (-width, 0, height), (width, 0, height), (.14, .14), azure, root)
                for side in (-1, 1):
                    segment("Rising aerial", (side * width, 0, height), (side * (width + .23), 0, height + .77),
                            (.16, .07), gold, root)
            segment("Beacon lens", (0, 0, 7.9), (0, 0, 8.48), (.46, .31), gold, root)
        # The visible base defines the pivot even though the plinth has bevels.
        yield root, [obj for obj in root.children if obj.type == "MESH"]
