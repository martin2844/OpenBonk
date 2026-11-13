using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GeorgeLeafAnimationRenderCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void RunAll() { PlayerSkinPortCheck.Run(); Run(); }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_george_leaf_default.mat");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/George.prefab");
        Require(original != null && prefab != null && original.shader.isSupported, "original material or prefab unavailable");
        bool bound = false;
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            foreach (var material in renderer.sharedMaterials) if (material == original) bound = true;
        Require(bound, "George prefab does not reference material");
        Require(original.IsKeywordEnabled("_MK_VERTEX_ANIMATION_SINE") && original.IsKeywordEnabled("_MK_VERTEX_ANIMATION_STUTTER"), "expected material keywords missing");
        var materialCopy = new Material(original);
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.GetComponent<Renderer>().sharedMaterial = materialCopy;
        var lightObject = new GameObject("light"); var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1f; lightObject.transform.rotation = Quaternion.Euler(30, -20, 0);
        var cameraObject = new GameObject("camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false; camera.transform.position = new Vector3(0,0,-5); camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true; camera.orthographicSize = 1.2f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue;
        var target = new RenderTexture(256,256,24,RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var image = new Texture2D(256,256,TextureFormat.RGBA32,false);
        Color32[] Render(float intensity) {
            materialCopy.SetFloat("_VertexAnimationIntensity", intensity);
            camera.Render(); var old = RenderTexture.active; RenderTexture.active = target;
            image.ReadPixels(new Rect(0,0,256,256),0,0); image.Apply(); RenderTexture.active = old;
            return image.GetPixels32();
        }
        try {
            var still = Render(0f); var moving = Render(.25f); var reset = Render(0f);
            int changed = 0, residual = 0;
            for (int i = 0; i < still.Length; i++) {
                int difference = Math.Abs(still[i].r-moving[i].r) + Math.Abs(still[i].g-moving[i].g) + Math.Abs(still[i].b-moving[i].b);
                int after = Math.Abs(still[i].r-reset[i].r) + Math.Abs(still[i].g-reset[i].g) + Math.Abs(still[i].b-reset[i].b);
                if (difference > 9) changed++; if (after > 9) residual++;
            }
            Require(changed > 100 && residual == 0, $"vertex animation render mismatch: changed={changed} resetDifference={residual}");
            Debug.Log($"GeorgeLeafAnimationRenderCheck PASS: actual material keywords on synthetic sphere; changed={changed}, resetDifference={residual}");
        } finally {
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(materialCopy); UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(lightObject); UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
