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
        public Vector2 half;
        public int level;
        public readonly List<Roof> neighbors = new List<Roof>(4);

        public float Extent(Vector3 direction) => direction.x == 0 ? half.y : half.x;
    }

    internal static Build Generate(NodeTree tree, int seed, MapParameters parameters, StageData stage, Profile profile)
    {
        if (!profile.IsValid()) throw new ArgumentException("Invalid roof profile");
        float cellWidth = parameters.scaledTileWidth;
        float rise = cellWidth * profile.riseFraction;
        float bridgeHalfWidth = cellWidth * profile.bridgeFraction * .5f;
        var roofs = new Dictionary<Vector2Int, Roof>();
        var random = new System.Random(seed);
        var pieces = new SurfaceMesh();
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
            pieces.Building(roof.center, roof.half, foundation, true);
            AddRoofGuards(pieces, roof, bridgeHalfWidth);
            foreach (var neighbor in roof.neighbors)
                if (neighbor.center.x > roof.center.x || neighbor.center.z > roof.center.z)
                    AddBridge(pieces, roof, neighbor, bridgeHalfWidth);
        }
        AddSkyline(pieces, parameters.size, cellWidth, foundation, random);
        pieces.StreetLevel(cellWidth * (parameters.size * .5f + 2), foundation - .05f);

        var build = new Build();
        try
        {
            build.buildings = new GameObject("Rooftop District");
            build.visualMesh = pieces.Visual();
            build.buildings.AddComponent<MeshFilter>().sharedMesh = build.visualMesh;
            build.buildings.AddComponent<MeshRenderer>().sharedMaterials =
                new[] { stage.flatMaterials[0], stage.m_fillMiddle, stage.m_fillTop };
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

    private static void AddRoofGuards(SurfaceMesh pieces, Roof roof, float bridgeHalfWidth)
    {
        foreach (var outside in Edges)
        {
            var along = Vector3.Cross(outside, Vector3.up);
            var midpoint = roof.center + outside * roof.Extent(outside);
            float half = roof.Extent(along);
            bool hasBridge = roof.neighbors.Exists(neighbor => Vector3.Dot(neighbor.center - roof.center, outside) > 0);
            if (hasBridge)
            {
                pieces.Guard(midpoint - along * half, midpoint - along * bridgeHalfWidth);
                pieces.Guard(midpoint + along * bridgeHalfWidth, midpoint + along * half);
            }
            else pieces.Guard(midpoint - along * half, midpoint + along * half);
        }
    }

    private static void AddBridge(SurfaceMesh pieces, Roof roof, Roof neighbor, float halfWidth)
    {
        var delta = neighbor.center - roof.center;
        delta.y = 0;
        var direction = delta.normalized;
        var across = Vector3.Cross(direction, Vector3.up) * halfWidth;
        var start = roof.center + direction * roof.Extent(direction);
        var end = neighbor.center - direction * neighbor.Extent(direction);
        var a = start - across;
        var b = start + across;
        var c = end + across;
        var d = end - across;
        pieces.Quad(a, b, c, d, 0, Vector2.one, true);
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
                pieces.Building(center, half, foundation - width * .35f, false);
            }
        }
    }

    private sealed class SurfaceMesh
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int>[] surfaces = { new List<int>(), new List<int>(), new List<int>() };
        readonly List<Vector3> collisionVertices = new List<Vector3>();
        readonly List<int> collisionFaces = new List<int>();

        public void StreetLevel(float halfSize, float height)
        {
            var repeat = Vector2.one * (halfSize / 20f);
            Quad(new Vector3(-halfSize, height, -halfSize), new Vector3(-halfSize, height, halfSize),
                new Vector3(halfSize, height, halfSize), new Vector3(halfSize, height, -halfSize), 0, repeat, false);
        }

        public void Building(Vector3 center, Vector2 half, float bottom, bool playable)
        {
            var a = center + new Vector3(-half.x, 0, -half.y);
            var b = center + new Vector3(-half.x, 0, half.y);
            var c = center + new Vector3(half.x, 0, half.y);
            var d = center + new Vector3(half.x, 0, -half.y);
            Quad(a, b, c, d, 0, new Vector2(2, 2), playable);
            Wall(a, b, bottom);
            Wall(b, c, bottom);
            Wall(c, d, bottom);
            Wall(d, a, bottom);
        }

        private void Wall(Vector3 a, Vector3 b, float bottom)
        {
            var baseA = new Vector3(a.x, bottom, a.z);
            var baseB = new Vector3(b.x, bottom, b.z);
            var lip = Vector3.up * .65f;
            var span = new Vector2(Vector3.Distance(a, b) / 15f, (a.y - bottom) / 14f);
            Quad(baseB, b - lip, a - lip, baseA, 1, span, false);
            Fascia(a, b);
        }

        public void Fascia(Vector3 a, Vector3 b) =>
            Quad(b - Vector3.up * .65f, b, a, a - Vector3.up * .65f, 2, Vector2.one, false);

        public void Guard(Vector3 a, Vector3 b)
        {
            var rise = Vector3.up * 1.1f;
            var offset = Vector3.Cross(Vector3.up, b - a).normalized * .11f;
            var nearA = a + offset;
            var nearB = b + offset;
            var farA = a - offset;
            var farB = b - offset;
            var tile = new Vector2(Vector3.Distance(a, b) / 10f, .18f);
            Quad(nearA, nearA + rise, nearB + rise, nearB, 2, tile, true);
            Quad(farB, farB + rise, farA + rise, farA, 2, tile, true);
            // Caps are visual. A vertical ground query must not place a pickup on a parapet.
            Quad(nearA + rise, farA + rise, farB + rise, nearB + rise, 0, Vector2.one, false);
            Quad(nearA, farA, farA + rise, nearA + rise, 2, Vector2.one, false);
            Quad(farB, nearB, nearB + rise, farB + rise, 2, Vector2.one, false);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material, Vector2 scale, bool solid)
        {
            AddQuad(vertices, surfaces[material], a, b, c, d);
            uv.Add(Vector2.zero);
            uv.Add(new Vector2(0, scale.y));
            uv.Add(scale);
            uv.Add(new Vector2(scale.x, 0));
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

        public Mesh Visual()
        {
            var mesh = new Mesh { name = "Rooftop architecture" };
            if (vertices.Count > ushort.MaxValue) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.subMeshCount = surfaces.Length;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
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
