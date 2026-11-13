using System;
using System.Reflection;
using Assets.Scripts.MapGeneration.ProceduralTiles;
using UnityEditor;
using UnityEngine;

public static class MeshColliderWeldPortCheck
{
    static readonly MethodInfo Weld = typeof(MeshColliderCombiner).GetMethod("AutoWeld", BindingFlags.Static | BindingFlags.NonPublic);
    static Mesh Make(Vector3[] vertices)
    {
        var mesh = new Mesh(); mesh.vertices = vertices;
        var triangles = new int[(vertices.Length / 3) * 3];
        for (int i = 0; i < triangles.Length; i++) triangles[i] = i;
        mesh.triangles = triangles; return mesh;
    }
    static void Reference(Mesh mesh, float threshold)
    {
        Vector3[] vertices = mesh.vertices, unique = new Vector3[vertices.Length];
        int[] remap = new int[vertices.Length]; int count = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            int j = 0; for (; j < count; j++) if ((unique[j] - vertices[i]).sqrMagnitude < threshold) break;
            if (j == count) unique[count++] = vertices[i]; remap[i] = j;
        }
        int[] triangles = mesh.triangles; for (int i = 0; i < triangles.Length; i++) triangles[i] = remap[triangles[i]];
        var result = new Vector3[count]; Array.Copy(unique, result, count);
        mesh.Clear(); mesh.vertices = result; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.Optimize();
    }
    static void Check(Vector3[] vertices, float threshold)
    {
        Mesh expected = Make(vertices), actual = Make(vertices);
        try
        {
            Reference(expected, threshold); Weld.Invoke(null, new object[] { actual, threshold });
            var ev = expected.vertices; var av = actual.vertices; var et = expected.triangles; var at = actual.triangles;
            if (ev.Length != av.Length || et.Length != at.Length) throw new Exception("weld lengths differ");
            for (int i = 0; i < ev.Length; i++) if (ev[i] != av[i]) throw new Exception("vertex differs at " + i);
            for (int i = 0; i < et.Length; i++) if (et[i] != at[i]) throw new Exception("triangle differs at " + i);
        }
        finally { UnityEngine.Object.DestroyImmediate(expected); UnityEngine.Object.DestroyImmediate(actual); }
    }
    public static void Run()
    {
        var random = new System.Random(4129);
        for (int run = 0; run < 200; run++)
        {
            var vertices = new Vector3[300];
            for (int i = 0; i < vertices.Length; i++)
            {
                if (i > 0 && i % 4 == 0) vertices[i] = vertices[random.Next(i)] + new Vector3((float)(random.NextDouble() - .5), 0, 0);
                else vertices[i] = new Vector3((float)(random.NextDouble() * 30 - 15), (float)(random.NextDouble() * 4 - 2), (float)(random.NextDouble() * 30 - 15));
            }
            Check(vertices, .25f);
        }
        Check(new[] { Vector3.zero, new Vector3(.5f, 0, 0), new Vector3(.49999997f, 0, 0), new Vector3(-.5f, 0, 0), new Vector3(-.000001f, 0, 0), new Vector3(1.5f, 0, 0) }, .25f);
        Check(new[] { Vector3.zero, Vector3.right, Vector3.one }, 1f);
        var realVertices = new System.Collections.Generic.List<Vector3>();
        foreach (string name in new[] { "TileGround", "TileSlope", "TileWall" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/" + name + ".prefab");
            var collider = prefab != null ? prefab.GetComponent<MeshCollider>() : null;
            if (collider == null || collider.sharedMesh == null) throw new Exception("missing collider for " + name);
            foreach (Vector3 vertex in collider.sharedMesh.vertices) realVertices.Add(vertex);
        }
        while (realVertices.Count % 3 != 0) realVertices.Add(realVertices[0]);
        Check(realVertices.ToArray(), .25f);
        var large = new Vector3[6000]; for (int i = 0; i < large.Length; i++) large[i] = new Vector3(i % 100 * 2f, 0, i / 100 * 2f);
        Mesh slow = Make(large), fast = Make(large);
        var timer = System.Diagnostics.Stopwatch.StartNew(); Reference(slow, .25f); timer.Stop(); long oldMs = timer.ElapsedMilliseconds;
        timer.Restart(); Weld.Invoke(null, new object[] { fast, .25f }); timer.Stop(); long newMs = timer.ElapsedMilliseconds;
        if (slow.vertexCount != fast.vertexCount || slow.triangles.Length != fast.triangles.Length) throw new Exception("large mesh differs");
        UnityEngine.Object.DestroyImmediate(slow); UnityEngine.Object.DestroyImmediate(fast);
        Debug.Log($"MeshColliderWeldPortCheck passed: 200 seeded meshes, boundaries, fallback, {realVertices.Count} real tile vertices; 6000 distinct vertices original={oldMs}ms indexed={newMs}ms");
    }
}
