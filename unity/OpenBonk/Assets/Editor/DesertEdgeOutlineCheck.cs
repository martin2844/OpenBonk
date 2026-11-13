using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DesertEdgeOutlineCheck
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var first = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_1_TerrainDesertEdge.mat");
        var third = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_3_TerrainDesertEdge.mat");
        var terrain = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_1_TerrainDesert.mat");
        if (!first || !third || !terrain || first.GetFloat("_SuppressOutline") != 1f || third.GetFloat("_SuppressOutline") != 1f || terrain.GetFloat("_SuppressOutline") != 0f) throw new Exception("Unexpected edge or terrain outline configuration");
        var material = new Material(first); material.SetColor("_OutlineColor", Color.red); material.SetFloat("_OutlineWidth", 0f);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.GetComponent<Renderer>().sharedMaterial = material;
        var obj = new GameObject("camera"); var camera = obj.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.transform.position = new Vector3(0, 0, -4);
        var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target; var pixel = new Texture2D(1, 1);
        try
        {
            // Find the quad orientation drawn by the existing front-cull outline.
            material.SetFloat("_SuppressOutline", 0f);
            Color old = Render(camera, target, pixel);
            if (old.r < .8f) { quad.transform.rotation = Quaternion.Euler(0, 180, 0); old = Render(camera, target, pixel); }
            if (old.r < .8f || old.b > .2f) throw new Exception("Could not render outline positive control: " + old);
            material.SetFloat("_SuppressOutline", 1f); Color fixedColor = Render(camera, target, pixel);
            if (fixedColor.b < .8f || fixedColor.r > .2f) throw new Exception("Suppressed outline still drew pixels: " + fixedColor);
            Debug.Log("DesertEdgeOutlineCheck PASS: both real edge materials suppress outline, regular terrain retains it, GPU positive control=" + old + ", suppressed=" + fixedColor);
        }
        finally { camera.targetTexture = null; RenderTexture.active = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixel); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(obj); }
    }
    static Color Render(Camera camera, RenderTexture target, Texture2D pixel)
    {
        camera.Render(); RenderTexture.active = target; pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); return pixel.GetPixel(0, 0);
    }
}
