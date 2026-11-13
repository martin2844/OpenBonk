using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class ChunkersPortCheck
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, P).Invoke(o, args);
    static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, P).GetValue(o);
    static void Require(bool value, string message) { if (!value) throw new Exception("ChunkersPortCheck: " + message); }
    public static void Run()
    {
        var game = new GameObject("game").AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << LayerMask.NameToLayer("Enemy");
        var manager = new GameObject("enemies").AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
        PoolManager.Instance = new GameObject("pool").AddComponent<PoolManager>(); EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>();
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f; player.inventory.playerStats = stats;
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponChunkers.asset"); data.Init();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/ChunkersAttack.prefab");
        var attack = UnityEngine.Object.Instantiate(prefab).GetComponent<ChunkersAttack>(); attack.Set(new WeaponBase(data));
        var rotating = attack.rotatingProjectiles;
        Require(Field<int>(rotating, "amount") == 3 && Mathf.Abs(Field<float>(rotating, "rotationSpeed") - 75f) < .001f, "original weapon configures quantity and speed");
        MyTime.time = 10f; attack.StartAttack(); Require(Field<bool>(rotating, "isActive") && Mathf.Abs(Field<float>(attack, "stopTime") - 13f) < .001f, "attack starts and schedules stop");
        var target = new GameObject("enemy"); target.layer = LayerMask.NameToLayer("Enemy"); target.transform.position = Vector3.forward * 3f;
        var collider = target.AddComponent<CapsuleCollider>(); var enemy = target.AddComponent<Enemy>(); enemy.collider = collider; enemy.hp = enemy.maxHp = 1000f;
        typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 1000f); enemy.renderer = target.AddComponent<MeshRenderer>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemy.rb = target.AddComponent<Rigidbody>(); enemy.enemyMovement = target.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enemy = enemy; enemy.enemyMovement.rb = enemy.rb; manager.collidersToEnemies.Add(collider, enemy);
        Require((bool)Call(rotating, "HitEnemy", 0, collider) && enemy.hp < 1000f, "rock damages living enemy"); float after = enemy.hp;
        MyTime.time = 10.1f; Require(!(bool)Call(rotating, "HitEnemy", 0, collider) && enemy.hp == after, "same rock respects cooldown");
        Require((bool)Call(rotating, "HitEnemy", 1, collider) && enemy.hp < after, "different rocks have independent cooldowns");
        MyTime.time = 11f; MyTime.deltaTime = .25f; Call(rotating, "Update");
        var rocks = Field<List<ParticleSystem>>(rotating, "prefabs");
        Require(Vector3.Distance(rocks[0].transform.localPosition, rocks[1].transform.localPosition) > 1f, "rocks spread around player");
        for (int i = 0; i < rocks.Count; i++) rocks[i].transform.position = i == 0 ? target.transform.position : Vector3.one * 100f;
        Physics.SyncTransforms(); after = enemy.hp; Call(rotating, "FixedUpdate"); Require(enemy.hp < after, "physics overlap delivers rock damage");
        MyTime.time = 13f; Call(attack, "FixedUpdate"); Require(!Field<bool>(rotating, "isActive"), "attack stops at duration boundary");
        MyTime.time = 14.5f; Call(attack, "FixedUpdate"); Require(Field<bool>(rotating, "isActive"), "attack restarts after cooldown");
        Debug.Log("ChunkersPortCheck passed: original asset quantity, timing and independent rock hit cooldowns");
    }
}
