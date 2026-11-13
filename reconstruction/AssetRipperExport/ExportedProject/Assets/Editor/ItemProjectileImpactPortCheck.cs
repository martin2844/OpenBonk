using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

public static class ItemProjectileImpactPortCheck
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("ItemProjectileImpactPortCheck: " + message); }
    static object Call(ItemProjectile p, string method, params object[] args) => typeof(ItemProjectile).GetMethod(method, P).Invoke(p, args);
    static void Set(ItemProjectile p, string field, object value) => typeof(ItemProjectile).GetField(field, P).SetValue(p, value);
    static T Get<T>(ItemProjectile p, string field) => (T)typeof(ItemProjectile).GetField(field, P).GetValue(p);
    public static void Run()
    {
        var state = new GameObject("item projectile test state"); state.SetActive(false);
        var enemyObject = new GameObject("enemy"); enemyObject.SetActive(false); enemyObject.layer = LayerMask.NameToLayer("Enemy");
        var terrain = new GameObject("ground"); terrain.layer = LayerMask.NameToLayer("Ground"); var groundCollider = terrain.AddComponent<BoxCollider>();
        var other = new GameObject("other"); var otherCollider = other.AddComponent<BoxCollider>();
        var targetColliderObject = new GameObject("live target collider"); targetColliderObject.layer = LayerMask.NameToLayer("Enemy");
        var targetCollider = targetColliderObject.AddComponent<SphereCollider>(); targetCollider.enabled = false;
        var data = ScriptableObject.CreateInstance<EnemyData>();
        ObjectPool<GameObject> projectilePool = null;
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/GhostProjectile.prefab");
            Check(prefab != null && prefab.GetComponent<ItemProjectile>() != null && prefab.GetComponent<ItemProjectile>().ignoreGroundCollision, "original Ghost prefab and ground setting");
            var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inventory; inventory.itemInventory = new ItemInventory();
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat, float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) stats.stats[stat] = 0f;
            stats.stats[EStat.DamageMultiplier] = 1f; inventory.playerStats = stats;
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game;
            var manager = state.AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
            var enemy = enemyObject.AddComponent<Enemy>(); var enemyCollider = enemyObject.AddComponent<BoxCollider>();
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
            enemy.hp = 100f; typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 100f); manager.collidersToEnemies.Add(enemyCollider, enemy);
            global::Utility.MyRandom.random = new System.Random(7); EffectManager.Instance = null; PoolManager.Instance = null;
            projectilePool = new ObjectPool<GameObject>(() => UnityEngine.Object.Instantiate(prefab), o => o.SetActive(true), o => o.SetActive(false), o => UnityEngine.Object.DestroyImmediate(o));
            var projectile = projectilePool.Get().GetComponent<ItemProjectile>(); projectile.projectilePool = projectilePool;
            Set(projectile, "damage", 10f); Set(projectile, "procCoefficient", 0f); Set(projectile, "damageSource", "Ghost"); Set(projectile, "currentDir", Vector3.forward);
            projectile.AddDamage(5f);
            Check(!(bool)Call(projectile, "CheckCollision", groundCollider, Vector3.up), "ghost ignores terrain");
            Check(!(bool)Call(projectile, "CheckCollision", otherCollider, Vector3.up), "unrelated layer ignored");
            Check((bool)Call(projectile, "CheckCollision", enemyCollider, Vector3.up), "enemy hit accepted");
            Check(Mathf.Abs(enemy.hp - 85f) < .001f && projectilePool.CountInactive == 1, "scaled damage and pool return");
            Call(projectile, "ProjectileDone"); Check(projectilePool.CountInactive == 1, "inactive projectile does not return twice");
            projectile = projectilePool.Get().GetComponent<ItemProjectile>(); projectile.ignoreGroundCollision = false;
            Check((bool)Call(projectile, "CheckCollision", groundCollider, Vector3.up) && projectilePool.CountInactive == 1, "terrain contact returns projectile");
            projectile = projectilePool.Get().GetComponent<ItemProjectile>(); MyTime.time = 20f; Set(projectile, "expirationTime", 21f); Call(projectile, "CheckTimeout");
            Check(projectile.gameObject.activeSelf, "projectile survives before expiry"); MyTime.time = 21f; Call(projectile, "CheckTimeout");
            Check(projectilePool.CountInactive == 1, "projectile returned at expiry");
            projectile = projectilePool.Get().GetComponent<ItemProjectile>();
            stats.stats[EStat.ProjectileSpeedMultiplier] = 2f; stats.stats[EStat.DurationMultiplier] = 1.5f;
            MyTime.time = 30f; MyTime.paused = false; UnityEngine.Random.InitState(11);
            projectile.Set(new Vector3(10, 2, 3), 13f, .25f, "Ghost", projectilePool, 1, 6, 4f, 20f);
            Check(projectile.transform.position == new Vector3(10, 2, 3) && Mathf.Abs(Get<float>(projectile,"finalProjectileSpeed") - projectile.projectileSpeed * 2f) < .001f, "setup position and speed scaling");
            Check(Mathf.Abs(Get<float>(projectile,"expirationTime") - 36f) < .001f && Mathf.Abs(Get<float>(projectile,"upTime") - .25f) < .001f, "duration and rise scaling");
            Vector3 before = projectile.transform.position; MyTime.paused = true; Call(projectile,"FixedUpdate");
            Check(projectile.transform.position == before, "paused projectile does not move");
            MyTime.paused = false; Call(projectile,"FixedUpdate");
            Check(Mathf.Abs(Vector3.Distance(before,projectile.transform.position) - projectile.projectileSpeed * 2f) < .001f, "fixed update moves by scaled speed");
            MyTime.time = 36f; Call(projectile,"FixedUpdate"); Check(projectilePool.CountInactive == 1, "fixed update expires projectile");
            targetColliderObject.transform.position = new Vector3(0, 0, 5); targetCollider.radius = .5f; targetCollider.enabled = true;
            enemyObject.transform.position = targetColliderObject.transform.position; manager.collidersToEnemies.Add(targetCollider, enemy);
            game.whatIsEnemy = 1 << targetColliderObject.layer; game.whatIsProjectileRaycast = game.whatIsEnemy; Physics.SyncTransforms();
            projectile = projectilePool.Get().GetComponent<ItemProjectile>(); MyTime.time = 40f;
            projectile.Set(Vector3.zero, 13f, 0f, "Ghost", projectilePool, 1, 6, 30f, 20f);
            for (int i = 0; i < 100 && projectile.gameObject.activeSelf; i++) { MyTime.time += .02f; Call(projectile, "FixedUpdate"); }
            Check(Mathf.Abs(enemy.hp - 72f) < .001f && projectilePool.CountInactive == 1, "automatic targeting, flight, hit and pool return");
            projectile = projectilePool.Get().GetComponent<ItemProjectile>(); MyTime.time += 1f;
            projectile.Set(targetColliderObject.transform.position, 5f, 0f, "Ghost", projectilePool, 1, 6, 30f, 20f);
            Check(Mathf.Abs(enemy.hp - 67f) < .001f && projectilePool.CountInactive == 1, "spawn overlap immediately hits enemy");
            inventory.itemInventory.Cleanup(); Debug.Log("ItemProjectileImpactPortCheck passed: Ghost prefab setup, pause, flight, targeting, spawn overlap, damage and pool returns");
        }
        finally
        {
            projectilePool?.Clear(); EnemyManager.Instance = null; GameManager.Instance = null; MyPlayer.Instance = null;
            UnityEngine.Object.DestroyImmediate(state); UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(terrain); UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(targetColliderObject); UnityEngine.Object.DestroyImmediate(data);
        }
    }
}
