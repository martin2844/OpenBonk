#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Managers;
using UnityEngine;

// Paired with the opt-in stage transition probe and its stage-two-only fixture.
internal sealed class AndroidMapObjectStagesProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.splittest" || !intent.Call<bool>("getBooleanExtra", "openbonk_map_object_stages_probe", false)) return;
        var root = new GameObject("Map object stages probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidMapObjectStagesProbe>();
    }
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MapController.currentMap == null || MapController.index != 0 || MapGenerationController.isGenerating || GameManager.Instance == null || !GameManager.Instance.IsPlaying())) yield return null;
        if (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MapController.currentMap == null || MapController.index != 0 || MapGenerationController.isGenerating || GameManager.Instance == null || !GameManager.Instance.IsPlaying()) { Debug.LogError("Map object stages probe INCONCLUSIVE: initial stage unavailable"); yield break; }
        int first = Count();
        var firstPlayer = MyPlayer.Instance;
        var firstStage = MapController.currentStage;
        while (Time.realtimeSinceStartup < deadline && (MapController.index != 1 || MapController.currentStage == firstStage || MyPlayer.Instance == null || MyPlayer.Instance == firstPlayer || !MyPlayer.Instance.hasStarted)) yield return null;
        if (MapController.index != 1 || MapController.currentStage == firstStage || MyPlayer.Instance == null || MyPlayer.Instance == firstPlayer || !MyPlayer.Instance.hasStarted) { Debug.LogError("Map object stages probe INCONCLUSIVE: second stage unavailable"); yield break; }
        int second = 0;
        float generationDeadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < generationDeadline && MapController.index == 1 && second == 0) { second = Count(); if (second == 0) yield return null; }
        Debug.Log($"Map object stages probe {(first == 0 && second > 0 ? "PASS" : "FAIL")}: stage1={first} stage2={second}");
    }
    private static int Count()
    {
        int count = 0; foreach (var item in FindObjectsOfType<Transform>()) if (item.name == "ProbeBeacon(Clone)") count++; return count;
    }
}
#endif
