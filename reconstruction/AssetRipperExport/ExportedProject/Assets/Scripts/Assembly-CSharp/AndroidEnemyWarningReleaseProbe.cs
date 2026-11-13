#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemySpecialAttacks.Implementations;
using Assets.Scripts.Inventory__Items__Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Isolated warning lifecycle after the original attacker is returned to its pool.
internal sealed class AndroidEnemyWarningReleaseProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_enemy_warning_release_probe", false)) return;
        var go = new GameObject("Enemy warning release probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidEnemyWarningReleaseProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || PoolManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance; var pools = PoolManager.Instance;
        if (player == null || manager == null || pools == null) { Debug.LogError("Enemy warning release probe: scene unavailable"); yield break; }
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>(); foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var data = DataManager.Instance.GetEnemyData(EEnemy.MinibossGolem);
        var enemy = manager.SpawnEnemy(data, player.transform.position + Vector3.forward * 10f, -1, true, canBeElite: false);
        if (enemy == null) { Debug.LogError("Enemy warning release probe: spawn failed"); Restore(); yield break; }
        deadline = Time.realtimeSinceStartup + 15f;
        while (enemy != null && enemy.IsTeleporting() && Time.realtimeSinceStartup < deadline) yield return null;
        if (enemy == null || enemy.IsTeleporting()) { Debug.LogError("Enemy warning release probe: spawn timeout"); Restore(); yield break; }
        var config = data.specialAttacks.First(x => x.attackName == "StoneAttackClose");
        int warningsBefore = pools.warningSpherePool.CountActive;
        var obj = pools.GetEnemyAttack(config);
        var attack = obj != null ? obj.GetComponent<EnemySpecialAttackPrefabSingle>() : null;
        if (attack == null) { Debug.LogError("Enemy warning release probe: attack unavailable"); Restore(); yield break; }
        int hits = 0;
        void OnHit(PlayerHealth ph, DamageContainer dc, bool shield) { if (dc != null && dc.damageSource == "Enemy") hits++; }
        PlayerHealth.A_TakeDamage += OnHit;
        attack.Set(config, enemy);
        int warningAtStart = pools.warningSpherePool.CountActive;
        enemy.ReleaseToPool();
        int warningAfterRelease = pools.warningSpherePool.CountActive;
        int attackAfterRelease = pools.enemyAttacksPools[config.attackName].CountActive;
        Debug.Log($"Enemy warning release probe: before={warningsBefore}, afterStart={warningAtStart}, afterRelease={warningAfterRelease}, attacksAfterRelease={attackAfterRelease}");
        yield return new WaitForSecondsRealtime(4f);
        int warningFinal = pools.warningSpherePool.CountActive;
        Debug.Log($"Enemy warning release probe result: hits={hits}, warningsFinal={warningFinal}, attacksFinal={pools.enemyAttacksPools[config.attackName].CountActive}, hp={player.inventory.playerHealth.hp}");
        PlayerHealth.A_TakeDamage -= OnHit; Restore();
        void Restore() { if (manager != null) manager.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable(); }
    }
}
#endif
