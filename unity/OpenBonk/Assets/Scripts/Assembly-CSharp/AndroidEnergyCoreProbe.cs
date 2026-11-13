#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Controlled acquisition and target; normal item ticking, projectile movement and collision.
internal sealed class AndroidEnergyCoreProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_energy_core_probe", false)) return;
        var go = new GameObject("Energy Core probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidEnergyCoreProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PoolManager.Instance == null || PoolManager.Instance.orbPool == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || player.inventory == null || manager == null || PoolManager.Instance == null || PoolManager.Instance.orbPool == null || DataManager.Instance == null) { Debug.LogError("Energy Core probe: scene unavailable"); yield break; }
        var pool = PoolManager.Instance.orbPool;
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var data = DataManager.Instance.GetEnemyData(EEnemy.Goblin);
        var enemy = manager.SpawnEnemy(data, player.transform.position + player.transform.forward * 6f, -1, true, canBeElite: false);
        deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && enemy != null && !enemy.CanTakeDamage()) yield return null;
        if (enemy == null || !enemy.CanTakeDamage()) { Debug.LogError("Energy Core probe: target unavailable"); Restore(); yield break; }
        enemy.SetDiagnosticHealth(10000f);
        int hits = 0, targetHits = 0, peak = 0;
        void OnDamage(Enemy target, DamageContainer dc) { if (dc == null || dc.damageSource != EItem.EnergyCore.ToString()) return; hits++; if (target == enemy) targetHits++; }
        Enemy.A_Damage += OnDamage;
        player.inventory.itemInventory.AddItem(EItem.EnergyCore);
        Debug.Log($"Energy Core probe: acquired={player.inventory.itemInventory.GetAmount(EItem.EnergyCore)}, target={data.enemyName}, active={pool.CountActive}");
        deadline = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < deadline) { peak = Mathf.Max(peak, pool.CountActive); yield return null; }
        Debug.Log($"Energy Core probe {(peak > 0 && hits > 0 ? "PASS" : "FAIL")}: peakActive={peak}, damageEvents={hits}, targetHits={targetHits}, active={pool.CountActive}, inactive={pool.CountInactive}");
        Enemy.A_Damage -= OnDamage; Restore();
        void Restore() { if (manager != null) manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable(); }
    }
}
#endif
