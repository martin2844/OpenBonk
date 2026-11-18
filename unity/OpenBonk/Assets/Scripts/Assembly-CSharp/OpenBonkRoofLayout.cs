using System;
using System.Collections.Generic;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using UnityEngine;
using Object = UnityEngine.Object;

public static class OpenBonkRoofLayout
{
    [Serializable, UnityEngine.Scripting.Preserve]
    public sealed class Profile
    {
        public float roofFraction, bridgeFraction, riseFraction;
        internal Material[] architecture;

        internal Profile WithArchitecture(Material deck, Material facade, Material trim) => new Profile
        {
            roofFraction = roofFraction,
            bridgeFraction = bridgeFraction,
            riseFraction = riseFraction,
            architecture = new[] { deck, facade, trim },
        };

        internal bool IsValid() =>
            roofFraction >= .60f && roofFraction <= .82f &&
            bridgeFraction >= .18f && bridgeFraction <= .40f &&
            riseFraction >= .03f && riseFraction <= .10f &&
            riseFraction <= (.96f - roofFraction) * .55f;
    }

    internal sealed class Build : IDisposable
    {
        public GameObject buildings, collider;
        public List<GameObject> anchors;
        public Mesh visualMesh, collisionMesh;

        public void Dispose()
        {
            if (buildings != null) Object.DestroyImmediate(buildings);
            if (collider != null) Object.DestroyImmediate(collider);
            if (visualMesh != null) Object.DestroyImmediate(visualMesh);
            if (collisionMesh != null) Object.DestroyImmediate(collisionMesh);
        }
    }

    private sealed class Roof
    {
        public Vector3 center;
        public Vector2 half, offset;
        public float chamfer;
        public int level;
        public readonly List<Roof> neighbors = new List<Roof>(4);

        public float Extent(Vector3 direction) =>
            direction.x != 0 ? half.x + direction.x * offset.x : half.y + direction.z * offset.y;

        public Vector3[] Outline()
        {
            float left = -Extent(Vector3.left), right = Extent(Vector3.right);
            float back = -Extent(Vector3.back), front = Extent(Vector3.forward);
            if (chamfer == 0)
                return new[] { Point(left, back), Point(left, front), Point(right, front), Point(right, back) };
            return new[]
            {
                Point(left + chamfer, back), Point(left, back + chamfer),
                Point(left, front - chamfer), Point(left + chamfer, front),
                Point(right - chamfer, front), Point(right, front - chamfer),
                Point(right, back + chamfer), Point(right - chamfer, back),
            };
        }

        private Vector3 Point(float x, float z) => center + new Vector3(x, 0, z);
    }

    internal static Build Generate(NodeTree tree, int seed, MapParameters parameters, StageData stage, Profile profile)
    {
        if (!profile.IsValid()) throw new ArgumentException("Invalid roof profile");
        float cellWidth = parameters.scaledTileWidth;
        float rise = cellWidth * profile.riseFraction;
        float bridgeHalfWidth = cellWidth * profile.bridgeFraction * .5f;
        var roofs = new Dictionary<Vector2Int, Roof>();
        var random = new System.Random(seed);
        var pieces = new SurfaceMesh(profile.architecture != null);
        // Footprint choices must not affect the seed's connections or elevations.
        var shapeRandom = new System.Random(unchecked(seed ^ 0x53e4b12d));
        var pending = new Queue<NodeTree>();
        pending.Enqueue(tree);
        while (pending.Count > 0)
        {
            var node = pending.Dequeue();
            var parent = node.parent == null ? null : roofs[node.parent.position];
            int level = parent == null ? 0 : Mathf.Clamp(parent.level + random.Next(-1, 2), -3, 3);
            var roof = new Roof
            {
                level = level,
                center = new Vector3((node.position.x - (parameters.size - 1) * .5f) * cellWidth,
                    level * rise, (node.position.y - (parameters.size - 1) * .5f) * cellWidth),
                half = new Vector2(RoofHalfExtent(random, profile, cellWidth), RoofHalfExtent(random, profile, cellWidth)),
            };
            if (pieces.Stylized) ShapeRoof(roof, node.position, profile, bridgeHalfWidth, cellWidth, shapeRandom);
            roofs.Add(node.position, roof);
            if (parent != null) Connect(roof, parent);
            foreach (var child in node.children) pending.Enqueue(child);
        }

        // A few shortcuts keep the outer branches from becoming one long corridor.
        for (int x = 0; x < parameters.size; x++)
            for (int z = 0; z < parameters.size; z++)
            {
                var position = new Vector2Int(x, z);
                var roof = roofs[position];
                TryShortcut(roof, position + Vector2Int.right, roofs, random);
                TryShortcut(roof, position + Vector2Int.up, roofs, random);
            }

        var entrance = roofs[tree.position];
        var exit = FindFarthest(entrance);
        float foundation = -3f * rise - .68f * cellWidth;
        foreach (var roof in roofs.Values)
        {
            int block = Mathf.FloorToInt(roof.center.x / (cellWidth * 2.1f)) * 3 +
                Mathf.FloorToInt(roof.center.z / (cellWidth * 2.3f)) * 7;
            int paint = (block % FacadeColors.Length + FacadeColors.Length) % FacadeColors.Length;
            var outline = roof.Outline();
            if (pieces.Stylized)
            {
                pieces.CoverRoof(roof, outline, paint);
                if (roof == entrance || roof == exit)
                    pieces.MarkPlaza(roof.center, cellWidth, roof == exit);
            }
            else pieces.Building(roof.center, roof.half, foundation, true, paint);
            AddRoofGuards(pieces, roof, outline, bridgeHalfWidth, foundation, paint);
            foreach (var neighbor in roof.neighbors)
                if (neighbor.center.x > roof.center.x || neighbor.center.z > roof.center.z)
                    AddBridge(pieces, roof, neighbor, bridgeHalfWidth);
        }
        AddSkyline(pieces, parameters.size, cellWidth, foundation, random);
        pieces.StreetLevel(cellWidth * (parameters.size * .5f + 2), foundation - .05f);
        if (pieces.Stylized) pieces.StreetGrid(parameters.size, cellWidth, foundation);

        var build = new Build();
        try
        {
            build.buildings = new GameObject("Rooftop District");
            build.visualMesh = pieces.Visual();
            build.buildings.AddComponent<MeshFilter>().sharedMesh = build.visualMesh;
            build.buildings.AddComponent<MeshRenderer>().sharedMaterials =
                profile.architecture ?? new[] { stage.flatMaterials[0], stage.m_fillMiddle, stage.m_fillTop };
            build.collider = new GameObject("Rooftop Ground") { layer = LayerMask.NameToLayer("Ground") };
            build.collisionMesh = pieces.Collision();
            build.collider.AddComponent<MeshFilter>().sharedMesh = build.collisionMesh;
            build.collider.AddComponent<MeshCollider>().sharedMesh = build.collisionMesh;
            build.anchors = new List<GameObject>(roofs.Count) { Anchor(build.buildings.transform, entrance, "Run entrance") };
            foreach (var roof in roofs.Values)
                if (roof != entrance && roof != exit)
                    build.anchors.Add(Anchor(build.buildings.transform, roof, "Roof anchor"));
            build.anchors.Add(Anchor(build.buildings.transform, exit, "Boss roof"));
            return build;
        }
        catch { build.Dispose(); throw; }
    }

    private static float RoofHalfExtent(System.Random random, Profile profile, float width) =>
        width * .5f * (profile.roofFraction + (float)(random.NextDouble() * .08 - .04));

    private static void ShapeRoof(Roof roof, Vector2Int cell, Profile profile, float bridge, float width, System.Random random)
    {
        float limit = width * (profile.roofFraction + .04f) * .5f;
        float clearance = bridge + width * .065f;
        int district = (cell.x / 2 + 2 * (cell.y / 2)) % 3;
        if (district != 0)
        {
            // Alternate wide, shallow roofs with narrow, deep terraces.
            float longEdge = limit - width * .02f;
            float shortEdge = Mathf.Max(clearance, roof.half[1] - width * .075f);
            roof.half = district == 1 ? new Vector2(longEdge, shortEdge) : new Vector2(shortEdge, longEdge);
        }
        float xShift = Mathf.Max(0, Mathf.Min(width * .022f, limit - roof.half.x, roof.half.x - clearance));
        float zShift = Mathf.Max(0, Mathf.Min(width * .022f, limit - roof.half.y, roof.half.y - clearance));
        roof.offset = new Vector2(xShift * (random.Next(2) == 0 ? -1 : 1), zShift * (random.Next(2) == 0 ? -1 : 1));
        if (district != 2)
        {
            float cornerRoom = Mathf.Min(roof.half.x - Mathf.Abs(roof.offset.x), roof.half.y - Mathf.Abs(roof.offset.y)) - bridge;
            roof.chamfer = Mathf.Max(0, Mathf.Min(width * (district == 0 ? .095f : .042f), cornerRoom - width * .012f));
        }
    }

    private static GameObject Anchor(Transform parent, Roof roof, string name)
    {
        var result = new GameObject(name);
        result.transform.SetParent(parent);
        result.transform.position = roof.center;
        return result;
    }

    private static void Connect(Roof a, Roof b)
    {
        a.neighbors.Add(b);
        b.neighbors.Add(a);
    }

    private static void TryShortcut(Roof roof, Vector2Int position, Dictionary<Vector2Int, Roof> roofs, System.Random random)
    {
        if (roofs.TryGetValue(position, out var neighbor) && Math.Abs(roof.level - neighbor.level) <= 1 &&
            !roof.neighbors.Contains(neighbor) && random.NextDouble() < .47)
            Connect(roof, neighbor);
    }

    private static Roof FindFarthest(Roof entrance)
    {
        var found = new HashSet<Roof> { entrance };
        var pending = new Queue<Roof>();
        pending.Enqueue(entrance);
        Roof last = entrance;
        while (pending.Count > 0)
        {
            last = pending.Dequeue();
            foreach (var neighbor in last.neighbors)
                if (found.Add(neighbor)) pending.Enqueue(neighbor);
        }
        return last;
    }

    private static readonly Vector3[] Edges = { Vector3.right, Vector3.forward, Vector3.left, Vector3.back };

    private static void AddRoofGuards(SurfaceMesh pieces, Roof roof, Vector3[] outline, float bridgeHalfWidth,
        float foundation, int paint)
    {
        for (int i = 0; i < outline.Length; i++)
        {
            var start = outline[i];
            var end = outline[(i + 1) % outline.Length];
            var along = (end - start).normalized;
            var outside = Vector3.Cross(along, Vector3.up);
            if (pieces.Stylized) pieces.Wall(start, end, foundation, FacadeColors[paint]);
            bool axis = Mathf.Abs(along.x) < .001f || Mathf.Abs(along.z) < .001f;
            bool hasBridge = axis && roof.neighbors.Exists(neighbor => Vector3.Dot(neighbor.center - roof.center, outside) > 0);
            if (!hasBridge)
            {
                pieces.Guard(start, end);
                continue;
            }
            var midpoint = roof.center + outside * roof.Extent(outside);
            pieces.Guard(start, midpoint - along * bridgeHalfWidth);
            pieces.Guard(midpoint + along * bridgeHalfWidth, end);
        }
    }

    private static void AddBridge(SurfaceMesh pieces, Roof roof, Roof neighbor, float halfWidth)
    {
        var delta = neighbor.center - roof.center;
        delta.y = 0;
        var direction = delta.normalized;
        var across = Vector3.Cross(direction, Vector3.up) * halfWidth;
        var start = roof.center + direction * roof.Extent(direction);
        var end = neighbor.center - direction * neighbor.Extent(-direction);
        var a = start - across;
        var b = start + across;
        var c = end + across;
        var d = end - across;
        pieces.Quad(a, b, c, d, 0, Vector2.one, true, new Color(.85f, .93f, 1f));
        pieces.Guard(d, a);
        pieces.Guard(b, c);
        pieces.Fascia(a, d);
        pieces.Fascia(c, b);
    }

    private static void AddSkyline(SurfaceMesh pieces, int size, float width, float foundation, System.Random random)
    {
        foreach (var outside in Edges)
        {
            var along = Vector3.Cross(outside, Vector3.up);
            for (int i = 0; i < size; i++)
            {
                var center = outside * width * (size * .5f + .6f + (float)random.NextDouble() * .5f) +
                    along * width * (i - (size - 1) * .5f);
                center.y = width * ((float)random.NextDouble() * 1.4f - .3f);
                var half = new Vector2(width * (.23f + (float)random.NextDouble() * .16f),
                    width * (.22f + (float)random.NextDouble() * .17f));
                if (!pieces.Stylized)
                {
                    pieces.Building(center, half, foundation - width * .35f, false);
                    continue;
                }

                int variant = (i + (outside.x == 0 ? 2 : 0)) % FacadeColors.Length;
                if (i % 4 == 0)
                {
                    center.y = -width * .14f;
                    half *= .88f;
                }
                pieces.Building(center, half, foundation - width * .35f, false, variant);
                if (i % 4 == 2)
                {
                    var setback = center + Vector3.up * width * (.52f + (float)random.NextDouble() * .39f) + along * width * .06f;
                    pieces.Building(setback, half * .66f, center.y - .03f, false, (variant + 2) % FacadeColors.Length);
                    var service = setback + Vector3.up * width * .13f - along * width * .06f;
                    pieces.Building(service, half * .33f, setback.y - .02f, false, 0);
                }
            }
        }
    }

    private static readonly Color[] FacadeColors =
    {
        new Color(1f, .97f, .86f), new Color(1f, .94f, .66f), new Color(.82f, .96f, .88f),
        new Color(.96f, .89f, 1f), new Color(1f, .89f, .79f),
    };
    private static readonly Color[] RoofColors =
    {
        new Color(.81f, .94f, .82f), new Color(.80f, .98f, .83f), new Color(.80f, .91f, 1f),
        new Color(.96f, .84f, 1f), new Color(1f, .90f, .74f),
    };

    private sealed class SurfaceMesh
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int>[] surfaces = { new List<int>(), new List<int>(), new List<int>() };
        readonly List<Vector3> collisionVertices = new List<Vector3>();
        readonly List<int> collisionFaces = new List<int>();
        public bool Stylized { get; }

        public SurfaceMesh(bool stylized) { Stylized = stylized; }

        public void StreetLevel(float halfSize, float height)
        {
            var repeat = Vector2.one * (halfSize / 20f);
            Quad(new Vector3(-halfSize, height, -halfSize), new Vector3(-halfSize, height, halfSize),
                new Vector3(halfSize, height, halfSize), new Vector3(halfSize, height, -halfSize), 0, repeat, false,
                new Color(.74f, .85f, .73f));
        }

        public void StreetGrid(int size, float width, float height)
        {
            float edge = size * width * .5f + width;
            var paving = new Color(.90f, .84f, .67f);
            var road = new Color(.44f, .57f, .64f);
            var line = new Color(.94f, .91f, .81f);
            for (int i = 1; i < size; i++)
            {
                float axis = (i - size * .5f) * width;
                foreach (bool north in new[] { true, false })
                {
                    float grade = height - (north ? .03f : .028f);
                    StreetStripe(north, axis, -edge, edge, grade, width * .103f, paving);
                    StreetStripe(north, axis, -edge, edge, grade + .007f, width * .065f, road);
                    for (int block = 0; block < size; block++)
                    {
                        float middle = (block - (size - 1) * .5f) * width;
                        StreetStripe(north, axis, middle - width * .14f, middle + width * .14f, grade + .012f,
                            width * .007f, line);
                    }
                }
            }
        }

        private void StreetStripe(bool north, float axis, float start, float end, float height, float halfWidth, Color color)
        {
            float left = axis - halfWidth, right = axis + halfWidth;
            float west = north ? left : start;
            float east = north ? right : end;
            float south = north ? start : left;
            float top = north ? end : right;
            Quad(new Vector3(west, height, south), new Vector3(west, height, top),
                new Vector3(east, height, top), new Vector3(east, height, south), 2, Vector2.one, false, color);
        }

        public void CoverRoof(Roof roof, Vector3[] outline, int variant)
        {
            var center = roof.center;
            var color = RoofColors[variant];
            Vector2 MapUv(Vector3 corner)
            {
                Vector3 local = corner - center;
                return new Vector2(1 + (local.x - roof.offset.x) / roof.half.x,
                    1 + (local.z - roof.offset.y) / roof.half.y);
            }
            for (int i = 0; i < outline.Length; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % outline.Length];
                AddTriangle(vertices, surfaces[0], center, a, b);
                uv.Add(MapUv(center));
                uv.Add(MapUv(a));
                uv.Add(MapUv(b));
                colors.Add(color); colors.Add(color); colors.Add(color);
                AddTriangle(collisionVertices, collisionFaces, center, a, b);
            }
        }

        public void MarkPlaza(Vector3 point, float width, bool arena)
        {
            point.y += .025f;
            int sides = arena ? 16 : 8;
            float radius = width * (arena ? .18f : .127f);
            float rim = width * .017f;
            var paint = arena ? new Color(.89f, .68f, .56f) : new Color(.97f, .86f, .55f);
            var core = new Color(.33f, .49f, .61f);
            for (int i = 0; i < sides; i++)
            {
                float from = i * 2f * Mathf.PI / sides, to = (i + 1) * 2f * Mathf.PI / sides;
                var a = new Vector3(Mathf.Cos(from), 0, Mathf.Sin(from));
                var b = new Vector3(Mathf.Cos(to), 0, Mathf.Sin(to));
                Quad(point + a * (radius - rim), point + b * (radius - rim),
                    point + b * radius, point + a * radius, 2, Vector2.one, false, paint);
                AddTriangle(vertices, surfaces[2], point, point + b * (radius - rim), point + a * (radius - rim));
                for (int vertex = 0; vertex < 3; vertex++)
                {
                    uv.Add(Vector2.zero);
                    colors.Add(core);
                }
            }
        }

        public void Building(Vector3 center, Vector2 half, float bottom, bool playable, int variant = 0)
        {
            var a = center + new Vector3(-half.x, 0, -half.y);
            var b = center + new Vector3(-half.x, 0, half.y);
            var c = center + new Vector3(half.x, 0, half.y);
            var d = center + new Vector3(half.x, 0, -half.y);
            Quad(a, b, c, d, 0, new Vector2(2, 2), playable, RoofColors[variant]);
            Color wall = FacadeColors[variant];
            Wall(a, b, bottom, wall);
            Wall(b, c, bottom, wall);
            Wall(c, d, bottom, wall);
            Wall(d, a, bottom, wall);
        }

        public void Wall(Vector3 a, Vector3 b, float bottom, Color color)
        {
            var baseA = new Vector3(a.x, bottom, a.z);
            var baseB = new Vector3(b.x, bottom, b.z);
            var lip = Vector3.up * .65f;
            var span = new Vector2(Vector3.Distance(a, b) / 15f, (a.y - bottom) / 14f);
            if (Stylized)
                span = new Vector2(Mathf.Max(.25f, Mathf.Round(span.x * 4) * .25f),
                    Mathf.Max(.25f, Mathf.Round(span.y * 4) * .25f));
            Quad(baseB, b - lip, a - lip, baseA, 1, span, false, color);
            Fascia(a, b);
        }

        public void Fascia(Vector3 a, Vector3 b) =>
            Quad(b - Vector3.up * .65f, b, a, a - Vector3.up * .65f, 2, Vector2.one, false, new Color(.44f, .56f, .68f));

        public void Guard(Vector3 a, Vector3 b)
        {
            var rise = Vector3.up * 1.1f;
            var offset = Vector3.Cross(Vector3.up, b - a).normalized * .11f;
            var nearA = a + offset;
            var nearB = b + offset;
            var farA = a - offset;
            var farB = b - offset;
            var tile = new Vector2(Vector3.Distance(a, b) / 10f, .18f);
            var enamel = new Color(.90f, .89f, .82f);
            var cap = new Color(.51f, .66f, .78f);
            Quad(nearA, nearA + rise, nearB + rise, nearB, 2, tile, true, enamel);
            Quad(farB, farB + rise, farA + rise, farA, 2, tile, true, enamel);
            // Caps are visual. A vertical ground query must not place a pickup on a parapet.
            Quad(nearA + rise, farA + rise, farB + rise, nearB + rise, Stylized ? 2 : 0, Vector2.one, false, cap);
            Quad(nearA, farA, farA + rise, nearA + rise, 2, Vector2.one, false, enamel);
            Quad(farB, nearB, nearB + rise, farB + rise, 2, Vector2.one, false, enamel);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material, Vector2 scale, bool solid,
            Color? tint = null)
        {
            AddQuad(vertices, surfaces[material], a, b, c, d);
            uv.Add(Vector2.zero);
            uv.Add(new Vector2(0, scale.y));
            uv.Add(scale);
            uv.Add(new Vector2(scale.x, 0));
            if (Stylized)
                for (int i = 0; i < 4; i++) colors.Add(tint ?? Color.white);
            if (solid) AddQuad(collisionVertices, collisionFaces, a, b, c, d);
        }

        private static void AddQuad(List<Vector3> points, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int first = points.Count;
            points.Add(a);
            points.Add(b);
            points.Add(c);
            points.Add(d);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }

        private static void AddTriangle(List<Vector3> points, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int first = points.Count;
            points.Add(a);
            points.Add(b);
            points.Add(c);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
        }

        public Mesh Visual()
        {
            var mesh = new Mesh { name = "Rooftop architecture" };
            if (vertices.Count > ushort.MaxValue) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.subMeshCount = surfaces.Length;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            if (Stylized) mesh.SetColors(colors);
            for (int i = 0; i < surfaces.Length; i++) mesh.SetTriangles(surfaces[i], i, false);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public Mesh Collision()
        {
            var mesh = new Mesh { name = "Rooftop walkable surfaces" };
            if (collisionVertices.Count > ushort.MaxValue) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(collisionVertices);
            mesh.SetTriangles(collisionFaces, 0, true);
            return mesh;
        }
    }
}
