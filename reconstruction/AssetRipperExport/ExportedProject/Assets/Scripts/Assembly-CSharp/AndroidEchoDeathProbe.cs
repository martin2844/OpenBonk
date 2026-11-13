#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using Inventory__Items__Pickups.Pickups;
using UnityEngine;

// Disposable test run: force one enemy death and inspect the real pickup list.
internal sealed class AndroidEchoDeathProbe : MonoBehaviour
{
    static bool installed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (installed) return;
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_echo_death_probe", false)) return;
        installed = true;
        var root = new GameObject("echo death probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidEchoDeathProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null || PickupManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.xpPool == null)) yield return null;
        var player = MyPlayer.Instance; var enemyManager = EnemyManager.Instance; var manager = PickupManager.Instance;
        if (player == null || player.inventory == null || enemyManager == null || manager == null || DataManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.xpPool == null) { Debug.LogError("Echo death probe INCONCLUSIVE: game unavailable"); yield break; }
        var list = typeof(PickupManager).GetField("xpList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager) as PickupStackableList;
        var data = DataManager.Instance.GetEnemyData(EEnemy.Goblin);
        if (list == null || data == null) { Debug.LogError("Echo death probe INCONCLUSIVE: missing data"); yield break; }
        bool waves = enemyManager.enabledWaves; enemyManager.enabledWaves = false;
        var disabled = new List<WeaponBase>();
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        while (list.pickupsList.Count > 0) manager.PickupTriggered(list.pickupsList.First.Value);
        for (int i = 0; i < 9; i++) player.inventory.itemInventory.AddItem(EItem.EchoShard);
        var enemy = enemyManager.SpawnEnemy(data, player.transform.position + new Vector3(20f, 0f, 20f), -1, true, canBeElite: false);
        if (enemy == null) { Debug.LogError("Echo death probe INCONCLUSIVE: enemy spawn failed"); Restore(); yield break; }
        deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && enemy != null && !enemy.CanTakeDamage()) yield return null;
        if (enemy == null || !enemy.CanTakeDamage()) { Debug.LogError("Echo death probe INCONCLUSIVE: enemy not damageable"); Restore(); yield break; }
        int expected = enemy.GetXp() + 1;
        enemy.Kill("echo_death_probe");
        int total = 0;
        foreach (var pickup in list.pickupsList) total += pickup.GetValue();
        bool pass = total == expected && list.pickupsList.Count >= 1;
        Debug.Log($"Echo death probe {(pass ? "PASS" : "FAIL")}: expectedXp={expected}, actualXp={total}, pickupObjects={list.pickupsList.Count}, echoStacks=9");
        int xpBefore = player.inventory.playerXp.xp;
        int countBefore = list.pickupsList.Count;
        int echoColors = 0;
        foreach (var pickup in list.pickupsList) {
            var visual = pickup.GetComponent<XpVisuals>();
            if (visual != null && visual.ps != null && visual.ps.main.startColor.color == visual.c_echo) echoColors++;
        }
        // Position is forced, but the pickup trigger, homing and award run normally.
        var body = player.playerMovement.GetRb();
        body.position = enemy.transform.position + Vector3.up * 1f;
        body.velocity = Vector3.zero;
        Physics.SyncTransforms();
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && list.pickupsList.Count > 0) yield return null;
        // PlayerInventory applies accumulated XP in LateUpdate after collection.
        yield return null;
        yield return null;
        int gained = player.inventory.playerXp.xp - xpBefore;
        bool collected = pass && countBefore > 0 && echoColors > 0 && list.pickupsList.Count == 0 && gained == expected;
        Debug.Log($"Echo collection probe {(collected ? "PASS" : "FAIL")}: beforeCount={countBefore}, echoColors={echoColors}, remaining={list.pickupsList.Count}, expectedXp={expected}, gainedXp={gained}");
        Restore();
        void Restore() { if (enemyManager != null) enemyManager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable(); }
    }
}
#endif
