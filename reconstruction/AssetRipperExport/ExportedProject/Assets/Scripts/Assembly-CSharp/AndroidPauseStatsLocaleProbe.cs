#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using UnityEngine;
using UnityEngine.Localization.Settings;

internal sealed class AndroidPauseStatsLocaleProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_pause_stats_locale_probe", false)) return;
        var root = new GameObject("pause stats locale probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPauseStatsLocaleProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        PauseUi pause = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            pause = UiManager.Instance != null ? UiManager.Instance.pause : null;
            if (pause != null && pause.CanPause() && MyPlayer.Instance != null && MyPlayer.Instance.hasStarted) break;
            yield return null;
        }
        if (pause == null || !pause.CanPause()) { Debug.LogError("Pause stats locale probe INCONCLUSIVE: gameplay unavailable"); yield break; }
        var locale = LocalizationSettings.AvailableLocales.GetLocale("es-419");
        if (locale == null) { Debug.LogError("Pause stats locale probe INCONCLUSIVE: locale unavailable"); yield break; }
        LocalizationSettings.SelectedLocale = locale;
        pause.Pause();
        yield return new WaitForSecondsRealtime(2f);
        var stats = pause.GetComponentInChildren<StatsUi>(true);
        if (stats == null) { Debug.LogError("Pause stats locale probe INCONCLUSIVE: stats panel unavailable"); yield break; }
        int count = 0, missing = 0, overflow = 0;
        var entries = (List<StatEntry>)typeof(StatsUi).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stats);
        if (entries == null) { Debug.LogError("Pause stats locale probe INCONCLUSIVE: entries not initialized"); yield break; }
        foreach (var entry in entries)
        {
            if (!entry.gameObject.activeInHierarchy) continue;
            count++;
            if (entry.t_stat == null || entry.t_value == null || string.IsNullOrWhiteSpace(entry.t_stat.text) || entry.t_stat.text.Contains("LOCALIZATION ERROR") || string.IsNullOrWhiteSpace(entry.t_value.text)) { missing++; continue; }
            entry.t_stat.ForceMeshUpdate(); entry.t_value.ForceMeshUpdate();
            if (entry.t_stat.isTextOverflowing || entry.t_value.isTextOverflowing) overflow++;
        }
        Debug.Log($"Pause stats locale probe {(count == 31 && missing == 0 && overflow == 0 ? "PASS" : "INCONCLUSIVE")}: locale={LocalizationSettings.SelectedLocale.Identifier.Code} entries={count} missing={missing} tmpOverflowRows={overflow}");
    }
}
#endif
