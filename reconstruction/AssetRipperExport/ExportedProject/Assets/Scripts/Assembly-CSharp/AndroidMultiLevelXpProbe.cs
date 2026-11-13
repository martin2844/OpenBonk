#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Managers;
using Assets.Scripts.Utility;
using Inventory__Items__Pickups;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

// Controlled multi-level XP pickup in a disposable test run.
internal sealed class AndroidMultiLevelXpProbe : MonoBehaviour
{
    static bool externalControls;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_multi_level_xp_probe", false)) return;
        externalControls = intent.Call<bool>("getBooleanExtra", "megabonk_multi_level_xp_external", false);
        var root = new GameObject("Multi-level XP probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidMultiLevelXpProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PickupManager.Instance == null || UiManager.Instance == null || UiManager.Instance.encounterWindows == null || SaveManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || UiManager.Instance == null || UiManager.Instance.encounterWindows == null) { Debug.LogError("Multi-level XP probe INCONCLUSIVE: game unavailable"); yield break; }
        var inventory = player.inventory; var xp = inventory.playerXp; var encounters = UiManager.Instance.encounterWindows;
        if (xp.xp != 0 || xp.level != 0 || encounters.HasEncounter()) { Debug.LogError("Multi-level XP probe INCONCLUSIVE: nonempty initial XP or encounter"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        typeof(MyPlayer).GetField("damageCooldownOverAtTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, float.MaxValue);
        SaveManager.Instance.config.cfGameSettings.auto_select_upgrades = 0;
        int levelEvents = 0, lastLevel = 0, opened = 0, closed = 0, picked = 0;
        void OnLevel(int level) { levelEvents++; lastLevel = level; }
        void OnOpen() { opened++; }
        void OnClose() { closed++; }
        void OnPickup(Pickup p) { if (p != null && p.ePickup == EPickup.Xp) picked++; }
        PlayerXp.A_LevelUp += OnLevel; LevelupScreen.A_LevelupEnabled += OnOpen; LevelupScreen.A_LevelUpClose += OnClose; Pickup.A_PickupTriggered += OnPickup;
        int amount = XpUtility.LevelToXp(3);
        var pickup = PickupManager.Instance.SpawnPickup(EPickup.Xp, player.transform.position, amount, false, 0f);
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && (picked < 1 || opened < 1)) yield return null;
        Debug.Log($"Multi-level XP probe START: amount={amount}, xp={xp.xp}, level={xp.level}, levelEvents={levelEvents}, lastLevel={lastLevel}, opened={opened}, pickups={picked}");
        int selected = 0;
        for (int i = 0; i < 3; i++)
        {
            deadline = Time.realtimeSinceStartup + 15f;
            LevelupScreen screen = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                screen = FindObjectOfType<LevelupScreen>();
                if (opened > i && screen != null && screen.window != null && screen.window.activeInHierarchy && screen.upgradePicker != null && screen.upgradePicker.GetNumUpgrades() > 0) break;
                yield return null;
            }
            if (screen == null || opened <= i || screen.upgradePicker == null) break;
            // Closing the preceding screen applies a 0.5-second banish-selection
            // cooldown after it has already opened the next queued screen.
            deadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < deadline && Time.time <= screen.upgradePicker.banishCooldownOverAtTime) yield return null;
            if (externalControls)
            {
                Debug.Log($"Multi-level XP probe CONTROL READY: screen={i + 1}, opened={opened}, closed={closed}");
                deadline = Time.realtimeSinceStartup + 25f;
                while (Time.realtimeSinceStartup < deadline && closed <= i) yield return null;
                if (closed > i) selected++; else break;
                yield return null;
                continue;
            }
            UpgradeButton button = null;
            foreach (var candidate in screen.upgradePicker.buttons) if (candidate != null && candidate.gameObject.activeInHierarchy) { button = candidate; break; }
            if (button == null) break;
            int beforeClosed = closed;
            button.SelectUpgrade();
            if (closed > beforeClosed) selected++;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(1f);
        bool pass = pickup != null && !pickup.gameObject.activeInHierarchy && xp.xp == amount && xp.level == 3 && levelEvents == 1 && lastLevel == 3 && picked == 1 && opened == 3 && closed == 3 && selected == 3 && !encounters.HasEncounter() && !MyTime.paused;
        Debug.Log($"Multi-level XP probe {(pass ? "PASS" : "FAIL")}: xp={xp.xp}, level={xp.level}, levelEvents={levelEvents}, lastLevel={lastLevel}, pickups={picked}, opened={opened}, selected={selected}, closed={closed}, pending={encounters.HasEncounter()}, paused={MyTime.paused}");
        PlayerXp.A_LevelUp -= OnLevel; LevelupScreen.A_LevelupEnabled -= OnOpen; LevelupScreen.A_LevelUpClose -= OnClose; Pickup.A_PickupTriggered -= OnPickup;
    }
}
#endif
