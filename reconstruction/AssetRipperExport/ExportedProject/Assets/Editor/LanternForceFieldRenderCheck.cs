using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LanternForceFieldRenderCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("LanternForceFieldRenderCheck: " + message); }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/LanternExplosion.prefab");
        var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Forcefield_Lantern.mat");
        Require(prefab != null && original != null && original.shader.isSupported, "prefab/material/shader unavailable");
        var sourceRenderer = prefab.GetComponent<Renderer>(); var sourceFilter = prefab.GetComponent<MeshFilter>();
        Require(sourceRenderer != null && sourceFilter != null && sourceFilter.sharedMesh != null && sourceRenderer.sharedMaterial == original, "actual mesh/material binding");
        var root = new GameObject("lantern render"); root.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
        var renderer = root.AddComponent<MeshRenderer>(); var material = new Material(original); renderer.sharedMaterial = material;
        var cameraObject = new GameObject("camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false; camera.transform.position = new Vector3(0,0,-5); camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true; camera.orthographicSize = 1.5f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.05f,.12f,.4f,1);
        camera.depthTextureMode = DepthTextureMode.Depth;
        var target = new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var image = new Texture2D(128,128,TextureFormat.RGBA32,false); var tint = material.GetColor("_MainColor");
        Color32[] Render(float alpha) {
            var color = tint; color.a = alpha; material.SetColor("_MainColor",color); camera.Render();
            var old = RenderTexture.active; RenderTexture.active = target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); RenderTexture.active = old;
            return image.GetPixels32();
        }
        bool oldFog = RenderSettings.fog;
        try {
            RenderSettings.fog = false;
            var empty = Render(0f); var actual = Render(tint.a); File.WriteAllBytes("/home/your-user/megabonk/build/android/lantern-forcefield-unity.png", image.EncodeToPNG());
            var opaque = Render(1f); int changed = 0, differsFromOpaque = 0;
            for(int i=0; i<actual.Length; i++) {
                int diff = Math.Abs(actual[i].r-empty[i].r)+Math.Abs(actual[i].g-empty[i].g)+Math.Abs(actual[i].b-empty[i].b);
                int opaqueDiff = Math.Abs(actual[i].r-opaque[i].r)+Math.Abs(actual[i].g-opaque[i].g)+Math.Abs(actual[i].b-opaque[i].b);
                if(diff > 9) changed++; if(diff > 9 && opaqueDiff > 9) differsFromOpaque++;
            }
            Require(tint.a > 0f && tint.a < 1f && changed > 100 && differsFromOpaque > 100, $"actual alpha must draw and blend: alpha={tint.a}, changed={changed}, mixed={differsFromOpaque}");
            Debug.Log($"LanternForceFieldRenderCheck passed: actual mesh/material renders blended alpha={tint.a}, changed={changed}, mixed={differsFromOpaque}");
        } finally {
            RenderSettings.fog = oldFog; camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
