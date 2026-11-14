using System;
using UnityEditor;
using UnityEngine;

public static class AndroidWorldEdgeOutlineCheck
{
    public static void Run()
    {
#if !UNITY_ANDROID
        throw new Exception("Android build target is required");
#else
        var shader = Shader.Find("Toon/Lit Tri Planar Noise Blend");
        if (shader == null) throw new Exception("Terrain shader unavailable");
        var material = new Material(shader);
        var stage = ScriptableObject.CreateInstance<StageData>();
        var root = new GameObject("Outline check");
        var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            material.SetFloat("_OutlineWidth", 0.02f);
            stage.triplanarMaterial = material;
            var edges = root.AddComponent<MapEdges>();
            edges.edgeTop = top.GetComponent<MeshRenderer>();
            edges.edgeBot = bottom.GetComponent<MeshRenderer>();
            var topBlock = new MaterialPropertyBlock();
            topBlock.SetFloat("_OpenBonkSentinel", 7f);
            edges.edgeTop.SetPropertyBlock(topBlock);
            edges.Set(Vector3.zero, new Vector3(20f, 10f, 20f), stage);
            edges.edgeTop.GetPropertyBlock(topBlock);
            var bottomBlock = new MaterialPropertyBlock();
            edges.edgeBot.GetPropertyBlock(bottomBlock);
            if (!topBlock.HasFloat(Shader.PropertyToID("_OutlineWidth")) || topBlock.GetFloat("_OutlineWidth") != 0f ||
                topBlock.GetFloat("_OpenBonkSentinel") != 7f ||
                !bottomBlock.isEmpty || Math.Abs(material.GetFloat("_OutlineWidth") - 0.02f) > 0.0001f ||
                edges.edgeTop.sharedMaterial != material || edges.edgeBot.sharedMaterial != material)
                throw new Exception("Outline override changed shared material or unrelated properties");
            Debug.Log("AndroidWorldEdgeOutlineCheck passed");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(top);
            UnityEngine.Object.DestroyImmediate(bottom);
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(stage);
            UnityEngine.Object.DestroyImmediate(material);
        }
#endif
    }
}
