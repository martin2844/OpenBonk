using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BananarangFlightPlayCheck
{
    const string Pending = "BananarangFlightPlayCheck.pending";
    static BananarangFlightPlayCheck() { EditorApplication.playModeStateChanged += OnState; }
    public static void Run() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true; }
    static void OnState(PlayModeStateChange state) { if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) { SessionState.SetBool(Pending, false); new GameObject("flight check runner").AddComponent<BananarangFlightRunner>().Begin(); } }
}
public class BananarangFlightRunner : MonoBehaviour
{
    ProjectileBase projectile;
    static void Require(bool ok, string why) { if (!ok) throw new Exception("BananarangFlightPlayCheck: " + why); }
    public void Begin() => StartCoroutine(Run());
    void FixedUpdate() { MyTime.fixedDeltaTime = Time.fixedDeltaTime; MyTime.time += Time.fixedDeltaTime; }
    IEnumerator Run()
    {
        Enemy enemy = null; PoolManager pool = null; WeaponData data = null;
        try
        {
            var root = new GameObject("test state"); root.SetActive(false);
            var gm = root.AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy"); gm.whatIsProjectileRaycast = gm.whatIsEnemy;
            var enemies = root.AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
            var save = root.AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
            stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f;
            player.inventory.playerStats = stats;
            player.playerMovement = root.AddComponent<PlayerMovement>(); player.playerMovement.playerCam = new GameObject("camera direction").transform;
            pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            EffectManager.Instance = root.AddComponent<EffectManager>();
            var target = new GameObject("stationary test enemy"); target.SetActive(false); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = new Vector3(0,1,4);
            var collider = target.AddComponent<SphereCollider>(); collider.radius = .6f;
            enemy = target.AddComponent<Enemy>(); enemy.enabled = false; enemy.hp = enemy.maxHp = 1000f;
            typeof(Enemy).GetField("controlHp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, 1000f);
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
            enemies.collidersToEnemies.Add(collider, enemy); target.SetActive(true);
            data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponBananarang.asset"); data.Init();
            Require(data.attack && data.attack.GetComponent<WeaponAttack>().prefabProjectile, "original attack and projectile references");
            var weapon = new WeaponBase(data);
            WeaponAttack.A_SpawnedProjectileSuccessfully += OnSpawn;
            MyTime.time = 10f; MyTime.paused = false; MyTime.fixedDeltaTime = Time.fixedDeltaTime; Physics.SyncTransforms();
            WeaponUtility.WeaponAttack(weapon);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); yield break; }
        float timeout = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < timeout && (projectile == null || enemy.hp >= 1000f || pool.projectilePools[data.eWeapon].CountInactive == 0)) yield return new WaitForFixedUpdate();
        try
        {
            Require(projectile != null, "weapon attack spawned a projectile through the pool");
            Require(enemy.hp < 1000f, "projectile flight damaged enemy through physics");
            Require(pool.projectilePools[data.eWeapon].CountInactive > 0 && !projectile.gameObject.activeSelf, "projectile returned to pool after flight");
            Debug.Log($"BananarangFlightPlayCheck passed: original weapon, attack and projectile flew, dealt {1000f - enemy.hp} damage and returned to pool");
            WeaponAttack.A_SpawnedProjectileSuccessfully -= OnSpawn; EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    void OnSpawn(ProjectileBase p) { projectile = p; }
}
