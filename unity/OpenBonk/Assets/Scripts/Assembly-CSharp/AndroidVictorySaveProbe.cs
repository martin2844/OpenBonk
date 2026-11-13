#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Linq;
using Assets.Scripts._Data.MapsAndStages;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using UnityEngine;

// Read-only inspection of the emulator's normal save after an opt-in restart.
internal sealed class AndroidVictorySaveProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_victory_read_probe", false)) return;
        var go = new GameObject("Victory save read probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidVictorySaveProbe>();
    }
    IEnumerator Start()
    {
        float until = Time.realtimeSinceStartup + 90f;
        while (Time.realtimeSinceStartup < until && (!SaveManager.loaded || SaveManager.Instance == null || SaveManager.Instance.progression == null || SaveManager.Instance.stats == null)) yield return null;
        Log("reloaded");
    }
    internal static void Log(string stage)
    {
        var save = SaveManager.Instance != null ? SaveManager.Instance.progression : null;
        if (!SaveManager.loaded || save == null || save.menuMeta == null) { Debug.LogError($"Victory save probe: {stage} save unavailable"); return; }
        var progress = save.menuMeta.GetMapProgress(EMap.Forest);
        progress.numRunsByTier.TryGetValue(0, out int runs);
        string completed = string.Join(",", progress.completedTiers.OrderBy(x => x));
        string characters = string.Join(",", progress.GetTierCompletionCharacters(0).Select(x => x.ToString()).OrderBy(x => x));
        string characterRuns = string.Join(",", save.characterProgression.OrderBy(x => x.Key.ToString()).Select(x => $"{x.Key}:{x.Value.numRuns}:{x.Value.xp}"));
        float bossKills = MyStats.GetStat("finalBossKills");
        Debug.Log($"Victory save probe: {stage} tier0={progress.IsTierCompleted(0)}, tiers=[{completed}], characters=[{characters}], tierRuns={runs}, characterRuns=[{characterRuns}], finalBossKills={bossKills}");
        string achievements = string.Join(",", save.achievements.OrderBy(x => x));
        Debug.Log($"Victory save probe rewards: {stage} silver={save.silver}, achievements=[{achievements}]");
    }
}
#endif
