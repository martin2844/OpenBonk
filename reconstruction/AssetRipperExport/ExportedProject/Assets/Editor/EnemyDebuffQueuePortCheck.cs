using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

public static class EnemyDebuffQueuePortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("EnemyDebuffQueuePortCheck: " + message); }
    public static void Run()
    {
        var game = new GameObject("game"); GameManager.Instance = game.AddComponent<GameManager>();
        var playerObject = new GameObject("player"); var player = playerObject.AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory));
        player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew));
        stats.stats = new Dictionary<EStat, float> { [EStat.DurationMultiplier] = 1f }; player.inventory.playerStats = stats;
        var target = new GameObject("enemy"); var enemy = target.AddComponent<Enemy>();
        try {
            enemy.debuffsToRemove.Add(EDebuff.Echo);
            target.SetActive(false); enemy.DebuffTick();
            Require(enemy.debuffsToRemove.Contains(EDebuff.Echo), "inactive enemy must retain pending removals");
            target.SetActive(true); enemy.DebuffTick();
            Require(enemy.debuffsToRemove.Count == 0, "active enemy with no effects must clear stale removals");
            var effect = DebuffFactory.GetDebuff(EDebuff.Echo, enemy, new DamageContainer(0f,"test"), 1f, 1);
            Require(effect != null && effect.ticksLeft == 2, "real Echo effect setup");
            enemy.debuffs.Add(EDebuff.Echo,effect); enemy.DebuffTick();
            Require(enemy.debuffs.ContainsKey(EDebuff.Echo) && effect.ticksLeft == 1, "new effect survives its first tick");
            enemy.debuffs.Clear();
            enemy.enemyMovement = target.AddComponent<EnemyMovementRb>(); enemy.enemyMovement.enemy = enemy;
            var enemyData = ScriptableObject.CreateInstance<EnemyData>();
            typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { enemyData });
            typeof(Enemy).GetField("nextVerifyTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 10000f);
            var hit = new DamageContainer(0f, "test"); int events = 0;
            enemy.A_DebuffAdded += kind => { events++; enemy.AddDebuff(kind, hit, 3f, 1); };
            enemy.AddDebuff(EDebuff.Echo, hit, 1f, 1);
            enemy.AddDebuff(EDebuff.Echo, hit, 2f, 2);
            var queued = enemy.debuffsToAdd[EDebuff.Echo];
            Require(queued.stacks == 3 && Mathf.Abs(queued.duration-2f)<.001f, "same-tick additions combine before processing");
            enemy.MyFixedUpdate();
            Require(events == 1 && enemy.debuffs[EDebuff.Echo].ticksLeft == 4, "one combined effect applied");
            Require(enemy.debuffsToAdd.Count == 0, "callback additions cleared after the original processing pass");
            UnityEngine.Object.DestroyImmediate(enemyData);
            Debug.Log("EnemyDebuffQueuePortCheck passed: removal queue, combined application, callback ordering and later real Echo");
        } finally {
            player.inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(game);
        }
    }
}
