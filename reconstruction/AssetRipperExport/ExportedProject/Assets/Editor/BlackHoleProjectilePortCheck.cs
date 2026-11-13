using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class BlackHoleProjectilePortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object obj, Type type, string name, object value) => type.GetField(name, P).SetValue(obj, value);
    static object Call(object obj, string name, params object[] args) => typeof(ProjectileBlackHole).GetMethod(name, P).Invoke(obj, args);
    static void Require(bool ok, string message) { if (!ok) throw new Exception("BlackHoleProjectilePortCheck: " + message); }
    public static void Run()
    {
        int layer = LayerMask.NameToLayer("Enemy");
        var game = new GameObject("game").AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << layer;
        var manager = new GameObject("enemies").AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
        var save = new GameObject("save").AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1;
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f;
        player.inventory.playerStats = stats;
        PoolManager.Instance = new GameObject("pool").AddComponent<PoolManager>(); EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>();
        var target = new GameObject("target"); target.layer = layer; target.transform.position = Vector3.forward * 2f;
        var collider = target.AddComponent<SphereCollider>(); var enemy = target.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000f;
        Set(enemy, typeof(Enemy), "controlHp", 1000f); enemy.renderer = target.AddComponent<MeshRenderer>();
        var enemyData = ScriptableObject.CreateInstance<EnemyData>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { enemyData });
        enemy.rb = target.AddComponent<Rigidbody>(); enemy.enemyMovement = target.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enemy = enemy; enemy.enemyMovement.rb = enemy.rb;
        Set(enemy.enemyMovement, typeof(EnemyMovementRb), "nextGetSpeedTime", 100f); Set(enemy.enemyMovement, typeof(EnemyMovementRb), "storedSpeed", 5f);
        manager.collidersToEnemies.Add(collider, enemy);
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponBlackHole.asset"); Require(data != null, "original weapon data"); data.Init();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BlackHoleProjectile.prefab"); Require(prefab != null, "original projectile prefab");
        var projectile = UnityEngine.Object.Instantiate(prefab).GetComponent<ProjectileBlackHole>(); Require(projectile != null, "projectile component");
        projectile.transform.position = Vector3.zero;
        projectile.weaponBase = new WeaponBase(data); projectile.projectileRadius = 3f; Set(projectile, typeof(ProjectileBase), "expirationTime", 15f);
        MyTime.time = 10f; MyTime.deltaTime = .5f; Physics.SyncTransforms();
        Require((bool)Call(projectile, "TryInit", 0), "target acquired by original projectile");
        Require(Vector3.Distance(projectile.transform.position, player.transform.position) < .001f, "starts at player");
        var desired = (Vector3)typeof(ProjectileBlackHole).GetField("desiredPosition", P).GetValue(projectile);
        Require(Mathf.Abs(desired.z - 11.3f) < .01f, "original travel distance");
        float before = enemy.hp; Call(projectile, "MyFixedUpdate"); Require(enemy.hp < before, "area damage reaches target");
        Require(enemy.enemyMovement.state == EnemyMovementRb.State.Sucked, "area damage applies suction");
        before = enemy.hp; MyTime.time += .1f; Call(projectile, "MyFixedUpdate"); Require(enemy.hp == before, "collision cooldown");
        MyTime.time += .3f; Call(projectile, "MyFixedUpdate"); Require(enemy.hp < before, "damage resumes after cooldown");
        Call(projectile, "MyUpdate"); Require(projectile.transform.position.z > 0f && projectile.transform.position.z < desired.z, "out-quad movement");
        Call(projectile, "OnDisable"); Require(enemy.enemyMovement.state == EnemyMovementRb.State.Normal, "disable releases target");
        projectile.transform.position = Vector3.zero; Set(enemy, typeof(Enemy), "enemyFlag", EEnemyFlag.StageBoss);
        before = enemy.hp; MyTime.time += 1f; Call(projectile, "MyFixedUpdate");
        Require(enemy.hp < before && enemy.enemyMovement.state == EnemyMovementRb.State.Normal, "stage boss takes damage but is not sucked");
        enemy.hp = 0f; MyTime.time += 1f; Require(!(bool)Call(projectile, "TryInit", 0), "projectile does not launch without a living target");
        Debug.Log("BlackHoleProjectilePortCheck passed: original prefab target, damage, suction, cooldown, motion and release");
    }
}
