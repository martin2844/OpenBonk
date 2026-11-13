using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SkyboxExtendedShaderCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception("SkyboxExtendedShaderCheck: " + message); }
    static Color Render(Camera camera, RenderTexture target, Texture2D pixel)
    {
        camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
        pixel.ReadPixels(new Rect(32, 32, 1, 1), 0, 0); pixel.Apply(); RenderTexture.active = previous;
        return pixel.GetPixel(0, 0);
    }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Skybox_Forest1.mat");
        Check(source != null && source.shader.isSupported, "Forest material and shader");
        Check(source.IsKeywordEnabled("_ENABLEFOG_ON") && source.IsKeywordEnabled("_ENABLEROTATION_ON"), "Forest variant keywords");
        var material = new Material(source);
        var cube = new Cubemap(1, TextureFormat.RGBAFloat, false);
        var sample = new Color(.02f, .04f, .08f, 1f);
        foreach (CubemapFace face in new[] { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ }) cube.SetPixel(face, 0, 0, sample);
        cube.Apply();
        material.SetTexture("_Tex", cube); material.SetVector("_Tex_HDR", new Vector4(1, 1, 0, 0));
        material.SetColor("_TintColor", Color.white); material.SetFloat("_Exposure", 1f);
        material.DisableKeyword("_ENABLEFOG_ON"); material.DisableKeyword("_ENABLEROTATION_ON");
        var oldSky = RenderSettings.skybox; var oldFog = RenderSettings.fogColor;
        var go = new GameObject("skybox test camera"); var camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox; camera.allowHDR = true; camera.fieldOfView = 60f;
        var target = new RenderTexture(65, 65, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear); camera.targetTexture = target;
        var pixel = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
        try
        {
            RenderSettings.skybox = material; RenderSettings.fogColor = Color.black;
            var clear = Render(camera, target, pixel);
            Check(Mathf.Abs(clear.r - sample.r * 4.5947938f) < .015f && Mathf.Abs(clear.b - sample.b * 4.5947938f) < .015f, "original decoded brightness: " + clear);
            material.EnableKeyword("_ENABLEFOG_ON"); material.SetFloat("_FogIntensity", 1f); material.SetFloat("_FogHeight", 1f);
            material.SetFloat("_FogSmoothness", 0f); material.SetFloat("_FogFill", .5f); material.SetFloat("_FogPosition", -.5f);
            var fog = Render(camera, target, pixel);
            Check(Mathf.Abs(fog.r / clear.r - .25f) < .04f && Mathf.Abs(fog.b / clear.b - .25f) < .04f, "original fog blend: " + fog + " / " + clear);
            foreach (var name in new[] { "Forest1", "Forest2", "Forest3", "Desert1", "Desert2", "Desert3", "Graveyard", "Hell" })
            {
                var actual = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Skybox_" + name + ".mat");
                Check(actual != null && actual.shader == source.shader, name + " material and shader");
                Check(name == "Hell" ? actual.GetTexture("_Tex") == null : actual.GetTexture("_Tex") != null, name + " original cubemap binding");
                Check(actual.IsKeywordEnabled("_ENABLEFOG_ON") && actual.IsKeywordEnabled("_ENABLEROTATION_ON"), name + " shader features");
                RenderSettings.skybox = actual;
                var value = Render(camera, target, pixel);
                Check(!float.IsNaN(value.r) && !float.IsNaN(value.g) && !float.IsNaN(value.b), name + " finite rendered color");
                Check(!(value.r > .9f && value.g < .01f && value.b > .9f), name + " error shader color");
                Debug.Log($"SkyboxExtendedShaderCheck: {name} color={value}");
            }
            Debug.Log($"SkyboxExtendedShaderCheck passed: clear={clear}, fog={fog}, Forest keywords and controlled original-formula values");
        }
        finally
        {
            RenderSettings.skybox = oldSky; RenderSettings.fogColor = oldFog; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixel);
            UnityEngine.Object.DestroyImmediate(cube); UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
