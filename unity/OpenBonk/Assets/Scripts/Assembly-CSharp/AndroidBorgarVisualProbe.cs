#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Opt-in only. Renders the original pooled prefab's mesh and material in an isolated
// camera, without spawning an interactable pickup or changing the player's inventory.
internal sealed class AndroidBorgarVisualProbe : MonoBehaviour
{
    const int ProbeLayer = 30;
    static bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (installed) return;
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_borgar_visual_probe", false)) return;
        installed = true;
        var go = new GameObject("Borgar visual probe");
        DontDestroyOnLoad(go);
        go.AddComponent<AndroidBorgarVisualProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (PoolManager.Instance == null || PoolManager.Instance.borgor == null)) yield return null;
        if (PoolManager.Instance == null || PoolManager.Instance.borgor == null) { Debug.LogError("Borgar visual probe: original prefab unavailable"); yield break; }
        GameObject source = PoolManager.Instance.borgor;
        var sourceRenderer = source.GetComponentInChildren<MeshRenderer>(true);
        var sourceFilter = sourceRenderer != null ? sourceRenderer.GetComponent<MeshFilter>() : null;
        if (sourceRenderer == null || sourceFilter == null || sourceFilter.sharedMesh == null || sourceRenderer.sharedMaterial == null)
        { Debug.LogError("Borgar visual probe: original mesh/material unavailable"); yield break; }
        var root = new GameObject("Borgar isolated visual"); root.transform.position = new Vector3(0f, 10000f, 0f);
        var visual = new GameObject("Original Borgar mesh and material"); visual.layer = ProbeLayer; visual.transform.SetParent(root.transform, false);
        visual.transform.localScale = sourceRenderer.transform.localScale;
        visual.AddComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
        var visualRenderer = visual.AddComponent<MeshRenderer>(); visualRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        var cameraObj = new GameObject("Borgar visual camera"); cameraObj.transform.SetParent(root.transform, false); cameraObj.transform.localPosition = new Vector3(0f,0f,-4f);
        var camera = cameraObj.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.1f;
        camera.nearClipPlane = .1f; camera.farClipPlane = 20f; camera.depth = 1000f; camera.cullingMask = 1 << ProbeLayer;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.05f,.2f,.95f,.35f);
        var lightObj = new GameObject("Borgar visual light"); lightObj.transform.SetParent(root.transform, false);
        var light = lightObj.AddComponent<Light>(); light.type = LightType.Directional; light.color = Color.white; light.intensity = 1f;
        light.cullingMask = 1 << ProbeLayer; light.renderMode = LightRenderMode.ForcePixel;
        bool oldFog = RenderSettings.fog; var oldFogColor = RenderSettings.fogColor; var oldFogMode = RenderSettings.fogMode;
        float oldFogStart = RenderSettings.fogStartDistance, oldFogEnd = RenderSettings.fogEndDistance, oldFogDensity = RenderSettings.fogDensity; RenderSettings.fog = false;
        bool fogLinear = Shader.IsKeywordEnabled("FOG_LINEAR"), fogExp = Shader.IsKeywordEnabled("FOG_EXP"), fogExp2 = Shader.IsKeywordEnabled("FOG_EXP2");
        Shader.DisableKeyword("FOG_LINEAR"); Shader.DisableKeyword("FOG_EXP"); Shader.DisableKeyword("FOG_EXP2");
        Debug.Log($"Borgar visual probe: ready source={source.name}, mesh={sourceFilter.sharedMesh.name}, material={sourceRenderer.sharedMaterial.name}, shader={sourceRenderer.sharedMaterial.shader.name}, layer={ProbeLayer}, collider={visual.GetComponent<Collider>() != null}, captureSeconds=12");
        yield return null;
        // Read only this dedicated camera, so unrelated scene UI cannot affect the sample.
        var target = new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(128,128,TextureFormat.RGBA32,false);
        camera.targetTexture = target;
        camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0,0,128,128),0,0); pixels.Apply();
        // Derive sample locations from the actual mesh bounds. This keeps the
        // transparent sample inside the quad even if the device aspect changes.
        Bounds bounds = sourceFilter.sharedMesh.bounds;
        Vector3 cornerWorld = visual.transform.TransformPoint(new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, .12f), Mathf.Lerp(bounds.min.y, bounds.max.y, .12f), bounds.center.z));
        Vector3 centerWorld = visual.transform.TransformPoint(bounds.center);
        Vector3 cornerViewport = camera.WorldToViewportPoint(cornerWorld), centerViewport = camera.WorldToViewportPoint(centerWorld);
        bool inside = cornerViewport.x > 0f && cornerViewport.x < 1f && cornerViewport.y > 0f && cornerViewport.y < 1f;
        Color corner = pixels.GetPixel(Mathf.Clamp((int)(cornerViewport.x * 128),0,127), Mathf.Clamp((int)(cornerViewport.y * 128),0,127));
        Color center = pixels.GetPixel(Mathf.Clamp((int)(centerViewport.x * 128),0,127), Mathf.Clamp((int)(centerViewport.y * 128),0,127));
        bool transparent = inside && corner.b > .65f && corner.b > corner.r * 2f;
        bool visible = center.r > .12f && center.g > .08f && center.b < .65f;
        bool alphaPreserved = Mathf.Abs(center.a - corner.a) < .02f && Mathf.Abs(corner.a - .35f) < .02f;
        Debug.Log($"Borgar visual probe: pixelCheck transparent={transparent}, centerVisible={visible}, alphaPreserved={alphaPreserved}, cornerViewport={cornerViewport}, corner={corner}, center={center}");
        camera.orthographic = false; camera.fieldOfView = 31f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = Color.green;
        RenderSettings.fogDensity = 4f; Shader.EnableKeyword("FOG_EXP2");
        camera.Render(); pixels.ReadPixels(new Rect(0,0,128,128),0,0); pixels.Apply();
        var fogged = pixels.GetPixel(64,64);
        Debug.Log($"Borgar fog probe {(fogged.g > .8f && fogged.r < .2f ? "PASS" : "FAIL")}: pixel={fogged}, shaderLevel={SystemInfo.graphicsShaderLevel}");
        RenderTexture.active = previous; camera.targetTexture = null;
        if (fogLinear) Shader.EnableKeyword("FOG_LINEAR"); else Shader.DisableKeyword("FOG_LINEAR");
        if (fogExp) Shader.EnableKeyword("FOG_EXP"); else Shader.DisableKeyword("FOG_EXP");
        if (fogExp2) Shader.EnableKeyword("FOG_EXP2"); else Shader.DisableKeyword("FOG_EXP2");
        RenderSettings.fog = oldFog; RenderSettings.fogColor = oldFogColor; RenderSettings.fogMode = oldFogMode;
        RenderSettings.fogStartDistance = oldFogStart; RenderSettings.fogEndDistance = oldFogEnd; RenderSettings.fogDensity = oldFogDensity;
        Destroy(pixels); target.Release(); Destroy(target);
        yield return new WaitForSecondsRealtime(12f);
        Destroy(root);
        Debug.Log("Borgar visual probe: completed");
    }
}
#endif
