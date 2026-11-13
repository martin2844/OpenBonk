#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Opt-in Test-only synthetic material binding and sampler check.
internal sealed class AndroidOriginalAtlasMultiProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.test" || !intent.Call<bool>("getBooleanExtra", "megabonk_atlas_multi_probe", false)) return;
        var go = new GameObject("Original atlas multi probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidOriginalAtlasMultiProbe>();
    }
    private IEnumerator Start()
    {
        for (int i = 0; i < 12; i++) yield return null;
        var materials = new List<Material>(); var textures = new List<Texture2D>();
        try
        {
            var runtime = FindObjectOfType<AndroidOriginalAtlasRuntime>();
            if (runtime == null) throw new Exception("Runtime is absent");
            Shader shader = null;
            foreach (var mat in Resources.FindObjectsOfTypeAll<Material>()) if (mat != null && mat.HasProperty("_MainTex")) { shader = mat.shader; break; }
            if (shader == null) throw new Exception("No shader with main texture");
            foreach (var spec in AndroidOriginalAtlasZip.Atlases)
            {
                var texture = new Texture2D(spec.Width, spec.Height, TextureFormat.RGBA32, spec.Mips > 1, false) { name = spec.Name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var material = new Material(shader); material.SetTexture("_MainTex", texture); textures.Add(texture); materials.Add(material);
            }
            typeof(AndroidOriginalAtlasRuntime).GetMethod("Scan", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(runtime, null);
            for (int i = 0; i < materials.Count; i++)
            {
                var spec = AndroidOriginalAtlasZip.Atlases[i]; var result = materials[i].GetTexture("_MainTex") as Texture2D;
                if (result == null || result == textures[i] || result.name != spec.Name + "-original-steam" || result.filterMode != FilterMode.Point || result.wrapMode != TextureWrapMode.Clamp)
                    throw new Exception("Binding or sampler mismatch: " + spec.Name);
                Debug.Log("Original atlas multi probe PASS: " + spec.Name);
            }
        }
        catch (Exception e) { Debug.LogError("Original atlas multi probe FAILED: " + e); }
        finally { foreach (var m in materials) Destroy(m); foreach (var t in textures) Destroy(t); Destroy(gameObject); }
    }
}
#endif
