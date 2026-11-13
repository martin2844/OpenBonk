using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Projectiles;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class BananarangProjectilePortCheck
{
    const BindingFlags P = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object o, Type type, string field, object value) => type.GetField(field, P).SetValue(o, value);
    static T Get<T>(object o, string field) => (T)typeof(ProjectileBanana).GetField(field, P).GetValue(o);
    static object Call(object o, string method, params object[] args) => typeof(ProjectileBanana).GetMethod(method, P).Invoke(o, args);
    static void Require(bool ok, string message) { if (!ok) throw new Exception("BananarangProjectilePortCheck: " + message); }
    public static void Run()
    {
        int layer = LayerMask.NameToLayer("Enemy");
        var manager = new GameObject("manager").AddComponent<GameManager>(); GameManager.Instance = manager; manager.whatIsEnemy = 1 << layer;
        var enemies = new GameObject("enemy manager").AddComponent<EnemyManager>(); EnemyManager.Instance = enemies;
        var save = new GameObject("save").AddComponent<SaveManager>(); save.Init(); save.config = new ConfigSaveFile(); save.config.cfGameSettings.enemy_targeting_mode = 1;
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float>();
        foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 1f;
        stats.stats[EStat.CritChance] = 0f; stats.stats[EStat.KnockbackMultiplier] = 0f; stats.stats[EStat.Projectiles] = 0f; stats.stats[EStat.ProjectileBounces] = 0f;
        player.inventory.playerStats = stats;
        PoolManager.Instance = new GameObject("pool").AddComponent<PoolManager>(); EffectManager.Instance = new GameObject("effects").AddComponent<EffectManager>();
        var target = new GameObject("target"); target.layer = layer; target.transform.position = Vector3.forward * 0.3f;
        var collider = target.AddComponent<SphereCollider>(); var enemy = target.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000f;
        Set(enemy, typeof(Enemy), "controlHp", 1000f);
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        enemies.collidersToEnemies.Add(collider, enemy);
        var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/MonoBehaviour/WeaponBananarang.asset"); data.Init();
        var weapon = new WeaponBase(data);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BananaProjectile.prefab"); Require(prefab != null, "original projectile prefab");
        var a = UnityEngine.Object.Instantiate(prefab).GetComponent<ProjectileBanana>(); var b = UnityEngine.Object.Instantiate(prefab).GetComponent<ProjectileBanana>();
        Require(a != null && a.rb != null, "projectile has original rigidbody");
        a.weaponBase = b.weaponBase = weapon;
        var attackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/BananaAttack.prefab");
        var attack = UnityEngine.Object.Instantiate(attackPrefab).GetComponent<WeaponAttack>();
        Require(attack != null, "original weapon attack prefab");
        attack.weaponBase = weapon; attack.prefabHit = null;
        Set(a, typeof(ProjectileBase), "weaponAttack", attack);
        Set(a, typeof(ProjectileBase), "projectileSpeed", 1f); Set(b, typeof(ProjectileBase), "projectileSpeed", 1f);
        MyTime.time = 10f; MyTime.fixedDeltaTime = .02f; Physics.SyncTransforms();
        Require((bool)Call(a, "TryInit", 0) && (bool)Call(b, "TryInit", 0), "original prefab acquires target");
        Require(Vector3.Distance(Get<Vector3>(a, "movementVelocity"), Vector3.forward * 55f) < .001f, "initial speed and direction");
        Call(a, "CheckRadiusDamage"); Call(b, "CheckRadiusDamage");
        var field = typeof(ProjectileBanana).GetField("numTimesEnemiesHitThisTick", BindingFlags.NonPublic | BindingFlags.Static);
        var pending = (Dictionary<Collider,int>)field.GetValue(null); Require(pending.TryGetValue(collider, out int count) && count == 2, "two overlapping bananas batch their hits");
        float before = enemy.hp; Call(a, "TryPopDamage"); float damage = before - enemy.hp;
        Require(damage > 0f && pending.Count == 0, "batched damage applied and queue cleared");
        MyTime.time += .01f; Call(a, "CheckRadiusDamage"); Require(pending.Count == 0, "cooldown prevents repeated hit");
        MyTime.time += .25f; Call(a, "CheckRadiusDamage"); Require(pending.TryGetValue(collider, out count) && count == 1, "hit becomes available after cooldown");
        before = enemy.hp; Call(a, "TryPopDamage"); Require(Mathf.Abs((before - enemy.hp) * 2f - damage) < .01f, "two batched hits deal twice single-hit damage");
        MyTime.time += .25f; before = enemy.hp; Call(a, "StepMovement");
        Require(Mathf.Abs((before - enemy.hp) * 2f - damage) < .01f, "movement step deals one direct hit");
        Require(Vector3.Distance(a.rb.velocity, Vector3.forward * 55f) < .001f, "movement step drives rigidbody velocity");
        before = enemy.hp; Call(a, "StepMovement"); Require(enemy.hp == before, "direct collision respects cooldown");
        enemy.hp = 0f; MyTime.time += .5f; Require(!(bool)Call(a, "TryInit", 0), "no launch without a live target");
        Debug.Log("BananarangProjectilePortCheck passed: prefab targeting, launch, batched damage and cooldown");
    }
}
