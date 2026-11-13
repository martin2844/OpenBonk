using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GrassBatchedMeshPortCheck
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Require(bool condition, string message) { if (!condition) throw new Exception("GrassBatchedMeshPortCheck: " + message); }
    public static void Run()
    {
        var obj = new GameObject("controlled grass batching");
        var manager = obj.AddComponent<GrassChunkManager>();
        var source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Mesh/GrassQuads.asset");
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/GrassBatched.shader");
        Require(source != null && source.isReadable && shader != null, "real mesh readability and shader");
        var material = new Material(shader); material.SetFloat("_WindStrength", .75f);
        manager.grassMesh = source; manager.grassMaterial = material;
        var field = typeof(GrassChunkManager).GetField("fallbackMatrices", Private);
        var batchField = typeof(GrassChunkManager).GetField("fallbackMeshes", Private);
        var rebuild = typeof(GrassChunkManager).GetMethod("RebuildFallbackMeshes", Private);
        try
        {
            var sourceVertices = source.vertices; var sourceUv = source.uv; var sourceTriangles = source.GetTriangles(0);
            int perBatch = 65535 / source.vertexCount;
            var matrices = new Matrix4x4[perBatch + 1];
            for (int i = 0; i < matrices.Length; i++) matrices[i] = Matrix4x4.TRS(new Vector3(i * 3, 12, -6), Quaternion.Euler(0, i * 17f, 0), Vector3.one);
            field.SetValue(manager, matrices); rebuild.Invoke(manager, null);
            var meshes = (List<Mesh>)batchField.GetValue(manager);
            Require(meshes.Count == 2, "16-bit split");
            Require(meshes[0].vertexCount == perBatch * source.vertexCount && meshes[1].vertexCount == source.vertexCount, "no lost instances");
            Require(meshes[0].indexFormat == UnityEngine.Rendering.IndexFormat.UInt16 && meshes[1].indexFormat == UnityEngine.Rendering.IndexFormat.UInt16, "16-bit indices");
            var vertices = meshes[0].vertices; var uv = meshes[0].uv; var height = meshes[0].uv2; var triangles = meshes[0].triangles;
            foreach (int index in new[] { 0, 1, perBatch - 1 })
            {
                int first = index * source.vertexCount;
                for (int v = 0; v < source.vertexCount; v++)
                {
                    Require(Vector3.Distance(vertices[first + v], matrices[index].MultiplyPoint3x4(sourceVertices[v])) < .01f, "transformed position");
                    Require(uv[first + v] == sourceUv[v] && Mathf.Approximately(height[first + v].x, sourceVertices[v].y), "UV and local height");
                }
                for (int t = 0; t < sourceTriangles.Length; t++) Require(triangles[index * sourceTriangles.Length + t] == first + sourceTriangles[t], "triangle index");
            }
            Require(meshes[0].bounds.Contains(vertices[0]) && meshes[0].bounds.size.y > source.bounds.size.y, "expanded bounds");
            var old0 = meshes[0]; var old1 = meshes[1];
            field.SetValue(manager, Array.Empty<Matrix4x4>()); rebuild.Invoke(manager, null);
            Require(meshes.Count == 0 && old0 == null && old1 == null, "old batches destroyed on empty rebuild");
            Debug.Log("GrassBatchedMeshPortCheck passed: original mesh, transformed positions, UV/height, winding, 16-bit split and cleanup");
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(material); }
    }
}
