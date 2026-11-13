using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Objects.Pooling;
using UnityEngine;
using UnityEngine.Pool;

public static class SnekPortCheck
{
    sealed class FixedRandom : System.Random { readonly double v; public FixedRandom(double value) { v = value; } public override double NextDouble() => v; }
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string reason) { if (!ok) throw new Exception("SnekPortCheck: " + reason); }
    public static void Run()
    {
        var oldPlayer = MyPlayer.Instance; var oldGame = GameManager.Instance; var oldEnemy = EnemyManager.Instance; var oldPool = PoolManager.Instance; var oldEffects = EffectManager.Instance; var oldRandom = global::Utility.MyRandom.random;
        var gameObj = new GameObject("snek game"); var playerObj = new GameObject("snek player"); var enemyObj = new GameObject("snek enemy"); var poolObj = new GameObject("snek pool"); var effect = new GameObject("snek effect"); effect.SetActive(false);
        PlayerInventory inv = null; EnemyData data = null;
        try
        {
            GameManager.Instance = gameObj.AddComponent<GameManager>(); EnemyManager.Instance = gameObj.AddComponent<EnemyManager>(); EffectManager.Instance = null;
            var player = playerObj.AddComponent<MyPlayer>(); MyPlayer.Instance = player; player.transform.position = new Vector3(0, 0, -2);
            inv = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory = inv; inv.itemInventory = new ItemInventory();
            inv.playerStats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); inv.playerStats.stats = new Dictionary<EStat, float>();
            foreach (EStat stat in Enum.GetValues(typeof(EStat))) inv.playerStats.stats[stat] = 1f;
            var enemy = enemyObj.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 1000; typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 1000f);
            data = ScriptableObject.CreateInstance<EnemyData>(); typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { data });
            enemy.collider = enemyObj.AddComponent<CapsuleCollider>();
            var pool = poolObj.AddComponent<PoolManager>(); PoolManager.Instance = pool;
            pool.snekPool = new ObjectPool<GameObject>(() => effect, obj => obj.SetActive(true), obj => obj.SetActive(false), null, true, 1, 1);
            inv.itemInventory.AddItem(EItem.Snek); var item = (ItemSnek)inv.itemInventory.GetItem(EItem.Snek);
            Require(item.HasOnHitEffectProc() && Mathf.Abs(item.damageRatio - .9f) < .001f, "item registration and stack scaling");
            var hit = new DamageContainer(1f, "fixture") { enemy = enemy, damage = 10f };
            global::Utility.MyRandom.random = new FixedRandom(.99); inv.itemInventory.PostDamage(hit);
            Require(enemy.debuffsToAdd[EDebuff.Poison].stacks == 4 && enemy.hp == 1000f, "non-burst hit queues four poison");
            var pending = enemy.debuffsToAdd[EDebuff.Poison]; enemy.debuffsToAdd.Clear();
            typeof(Enemy).GetMethod("AddDebuffImplementation", Private).Invoke(enemy, new object[] { pending });
            var poison = enemy.debuffs[EDebuff.Poison]; int ticks = poison.ticksLeft; Require(ticks > 0 && poison.GetStacks() == 4, "poison becomes active");
            global::Utility.MyRandom.random = new FixedRandom(0); inv.itemInventory.PostDamage(hit);
            Require(Mathf.Abs(enemy.hp - 991f) < .001f && enemy.debuffsToAdd[EDebuff.Poison].stacks == 4 && effect.activeSelf, "burst applies immediate damage, pending poison and effect");
            inv.itemInventory.Tick();
            Require(!enemy.HasDebuff(EDebuff.Poison) && !enemy.debuffsToAdd.ContainsKey(EDebuff.Poison), "burst consumes active and pending poison");
            Require(Mathf.Abs(enemy.hp - (991f - 8f * ticks)) < .001f, "burst converts remaining poison ticks to damage");
            float after = enemy.hp; inv.itemInventory.Tick(); Require(enemy.hp == after, "queued burst processes once");
            inv.itemInventory.RemoveItem(EItem.Snek); Require(inv.itemInventory.GetItem(EItem.Snek) == null, "last stack removes item");
            Debug.Log("SnekPortCheck passed: inventory proc, poison, burst, effect and cleanup");
        }
        finally
        {
            inv?.itemInventory?.Cleanup(); DebuffFactory.Reset(); global::Utility.MyRandom.random = oldRandom;
            UnityEngine.Object.DestroyImmediate(data); UnityEngine.Object.DestroyImmediate(effect); UnityEngine.Object.DestroyImmediate(poolObj); UnityEngine.Object.DestroyImmediate(enemyObj); UnityEngine.Object.DestroyImmediate(playerObj); UnityEngine.Object.DestroyImmediate(gameObj);
            MyPlayer.Instance = oldPlayer; GameManager.Instance = oldGame; EnemyManager.Instance = oldEnemy; PoolManager.Instance = oldPool; EffectManager.Instance = oldEffects;
        }
    }
}
