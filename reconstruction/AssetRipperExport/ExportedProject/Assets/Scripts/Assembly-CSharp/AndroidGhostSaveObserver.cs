#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;

// Opt-in observer for an isolated test save; never writes progression itself.
internal sealed class AndroidGhostSaveObserver : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_ghost_save_observer", false)) return;
        var root = new GameObject("Ghost save observer");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidGhostSaveObserver>();
    }
    private void OnEnable() { SaveManager.A_ProgressionSaved += OnSaved; }
    private void OnDisable() { SaveManager.A_ProgressionSaved -= OnSaved; }
    private void OnSaved() { Debug.Log($"Ghost save observer: progression saved, scythe={MyAchievements.IsAchievementDone("a_scythe")}"); }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < deadline && (!SaveManager.loaded || SaveManager.Instance == null || SaveManager.Instance.progression == null)) yield return null;
        if (!SaveManager.loaded || SaveManager.Instance == null || SaveManager.Instance.progression == null)
        { Debug.LogError("Ghost save observer: progression unavailable"); yield break; }
        Debug.Log($"Ghost save observer: loaded scythe={MyAchievements.IsAchievementDone("a_scythe")}");
    }
}
#endif
