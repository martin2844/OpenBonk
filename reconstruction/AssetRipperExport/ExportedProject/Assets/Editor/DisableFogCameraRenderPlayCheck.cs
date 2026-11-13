using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class DisableFogCameraRenderPlayCheck
{
    const string Pending = "DisableFogCameraRenderPlayCheck.pending";
    static DisableFogCameraRenderPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            SessionState.SetBool(Pending, false);
            new GameObject("fog render test runner").AddComponent<DisableFogCameraRenderRunner>().Begin();
        }
    }
}

public class DisableFogCameraRenderRunner : MonoBehaviour
{
    public void Begin() { StartCoroutine(Run()); }
    IEnumerator Run()
    {
        var cameraObject = new GameObject("test camera");
        var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
        camera.targetTexture = new RenderTexture(64, 64, 24);
        var disableFog = cameraObject.AddComponent<DisableFogForCamera>();
        var cube = GameObject.CreatePrimitive(PrimitiveType.Quad); cube.transform.position = Vector3.forward * 4f; cube.transform.localScale = Vector3.one * 4f;
        var material = new Material(Shader.Find("Unlit/Texture"));
        var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, Color.green); texture.Apply(); material.mainTexture = texture;
        cube.GetComponent<Renderer>().material = material;
        var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        yield return null;
        try
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = Color.red; RenderSettings.fogStartDistance = 0f; RenderSettings.fogEndDistance = 1f;
            camera.Render();
            RenderTexture.active = camera.targetTexture; pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); var withoutFog = pixel.GetPixel(0, 0);
            if (!RenderSettings.fog) throw new Exception("fog state was not restored after first render");
            disableFog.enabled = false;
            camera.Render();
            pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); var withFog = pixel.GetPixel(0, 0); RenderTexture.active = null;
            Debug.Log($"DisableFogCameraRenderPlayCheck pixels: disabled={withoutFog} control={withFog} restored={RenderSettings.fog}");
            if (withoutFog.g < .5f || withoutFog.r > .3f || withFog.r < .5f || withFog.g > .3f || !RenderSettings.fog) throw new Exception("rendered fog colors or restoration differ");
            Debug.Log("DisableFogCameraRenderPlayCheck passed: real camera callbacks disable fog for one render and preserve it for the next");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
