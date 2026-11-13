#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections.Generic;
using Assets.Scripts.Managers;
using UnityEngine;

// Opt-in diagnostic: report visible Standard renderers at the first link warning.
internal sealed class AndroidStandardShaderProbe : MonoBehaviour
{
    private bool pending;
    private bool reported;
    private bool callbackChecked;
    private bool callbackObserved;
    private float nextSnapshot;
    private int snapshotCount;
    private bool forceFade;
    private float forceUntil;
    private bool forceStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_standard_shader_probe", false)) return;
        var root = new GameObject("Standard shader probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidStandardShaderProbe>().forceFade = intent.Call<bool>("getBooleanExtra", "megabonk_standard_shader_force_fade", false);
    }

    private void OnEnable() { Application.logMessageReceived += Observe; }
    private void OnDisable() { Application.logMessageReceived -= Observe; }
    private void Observe(string message, string stack, LogType type)
    {
        if (message == "Standard shader probe callback self-test") callbackObserved = true;
        if (!reported && message.Contains("Shader Standard")) pending = true;
    }

    private void LateUpdate()
    {
        if (!callbackChecked)
        {
            callbackChecked = true;
            Debug.LogWarning("Standard shader probe callback self-test");
            Debug.Log($"Standard shader probe: managed callback self-test={callbackObserved}");
        }
        if (forceFade && !forceStarted && GameManager.Instance != null && GameManager.Instance.IsPlaying())
        {
            forceStarted = true;
            forceUntil = Time.realtimeSinceStartup + 15f;
        }
        if (forceStarted && Time.realtimeSinceStartup < forceUntil)
        {
            foreach (var fade in FindObjectsOfType<CameraFadeObject>())
            {
                if (fade.renderer == null || !fade.renderer.isVisible || fade.fadeMaterial == null ||
                    fade.fadeMaterial.shader == null || fade.fadeMaterial.shader.name != "Standard") continue;
                PlayerCamera.A_CameraFadeObjectEnter?.Invoke(fade.gameObject);
            }
        }
        bool warningSnapshot = pending && !reported;
        bool periodicSnapshot = GameManager.Instance != null && GameManager.Instance.IsPlaying() &&
            Time.realtimeSinceStartup >= nextSnapshot && snapshotCount < 12;
        if (!warningSnapshot && !periodicSnapshot) return;
        if (warningSnapshot) reported = true;
        nextSnapshot = Time.realtimeSinceStartup + 5f;
        snapshotCount++;
        var counts = new Dictionary<string, int>();
        foreach (var renderer in FindObjectsOfType<Renderer>())
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || !renderer.isVisible) continue;
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == null || material.shader.name != "Standard") continue;
                string key = $"material={material.name}, instancing={material.enableInstancing}, renderer={renderer.name}, type={renderer.GetType().Name}";
                counts.TryGetValue(key, out int count);
                counts[key] = count + 1;
            }
        }
        Debug.Log($"Standard shader probe: snapshot={snapshotCount}, warning={warningSnapshot}, visible renderer groups={counts.Count}");
        foreach (var item in counts) Debug.Log($"Standard shader probe: {item.Key}, count={item.Value}");
    }
}
#endif
