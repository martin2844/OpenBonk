#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.UI.InGame.Rewards;
using UnityEngine;

// Opt-in emulator test: real chest UI with controlled rewards, skip from external touch.
internal sealed class AndroidChestRaritySkipProbe : MonoBehaviour
{
    static bool manualTake;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_chest_rarity_skip_probe", false)) return;
        manualTake = intent.Call<bool>("getBooleanExtra", "megabonk_chest_manual_take_probe", false);
        var root = new GameObject("chest rarity skip probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidChestRaritySkipProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || MyPlayer.Instance.inventory == null || DataManager.Instance == null || RunUnlockables.availableItems == null || UiManager.Instance == null || UiManager.Instance.encounterWindows == null || UiManager.Instance.encounterWindows.HasEncounter())) yield return null;
        var player = MyPlayer.Instance; var encounter = UiManager.Instance != null ? UiManager.Instance.encounterWindows : null;
        var chest = encounter != null ? encounter.chestWindow as ChestWindowUi : null;
        if (player == null || chest == null) { Debug.LogError("Chest rarity probe: scene unavailable"); yield break; }
        if (EnemyManager.Instance != null) EnemyManager.Instance.enabledWaves = false;
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        var skipped = typeof(ChestOpening).GetField("skipped", BindingFlags.Instance | BindingFlags.NonPublic);
        int checkedCount = 0, failures = 0;
        foreach (EItemRarity rarity in new[] { EItemRarity.Common, EItemRarity.Rare, EItemRarity.Epic, EItemRarity.Legendary })
        {
            ItemData item = DataManager.Instance.unsortedItems.Find(x => x != null && x.isEnabled && x.rarity == rarity);
            if (item == null) { failures++; Debug.LogError("Chest rarity probe: missing item for " + rarity); continue; }
            var pool = RunUnlockables.availableItems[rarity]; bool inserted = !pool.Contains(item); if (inserted) pool.Add(item);
            int before = player.inventory.itemInventory.GetAmount(item.eItem);
            encounter.AddEncounter(EEncounter.ChestNormal);
            yield return new WaitForSecondsRealtime(.6f);
            chest.itemData = item; chest.b_open.gameObject.SetActive(false);
            float started = Time.realtimeSinceStartup;
            chest.chestOpening.OpenChest(item);
            Debug.Log($"Chest rarity probe waiting: rarity={rarity}, item={item.eItem}");
            deadline = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < deadline && !chest.b_take.gameObject.activeInHierarchy) yield return null;
            bool didSkip = (bool)skipped.GetValue(chest.chestOpening);
            bool revealed = chest.b_take.gameObject.activeInHierarchy;
            float elapsed = Time.realtimeSinceStartup - started;
            if (revealed && manualTake)
            {
                Debug.Log($"Chest manual take probe READY: item={item.eItem}, skipped={didSkip}");
                deadline = Time.realtimeSinceStartup + 30f;
                while (Time.realtimeSinceStartup < deadline && player.inventory.itemInventory.GetAmount(item.eItem) == before) yield return null;
            }
            else if (revealed) chest.TakeButton();
            int awarded = player.inventory.itemInventory.GetAmount(item.eItem) - before;
            bool pass = didSkip && revealed && awarded == 1 && (!manualTake || !chest.gameObject.activeInHierarchy);
            if (!pass) failures++; else checkedCount++;
            Debug.Log($"Chest rarity probe {(pass ? "PASS" : "FAIL")}: rarity={rarity}, skipped={didSkip}, revealed={revealed}, awarded={awarded}, elapsed={elapsed:F2}");
            if (inserted) pool.Remove(item);
            if (manualTake) yield break;
            if (!revealed) yield break;
            yield return new WaitForSecondsRealtime(.6f);
        }
        Debug.Log($"Chest rarity probe completed: checked={checkedCount}, failures={failures}");
    }
}
#endif
