#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Test-only check: create a material after the initial scan and do not invoke
// the runtime scanner directly. The material is destroyed after observation.
internal sealed class AndroidOriginalLateTextureProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.test" || !intent.Call<bool>("getBooleanExtra", "megabonk_late_texture_probe", false)) return;
        var go = new GameObject("Original late texture probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidOriginalLateTextureProbe>();
    }
    private IEnumerator Start()
    {
        float sceneDeadline = Time.realtimeSinceStartup + 90f;
        while (SceneManager.GetActiveScene().name != "MainMenu" && Time.realtimeSinceStartup < sceneDeadline) yield return null;
        if (SceneManager.GetActiveScene().name != "MainMenu") { Debug.LogError("Late texture probe INCONCLUSIVE: no main menu"); Destroy(gameObject); yield break; }
        float wait = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < wait) yield return null;
        var runtime = FindFirstObjectByType<AndroidOriginalAtlasRuntime>();
        if (runtime == null) { Debug.LogError("Late texture probe INCONCLUSIVE: runtime missing"); Destroy(gameObject); yield break; }
        Shader shader = null;
        foreach (var mat in Resources.FindObjectsOfTypeAll<Material>()) if (mat != null && mat.HasProperty("_MainTex")) { shader = mat.shader; break; }
        if (shader == null) { Debug.LogError("Late texture probe INCONCLUSIVE: shader missing"); Destroy(gameObject); yield break; }
        var spec = AndroidOriginalAtlasZip.Atlases[0];
        var texture = new Texture2D(spec.Width, spec.Height, TextureFormat.RGBA32, true, false) { name = spec.Name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var material = new Material(shader); material.SetTexture("_MainTex", texture);
        Debug.Log("Late texture probe created material after settled MainMenu");
        float start = Time.realtimeSinceStartup, deadline = start + 12f;
        while (Time.realtimeSinceStartup < deadline && material.GetTexture("_MainTex") == texture) yield return null;
        var actual = material.GetTexture("_MainTex") as Texture2D;
        bool replaced = actual != null && actual != texture && actual.name == spec.Name + "-original-steam" && actual.filterMode == FilterMode.Point && actual.wrapMode == TextureWrapMode.Clamp;
        Debug.Log($"Late texture probe {(replaced ? "PASS" : "FAIL")}: replaced={replaced} elapsed={Time.realtimeSinceStartup-start:F2}");
        if (replaced)
        {
            material.SetTexture("_MainTex", texture);
            deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline && material.GetTexture("_MainTex") == texture) yield return null;
            bool rebound = material.GetTexture("_MainTex") == actual;
            Debug.Log($"Late texture rebind probe {(rebound ? "PASS" : "FAIL")}");
            var clone = new Material(material);
            Destroy(material);
            yield return new WaitForSecondsRealtime(10f);
            bool cloneRetained = actual != null && clone.GetTexture("_MainTex") == actual;
            Debug.Log($"Late texture clone probe {(cloneRetained ? "PASS" : "FAIL")}");
            Destroy(clone);
        }
        else Destroy(material);
        Destroy(texture); Destroy(gameObject);
    }
}
#endif
