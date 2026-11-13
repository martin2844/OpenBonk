using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Game.Combat.EnemyDebuffs.Implementations;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEngine;

public static class EnemyCrowdControlCapPortCheck
{
    const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new Exception("EnemyCrowdControlCapPortCheck: " + message); }
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance;
        var oldGame = GameManager.Instance;
        float oldTime = MyTime.time, oldSwarm = MyTime.finalSwarmTimer;
        var capTime = typeof(EnemyStats).GetField("lastFoundCcCapTime", PrivateStatic);
        var capValue = typeof(EnemyStats).GetField("lastFoundCcCap", PrivateStatic);
        var oldCapTime = capTime.GetValue(null); var oldCapValue = capValue.GetValue(null);
        var playerObject = new GameObject("cap player");
        var enemyObject = new GameObject("cap enemy");
        var gameObject = new GameObject("cap game");
        try
        {
            GameManager.Instance = gameObject.AddComponent<GameManager>();
            var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
            player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
            var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
            stats.stats = new Dictionary<EStat, float> { [EStat.DurationMultiplier] = 1f };
            player.inventory.playerStats = stats;
            var enemy = enemyObject.AddComponent<Enemy>();
            var counts = (Dictionary<EDebuff, int>)typeof(Enemy).GetField("debuffCounts", PrivateInstance).GetValue(enemy);
            var apply = typeof(Enemy).GetMethod("AddDebuffImplementation", PrivateInstance);
            var ice = new DebuffIce(); enemy.debuffs.Add(EDebuff.Freeze, ice);
            int events = 0; enemy.A_DebuffAdded += kind => { if (kind == EDebuff.Freeze) events++; };
            var input = new AddDebuffContainer(EDebuff.Freeze, new DamageContainer(1f, "fixture"), 3f, 1);
            MyTime.time = 100f; MyTime.finalSwarmTimer = 0f; capTime.SetValue(null, -1f);
            Check(EnemyStats.GetCapCC() == 15, "initial cap");
            for (int i = 0; i < 15; i++) apply.Invoke(enemy, new object[] { input });
            Check(counts[EDebuff.Freeze] == 15 && events == 15 && ice.ticksLeft == 6, "fifteen refreshes accepted");
            apply.Invoke(enemy, new object[] { input });
            Check(counts[EDebuff.Freeze] == 16 && events == 15, "sixteenth refresh rejected");
            MyTime.finalSwarmTimer = 2100f;
            Check(EnemyStats.GetCapCC() == 15, "same-time cached value");
            MyTime.time += 1f;
            Check(EnemyStats.GetCapCC() == 8, "halfway through decay");
            counts[EDebuff.Freeze] = 7;
            apply.Invoke(enemy, new object[] { input });
            Check(counts[EDebuff.Freeze] == 8 && events == 16, "refresh at reduced cap accepted");
            apply.Invoke(enemy, new object[] { input });
            Check(counts[EDebuff.Freeze] == 9 && events == 16, "refresh beyond reduced cap rejected");
            MyTime.time += 1f; MyTime.finalSwarmTimer = 3000f;
            Check(EnemyStats.GetCapCC() == 0, "cap reaches zero");
            counts[EDebuff.Freeze] = 0;
            apply.Invoke(enemy, new object[] { input });
            Check(counts[EDebuff.Freeze] == 1 && events == 16, "zero cap rejects refresh");
            // The shipped factory has no Charm construction case. Its null result still
            // reaches A_DebuffAdded in the original application routine.
            MyTime.time += 1f; MyTime.finalSwarmTimer = 0f;
            int charmEvents = 0; enemy.A_DebuffAdded += kind => { if (kind == EDebuff.Charm) charmEvents++; };
            var missing = new AddDebuffContainer(EDebuff.Charm, new DamageContainer(1f, "fixture"), 3f, 1);
            apply.Invoke(enemy, new object[] { missing });
            Check(!enemy.debuffs.ContainsKey(EDebuff.Charm) && charmEvents == 1, "null factory result still dispatches event");
            DebuffFactory.Reset();
            var createPool = typeof(DebuffFactory).GetMethod("CreatePool", PrivateStatic);
            var pool = (UnityEngine.Pool.ObjectPool<EnemyDebuff>)createPool.Invoke(null, new object[] { EDebuff.Freeze, 1 });
            Check(pool.Get() != null, "first freeze occupies small test pool");
            var pools = (Dictionary<EDebuff, UnityEngine.Pool.ObjectPool<EnemyDebuff>>)typeof(DebuffFactory).GetField("debuffPools", PrivateStatic).GetValue(null);
            pools.Add(EDebuff.Freeze, pool);
            enemy.debuffs.Remove(EDebuff.Freeze); counts[EDebuff.Freeze] = 0;
            apply.Invoke(enemy, new object[] { input });
            Check(!enemy.debuffs.ContainsKey(EDebuff.Freeze) && events == 17, "saturated freeze pool still dispatches event");
            Debug.Log("EnemyCrowdControlCapPortCheck passed: cap decay, caching, freeze refresh limits and null pool notifications");
        }
        finally
        {
            MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; MyTime.time = oldTime; MyTime.finalSwarmTimer = oldSwarm;
            capTime.SetValue(null, oldCapTime); capValue.SetValue(null, oldCapValue);
            DebuffFactory.Reset();
            UnityEngine.Object.DestroyImmediate(enemyObject); UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }
}
