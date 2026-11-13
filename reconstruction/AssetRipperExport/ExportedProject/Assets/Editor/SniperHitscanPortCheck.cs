using System;
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
using UnityEngine;

public static class SniperHitscanPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool b, string s) { if (!b) throw new Exception("SniperHitscanPortCheck: " + s); }
    public static void Run()
    {
        int layer = LayerMask.NameToLayer("Enemy");
        var manager = new GameObject("game manager").AddComponent<GameManager>(); GameManager.Instance = manager; manager.whatIsEnemy = 1 << layer;
        var enemies = new GameObject("enemy manager").AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
        var save = new GameObject("save").AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1;
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; player.inventory.playerStats = stats;
        PoolManager.Instance = new GameObject("pool").AddComponent<PoolManager>(); EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>();
        Enemy[] targets = new Enemy[3];
        for (int i = 0; i < targets.Length; i++)
        {
            var go = new GameObject("target " + i); go.layer = layer; go.transform.position = Vector3.forward * (5f + 5f * i);
            var collider = go.AddComponent<SphereCollider>(); var enemy = go.AddComponent<Enemy>(); enemy.renderer = go.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
            typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
            enemies.collidersToEnemies.Add(collider, enemy); targets[i] = enemy;
        }
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponSniper.asset"); Check(data != null, "original weapon data"); data.Init(); var weapon = new WeaponBase(data);
        var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/SniperProjectile.prefab"); var attackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/SniperAttack.prefab");
        Check(projectilePrefab != null && attackPrefab != null, "original prefabs"); var projectile = UnityEngine.Object.Instantiate(projectilePrefab).GetComponent<ProjectileSniper>(); var attack = UnityEngine.Object.Instantiate(attackPrefab).GetComponent<WeaponAttack>();
        Check(projectile != null && attack != null, "prefab components"); attack.weaponBase = weapon; attack.prefabHit = null; projectile.weaponBase = weapon; projectile.attackDir = Vector3.forward;
        typeof(ProjectileBase).GetField("weaponAttack", Private).SetValue(projectile, attack); typeof(ProjectileSniper).GetField("maxDistance", Private).SetValue(projectile, 12f); projectile.transform.position = Vector3.zero;
        Physics.SyncTransforms(); projectile.Hitscan(weapon, .2f);
        Check(targets[0].hp < 1000f && targets[1].hp < 1000f && targets[2].hp == 1000f, "hits both unobstructed targets and respects distance");
        foreach (var target in targets) { target.hp = 1000f; typeof(Enemy).GetField("controlHp", Private).SetValue(target, 1000f); }
        var wall = new GameObject("wall"); wall.layer = LayerMask.NameToLayer("Ground"); wall.transform.position = new Vector3(0f, 0f, 12f); wall.AddComponent<BoxCollider>().size = new Vector3(8f, 8f, .1f); manager.whatIsGroundAndObjects = 1 << wall.layer;
        projectile.projectileRadius = .2f; projectile.transform.position = Vector3.zero; MyTime.time = 1f; Physics.SyncTransforms();
        Check((bool)typeof(ProjectileSniper).GetMethod("TryInit", Private).Invoke(projectile, new object[] { 0 }), "automatic targeting and launch");
        Check(targets[0].hp < 1000f && targets[1].hp < 1000f && targets[2].hp == 1000f, "launch damages enemies before wall only");
        Check(Mathf.Abs(projectile.transform.position.y - data.spawnOffset.y) < .001f && Vector3.Dot(projectile.attackDir, Vector3.forward) > .99f, "launch position and target direction");
        Vector3 before = projectile.transform.position; MyTime.deltaTime = .01f; typeof(ProjectileSniper).GetMethod("MyUpdate", Private).Invoke(projectile, null); Check(Vector3.Distance(before, projectile.transform.position) > 7.9f, "tracer advances with game time");
        var cameraObject = new GameObject("aim camera"); cameraObject.tag = "MainCamera"; var camera = cameraObject.AddComponent<Camera>();
        player.playerInput = player.gameObject.AddComponent<PlayerInput>(); typeof(PlayerInput).GetField("aiming", Private).SetValue(player.playerInput, true);
        Ray aimRay = camera.ScreenPointToRay(CrosshairUi.GetCrosshairRaycastPosition()); targets[0].transform.position = aimRay.GetPoint(5f); targets[1].gameObject.SetActive(false); targets[2].gameObject.SetActive(false); wall.SetActive(false); Physics.SyncTransforms();
        Vector3 aimed = (Vector3)typeof(ProjectileSniper).GetMethod("GetAttackDir", Private).Invoke(projectile, new object[] { 0 });
        Check(Vector3.Dot(aimed, (targets[0].GetFeetPosition() - player.GetFeetPosition()).normalized) > .99f, "manual aim finds enemy along crosshair ray");
        Debug.Log("SniperHitscanPortCheck passed: original projectile multi-hit, auto and manual targeting, wall limit and tracer movement");
    }
}
