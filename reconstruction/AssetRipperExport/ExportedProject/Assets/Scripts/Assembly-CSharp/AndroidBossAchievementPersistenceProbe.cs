#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using UnityEngine;

// Read-only observation of progression across an isolated app restart.
internal sealed class AndroidBossAchievementPersistenceProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_boss_achievement_persistence_probe", false)) return;
        var go = new GameObject("Boss achievement persistence probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidBossAchievementPersistenceProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (!SaveManager.loaded || SaveManager.Instance == null || SaveManager.Instance.progression == null || DataManager.Instance == null)) yield return null;
        if (!SaveManager.loaded || SaveManager.Instance == null || SaveManager.Instance.progression == null) { Debug.LogError("Boss achievement persistence probe: save unavailable"); yield break; }
        SaveManager.A_ProgressionSaved += OnSaved;
        LogState("loaded");
    }
    static void LogState(string reason)
    {
        var progression = SaveManager.Instance != null ? SaveManager.Instance.progression : null;
        if (progression == null || progression.achievements == null) { Debug.LogError("Boss achievement persistence probe: missing progression"); return; }
        var unlocked = progression.achievements;
        Debug.Log($"Boss achievement persistence probe: {reason} cursed={unlocked.Contains("a_cursedTome")}, holy={unlocked.Contains("a_holyBook")}, speed={unlocked.Contains("a_skin_foxSpeedrun")}, final={unlocked.Contains("a_skin_foxFinalBoss")}");
    }
    static void OnSaved() { LogState("saved"); }
    void OnDestroy() { SaveManager.A_ProgressionSaved -= OnSaved; }
}
#endif
