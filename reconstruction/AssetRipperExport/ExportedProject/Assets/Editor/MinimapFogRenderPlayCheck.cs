using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MinimapFogRenderPlayCheck
{
    const string Pending = "MinimapFogRenderPlayCheck.pending";
    static MinimapFogRenderPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("minimap fog render check").AddComponent<MinimapFogRenderRunner>().Begin();
        }
    }
}

public class MinimapFogRenderRunner : MonoBehaviour
{
    public void Begin() { StartCoroutine(Run()); }
    IEnumerator Run()
    {
        var camera = new GameObject("camera").AddComponent<Camera>();
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.targetTexture = new RenderTexture(64, 64, 24);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.position = Vector3.forward * 4f;
        quad.transform.localScale = Vector3.one * 4f;
        var control = new Material(Shader.Find("Unlit/Texture"));
        var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, Color.green); texture.Apply(); control.mainTexture = texture;
        var minimap = new Material(Shader.Find("Custom/MinimapHeightColorGradient"));
        minimap.SetColor("_Color", Color.green); minimap.SetColor("_MaxColor", Color.green);
        minimap.SetFloat("_MinDistance", -1f); minimap.SetFloat("_MaxDistance", 1f);
        var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        yield return null;
        try
        {
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Color.red;
            RenderSettings.fogStartDistance = 0f; RenderSettings.fogEndDistance = 1f;
            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = control; camera.Render(); RenderTexture.active = camera.targetTexture;
            pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); var reference = pixel.GetPixel(0, 0);
            renderer.sharedMaterial = minimap; camera.Render();
            pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); var actual = pixel.GetPixel(0, 0);
            Debug.Log($"MinimapFogRenderPlayCheck control={reference} minimap={actual}");
            if (reference.r < .5f || reference.g > .3f) throw new Exception("fog control did not render red");
            if (actual.r < .5f || actual.g > .3f) throw new Exception("minimap did not render fog");
            QualitySettings.pixelLightCount = 4;
            var sun = new GameObject("directional light").AddComponent<Light>(); sun.type = LightType.Directional;
            var point = new GameObject("point light").AddComponent<Light>(); point.type = LightType.Point;
            point.transform.position = new Vector3(0, 0, 2); point.range = 10; point.renderMode = LightRenderMode.ForcePixel;
            camera.Render(); pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); var fogWithLight = pixel.GetPixel(0, 0);
            Debug.Log($"MinimapFogRenderPlayCheck fogWithLight={fogWithLight}");
            if (fogWithLight.r < .5f || fogWithLight.g > .3f) throw new Exception("additional light was not attenuated by fog");
            point.enabled = false; sun.enabled = false;
            RenderSettings.fog = false; camera.Render();
            pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); var clear = pixel.GetPixel(0, 0);
            if (clear.g < .5f || clear.r > .3f) throw new Exception($"minimap without fog did not render green: {clear}");
            Debug.Log($"MinimapFogRenderPlayCheck passed: fog={actual} clear={clear}");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
