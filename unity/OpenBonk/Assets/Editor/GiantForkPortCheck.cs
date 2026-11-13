using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Assets.Scripts.Actors;
using Assets.Scripts.Actors.Enemies;
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Items.ItemImplementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Weapons;
using Assets.Scripts.Menu.Shop;
using UnityEngine;

public static class GiantForkPortCheck
{
    sealed class FixedRandom : System.Random { readonly double value; public FixedRandom(double value) { this.value = value; } public override double NextDouble() => value; }
    static void Require(bool condition, string text) { if (!condition) throw new Exception("GiantForkPortCheck: " + text); }
    public static void Run() {
        var prior = global::Utility.MyRandom.random; var inventory = new ItemInventory(); var go = new GameObject("fork enemy"); go.SetActive(false); var enemy = go.AddComponent<Enemy>();
        var root = new GameObject("fork player"); root.SetActive(false); var player = root.AddComponent<MyPlayer>(); MyPlayer.Instance = player; GameManager.Instance = root.AddComponent<GameManager>();
        player.inventory = (PlayerInventory)FormatterServices.GetUninitializedObject(typeof(PlayerInventory)); player.inventory.itemInventory = inventory;
        var stats = (PlayerStatsNew)FormatterServices.GetUninitializedObject(typeof(PlayerStatsNew)); stats.stats = new Dictionary<EStat,float> { [EStat.CritChance] = 1, [EStat.CritDamage] = 1, [EStat.DamageMultiplier] = 2 }; stats.statValuesMap = new Dictionary<EStat,StatComponents>();
        var damageStat = new StatComponents(); damageStat.SetValues(1, 1, 2); stats.statValuesMap[EStat.DamageMultiplier] = damageStat; player.inventory.playerStats = stats;
        try {
            inventory.AddItem(EItem.GiantFork); var fork = (ItemGiantFork)inventory.GetItem(EItem.GiantFork);
            var stat = fork.statModifiers[EStat.CritChance].GetModifiers().Single(); Require(stat.modifyType == EStatModifyType.Flat && Mathf.Abs(stat.modification - .15f) < .0001f, "first crit bonus");
            var modifier = new StatComponents(); global::Utility.MyRandom.random = new FixedRandom(.13);
            var critical = new DamageContainer(1f, "test") { crit = true, enemy = enemy }; inventory.PreAttack(critical, modifier);
            Require(critical.damageEffect == EDamageEffect.Megacrit && modifier.hasModifications && Mathf.Abs(modifier.multiplicativeValue - 4f) < .0001f, "first-stack critical proc");
            global::Utility.MyRandom.random = new FixedRandom((double).14f); critical = new DamageContainer(1f, "test") { crit = true, enemy = enemy }; inventory.PreAttack(critical, modifier);
            Require(critical.damageEffect == EDamageEffect.None && !modifier.hasModifications, "chance strict boundary");
            global::Utility.MyRandom.random = new FixedRandom(0); var normal = new DamageContainer(1f, "test") { enemy = enemy }; inventory.PreAttack(normal, modifier); Require(!modifier.hasModifications, "non-critical cannot proc");
            var noEnemy = new DamageContainer(1f, "test") { crit = true }; inventory.PreAttack(noEnemy, modifier); Require(!modifier.hasModifications, "missing enemy cannot proc");
            inventory.AddItem(EItem.GiantFork); stat = fork.statModifiers[EStat.CritChance].GetModifiers().Single(); Require(Mathf.Abs(stat.modification - .3f) < .0001f, "stacked crit bonus");
            global::Utility.MyRandom.random = new FixedRandom(.139); var halfProc = new DamageContainer(.5f, "test") { crit = true, enemy = enemy }; inventory.PreAttack(halfProc, modifier);
            Require(halfProc.damageEffect == EDamageEffect.Megacrit && Mathf.Abs(modifier.multiplicativeValue - 4.15f) < .0001f, "stacked mega crit and proc coefficient");
            global::Utility.MyRandom.random = new FixedRandom(.01); var actualDamage = WeaponUtility.GetDamageContainer(new DamageContainer(0, ""), 10f, 1f, "test", Vector3.forward, enemy);
            Require(actualDamage.crit && actualDamage.damageEffect == EDamageEffect.Megacrit && Mathf.Abs(actualDamage.damage - 166f) < .01f, "weapon damage pipeline applies mega critical multiplier");
            global::Utility.MyRandom.random = new FixedRandom(.141); halfProc = new DamageContainer(.5f, "test") { crit = true, enemy = enemy }; inventory.PreAttack(halfProc, modifier); Require(!modifier.hasModifications, "proc coefficient chance boundary");
            inventory.RemoveItem(EItem.GiantFork); inventory.RemoveItem(EItem.GiantFork); critical = new DamageContainer(1f, "test") { crit = true, enemy = enemy }; global::Utility.MyRandom.random = new FixedRandom(0); inventory.PreAttack(critical, modifier); Require(!modifier.hasModifications, "removal clears proc registration");
            Debug.Log("GiantForkPortCheck passed: item inventory, stacking, crit gate, probability, coefficient and removal");
        } finally { inventory.Cleanup(); global::Utility.MyRandom.random = prior; MyPlayer.Instance = null; GameManager.Instance = null; UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(root); }
    }
}
