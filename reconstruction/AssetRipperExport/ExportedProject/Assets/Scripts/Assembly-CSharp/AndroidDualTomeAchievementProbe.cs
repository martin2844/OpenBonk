#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using Assets.Scripts.Actors.Player;
using Assets.Scripts._Data.Tomes;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization;
using Assets.Scripts.Saves___Serialization.Progression.Unlocks;
using UnityEngine;

// Controlled disposable-save check for the real two-tome achievement and reload.
internal sealed class AndroidDualTomeAchievementProbe : MonoBehaviour
{
    private bool reload;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (activity.Call<string>("getPackageName") != "com.megabonk.nativeport.test" || !intent.Call<bool>("getBooleanExtra", "openbonk_dual_tome_probe", false)) return;
        var go = new GameObject("Dual tome achievement probe"); DontDestroyOnLoad(go);
        var probe = go.AddComponent<AndroidDualTomeAchievementProbe>();
        probe.reload = intent.Call<bool>("getBooleanExtra", "openbonk_dual_tome_reload", false);
    }
    private IEnumerator Start()
    {
        float end = Time.realtimeSinceStartup + 150f;
        while (Time.realtimeSinceStartup < end && (SaveManager.Instance == null || SaveManager.Instance.progression == null || DataManager.Instance == null)) yield return null;
        if (SaveManager.Instance == null || SaveManager.Instance.progression == null || DataManager.Instance == null) { Debug.LogError("Dual tome probe INCONCLUSIVE: save unavailable"); yield break; }
        if (reload)
        {
            bool saved = MyAchievements.IsAchievementDone("a_turboSkates");
            Debug.Log($"Dual tome reload {(saved ? "PASS" : "FAIL")}: achievement={saved}"); yield break;
        }
        while (Time.realtimeSinceStartup < end && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene)) yield return null;
        var player = MyPlayer.Instance; var achievement = DataManager.Instance.GetAchievement("a_turboSkates");
        if (player == null || player.inventory == null || achievement == null || MyAchievements.IsAchievementDone("a_turboSkates")) { Debug.LogError("Dual tome probe INCONCLUSIVE: fresh run or locked achievement unavailable"); yield break; }
        var agility = DataManager.Instance.GetTome(ETome.Agility); var cooldown = DataManager.Instance.GetTome(ETome.Cooldown);
        var inv = player.inventory.tomeInventory; int target = achievement.targetValue;
        if (agility == null || cooldown == null || target < 1 || target > agility.GetMaxLevel() || target > cooldown.GetMaxLevel() || inv.HasTome(ETome.Agility) || inv.HasTome(ETome.Cooldown)) { Debug.LogError("Dual tome probe INCONCLUSIVE: invalid tome state"); yield break; }
        for (int i = 0; i < target; i++) inv.AddTome(agility, agility.GetUpgradeOffer(ERarity.Common), ERarity.Common);
        bool afterFirst = MyAchievements.IsAchievementDone("a_turboSkates");
        for (int i = 1; i < target; i++) inv.AddTome(cooldown, cooldown.GetUpgradeOffer(ERarity.Common), ERarity.Common);
        bool beforeFinal = MyAchievements.IsAchievementDone("a_turboSkates");
        inv.AddTome(cooldown, cooldown.GetUpgradeOffer(ERarity.Common), ERarity.Common);
        bool unlocked = MyAchievements.IsAchievementDone("a_turboSkates");
        bool pass = !afterFirst && !beforeFinal && unlocked && inv.GetTomeLevel(ETome.Agility) == target && inv.GetTomeLevel(ETome.Cooldown) == target;
        SaveManager.Instance.SaveProgression();
        Debug.Log($"Dual tome probe {(pass ? "PASS" : "FAIL")}: target={target} first={afterFirst} beforeFinal={beforeFinal} unlocked={unlocked} agility={inv.GetTomeLevel(ETome.Agility)} cooldown={inv.GetTomeLevel(ETome.Cooldown)}");
    }
}
#endif
