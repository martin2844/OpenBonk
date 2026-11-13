#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Synthetic sampler/lifecycle test. Never enabled without the test intent.
internal sealed class AndroidTextureReuseProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_texture_reuse_probe", false)) return;
        var go = new GameObject("Texture reuse probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidTextureReuseProbe>();
    }
    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(10f);
        Shader shader = null;
        foreach (var material in Resources.FindObjectsOfTypeAll<Material>()) if (material != null && material.HasProperty("_MainTex")) { shader = material.shader; break; }
        if (shader == null) { Debug.LogError("Texture reuse probe INCONCLUSIVE: shader unavailable"); yield break; }
        var host = new GameObject("Texture reuse fixture");
        var runtime = host.AddComponent<AndroidOriginalAtlasRuntime>();
        var spec = AndroidOriginalAtlasZip.Atlases[0];
        var raw = (Dictionary<string, byte[]>)typeof(AndroidOriginalAtlasRuntime).GetField("raw", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runtime);
        raw.Add(spec.Name, new byte[spec.Length]);
        var bind = typeof(AndroidOriginalAtlasRuntime).GetMethod("BindMaterial", BindingFlags.NonPublic | BindingFlags.Instance);
        var prune = typeof(AndroidOriginalAtlasRuntime).GetMethod("PruneBindings", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = new List<Texture2D>(); var materials = new List<Material>();
        var counts = new Dictionary<string, int>();
        Texture shared = null, distinct = null; Material clone = null;
        bool ok = true;
        try
        {
            for (int i = 0; i < 34; i++)
            {
                var source = new Texture2D(spec.Width, spec.Height, TextureFormat.RGBA32, true, false) { name = spec.Name, filterMode = i == 33 ? FilterMode.Bilinear : FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var material = new Material(shader); material.SetTexture("_MainTex", source);
                sources.Add(source); materials.Add(material); bind.Invoke(runtime, new object[] { material, counts });
                var actual = material.GetTexture("_MainTex");
                if (i == 0) shared = actual;
                if (i < 33) ok &= actual != null && actual != source && actual == shared;
                else { distinct = actual; ok &= distinct != null && distinct != shared && distinct.filterMode == FilterMode.Bilinear; }
            }
            clone = new Material(materials[0]);
            materials[0].SetTexture("_MainTex", sources[0]); prune.Invoke(runtime, null);
            ok &= clone.GetTexture("_MainTex") == shared && materials[1].GetTexture("_MainTex") == shared;
            Debug.Log($"Texture reuse probe {(ok ? "PASS" : "FAIL")}: sourceInstances=34 sharedPoint=33 distinctBilinear={(distinct != shared)} cloneRetained={clone.GetTexture("_MainTex") == shared}");
        }
        catch (Exception e) { Debug.LogError("Texture reuse probe FAILED: " + e); }
        finally
        {
            Destroy(host); foreach (var material in materials) Destroy(material); foreach (var source in sources) Destroy(source); if (clone != null) Destroy(clone);
            Destroy(gameObject);
        }
    }
}
#endif
