#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Isolated Test-package diagnostic; never runs in the launcher build.
internal sealed class AndroidPikachuBundleProbe : MonoBehaviour
{
    private bool packageProbe;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        bool package = intent.Call<bool>("getBooleanExtra", "openbonk_pikachu_package_probe", false);
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.test" || (!package && !intent.Call<bool>("getBooleanExtra", "openbonk_pikachu_bundle_probe", false))) return;
        var go = new GameObject("Pikachu bundle test"); DontDestroyOnLoad(go); go.AddComponent<AndroidPikachuBundleProbe>().packageProbe = package;
    }
    private IEnumerator Start()
    {
        yield return null;
        AssetBundle bundle = null; GameObject instance = null; string temporary = null;
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "Probe/pikachu-model");
            if (packageProbe)
            {
                temporary = Path.Combine(Application.temporaryCachePath, "pikachu-content-probe-" + Guid.NewGuid().ToString("N"));
                using var archive = File.OpenRead(Path.Combine(Application.persistentDataPath, "Probe/pikachu-content.zip"));
                string id = OpenBonkContentArchive.Install(archive, temporary);
                if (id != "pikachu-test") throw new InvalidDataException("unexpected content ID");
                path = Path.Combine(temporary, id, "assets/model.bundle");
                bool refused = false;
                try { OpenBonkContentArchive.Install(archive, temporary); }
                catch (IOException e) when (e.Message == "Mod ID already installed") { refused = true; }
                if (!refused) throw new InvalidDataException("duplicate was accepted");
            }
            bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null) throw new IOException("bundle could not be opened");
            GameObject source = bundle.LoadAsset<GameObject>("pikachu-study");
            AnimationClip found = null;
            foreach (var clip in bundle.LoadAllAssets<AnimationClip>()) if (clip.name.Contains("OpenBonk walk")) found = clip;
            if (source == null || found == null) throw new InvalidDataException("model or walk clip missing");
            instance = Instantiate(source);
            if (instance.GetComponent<Animator>() == null) instance.AddComponent<Animator>();
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0) throw new InvalidDataException("skinned mesh missing");
            found.SampleAnimation(instance, 0f);
            var before = new Mesh(); renderers[0].BakeMesh(before); var first = before.vertices; Destroy(before);
            found.SampleAnimation(instance, 0.5f);
            var after = new Mesh(); renderers[0].BakeMesh(after); var second = after.vertices; Destroy(after);
            float max = 0f;
            for (int i = 0; i < Math.Min(first.Length, second.Length); i++) max = Mathf.Max(max, (first[i] - second[i]).magnitude);
            if (max <= 0.00001f) throw new InvalidDataException("animation did not deform sampled mesh");
            Debug.Log($"OpenBonk Pikachu {(packageProbe ? "package" : "bundle")} probe PASS renderers={renderers.Length} vertices={first.Length} clip={found.name} seconds={found.length:F2} displacement={max:F4}");
        }
        catch (Exception e) { Debug.LogError("OpenBonk Pikachu bundle probe FAIL: " + e); }
        finally { if (instance != null) Destroy(instance); if (bundle != null) bundle.Unload(true); if (temporary != null && Directory.Exists(temporary)) Directory.Delete(temporary, true); Destroy(gameObject); }
    }
}
#endif
