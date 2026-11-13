#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

internal sealed class AndroidDeferredAudioProbe : MonoBehaviour
{
    static bool installed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (installed) return;
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_deferred_audio_probe", false)) return;
        installed = true;
        var root = new GameObject("deferred audio probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidDeferredAudioProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline &&
               (CurrentSettings.Instance == null || CurrentSettings.Instance.audioMixer == null ||
                SaveManager.Instance == null || SaveManager.Instance.config == null ||
                SaveManager.Instance.config.cfAudioSettings == null || !SaveManager.loaded))
            yield return null;
        var settings = CurrentSettings.Instance;
        if (settings == null || settings.audioMixer == null || SaveManager.Instance == null ||
            SaveManager.Instance.config == null || SaveManager.Instance.config.cfAudioSettings == null || !SaveManager.loaded)
        {
            Debug.LogError("Deferred audio probe FAIL: startup timeout");
            yield break;
        }
        yield return null;
        var audio = SaveManager.Instance.config.cfAudioSettings;
        var entries = new[] { ("MasterVolume", audio.master_volume), ("MusicVolume", audio.music),
            ("GameSfxVolume", audio.game_sfx), ("AmbienceVolume", audio.ambience), ("UiVolume", audio.ui) };
        var seen = new HashSet<string>();
        Action<string, object, object> listener = (name, _, __) => seen.Add(name);
        CurrentSettings.A_SettingUpdated += listener;
        foreach (var entry in entries) settings.audioMixer.SetFloat(entry.Item1, -70f);
        typeof(CurrentSettings).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(settings, null);
        yield return null;
        yield return null;
        bool pass = seen.SetEquals(new[] { "master_volume", "music", "game_sfx", "ambience", "ui", "xp_and_gold" });
        foreach (var entry in entries)
        {
            float expected = entry.Item2 <= .0001f ? -80f : 20f * Mathf.Log10(entry.Item2);
            bool ok = settings.audioMixer.GetFloat(entry.Item1, out float actual) && Mathf.Abs(actual - expected) < .01f;
            Debug.Log($"Deferred audio probe: {entry.Item1} actual={actual:F2} expected={expected:F2} ok={ok}");
            pass &= ok;
        }
        CurrentSettings.A_SettingUpdated -= listener;
        Debug.Log($"Deferred audio probe {(pass ? "PASS" : "FAIL")}: sixNotifications={seen.Count}");
    }
}
#endif
