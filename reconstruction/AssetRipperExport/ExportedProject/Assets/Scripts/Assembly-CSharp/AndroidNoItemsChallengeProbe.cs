#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Linq;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Other;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Challenges;
using UnityEngine;

// Disposable emulator probe: install the exported challenge before real run initialization.
internal sealed class AndroidNoItemsChallengeProbe : MonoBehaviour
{
    private static bool enabledForRun;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_no_items_challenge_probe", false)) return;
        enabledForRun = true;
        var go = new GameObject("No items challenge probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidNoItemsChallengeProbe>();
    }
    public static void Configure(RunConfig config)
    {
        if (!enabledForRun) return;
        var challenge = DataManager.Instance != null ? DataManager.Instance.GetAchievement("a_forest1_no_items") as ChallengeData : null;
        if (challenge == null) { Debug.LogError("No items challenge probe: original challenge unavailable"); return; }
        config.challenge = challenge;
        Debug.Log($"No items challenge probe: configured={challenge.internalName}, modifiers={challenge.challengeModifiers.Length}");
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MapController.currentMap == null)) yield return null;
        if (MyPlayer.Instance == null || MapController.currentMap == null) { Debug.LogError("No items challenge probe: gameplay unavailable"); yield break; }
        yield return new WaitForSecondsRealtime(3f);
        var sources = Resources.FindObjectsOfTypeAll<BaseInteractable>().Where(x => x != null && x.gameObject.scene.IsValid() && x.gameObject.scene.isLoaded && x.isItemSource).ToArray();
        int active = sources.Count(x => x.transform.root.gameObject.activeSelf);
        int inactive = sources.Length - active;
        bool modifier = ChallengesTracker.HasChallengeModifier(ChallengesTracker.MODIFIER_NO_ITEMS);
        Debug.Log($"No items challenge probe {(modifier && sources.Length > 0 && active == 0 ? "PASS" : "OBSERVE")}: challenge={MapController.runConfig?.challenge?.internalName}, modifier={modifier}, itemSources={sources.Length}, activeRoots={active}, inactiveRoots={inactive}, types=[{string.Join(", ", sources.Select(x => x.GetType().Name).Distinct())}]");
    }
}
#endif
