#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.Managers;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using Assets.Scripts.Saves___Serialization.Progression.Stats;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using Assets.Scripts.Saves___Serialization.SaveFiles;
using UnityEngine;

internal sealed class AndroidQuestClaimProbe : MonoBehaviour
{
    private const string QuestId = "a_chadwell";
    private bool seed;
    private int events;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_quest_claim_probe", false)) return;
        var root = new GameObject("quest claim probe"); DontDestroyOnLoad(root);
        root.AddComponent<AndroidQuestClaimProbe>().seed = intent.Call<bool>("getBooleanExtra", "megabonk_quest_claim_seed", false);
    }

    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        MainMenu menu = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            menu = FindObjectOfType<MainMenu>();
            if (menu != null && SaveManager.loaded && SaveManager.Instance?.progression != null && DataManager.Instance != null) break;
            yield return null;
        }
        if (menu == null || !SaveManager.loaded || SaveManager.Instance?.progression == null || DataManager.Instance == null)
        { Debug.LogError("Quest claim probe INCONCLUSIVE: startup unavailable"); yield break; }
        var achievement = DataManager.Instance.GetAchievement(QuestId);
        if (achievement == null) { Debug.LogError("Quest claim probe INCONCLUSIVE: achievement missing"); yield break; }
        var save = SaveManager.Instance.progression;
        if (!seed)
        {
            Debug.Log($"Quest claim probe RELOAD: completed={achievement.IsCompleted()} claimed={achievement.IsClaimed()} silver={save.silver} reward={achievement.GetSilverReward()}");
            yield break;
        }
        MyStats.SetValueForce(EMyStat.runs.ToString(), 4f);
        bool unlocked = MyAchievements.TryUnlock(QuestId);
        int silverBefore = save.silver, reward = achievement.GetSilverReward();
        ProgressionSaveFile.A_AchievementClaimed += OnClaim;
        yield return new WaitForSecondsRealtime(18f);
        menu.GoToQuests();
        yield return new WaitForSecondsRealtime(2f);
        MyButtonQuest target = null;
        foreach (var button in FindObjectsOfType<MyButtonQuest>())
            if (button.achievement == achievement) { target = button; break; }
        Debug.Log($"Quest claim probe READY: unlocked={unlocked} row={(target != null)} completed={achievement.IsCompleted()} claimed={achievement.IsClaimed()} silver={silverBefore} reward={reward}");
        deadline = Time.realtimeSinceStartup + 120f;
        while (events == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Debug.Log($"Quest claim probe {(events == 1 && achievement.IsClaimed() && save.silver == silverBefore + reward ? "PASS" : "INCONCLUSIVE")}: events={events} claimed={achievement.IsClaimed()} silver={save.silver} expected={silverBefore + reward}");
        ProgressionSaveFile.A_AchievementClaimed -= OnClaim;
    }

    private void OnClaim(MyAchievement achievement)
    {
        if (achievement != null && achievement.internalName == QuestId) events++;
    }
}
#endif
