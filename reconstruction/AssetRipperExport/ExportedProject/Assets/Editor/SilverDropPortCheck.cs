using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Inventory__Items__Pickups.GoldAndMoney;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using Assets.Scripts.Utility;
using UnityEngine;
using UnityEngine.Pool;

public static class SilverDropPortCheck
{
    const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
    static object Get(string n) => typeof(MoneyUtility).GetField(n, StaticPrivate).GetValue(null);
    static void Set(string n, object v) => typeof(MoneyUtility).GetField(n, StaticPrivate).SetValue(null, v);
    static void Call(string n, params object[] args) => typeof(MoneyUtility).GetMethod(n, StaticPrivate).Invoke(null, args);
    static void Require(bool v, string s) { if (!v) throw new Exception("SilverDropPortCheck: " + s); }
    public static void Run()
    {
        var oldManager = GameManager.Instance; var oldPool = PoolManager.Instance; var oldPlayer = MyPlayer.Instance;
        var oldStage = GameManager.A_StageStarted; var oldRun = GameManager.A_RunStarted; var oldDeaths = Enemy.A_EnemyDied;
        var oldNext = Get("nextSilverSpawnTime"); var oldDict = Get("flyingMoneyDict"); var oldLast = Get("lastSpawnedMoney"); var oldTime = MyTime.time;
        var oldRandom = UnityEngine.Random.state; var root = new GameObject("silver test"); root.SetActive(false);
        var silver = new GameObject("test silver"); silver.SetActive(false);
        try
        {
            GameManager.A_StageStarted = null; GameManager.A_RunStarted = null; Enemy.A_EnemyDied = null;
            var manager = root.AddComponent<GameManager>(); GameManager.Instance = manager;
            var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            stats.stats = new Dictionary<EStat, float> { [EStat.SilverIncreaseMultiplier] = 2f }; player.inventory.playerStats = stats;
            var pool = root.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            silver.AddComponent<SilverFlying>();
            pool.silverPool = new ObjectPool<GameObject>(() => silver, o => o.SetActive(true), o => o.SetActive(false));
            var enemyObj = new GameObject("test enemy"); enemyObj.transform.SetParent(root.transform); enemyObj.transform.position = new Vector3(3, 4, 5);
            var enemy = enemyObj.AddComponent<Enemy>();
            Set("nextSilverSpawnTime", 50f); MyTime.time = 50f; Call("CheckSilver", enemy);
            Require(pool.silverPool.CountAll == 0, "no silver exactly at the deadline");
            MyTime.time = 50.5f; Call("CheckSilver", enemy);
            Require(pool.silverPool.CountAll == 1 && silver.activeSelf && silver.transform.position == enemy.transform.position, "enemy death produces silver at enemy center");
            Require(Mathf.Abs((float)Get("nextSilverSpawnTime") - 80.5f) < .001f, "stat scales the next drop interval");
            pool.silverPool.Release(silver); MyTime.time = 80.5f; Call("CheckSilver", enemy);
            Require(pool.silverPool.CountInactive == 1, "deadline is strict");
            MyTime.time = 80.6f; Call("CheckSilver", enemy); Require(pool.silverPool.CountInactive == 0, "next eligible kill reuses pooled silver");
            var moneyObj = new GameObject("cached money"); moneyObj.transform.SetParent(root.transform); var money = moneyObj.AddComponent<MoneyFlying>();
            var cache = new Dictionary<GameObject, MoneyFlying> { [moneyObj] = money }; Set("flyingMoneyDict", cache); Set("lastSpawnedMoney", money);
            MoneyUtility.Init(); MoneyUtility.Init(); Require(GameManager.A_StageStarted.GetInvocationList().Length == 1, "stage handler registered once");
            GameManager.A_StageStarted(); Require(cache.Count == 0 && Get("lastSpawnedMoney") == null, "new stage clears stale money cache");
            MoneyUtility.Cleanup(); Require(GameManager.A_StageStarted == null && GameManager.A_RunStarted == null && Enemy.A_EnemyDied == null, "cleanup unregisters handlers");
            Debug.Log("SilverDropPortCheck passed");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(silver);
            GameManager.Instance = oldManager; PoolManager.Instance = oldPool; MyPlayer.Instance = oldPlayer; GameManager.A_StageStarted = oldStage; GameManager.A_RunStarted = oldRun; Enemy.A_EnemyDied = oldDeaths;
            Set("nextSilverSpawnTime", oldNext); Set("flyingMoneyDict", oldDict); Set("lastSpawnedMoney", oldLast); MyTime.time = oldTime; UnityEngine.Random.state = oldRandom;
        }
    }
}
