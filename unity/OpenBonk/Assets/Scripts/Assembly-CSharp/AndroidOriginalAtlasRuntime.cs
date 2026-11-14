#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Experimental split build only; old text-only or atlas01-only generations remain supported.
internal sealed class AndroidOriginalAtlasRuntime : MonoBehaviour
{
    private readonly Dictionary<string, byte[]> raw = new Dictionary<string, byte[]>(StringComparer.Ordinal);
    // Multiple scene instances can contain equivalent copies of one texture.
    // Share only when their full sampler and graphics format also match.
    private readonly Dictionary<(string name, UnityEngine.Experimental.Rendering.GraphicsFormat format, FilterMode filter,
        TextureWrapMode u, TextureWrapMode v, TextureWrapMode w, int aniso, float bias), Texture2D> replacements =
        new Dictionary<(string, UnityEngine.Experimental.Rendering.GraphicsFormat, FilterMode,
            TextureWrapMode, TextureWrapMode, TextureWrapMode, int, float), Texture2D>();
    private readonly List<(Material material, string property, Texture original, Texture replacement)> bindings = new List<(Material, string, Texture, Texture)>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        string package = activity.Call<string>("getPackageName");
        bool test = package == "com.megabonk.nativeport.test" && intent.Call<bool>("getBooleanExtra", "megabonk_original_atlas_runtime_probe", false);
        if (package != "com.megabonk.nativeport.splittest" && !test &&
            !intent.Call<bool>("getBooleanExtra", "openbonk_split_launcher", false)) return;
        string active = AndroidOriginalLocalizationStore.ActiveDirectory(Path.Combine(Application.persistentDataPath, "OriginalLocalization"));
        if (active == null || !File.Exists(Path.Combine(active, "atlas01.raw"))) return;
        var go = new GameObject("Original atlas runtime"); DontDestroyOnLoad(go); go.AddComponent<AndroidOriginalAtlasRuntime>();
    }
    private void Start()
    {
        string active = AndroidOriginalLocalizationStore.ActiveDirectory(Path.Combine(Application.persistentDataPath, "OriginalLocalization"));
        foreach (var spec in AndroidOriginalAtlasZip.Atlases)
        {
            string path = Path.Combine(active, spec.Name + ".raw");
            if (!File.Exists(path)) continue;
            try
            {
                if (new FileInfo(path).Length != spec.Length) throw new InvalidDataException("Invalid atlas size");
                byte[] bytes = File.ReadAllBytes(path); AndroidOriginalAtlasZip.VerifyRaw(spec, bytes); raw.Add(spec.Name, bytes);
            }
            catch (Exception e) { Debug.LogWarning("Stored original atlas unavailable: " + spec.Name + ": " + e.Message); }
        }
        if (raw.Count == 0) { Destroy(gameObject); return; }
        SceneManager.sceneLoaded += OnSceneLoaded; StartCoroutine(ScanSoon()); StartCoroutine(ScanPeriodically());
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        foreach (var binding in bindings) if (binding.material != null && binding.material.GetTexture(binding.property) == binding.replacement) binding.material.SetTexture(binding.property, binding.original);
        foreach (var replacement in replacements.Values) if (replacement != null) Destroy(replacement);
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { StartCoroutine(ScanSoon()); }
    private IEnumerator ScanSoon() { yield return null; yield return null; Scan(); }
    private void PruneBindings()
    {
        bindings.RemoveAll(b => b.material == null || b.material.GetTexture(b.property) != b.replacement);
        // A material can be cloned after binding, inheriting the replacement
        // without appearing in bindings. Keep replacements until this runtime
        // is destroyed so those clones cannot lose their texture.
    }
    private IEnumerator ScanPeriodically()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(3f);
            Material[] materials;
            try { PruneBindings(); materials = Resources.FindObjectsOfTypeAll<Material>(); }
            catch (Exception e) { Debug.LogWarning("Stored original texture scan failed: " + e.Message); continue; }
            var counts = new Dictionary<string, int>();
            int index = 0;
            while (index < materials.Length)
            {
                long start = System.Diagnostics.Stopwatch.GetTimestamp();
                int visited = 0;
                do
                {
                    try { BindMaterial(materials[index], counts); }
                    catch (Exception e) { Debug.LogWarning("Stored original texture material failed: " + e.Message); }
                    index++; visited++;
                }
                while (index < materials.Length && visited < 64 &&
                    System.Diagnostics.Stopwatch.GetTimestamp() - start < System.Diagnostics.Stopwatch.Frequency * 0.0015);
                if (index < materials.Length) yield return null;
            }
            LogCounts(counts);
        }
    }
    private void Scan()
    {
        var counts = new Dictionary<string, int>();
        try
        {
            PruneBindings();
            foreach (var material in Resources.FindObjectsOfTypeAll<Material>()) BindMaterial(material, counts);
            LogCounts(counts);
        }
        catch (Exception e) { Debug.LogWarning("Stored original atlas binding failed: " + e.Message); }
    }
    private void BindMaterial(Material material, Dictionary<string, int> counts)
    {
        if (material == null) return;
        foreach (string property in material.GetTexturePropertyNames())
        {
            var target = material.GetTexture(property) as Texture2D;
            if (target == null || !raw.TryGetValue(target.name, out var bytes)) continue;
            AndroidOriginalAtlasZip.Spec spec = null;
            foreach (var candidate in AndroidOriginalAtlasZip.Atlases) if (candidate.Name == target.name) { spec = candidate; break; }
            if (spec == null || target.width != spec.Width || target.height != spec.Height || target.format != TextureFormat.RGBA32 || target.mipmapCount != spec.Mips) continue;
            var key = (spec.Name, target.graphicsFormat, target.filterMode, target.wrapModeU, target.wrapModeV,
                target.wrapModeW, target.anisoLevel, target.mipMapBias);
            if (!replacements.TryGetValue(key, out var replacement))
            {
                replacement = new Texture2D(spec.Width, spec.Height, TextureFormat.RGBA32, spec.Mips > 1, false) { name = spec.Name + "-original-steam" };
                if (replacement.graphicsFormat != target.graphicsFormat) { Destroy(replacement); continue; }
                replacement.filterMode = target.filterMode; replacement.wrapModeU = target.wrapModeU; replacement.wrapModeV = target.wrapModeV; replacement.wrapModeW = target.wrapModeW;
                replacement.anisoLevel = target.anisoLevel; replacement.mipMapBias = target.mipMapBias;
                replacement.LoadRawTextureData(bytes); replacement.Apply(false, true); replacements.Add(key, replacement);
            }
            material.SetTexture(property, replacement); bindings.Add((material, property, target, replacement));
            counts[spec.Name] = counts.TryGetValue(spec.Name, out int count) ? count + 1 : 1;
        }
    }
    private void LogCounts(Dictionary<string, int> counts)
    {
        foreach (var count in counts) Debug.Log($"Stored original atlas applied: name={count.Key} bindings={count.Value} scene={SceneManager.GetActiveScene().name}");
    }
}
#endif
