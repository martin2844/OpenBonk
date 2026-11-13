#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

internal sealed class AndroidPowerupDeathProbe : MonoBehaviour
{
    static bool installed;
    Pickup observed;
    int events;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (installed) return;
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_powerup_death_probe", false)) return;
        installed = true;
        var root = new GameObject("powerup death probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidPowerupDeathProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null || PickupManager.Instance == null || !PlayerStats.HasStats())) yield return null;
        var player = MyPlayer.Instance; var manager = PickupManager.Instance; var enemies = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || enemies == null || DataManager.Instance == null) { Debug.LogError("Powerup death probe INCONCLUSIVE: game unavailable"); yield break; }
        enemies.enabledWaves = false;
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) weapon.Disable();
        typeof(MyPlayer).GetField("isInvincible", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, true);
        player.inventory.playerHealth.hp = 10;
        player.inventory.statInventory.ChangeMovingStat("powerup death probe", new StatModifier { stat = EStat.PowerupChance, modifyType = EStatModifyType.Flat, modification = 1000f });
        deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && PlayerStats.GetStat(EStat.PowerupChance) < 100f) yield return null;
        var last = typeof(PickupManager).GetField("lastPowerupAtTime", BindingFlags.NonPublic | BindingFlags.Instance);
        var count = typeof(PickupManager).GetField("numPowerupsOnMap", BindingFlags.NonPublic | BindingFlags.Instance);
        last.SetValue(manager, MyTime.time - 120f);
        var existing = new HashSet<Pickup>(FindObjectsOfType<Pickup>());
        var previousOrbs = new HashSet<PickupOrb>(FindObjectsOfType<PickupOrb>());
        int before = (int)count.GetValue(manager);
        var data = DataManager.Instance.GetEnemyData(EEnemy.Goblin);
        var enemy = data != null ? enemies.SpawnEnemy(data, player.transform.position + player.transform.right * 5f, -1, true, canBeElite: false) : null;
        if (enemy == null) { Debug.LogError("Powerup death probe INCONCLUSIVE: enemy missing"); yield break; }
        deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && enemy != null && !enemy.CanTakeDamage()) yield return null;
        if (enemy == null || !enemy.CanTakeDamage()) { Debug.LogError("Powerup death probe INCONCLUSIVE: enemy not damageable"); yield break; }
        enemy.Kill("powerup_death_probe");
        int inFlight = (int)count.GetValue(manager);
        PickupOrb orb = null;
        foreach (var candidate in FindObjectsOfType<PickupOrb>()) if (!previousOrbs.Contains(candidate)) { orb = candidate; break; }
        var type = orb != null ? orb.ePickup : EPickup.Xp;
        deadline = Time.realtimeSinceStartup + 25f;
        while (Time.realtimeSinceStartup < deadline && observed == null)
        {
            foreach (var pickup in FindObjectsOfType<Pickup>()) if (!existing.Contains(pickup) && pickup.ePickup == type && type != EPickup.Xp) { observed = pickup; break; }
            if (observed == null) yield return null;
        }
        int landed = (int)count.GetValue(manager);
        if (observed == null) { Debug.LogError($"Powerup death probe INCONCLUSIVE: orb={orb != null}, before={before}, inFlight={inFlight}, landed={landed}"); yield break; }
        Pickup.A_PickupTriggered += OnPickup;
        // Place the player at the drop; physics and pickup-following remain active.
        player.transform.position = observed.transform.position;
        var body = player.GetComponent<Rigidbody>();
        if (body != null) { body.position = observed.transform.position; body.velocity = Vector3.zero; }
        Physics.SyncTransforms();
        deadline = Time.realtimeSinceStartup + 35f;
        while (Time.realtimeSinceStartup < deadline && events == 0) yield return null;
        yield return null;
        int after = (int)count.GetValue(manager);
        bool pass = inFlight == before + 1 && landed == before + 1 && events == 1 && after == before;
        Debug.Log($"Powerup death probe {(pass ? "PASS" : "INCONCLUSIVE")}: type={type}, before={before}, inFlight={inFlight}, landed={landed}, events={events}, after={after}");
        Pickup.A_PickupTriggered -= OnPickup;
    }
    void OnPickup(Pickup pickup) { if (pickup == observed) events++; }
    void OnDestroy() { Pickup.A_PickupTriggered -= OnPickup; }
}
#endif
