using System;
using System.Reflection;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Managers;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;
using UnityEngine.Pool;

public static class RocketImpactPortCheck
{
    const BindingFlags P = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("RocketImpactPortCheck: " + message); }
    static object Call(Rocket rocket, string method, params object[] args) => typeof(Rocket).GetMethod(method, P).Invoke(rocket, args);
    static void Set(Rocket rocket, string field, object value) => typeof(Rocket).GetField(field, P).SetValue(rocket, value);
    public static void Run()
    {
        var state = new GameObject("rocket impact test state"); state.SetActive(false);
        var enemyObject = new GameObject("enemy"); enemyObject.SetActive(false); enemyObject.layer = LayerMask.NameToLayer("Enemy");
        var terrain = new GameObject("ground"); terrain.layer = LayerMask.NameToLayer("Ground"); var groundCollider = terrain.AddComponent<BoxCollider>();
        var other = new GameObject("other"); var otherCollider = other.AddComponent<BoxCollider>();
        var data = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            var manager = state.AddComponent<EnemyManager>(); EnemyManager.Instance = manager;
            var enemy = enemyObject.AddComponent<Enemy>(); var enemyCollider = enemyObject.AddComponent<BoxCollider>();
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
            enemy.hp = 100f; typeof(Enemy).GetField("controlHp", P).SetValue(enemy, 100f); manager.collidersToEnemies.Add(enemyCollider, enemy);
            var pool = state.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.rocketPool = new ObjectPool<GameObject>(() => new GameObject("rocket").AddComponent<Rocket>().gameObject, o => o.SetActive(true), o => o.SetActive(false));
            pool.explosionPool = new ObjectPool<GameObject>(() => new GameObject("explosion"), o => o.SetActive(true), o => o.SetActive(false));
            var rocket = pool.rocketPool.Get().GetComponent<Rocket>(); Set(rocket, "useGenericPool", true); Set(rocket, "damage", 17f); Set(rocket, "procCoefficient", 0f); Set(rocket, "damageSource", "test rocket");
            rocket.transform.position = new Vector3(3,4,5); rocket.transform.rotation = Quaternion.identity; int callbacks = 0; rocket.A_ProjectileDone += () => callbacks++;
            Check(!(bool)Call(rocket, "CheckCollision", otherCollider, Vector3.up) && rocket.gameObject.activeSelf, "unrelated layer ignored");
            Check((bool)Call(rocket, "CheckCollision", enemyCollider, Vector3.up), "enemy hit accepted");
            Check(Mathf.Approximately(enemy.hp,83f) && callbacks == 1 && pool.rocketPool.CountInactive == 1, "damage, callback and rocket pool release");
            var explosion = GameObject.Find("explosion"); Check(explosion && explosion.transform.position == new Vector3(3,4,6), "explosion offset at impact");
            Call(rocket, "ProjectileDone"); Check(callbacks == 1 && pool.explosionPool.CountAll == 1, "inactive rocket cannot complete twice");
            rocket = pool.rocketPool.Get().GetComponent<Rocket>(); Set(rocket, "useGenericPool", true);
            Check((bool)Call(rocket, "CheckCollision", groundCollider, Vector3.up) && pool.rocketPool.CountInactive == 1, "terrain collision releases rocket");
            rocket = pool.rocketPool.Get().GetComponent<Rocket>(); Set(rocket, "useGenericPool", true);
            Call(rocket, "HitOther", groundCollider, Vector3.up);
            Check(!rocket.gameObject.activeSelf && pool.rocketPool.CountInactive == 1 && callbacks == 3 && pool.explosionPool.CountAll == 3, "direct non-enemy impact completes once");
            Call(rocket, "HitOther", groundCollider, Vector3.up);
            Check(callbacks == 3 && pool.explosionPool.CountAll == 3, "repeated non-enemy impact does not complete again");
            Debug.Log("RocketImpactPortCheck passed: enemy damage, terrain and ignored layers, effect placement, callback and pool return");
        }
        finally { EnemyManager.Instance = null; PoolManager.Instance = null; UnityEngine.Object.DestroyImmediate(state); UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(terrain); UnityEngine.Object.DestroyImmediate(other); UnityEngine.Object.DestroyImmediate(data); foreach (var o in UnityEngine.Object.FindObjectsOfType<Rocket>()) UnityEngine.Object.DestroyImmediate(o.gameObject); var e = GameObject.Find("explosion"); if (e != null) UnityEngine.Object.DestroyImmediate(e); }
    }
}
