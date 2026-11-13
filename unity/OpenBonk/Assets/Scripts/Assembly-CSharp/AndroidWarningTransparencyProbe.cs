#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Actors.Enemies;
using UnityEngine;

// Renders the original enemy warning mesh/material against a known background.
internal sealed class AndroidWarningTransparencyProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_warning_transparency_probe", false)) return;
        var root = new GameObject("warning transparency probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidWarningTransparencyProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && DataManager.Instance == null) yield return null;
        var enemy = DataManager.Instance != null ? DataManager.Instance.GetEnemyData(EEnemy.MinibossPig) : null;
        GameObject prefab = null;
        if (enemy != null && enemy.specialAttacks != null)
            foreach (var attack in enemy.specialAttacks)
                if (attack != null && attack.attackName == "SphereSlam") { prefab = attack.attackPrefab; break; }
        if (prefab == null) { Debug.LogError("Warning transparency probe FAIL: attack prefab unavailable"); yield break; }
        MeshFilter source = null;
        foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.name == "WarningFlat") { source = filter; break; }
        }
        if (source == null || source.sharedMesh == null) { Debug.LogError("Warning transparency probe FAIL: warning mesh/material missing"); yield break; }
        const int layer = 30;
        var root = new GameObject("isolated warning render"); root.transform.position = new Vector3(0, 10000, 0);
        var visual = new GameObject("original warning mesh"); visual.layer = layer; visual.transform.SetParent(root.transform, false);
        visual.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
        var rendererCopy = visual.AddComponent<MeshRenderer>(); rendererCopy.sharedMaterial = source.GetComponent<MeshRenderer>().sharedMaterial;
        var cameraObject = new GameObject("warning camera"); cameraObject.transform.SetParent(root.transform, false); cameraObject.transform.localPosition = new Vector3(0, 4, 0); cameraObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1; camera.cullingMask = 1 << layer;
        camera.nearClipPlane = .1f; camera.farClipPlane = 20f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32); var pixels = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        bool oldFog = RenderSettings.fog; RenderSettings.fog = false;
        yield return null;
        camera.targetTexture = target; camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); pixels.Apply();
        Color center = pixels.GetPixel(32, 32), corner = pixels.GetPixel(0, 0);
        bool blended = center.r > .02f && center.r < .4f && center.b > .55f;
        Debug.Log($"Warning transparency probe {(blended ? "PASS" : "FAIL")}: mesh={source.sharedMesh.name}, material={rendererCopy.sharedMaterial.name}, shader={rendererCopy.sharedMaterial.shader.name}, center={center}, corner={corner}");
        RenderTexture.active = previous; camera.targetTexture = null; target.Release(); Destroy(target); Destroy(pixels); RenderSettings.fog = oldFog; Destroy(root);
    }
}
#endif
