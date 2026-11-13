#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

// Controlled offscreen render of a real chest effect. Diagnostic only.
internal sealed class AndroidOriginalAtlasVisualProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_original_atlas_visual_probe", false)) return;
        var root = new GameObject("Original atlas visual probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidOriginalAtlasVisualProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || EffectManager.Instance == null || EffectManager.Instance.openChestNormal == null || Camera.main == null)) yield return null;
        if (EffectManager.Instance?.openChestNormal == null || Camera.main == null) { Debug.LogError("Original atlas visual probe INCONCLUSIVE: no scene"); yield break; }
        byte[] raw;
        try { using var zip = File.OpenRead(Path.Combine(Application.persistentDataPath, "Probe", "source.zip")); raw = AndroidOriginalAtlasZip.Extract(zip); }
        catch (Exception e) { Debug.LogError("Original atlas visual probe FAILED extraction " + e); yield break; }
        if (EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = false;
        var camera = Camera.main;
        var chest = Instantiate(EffectManager.Instance.openChestNormal, camera.transform.position + camera.transform.forward * 6f - camera.transform.up, Quaternion.LookRotation(-camera.transform.forward));
        foreach (var transform in chest.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 31;
        foreach (var collider in chest.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        yield return new WaitForSecondsRealtime(3f);
        float oldScale = Time.timeScale; Time.timeScale = 0;
        GameObject cameraObject = null; Texture2D original = null, marker = null; RenderTexture target = null;
        var bindings = new List<(Material material, string property, Texture old)>();
        try
        {
            int particles = chest.GetComponentsInChildren<ParticleSystem>().Sum(p => p.particleCount);
            var renderers = chest.GetComponentsInChildren<ParticleSystemRenderer>().Where(r => r.enabled).ToArray();
            foreach (var mat in renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct())
                foreach (string property in mat.GetTexturePropertyNames())
                {
                    var texture = mat.GetTexture(property);
                    if (texture != null && (texture.name == "atlas01" || texture.name == "atlas01-original-steam")) bindings.Add((mat, property, texture));
                }
            if (bindings.Count == 0 || particles == 0) throw new InvalidDataException("No live atlas particle binding");
            var template = bindings[0].old as Texture2D;
            original = new Texture2D(512, 256, TextureFormat.RGBA32, true); original.LoadRawTextureData(raw); original.Apply(false, false);
            marker = new Texture2D(512, 256, TextureFormat.RGBA32, true); marker.LoadRawTextureData(raw); marker.Apply(false, false);
            if (template == null || template.graphicsFormat != original.graphicsFormat) throw new InvalidDataException("Texture format mismatch");
            foreach (var t in new[] { original, marker }) { t.filterMode = template.filterMode; t.wrapMode = template.wrapMode; t.anisoLevel = template.anisoLevel; t.mipMapBias = template.mipMapBias; }
            var pixels = marker.GetPixels32(); for (int i = 0; i < pixels.Length; i++) { pixels[i].r = 255; pixels[i].g = 0; pixels[i].b = 255; }
            marker.SetPixels32(pixels); marker.Apply(true, false);
            cameraObject = new GameObject("Original atlas isolated camera"); var isolated = cameraObject.AddComponent<Camera>();
            isolated.CopyFrom(camera); isolated.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation); isolated.enabled = false; isolated.cullingMask = 1 << 31; isolated.clearFlags = CameraClearFlags.SolidColor; isolated.backgroundColor = Color.black;
            target = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32); target.Create(); isolated.targetTexture = target;
            var baseline = Capture(isolated, target);
            foreach (var binding in bindings) binding.material.SetTexture(binding.property, marker);
            var marked = Capture(isolated, target);
            foreach (var binding in bindings) binding.material.SetTexture(binding.property, original);
            var originalPixels = Capture(isolated, target);
            int markerDiff = Difference(baseline, marked), originalDiff = Difference(baseline, originalPixels);
            bool runtimeBound = bindings.All(b => b.old.name == "atlas01-original-steam");
            Debug.Log($"Original atlas visual probe {(markerDiff > 50 ? "PASS" : "INCONCLUSIVE")} runtimeBound={runtimeBound} bindings={bindings.Count} activeRenderers={renderers.Length} particles={particles} markerDiff={markerDiff} originalDiff={originalDiff}");
        }
        catch (Exception e) { Debug.LogError("Original atlas visual probe FAILED " + e); }
        finally
        {
            foreach (var binding in bindings) if (binding.material != null) binding.material.SetTexture(binding.property, binding.old);
            Time.timeScale = oldScale; if (target != null) { target.Release(); Destroy(target); } if (original != null) Destroy(original); if (marker != null) Destroy(marker); if (cameraObject != null) Destroy(cameraObject); Destroy(chest);
        }
    }
    private static byte[] Capture(Camera camera, RenderTexture target)
    {
        RenderTexture previous = RenderTexture.active; var read = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        try { camera.Render(); RenderTexture.active = target; read.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); read.Apply(); return read.GetRawTextureData(); }
        finally { RenderTexture.active = previous; Destroy(read); }
    }
    private static int Difference(byte[] a, byte[] b)
    {
        int count = 0; for (int i = 0; i < a.Length; i += 4) if (Math.Abs(a[i] - b[i]) + Math.Abs(a[i+1] - b[i+1]) + Math.Abs(a[i+2] - b[i+2]) > 8) count++; return count;
    }
}
#endif
