#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;

// Read-only observer for an isolated save/restart diagnostic.
internal sealed class AndroidChallengePersistenceProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_challenge_persistence_probe", false)) return;
        var go = new GameObject("Challenge persistence probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidChallengePersistenceProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (!SaveManager.loaded || SaveManager.Instance == null || SaveManager.Instance.progression == null || DataManager.Instance == null || DataManager.Instance.GetAchievement("a_forest1_turbo") == null)) yield return null;
        if (!SaveManager.loaded || SaveManager.Instance == null || DataManager.Instance == null || DataManager.Instance.GetAchievement("a_forest1_turbo") == null) { Debug.LogError("Challenge persistence probe: save unavailable"); yield break; }
        SaveManager.A_ProgressionSaved += OnSaved;
        Debug.Log($"Challenge persistence probe: loaded unlocked={Unlocked()}");
    }
    static bool Unlocked() { var achievement = DataManager.Instance != null ? DataManager.Instance.GetAchievement("a_forest1_turbo") : null; return achievement != null && MyAchievements.IsUnlocked(achievement); }
    static void OnSaved() { Debug.Log($"Challenge persistence probe: progressionSaved unlocked={Unlocked()}"); }
    void OnDestroy() { SaveManager.A_ProgressionSaved -= OnSaved; }
}
#endif
