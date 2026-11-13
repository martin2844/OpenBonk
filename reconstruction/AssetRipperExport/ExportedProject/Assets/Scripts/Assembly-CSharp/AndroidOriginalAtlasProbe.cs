#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in experiment only: replaces GPU copies in the disposable test process.
internal sealed class AndroidOriginalAtlasProbe : MonoBehaviour
{
    private static bool readZip;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_original_atlas_probe", false)) return;
        readZip = intent.Call<bool>("getBooleanExtra", "megabonk_original_atlas_zip_probe", false);
        var go = new GameObject("Original atlas probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidOriginalAtlasProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180;
        while (Time.realtimeSinceStartup < deadline && SceneManager.GetActiveScene().name != "GeneratedMap") yield return null;
        if (SceneManager.GetActiveScene().name != "GeneratedMap") { Debug.LogError("Original atlas probe INCONCLUSIVE no gameplay scene"); yield break; }
        yield return null;
        Texture2D source = null;
        try
        {
            var matches = Resources.FindObjectsOfTypeAll<Texture2D>().Where(t => t.name == "atlas01").ToArray();
            Debug.Log($"Original atlas probe matches={matches.Length} copySupport={SystemInfo.copyTextureSupport}");
            byte[] raw;
            if (readZip)
            {
                using var zip = File.OpenRead(Path.Combine(Application.persistentDataPath, "Probe", "source.zip"));
                raw = AndroidOriginalAtlasZip.Extract(zip);
                Debug.Log($"Original atlas ZIP extracted and verified: bytes={raw.Length}");
            }
            else raw = File.ReadAllBytes(Path.Combine(Application.persistentDataPath, "Probe", "atlas01.raw"));
            source = new Texture2D(512, 256, TextureFormat.RGBA32, true); source.LoadRawTextureData(raw); source.Apply(false, false);
            foreach (var target in matches)
            {
                Debug.Log($"Original atlas probe target={target.name} size={target.width}x{target.height} format={target.format} graphics={target.graphicsFormat} mips={target.mipmapCount} readable={target.isReadable} sourceGraphics={source.graphicsFormat}");
                if (target.width != 512 || target.height != 256 || target.mipmapCount != source.mipmapCount || target.graphicsFormat != source.graphicsFormat) continue;
                if (SystemInfo.copyTextureSupport == UnityEngine.Rendering.CopyTextureSupport.None)
                {
                    TestMaterialSwap(target, source, raw);
                    continue;
                }
                string before = Capture(target);
                Color32 p = source.GetPixel(0, 0); source.SetPixel(0, 0, new Color32((byte)(255 - p.r), (byte)(255 - p.g), (byte)(255 - p.b), 255)); source.Apply(false, false);
                string marked = Capture(source); Graphics.CopyTexture(source, target); string copied = Capture(target);
                source.LoadRawTextureData(raw); source.Apply(false, false); Graphics.CopyTexture(source, target);
                string restored = Capture(target), expected = Capture(source);
                bool pass = marked == copied && copied != before && restored == expected;
                Debug.Log($"Original atlas probe {(pass ? "PASS" : "FAIL")} markerCopied={marked == copied} changed={copied != before} originalRestored={restored == expected} before={before} restoredHash={restored}");
            }
        }
        catch (Exception e) { Debug.LogError("Original atlas probe FAILED " + e); }
        finally { if (source != null) Destroy(source); }
    }
    private static void TestMaterialSwap(Texture2D target, Texture2D source, byte[] raw)
    {
        var bindings = new System.Collections.Generic.List<(Material material, string property)>();
        foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
            foreach (string property in material.GetTexturePropertyNames())
                if (material.GetTexture(property) == target) bindings.Add((material, property));
        var renderers = FindObjectsOfType<Renderer>();
        int activeRenderers = renderers.Count(r => r.enabled && r.sharedMaterials.Any(m => bindings.Any(b => b.material == m)));
        Color32 p = source.GetPixel(0, 0); source.SetPixel(0, 0, new Color32((byte)(255 - p.r), (byte)(255 - p.g), (byte)(255 - p.b), 255)); source.Apply(false, false);
        string marked = Capture(source);
        try
        {
            foreach (var binding in bindings) binding.material.SetTexture(binding.property, source);
            bool assigned = bindings.Count > 0 && bindings.All(b => b.material.GetTexture(b.property) == source);
            bool pixels = bindings.Count > 0 && Capture(bindings[0].material.GetTexture(bindings[0].property)) == marked;
            source.LoadRawTextureData(raw); source.Apply(false, false);
            string original = Capture(source);
            bool restoredOriginalPixels = bindings.Count > 0 && Capture(bindings[0].material.GetTexture(bindings[0].property)) == original && original != marked;
            Debug.Log($"Original atlas material probe {(assigned && pixels && restoredOriginalPixels ? "PASS" : "FAIL")} bindings={bindings.Count} activeRenderers={activeRenderers} assigned={assigned} markerPixels={pixels} originalPixels={restoredOriginalPixels}");
        }
        finally { foreach (var binding in bindings) binding.material.SetTexture(binding.property, target); }
    }
    private static string Capture(Texture texture)
    {
        RenderTexture previous = RenderTexture.active;
        var rt = RenderTexture.GetTemporary(512, 256, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        var read = new Texture2D(512, 256, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(texture, rt); RenderTexture.active = rt;
            read.ReadPixels(new Rect(0, 0, 512, 256), 0, 0); read.Apply();
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(read.GetRawTextureData())).Replace("-", "").ToLowerInvariant();
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); Destroy(read); }
    }
}
#endif
