#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using UnityEngine;

// Isolated diagnostic: controlled targets and collection, real nuke effect and collision.
internal sealed class AndroidNukeCombatProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_nuke_combat_probe", false)) return;
        var go = new GameObject("Nuke combat probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidNukeCombatProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 180f;
        while (Time.realtimeSinceStartup < deadline && (GameManager.Instance == null || !GameManager.Instance.IsPlaying() || GameManager.Instance.cutscene || MyPlayer.Instance == null || !MyPlayer.Instance.hasStarted || MyPlayer.Instance.isTeleporting || EnemyManager.Instance == null || PickupManager.Instance == null || DataManager.Instance == null)) yield return null;
        var player = MyPlayer.Instance; var enemies = EnemyManager.Instance;
        if (player == null || enemies == null || player.inventory == null || player.playerMovement == null || PickupManager.Instance == null) { Debug.LogError("Nuke combat probe: scene unavailable"); yield break; }
        bool waves = enemies.enabledWaves; enemies.enabledWaves = false;
        var disabled = new List<WeaponBase>();
        foreach (var weapon in player.inventory.weaponInventory.weapons.Values) if (weapon.enabled) { weapon.Disable(); disabled.Add(weapon); }
        var body = player.playerMovement.GetRb(); var constraints = body.constraints; body.constraints = RigidbodyConstraints.FreezeAll;
        var goblin = enemies.SpawnEnemy(DataManager.Instance.GetEnemyData(EEnemy.Goblin), player.transform.position + Vector3.right * 5f, -1, true, canBeElite: false);
        var boss = enemies.SpawnBoss(EEnemy.Ent1, -1, EEnemyFlag.Boss | EEnemyFlag.StageBoss, player.transform.position + Vector3.left * 7f);
        deadline = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < deadline && (goblin == null || boss == null || !goblin.CanTakeDamage() || !boss.CanTakeDamage())) yield return null;
        if (goblin == null || boss == null || !goblin.CanTakeDamage() || !boss.CanTakeDamage()) { Debug.LogError("Nuke combat probe: targets unavailable"); Restore(); yield break; }
        goblin.enemyMovement.StopMovement(); boss.enemyMovement.StopMovement();
        float before = boss.hp, max = boss.maxHp; bool killed = false;
        void OnDeath(Enemy enemy, Assets.Scripts.Actors.DamageContainer dc) { if (enemy == goblin) killed = true; }
        Enemy.A_EnemyDied += OnDeath;
        var pickup = PickupManager.Instance.SpawnPickup(EPickup.Nuke, player.transform.position, 1, false);
        if (pickup == null) { Debug.LogError("Nuke combat probe: pickup unavailable"); Enemy.A_EnemyDied -= OnDeath; Restore(); yield break; }
        Physics.SyncTransforms(); typeof(Pickup).GetMethod("ApplyPickup", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pickup, null);
        deadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < deadline && (!killed || boss.hp >= before)) yield return null;
        float damage = before - boss.hp;
        bool pass = killed && Mathf.Abs(damage - max * .15f) < Mathf.Max(1f, max * .001f);
        Debug.Log($"Nuke combat probe {(pass ? "PASS" : "FAIL")}: goblinDeath={killed}, bossBefore={before:F2}, bossAfter={boss.hp:F2}, bossMax={max:F2}, damage={damage:F2}");
        Enemy.A_EnemyDied -= OnDeath; Restore();
        void Restore() { body.constraints = constraints; enemies.enabledWaves = waves; foreach (var weapon in disabled) if (weapon != null) weapon.Enable(); }
    }
}
#endif
