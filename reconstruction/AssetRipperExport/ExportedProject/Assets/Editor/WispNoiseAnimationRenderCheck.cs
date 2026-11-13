using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class WispNoiseAnimationRenderCheck
{
    const string Pending = "WispNoiseAnimationRenderCheck.pending";
    static WispNoiseAnimationRenderCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void RunPlay() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) {
            SessionState.SetBool(Pending, false);
            new GameObject("Wisp noise test").AddComponent<WispNoiseAnimationRunner>();
        }
    }
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void RunAll() { Run(); }
    public static void Run()
    {
        if (!EditorApplication.isPlaying) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_wisp.mat");
        var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/MonoBehaviour/Wisp_0.asset");
        Require(original != null && enemy != null && original.shader.isSupported, "original material or prefab unavailable");
        Require(enemy.material == original, "Wisp enemy data does not reference material");
        Require(original.IsKeywordEnabled("_MK_VERTEX_ANIMATION_NOISE"), "expected material keywords missing");
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
            Debug.Log($"WispNoiseAnimationRenderCheck PASS: Wisp material on synthetic sphere; changed={changed}, resetDifference={residual}");
        } finally {
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(materialCopy); UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(lightObject); UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}

public class WispNoiseAnimationRunner : MonoBehaviour
{
    System.Collections.IEnumerator Start() {
        yield return new WaitForSecondsRealtime(1f);
        try { WispNoiseAnimationRenderCheck.Run(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
