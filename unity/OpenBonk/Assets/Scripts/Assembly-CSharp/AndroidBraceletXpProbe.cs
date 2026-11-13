#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Reflection;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Inventory__Items__Pickups.Xp_and_Levels;
using UnityEngine;

// Disposable opt-in run. Forces items, XP and level for a controlled pickup check.
internal sealed class AndroidBraceletXpProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_bracelet_xp_probe", false)) return;
        var root = new GameObject("bracelet XP probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidBraceletXpProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PickupManager.Instance == null || !PlayerStats.HasStats())) yield return null;
        var player = MyPlayer.Instance;
        if (player == null || player.inventory == null || EnemyManager.Instance == null || PickupManager.Instance == null) { Debug.LogError("Bracelet XP probe INCONCLUSIVE: game unavailable"); yield break; }
        EnemyManager.Instance.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, true);
        var inventory = player.inventory; var xp = inventory.playerXp;
        inventory.itemInventory.AddItem(EItem.TimeBracelet, 2);
        deadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < deadline && Mathf.Abs(PlayerStats.GetStat(EStat.XpIncreaseMultiplier) - 1.16f) > .001f) yield return null;
        xp.xp = 0; xp.level = 9999;
        typeof(PlayerXp).GetField("leftOverXp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(xp, 0f);
        int pickups = 0; void OnPickup(Pickup p) { if (p != null && p.ePickup == EPickup.Xp) pickups++; }
        Pickup.A_PickupTriggered += OnPickup;
        float boosted = PlayerStats.GetStat(EStat.XpIncreaseMultiplier);
        var first = PickupManager.Instance.SpawnPickup(EPickup.Xp, player.transform.position, 25, false, 0f);
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && (pickups < 1 || xp.xp < 29)) yield return null;
        int firstXp = xp.xp; bool firstReturned = first != null && !first.gameObject.activeInHierarchy;
        inventory.itemInventory.RemoveItem(EItem.TimeBracelet); inventory.itemInventory.RemoveItem(EItem.TimeBracelet);
        deadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < deadline && Mathf.Abs(PlayerStats.GetStat(EStat.XpIncreaseMultiplier) - 1f) > .001f) yield return null;
        float restored = PlayerStats.GetStat(EStat.XpIncreaseMultiplier);
        var second = PickupManager.Instance.SpawnPickup(EPickup.Xp, player.transform.position, 25, false, 0f);
        deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline && (pickups < 2 || xp.xp < firstXp + 25)) yield return null;
        bool pass = Mathf.Abs(boosted - 1.16f) < .001f && Mathf.Abs(restored - 1f) < .001f && pickups == 2 && firstXp == 29 && xp.xp - firstXp == 25 && firstReturned && second != null && !second.gameObject.activeInHierarchy;
        Debug.Log($"Bracelet XP probe {(pass ? "PASS" : "FAIL")}: multiplier={boosted:F3}->{restored:F3}, xpFirst={firstXp}, xpSecond={xp.xp-firstXp}, pickups={pickups}, pooledFirst={firstReturned}, pooledSecond={(second != null && !second.gameObject.activeInHierarchy)}");
        if (pass)
        {
            int levelEvents = 0, screenEvents = 0; void OnLevel(int level) { if (level == 1) levelEvents++; } void OnScreen() { screenEvents++; }
            PlayerXp.A_LevelUp += OnLevel; LevelupScreen.A_LevelupEnabled += OnScreen;
            xp.xp = 6; xp.level = 0; typeof(PlayerXp).GetField("leftOverXp", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(xp, 0f);
            var third = PickupManager.Instance.SpawnPickup(EPickup.Xp, player.transform.position, 1, false, 0f);
            deadline = Time.realtimeSinceStartup + 30f; LevelupScreen screen = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                screen = FindObjectOfType<LevelupScreen>();
                if (levelEvents > 0 && screenEvents > 0 && screen != null && screen.window != null && screen.window.activeInHierarchy) break;
                yield return null;
            }
            bool visible = screen != null && screen.window != null && screen.window.activeInHierarchy;
            int offers = screen != null && screen.upgradePicker != null ? screen.upgradePicker.GetNumUpgrades() : 0;
            bool levelPass = xp.xp == 7 && xp.level == 1 && pickups == 3 && levelEvents == 1 && screenEvents == 1 && visible && offers > 0 && third != null && !third.gameObject.activeInHierarchy;
            Debug.Log($"XP pickup level-up probe {(levelPass ? "PASS" : "FAIL")}: xp={xp.xp}, level={xp.level}, pickups={pickups}, levelEvents={levelEvents}, screenEvents={screenEvents}, visible={visible}, offers={offers}, pooled={(third != null && !third.gameObject.activeInHierarchy)}");
            PlayerXp.A_LevelUp -= OnLevel; LevelupScreen.A_LevelupEnabled -= OnScreen;
        }
        Pickup.A_PickupTriggered -= OnPickup;
    }
}
#endif
