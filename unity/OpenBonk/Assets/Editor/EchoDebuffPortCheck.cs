using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Actors.Enemies;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Game.Combat.EnemyDebuffs;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Utility;
using UnityEditor;
using UnityEngine;

public static class EchoDebuffPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool ok, string message) { if (!ok) throw new Exception("EchoDebuffPortCheck: " + message); }
    public static void Run()
    {
        GameManager.Instance = new GameObject("game").AddComponent<GameManager>();
        var player = new GameObject("player").AddComponent<MyPlayer>(); MyPlayer.Instance = player;
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = new ItemInventory();
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.DurationMultiplier] = 1f }; player.inventory.playerStats = stats;
        var target = new GameObject("enemy"); var enemy = target.AddComponent<Enemy>(); enemy.hp = enemy.maxHp = 100f;
        typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 100f); enemy.renderer = target.AddComponent<MeshRenderer>();
        typeof(Enemy).GetProperty("enemyData").GetSetMethod(true).Invoke(enemy, new object[] { ScriptableObject.CreateInstance<EnemyData>() });
        OpenBonkRunMetrics.SetEnabled(true);
        var hit = new Func<float, DamageContainer>(amount => new DamageContainer(0f, "test") { enemy = enemy, damage = amount });
        enemy.MakeInvulnerable(true); enemy.DamageFromPlayerWeapon(hit(12));
        enemy.MakeInvulnerable(false); enemy.state = EEnemyState.Teleporting; enemy.DamageFromPlayerWeapon(hit(12));
        enemy.state = EEnemyState.Default;
        Require(enemy.hp == 100f && OpenBonkRunMetrics.Get(MyTime.time).DamagePerSecond == 0, "rejected hits do not count");
        typeof(Enemy).GetField("armor", Private).SetValue(enemy, .5f);
        enemy.DamageFromPlayerWeapon(hit(20));
        Require(enemy.hp == 90f && Math.Abs(OpenBonkRunMetrics.Get(MyTime.time).DamagePerSecond - 10.0 / 3) < .0001, "armor applied to metric");
        enemy.DamageFromPlayerOther(hit(10)); enemy.DamageExternal(hit(10));
        Require(enemy.hp == 80f && Math.Abs(OpenBonkRunMetrics.Get(MyTime.time).DamagePerSecond - 10.0 / 3) < .0001, "non-weapon paths excluded");
        typeof(Enemy).GetField("armor", Private).SetValue(enemy, 0f);
        enemy.hp = 100f; typeof(Enemy).GetField("controlHp", Private).SetValue(enemy, 100f); OpenBonkRunMetrics.Reset();
        var effect = DebuffFactory.GetDebuff(EDebuff.Echo, enemy, new DamageContainer(0f, "test"), 1f, 1);
        Require(effect != null && effect.GetDebuffType() == EDebuff.Echo && effect.ticksLeft == 2, "factory creates timed echo"); enemy.debuffs.Add(EDebuff.Echo, effect);
        enemy.DamageFromPlayerWeapon(new DamageContainer(0f, "test") { enemy = enemy, damage = 12f });
        enemy.DamageFromPlayerWeapon(new DamageContainer(0f, "test") { enemy = enemy, damage = 8f });
        Require(enemy.hp == 100f && OpenBonkRunMetrics.Get(MyTime.time).DamagePerSecond == 0 && (float)typeof(Enemy).GetField("echoDamage", Private).GetValue(enemy) == 20f, "active echo stores incoming damage");
        enemy.DebuffTick(); Require(enemy.HasDebuff(EDebuff.Echo), "effect persists for first tick"); enemy.DebuffTick();
        Require(!enemy.HasDebuff(EDebuff.Echo) && enemy.hp == 100f && (float)typeof(Enemy).GetField("echoDamage", Private).GetValue(enemy) == 0f, "original removal ordering re-enters echo and then clears stored damage");
        var echoField = typeof(Enemy).GetField("echoDamage", Private); echoField.SetValue(enemy, 20f);
        DamageContainer observed = null; enemy.A_DamageNonStatic += (e, dc) => observed = dc; enemy.ReleaseEcho();
        Require(enemy.hp == 80f && observed != null && observed.damageEffect == EDamageEffect.Echo && observed.damageSource == "Echo" && observed.procCoefficient == 0f && (float)echoField.GetValue(enemy) == 0f, "release after effect removal applies recorded echo damage once");
        enemy.ReleaseEcho(); Require(enemy.hp == 80f && Math.Abs(OpenBonkRunMetrics.Get(MyTime.time).DamagePerSecond - 20.0 / 3) < .0001, "echo release counts once");
        typeof(Enemy).GetField("deathFunctionCalled", Private).SetValue(enemy, true);
        OpenBonkRunMetrics.Reset(); enemy.DamageFromPlayerWeapon(hit(1000));
        Require(enemy.hp == 0f && Math.Abs(OpenBonkRunMetrics.Get(MyTime.time).DamagePerSecond - 80.0 / 3) < .0001, "lethal overkill capped at remaining health");
        OpenBonkRunMetrics.SetEnabled(false);
        player.inventory.itemInventory.Cleanup(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(player.gameObject);
        Debug.Log("EchoDebuffPortCheck passed: original timer, accumulation, removal ordering and release damage");
    }
}
