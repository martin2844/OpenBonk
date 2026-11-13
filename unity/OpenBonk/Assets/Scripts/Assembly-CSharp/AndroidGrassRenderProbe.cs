#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Opt-in pixel probe on a separate camera/target; never enabled during normal play.
internal sealed class AndroidGrassRenderProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_grass_render_probe", false)) return;
        var go = new GameObject("Grass render pixel probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidGrassRenderProbe>();
    }

    private IEnumerator Start()
    {
        GrassChunkManager grass = null;
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline)
        {
            foreach (var candidate in FindObjectsByType<GrassChunkManager>(FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled && candidate.grassMesh != null && candidate.grassMaterial != null && PlayerCamera.Instance != null) { grass = candidate; break; }
            if (grass != null) break;
            yield return null;
        }
        if (grass == null) { Debug.LogError("Grass pixel probe: manager timeout"); yield break; }
        var shader = Resources.Load<Shader>("GrassFallback");
        if (shader == null) { Debug.LogError("Grass pixel probe: missing shader"); yield break; }
        var material = new Material(grass.grassMaterial) { shader = shader, enableInstancing = true };
        material.SetFloat("_GrassDebug", 1); material.SetVector("_PlayerPos", new Vector4(0, 1000, 0, 0)); material.SetFloat("_RenderDistance", 100);
        var freshMaterial = new Material(shader) { enableInstancing = true };
        freshMaterial.SetFloat("_GrassDebug", 5); freshMaterial.SetVector("_PlayerPos", new Vector4(0, 1000, 0, 0)); freshMaterial.SetFloat("_RenderDistance", 100);
        var cameraObject = new GameObject("Grass pixel probe camera");
        cameraObject.transform.position = new Vector3(0, 1000, 0);
        var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        camera.cullingMask = 1 << 31; camera.nearClipPlane = .1f; camera.farClipPlane = 30; camera.depth = 100;
        var target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var readback = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        var white = new Texture2D(2, 2); white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); white.Apply();
        var originalTexture = material.mainTexture;
        var matrices = new[] { Matrix4x4.TRS(new Vector3(0, 999, 5), Quaternion.identity, Vector3.one * 2) };
        var control = new GameObject("Grass probe mesh renderer"); control.layer = 31;
        control.transform.SetPositionAndRotation(new Vector3(0, 999, 5), Quaternion.identity); control.transform.localScale = Vector3.one * 2;
        control.AddComponent<MeshFilter>().sharedMesh = grass.grassMesh;
        var controlRenderer = control.AddComponent<MeshRenderer>(); controlRenderer.sharedMaterial = material; controlRenderer.enabled = false;
        var solidShader = Resources.Load<Shader>("GrassProbeSolid");
        var solid = solidShader != null ? new Material(solidShader) : null;
        var instancedShader = Resources.Load<Shader>("GrassProbeInstanced");
        var instancedControl = instancedShader != null ? new Material(instancedShader) { enableInstancing = true } : null;
        var targetShader = Resources.Load<Shader>("GrassProbeTarget3");
        var targetControl = targetShader != null ? new Material(targetShader) : null;
        var fogShader = Resources.Load<Shader>("GrassProbeFog");
        var fogControl = fogShader != null ? new Material(fogShader) : null;
        Debug.Log($"Grass pixel probe materials: fallbackPasses={material.passCount}, fallbackEnabled={material.GetShaderPassEnabled("Always")}, minimalInstancedPasses={(instancedControl != null ? instancedControl.passCount : -1)}");
        bool previousFog = RenderSettings.fog; RenderSettings.fog = false;
        for (int pass = 0; pass < 22; pass++)
        {
            material.mainTexture = pass == 0 ? originalTexture : white;
            material.SetFloat("_GrassDebug", pass >= 10 ? 5 : pass >= 8 ? 4 : pass >= 6 ? 3 : pass >= 4 ? 2 : 1);
            material.enableInstancing = pass != 21;
            material.renderQueue = pass == 12 || pass == 13 ? 2000 : 2600;
            if (instancedControl != null) instancedControl.enableInstancing = pass != 20;
            controlRenderer.sharedMaterial = pass == 3 ? solid : pass == 18 ? targetControl : pass == 19 ? fogControl : pass == 21 ? material : pass >= 16 ? instancedControl : pass >= 14 ? freshMaterial : material;
            controlRenderer.enabled = pass == 2 || pass == 3 || pass == 4 || pass == 6 || pass == 8 || pass == 10 || pass == 12 || pass == 14 || pass == 16 || pass >= 18;
            for (int frame = 0; frame < 3; frame++)
            {
                if (pass < 2 || pass == 5 || pass == 7 || pass == 9 || pass == 11 || pass == 13 || pass == 15 || pass == 17) Graphics.DrawMeshInstanced(grass.grassMesh, 0, pass == 17 ? instancedControl : pass == 15 ? freshMaterial : material, matrices, 1, null, ShadowCastingMode.Off, false, 31, camera);
                yield return new WaitForEndOfFrame();
            }
            var old = RenderTexture.active; RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); readback.Apply(); RenderTexture.active = old;
            int magenta = 0, nonBlack = 0; foreach (Color32 pixel in readback.GetPixels32()) { if (pixel.r > 180 && pixel.b > 180 && pixel.g < 80) magenta++; if (pixel.r > 5 || pixel.g > 5 || pixel.b > 5) nonBlack++; }
            string mode = pass == 0 ? "instanced-original" : pass == 1 ? "instanced-white" : pass == 2 ? "renderer-white" : pass == 3 ? "renderer-solid-control" : pass == 4 ? "renderer-no-clip" : pass == 5 ? "instanced-no-clip" : pass == 6 ? "renderer-no-distance" : pass == 7 ? "instanced-no-distance" : pass == 8 ? "renderer-world-vp" : pass == 9 ? "instanced-world-vp" : pass == 10 ? "renderer-object-clip" : pass == 11 ? "instanced-object-clip" : pass == 12 ? "renderer-queue-2000" : pass == 13 ? "instanced-queue-2000" : pass == 14 ? "renderer-fresh-material" : pass == 15 ? "instanced-fresh-material" : pass == 16 ? "renderer-instanced-control" : pass == 17 ? "instanced-instanced-control" : pass == 18 ? "renderer-target3" : pass == 19 ? "renderer-fog" : pass == 20 ? "renderer-instancing-disabled" : "fallback-instancing-disabled";
            Debug.Log($"Grass pixel probe: mode={mode}, magentaPixels={magenta}, nonBlack={nonBlack}, shaderSupported={shader.isSupported}, solidSupported={(solidShader != null && solidShader.isSupported)}, mesh={grass.grassMesh.name}, instancing={SystemInfo.supportsInstancing}");
        }
        RenderSettings.fog = previousFog; Destroy(control); camera.targetTexture = null; Destroy(cameraObject); target.Release(); Destroy(target); Destroy(readback); Destroy(white); Destroy(material); Destroy(freshMaterial); if (solid != null) Destroy(solid); if (instancedControl != null) Destroy(instancedControl); if (targetControl != null) Destroy(targetControl); if (fogControl != null) Destroy(fogControl);
    }
}
#endif
