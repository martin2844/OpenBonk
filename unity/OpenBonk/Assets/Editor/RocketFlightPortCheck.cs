using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Pool;

public static class RocketFlightPortCheck
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("RocketFlightPortCheck: " + message); }
    static void Set(Rocket rocket, string field, object value) => typeof(Rocket).GetField(field, P).SetValue(rocket, value);
    static T Get<T>(Rocket rocket, string field) => (T)typeof(Rocket).GetField(field, P).GetValue(rocket);
    static void Call(Rocket rocket, string method) => typeof(Rocket).GetMethod(method, P).Invoke(rocket, null);
    public static void Run()
    {
        var state = new GameObject("flight state"); state.SetActive(false);
        var enemyGo = new GameObject("target"); enemyGo.SetActive(false); enemyGo.transform.position = Vector3.forward * 2;
        var hitGo = new GameObject("target collider"); hitGo.layer = LayerMask.NameToLayer("Enemy"); hitGo.transform.position = Vector3.forward * 2; var collider = hitGo.AddComponent<SphereCollider>(); collider.radius = .25f;
        var data = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            var player = state.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            var inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv;
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.ProjectileSpeedMultiplier] = 1f }; inv.playerStats = stats;
            var game = state.AddComponent<GameManager>(); GameManager.Instance = game; game.whatIsEnemy = 1 << hitGo.layer; game.whatIsProjectileRaycast = 1 << hitGo.layer;
            var manager = state.AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
            var enemy = enemyGo.AddComponent<Enemy>(); enemy.hp = 100; typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 100f); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data }); manager.collidersToEnemies.Add(collider, enemy);
            var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.rocketPool = new ObjectPool<GameObject>(() => new GameObject("flight rocket").AddComponent<Rocket>().gameObject, o => o.SetActive(true), o => o.SetActive(false));
            var rocket = pool.rocketPool.Get().GetComponent<Rocket>(); rocket.projectileRadius = .15f; MyTime.time = 1f; MyTime.paused = false; Physics.SyncTransforms();
            rocket.Set(Vector3.zero, 17f, 0f, null, true, "test");
            Check(Mathf.Approximately(Get<float>(rocket,"projectileSpeed"),.45f) && Mathf.Approximately(Get<float>(rocket,"expirationTime"),4f) && Mathf.Approximately(Get<float>(rocket,"upTime"),.5f), "speed and setup timings");
            rocket.SetTarget(enemy); Set(rocket,"startDirection",Vector3.forward); Set(rocket,"currentDir",Vector3.forward); Set(rocket,"lastDirection",Vector3.forward); MyTime.time = 1.6f;
            for (int i = 0; i < 10 && rocket.gameObject.activeSelf; i++) Call(rocket,"StepMovement");
            Check(enemy.hp == 83f && pool.rocketPool.CountInactive == 1, "real sphere cast hits enemy and returns rocket");
            collider.enabled = false; enemyGo.transform.position = Vector3.forward * 100; MyTime.time = 5f;
            rocket = pool.rocketPool.Get().GetComponent<Rocket>(); rocket.Set(Vector3.zero, 1f, 0f, null, true, "test"); rocket.SetTarget(enemy);
            MyTime.paused = true; MyTime.time = 9f; Call(rocket,"FixedUpdate"); Check(rocket.gameObject.activeSelf, "pause suppresses timeout and movement");
            MyTime.paused = false; Call(rocket,"FixedUpdate"); Check(!rocket.gameObject.activeSelf && pool.rocketPool.CountInactive == 1, "unpaused timeout returns rocket");
            Debug.Log("RocketFlightPortCheck passed: setup, steering, real spherecast damage, pause and timeout");
        }
        finally { MyTime.paused = false; MyTime.time = 0; MyPlayer.Instance = null; GameManager.Instance = null; EnemyManager.Instance = null; PoolManager.Instance = null; UnityEngine.Object.DestroyImmediate(state); UnityEngine.Object.DestroyImmediate(enemyGo); UnityEngine.Object.DestroyImmediate(hitGo); UnityEngine.Object.DestroyImmediate(data); }
    }
}
