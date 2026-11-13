#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;

// Opt-in emulator probe; the player and target are controlled, but damage and stats use game code.
internal sealed class AndroidBloodMagicPassiveProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_blood_passive_probe", false)) return;
        var go = new GameObject("Blood Magic passive probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidBloodMagicPassiveProbe>();
    }

    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || MyPlayer.Instance.inventory == null || GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || EnemyManager.Instance == null || DataManager.Instance == null || PoolManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var manager = EnemyManager.Instance;
        if (player == null || manager == null || player.inventory == null || player.playerMovement == null) { Debug.LogError("Blood passive probe: scene unavailable"); yield break; }
        bool waves = manager.enabledWaves; manager.enabledWaves = false;
        var disabled = new List<WeaponBase>();
        foreach (var existing in player.inventory.weaponInventory.weapons.Values) if (existing.enabled) { existing.Disable(); disabled.Add(existing); }
        var body = player.playerMovement.GetRb(); var constraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
        var data = DataManager.Instance.GetWeapon(EWeapon.BloodMagic);
        var weapon = new WeaponBase(data);
        var passive = typeof(WeaponBase).GetField("passive", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon);
        passive.GetType().GetField("stackChance", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(passive, 1f);
        var target = manager.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + player.playerRenderer.transform.forward * 3f, -1, true, canBeElite: false);
        deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && target != null && !target.CanTakeDamage()) yield return null;
        if (target == null || !target.CanTakeDamage()) { Debug.LogError("Blood passive probe: target unavailable"); Restore(); yield break; }
        target.enemyMovement.StopMovement(); target.SetDiagnosticHealth(1f);
        int oldMax = player.inventory.playerHealth.maxHp;
        bool targetDeath = false;
        void OnDeath(Enemy enemy, DamageContainer dc) { if (enemy == target && dc != null && dc.damageSource == EWeapon.BloodMagic.ToString()) targetDeath = true; }
        Enemy.A_EnemyDied += OnDeath;
        Physics.SyncTransforms(); WeaponUtility.WeaponAttack(weapon);
        deadline = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < deadline && (!targetDeath || player.inventory.playerHealth.maxHp <= oldMax)) yield return null;
        int newMax = player.inventory.playerHealth.maxHp;
        bool hasModifier = player.inventory.statInventory.movingStats.TryGetValue(EWeapon.BloodMagic.ToString(), out var modifier);
        bool pass = targetDeath && hasModifier && modifier.stat == EStat.MaxHealth && newMax == oldMax + 1;
        Debug.Log($"Blood passive probe {(pass ? "PASS" : "FAIL")}: targetDeath={targetDeath}, modifier={(hasModifier ? modifier.modification : 0f)}, maxHp={oldMax}->{newMax}");
        Enemy.A_EnemyDied -= OnDeath; Restore();
        void Restore() { weapon.Cleanup(); body.constraints = constraints; manager.enabledWaves = waves; foreach (var existing in disabled) if (existing != null) existing.Enable(); }
    }
}
#endif
