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

public static class ShotgunPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool b, string s) { if (!b) throw new Exception("ShotgunPortCheck: " + s); }
    public static void Run()
    {
        int layer = LayerMask.NameToLayer("Enemy"); var gm = new GameObject("manager").AddComponent<GameManager>(); GameManager.Instance = gm; gm.whatIsEnemy = 1 << layer;
        var enemies = new GameObject("enemies").AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
        var save = new GameObject("save").AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1; save.config.cfVisualsSettings.damage_numbers = 0;
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>(); foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; player.inventory.playerStats = stats;
        PoolManager.Instance = new GameObject("pool").AddComponent<PoolManager>(); EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>();
        var positions = new[] { new Vector3(0f, .6f, 3f), new Vector3(0f, .6f, 8f), new Vector3(8f, .6f, 3f), new Vector3(0f, .6f, 14f) }; var targets = new Enemy[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            var go = new GameObject("enemy " + i); go.layer = layer; go.transform.position = positions[i]; var collider = go.AddComponent<SphereCollider>(); var enemy = go.AddComponent<Enemy>(); enemy.renderer = go.AddComponent<MeshRenderer>(); enemy.hp = enemy.maxHp = 1000f;
            typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 1000f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() }); enemies.collidersToEnemies.Add(collider, enemy); targets[i] = enemy;
        }
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponShotgun.asset"); Check(data != null, "original weapon"); data.Init(); var weapon = new WeaponBase(data);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ShotgunProjectile.prefab"); var attackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ShotgunAttack.prefab");
        var projectile = UnityEngine.Object.Instantiate(prefab).GetComponent<ProjectileShotgun>(); var attack = UnityEngine.Object.Instantiate(attackPrefab).GetComponent<WeaponAttack>(); Check(projectile != null && attack != null && projectile.psBullets != null, "original prefab bindings");
        attack.weaponBase = weapon; attack.prefabHit = null; projectile.weaponBase = weapon; projectile.projectileRadius = 10f; projectile.transform.position = Vector3.zero; typeof(ProjectileBase).GetField("weaponAttack", Private).SetValue(projectile, attack);
        Physics.SyncTransforms(); var spheres = global::Utility.RaycastUtility.GetConecastPositions(Vector3.zero, Vector3.forward, 10f, 35f); Check(spheres.Count > 2 && spheres[0].pos == Vector3.zero && Mathf.Approximately(spheres[0].radius, 1f) && Mathf.Abs(spheres[spheres.Count-1].radius - Mathf.Tan(35f * Mathf.Deg2Rad) * 10f) < .001f, "cone sphere endpoints");
        MyTime.time = 1f; Check((bool)typeof(ProjectileShotgun).GetMethod("TryInit", Private).Invoke(projectile, new object[] { 0 }), "initialization");
        Check(targets[0].hp < 1000f && targets[1].hp < 1000f && targets[2].hp == 1000f && targets[3].hp == 1000f, $"cone and range filter hp={targets[0].hp},{targets[1].hp},{targets[2].hp},{targets[3].hp} direction={projectile.transform.forward}");
        Check(1000f-targets[0].hp > 1000f-targets[1].hp, "distance falloff");
        var burst = projectile.psBullets.emission.GetBurst(0); Check(Mathf.Approximately(burst.count.constant, 3f), "original particle count");
        Debug.Log("ShotgunPortCheck passed: original prefab, cone geometry, range, damage falloff and pellet count");
    }
}
